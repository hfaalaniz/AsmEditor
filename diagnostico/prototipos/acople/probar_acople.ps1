# ============================================================================
# Etapa 0.4 (PLAN_IDE.md): el acople con el RATON DE VERDAD.
#
#   1. Arrastrar el titulo de "Salida" y soltarlo en el medio (no sobre una
#      guia): queda en una ventana flotante y la zona de abajo se libera.
#   2. Arrastrar la flotante: aparecen las guias; sobre la guia de abajo
#      aparece la vista previa; al soltar, "Salida" vuelve abajo y la flotante
#      se cierra.
#   3. Lo mismo con "Lista de errores": afuera y de vuelta a la izquierda por
#      su guia, en un solo arrastre.
#
# ⚠ Protecciones (el 23/09 una prueba le hizo clic a Chrome): no arranca si no
# logra traer el prototipo al frente; solo APRIETA y SUELTA sobre ventanas del
# prototipo; no manda teclas. Durante el arrastre el cursor pasa por encima de
# otras ventanas, sin hacer clic.
#
# Es para que lo lea yo, no forma parte de la interfaz.
# ============================================================================

$ErrorActionPreference = "Stop"
Add-Type -AssemblyName System.Windows.Forms, System.Drawing

Add-Type @"
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
public class W {
    public struct RECT { public int Left, Top, Right, Bottom; }
    public struct POINT { public int X, Y; }
    [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr h, out RECT r);
    [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr h);
    [DllImport("user32.dll")] public static extern IntPtr GetForegroundWindow();
    [DllImport("user32.dll")] public static extern bool ShowWindow(IntPtr h, int cmd);
    [DllImport("user32.dll")] public static extern bool IsWindowVisible(IntPtr h);
    [DllImport("user32.dll")] public static extern IntPtr GetWindow(IntPtr h, uint c);
    [DllImport("user32.dll")] public static extern IntPtr WindowFromPoint(POINT p);
    [DllImport("user32.dll")] public static extern IntPtr GetAncestor(IntPtr h, uint f);
    [DllImport("user32.dll")] public static extern uint GetWindowThreadProcessId(IntPtr h, out uint pid);
    [DllImport("user32.dll", CharSet=CharSet.Unicode, EntryPoint="GetWindowTextW")] public static extern int GetWindowText(IntPtr h, StringBuilder s, int n);
    [DllImport("user32.dll", CharSet=CharSet.Unicode, EntryPoint="GetClassNameW")] public static extern int GetClassName(IntPtr h, StringBuilder s, int n);
    [DllImport("user32.dll")] public static extern bool SetCursorPos(int x, int y);
    [DllImport("user32.dll")] public static extern void mouse_event(uint f, uint x, uint y, uint d, IntPtr e);
    public delegate bool EnumProc(IntPtr h, IntPtr p);
    [DllImport("user32.dll")] public static extern bool EnumWindows(EnumProc cb, IntPtr p);

    public static string Titulo(IntPtr h) { var sb = new StringBuilder(256); GetWindowText(h, sb, 256); return sb.ToString(); }
    public static string Clase(IntPtr h) { var sb = new StringBuilder(256); GetClassName(h, sb, 256); return sb.ToString(); }

    public static uint ProcesoEn(int x, int y) {
        POINT p; p.X = x; p.Y = y; IntPtr r = GetAncestor(WindowFromPoint(p), 2);
        uint pid; GetWindowThreadProcessId(r, out pid); return pid;
    }
    public static string TituloEn(int x, int y) { POINT p; p.X = x; p.Y = y; return Titulo(GetAncestor(WindowFromPoint(p), 2)); }

    // Ventanas de primer nivel visibles del proceso: titulo -> rect.
    public static List<KeyValuePair<string, RECT>> Ventanas(uint pid) {
        var l = new List<KeyValuePair<string, RECT>>();
        EnumWindows(delegate(IntPtr h, IntPtr p) {
            uint suyo; GetWindowThreadProcessId(h, out suyo);
            if (suyo == pid && IsWindowVisible(h)) { RECT r; GetWindowRect(h, out r); l.Add(new KeyValuePair<string, RECT>(Titulo(h), r)); }
            return true;
        }, IntPtr.Zero);
        return l;
    }

    // La etiqueta (STATIC) visible con ese texto, a cualquier profundidad.
    public static RECT Etiqueta(IntPtr raiz, string texto) { RECT r = new RECT(); Buscar(raiz, texto, ref r, 0); return r; }
    static bool Buscar(IntPtr h, string texto, ref RECT hallado, int nivel) {
        if (nivel > 12) return false;
        IntPtr c = GetWindow(h, 5);
        while (c != IntPtr.Zero) {
            if (IsWindowVisible(c) && Clase(c).ToUpper().Contains("STATIC") && Titulo(c) == texto) { GetWindowRect(c, out hallado); return true; }
            if (Buscar(c, texto, ref hallado, nivel + 1)) return true;
            c = GetWindow(c, 2);
        }
        return false;
    }

    public static void Apretar(int x, int y) { SetCursorPos(x, y); System.Threading.Thread.Sleep(200); mouse_event(0x02, 0, 0, 0, IntPtr.Zero); System.Threading.Thread.Sleep(150); }
    public static void Soltar() { System.Threading.Thread.Sleep(200); mouse_event(0x04, 0, 0, 0, IntPtr.Zero); System.Threading.Thread.Sleep(700); }
    public static void MoverA(int x1, int y1, int x2, int y2, int pasos) {
        for (int i = 1; i <= pasos; i++) { SetCursorPos(x1 + (x2 - x1) * i / pasos, y1 + (y2 - y1) * i / pasos); System.Threading.Thread.Sleep(25); }
    }
}
"@

$exe = "$PSScriptRoot\bin\Debug\net8.0-windows\PruebaAcople.exe"
if (-not (Test-Path $exe)) { throw "No existe $exe. Compila primero." }

$fallas = 0
function Bien($t) { Write-Host "  OK   $t" -ForegroundColor Green }
function Mal($t)  { Write-Host "  MAL  $t" -ForegroundColor Red; $script:fallas++ }

function EsDelPrototipo($x, $y) {
    if ([W]::ProcesoEn($x, $y) -eq [uint32]$script:p.Id) { return $true }
    Mal "el punto ($x,$y) esta tapado por '$([W]::TituloEn($x, $y))': NO se aprieta ni se suelta"
    return $false
}

function Centro($r) { @(([int](($r.Left + $r.Right) / 2)), ([int](($r.Top + $r.Bottom) / 2))) }
function Ventanas { [W]::Ventanas([uint32]$script:p.Id) }
function VentanaLlamada($t) { @(Ventanas | Where-Object { $_.Key -eq $t }) }

function CapturarPantalla($archivo) {
    $s = [System.Windows.Forms.Screen]::FromHandle($script:h).Bounds
    $bmp = New-Object System.Drawing.Bitmap $s.Width, $s.Height
    $g = [System.Drawing.Graphics]::FromImage($bmp); $g.CopyFromScreen($s.X, $s.Y, 0, 0, $bmp.Size)
    $bmp.Save("$PSScriptRoot\$archivo", [System.Drawing.Imaging.ImageFormat]::Png); $g.Dispose(); $bmp.Dispose()
}

# La guia de una zona, en pantalla: misma cuenta que GeometriaAcople sobre el
# rectangulo del anfitrion (el cliente del formulario).
function Guia($zona) {
    $r = New-Object W+RECT; [void][W]::GetWindowRect($script:h, [ref]$r)
    $c = [System.Windows.Forms.Screen]::FromHandle($script:h) | Out-Null
    $cli = $script:cliente
    $lado = 36; $paso = 40
    $cx = $cli.Left + [int](($cli.Right - $cli.Left) / 2) - 18
    $cy = $cli.Top + [int](($cli.Bottom - $cli.Top) / 2) - 18
    switch ($zona) {
        "Izquierda" { @(($cx - $paso + 18), ($cy + 18)) }
        "Derecha"   { @(($cx + $paso + 18), ($cy + 18)) }
        "Abajo"     { @(($cx + 18), ($cy + $paso + 18)) }
    }
}

$p = Start-Process $exe -PassThru
Start-Sleep -Seconds 5; $p.Refresh(); $h = $p.MainWindowHandle
if ($h -eq [IntPtr]::Zero) { throw "No aparecio la ventana del prototipo" }

[void][W]::SetForegroundWindow($h); Start-Sleep -Milliseconds 400
if ([W]::GetForegroundWindow() -ne $h) { [void][W]::ShowWindow($h, 6); Start-Sleep -Milliseconds 500; [void][W]::ShowWindow($h, 9); Start-Sleep -Milliseconds 800; [void][W]::SetForegroundWindow($h); Start-Sleep -Milliseconds 400 }
if ([W]::GetForegroundWindow() -ne $h) { $p.Kill(); throw "El prototipo no pudo pasar al primer plano: se corta sin tocar nada." }

# El area de cliente = el anfitrion (ocupa todo el formulario).
Add-Type @"
using System; using System.Runtime.InteropServices;
public class C { public struct R { public int L, T, Ri, B; } public struct P { public int X, Y; }
  [DllImport("user32.dll")] static extern bool GetClientRect(IntPtr h, out R r);
  [DllImport("user32.dll")] static extern bool ClientToScreen(IntPtr h, ref P p);
  public static int[] Cliente(IntPtr h) { R r; GetClientRect(h, out r); P p = new P(); ClientToScreen(h, ref p); return new[] { p.X, p.Y, p.X + r.Ri, p.Y + r.B }; } }
"@
$cc = [C]::Cliente($h); $cliente = [pscustomobject]@{ Left = $cc[0]; Top = $cc[1]; Right = $cc[2]; Bottom = $cc[3] }

try {

# ---------------------------------------------------------------------------
Write-Host ""; Write-Host "=== 1. Sacar 'Salida' a una ventana flotante ===" -ForegroundColor Cyan
$t = [W]::Etiqueta($h, "Salida")
if ($t.Right -eq 0) { throw "No encontre la barra de titulo de 'Salida'" }
$a = Centro $t
# Destino: arriba en el medio del area de documentos, lejos de las guias.
$dx = [int](($cliente.Left + $cliente.Right) / 2); $dy = $cliente.Top + 120
if (EsDelPrototipo $a[0] $a[1]) {
    [W]::Apretar($a[0], $a[1]); [W]::MoverA($a[0], $a[1], $dx, $dy, 25); [W]::Soltar()
}
$f = VentanaLlamada "Salida"
if ($f.Count -eq 1) { Bien "hay una ventana flotante 'Salida'" } else { Mal "no aparecio la ventana flotante 'Salida'" }
if ($f.Count -eq 1 -and [math]::Abs((Centro $f[0].Value)[0] - $dx) -lt 60) { Bien "la flotante quedo donde se solto el raton" }
elseif ($f.Count -eq 1) { Mal "la flotante no quedo bajo el raton" }
if ((VentanaLlamada "Guías de acople").Count -eq 0) { Bien "al soltar lejos de una guia, las guias se ocultaron" } else { Mal "las guias quedaron visibles" }
CapturarPantalla "acople_flotante.png"

# ---------------------------------------------------------------------------
Write-Host ""; Write-Host "=== 2. Arrastrarla a la guia de abajo ===" -ForegroundColor Cyan
if ($f.Count -eq 1) {
    $r = $f[0].Value
    # Titulo nativo de la flotante: 12 px debajo del borde superior, en el medio.
    $tx = [int](($r.Left + $r.Right) / 2); $ty = $r.Top + 12
    $g = Guia "Abajo"
    if (EsDelPrototipo $tx $ty) {
        [W]::Apretar($tx, $ty); [W]::MoverA($tx, $ty, $g[0], $g[1], 30); Start-Sleep -Milliseconds 400
        $guias = (VentanaLlamada "Guías de acople").Count -eq 1
        $previa = (VentanaLlamada "Vista previa de acople").Count -eq 1
        CapturarPantalla "acople_guias.png"
        [W]::Soltar()
        if ($guias) { Bien "mientras se arrastraba se vieron las guias" } else { Mal "no aparecieron las guias" }
        if ($previa) { Bien "sobre la guia de abajo se vio la vista previa" } else { Mal "no aparecio la vista previa" }
    }
    if ((VentanaLlamada "Salida").Count -eq 0) { Bien "la flotante se cerro" } else { Mal "la flotante sigue abierta" }
    $t = [W]::Etiqueta($h, "Salida")
    if ($t.Right -gt 0 -and $t.Top -gt ($cliente.Top + ($cliente.Bottom - $cliente.Top) / 2)) { Bien "'Salida' volvio a la zona de abajo" }
    else { Mal "'Salida' no esta en la zona de abajo" }
}

# ---------------------------------------------------------------------------
Write-Host ""; Write-Host "=== 3. 'Lista de errores': afuera y de vuelta a la izquierda en un arrastre ===" -ForegroundColor Cyan
$t = [W]::Etiqueta($h, "Lista de errores")
if ($t.Right -eq 0) { Mal "no encontre la barra de titulo de 'Lista de errores'" }
else {
    $a = Centro $t; $g = Guia "Izquierda"
    if (EsDelPrototipo $a[0] $a[1]) {
        [W]::Apretar($a[0], $a[1]); [W]::MoverA($a[0], $a[1], ($a[0] + 200), ($a[1] + 150), 15)
        [W]::MoverA(($a[0] + 200), ($a[1] + 150), $g[0], $g[1], 25); Start-Sleep -Milliseconds 300
        [W]::Soltar()
    }
    $t = [W]::Etiqueta($h, "Lista de errores")
    if ((VentanaLlamada "Lista de errores").Count -eq 0 -and $t.Right -gt 0 -and $t.Left -lt ($cliente.Left + 60)) { Bien "volvio a la zona izquierda" }
    else { Mal "no volvio a la zona izquierda" }
}

}
finally {
    $p.Refresh(); if (-not $p.HasExited) { $p.Kill() }
}

Write-Host ""
if ($fallas -eq 0) { Write-Host "=== TODO BIEN: flotar y volver a acoplar con el raton ===" -ForegroundColor Green; exit 0 }
Write-Host "=== $fallas FALLAS ===" -ForegroundColor Red
exit 1
