# ============================================================================
# Prueba de la VENTANA DE INICIO por la interfaz (PLAN_IDE.md, Etapa 1).
#
#   A. Aparece sola, con el IDE todavia oculto; recientes agrupados por fecha
#      (un reciente que ya no existe no se muestra); Alt+U y el filtro; Supr
#      quita de la lista y queda guardado; Enter abre el proyecto CON SU
#      SESION (sus archivos y la pestana activa); al salir se guarda la sesion.
#   B. Esc la cierra y cierra el editor.
#   C. "Continuar sin codigo": el IDE vacio, sin proyecto.
#   D. "Abrir un archivo o proyecto": dialogo, y el IDE abre el archivo.
#   E. "Crear un proyecto": el dialogo sale con el IDE ya visible (Shown).
#   F. Al iniciar = ultimo proyecto: sin ventana de inicio, con su sesion.
#   G. Al iniciar = entorno vacio: sin ventana de inicio, sin proyecto.
#   H. Un archivo por linea de comandos: sin ventana de inicio.
#
# Editor AISLADO (editor_aislado.ps1) y teclas/clics PROTEGIDOS
# (proteccion_interfaz.ps1): nada va a otra ventana que no sea del editor.
# Solo se leen titulos de ventanas DEL EDITOR (VI.Titulo lo exige).
#
# Es para que lo lea yo, no forma parte de la interfaz.
# ============================================================================

$ErrorActionPreference = "Stop"

. "$PSScriptRoot\editor_aislado.ps1"
. "$PSScriptRoot\proteccion_interfaz.ps1"

Add-Type @"
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
public static class VI {
    public struct RECT { public int Left, Top, Right, Bottom; }
    delegate bool EnumProc(IntPtr h, IntPtr p);
    [DllImport("user32.dll")] static extern bool EnumWindows(EnumProc cb, IntPtr p);
    [DllImport("user32.dll")] static extern bool EnumChildWindows(IntPtr h, EnumProc cb, IntPtr p);
    [DllImport("user32.dll")] static extern uint GetWindowThreadProcessId(IntPtr h, out uint pid);
    [DllImport("user32.dll")] public static extern bool IsWindowVisible(IntPtr h);
    [DllImport("user32.dll", CharSet=CharSet.Unicode)] static extern int GetWindowTextW(IntPtr h, StringBuilder s, int n);
    [DllImport("user32.dll", CharSet=CharSet.Unicode)] static extern int GetClassNameW(IntPtr h, StringBuilder s, int n);
    [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr h, out RECT r);
    [DllImport("user32.dll")] static extern IntPtr SendMessageW(IntPtr h, int m, IntPtr w, IntPtr l);
    [DllImport("user32.dll", CharSet=CharSet.Unicode, EntryPoint="SendMessageW")] static extern IntPtr SendMessageTexto(IntPtr h, int m, IntPtr w, StringBuilder l);
    [DllImport("user32.dll")] public static extern bool PostMessageW(IntPtr h, int m, IntPtr w, IntPtr l);

    static uint Pid(IntPtr h) { uint p; GetWindowThreadProcessId(h, out p); return p; }

    public static List<IntPtr> Ventanas(uint pid) {
        var r = new List<IntPtr>();
        EnumWindows(delegate(IntPtr h, IntPtr p) {
            if (Pid(h) == pid && IsWindowVisible(h)) r.Add(h);
            return true;
        }, IntPtr.Zero);
        return r;
    }

    // SOLO de ventanas del proceso indicado: el titulo de una ventana ajena
    // no se lee nunca (puede mostrar lo que Fabian tiene abierto).
    public static string Titulo(IntPtr h, uint pid) {
        if (Pid(h) != pid) return "";
        var sb = new StringBuilder(1024); GetWindowTextW(h, sb, 1024); return sb.ToString();
    }

    public static string Clase(IntPtr h) { var sb = new StringBuilder(256); GetClassNameW(h, sb, 256); return sb.ToString(); }

    // Recorre todas las hijas (EnumChildWindows ya es recursivo). Las hijas de
    // una ventana del editor son del editor.
    public static IntPtr Hija(IntPtr raiz, string clase, string texto) {
        IntPtr hallada = IntPtr.Zero;
        uint pid = Pid(raiz);
        EnumChildWindows(raiz, delegate(IntPtr h, IntPtr p) {
            if (!IsWindowVisible(h)) return true;
            if (!Clase(h).ToUpperInvariant().Contains(clase)) return true;
            if (texto != null && Titulo(h, pid) != texto) return true;
            hallada = h; return false;
        }, IntPtr.Zero);
        return hallada;
    }

    // Los renglones de un ListBox (WinForms usa LBS_HASSTRINGS aun en owner-draw).
    public static List<string> Items(IntPtr lista) {
        var r = new List<string>();
        int n = (int)SendMessageW(lista, 0x018B, IntPtr.Zero, IntPtr.Zero);
        for (int i = 0; i < n; i++) {
            int largo = (int)SendMessageW(lista, 0x018A, (IntPtr)i, IntPtr.Zero);
            var sb = new StringBuilder(Math.Max(largo, 0) + 1);
            SendMessageTexto(lista, 0x0189, (IntPtr)i, sb);
            r.Add(sb.ToString());
        }
        return r;
    }

    public static int Seleccion(IntPtr lista) { return (int)SendMessageW(lista, 0x0188, IntPtr.Zero, IntPtr.Zero); }

    public static int[] Centro(IntPtr h) { RECT r; GetWindowRect(h, out r); return new int[] { (r.Left + r.Right) / 2, (r.Top + r.Bottom) / 2 }; }
}
"@

$fallas = 0
function Bien($t) { Write-Host "  OK   $t" -ForegroundColor Green }
function Mal($t)  { Write-Host "  MAL  $t" -ForegroundColor Red; $script:fallas++ }
function Titulo($t) { Write-Host ""; Write-Host "=== $t ===" -ForegroundColor Cyan }

# Sin caracteres fuera de ASCII en el script: se arman con [char].
$tituloInicio   = "Editor ASM $([char]0x2014) Inicio"
$finTituloIDE   = "Editor ASM (NASM + GoLink)"
$textoContinuar = "Continuar sin c$([char]0xF3)digo"

# ---------------------------------------------------------------------------
Titulo "Preparando el banco"

$exe        = PrepararEditorAislado
$cfgAislado = Join-Path (Split-Path $exe -Parent) "settings.json"
$banco      = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot "banco_inicio"))

if (Test-Path $banco) { Remove-Item $banco -Recurse -Force }

function Proyecto($nombre, [string[]]$archivos) {
    $dir = Join-Path $banco $nombre.ToLower()
    New-Item -ItemType Directory -Path $dir | Out-Null
    foreach ($a in $archivos) { Set-Content (Join-Path $dir $a) "; $a" -Encoding ASCII }
    $json = @{
        Version  = 1
        Proyecto = @{ Nombre = $nombre; ArchivoPrincipal = $archivos[0]; CarpetaSalida = "bin"
                      Archivos = $archivos; Targets = @(); TargetActivo = 0 }
    } | ConvertTo-Json -Depth 6
    $ruta = Join-Path $dir "$nombre.asmproj"
    Set-Content $ruta $json -Encoding UTF8
    return $ruta
}

$alfa  = Proyecto "Alfa"  @("uno.asm", "dos.asm")
$beta  = Proyecto "Beta"  @("beta.asm")
$gamma = Proyecto "Gamma" @("gamma.asm")
$borrado = Join-Path $banco "borrado\Borrado.asmproj"     # nunca existe
$uno = Join-Path (Split-Path $alfa) "uno.asm"
$dos = Join-Path (Split-Path $alfa) "dos.asm"

# Un archivo suelto, en una carpeta SIN .asmproj (no pregunta por proyecto).
New-Item -ItemType Directory -Path (Join-Path $banco "suelto") | Out-Null
$suelto = Join-Path $banco "suelto\suelto.asm"
Set-Content $suelto "; suelto" -Encoding ASCII

function EscribirUi([scriptblock]$cambiar) {
    $cfg = Get-Content $cfgAislado -Raw -Encoding UTF8 | ConvertFrom-Json
    & $cambiar $cfg.Ui
    [IO.File]::WriteAllText($cfgAislado, ($cfg | ConvertTo-Json -Depth 20), (New-Object System.Text.UTF8Encoding($false)))
}
function LeerUi { return (Get-Content $cfgAislado -Raw -Encoding UTF8 | ConvertFrom-Json).Ui }

# Valor de un "diccionario" del JSON (objeto con rutas como nombres).
function Clave($objeto, $ruta) {
    if ($null -eq $objeto) { return $null }
    $pr = @($objeto.PSObject.Properties | Where-Object { $_.Name -ieq $ruta })
    if ($pr.Count -eq 0) { return $null }
    return $pr[0].Value
}

$ahora = Get-Date
EscribirUi {
    param($ui)
    $ui | Add-Member -NotePropertyName AlIniciar -NotePropertyValue 0 -Force
    $ui | Add-Member -NotePropertyName ProyectoAbierto -NotePropertyValue $null -Force
    $ui.RecentProjects = @($gamma, $borrado, $beta, $alfa)
    $fechas = [ordered]@{}
    $fechas[$alfa]  = $ahora.AddMinutes(-1).ToString("s")
    $fechas[$beta]  = $ahora.Date.AddDays(-1).AddHours(12).ToString("s")
    $fechas[$gamma] = $ahora.Date.AddDays(-60).ToString("s")
    $ui | Add-Member -NotePropertyName FechasProyectos -NotePropertyValue ([pscustomobject]$fechas) -Force
    $sesiones = [ordered]@{}
    $sesiones[$alfa] = [pscustomobject]@{ Archivos = @($uno, $dos); Activo = 1 }
    $ui | Add-Member -NotePropertyName SesionesProyectos -NotePropertyValue ([pscustomobject]$sesiones) -Force
}
Write-Host "  banco: $banco"

# ---------------------------------------------------------------------------
# Ventanas del editor
# ---------------------------------------------------------------------------

function Lanzar([string]$archivo) {
    if ($archivo) { return Start-Process $exe -ArgumentList "`"$archivo`"" -PassThru }
    return Start-Process $exe -PassThru
}

function Buscar($p, [scriptblock]$filtro) {
    foreach ($h in [VI]::Ventanas([uint32]$p.Id)) {
        if (& $filtro $h ([VI]::Titulo($h, [uint32]$p.Id))) { return $h }
    }
    return [IntPtr]::Zero
}

function Esperar($p, [scriptblock]$filtro, $segundos = 25) {
    $reloj = [Diagnostics.Stopwatch]::StartNew()
    while ($reloj.Elapsed.TotalSeconds -lt $segundos -and -not $p.HasExited) {
        $h = Buscar $p $filtro
        if ($h -ne [IntPtr]::Zero) { return $h }
        Start-Sleep -Milliseconds 250
    }
    return [IntPtr]::Zero
}

$esInicio  = { param($h, $t) $t -eq $tituloInicio }
$esIDE     = { param($h, $t) $t.EndsWith($finTituloIDE) }
$esAlguna  = { param($h, $t) ($t -eq $tituloInicio) -or $t.EndsWith($finTituloIDE) }
$esDialogo = { param($h, $t) [VI]::Clase($h) -eq "#32770" }

function TituloDe($p, $h) { return [VI]::Titulo($h, [uint32]$p.Id) }

function Clic($p, $h) {
    $c = [VI]::Centro($h)
    return (ClicProtegido $p $c[0] $c[1])
}

# Cierra el IDE como el usuario: WM_CLOSE y "Salir" en el dialogo de salida.
# Asi corre OnFormClosing y se guarda la sesion (matarlo no la guardaria).
function CerrarIDE($p, $ide) {
    [void](TraerAlFrente $p $ide)
    [void][VI]::PostMessageW($ide, 0x10, [IntPtr]::Zero, [IntPtr]::Zero)

    $salir = [IntPtr]::Zero
    $reloj = [Diagnostics.Stopwatch]::StartNew()
    while ($salir -eq [IntPtr]::Zero -and $reloj.Elapsed.TotalSeconds -lt 8 -and -not $p.HasExited) {
        foreach ($v in [VI]::Ventanas([uint32]$p.Id)) {
            $b = [VI]::Hija($v, "BUTTON", "Salir")
            if ($b -ne [IntPtr]::Zero) { $salir = $b; break }
        }
        Start-Sleep -Milliseconds 250
    }

    if ($salir -ne [IntPtr]::Zero) { [void](Clic $p $salir) }
    if (-not $p.WaitForExit(15000)) {
        Mal "el editor no se cerro con Salir: se lo mata"
        CerrarEditorAislado
        return $false
    }
    return $true
}

function Lista($p, $inicio) { return [VI]::Hija($inicio, "LISTBOX", $null) }
function Renglones($lista) { return (@([VI]::Items($lista)) -join " | ") }

# ===========================================================================
try {

Titulo "A. Ventana de inicio: recientes, filtro, quitar, abrir con sesion"

$p = Lanzar
$inicio = Esperar $p $esAlguna
if ($inicio -eq [IntPtr]::Zero -or (TituloDe $p $inicio) -ne $tituloInicio) {
    Mal "no aparecio la ventana de inicio"
} else {
    Bien "aparecio la ventana de inicio"

    if ((Buscar $p $esIDE) -eq [IntPtr]::Zero) { Bien "el IDE todavia no se ve" } else { Mal "el IDE ya estaba visible detras" }

    $lista = Lista $p $inicio
    $esperado = "Hoy | Alfa | Ayer | Beta | Anterior | Gamma"
    $hay = Renglones $lista
    if ($hay -eq $esperado) { Bien "recientes agrupados: $hay" } else { Mal "recientes: '$hay' (esperaba '$esperado')" }
    if ($hay -notlike "*Borrado*") { Bien "el reciente que no existe no aparece" } else { Mal "aparece un reciente inexistente" }

    if (TraerAlFrente $p $inicio) {
        [void](TeclasProtegidas $p "%u" 400)
        [void](TeclasProtegidas $p "bet" 700)
        $hay = Renglones $lista
        if ($hay -eq "Ayer | Beta") { Bien "Alt+U y filtro 'bet': $hay" } else { Mal "filtro 'bet': '$hay'" }

        [void](TeclasProtegidas $p "{DOWN}" 400)
        if ([VI]::Seleccion($lista) -eq 1) { Bien "flecha abajo pasa a la lista, en Beta" } else { Mal "seleccion tras flecha abajo: $([VI]::Seleccion($lista))" }

        [void](TeclasProtegidas $p "{DELETE}" 900)
        $hay = Renglones $lista
        if ($hay -eq "") { Bien "Supr quito Beta de la lista" } else { Mal "tras Supr la lista es '$hay'" }
        $sinRecientes = [VI]::Hija($inicio, "STATIC", "Ning$([char]0xFA)n proyecto coincide con la b$([char]0xFA)squeda.")
        if ($sinRecientes -ne [IntPtr]::Zero) { Bien "avisa que nada coincide" } else { Mal "no aparece el aviso de lista vacia" }

        $ui = LeerUi
        if (@($ui.RecentProjects) -notcontains $beta -and $null -eq (Clave $ui.FechasProyectos $beta)) {
            Bien "Beta quedo fuera de settings.json (recientes y fechas)"
        } else { Mal "Beta sigue en settings.json" }
        if (Test-Path $beta) { Bien "el proyecto Beta sigue en el disco" } else { Mal "se borro el archivo de Beta" }

        [void](TeclasProtegidas $p "%u" 400)
        [void](TeclasProtegidas $p "{END}{BACKSPACE 3}" 700)
        $hay = Renglones $lista
        if ($hay -eq "Hoy | Alfa | Anterior | Gamma") { Bien "sin filtro: $hay" } else { Mal "sin filtro: '$hay'" }

        [void](TeclasProtegidas $p "alfa{ENTER}" 500)
    } else { Mal "no se pudo traer la ventana de inicio al frente" }

    $ide = Esperar $p $esIDE
    if ($ide -eq [IntPtr]::Zero) {
        Mal "Enter no abrio el IDE"
    } else {
        Start-Sleep -Milliseconds 800
        if ((Buscar $p $esInicio) -eq [IntPtr]::Zero) { Bien "la ventana de inicio se cerro" } else { Mal "la ventana de inicio sigue abierta" }
        $t = TituloDe $p $ide
        if ($t.Contains("[Alfa]") -and $t.Contains("dos.asm")) { Bien "IDE con Alfa y su pestana activa (dos.asm)" } else { Mal "titulo del IDE: '$t'" }

        # Se cierra la pestana activa: la sesion guardada al salir tiene que
        # quedar con uno.asm solo. Asi se ve que se GUARDA, no que se conserva.
        if (TraerAlFrente $p $ide) { [void](TeclasProtegidas $p "^w" 900) }
        $t = TituloDe $p $ide
        if ($t.Contains("uno.asm")) { Bien "Ctrl+W cerro dos.asm; queda uno.asm" } else { Mal "tras Ctrl+W el titulo es '$t'" }

        if (CerrarIDE $p $ide) {
            $ui = LeerUi
            $s = Clave $ui.SesionesProyectos $alfa
            $archivos = @($s.Archivos)
            if ($archivos.Count -eq 1 -and $archivos[0] -ieq $uno -and $s.Activo -eq 0) { Bien "sesion de Alfa guardada al salir: uno.asm, activo 0" }
            else { Mal "sesion de Alfa: $($archivos -join ', ') / activo $($s.Activo)" }
            if ($ui.ProyectoAbierto -ieq $alfa) { Bien "ProyectoAbierto = Alfa" } else { Mal "ProyectoAbierto = '$($ui.ProyectoAbierto)'" }
            $f = Clave $ui.FechasProyectos $alfa
            if ($f -and ([datetime]$f).Date -eq $ahora.Date) { Bien "la fecha de Alfa es de hoy" } else { Mal "fecha de Alfa: '$f'" }
        }
    }
}
CerrarEditorAislado

# ---------------------------------------------------------------------------
Titulo "B. Esc cierra la ventana de inicio y el editor"

$p = Lanzar
$inicio = Esperar $p $esInicio
if ($inicio -eq [IntPtr]::Zero) { Mal "no aparecio la ventana de inicio" }
elseif (TraerAlFrente $p $inicio) {
    [void](TeclasProtegidas $p "{ESC}" 300)
    if ($p.WaitForExit(10000)) { Bien "Esc cerro el editor sin mostrar el IDE" } else { Mal "el editor sigue corriendo tras Esc" }
} else { Mal "no se pudo traer la ventana de inicio al frente" }
CerrarEditorAislado

# ---------------------------------------------------------------------------
Titulo "C. Continuar sin codigo"

$p = Lanzar
$inicio = Esperar $p $esInicio
if ($inicio -eq [IntPtr]::Zero) { Mal "no aparecio la ventana de inicio" }
elseif (TraerAlFrente $p $inicio) {
    $boton = [VI]::Hija($inicio, "BUTTON", $textoContinuar)
    if ($boton -eq [IntPtr]::Zero) { Mal "no encuentro el boton Continuar sin codigo" }
    else {
        [void](Clic $p $boton)
        $ide = Esperar $p $esIDE
        if ($ide -eq [IntPtr]::Zero) { Mal "no se abrio el IDE" }
        else {
            $t = TituloDe $p $ide
            if ($t -eq $finTituloIDE) { Bien "IDE vacio, sin proyecto ni archivos" } else { Mal "titulo: '$t'" }
            [void](CerrarIDE $p $ide)
        }
    }
} else { Mal "no se pudo traer la ventana de inicio al frente" }
CerrarEditorAislado

# ---------------------------------------------------------------------------
Titulo "D. Abrir un archivo o proyecto"

$p = Lanzar
$inicio = Esperar $p $esInicio
if ($inicio -eq [IntPtr]::Zero) { Mal "no aparecio la ventana de inicio" }
elseif (TraerAlFrente $p $inicio) {
    $accion = [VI]::Hija($inicio, "STATIC", "Abrir un archivo o proyecto")
    if ($accion -eq [IntPtr]::Zero) { Mal "no encuentro la accion Abrir un archivo o proyecto" }
    else {
        [void](Clic $p $accion)
        $dlg = Esperar $p $esDialogo 10
        if ($dlg -eq [IntPtr]::Zero) { Mal "no se abrio el dialogo de abrir" }
        else {
            Bien "se abrio el dialogo: '$(TituloDe $p $dlg)'"
            if (TraerAlFrente $p $dlg) { [void](TeclasProtegidas $p "$suelto{ENTER}" 500) }
            $ide = Esperar $p $esIDE
            if ($ide -eq [IntPtr]::Zero) { Mal "no se abrio el IDE" }
            else {
                Start-Sleep -Milliseconds 800
                $t = TituloDe $p $ide
                if ($t.Contains("suelto.asm") -and -not $t.Contains("[")) { Bien "IDE con suelto.asm, sin proyecto" } else { Mal "titulo: '$t'" }
                [void](CerrarIDE $p $ide)
            }
        }
    }
} else { Mal "no se pudo traer la ventana de inicio al frente" }
CerrarEditorAislado

# ---------------------------------------------------------------------------
Titulo "E. Crear un proyecto"

$p = Lanzar
$inicio = Esperar $p $esInicio
if ($inicio -eq [IntPtr]::Zero) { Mal "no aparecio la ventana de inicio" }
elseif (TraerAlFrente $p $inicio) {
    $accion = [VI]::Hija($inicio, "STATIC", "Crear un proyecto")
    if ($accion -eq [IntPtr]::Zero) { Mal "no encuentro la accion Crear un proyecto" }
    else {
        [void](Clic $p $accion)
        $dlg = Esperar $p { param($h, $t) ([VI]::Clase($h) -eq "#32770") -and $t -eq "Nuevo proyecto" } 15
        if ($dlg -eq [IntPtr]::Zero) { Mal "no aparecio el dialogo Nuevo proyecto" }
        else {
            if ((Buscar $p $esIDE) -ne [IntPtr]::Zero) { Bien "el dialogo Nuevo proyecto salio con el IDE visible" } else { Mal "el dialogo salio sin el IDE" }
            if (TraerAlFrente $p $dlg) { [void](TeclasProtegidas $p "{ESC}" 800) }
            $ide = Buscar $p $esIDE
            $t = TituloDe $p $ide
            if ((Buscar $p $esDialogo) -eq [IntPtr]::Zero -and $t -eq $finTituloIDE) { Bien "cancelado: IDE vacio, sin proyecto" } else { Mal "tras cancelar: '$t'" }
            [void](CerrarIDE $p $ide)
        }
    }
} else { Mal "no se pudo traer la ventana de inicio al frente" }
CerrarEditorAislado

# ---------------------------------------------------------------------------
Titulo "F. Al iniciar = ultimo proyecto"

# "Ultimo proyecto" es el que estaba ABIERTO AL SALIR (Ui.ProyectoAbierto),
# como hacia el editor antes de la ventana de inicio. C, D y E salieron sin
# proyecto y lo dejaron en null (medido el 23/09): se parte de una sesion que
# termino con Alfa abierto.
EscribirUi { param($ui) $ui.AlIniciar = 1; $ui.ProyectoAbierto = $alfa }
$p = Lanzar
$h = Esperar $p $esAlguna
if ($h -eq [IntPtr]::Zero) { Mal "no aparecio ninguna ventana" }
elseif ((TituloDe $p $h) -eq $tituloInicio) { Mal "aparecio la ventana de inicio" }
else {
    Start-Sleep -Milliseconds 1000
    $t = TituloDe $p $h
    if ($t.Contains("[Alfa]") -and $t.Contains("uno.asm")) { Bien "sin ventana de inicio; Alfa con su sesion (uno.asm)" } else { Mal "titulo: '$t'" }
    [void](CerrarIDE $p $h)
}
CerrarEditorAislado

# ---------------------------------------------------------------------------
Titulo "G. Al iniciar = entorno vacio"

EscribirUi { param($ui) $ui.AlIniciar = 2 }
$p = Lanzar
$h = Esperar $p $esAlguna
if ($h -eq [IntPtr]::Zero) { Mal "no aparecio ninguna ventana" }
elseif ((TituloDe $p $h) -eq $tituloInicio) { Mal "aparecio la ventana de inicio" }
else {
    Start-Sleep -Milliseconds 1000
    $t = TituloDe $p $h
    if ($t -eq $finTituloIDE) { Bien "sin ventana de inicio; IDE vacio" } else { Mal "titulo: '$t'" }
    [void](CerrarIDE $p $h)
}
CerrarEditorAislado

# ---------------------------------------------------------------------------
Titulo "H. Archivo por linea de comandos"

EscribirUi { param($ui) $ui.AlIniciar = 0 }
$p = Lanzar $suelto
$h = Esperar $p $esAlguna
if ($h -eq [IntPtr]::Zero) { Mal "no aparecio ninguna ventana" }
elseif ((TituloDe $p $h) -eq $tituloInicio) { Mal "aparecio la ventana de inicio" }
else {
    $t = TituloDe $p $h
    if ($t.Contains("suelto.asm")) { Bien "sin ventana de inicio; abrio suelto.asm" } else { Mal "titulo: '$t'" }
    [void](CerrarIDE $p $h)
}

} finally {
    CerrarEditorAislado
}

Write-Host ""
if ($fallas -eq 0) { Write-Host "TODO BIEN" -ForegroundColor Green } else { Write-Host "$fallas FALLA(S)" -ForegroundColor Red }
