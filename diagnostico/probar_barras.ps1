# ============================================================================
# Etapa 2 (PLAN_IDE.md): las barras de titulo y de estado propias, en el
# EDITOR REAL (aislado), medidas contra Windows.
#
#   1. Sin franja de titulo nativa
#   2. Doble clic en el titulo: maximizar exacto al area de trabajo y volver
#   3. Arrastrar desde el titulo
#   4. Logo: clic abre el menu de sistema
#   5. Botones minimizar / maximizar de la barra
#   6. Barra de estado: compilar un fuente roto -> "1 error" y contador 1;
#      uno bueno -> "Sin errores" y contadores en 0
#   7. Barra de estado: clic en el target -> se elige otro
#   8. Los atajos del menu siguen andando (Ctrl+N)
#   9. Buscador de comandos: Ctrl+Q, "nuevo", Enter -> ejecuta Archivo > Nuevo
#  10. Boton cerrar
#
# ⚠ Ningun clic sin verificar que el punto es de una ventana DEL EDITOR, y
# ninguna tecla sin el editor en primer plano (el 23/09 una prueba le hizo
# clic a Chrome). Necesita el escritorio libre.
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
    [DllImport("user32.dll")] public static extern bool GetClientRect(IntPtr h, out RECT r);
    [DllImport("user32.dll")] public static extern bool ClientToScreen(IntPtr h, ref POINT p);
    [DllImport("user32.dll")] public static extern bool IsZoomed(IntPtr h);
    [DllImport("user32.dll")] public static extern bool IsIconic(IntPtr h);
    [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr h);
    [DllImport("user32.dll")] public static extern IntPtr GetForegroundWindow();
    [DllImport("user32.dll")] public static extern bool ShowWindow(IntPtr h, int cmd);
    [DllImport("user32.dll")] public static extern bool IsWindowVisible(IntPtr h);
    [DllImport("user32.dll")] public static extern IntPtr GetWindow(IntPtr h, uint c);
    [DllImport("user32.dll")] public static extern IntPtr FindWindow(string cls, string title);
    [DllImport("user32.dll")] public static extern IntPtr WindowFromPoint(POINT p);
    [DllImport("user32.dll")] public static extern IntPtr GetAncestor(IntPtr h, uint f);
    [DllImport("user32.dll")] public static extern uint GetWindowThreadProcessId(IntPtr h, out uint pid);
    [DllImport("user32.dll", CharSet=CharSet.Unicode, EntryPoint="GetWindowTextW")]
    public static extern int GetWindowText(IntPtr h, StringBuilder s, int n);
    [DllImport("user32.dll", CharSet=CharSet.Unicode, EntryPoint="GetClassNameW")]
    public static extern int GetClassName(IntPtr h, StringBuilder s, int n);
    [DllImport("user32.dll")] public static extern bool SetCursorPos(int x, int y);
    [DllImport("user32.dll")] public static extern void mouse_event(uint f, uint x, uint y, uint d, IntPtr e);
    public delegate bool EnumProc(IntPtr h, IntPtr p);
    [DllImport("user32.dll")] public static extern bool EnumWindows(EnumProc cb, IntPtr p);

    public static string Titulo(IntPtr h) { var sb = new StringBuilder(512); GetWindowText(h, sb, 512); return sb.ToString(); }
    public static string Clase(IntPtr h) { var sb = new StringBuilder(256); GetClassName(h, sb, 256); return sb.ToString(); }

    public static RECT Cliente(IntPtr h) {
        RECT c; GetClientRect(h, out c);
        POINT p = new POINT(); ClientToScreen(h, ref p);
        RECT r; r.Left = p.X; r.Top = p.Y; r.Right = p.X + c.Right; r.Bottom = p.Y + c.Bottom;
        return r;
    }

    // Proceso duenio de la ventana de primer nivel VISIBLE en ese punto.
    public static uint ProcesoEn(int x, int y) {
        POINT p; p.X = x; p.Y = y;
        IntPtr raiz = GetAncestor(WindowFromPoint(p), 2);
        uint pid; GetWindowThreadProcessId(raiz, out pid); return pid;
    }
    public static string TituloEn(int x, int y) {
        POINT p; p.X = x; p.Y = y;
        return Titulo(GetAncestor(WindowFromPoint(p), 2));
    }

    // Hijas visibles, a cualquier profundidad, con su rect.
    public static List<KeyValuePair<IntPtr, RECT>> Hijas(IntPtr raiz) {
        var l = new List<KeyValuePair<IntPtr, RECT>>(); Recorrer(raiz, l, 0); return l;
    }
    static void Recorrer(IntPtr h, List<KeyValuePair<IntPtr, RECT>> acc, int nivel) {
        if (nivel > 12) return;
        IntPtr hijo = GetWindow(h, 5);
        while (hijo != IntPtr.Zero) {
            if (IsWindowVisible(hijo)) { RECT r; GetWindowRect(hijo, out r); acc.Add(new KeyValuePair<IntPtr, RECT>(hijo, r)); }
            Recorrer(hijo, acc, nivel + 1);
            hijo = GetWindow(hijo, 2);
        }
    }

    // Ventanas de primer nivel visibles de un proceso, con su titulo.
    public static List<string> TitulosDe(uint pid) {
        var l = new List<string>();
        EnumWindows(delegate(IntPtr h, IntPtr p) {
            uint suyo; GetWindowThreadProcessId(h, out suyo);
            if (suyo == pid && IsWindowVisible(h)) l.Add(Titulo(h));
            return true;
        }, IntPtr.Zero);
        return l;
    }

    // Un Button con ese texto en cualquier ventana de primer nivel del proceso
    // (los dialogos del editor son Form aparte). Rect vacio si no esta.
    public static RECT BotonDe(uint pid, string texto) {
        RECT hallado = new RECT();
        var raices = new List<IntPtr>();
        EnumWindows(delegate(IntPtr h, IntPtr p) {
            uint suyo; GetWindowThreadProcessId(h, out suyo);
            if (suyo == pid && IsWindowVisible(h)) raices.Add(h);
            return true;
        }, IntPtr.Zero);
        foreach (var r in raices)
            foreach (var hija in Hijas(r))
                if (Clase(hija.Key).ToUpper().Contains("BUTTON") && Titulo(hija.Key) == texto) return hija.Value;
        return hallado;
    }

    public static void Clic(int x, int y) {
        SetCursorPos(x, y); System.Threading.Thread.Sleep(150);
        mouse_event(0x02, 0, 0, 0, IntPtr.Zero); System.Threading.Thread.Sleep(50);
        mouse_event(0x04, 0, 0, 0, IntPtr.Zero); System.Threading.Thread.Sleep(500);
    }
    public static void DobleClic(int x, int y) {
        SetCursorPos(x, y); System.Threading.Thread.Sleep(150);
        for (int i = 0; i < 2; i++) {
            mouse_event(0x02, 0, 0, 0, IntPtr.Zero); System.Threading.Thread.Sleep(40);
            mouse_event(0x04, 0, 0, 0, IntPtr.Zero); System.Threading.Thread.Sleep(60);
        }
        System.Threading.Thread.Sleep(900);
    }
    public static void Arrastrar(int x1, int y1, int x2, int y2) {
        SetCursorPos(x1, y1); System.Threading.Thread.Sleep(200);
        mouse_event(0x02, 0, 0, 0, IntPtr.Zero); System.Threading.Thread.Sleep(150);
        for (int i = 1; i <= 15; i++) { SetCursorPos(x1 + (x2 - x1) * i / 15, y1 + (y2 - y1) * i / 15); System.Threading.Thread.Sleep(30); }
        System.Threading.Thread.Sleep(150);
        mouse_event(0x04, 0, 0, 0, IntPtr.Zero); System.Threading.Thread.Sleep(600);
    }
}
"@

. "$PSScriptRoot\editor_aislado.ps1"
$exe = PrepararEditorAislado

$fallas = 0
function Bien($t) { Write-Host "  OK   $t" -ForegroundColor Green }
function Mal($t)  { Write-Host "  MAL  $t" -ForegroundColor Red; $script:fallas++ }
function Casi($a, $b, $tol = 3) { [math]::Abs($a - $b) -le $tol }
function Texto($r) { "x=$($r.Left) y=$($r.Top) $($r.Right - $r.Left)x$($r.Bottom - $r.Top)" }

# ⚠ Clics solo sobre ventanas DEL EDITOR (la principal, o sus popups: la lista
# del buscador, el menu de targets, el menu de sistema).
function EsDelEditor($x, $y) {
    if ([W]::ProcesoEn($x, $y) -eq [uint32]$script:p.Id) { return $true }
    $menuSistema = [W]::FindWindow("#32768", $null)
    Mal "el punto ($x,$y) esta tapado por '$([W]::TituloEn($x, $y))': NO se toca"
    return $false
}
function Tocar($a)           { if (EsDelEditor $a[0] $a[1]) { [W]::Clic.Invoke($a[0], $a[1]) } }
function TocarDoble($a)      { if (EsDelEditor $a[0] $a[1]) { [W]::DobleClic.Invoke($a[0], $a[1]) } }
function ArrastrarSeguro($a) { if (EsDelEditor $a[0] $a[1]) { [W]::Arrastrar.Invoke($a[0], $a[1], $a[2], $a[3]) } }

function AlFrente {
    [void][W]::SetForegroundWindow($script:h); Start-Sleep -Milliseconds 400
    return ([W]::GetForegroundWindow() -eq $script:h)
}
function Teclas($k) {
    if (-not (AlFrente)) { Mal "el editor no esta en primer plano: NO se manda '$k'"; return }
    [System.Windows.Forms.SendKeys]::SendWait($k); Start-Sleep -Milliseconds 700
}

function Pantalla { [System.Windows.Forms.Screen]::FromHandle($script:h) }

# Los 3 botones de la barra de titulo: Button con su borde de arriba en el borde
# superior del cliente y 32 px de alto.
function BotonesTitulo {
    $c = [W]::Cliente($script:h)
    @([W]::Hijas($script:h) | Where-Object {
        ([W]::Clase($_.Key)).ToUpper().Contains("BUTTON") -and $_.Value.Top -eq $c.Top -and ($_.Value.Bottom - $_.Value.Top) -eq 32
    } | Sort-Object { $_.Value.Left } | ForEach-Object { $_.Value })
}

# Un punto de TITULO: entre la insignia y los botones (la barra mide 32 px).
function PuntoTitulo {
    $b = BotonesTitulo; $c = [W]::Cliente($script:h)
    @(($b[0].Left - 60), ($c.Top + 16))
}

# Las etiquetas de la barra de estado (clase STATIC en la ultima franja de 24 px).
function EtiquetasEstado {
    $c = [W]::Cliente($script:h)
    @([W]::Hijas($script:h) | Where-Object {
        ([W]::Clase($_.Key)).ToUpper().Contains("STATIC") -and $_.Value.Top -ge ($c.Bottom - 24) -and $_.Value.Bottom -le $c.Bottom
    } | Sort-Object { $_.Value.Left } | ForEach-Object { [pscustomobject]@{ Texto = [W]::Titulo($_.Key); R = $_.Value } })
}
function TextoEstado { $e = EtiquetasEstado; if ($e.Count -ge 2) { $e[1].Texto } else { "" } }

# La pantalla entera donde esta el editor: para ver menus desplegables, que son
# ventanas aparte y pueden caer fuera del area de cliente.
function CapturarPantalla($archivo) {
    $s = (Pantalla).Bounds
    $bmp = New-Object System.Drawing.Bitmap $s.Width, $s.Height
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.CopyFromScreen($s.X, $s.Y, 0, 0, $bmp.Size)
    $bmp.Save("$PSScriptRoot\$archivo", [System.Drawing.Imaging.ImageFormat]::Png); $g.Dispose(); $bmp.Dispose()
}

function Capturar($archivo) {
    $r = [W]::Cliente($script:h); $an = $r.Right - $r.Left; $al = $r.Bottom - $r.Top
    if ($an -le 0 -or $al -le 0) { return }
    $bmp = New-Object System.Drawing.Bitmap $an, $al
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.CopyFromScreen($r.Left, $r.Top, 0, 0, $bmp.Size)
    $bmp.Save("$PSScriptRoot\$archivo", [System.Drawing.Imaging.ImageFormat]::Png); $g.Dispose(); $bmp.Dispose()
}

# ---- Fuentes: uno bueno (copia de Win6Win) y uno roto -----------------------
$bueno = CopiarFuenteABanco "C:\Users\Fabian\NASM\Win6Win.asm"
$roto = Join-Path (Split-Path $bueno) "roto.asm"
Set-Content $roto "default rel`r`nsection .text`r`nglobal main`r`nmain:`r`n    mov eax,`r`n    ret`r`n" -Encoding ASCII
Remove-Item ([IO.Path]::ChangeExtension($bueno, ".obj")), ([IO.Path]::ChangeExtension($roto, ".obj")) -ErrorAction SilentlyContinue

$p = Start-Process $exe -ArgumentList "`"$bueno`"", "`"$roto`"" -PassThru
$reloj = [Diagnostics.Stopwatch]::StartNew()
while ($p.MainWindowHandle -eq [IntPtr]::Zero -and $reloj.Elapsed.TotalSeconds -lt 20) { Start-Sleep -Milliseconds 300; $p.Refresh() }
if ($p.MainWindowHandle -eq [IntPtr]::Zero) { throw "No aparecio la ventana del editor" }
Start-Sleep -Seconds 4; $p.Refresh(); $h = $p.MainWindowHandle

if (-not (AlFrente)) { [void][W]::ShowWindow($h, 6); Start-Sleep -Milliseconds 500; [void][W]::ShowWindow($h, 9); Start-Sleep -Milliseconds 800 }
if (-not (AlFrente)) { $p.Kill(); throw "El editor no pudo pasar al primer plano: se corta sin tocar nada." }

try {

# ---------------------------------------------------------------------------
Write-Host ""; Write-Host "=== 1. Sin franja de titulo nativa ===" -ForegroundColor Cyan
$v = New-Object W+RECT; [void][W]::GetWindowRect($h, [ref]$v); $c = [W]::Cliente($h)
$maxAlInicio = [W]::IsZoomed($h)
$arribaEsperado = if ($maxAlInicio) { (Pantalla).WorkingArea.Y } else { $v.Top }
if ($c.Top -eq $arribaEsperado) { Bien "el cliente arranca arriba de todo (maximizada al inicio: $maxAlInicio)" }
else { Mal "el cliente arranca en y=$($c.Top), se esperaba $arribaEsperado" }
if ((BotonesTitulo).Count -eq 3) { Bien "la barra tiene sus 3 botones" } else { Mal "se esperaban 3 botones en la barra, hay $((BotonesTitulo).Count)" }
Capturar "barras_inicio.png"

# ---------------------------------------------------------------------------
Write-Host ""; Write-Host "=== 2. Doble clic en el titulo ===" -ForegroundColor Cyan
if ($maxAlInicio) { TocarDoble (PuntoTitulo); if (-not [W]::IsZoomed($h)) { Bien "restauro la ventana maximizada" } else { Mal "no la restauro" } }
$normal = [W]::Cliente($h)
TocarDoble (PuntoTitulo)
$c = [W]::Cliente($h); $wa = (Pantalla).WorkingArea
if ([W]::IsZoomed($h) -and $c.Left -eq $wa.X -and $c.Top -eq $wa.Y -and ($c.Right - $c.Left) -eq $wa.Width -and ($c.Bottom - $c.Top) -eq $wa.Height) {
    Bien "maximizada ocupa exactamente el area de trabajo ($($wa.Width)x$($wa.Height))"
} else { Mal "maximizada: cliente $(Texto $c), area de trabajo x=$($wa.X) y=$($wa.Y) $($wa.Width)x$($wa.Height)" }
TocarDoble (PuntoTitulo)
if (-not [W]::IsZoomed($h)) { Bien "el segundo doble clic la restauro" } else { Mal "no se restauro" }

# ---------------------------------------------------------------------------
Write-Host ""; Write-Host "=== 3. Arrastrar desde el titulo ===" -ForegroundColor Cyan
$antes = [W]::Cliente($h); $t = PuntoTitulo
ArrastrarSeguro @($t[0], $t[1], ($t[0] - 100), ($t[1] + 50))
$despues = [W]::Cliente($h)
if ((Casi ($despues.Left - $antes.Left) -100) -and (Casi ($despues.Top - $antes.Top) 50)) { Bien "se movio -100,50" }
else { Mal "se movio $($despues.Left - $antes.Left),$($despues.Top - $antes.Top); se esperaba -100,50" }

# ---------------------------------------------------------------------------
Write-Host ""; Write-Host "=== 4. Logo: menu de sistema ===" -ForegroundColor Cyan
$c = [W]::Cliente($h)
Tocar @(($c.Left + 18), ($c.Top + 16))
$menu = [W]::FindWindow("#32768", $null)
if ($menu -ne [IntPtr]::Zero -and [W]::IsWindowVisible($menu)) { Bien "el clic en el logo abrio el menu de sistema" } else { Mal "el logo no abrio el menu de sistema" }
[System.Windows.Forms.SendKeys]::SendWait("{ESC}"); Start-Sleep -Milliseconds 500

# ---------------------------------------------------------------------------
Write-Host ""; Write-Host "=== 5. Botones de la barra ===" -ForegroundColor Cyan
$b = BotonesTitulo
Tocar @(([int](($b[1].Left + $b[1].Right) / 2)), ([int](($b[1].Top + $b[1].Bottom) / 2)))
if ([W]::IsZoomed($h)) { Bien "maximizar maximizo" } else { Mal "maximizar no hizo nada" }
$b = BotonesTitulo
Tocar @(([int](($b[1].Left + $b[1].Right) / 2)), ([int](($b[1].Top + $b[1].Bottom) / 2)))
if (-not [W]::IsZoomed($h)) { Bien "el mismo boton restauro" } else { Mal "no restauro" }
$b = BotonesTitulo
Tocar @(([int](($b[0].Left + $b[0].Right) / 2)), ([int](($b[0].Top + $b[0].Bottom) / 2)))
if ([W]::IsIconic($h)) { Bien "minimizar minimizo" } else { Mal "minimizar no hizo nada" }
[void][W]::ShowWindow($h, 9); Start-Sleep -Milliseconds 800; [void](AlFrente)

# ---------------------------------------------------------------------------
Write-Host ""; Write-Host "=== 6. Barra de estado al compilar ===" -ForegroundColor Cyan
# ⚠ MAXIMIZADA, para que el pie este a la vista. El 23/09 el paso 3 dejo la
# ventana 50 px mas abajo y, con su alto restaurado, la barra de estado quedo
# DETRAS de la barra de tareas de Windows: el clic en el target no llego.
if (-not [W]::IsZoomed($h)) { TocarDoble (PuntoTitulo) }
if ([W]::IsZoomed($h)) { Bien "maximizada: la barra de estado queda a la vista" } else { Mal "no se pudo maximizar antes de probar la barra de estado" }
# Recien abierto hay dos pestanas: Win6Win.asm y roto.asm, activa roto.asm (la
# ultima que se abrio). Se compila ANTES de crear documentos nuevos: la primera
# version cerraba "Sin titulo" con Ctrl+W y dependia de que pestana quedaba
# seleccionada despues, que no es la vecina (defecto previo de CloseTabAt).
if ([W]::Titulo($h) -match "roto\.asm") { Bien "activa: roto.asm" } else { Mal "se esperaba roto.asm activo: '$([W]::Titulo($h))'" }
Teclas "{F7}"; Start-Sleep -Seconds 4
$e = EtiquetasEstado
Write-Host "  barra de estado: $(($e | ForEach-Object { "'$($_.Texto)'" }) -join ' ')"
if ((TextoEstado) -match "1 error") { Bien "el estado dice '$((TextoEstado))'" } else { Mal "tras compilar roto.asm el estado dice '$((TextoEstado))'" }
# @( ): en PowerShell 5.1 un solo objeto filtrado no tiene .Count.
if (@($e | Where-Object { $_.Texto -eq "1" }).Count -ge 1) { Bien "el contador de errores marca 1" } else { Mal "el contador de errores no marca 1" }
Capturar "barras_error.png"

Teclas "^+{TAB}"
if ([W]::Titulo($h) -match "Win6Win\.asm") { Bien "activa: Win6Win.asm" } else { Mal "se esperaba Win6Win.asm activo: '$([W]::Titulo($h))'" }
Teclas "{F7}"; Start-Sleep -Seconds 4
if ((TextoEstado) -eq "Sin errores") { Bien "el estado dice 'Sin errores'" } else { Mal "tras compilar Win6Win.asm el estado dice '$((TextoEstado))'" }
$e = EtiquetasEstado
if (@($e | Where-Object { $_.Texto -eq "0" }).Count -ge 2) { Bien "los contadores volvieron a 0" } else { Mal "los contadores no volvieron a 0" }

# ---------------------------------------------------------------------------
Write-Host ""; Write-Host "=== 7. Target desde la barra de estado ===" -ForegroundColor Cyan
$e = EtiquetasEstado; $target = $e[$e.Count - 1]
Write-Host "  target antes: '$($target.Texto)'"
Tocar @(([int](($target.R.Left + $target.R.Right) / 2)), ([int](($target.R.Top + $target.R.Bottom) / 2)))
Start-Sleep -Milliseconds 500
CapturarPantalla "barras_menu_target.png"
Write-Host "  ventanas del editor tras el clic: $((([W]::TitulosDe([uint32]$p.Id)) | ForEach-Object { "'$_'" }) -join ', ')"
# El menu se abre hacia arriba; se elige el segundo target con el teclado.
[System.Windows.Forms.SendKeys]::SendWait("{DOWN}{DOWN}{ENTER}"); Start-Sleep -Milliseconds 800
$e = EtiquetasEstado; $despues = $e[$e.Count - 1].Texto
if ($despues -ne $target.Texto -and $despues.Length -gt 0) { Bien "el target cambio a '$despues'" } else { Mal "el target no cambio: '$despues'" }

# ---------------------------------------------------------------------------
Write-Host ""; Write-Host "=== 8. Atajos del menu (Ctrl+N) ===" -ForegroundColor Cyan
Teclas "^n"
if ([W]::Titulo($h) -match "Sin t") { Bien "Ctrl+N abrio un documento nuevo: '$([W]::Titulo($h))'" } else { Mal "Ctrl+N no hizo nada: '$([W]::Titulo($h))'" }

# ---------------------------------------------------------------------------
Write-Host ""; Write-Host "=== 9. Buscador de comandos ===" -ForegroundColor Cyan
$tituloAntes = [W]::Titulo($h)
Teclas "^q"
Teclas "nuevo"
$popup = (([W]::TitulosDe([uint32]$p.Id)) -contains "Resultados de la búsqueda")
if ($popup) { Bien "aparecio la lista de resultados" } else { Mal "no aparecio la lista de resultados" }
Capturar "barras_buscador.png"
Teclas "{ENTER}"
$tituloDespues = [W]::Titulo($h)
if ($tituloDespues -ne $tituloAntes -and $tituloDespues -match "Sin t") { Bien "Enter ejecuto Archivo > Nuevo: '$tituloDespues'" }
else { Mal "Enter no ejecuto el comando: antes '$tituloAntes', despues '$tituloDespues'" }
if (-not (([W]::TitulosDe([uint32]$p.Id)) -contains "Resultados de la búsqueda")) { Bien "la lista se cerro" } else { Mal "la lista quedo abierta" }

# ---------------------------------------------------------------------------
Write-Host ""; Write-Host "=== 10. Boton cerrar ===" -ForegroundColor Cyan
$b = BotonesTitulo
Tocar @(([int](($b[2].Left + $b[2].Right) / 2)), ([int](($b[2].Top + $b[2].Bottom) / 2)))
Start-Sleep -Seconds 2

# El editor confirma la salida ("¿Cerrar el editor?", DialogoSalida): que
# aparezca prueba que el boton disparo el cierre. Se contesta "Salir".
$salir = [W]::BotonDe([uint32]$p.Id, "Salir")
if ($salir.Right -gt 0) {
    Bien "el boton cerrar pidio confirmar la salida"
    Tocar @(([int](($salir.Left + $salir.Right) / 2)), ([int](($salir.Top + $salir.Bottom) / 2)))
    Start-Sleep -Seconds 2
}

$p.Refresh()
if ($p.HasExited) { Bien "el editor se cerro" }
else {
    Mal "el editor sigue abierto tras el boton cerrar"
    Capturar "barras_cerrar.png"
}

}
finally {
    $p.Refresh()
    if (-not $p.HasExited) { $p.Kill() }
}

Write-Host ""
if ($fallas -eq 0) { Write-Host "=== TODO BIEN: barras de titulo y de estado ===" -ForegroundColor Green; exit 0 }
Write-Host "=== $fallas FALLAS ===" -ForegroundColor Red
exit 1
