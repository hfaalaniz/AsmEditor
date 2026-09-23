# Compila desde EL EDITOR, accionando su interfaz de verdad.
#
# Verifica lo que ningún otro diagnóstico cubre: que la cadena que ARMA EL
# EDITOR —no un comando escrito a mano en un script— ensambla, enlaza y produce
# un .exe de la arquitectura correcta.
#
# ⚠ SE ACCIONA EL BOTÓN DE LA BARRA, NO EL MENÚ. Dos caminos fallaron antes:
#
#   1. SendKeys. El «Ctrl+Shift+B» terminó escribiéndose DENTRO del editor en
#      vez de disparar el atajo: dejó el archivo modificado y un diálogo de
#      guardado bloqueando todo. El «Alt+C» tampoco abrió el menú porque el foco
#      no siempre llega.
#
#   2. El menú por UI Automation. Los ToolStripMenuItem de WinForms solo existen
#      en el árbol mientras el desplegable está abierto, y el desplegable se
#      cierra entre llamadas: el ítem no se puede hallar e invocar de forma
#      confiable.
#
#   El ToolStripButton de la barra, en cambio, está siempre visible y habilitado,
#   y llama exactamente al mismo BuildOnlyAsync que el menú.
#
# Es para leer yo.

param(
    # Vacio = se elige segun el ensamblador del target activo (ver abajo).
    [string]$Fuente = '',
    [string]$Boton  = 'Compilar y enlazar',
    [int]$EsperaSeg = 30
)

$ErrorActionPreference = 'Stop'

Add-Type -AssemblyName UIAutomationClient, UIAutomationTypes

# Editor aislado: no usa la configuracion ni los proyectos de Fabian.
. "$PSScriptRoot\editor_aislado.ps1"
# Conserva el target activo de Fabian: este script compila con el que haya.
$editor = PrepararEditorAislado -ConservarTargetActivo

if (-not (Test-Path $editor)) { throw "Falta el editor compilado $editor" }

# ⚠ EL FUENTE TIENE QUE SER DEL ENSAMBLADOR DEL TARGET ACTIVO. Antes el
# default era siempre masm64.asm, y con un target de NASM (el activo de Fabian
# era "NASM 64 + MSVC link") NASM no puede ensamblar sintaxis de MASM: la
# prueba fallaba por el fuente, no por el editor. Se lee el target activo de
# la configuracion del editor aislado, que es la que va a usar.
if (-not $Fuente) {
    $cfg = Get-Content (Join-Path (Split-Path $editor -Parent) "settings.json") -Raw | ConvertFrom-Json
    $activo = $cfg.Targets[$cfg.ActiveTargetIndex]
    $Fuente = if ($activo.UsaMasm) { 'C:\Users\Fabian\NASM\masm64.asm' } else { 'C:\Users\Fabian\NASM\Win6Win.asm' }
    Write-Host "  target activo en la configuracion: $($activo.Name) -> fuente $(Split-Path $Fuente -Leaf)"
}

# Se compila una COPIA en diagnostico\banco_fuentes\: el .obj y el .exe que se
# borran y regeneran son los de la copia, nunca los de Fabian.
$Fuente = CopiarFuenteABanco $Fuente

$raiz = Split-Path $Fuente -Parent
$base = [System.IO.Path]::GetFileNameWithoutExtension($Fuente)
$obj  = Join-Path $raiz "$base.obj"
$exe  = Join-Path $raiz "$base.exe"

Write-Host "  fuente (copia): $Fuente"

# ── Partir de limpio ────────────────────────────────────────────────────
# Solo el editor aislado: el de Fabian, si esta abierto, no se toca.
CerrarEditorAislado
Start-Sleep -Milliseconds 900
Remove-Item $obj -ErrorAction SilentlyContinue
Remove-Item $exe -ErrorAction SilentlyContinue

Write-Host "=== 1. Abrir el editor ==="
$p = Start-Process $editor -ArgumentList $Fuente -PassThru

# El splash tarda: se espera a que aparezca la ventana grande, no un tiempo fijo.
[Windows.Automation.AutomationElement]$ventana = $null
$reloj = [Diagnostics.Stopwatch]::StartNew()
while ($reloj.Elapsed.TotalSeconds -lt 30) {
    # ⚠ El arbol cambia mientras el editor arranca (splash que se va, ventana
    # que aparece): un elemento que desaparece a mitad de la consulta hace que
    # FindAll tire, y el 23/09 eso tumbo la corrida. Se reintenta, igual que en
    # probar_combinaciones.ps1.
    try {
        $cond = New-Object Windows.Automation.PropertyCondition(
            [Windows.Automation.AutomationElement]::ProcessIdProperty, $p.Id)
        foreach ($w in [Windows.Automation.AutomationElement]::RootElement.FindAll(
                            [Windows.Automation.TreeScope]::Children, $cond)) {
            if ($w.Current.BoundingRectangle.Width -gt 900) { $ventana = $w; break }
        }
    }
    catch { $ventana = $null }

    if ($ventana) { break }
    Start-Sleep -Milliseconds 400
}

if (-not $ventana) { Write-Host "FALLA: no apareció la ventana principal" -ForegroundColor Red; exit 1 }
Write-Host ("  ventana: '{0}'" -f $ventana.Current.Name) -ForegroundColor Green

# ── Qué target está activo ──────────────────────────────────────────────
# Sin esto no se sabe QUÉ se está probando: el mismo botón compila con NASM o
# con MASM según lo que esté elegido.
$cbCond = New-Object Windows.Automation.PropertyCondition(
    [Windows.Automation.AutomationElement]::ControlTypeProperty,
    [Windows.Automation.ControlType]::ComboBox)
# ⚠ El combo del editor expone ValuePattern, NO SelectionPattern: pedirle la
#   selección no devuelve nada. El nombre del elemento ya trae el target entero.
$combo = $ventana.FindFirst([Windows.Automation.TreeScope]::Descendants, $cbCond)
if ($combo -and $combo.Current.Name) {
    Write-Host ("  target activo: {0}" -f $combo.Current.Name) -ForegroundColor Cyan
} else {
    Write-Host "  (no se pudo leer el target activo)" -ForegroundColor Yellow
}

# ── El botón de la barra ────────────────────────────────────────────────
Write-Host ""
Write-Host "=== 2. Botón '$Boton' ==="

$cond = New-Object Windows.Automation.PropertyCondition(
    [Windows.Automation.AutomationElement]::NameProperty, $Boton)
[Windows.Automation.AutomationElement]$btn =
    $ventana.FindFirst([Windows.Automation.TreeScope]::Descendants, $cond)

if (-not $btn) {
    Write-Host "FALLA: no se halló el botón. Los que hay:" -ForegroundColor Red
    $ct = New-Object Windows.Automation.PropertyCondition(
        [Windows.Automation.AutomationElement]::ControlTypeProperty,
        [Windows.Automation.ControlType]::Button)
    foreach ($x in $ventana.FindAll([Windows.Automation.TreeScope]::Descendants, $ct)) {
        if ($x.Current.Name) { Write-Host "    '$($x.Current.Name)'" }
    }
    exit 1
}

if (-not $btn.Current.IsEnabled) {
    Write-Host "FALLA: el botón está deshabilitado (¿hay un archivo abierto?)" -ForegroundColor Red
    exit 1
}

$ip = $null
if (-not $btn.TryGetCurrentPattern([Windows.Automation.InvokePattern]::Pattern, [ref]$ip)) {
    Write-Host "FALLA: el botón no soporta Invoke" -ForegroundColor Red
    exit 1
}

$ip.Invoke()
Write-Host "  invocado" -ForegroundColor Green

# ── Esperar el resultado, no un tiempo fijo ─────────────────────────────
Write-Host ""
Write-Host "=== 3. Esperar la compilación ==="
$reloj = [Diagnostics.Stopwatch]::StartNew()
while ($reloj.Elapsed.TotalSeconds -lt $EsperaSeg) {
    if (Test-Path $exe) { break }
    Start-Sleep -Milliseconds 400
}
Write-Host ("  {0:N1} s" -f $reloj.Elapsed.TotalSeconds)

$fallo = $false

if (Test-Path $obj) {
    Write-Host "  OBJ: $((Get-Item $obj).Length) bytes" -ForegroundColor Green
} else {
    Write-Host "  FALLA: el editor no generó el .obj" -ForegroundColor Red
    $fallo = $true
}

if (Test-Path $exe) {
    Write-Host "  EXE: $((Get-Item $exe).Length) bytes" -ForegroundColor Green
} else {
    Write-Host "  FALLA: el editor no generó el .exe" -ForegroundColor Red
    $fallo = $true
}

if ($fallo) {
    Write-Host ""
    Write-Host "  (el editor queda abierto para mirar su panel de salida)" -ForegroundColor Yellow
    exit 1
}

# ── La arquitectura del .exe que produjo EL EDITOR ──────────────────────
Write-Host ""
Write-Host "=== 4. Cabecera PE ==="
$b  = [System.IO.File]::ReadAllBytes($exe)
$pe = [BitConverter]::ToInt32($b, 0x3C)
$m  = [BitConverter]::ToUInt16($b, $pe + 4)
switch ($m) {
    0x8664  { Write-Host "  AMD64 -> 64 bits" -ForegroundColor Green }
    0x014c  { Write-Host "  i386 -> 32 bits"  -ForegroundColor Green }
    default { Write-Host ("  maquina 0x{0:X} INESPERADA" -f $m) -ForegroundColor Red; exit 1 }
}

# ── Que además corra ────────────────────────────────────────────────────
Write-Host ""
Write-Host "=== 5. Ejecutar lo que produjo el editor ==="
$q = Start-Process $exe -PassThru
Start-Sleep -Seconds 2
$q.Refresh()
if ($q.HasExited) {
    Write-Host "  el exe salió con código $($q.ExitCode)" -ForegroundColor Yellow
} else {
    Write-Host "  EL EXE CORRE" -ForegroundColor Green
    Stop-Process -Id $q.Id -Force -ErrorAction SilentlyContinue
}

Write-Host ""
Write-Host "COMPILADO DESDE LA INTERFAZ DEL EDITOR, OK" -ForegroundColor Green

CerrarEditorAislado
