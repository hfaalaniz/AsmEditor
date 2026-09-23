# ============================================================================
# Etapa 0.2 (PLAN_IDE.md): la barra de titulo propia, medida contra Windows.
#
# Arranca el prototipo y verifica, con el raton y el teclado de verdad:
#
#   1. No queda la franja de titulo nativa (el cliente empieza arriba de todo)
#   2. Arrastrar desde el titulo mueve la ventana
#   3. El borde derecho (nativo) y el superior (nuestro) redimensionan
#   4. Doble clic en el titulo maximiza SIN tapar la barra de tareas, y vuelve
#   5. Los botones maximizar y minimizar
#   6. Win+Izquierda (Aero Snap) la acomoda a la mitad de la pantalla
#   7. Llevarla al otro monitor y maximizarla ahi
#   8. Alt+Espacio abre el menu de sistema
#   9. El boton cerrar la cierra
#
# Necesita el escritorio libre: mueve el raton y manda teclas.
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
    [DllImport("user32.dll")] public static extern bool GetClientRect(IntPtr h, out RECT r);
    [DllImport("user32.dll")] public static extern bool ClientToScreen(IntPtr h, ref POINT p);
    [DllImport("user32.dll")] public static extern bool IsZoomed(IntPtr h);
    [DllImport("user32.dll")] public static extern bool IsIconic(IntPtr h);
    [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr h);
    [DllImport("user32.dll")] public static extern IntPtr GetForegroundWindow();
    [DllImport("user32.dll")] public static extern IntPtr WindowFromPoint(POINT p);
    [DllImport("user32.dll")] public static extern IntPtr GetAncestor(IntPtr h, uint f);
    [DllImport("user32.dll", CharSet=CharSet.Unicode, EntryPoint="GetWindowTextW")]
    public static extern int GetWindowText(IntPtr h, StringBuilder s, int n);

    // La ventana de primer nivel que esta VISIBLE en ese punto de la pantalla.
    public static IntPtr RaizEn(int x, int y) {
        POINT p; p.X = x; p.Y = y;
        return GetAncestor(WindowFromPoint(p), 2);    // GA_ROOT
    }
    public static string TituloDe(IntPtr h) {
        var sb = new StringBuilder(256); GetWindowText(h, sb, 256); return sb.ToString();
    }
    [DllImport("user32.dll")] public static extern bool ShowWindow(IntPtr h, int cmd);
    [DllImport("user32.dll")] public static extern bool IsWindowVisible(IntPtr h);
    [DllImport("user32.dll")] public static extern IntPtr GetWindow(IntPtr h, uint c);
    [DllImport("user32.dll")] public static extern IntPtr FindWindow(string cls, string title);
    [DllImport("user32.dll", CharSet=CharSet.Unicode, EntryPoint="GetClassNameW")]
    public static extern int GetClassName(IntPtr h, StringBuilder s, int n);
    [DllImport("user32.dll")] public static extern bool SetCursorPos(int x, int y);
    [DllImport("user32.dll")] public static extern void mouse_event(uint f, uint x, uint y, uint d, IntPtr e);
    [DllImport("user32.dll")] public static extern void keybd_event(byte vk, byte scan, uint f, IntPtr e);

    // El area de cliente en coordenadas de pantalla: es la parte VISIBLE de
    // la ventana (el rect de ventana incluye los bordes invisibles).
    public static RECT Cliente(IntPtr h) {
        RECT c; GetClientRect(h, out c);
        POINT p = new POINT(); ClientToScreen(h, ref p);
        RECT r; r.Left = p.X; r.Top = p.Y; r.Right = p.X + c.Right; r.Bottom = p.Y + c.Bottom;
        return r;
    }

    // Los Button (hijos a cualquier profundidad), para ubicar los de la barra.
    public static List<RECT> Botones(IntPtr raiz) {
        var l = new List<RECT>(); Recorrer(raiz, l, 0); return l;
    }
    static void Recorrer(IntPtr h, List<RECT> acc, int nivel) {
        if (nivel > 6) return;
        IntPtr hijo = GetWindow(h, 5);
        while (hijo != IntPtr.Zero) {
            if (IsWindowVisible(hijo)) {
                var sb = new StringBuilder(128); GetClassName(hijo, sb, 128);
                if (sb.ToString().ToUpper().Contains("BUTTON")) { RECT r; GetWindowRect(hijo, out r); acc.Add(r); }
            }
            Recorrer(hijo, acc, nivel + 1);
            hijo = GetWindow(hijo, 2);
        }
    }

    public static void Clic(int x, int y) {
        SetCursorPos(x, y); System.Threading.Thread.Sleep(150);
        mouse_event(0x02, 0, 0, 0, IntPtr.Zero); System.Threading.Thread.Sleep(50);
        mouse_event(0x04, 0, 0, 0, IntPtr.Zero); System.Threading.Thread.Sleep(400);
    }
    public static void DobleClic(int x, int y) {
        SetCursorPos(x, y); System.Threading.Thread.Sleep(150);
        for (int i = 0; i < 2; i++) {
            mouse_event(0x02, 0, 0, 0, IntPtr.Zero); System.Threading.Thread.Sleep(40);
            mouse_event(0x04, 0, 0, 0, IntPtr.Zero); System.Threading.Thread.Sleep(60);
        }
        System.Threading.Thread.Sleep(700);
    }
    public static void Arrastrar(int x1, int y1, int x2, int y2) {
        SetCursorPos(x1, y1); System.Threading.Thread.Sleep(200);
        mouse_event(0x02, 0, 0, 0, IntPtr.Zero); System.Threading.Thread.Sleep(150);
        for (int i = 1; i <= 15; i++) {
            SetCursorPos(x1 + (x2 - x1) * i / 15, y1 + (y2 - y1) * i / 15);
            System.Threading.Thread.Sleep(30);
        }
        System.Threading.Thread.Sleep(150);
        mouse_event(0x04, 0, 0, 0, IntPtr.Zero); System.Threading.Thread.Sleep(500);
    }
    public static void WinMas(byte vk) {
        keybd_event(0x5B, 0, 0, IntPtr.Zero);                 // Win abajo
        keybd_event(vk, 0, 0x1, IntPtr.Zero);                 // flecha (extendida)
        keybd_event(vk, 0, 0x1 | 0x2, IntPtr.Zero);
        keybd_event(0x5B, 0, 0x2, IntPtr.Zero);               // Win arriba
        System.Threading.Thread.Sleep(900);
    }
}
"@

$exe = "$PSScriptRoot\bin\Debug\net8.0-windows\PruebaBarraTitulo.exe"
if (-not (Test-Path $exe)) { throw "No existe $exe. Compila primero." }

$fallas = 0
function Bien($t) { Write-Host "  OK   $t" -ForegroundColor Green }
function Mal($t)  { Write-Host "  MAL  $t" -ForegroundColor Red; $script:fallas++ }
function Casi($a, $b, $tol = 3) { [math]::Abs($a - $b) -le $tol }
function Texto($r) { "x=$($r.Left) y=$($r.Top) $($r.Right - $r.Left)x$($r.Bottom - $r.Top)" }

function Capturar($archivo) {
    $r = [W]::Cliente($script:h)
    $an = $r.Right - $r.Left; $al = $r.Bottom - $r.Top
    if ($an -le 0 -or $al -le 0) { return }
    $bmp = New-Object System.Drawing.Bitmap $an, $al
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.CopyFromScreen($r.Left, $r.Top, 0, 0, $bmp.Size)
    $bmp.Save("$PSScriptRoot\$archivo", [System.Drawing.Imaging.ImageFormat]::Png)
    $g.Dispose(); $bmp.Dispose()
}

# Un punto de la barra que es TITULO (no menu ni botones): el centro del
# texto del titulo, a la mitad del alto de la barra (32 px).
function PuntoTitulo { $c = [W]::Cliente($script:h); @(([int](($c.Left + $c.Right) / 2)), ($c.Top + 16)) }

function Pantalla { [System.Windows.Forms.Screen]::FromHandle($script:h) }

# ⚠ Win+flechas y Alt+Espacio actuan sobre la ventana EN PRIMER PLANO. Si no es
# el prototipo (el 23/09 Paint le gano el primer plano a otra prueba), le
# acomodarian la ventana o le abririan el menu a otro programa de Fabian. Se
# comprueba antes de mandarlas; si no esta al frente, no se mandan.
function AlFrente {
    [void][W]::SetForegroundWindow($script:h); Start-Sleep -Milliseconds 400
    return ([W]::GetForegroundWindow() -eq $script:h)
}

# ⚠ NINGUN CLIC NI ARRASTRE SIN VERIFICAR QUE EL PUNTO ES DEL PROTOTIPO. El
# 23/09 el prototipo arranco TAPADO por Chrome en el monitor 2 y los clics,
# arrastres y dobles clics de los pasos 2 a 5 le cayeron a Chrome. Antes de
# tocar se mira que ventana esta visible en ese punto; si no es el prototipo,
# no se toca nada y se informa cual la tapa.
function EsNuestro($x, $y) {
    $raiz = [W]::RaizEn($x, $y)
    if ($raiz -eq $script:h) { return $true }
    Mal "el punto ($x,$y) esta tapado por '$([W]::TituloDe($raiz))': NO se toca"
    return $false
}
# Reciben los numeros como un arreglo: Tocar @(x, y). Adentro se usa .Invoke
# ⚠ Cada elemento con cuenta VA ENTRE PARENTESIS: en @( ) la coma tiene mas
# prioridad que la suma, y @($x + 3, $y) se lee como $x + (3, $y).
# para que ningun reemplazo de texto las confunda con las llamadas directas.
function Tocar($a)          { if (EsNuestro $a[0] $a[1]) { [W]::Clic.Invoke($a[0], $a[1]) } }
function TocarDoble($a)     { if (EsNuestro $a[0] $a[1]) { [W]::DobleClic.Invoke($a[0], $a[1]) } }
function ArrastrarSeguro($a) { if (EsNuestro $a[0] $a[1]) { [W]::Arrastrar.Invoke($a[0], $a[1], $a[2], $a[3]) } }

$p = Start-Process $exe -PassThru
$reloj = [Diagnostics.Stopwatch]::StartNew()
while ($p.MainWindowHandle -eq [IntPtr]::Zero -and $reloj.Elapsed.TotalSeconds -lt 15) { Start-Sleep -Milliseconds 200; $p.Refresh() }
if ($p.MainWindowHandle -eq [IntPtr]::Zero) { throw "No aparecio la ventana del prototipo" }
$h = $p.MainWindowHandle
Start-Sleep -Milliseconds 800

# Al frente ANTES de empezar. Si Windows no lo deja (otra ventana tiene el
# primer plano), minimizar y restaurar lo trae: fue lo que lo destrabo el
# 23/09. Si ni asi, se corta sin tocar nada.
if (-not (AlFrente)) {
    [void][W]::ShowWindow($h, 6); Start-Sleep -Milliseconds 500
    [void][W]::ShowWindow($h, 9); Start-Sleep -Milliseconds 700
}
if (-not (AlFrente)) {
    $p.Kill()
    throw "El prototipo no pudo pasar al primer plano: la prueba se corta sin tocar nada."
}

try {

# ---------------------------------------------------------------------------
Write-Host ""; Write-Host "=== 1. Sin franja de titulo nativa ===" -ForegroundColor Cyan
$v = New-Object W+RECT; [void][W]::GetWindowRect($h, [ref]$v)
$c = [W]::Cliente($h)
Write-Host "  ventana: $(Texto $v)   cliente: $(Texto $c)"
# Con marco nativo, el cliente queda ~30 px debajo del borde de la ventana.
# Sin la franja, el cliente arranca en el borde superior de la ventana.
if ($c.Top -eq $v.Top) { Bien "el cliente arranca en el borde superior (no hay titulo nativo)" }
else { Mal "el cliente arranca $($c.Top - $v.Top) px debajo del borde: quedo la franja nativa" }
Capturar "barra_inicial.png"

# ---------------------------------------------------------------------------
Write-Host ""; Write-Host "=== 2. Arrastrar desde el titulo ===" -ForegroundColor Cyan
$antes = [W]::Cliente($h); $t = PuntoTitulo
ArrastrarSeguro @($t[0], $t[1], ($t[0] + 120), ($t[1] + 60))
$despues = [W]::Cliente($h)
if ((Casi ($despues.Left - $antes.Left) 120) -and (Casi ($despues.Top - $antes.Top) 60)) { Bien "se movio 120,60" }
else { Mal "se movio $($despues.Left - $antes.Left),$($despues.Top - $antes.Top); se esperaba 120,60" }

# ---------------------------------------------------------------------------
Write-Host ""; Write-Host "=== 3. Bordes: derecho (nativo) y superior (nuestro) ===" -ForegroundColor Cyan
$antes = [W]::Cliente($h)
$medio = [int](($antes.Top + $antes.Bottom) / 2)
# El borde nativo es INVISIBLE y queda por fuera del area visible.
ArrastrarSeguro @(($antes.Right + 3), $medio, ($antes.Right + 103), $medio)
$despues = [W]::Cliente($h)
if (Casi (($despues.Right - $despues.Left) - ($antes.Right - $antes.Left)) 100) { Bien "el borde derecho agrando 100 px" }
else { Mal "el borde derecho cambio el ancho en $(($despues.Right - $despues.Left) - ($antes.Right - $antes.Left)) px" }

$antes = [W]::Cliente($h); $cx = [int](($antes.Left + $antes.Right) / 2)
ArrastrarSeguro @($cx, ($antes.Top + 2), $cx, ($antes.Top - 38))
$despues = [W]::Cliente($h)
if ((Casi ($antes.Top - $despues.Top) 40) -and (Casi ($antes.Bottom - $despues.Bottom) 0)) { Bien "el borde superior subio 40 px y el de abajo quedo fijo" }
else { Mal "borde superior: arriba cambio $($antes.Top - $despues.Top), abajo $($antes.Bottom - $despues.Bottom)" }

# ---------------------------------------------------------------------------
Write-Host ""; Write-Host "=== 4. Doble clic: maximizar sin tapar la barra de tareas ===" -ForegroundColor Cyan
$normal = [W]::Cliente($h); $t = PuntoTitulo
TocarDoble @($t[0], $t[1])
$c = [W]::Cliente($h); $wa = (Pantalla).WorkingArea
Write-Host "  cliente: $(Texto $c)   area de trabajo: x=$($wa.X) y=$($wa.Y) $($wa.Width)x$($wa.Height)"
if ([W]::IsZoomed($h)) { Bien "quedo maximizada" } else { Mal "no se maximizo" }
if ($c.Left -eq $wa.X -and $c.Top -eq $wa.Y -and ($c.Right - $c.Left) -eq $wa.Width -and ($c.Bottom - $c.Top) -eq $wa.Height) {
    Bien "ocupa exactamente el area de trabajo (la barra de tareas queda visible)"
} else { Mal "no coincide con el area de trabajo" }
Capturar "barra_maximizada.png"

$t = PuntoTitulo
TocarDoble @($t[0], $t[1])
$c = [W]::Cliente($h)
if (-not [W]::IsZoomed($h) -and $c.Left -eq $normal.Left -and $c.Top -eq $normal.Top -and ($c.Right - $c.Left) -eq ($normal.Right - $normal.Left)) {
    Bien "el segundo doble clic la devolvio a su lugar y tamano"
} else { Mal "tras el segundo doble clic: $(Texto $c), antes $(Texto $normal)" }

# ---------------------------------------------------------------------------
Write-Host ""; Write-Host "=== 5. Botones maximizar y minimizar ===" -ForegroundColor Cyan
$b = @([W]::Botones($h) | Sort-Object { $_.Left })
if ($b.Count -lt 3) { Mal "se esperaban 3 botones en la barra, hay $($b.Count)" }
else {
    $bmax = $b[$b.Count - 2]
    Tocar @([int](($bmax.Left + $bmax.Right) / 2), [int](($bmax.Top + $bmax.Bottom) / 2))
    if ([W]::IsZoomed($h)) { Bien "el boton maximizar maximizo" } else { Mal "el boton maximizar no hizo nada" }

    $b = @([W]::Botones($h) | Sort-Object { $_.Left }); $bmax = $b[$b.Count - 2]
    Tocar @([int](($bmax.Left + $bmax.Right) / 2), [int](($bmax.Top + $bmax.Bottom) / 2))
    if (-not [W]::IsZoomed($h)) { Bien "el mismo boton la restauro" } else { Mal "el boton no la restauro" }

    $b = @([W]::Botones($h) | Sort-Object { $_.Left }); $bmin = $b[$b.Count - 3]
    Tocar @([int](($bmin.Left + $bmin.Right) / 2), [int](($bmin.Top + $bmin.Bottom) / 2))
    if ([W]::IsIconic($h)) { Bien "el boton minimizar la minimizo" } else { Mal "el boton minimizar no hizo nada" }
    [void][W]::ShowWindow($h, 9); Start-Sleep -Milliseconds 700
    [void][W]::SetForegroundWindow($h); Start-Sleep -Milliseconds 400
}

# ---------------------------------------------------------------------------
Write-Host ""; Write-Host "=== 6. Win+Izquierda (Aero Snap) ===" -ForegroundColor Cyan
if (-not (AlFrente)) { Mal "el prototipo no esta en primer plano: Win+Izquierda NO se manda" }
else {
[W]::WinMas(0x25)
$c = [W]::Cliente($h); $wa = (Pantalla).WorkingArea
Write-Host "  cliente: $(Texto $c)   mitad izquierda: x=$($wa.X) ancho=$([int]($wa.Width / 2))"
if ((Casi $c.Left $wa.X 10) -and (Casi ($c.Right - $c.Left) ($wa.Width / 2) 20) -and (Casi ($c.Bottom - $c.Top) $wa.Height 10)) {
    Bien "quedo acomodada en la mitad izquierda"
} else { Mal "Snap no la acomodo a la mitad izquierda" }
Capturar "barra_snap.png"
[void][W]::ShowWindow($h, 9); Start-Sleep -Milliseconds 700
[void][W]::SetForegroundWindow($h); Start-Sleep -Milliseconds 400
}

# ---------------------------------------------------------------------------
Write-Host ""; Write-Host "=== 7. Otro monitor ===" -ForegroundColor Cyan
$pantallas = [System.Windows.Forms.Screen]::AllScreens
if ($pantallas.Count -lt 2) { Write-Host "  (hay un solo monitor: se saltea)" }
else {
    $origen = Pantalla
    $destino = $pantallas | Where-Object { $_.DeviceName -ne $origen.DeviceName } | Select-Object -First 1
    $t = PuntoTitulo
    $dx = [int]($destino.WorkingArea.X + $destino.WorkingArea.Width / 2)
    $dy = [int]($destino.WorkingArea.Y + 120)
    ArrastrarSeguro @($t[0], $t[1], $dx, $dy)
    $ahora = Pantalla
    if ($ahora.DeviceName -eq $destino.DeviceName) { Bien "paso de $($origen.DeviceName) a $($destino.DeviceName)" }
    else { Mal "sigue en $($ahora.DeviceName)" }

    $t = PuntoTitulo
    TocarDoble @($t[0], $t[1])
    $c = [W]::Cliente($h); $wa = $destino.WorkingArea
    Write-Host "  cliente: $(Texto $c)   area de trabajo: x=$($wa.X) y=$($wa.Y) $($wa.Width)x$($wa.Height)"
    if ([W]::IsZoomed($h) -and $c.Left -eq $wa.X -and $c.Top -eq $wa.Y -and ($c.Right - $c.Left) -eq $wa.Width -and ($c.Bottom - $c.Top) -eq $wa.Height) {
        Bien "maximizada en el otro monitor ocupa exactamente su area de trabajo"
    } else { Mal "maximizada en el otro monitor no coincide con su area de trabajo" }
    Capturar "barra_otro_monitor.png"

    $t = PuntoTitulo
    TocarDoble @($t[0], $t[1])
}

# ---------------------------------------------------------------------------
Write-Host ""; Write-Host "=== 8. Alt+Espacio: menu de sistema ===" -ForegroundColor Cyan
if (-not (AlFrente)) { Mal "el prototipo no esta en primer plano: Alt+Espacio NO se manda" }
else {
    [System.Windows.Forms.SendKeys]::SendWait("% "); Start-Sleep -Milliseconds 700
    $menu = [W]::FindWindow("#32768", $null)
    if ($menu -ne [IntPtr]::Zero -and [W]::IsWindowVisible($menu)) { Bien "se abrio el menu de sistema" }
    else { Mal "Alt+Espacio no abrio el menu de sistema" }
    [System.Windows.Forms.SendKeys]::SendWait("{ESC}"); Start-Sleep -Milliseconds 400
}

# ---------------------------------------------------------------------------
Write-Host ""; Write-Host "=== 9. Boton cerrar ===" -ForegroundColor Cyan
[void][W]::SetForegroundWindow($h); Start-Sleep -Milliseconds 400
$b = @([W]::Botones($h) | Sort-Object { $_.Left }); $bcer = $b[$b.Count - 1]
Tocar @([int](($bcer.Left + $bcer.Right) / 2), [int](($bcer.Top + $bcer.Bottom) / 2))
Start-Sleep -Milliseconds 800
$p.Refresh()
if ($p.HasExited) { Bien "el boton cerrar cerro la ventana" } else { Mal "el boton cerrar no la cerro" }

}
finally {
    $p.Refresh()
    if (-not $p.HasExited) { $p.Kill() }
}

Write-Host ""
if ($fallas -eq 0) { Write-Host "=== TODO BIEN: la barra de titulo propia se comporta como la nativa ===" -ForegroundColor Green; exit 0 }
Write-Host "=== $fallas FALLAS ===" -ForegroundColor Red
exit 1
