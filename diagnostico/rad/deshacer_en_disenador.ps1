# ============================================================================
# Deshacer y rehacer del DISENADOR, en el editor real.
#
# Abre un .asmform con 6 controles y verifica, por la interfaz:
#
#   1. Recien abierto esta limpio, y un clic que no mueve nada NO lo ensucia
#   2. Mantener la flecha apretada 10 px es UN SOLO paso de deshacer
#   3. Deshacer hasta pasar lo guardado lo vuelve a ensuciar; rehacer vuelve
#   4. Borrar y deshacer recupera el control CON la seleccion (por Id)
#   5. Soltar un control nuevo y deshacerlo lo saca
#   6. Ctrl+Z escribiendo en el PropertyGrid deshace la CASILLA, no el formulario
#   7. La grilla no corre lo que no se toco: un clic no mueve, y una manija
#      solo cambia su propio borde
#
# Las posiciones se leen del .asmform que escribe Ctrl+S, no se deducen de la
# pantalla. La cantidad de controles se lee del arbol del disenador.
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
    [DllImport("user32.dll", CharSet=CharSet.Unicode, EntryPoint="GetWindowTextW")]
    public static extern int GetWindowText(IntPtr h, StringBuilder s, int n);
    [DllImport("user32.dll", CharSet=CharSet.Unicode, EntryPoint="GetClassNameW")]
    public static extern int GetClassName(IntPtr h, StringBuilder s, int n);
    [DllImport("user32.dll")] public static extern bool IsWindowVisible(IntPtr h);
    [DllImport("user32.dll")] public static extern bool IsWindowEnabled(IntPtr h);
    [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr h, out RECT r);
    [DllImport("user32.dll")] public static extern uint GetWindowThreadProcessId(IntPtr h, out uint pid);
    [DllImport("user32.dll", CharSet=CharSet.Unicode, EntryPoint="SendMessageW")]
    public static extern IntPtr SendMessage(IntPtr h, uint m, IntPtr w, IntPtr l);
    [DllImport("user32.dll", CharSet=CharSet.Unicode, EntryPoint="SendMessageW")]
    public static extern IntPtr SendMessageTexto(IntPtr h, uint m, IntPtr w, StringBuilder l);
    public delegate bool EnumProc(IntPtr h, IntPtr p);
    [DllImport("user32.dll")] public static extern bool EnumWindows(EnumProc cb, IntPtr p);
    public struct RECT { public int Left, Top, Right, Bottom; }

    public static IntPtr VentanaPrincipal(uint id) {
        IntPtr res = IntPtr.Zero;
        EnumWindows(delegate(IntPtr h, IntPtr p) {
            uint suyo; GetWindowThreadProcessId(h, out suyo);
            if (suyo == id && IsWindowVisible(h) && Titulo(h).Contains("Editor ASM")) { res = h; return false; }
            return true;
        }, IntPtr.Zero);
        return res;
    }

    public static string Titulo(IntPtr h) {
        var sb = new StringBuilder(512); GetWindowText(h, sb, 512); return sb.ToString();
    }

    // Todas las ventanas hijas visibles, a cualquier profundidad.
    public static List<IntPtr> Hijas(IntPtr raiz) {
        var r = new List<IntPtr>(); Recorrer(raiz, r, 0); return r;
    }
    static void Recorrer(IntPtr h, List<IntPtr> acc, int nivel) {
        if (nivel > 12) return;
        IntPtr hijo = GetWindow(h, 5);
        while (hijo != IntPtr.Zero) {
            if (IsWindowVisible(hijo)) acc.Add(hijo);
            Recorrer(hijo, acc, nivel + 1);
            hijo = GetWindow(hijo, 2);
        }
    }

    public static string Clase(IntPtr h) {
        var sb = new StringBuilder(256); GetClassName(h, sb, 256); return sb.ToString();
    }

    // ⚠ LB_GETTEXT se puede mandar a otro proceso: Windows lo traslada para
    // los mensajes por debajo de WM_USER, que es el caso de los de ListBox.
    public static int CantidadEnLista(IntPtr lista) {
        return (int)SendMessage(lista, 0x018B, IntPtr.Zero, IntPtr.Zero);   // LB_GETCOUNT
    }
    public static string ItemDeLista(IntPtr lista, int i) {
        var sb = new StringBuilder(512);
        SendMessageTexto(lista, 0x0189, (IntPtr)i, sb);                      // LB_GETTEXT
        return sb.ToString();
    }
    public static int SeleccionDeLista(IntPtr lista) {
        return (int)SendMessage(lista, 0x0188, IntPtr.Zero, IntPtr.Zero);   // LB_GETCURSEL
    }
}
public class M {
    [DllImport("user32.dll")] public static extern bool SetCursorPos(int x, int y);
    [DllImport("user32.dll")] public static extern void mouse_event(uint f, uint x, uint y, uint d, IntPtr e);
    [DllImport("user32.dll")] public static extern void keybd_event(byte vk, byte scan, uint f, IntPtr e);
    public static void Clic(int x, int y) {
        SetCursorPos(x, y);
        System.Threading.Thread.Sleep(130);
        mouse_event(0x02, 0, 0, 0, IntPtr.Zero);
        System.Threading.Thread.Sleep(60);
        mouse_event(0x04, 0, 0, 0, IntPtr.Zero);
        System.Threading.Thread.Sleep(250);
    }
    // Clic con el temblor de una mano real: aprieta, se corre 1 px y suelta.
    public static void ClicConTemblor(int x, int y) {
        SetCursorPos(x, y);
        System.Threading.Thread.Sleep(130);
        mouse_event(0x02, 0, 0, 0, IntPtr.Zero);
        System.Threading.Thread.Sleep(60);
        SetCursorPos(x + 1, y + 1);
        System.Threading.Thread.Sleep(60);
        mouse_event(0x04, 0, 0, 0, IntPtr.Zero);
        System.Threading.Thread.Sleep(250);
    }
    // Arrastre de verdad, en pasos, como lo haria una persona.
    public static void Arrastrar(int x1, int y1, int x2, int y2) {
        SetCursorPos(x1, y1);
        System.Threading.Thread.Sleep(130);
        mouse_event(0x02, 0, 0, 0, IntPtr.Zero);
        System.Threading.Thread.Sleep(80);
        for (int i = 1; i <= 10; i++) {
            SetCursorPos(x1 + (x2 - x1) * i / 10, y1 + (y2 - y1) * i / 10);
            System.Threading.Thread.Sleep(30);
        }
        System.Threading.Thread.Sleep(80);
        mouse_event(0x04, 0, 0, 0, IntPtr.Zero);
        System.Threading.Thread.Sleep(300);
    }
    // Tecla MANTENIDA: varias pulsaciones y UNA sola liberacion, como cuando
    // se deja el dedo apretado y actua la autorrepeticion del teclado.
    public static void Mantener(byte vk, int repeticiones) {
        for (int i = 0; i < repeticiones; i++) {
            keybd_event(vk, 0, 0x1, IntPtr.Zero);          // KEYEVENTF_EXTENDEDKEY
            System.Threading.Thread.Sleep(50);
        }
        keybd_event(vk, 0, 0x1 | 0x2, IntPtr.Zero);        // + KEYEVENTF_KEYUP
        System.Threading.Thread.Sleep(300);
    }
}
"@

# Editor aislado: no usa la configuracion ni los proyectos de Fabian.
. "$PSScriptRoot\..\editor_aislado.ps1"
$exe    = PrepararEditorAislado
$banco  = "$PSScriptRoot\banco_deshacer"
$fallas = 0

function Bien($t) { Write-Host "  OK   $t" -ForegroundColor Green }
function Mal($t)  { Write-Host "  MAL  $t" -ForegroundColor Red; $script:fallas++ }

function Capturar($h, $archivo) {
    $r = New-Object W+RECT
    [void][W]::GetWindowRect($h, [ref]$r)
    $an = $r.Right - $r.Left; $al = $r.Bottom - $r.Top
    if ($an -le 0 -or $al -le 0) { return }
    $bmp = New-Object System.Drawing.Bitmap $an, $al
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.CopyFromScreen($r.Left, $r.Top, 0, 0, $bmp.Size)
    $bmp.Save($archivo, [System.Drawing.Imaging.ImageFormat]::Png)
    $g.Dispose(); $bmp.Dispose()
}

function Teclas($t) {
    [void][W]::SetForegroundWindow($script:hMain)
    Start-Sleep -Milliseconds 300
    [System.Windows.Forms.SendKeys]::SendWait($t)
    Start-Sleep -Milliseconds 700
}

# ⚠ NO SE USA -like con "`* *": entre comillas dobles PowerShell se come el
# backtick, el patron queda "* *" (cualquier cosa con un espacio) y da
# "sucio" para casi cualquier titulo. El titulo sucio EMPIEZA con "* ".
function Sucio { ([W]::Titulo($script:hMain)).StartsWith("* ") }

function Controles { [W]::CantidadEnLista($script:arbol) }

# Guarda con Ctrl+S y devuelve el control pedido tal como quedo en el disco.
function EnDisco($nombre) {
    Teclas "^s"
    Start-Sleep -Milliseconds 500
    $j = Get-Content $script:asmform -Raw -Encoding UTF8 | ConvertFrom-Json
    return ($j.Formulario.Controles | Where-Object { $_.Nombre -eq $nombre })
}

function XEnDisco($nombre) {
    $c = EnDisco $nombre
    if ($null -eq $c) { return $null }
    return [int]$c.X
}

if (-not (Test-Path $exe)) { throw "No existe $exe. Compila primero." }
if (Test-Path $banco) { Remove-Item $banco -Recurse -Force }
New-Item -ItemType Directory -Path $banco | Out-Null

# ---------------------------------------------------------------------------
Write-Host ""
Write-Host "=== Preparando un .asmform con 6 controles ===" -ForegroundColor Cyan

# El mismo generador que usa abrir_asmform_y_generar.ps1: el archivo es el que
# escribe el propio modelo, no uno escrito a mano.
$gen = "$PSScriptRoot\generarform"
Push-Location $gen
$ErrorActionPreference = "Continue"
$build = & dotnet build -v quiet --nologo 2>&1
$codigo = $LASTEXITCODE
$ErrorActionPreference = "Stop"
Pop-Location
if ($codigo -ne 0) { $build | Write-Host; throw "No compilo generarform" }

& "$gen\bin\Debug\net8.0\generarform.exe" $banco | Out-Null
if ($LASTEXITCODE -ne 0) { throw "generarform fallo" }

$asmform = "$banco\Prueba.asmform"
if (-not (Test-Path $asmform)) { throw "No se genero $asmform" }
Write-Host "  $asmform"

# Se mide sobre la etiqueta: etiqueta1, en (20, 20) de 80x20.
#
# ⚠ TIENE QUE ESTAR ALINEADA A LA GRILLA (multiplos de 4). Un clic sobre un
# control lo pasa por el ajuste a la grilla aunque no se mueva el raton: el
# boton "Aceptar" esta en X=110 y un clic lo corria a 112 (Math.Round(27.5)
# redondea al par). Es un comportamiento previo del canvas, no del deshacer, y
# medir sobre el boton mezclaba las dos cosas.
$nombre = "etiqueta1"

# ---------------------------------------------------------------------------
Write-Host ""
Write-Host "=== Abriendo el editor ===" -ForegroundColor Cyan

$p = Start-Process $exe -ArgumentList "`"$asmform`"" -PassThru
Start-Sleep -Seconds 6
$p.Refresh()
if ($p.HasExited) { throw "El editor se cerro solo" }

$hMain = [W]::VentanaPrincipal([uint32]$p.Id)
if ($hMain -eq [IntPtr]::Zero) { $p.Kill(); throw "No se encontro la ventana del editor" }
Write-Host "  titulo: '$([W]::Titulo($hMain))'"

try {

# Arbol de controles = el ListBox visible MAS A LA DERECHA (la paleta va a la
# izquierda del canvas y el explorador todavia mas a la izquierda).
$listas = [W]::Hijas($hMain) | Where-Object { [W]::Clase($_) -like "*LISTBOX*" }
$conRect = foreach ($l in $listas) {
    $r = New-Object W+RECT; [void][W]::GetWindowRect($l, [ref]$r)
    [pscustomobject]@{ H = $l; R = $r }
}
$ordenadas = @($conRect | Sort-Object { $_.R.Left })
$arbol  = $ordenadas[-1].H
$rArbol = $ordenadas[-1].R

# La paleta es la lista pasado el explorador (x > 240 respecto de la ventana).
$rVentana = New-Object W+RECT; [void][W]::GetWindowRect($hMain, [ref]$rVentana)
$paleta = @($ordenadas | Where-Object { $_.R.Left -gt ($rVentana.Left + 240) })[0].R

# Canvas = la ventana hija que mide exactamente formulario + 2 margenes de 40:
# 400x300 -> 480x380. Es la unica con esa medida.
$canvas = $null
foreach ($h in [W]::Hijas($hMain)) {
    $r = New-Object W+RECT; [void][W]::GetWindowRect($h, [ref]$r)
    if (($r.Right - $r.Left) -eq 480 -and ($r.Bottom - $r.Top) -eq 380) { $canvas = $r; break }
}
if ($null -eq $canvas) { throw "No se encontro el canvas (480x380)" }

Write-Host "  listas: $($ordenadas.Count)   arbol x=$($rArbol.Left)   paleta x=$($paleta.Left)   canvas x=$($canvas.Left) y=$($canvas.Top)"
Write-Host "  ventana: x=$($rVentana.Left) y=$($rVentana.Top) $($rVentana.Right - $rVentana.Left)x$($rVentana.Bottom - $rVentana.Top)"
Capturar $hMain "$PSScriptRoot\deshacer_inicio.png"

# ⚠ LAS COORDENADAS SE MIDEN UNA VEZ. Si la ventana se mueve o cambia de
# tamaño a mitad de la prueba (el 23/09 paso: termino sin maximizar), todos
# los clics caen corridos y la prueba falla por algo ajeno al editor. Se
# vuelve a mirar el rectangulo antes de cada seccion y se corta si cambio.
$script:rVentanaInicial = $rVentana
function VentanaQuieta($seccion) {
    $r = New-Object W+RECT; [void][W]::GetWindowRect($script:hMain, [ref]$r)
    $i = $script:rVentanaInicial
    if ($r.Left -ne $i.Left -or $r.Top -ne $i.Top -or $r.Right -ne $i.Right -or $r.Bottom -ne $i.Bottom) {
        Capturar $script:hMain "$PSScriptRoot\deshacer_ventana_movida.png"
        throw "La ventana del editor se movio o cambio de tamano antes de '$seccion' " +
              "(era x=$($i.Left) y=$($i.Top) $($i.Right - $i.Left)x$($i.Bottom - $i.Top), " +
              "ahora x=$($r.Left) y=$($r.Top) $($r.Right - $r.Left)x$($r.Bottom - $r.Top)). " +
              "Los clics caerian corridos: la corrida no vale. Captura: deshacer_ventana_movida.png"
    }
}

# Punto de pantalla de una coordenada del modelo: margen de 40 del canvas.
function EnPantalla($x, $y) { @(($script:canvas.Left + 40 + $x), ($script:canvas.Top + 40 + $y)) }

# ---------------------------------------------------------------------------
Write-Host ""
Write-Host "=== 1. Recien abierto esta limpio; un clic sin mover no lo ensucia ===" -ForegroundColor Cyan
VentanaQuieta "1"

if ((Controles) -eq 6) { Bien "el arbol muestra los 6 controles" } else { Mal "el arbol muestra $(Controles) controles" }
if (-not (Sucio)) { Bien "arranca limpio" } else { Mal "arranca sucio" }

# Clic en el medio de la etiqueta: la selecciona y le da el foco al canvas. Es
# un "arrastre" de cero pixeles.
$pt = EnPantalla 60 30
[void][W]::SetForegroundWindow($hMain); Start-Sleep -Milliseconds 300
[M]::Clic($pt[0], $pt[1])

$sel = [W]::SeleccionDeLista($arbol)
if ($sel -ge 0 -and ([W]::ItemDeLista($arbol, $sel)) -like "$nombre *") { Bien "quedo seleccionado $nombre" }
else { Mal "no quedo seleccionado $nombre (seleccion del arbol: $sel)" }

if (-not (Sucio)) { Bien "un clic sin mover NO ensucia (el historial compara estados)" }
else { Mal "un clic sin mover ensucio la pestana" }

# ---------------------------------------------------------------------------
Write-Host ""
Write-Host "=== 2. Flecha mantenida 10 px = un solo paso ===" -ForegroundColor Cyan
VentanaQuieta "2"

[void][W]::SetForegroundWindow($hMain); Start-Sleep -Milliseconds 300
[M]::Mantener(0x27, 10)   # VK_RIGHT

if (Sucio) { Bien "mover lo ensucio" } else { Mal "mover no ensucio" }

$x = XEnDisco $nombre
if ($x -eq 30) { Bien "en el disco quedo X=30 (20 + 10)" } else { Mal "en el disco quedo X=$x, se esperaba 30" }
if (-not (Sucio)) { Bien "Ctrl+S lo dejo limpio" } else { Mal "Ctrl+S no lo limpio" }

Teclas "^z"
if (Sucio) { Bien "deshacer despues de guardar lo vuelve a ensuciar" } else { Mal "deshacer despues de guardar lo dejo limpio" }

$x = XEnDisco $nombre
if ($x -eq 20) { Bien "UN Ctrl+Z deshizo los 10 px juntos (X=20)" } else { Mal "tras un Ctrl+Z quedo X=$x, se esperaba 20" }

# ---------------------------------------------------------------------------
Write-Host ""
Write-Host "=== 3. Rehacer ===" -ForegroundColor Cyan
VentanaQuieta "3"

Teclas "^y"
$x = XEnDisco $nombre
if ($x -eq 30) { Bien "Ctrl+Y rehizo el movimiento (X=30)" } else { Mal "tras Ctrl+Y quedo X=$x, se esperaba 30" }

# ---------------------------------------------------------------------------
Write-Host ""
Write-Host "=== 4. Borrar y deshacer recupera el control y su seleccion ===" -ForegroundColor Cyan
VentanaQuieta "4"

# ⚠ Supr solo borra si la seleccion sobrevivio al deshacer/rehacer: el
# formulario repuesto trae objetos nuevos y la seleccion se recupera por Id.
Teclas "{DELETE}"
if ((Controles) -eq 5) { Bien "Supr borro el control seleccionado (quedan 5): la seleccion sobrevivio a Ctrl+Z/Ctrl+Y" }
else { Mal "tras Supr hay $(Controles) controles, se esperaban 5" }

Teclas "^z"
if ((Controles) -eq 6) { Bien "Ctrl+Z lo recupero (6 controles)" } else { Mal "tras Ctrl+Z hay $(Controles) controles" }

$items = 0..((Controles) - 1) | ForEach-Object { [W]::ItemDeLista($arbol, $_) }
if ($items -like "$nombre *") { Bien "el recuperado es $nombre" } else { Mal "no aparece $nombre en el arbol" }

$x = XEnDisco $nombre
if ($x -eq 30) { Bien "volvio en su posicion (X=30)" } else { Mal "volvio con X=$x" }

# ---------------------------------------------------------------------------
Write-Host ""
Write-Host "=== 5. Soltar un control nuevo y deshacerlo ===" -ForegroundColor Cyan
VentanaQuieta "5"

# Boton = indice 3 de la paleta; los items miden 15 px (medido en
# disenador_en_pestanas.ps1). Se suelta en (330,200), donde no hay nada.
[void][W]::SetForegroundWindow($hMain); Start-Sleep -Milliseconds 300
[M]::Clic(($paleta.Left + 40), ($paleta.Top + 7 + 3 * 15))
$pt = EnPantalla 330 200
[M]::Clic($pt[0], $pt[1])

if ((Controles) -eq 7) { Bien "se solto el control nuevo (7)" } else { Mal "tras soltar hay $(Controles) controles" }

# ---------------------------------------------------------------------------
Write-Host ""
Write-Host "=== 6. Ctrl+Z escribiendo en el PropertyGrid es de la casilla ===" -ForegroundColor Cyan
VentanaQuieta "6"

# El PropertyGrid va debajo del arbol y su rotulo (26 px). Se hace clic en
# la columna de nombres de una fila, se navega con teclas hasta "Nombre"
# (Inicio = categoria "Identidad", Abajo = Nombre) y se escribe: eso abre la
# casilla de edicion con el foco.
[M]::Clic(($rArbol.Left + 50), ($rArbol.Bottom + 26 + 60))
Teclas "{HOME}"
Teclas "{DOWN}"
Teclas "zz"

$foco = [W]::Hijas($hMain) | Where-Object { [W]::Clase($_) -like "*EDIT*" } |
        ForEach-Object { $r = New-Object W+RECT; [void][W]::GetWindowRect($_, [ref]$r); $r } |
        Where-Object { $_.Left -ge $rArbol.Left }
Write-Host "  casillas de edicion visibles en el panel derecho: $(@($foco).Count)"
Capturar $hMain "$PSScriptRoot\deshacer_propertygrid.png"
Write-Host "  captura: $PSScriptRoot\deshacer_propertygrid.png"

Teclas "^z"

if ((Controles) -eq 7) { Bien "Ctrl+Z en la casilla NO deshizo el formulario (siguen 7)" }
else { Mal "Ctrl+Z en la casilla deshizo el formulario: hay $(Controles) controles" }

# Se cancela la edicion: el nombre tiene que seguir siendo boton3.
Teclas "{ESC}"
if ([W]::ItemDeLista($arbol, 6) -like "boton3 *") { Bien "Esc cancelo la edicion (sigue boton3)" }
else { Mal "tras Esc el control nuevo quedo como '$([W]::ItemDeLista($arbol, 6))'" }

# Se vuelve al canvas.
$pt = EnPantalla 370 290
[M]::Clic($pt[0], $pt[1])

Teclas "^z"
if ((Controles) -eq 6) { Bien "con el foco en el canvas, Ctrl+Z saco el control soltado (6)" }
else { Mal "tras Ctrl+Z en el canvas hay $(Controles) controles, se esperaban 6" }

# ---------------------------------------------------------------------------
Write-Host ""
Write-Host "=== 7. La grilla no corre lo que no se toco ===" -ForegroundColor Cyan
VentanaQuieta "7"

# Sobre "Aceptar" (boton1): X=110, Y=60, 100x30. X NO es multiplo de 4, que
# es justo el caso en que un clic lo corria a 112.
$b = "boton1"

if (Sucio) { Mal "el paso 7 arranca sucio: los pasos anteriores dejaron algo sin guardar" }

# a) Clic con temblor de 1 px: tiene que quedar dentro de la tolerancia de clic.
$pt = EnPantalla 160 75
[void][W]::SetForegroundWindow($hMain); Start-Sleep -Milliseconds 300
[M]::ClicConTemblor($pt[0], $pt[1])

if (-not (Sucio)) { Bien "un clic con temblor de 1 px NO ensucio" } else { Mal "un clic con temblor de 1 px ensucio la pestana" }

$c = EnDisco $b
if ([int]$c.X -eq 110 -and [int]$c.Y -eq 60) { Bien "sigue en (110, 60)" } else { Mal "quedo en ($($c.X), $($c.Y)), se esperaba (110, 60)" }

# b) Estirar la manija DERECHA 20 px: cambia el ancho y el borde izquierdo
# (X=110, fuera de la grilla) no se toca. La manija E esta en (210, 75).
$p1 = EnPantalla 210 75; $p2 = EnPantalla 230 75
[M]::Arrastrar($p1[0], $p1[1], $p2[0], $p2[1])

$c = EnDisco $b
if ([int]$c.Ancho -eq 120) { Bien "la manija derecha dejo el ancho en 120" } else { Mal "tras la manija derecha el ancho es $($c.Ancho), se esperaba 120" }
if ([int]$c.X -eq 110 -and [int]$c.Y -eq 60) { Bien "X e Y no se movieron (110, 60)" } else { Mal "la manija derecha corrio el origen a ($($c.X), $($c.Y))" }

# c) Arrastre de verdad, 30 px a la derecha: se ajusta a la grilla como antes.
# 110 + 30 = 140, que ya es multiplo de 4; Y=60 tambien.
$p1 = EnPantalla 170 75; $p2 = EnPantalla 200 75
[M]::Arrastrar($p1[0], $p1[1], $p2[0], $p2[1])

$c = EnDisco $b
if ([int]$c.X -eq 140 -and [int]$c.Y -eq 60) { Bien "el arrastre real lo llevo a (140, 60), en la grilla" } else { Mal "tras arrastrar quedo en ($($c.X), $($c.Y)), se esperaba (140, 60)" }

# d) Estirar la manija IZQUIERDA 10 px hacia la izquierda: el borde izquierdo
# va a la grilla (140-10=130 -> 128) y el DERECHO queda fijo en 140+120=260.
$p1 = EnPantalla 140 75; $p2 = EnPantalla 130 75
[M]::Arrastrar($p1[0], $p1[1], $p2[0], $p2[1])

$c = EnDisco $b
$derecha = [int]$c.X + [int]$c.Ancho
if ([int]$c.X -eq 128) { Bien "el borde izquierdo quedo en la grilla (X=128)" } else { Mal "el borde izquierdo quedo en X=$($c.X), se esperaba 128" }
if ($derecha -eq 260) { Bien "el borde derecho no se movio (260)" } else { Mal "el borde derecho se movio a $derecha, se esperaba 260" }

Capturar $hMain "$PSScriptRoot\deshacer_final.png"

}
finally {
    Write-Host ""
    Write-Host "=== Cerrando ===" -ForegroundColor Cyan
    try { $p.Kill() } catch { }
}

Write-Host ""
if ($fallas -eq 0) {
    Write-Host "=== TODO BIEN: deshacer y rehacer del disenador ===" -ForegroundColor Green
    exit 0
}

Write-Host "=== $fallas FALLAS ===" -ForegroundColor Red
exit 1
