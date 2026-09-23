# ============================================================================
# Abrir un .asmform existente en la pestana del disenador y generar su codigo.
#
# Verifica lo que la etapa 3 agrega sobre el modal:
#
#   1. Un .asmform se abre en el DISENADOR, no como texto JSON
#   2. El formulario cargado muestra sus controles
#   3. "Generar codigo" escribe el .inc y el .asm desde la pestana
#   4. Lo generado ENSAMBLA y ENLAZA de verdad
#
# ⚠ EL DIALOGO DEL EDITOR NO SE DETECTA POR TITULO: es un Form con
#   FormBorderStyle.None y sin Text (ver Dialogo.cs). Lo que si se puede medir
#   es que, siendo modal, DESHABILITA la ventana principal.
#
# Es para que lo lea yo, no forma parte de la interfaz.
# ============================================================================

$ErrorActionPreference = "Stop"

Add-Type -AssemblyName System.Windows.Forms
Add-Type -AssemblyName System.Drawing

Add-Type @"
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
public class W {
    [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr h);
    [DllImport("user32.dll")] public static extern IntPtr GetWindow(IntPtr h, uint c);
    [DllImport("user32.dll")] public static extern bool IsWindowVisible(IntPtr h);
    [DllImport("user32.dll")] public static extern bool IsWindowEnabled(IntPtr h);
    [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr h, out RECT r);
    [DllImport("user32.dll")] public static extern uint GetWindowThreadProcessId(IntPtr h, out uint pid);
    [DllImport("user32.dll", CharSet=CharSet.Unicode, EntryPoint="GetWindowTextW")]
    public static extern int GetWindowText(IntPtr h, StringBuilder s, int n);
    [DllImport("user32.dll", CharSet=CharSet.Unicode, EntryPoint="GetClassNameW")]
    public static extern int GetClassName(IntPtr h, StringBuilder s, int n);
    public delegate bool EnumProc(IntPtr h, IntPtr p);
    [DllImport("user32.dll")] public static extern bool EnumWindows(EnumProc cb, IntPtr p);
    public struct RECT { public int Left, Top, Right, Bottom; }

    public static IntPtr VentanaPrincipal(uint id) {
        IntPtr hallada = IntPtr.Zero;
        EnumWindows(delegate(IntPtr h, IntPtr p) {
            uint suyo;
            GetWindowThreadProcessId(h, out suyo);
            if (suyo == id && IsWindowVisible(h)) {
                var sb = new StringBuilder(512);
                GetWindowText(h, sb, 512);
                if (sb.ToString().Contains("Editor ASM")) { hallada = h; return false; }
            }
            return true;
        }, IntPtr.Zero);
        return hallada;
    }

    public static string Titulo(IntPtr h) {
        var sb = new StringBuilder(512);
        GetWindowText(h, sb, 512);
        return sb.ToString();
    }

    public static List<string> TextosDe(IntPtr raiz) {
        var r = new List<string>();
        Recorrer(raiz, r, 0);
        return r;
    }
    static void Recorrer(IntPtr h, List<string> acc, int nivel) {
        if (nivel > 8) return;
        IntPtr hijo = GetWindow(h, 5);
        while (hijo != IntPtr.Zero) {
            if (IsWindowVisible(hijo)) {
                var sb = new StringBuilder(1024);
                GetWindowText(hijo, sb, 1024);
                if (sb.Length > 0) acc.Add(sb.ToString());
            }
            Recorrer(hijo, acc, nivel + 1);
            hijo = GetWindow(hijo, 2);
        }
    }

    // Un Button por su texto, buscando tambien en ventanas sin titulo.
    public static IntPtr BotonPorTexto(string texto) {
        return BuscarBoton(GetDesktopWindow(), texto, 0);
    }
    [DllImport("user32.dll")] static extern IntPtr GetDesktopWindow();
    static IntPtr BuscarBoton(IntPtr h, string texto, int nivel) {
        if (nivel > 8) return IntPtr.Zero;
        IntPtr hijo = GetWindow(h, 5);
        while (hijo != IntPtr.Zero) {
            if (IsWindowVisible(hijo)) {
                var cls = new StringBuilder(256);
                GetClassName(hijo, cls, 256);
                var txt = new StringBuilder(256);
                GetWindowText(hijo, txt, 256);
                if (cls.ToString().Contains("Button") && txt.ToString() == texto) return hijo;
            }
            IntPtr dentro = BuscarBoton(hijo, texto, nivel + 1);
            if (dentro != IntPtr.Zero) return dentro;
            hijo = GetWindow(hijo, 2);
        }
        return IntPtr.Zero;
    }
}
public class M {
    [DllImport("user32.dll")] public static extern bool SetCursorPos(int x, int y);
    [DllImport("user32.dll")] public static extern void mouse_event(uint f, uint x, uint y, uint d, IntPtr e);
    public static void Clic(int x, int y) {
        SetCursorPos(x, y);
        System.Threading.Thread.Sleep(140);
        mouse_event(0x02, 0, 0, 0, IntPtr.Zero);
        System.Threading.Thread.Sleep(60);
        mouse_event(0x04, 0, 0, 0, IntPtr.Zero);
        System.Threading.Thread.Sleep(220);
    }
}
"@

$raiz   = "C:\Users\Fabian\NASM"
# Editor aislado: no usa la configuracion ni los proyectos de Fabian.
. "$PSScriptRoot\..\editor_aislado.ps1"
$exe    = PrepararEditorAislado
$banco  = "$PSScriptRoot\banco_asmform"
$fallas = 0

function Bien($t) { Write-Host "  OK   $t" -ForegroundColor Green }
function Mal($t)  { Write-Host "  MAL  $t" -ForegroundColor Red; $script:fallas++ }

if (-not (Test-Path $exe)) { throw "No existe $exe. Compila primero." }
if (Test-Path $banco) { Remove-Item $banco -Recurse -Force }
New-Item -ItemType Directory -Path $banco | Out-Null

# ---------------------------------------------------------------------------
Write-Host ""
Write-Host "=== Preparando un .asmform con controles ===" -ForegroundColor Cyan

# Se usa el generador del propio modelo: asi el archivo es exactamente el que
# escribe el editor, no una aproximacion escrita a mano.
$gen = "$raiz\AsmEditor\diagnostico\rad\generarform"

Push-Location $gen
$build = & dotnet build -v quiet --nologo 2>&1
$codigo = $LASTEXITCODE
Pop-Location

if ($codigo -ne 0) { $build | Write-Host; throw "No compilo generarform" }

& "$gen\bin\Debug\net8.0\generarform.exe" $banco | Out-Null
if ($LASTEXITCODE -ne 0) { throw "generarform fallo" }

$asmform = "$banco\Prueba.asmform"
if (-not (Test-Path $asmform)) { throw "No se genero $asmform" }

# Se borran el .inc y el .asm: la prueba es que el editor los vuelva a generar.
Remove-Item "$banco\Prueba.inc", "$banco\Prueba.asm" -Force -ErrorAction SilentlyContinue

Write-Host "  $asmform"

# ---------------------------------------------------------------------------
Write-Host ""
Write-Host "=== 1. El .asmform se abre en el disenador ===" -ForegroundColor Cyan

# Se pasa por linea de comandos: es la via mas directa y no depende de dialogos.
$p = Start-Process $exe -ArgumentList "`"$asmform`"" -PassThru
Start-Sleep -Seconds 6
$p.Refresh()
if ($p.HasExited) { throw "El editor se cerro solo" }

$hMain = [W]::VentanaPrincipal([uint32]$p.Id)
if ($hMain -eq [IntPtr]::Zero) { $p.Kill(); throw "No se encontro la ventana del editor" }

$titulo = [W]::Titulo($hMain)
Write-Host "  titulo: '$titulo'"

if ($titulo -like "*Prueba.asmform*") {
    Bien "el .asmform quedo como documento activo"
} else {
    Mal "el titulo no muestra el .asmform: '$titulo'"
}

$textos = [W]::TextosDe($hMain)

# ⚠ Si se hubiera abierto como TEXTO, no habria paleta ni arbol de controles.
if ($textos -match "^Controles$") {
    Bien "abrio en el disenador (hay paleta de controles)"
} else {
    Mal "no abrio en el disenador: no hay paleta"
}

# ---------------------------------------------------------------------------
Write-Host ""
Write-Host "=== 2. El formulario cargado muestra sus controles ===" -ForegroundColor Cyan

$estado = $textos | Where-Object { $_ -match "controles ·" } | Select-Object -First 1
Write-Host "  barra de estado: '$estado'"

if ($estado -match "^(\d+) controles") {
    $n = [int]$Matches[1]
    if ($n -eq 6) {
        Bien "cargo los 6 controles del formulario"
    } else {
        Mal "deberia haber 6 controles y hay $n"
    }
} else {
    Mal "no se pudo leer la barra de estado"
}

# ---------------------------------------------------------------------------
Write-Host ""
Write-Host "=== 3. Generar codigo desde la pestana ===" -ForegroundColor Cyan

[void][W]::SetForegroundWindow($hMain)
Start-Sleep -Milliseconds 900

# ⚠ "Generar código" ES UN ToolStripButton, Y ESOS NO SON VENTANAS: no
# aparecen al enumerar ventanas hijas ni se los puede ubicar con GetWindowRect.
# Se los ve por Accesibilidad (UI Automation), que sí los expone.
Add-Type -AssemblyName UIAutomationClient
Add-Type -AssemblyName UIAutomationTypes

$auto = [System.Windows.Automation.AutomationElement]::FromHandle($hMain)

$cond = New-Object System.Windows.Automation.PropertyCondition(
    [System.Windows.Automation.AutomationElement]::ControlTypeProperty,
    [System.Windows.Automation.ControlType]::Button)

$generar = $null
foreach ($b in $auto.FindAll([System.Windows.Automation.TreeScope]::Descendants, $cond)) {
    if ($b.Current.Name -like "*enerar c*") { $generar = $b; break }
}

if ($null -eq $generar) {
    Mal "no se encontro el boton 'Generar codigo' en la barra del disenador"
} else {
    Write-Host "  boton: '$($generar.Current.Name)'"
    $generar.GetCurrentPattern(
        [System.Windows.Automation.InvokePattern]::Pattern).Invoke()
}

Start-Sleep -Seconds 3

$inc = "$banco\Prueba.inc"
$asm = "$banco\Prueba.asm"

if (Test-Path $inc) { Bien "genero Prueba.inc" } else { Mal "no genero Prueba.inc" }
if (Test-Path $asm) { Bien "genero Prueba.asm" } else { Mal "no genero Prueba.asm" }

$r = New-Object W+RECT
[void][W]::GetWindowRect($hMain, [ref]$r)
$an = $r.Right - $r.Left; $al = $r.Bottom - $r.Top
if ($an -gt 0 -and $al -gt 0) {
    $bmp = New-Object System.Drawing.Bitmap $an, $al
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.CopyFromScreen($r.Left, $r.Top, 0, 0, $bmp.Size)
    $cap = "$PSScriptRoot\asmform_en_pestana.png"
    $bmp.Save($cap, [System.Drawing.Imaging.ImageFormat]::Png)
    $g.Dispose(); $bmp.Dispose()
    Write-Host "  captura: $cap"
}

try { $p.Kill() } catch { }
Start-Sleep -Milliseconds 700

if ($fallas -gt 0) {
    Write-Host ""
    Write-Host "=== $fallas FALLAS (no se puede seguir) ===" -ForegroundColor Red
    exit 1
}

# ---------------------------------------------------------------------------
Write-Host ""
Write-Host "=== 4. Lo generado ensambla y enlaza ===" -ForegroundColor Cyan

Push-Location $banco
& "$raiz\nasm.exe" -f win64 "Prueba.asm" -o "Prueba.obj"
$cn = $LASTEXITCODE
Pop-Location

if ($cn -ne 0) { Mal "NASM rechazo el codigo generado" }
else { Bien "ensamblo" }

Push-Location $banco
& "$raiz\GoLink.exe" /entry main "Prueba.obj" kernel32.dll user32.dll gdi32.dll | Out-Null
$cg = $LASTEXITCODE
Pop-Location

if ($cg -ne 0) { Mal "GoLink fallo" }
elseif (Test-Path "$banco\Prueba.exe") { Bien "enlazo" }
else { Mal "no quedo el .exe" }

Write-Host ""
if ($fallas -eq 0) {
    Write-Host "=== TODO BIEN: .asmform -> pestana -> generar -> ensamblar -> enlazar ===" -ForegroundColor Green
    exit 0
}

Write-Host "=== $fallas FALLAS ===" -ForegroundColor Red
exit 1
