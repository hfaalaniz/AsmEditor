# Compila las TRES combinaciones válidas desde la interfaz del editor.
#
#                     GoLink            MSVC link.exe
#     NASM            sí                sí
#     MASM            (no aplica)       sí
#
# ⚠ MASM + GoLink NO ES UNA COMBINACIÓN VÁLIDA: GoLink enlaza contra los .dll
#   directamente y MASM produce objetos que esperan .lib. No se prueba porque no
#   tiene que funcionar.
#
# Cada combinación se prueba de punta a punta: se elige el target en el combo de
# la barra, se acciona el botón, y se comprueba el .obj, el .exe, su cabecera PE
# y que el ejecutable corra.
#
# Es para leer yo.

param([int]$EsperaSeg = 30)

$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName UIAutomationClient, UIAutomationTypes

# Editor aislado: no usa la configuracion ni los proyectos de Fabian.
. "$PSScriptRoot\editor_aislado.ps1"
$editor = PrepararEditorAislado

# Se compilan COPIAS en diagnostico\banco_fuentes\: el .obj y el .exe que se
# borran y regeneran en cada caso son los de las copias, nunca los de Fabian.
$fuenteNasm = CopiarFuenteABanco 'C:\Users\Fabian\NASM\Win6Win.asm'
$fuenteMasm = CopiarFuenteABanco 'C:\Users\Fabian\NASM\masm64.asm'

# El fuente tiene que coincidir con el ensamblador del target: son sintaxis
# distintas, no dialectos del mismo lenguaje.
#
# El «Distinto» es la comprobación que hace concluyente el cambio en caliente:
# los dos primeros casos ensamblan EL MISMO fuente y producen el mismo .obj,
# así que si el .exe cambia de tamaño es porque corrió otro enlazador. Sin eso,
# un cambio de target que no se aplicara pasaría igual.
$casos = @(
    @{ Target = 'NASM 64 + GoLink';    Fuente = $fuenteNasm; Distinto = $true },
    @{ Target = 'NASM 64 + MSVC link'; Fuente = $fuenteNasm; Distinto = $true },
    @{ Target = 'MASM 64 + MSVC link'; Fuente = $fuenteMasm; Distinto = $false }
)

# Tamaños del .exe por caso, para comparar los que comparten fuente.
$tamanos = @{}

function Buscar($raiz, $tipo, $nombre) {
    $c1 = New-Object Windows.Automation.PropertyCondition(
        [Windows.Automation.AutomationElement]::ControlTypeProperty, $tipo)
    $c2 = New-Object Windows.Automation.PropertyCondition(
        [Windows.Automation.AutomationElement]::NameProperty, $nombre)
    $and = New-Object Windows.Automation.AndCondition($c1, $c2)

    [Windows.Automation.AutomationElement]$hallado =
        $raiz.FindFirst([Windows.Automation.TreeScope]::Descendants, $and)
    return $hallado
}

$fallos = 0

foreach ($caso in $casos) {
    $nombre = $caso.Target
    $fuente = $caso.Fuente
    $raiz = Split-Path $fuente -Parent
    $base = [System.IO.Path]::GetFileNameWithoutExtension($fuente)
    $obj  = Join-Path $raiz "$base.obj"
    $exe  = Join-Path $raiz "$base.exe"

    Write-Host ""
    Write-Host ("=" * 62)
    Write-Host "  $nombre" -ForegroundColor Cyan
    Write-Host ("  fuente: {0}" -f (Split-Path $fuente -Leaf))
    Write-Host ("=" * 62)

    Remove-Item $obj -ErrorAction SilentlyContinue
    Remove-Item $exe -ErrorAction SilentlyContinue

    # ⚠ ESTO VA EN LÍNEA, NO EN UNA FUNCIÓN. Envuelto en función falla de una
    #   forma que engaña: la ventana se halla y su Name es correcto, pero
    #   FindAll sobre ella devuelve CERO controles, como si estuviera vacía.
    #   El proceso queda local a la función y el árbol deja de ser navegable.
    #   Medido: en línea el combo aparece a los 0,1 s; en función no aparece
    #   nunca, ni esperando 20 s.
    # Se espera a que el proceso anterior muera DE VERDAD antes de recorrer el
    # árbol: consultarlo mientras se está cerrando tira
    # ElementNotAvailableException y tumba la corrida entera.
    # Solo el editor aislado: el de Fabian, si esta abierto, no se toca.
    CerrarEditorAislado
    Start-Sleep -Milliseconds 600

    # El target se cambia EN CALIENTE más abajo, con el editor ya abierto: es
    # justamente lo que hay que verificar.

    $proc = Start-Process $editor -ArgumentList $fuente -PassThru

    $v = $null
    $reloj = [Diagnostics.Stopwatch]::StartNew()
    while ($reloj.Elapsed.TotalSeconds -lt 30) {
        # El árbol puede cambiar mientras se lo recorre: un elemento que se va
        # deja de estar disponible y la consulta tira. Se reintenta.
        try {
            $cond = New-Object Windows.Automation.PropertyCondition(
                [Windows.Automation.AutomationElement]::ProcessIdProperty, $proc.Id)
            foreach ($w in [Windows.Automation.AutomationElement]::RootElement.FindAll(
                                [Windows.Automation.TreeScope]::Children, $cond)) {
                if ($w.Current.BoundingRectangle.Width -gt 900) { $v = $w; break }
            }
        }
        catch { $v = $null }

        if ($v) { break }
        Start-Sleep -Milliseconds 400
    }

    if (-not $v) { Write-Host "  FALLA: no abrió el editor" -ForegroundColor Red; $fallos++; continue }

    # ── Cambiar el target EN CALIENTE ──────────────────────────────────
    # Con el editor ya abierto: es parte de lo que se verifica. El editor tiene
    # que empezar a usar el ensamblador y el enlazador nuevos sin reiniciarse.
    $cbCond = New-Object Windows.Automation.PropertyCondition(
        [Windows.Automation.AutomationElement]::ControlTypeProperty,
        [Windows.Automation.ControlType]::ComboBox)
    $combo = $v.FindFirst([Windows.Automation.TreeScope]::Descendants, $cbCond)
    if (-not $combo) { Write-Host "  FALLA: no se halló el combo de targets" -ForegroundColor Red; $fallos++; continue }

    # ⚠ SE SELECCIONA EL ÍTEM, NO SE ESCRIBE EL TEXTO. El combo expone
    #   ValuePattern, pero SetValue solo cambia lo que se lee: no dispara el
    #   SelectedIndexChanged del editor, así que sigue compilando con el target
    #   anterior y el caso pasa o falla por la razón equivocada. Hay que abrir
    #   el desplegable y seleccionar con SelectionItemPattern.
    $ec = $null
    if ($combo.TryGetCurrentPattern(
            [Windows.Automation.ExpandCollapsePattern]::Pattern, [ref]$ec)) {
        $ec.Expand()
        Start-Sleep -Milliseconds 800
    }

    $ctLi = New-Object Windows.Automation.PropertyCondition(
        [Windows.Automation.AutomationElement]::ControlTypeProperty,
        [Windows.Automation.ControlType]::ListItem)

    $elegido = $false
    foreach ($it in $combo.FindAll([Windows.Automation.TreeScope]::Descendants, $ctLi)) {
        if ($it.Current.Name -like "$nombre*") {
            $si = $null
            if ($it.TryGetCurrentPattern(
                    [Windows.Automation.SelectionItemPattern]::Pattern, [ref]$si)) {
                $si.Select()
                $elegido = $true
            }
            break
        }
    }

    if ($ec) { $ec.Collapse() }
    Start-Sleep -Milliseconds 800

    if (-not $elegido) {
        Write-Host "  FALLA: no se pudo elegir el target en el combo" -ForegroundColor Red
        $fallos++; continue
    }

    Write-Host ("  target (en caliente): {0}" -f $combo.Current.Name)

    if ($combo.Current.Name -notlike "$nombre*") {
        Write-Host "  FALLA: el target no quedó seleccionado" -ForegroundColor Red; $fallos++; continue
    }

    # ── Compilar ────────────────────────────────────────────────────────
    $btn = Buscar $v ([Windows.Automation.ControlType]::Button) 'Compilar y enlazar'
    if (-not $btn -or -not $btn.Current.IsEnabled) {
        Write-Host "  FALLA: el botón no está disponible" -ForegroundColor Red; $fallos++; continue
    }

    $ip = $null
    [void]$btn.TryGetCurrentPattern([Windows.Automation.InvokePattern]::Pattern, [ref]$ip)
    $ip.Invoke()

    $reloj = [Diagnostics.Stopwatch]::StartNew()
    while ($reloj.Elapsed.TotalSeconds -lt $EsperaSeg) {
        if (Test-Path $exe) { break }
        Start-Sleep -Milliseconds 400
    }

    # ── Comprobar ───────────────────────────────────────────────────────
    if (-not (Test-Path $obj)) {
        Write-Host "  FALLA: sin .obj" -ForegroundColor Red; $fallos++; continue
    }
    if (-not (Test-Path $exe)) {
        Write-Host "  FALLA: sin .exe" -ForegroundColor Red; $fallos++; continue
    }

    Write-Host ("  OBJ: {0} bytes    EXE: {1} bytes    {2:N1} s" -f `
        (Get-Item $obj).Length, (Get-Item $exe).Length, $reloj.Elapsed.TotalSeconds)

    $b  = [System.IO.File]::ReadAllBytes($exe)
    $pe = [BitConverter]::ToInt32($b, 0x3C)
    $m  = [BitConverter]::ToUInt16($b, $pe + 4)
    if ($m -ne 0x8664) {
        Write-Host ("  FALLA: la cabecera PE dice 0x{0:X}, se esperaba AMD64" -f $m) -ForegroundColor Red
        $fallos++; continue
    }
    Write-Host "  PE: AMD64 (64 bits)"

    $q = Start-Process $exe -PassThru
    Start-Sleep -Seconds 2
    $q.Refresh()
    if ($q.HasExited) {
        Write-Host ("  el exe salió con código {0}" -f $q.ExitCode) -ForegroundColor Yellow
    } else {
        Write-Host "  EL EXE CORRE" -ForegroundColor Green
        Stop-Process -Id $q.Id -Force -ErrorAction SilentlyContinue
    }

    if ($caso.Distinto) { $tamanos[$nombre] = (Get-Item $exe).Length }

    Write-Host "  OK" -ForegroundColor Green
}

# ── La prueba de que el cambio en caliente se aplicó ────────────────────
# Mismo fuente y mismo .obj: si el .exe pesa distinto es porque enlazó otro
# enlazador. Si pesaran igual, el target no se habría aplicado.
if ($tamanos.Count -ge 2) {
    Write-Host ""
    Write-Host "=== ¿El cambio de target en caliente se aplicó? ==="
    foreach ($k in $tamanos.Keys) { Write-Host ("  {0,-22} {1} bytes" -f $k, $tamanos[$k]) }

    $distintos = ($tamanos.Values | Select-Object -Unique).Count
    if ($distintos -ge 2) {
        Write-Host "  SÍ: el mismo fuente dio .exe distintos -> enlazó cada enlazador" -ForegroundColor Green
    } else {
        Write-Host "  NO: el mismo tamaño con los dos targets -> el cambio NO se aplicó" -ForegroundColor Red
        $fallos++
    }
}

CerrarEditorAislado

Write-Host ""
Write-Host ("=" * 62)
if ($fallos -eq 0) {
    Write-Host "LAS $($casos.Count) COMBINACIONES COMPILAN DESDE EL EDITOR" -ForegroundColor Green
} else {
    Write-Host "$fallos de $($casos.Count) combinaciones FALLARON" -ForegroundColor Red
    exit 1
}
