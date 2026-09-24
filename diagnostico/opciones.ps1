# ============================================================================
# Prueba de Configuracion -> Opciones por la interfaz.
#
#   A. Se abre desde el buscador (Ctrl+Q "opciones"); el arbol recorre las
#      cuatro paginas con las flechas, en los dos sentidos; cada pagina carga
#      sus valores. Cambiar tema y "Al iniciar" y salir con Esc NO cambia nada
#      (settings.json intacto, el IDE sigue oscuro).
#   B. Aceptar guarda tema, "Al iniciar" y carpeta de trabajo; el tema se
#      aplica en el momento; lo demas (NASM, MSVC) queda como estaba; al
#      reabrir, la ventana muestra lo guardado.
#   C. Con un proyecto abierto, aceptar Opciones NO le quita el proyecto al
#      explorador (antes, «Rutas de herramientas» lo hacia).
#
# Editor AISLADO y teclas/clics PROTEGIDOS. Solo se leen textos de ventanas
# DEL EDITOR (OP.Titulo lo exige).
#
# Es para que lo lea yo, no forma parte de la interfaz.
# ============================================================================

$ErrorActionPreference = "Stop"

. "$PSScriptRoot\editor_aislado.ps1"
. "$PSScriptRoot\proteccion_interfaz.ps1"

Add-Type -ReferencedAssemblies System.Drawing @"
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Text;
public static class OP {
    public struct RECT { public int Left, Top, Right, Bottom; }
    delegate bool EnumProc(IntPtr h, IntPtr p);
    [DllImport("user32.dll")] static extern bool EnumWindows(EnumProc cb, IntPtr p);
    [DllImport("user32.dll")] static extern bool EnumChildWindows(IntPtr h, EnumProc cb, IntPtr p);
    [DllImport("user32.dll")] static extern uint GetWindowThreadProcessId(IntPtr h, out uint pid);
    [DllImport("user32.dll")] public static extern bool IsWindowVisible(IntPtr h);
    [DllImport("user32.dll")] static extern IntPtr GetParent(IntPtr h);
    [DllImport("user32.dll", CharSet=CharSet.Unicode)] static extern int GetWindowTextW(IntPtr h, StringBuilder s, int n);
    [DllImport("user32.dll", CharSet=CharSet.Unicode)] static extern int GetClassNameW(IntPtr h, StringBuilder s, int n);
    [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr h, out RECT r);
    [DllImport("user32.dll")] public static extern bool PostMessageW(IntPtr h, int m, IntPtr w, IntPtr l);
    [DllImport("user32.dll")] static extern bool PrintWindow(IntPtr h, IntPtr hdc, uint f);

    static uint Pid(IntPtr h) { uint p; GetWindowThreadProcessId(h, out p); return p; }

    public static List<IntPtr> Ventanas(uint pid) {
        var r = new List<IntPtr>();
        EnumWindows(delegate(IntPtr h, IntPtr p) {
            if (Pid(h) == pid && IsWindowVisible(h)) r.Add(h);
            return true;
        }, IntPtr.Zero);
        return r;
    }

    [DllImport("user32.dll", CharSet=CharSet.Unicode, EntryPoint="SendMessageTimeoutW")]
    static extern IntPtr SendMessageTimeoutTexto(IntPtr h, int m, IntPtr w, StringBuilder l, uint f, uint ms, out IntPtr r);
    [DllImport("user32.dll")] static extern IntPtr GetFocus();
    [DllImport("user32.dll")] static extern bool AttachThreadInput(uint a, uint b, bool unir);
    [DllImport("kernel32.dll")] static extern uint GetCurrentThreadId();

    // SOLO de ventanas del proceso indicado. Con WM_GETTEXT y no con
    // GetWindowText: entre procesos, GetWindowText devuelve el titulo
    // guardado y no le pregunta al control (un combo da vacio). Medido el 23/09.
    public static string Titulo(IntPtr h, uint pid) {
        if (Pid(h) != pid) return "";
        var sb = new StringBuilder(1024);
        IntPtr r;
        SendMessageTimeoutTexto(h, 0x000D, (IntPtr)1024, sb, 0x2, 1000, out r);
        return sb.ToString();
    }

    // El control con el foco en la ventana (de este proceso), para rastrear.
    public static IntPtr Foco(IntPtr ventana) {
        uint pid;
        uint hilo = GetWindowThreadProcessId(ventana, out pid);
        uint mio = GetCurrentThreadId();
        AttachThreadInput(mio, hilo, true);
        IntPtr f = GetFocus();
        AttachThreadInput(mio, hilo, false);
        return f;
    }

    public static string Clase(IntPtr h) { var sb = new StringBuilder(256); GetClassNameW(h, sb, 256); return sb.ToString(); }

    // Hijas visibles (recursivo) de una clase, con un texto dado (vacio = cualquiera).
    // ⚠ VACIO Y NO null: el $null de PowerShell llega a un string de C# como "".
    // Con "texto != null" se buscaban los controles SIN texto (medido el 23/09).
    public static List<IntPtr> Hijas(IntPtr raiz, string clase, string texto, bool soloDirectas) {
        var r = new List<IntPtr>();
        uint pid = Pid(raiz);
        EnumChildWindows(raiz, delegate(IntPtr h, IntPtr p) {
            if (!IsWindowVisible(h)) return true;
            if (soloDirectas && GetParent(h) != raiz) return true;
            if (!Clase(h).ToUpperInvariant().Contains(clase)) return true;
            if (!string.IsNullOrEmpty(texto) && Titulo(h, pid) != texto) return true;
            r.Add(h); return true;
        }, IntPtr.Zero);
        return r;
    }

    public static List<string> Textos(IntPtr raiz) {
        var r = new List<string>();
        uint pid = Pid(raiz);
        EnumChildWindows(raiz, delegate(IntPtr h, IntPtr p) {
            if (IsWindowVisible(h)) { var t = Titulo(h, pid); if (t.Length > 0) r.Add(t); }
            return true;
        }, IntPtr.Zero);
        return r;
    }

    public static int[] Centro(IntPtr h) { RECT r; GetWindowRect(h, out r); return new int[] { (r.Left + r.Right) / 2, (r.Top + r.Bottom) / 2 }; }

    // Captura de la ventana (del editor) tal como se ve, para mirarla yo.
    public static void Capturar(IntPtr h, string ruta) {
        RECT r; GetWindowRect(h, out r);
        using (var bmp = new Bitmap(r.Right - r.Left, r.Bottom - r.Top)) {
            using (var g = Graphics.FromImage(bmp)) {
                var dc = g.GetHdc(); PrintWindow(h, dc, 2); g.ReleaseHdc(dc);
            }
            bmp.Save(ruta, System.Drawing.Imaging.ImageFormat.Png);
        }
    }

    // Luminancia media (0-255) de una franja de la ventana, dibujada con
    // PrintWindow: se lee la ventana del editor aunque algo la tape, y nunca
    // pixeles de otra ventana.
    public static double Luz(IntPtr h, double fx, int y) {
        RECT r; GetWindowRect(h, out r);
        int w = r.Right - r.Left, alto = r.Bottom - r.Top;
        using (var bmp = new Bitmap(w, alto)) {
            using (var g = Graphics.FromImage(bmp)) {
                var dc = g.GetHdc(); PrintWindow(h, dc, 2); g.ReleaseHdc(dc);
            }
            int x0 = (int)(w * fx); double s = 0; int n = 0;
            for (int x = x0; x < x0 + 12 && x < w; x++)
                for (int yy = y; yy < y + 4 && yy < alto; yy++) {
                    var c = bmp.GetPixel(x, yy); s += 0.299 * c.R + 0.587 * c.G + 0.114 * c.B; n++;
                }
            return n == 0 ? -1 : s / n;
        }
    }
}
"@

$fallas = 0
function Bien($t) { Write-Host "  OK   $t" -ForegroundColor Green }
function Mal($t)  { Write-Host "  MAL  $t" -ForegroundColor Red; $script:fallas++ }
function Titulo($t) { Write-Host ""; Write-Host "=== $t ===" -ForegroundColor Cyan }

$finTituloIDE = "Editor ASM (NASM + GoLink)"
$ultimo       = "$([char]0xDA)ltimo proyecto"
# El editor aislado arranca en "Entorno vacio" (editor_aislado.ps1).
$vacio        = "Entorno vac$([char]0xED)o"

# ---------------------------------------------------------------------------
Titulo "Preparando el banco"

$exe        = PrepararEditorAislado
$cfgAislado = Join-Path (Split-Path $exe -Parent) "settings.json"
$banco      = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot "banco_opciones"))
if (Test-Path $banco) { Remove-Item $banco -Recurse -Force }
New-Item -ItemType Directory -Path $banco | Out-Null

$trabajo = Join-Path $banco "carpeta_trabajo"
New-Item -ItemType Directory -Path $trabajo | Out-Null

$dirProy = Join-Path $banco "proyecto"
New-Item -ItemType Directory -Path $dirProy | Out-Null
Set-Content (Join-Path $dirProy "principal.asm") "; principal" -Encoding ASCII
$rutaProy = Join-Path $dirProy "Banco Opciones.asmproj"
@{ Version = 1; Proyecto = @{ Nombre = "Banco Opciones"; ArchivoPrincipal = "principal.asm"; CarpetaSalida = "bin"
   Archivos = @("principal.asm"); Targets = @(); TargetActivo = 0 } } | ConvertTo-Json -Depth 6 | Set-Content $rutaProy -Encoding UTF8

function EscribirConfig([scriptblock]$cambiar) {
    $cfg = Get-Content $cfgAislado -Raw -Encoding UTF8 | ConvertFrom-Json
    & $cambiar $cfg
    [IO.File]::WriteAllText($cfgAislado, ($cfg | ConvertTo-Json -Depth 20), (New-Object System.Text.UTF8Encoding($false)))
}
function LeerConfig { return (Get-Content $cfgAislado -Raw -Encoding UTF8 | ConvertFrom-Json) }

# Tema oscuro de partida, aunque Fabian use el claro: la prueba lo pasa a claro.
EscribirConfig { param($c) $c.Ui | Add-Member -NotePropertyName Theme -NotePropertyValue 0 -Force }
$inicial = LeerConfig
Write-Host "  banco: $banco"

# ---------------------------------------------------------------------------
# Ventanas del editor
# ---------------------------------------------------------------------------

function Lanzar([string]$archivo) {
    if ($archivo) { return Start-Process $exe -ArgumentList "`"$archivo`"" -PassThru }
    return Start-Process $exe -PassThru
}

function Buscar($p, [scriptblock]$filtro) {
    foreach ($h in [OP]::Ventanas([uint32]$p.Id)) {
        if (& $filtro $h ([OP]::Titulo($h, [uint32]$p.Id))) { return $h }
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

$esIDE      = { param($h, $t) $t.EndsWith($finTituloIDE) }
$esOpciones = { param($h, $t) $t -eq "Opciones" }

function Texto($p, $h) { return [OP]::Titulo($h, [uint32]$p.Id) }

# El titulo de la pagina: la etiqueta hija DIRECTA de la ventana Opciones.
function TituloPagina($p, $op) {
    $l = @([OP]::Hijas($op, "STATIC", $null, $true))
    if ($l.Count -eq 0) { return "" }
    return (Texto $p $l[0])
}

# Los combos visibles (de la pagina a la vista), en orden de pantalla.
function Combos($p, $op) {
    $l = @([OP]::Hijas($op, "COMBOBOX", $null, $false))
    return @($l | Sort-Object { ([OP]::Centro($_))[1] } | ForEach-Object { Texto $p $_ })
}

# Rastro para leer yo: que control tiene el foco y que muestran los combos.
function Rastro($p, $op, $paso) {
    $f = [OP]::Foco($op)
    $clase = if ($f -ne [IntPtr]::Zero) { [OP]::Clase($f) -replace "^WindowsForms10\.", "" -replace "\.app.*$", "" } else { "(nada)" }
    Write-Host ("    rastro {0,-14} foco {1,-10} '{2}'  combos: {3}" -f $paso, $clase, (Texto $p $f), ((Combos $p $op) -join " | ")) -ForegroundColor DarkGray
}

function Visible($p, $op, $texto) { return (@([OP]::Hijas($op, "STATIC", $texto, $false)).Count -gt 0) }

function AbrirOpciones($p, $ide) {
    if (-not (TraerAlFrente $p $ide)) { Mal "no se pudo traer el IDE al frente"; return [IntPtr]::Zero }
    [void](TeclasProtegidas $p "^q" 500)
    [void](TeclasProtegidas $p "opciones" 800)
    [void](TeclasProtegidas $p "{ENTER}" 300)
    $op = Esperar $p $esOpciones 10
    if ($op -eq [IntPtr]::Zero) { Mal "no se abrio la ventana Opciones" }
    else { Start-Sleep -Milliseconds 400; [void](TraerAlFrente $p $op) }
    return $op
}

function EsperarCierre($p, $segundos = 8) {
    $reloj = [Diagnostics.Stopwatch]::StartNew()
    while ($reloj.Elapsed.TotalSeconds -lt $segundos) {
        if ((Buscar $p $esOpciones) -eq [IntPtr]::Zero) { return $true }
        Start-Sleep -Milliseconds 200
    }
    return $false
}

# Luminancia de la barra de titulo propia del IDE (fondo Superficie2).
function LuzIDE($ide) { return [Math]::Round([OP]::Luz($ide, 0.45, 14)) }

# Cierra el IDE como el usuario (WM_CLOSE y "Salir"): matarlo no guardaria.
function CerrarIDE($p, $ide) {
    [void](TraerAlFrente $p $ide)
    [void][OP]::PostMessageW($ide, 0x10, [IntPtr]::Zero, [IntPtr]::Zero)
    $salir = [IntPtr]::Zero
    $reloj = [Diagnostics.Stopwatch]::StartNew()
    while ($salir -eq [IntPtr]::Zero -and $reloj.Elapsed.TotalSeconds -lt 8 -and -not $p.HasExited) {
        foreach ($v in [OP]::Ventanas([uint32]$p.Id)) {
            $b = @([OP]::Hijas($v, "BUTTON", "Salir", $false))
            if ($b.Count -gt 0) { $salir = $b[0]; break }
        }
        Start-Sleep -Milliseconds 250
    }
    if ($salir -ne [IntPtr]::Zero) { $c = [OP]::Centro($salir); [void](ClicProtegido $p $c[0] $c[1]) }
    if (-not $p.WaitForExit(15000)) { Mal "el editor no se cerro con Salir: se lo mata"; CerrarEditorAislado }
}

# ===========================================================================
try {

Titulo "A. Abrir, recorrer las paginas y cancelar"

$p = Lanzar
$ide = Esperar $p $esIDE
if ($ide -eq [IntPtr]::Zero) { throw "no aparecio el IDE" }
Start-Sleep -Milliseconds 1500
$luzAntes = LuzIDE $ide
if ($luzAntes -lt 128) { Bien "el IDE arranca oscuro (luz $luzAntes)" } else { Mal "el IDE no arranca oscuro (luz $luzAntes)" }
$escrituraAntes = (Get-Item $cfgAislado).LastWriteTimeUtc

$op = AbrirOpciones $p $ide
if ($op -ne [IntPtr]::Zero) {
    Bien "Ctrl+Q 'opciones' abrio la ventana Opciones"

    $t = TituloPagina $p $op
    if ($t -eq "General") { Bien "abre en General" } else { Mal "abre en '$t'" }
    $c = Combos $p $op
    if (($c -join " | ") -eq "Oscuro | $vacio") { Bien "General cargo: $($c -join ' | ')" } else { Mal "General muestra: '$($c -join ' | ')'" }

    # Abajo: General -> Proyectos -> Herramientas (muestra NASM y GoLink) -> NASM y GoLink -> MSVC
    [OP]::Capturar($op, (Join-Path $PSScriptRoot "opciones_General.png"))
    $recorrido = @()
    foreach ($i in 1..4) {
        [void](TeclasProtegidas $p "{DOWN}" 350)
        $t = TituloPagina $p $op
        $recorrido += $t
        [OP]::Capturar($op, (Join-Path $PSScriptRoot ("opciones_" + ($t -replace " ", "_") + ".png")))
    }
    $esperado = "Proyectos | NASM y GoLink | NASM y GoLink | MSVC"
    if (($recorrido -join " | ") -eq $esperado) { Bien "flecha abajo: $($recorrido -join ' | ')" } else { Mal "flecha abajo: '$($recorrido -join ' | ')' (esperaba '$esperado')" }

    if ((Visible $p $op "Toolchain 64 bits:") -and -not (Visible $p $op "Tema:")) { Bien "se ve solo la pagina MSVC" } else { Mal "en MSVC se ven otras paginas o falta la suya" }
    $c = @(Combos $p $op)
    if ($c.Count -eq 4 -and ($c | Where-Object { $_ -eq "" }).Count -eq 0) { Bien "MSVC cargo sus 4 combos ('$($c[0])')" } else { Mal "combos de MSVC: '$($c -join ' | ')'" }

    # Arriba, hasta el principio: tiene que poder pasar de categoria.
    $recorrido = @()
    foreach ($i in 1..5) { [void](TeclasProtegidas $p "{UP}" 350); $recorrido += (TituloPagina $p $op) }
    $esperado = "NASM y GoLink | NASM y GoLink | Proyectos | General | General"
    if (($recorrido -join " | ") -eq $esperado) { Bien "flecha arriba: $($recorrido -join ' | ')" } else { Mal "flecha arriba: '$($recorrido -join ' | ')' (esperaba '$esperado')" }

    # Estamos en "Entorno" (muestra General). Tab al tema, abajo = Claro;
    # Tab a "Al iniciar", abajo = Ultimo proyecto.
    Rastro $p $op "antes"
    foreach ($tecla in @("{TAB}", "{DOWN}", "{TAB}", "{UP}")) { [void](TeclasProtegidas $p $tecla 400); Rastro $p $op $tecla }
    $c = Combos $p $op
    if (($c -join " | ") -eq "Claro | $ultimo") { Bien "cambiados en la ventana: $($c -join ' | ')" } else { Mal "tras cambiar: '$($c -join ' | ')'" }

    [void](TeclasProtegidas $p "{ESC}" 300)
    if (EsperarCierre $p) { Bien "Esc cerro Opciones" } else { Mal "Opciones sigue abierta tras Esc" }

    Start-Sleep -Milliseconds 800
    if ((Get-Item $cfgAislado).LastWriteTimeUtc -eq $escrituraAntes) { Bien "Cancelar no escribio settings.json" } else { Mal "Cancelar escribio settings.json" }
    $cfg = LeerConfig
    if ($cfg.Ui.Theme -eq 0 -and $cfg.Ui.AlIniciar -eq 2) { Bien "tema y Al iniciar siguen como estaban" } else { Mal "tras cancelar: Theme $($cfg.Ui.Theme), AlIniciar $($cfg.Ui.AlIniciar)" }
    $luz = LuzIDE $ide
    if ($luz -lt 128) { Bien "el IDE sigue oscuro (luz $luz)" } else { Mal "el IDE cambio de tema al cancelar (luz $luz)" }
}

# ---------------------------------------------------------------------------
Titulo "B. Aceptar guarda y aplica"

$op = AbrirOpciones $p $ide
if ($op -ne [IntPtr]::Zero) {
    $c = Combos $p $op
    if (($c -join " | ") -eq "Oscuro | $vacio") { Bien "reabre con lo guardado, no con lo cancelado" } else { Mal "reabre con '$($c -join ' | ')'" }

    Rastro $p $op "antes"
    foreach ($tecla in @("{TAB}", "{DOWN}", "{TAB}", "{UP}")) { [void](TeclasProtegidas $p $tecla 400); Rastro $p $op $tecla }

    # Al arbol (dos Shift+Tab), a Proyectos, a la carpeta, y se reemplaza.
    [void](TeclasProtegidas $p "+{TAB}+{TAB}" 400)
    [void](TeclasProtegidas $p "{DOWN}" 400)
    if ((TituloPagina $p $op) -eq "Proyectos") {
        [void](TeclasProtegidas $p "{TAB}{HOME}+{END}" 300)
        [void](TeclasProtegidas $p $trabajo 500)
    } else { Mal "no se llego a la pagina Proyectos" }

    [void](TeclasProtegidas $p "{ENTER}" 300)
    if (EsperarCierre $p) { Bien "Enter acepto y cerro Opciones" } else { Mal "Opciones sigue abierta tras Enter" }

    Start-Sleep -Milliseconds 1000
    $cfg = LeerConfig
    if ($cfg.Ui.Theme -eq 1) { Bien "tema guardado: claro" } else { Mal "Theme = $($cfg.Ui.Theme)" }
    if ($cfg.Ui.AlIniciar -eq 1) { Bien "Al iniciar guardado: ultimo proyecto" } else { Mal "AlIniciar = $($cfg.Ui.AlIniciar)" }
    if ($cfg.ProjectFolder -ieq $trabajo) { Bien "carpeta de trabajo guardada" } else { Mal "ProjectFolder = '$($cfg.ProjectFolder)'" }

    $iguales = $true
    foreach ($k in @("NasmPath", "GoLinkPath", "MsvcToolchainDir64", "MsvcToolchainDir32", "SdkLibPath64", "SdkLibPath32")) {
        if ($cfg.$k -ne $inicial.$k) { $iguales = $false; Mal "$k cambio sin tocarlo: '$($inicial.$k)' -> '$($cfg.$k)'" }
    }
    if ($iguales) { Bien "NASM, GoLink y MSVC quedaron como estaban" }
    if (@($cfg.Targets).Count -eq @($inicial.Targets).Count) { Bien "los targets no se tocaron" } else { Mal "cambio la cantidad de targets" }

    $luz = LuzIDE $ide
    if ($luz -ge 128) { Bien "el tema claro se aplico en el momento (luz $luz)" } else { Mal "el IDE sigue oscuro (luz $luz)" }

    $op = AbrirOpciones $p $ide
    if ($op -ne [IntPtr]::Zero) {
        $c = Combos $p $op
        if (($c -join " | ") -eq "Claro | $ultimo") { Bien "al reabrir muestra lo guardado: $($c -join ' | ')" } else { Mal "al reabrir: '$($c -join ' | ')'" }
        [void](TeclasProtegidas $p "{DOWN}" 350)
        $carpeta = @([OP]::Hijas($op, "EDIT", $null, $false))
        if ($carpeta.Count -eq 1 -and (Texto $p $carpeta[0]) -ieq $trabajo) { Bien "Proyectos muestra la carpeta guardada" } else { Mal "Proyectos no muestra la carpeta guardada" }
        [void](TeclasProtegidas $p "{ESC}" 300)
        [void](EsperarCierre $p)
    }
}
CerrarIDE $p $ide
CerrarEditorAislado

# ---------------------------------------------------------------------------
Titulo "C. Con un proyecto abierto, el explorador lo conserva"

EscribirConfig { param($c) $c.Ui.AlIniciar = 2 }
$p = Lanzar $rutaProy
$ide = Esperar $p { param($h, $t) $t.Contains("[Banco Opciones]") }
if ($ide -eq [IntPtr]::Zero) { Mal "el IDE no abrio el proyecto" }
else {
    Start-Sleep -Milliseconds 1000
    if (@([OP]::Textos($ide)) -contains "Proyecto: Banco Opciones") { Bien "el explorador muestra el proyecto" } else { Mal "el explorador no muestra el proyecto antes de Opciones" }

    $op = AbrirOpciones $p $ide
    if ($op -ne [IntPtr]::Zero) {
        [void](TeclasProtegidas $p "{TAB}{ENTER}" 300)
        if (EsperarCierre $p) { Bien "aceptado sin cambios" } else { Mal "Opciones sigue abierta" }
        Start-Sleep -Milliseconds 800
        if (@([OP]::Textos($ide)) -contains "Proyecto: Banco Opciones") { Bien "tras aceptar, el explorador sigue mostrando el proyecto" }
        else { Mal "tras aceptar, el explorador perdio el proyecto" }
    }
    CerrarIDE $p $ide
}

} finally {
    CerrarEditorAislado
}

Write-Host ""
if ($fallas -eq 0) { Write-Host "TODO BIEN" -ForegroundColor Green } else { Write-Host "$fallas FALLA(S)" -ForegroundColor Red }
