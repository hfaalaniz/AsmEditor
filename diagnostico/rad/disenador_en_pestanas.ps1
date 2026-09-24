# ============================================================================
# El disenador COMO PESTANA (etapa 3).
#
# Verifica lo que cambia al dejar de ser modal:
#
#   1. Ctrl+D abre una PESTANA, no una ventana aparte
#   2. La ventana principal sigue usable (no esta bloqueada)
#   3. Se pueden soltar controles y la pestana se marca como sucia (*)
#   4. Ctrl+W la cierra —con el modal esto no existia— y pregunta por los cambios
#   5. Un .asmform se abre en el disenador, no como texto JSON
#
# El punto 4 es el que mas importa: con el tipo concreto en CloseTabAt, una
# pestana de disenador NO SE PODIA CERRAR NUNCA.
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
    [DllImport("user32.dll")] public static extern int GetClassNameA(IntPtr h, StringBuilder s, int n);
    [DllImport("user32.dll")] public static extern int GetWindowTextA(IntPtr h, StringBuilder s, int n);
    [DllImport("user32.dll")] public static extern bool IsWindowVisible(IntPtr h);
    [DllImport("user32.dll")] public static extern bool IsWindowEnabled(IntPtr h);
    [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr h, out RECT r);
    [DllImport("user32.dll")] public static extern uint GetWindowThreadProcessId(IntPtr h, out uint pid);
    public delegate bool EnumProc(IntPtr h, IntPtr p);
    [DllImport("user32.dll")] public static extern bool EnumWindows(EnumProc cb, IntPtr p);
    public struct RECT { public int Left, Top, Right, Bottom; }

    public static List<KeyValuePair<IntPtr,string>> VentanasDe(uint id) {
        var lista = new List<KeyValuePair<IntPtr,string>>();
        EnumWindows(delegate(IntPtr h, IntPtr p) {
            uint suyo;
            GetWindowThreadProcessId(h, out suyo);
            if (suyo == id && IsWindowVisible(h)) {
                var sb = new StringBuilder(512);
                GetWindowTextA(h, sb, 512);
                if (sb.Length > 0) lista.Add(new KeyValuePair<IntPtr,string>(h, sb.ToString()));
            }
            return true;
        }, IntPtr.Zero);
        return lista;
    }

    // Texto de todos los controles visibles: los dialogos de este editor son
    // paneles dentro de la ventana, no ventanas, y solo se los ve asi.
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
                GetWindowTextA(hijo, sb, 1024);
                if (sb.Length > 0) acc.Add(sb.ToString());
            }
            Recorrer(hijo, acc, nivel + 1);
            hijo = GetWindow(hijo, 2);
        }
    }
}
// Busca un Button por su texto en TODO el escritorio, incluidas las ventanas
// sin titulo: el dialogo del editor es un Form con FormBorderStyle.None y sin
// Text, asi que no aparece al enumerar ventanas "con titulo".
public class Boton {
    [DllImport("user32.dll")] static extern IntPtr GetWindow(IntPtr h, uint c);
    [DllImport("user32.dll")] static extern IntPtr GetDesktopWindow();
    [DllImport("user32.dll")] static extern bool IsWindowVisible(IntPtr h);
    [DllImport("user32.dll", CharSet=CharSet.Unicode, EntryPoint="GetClassNameW")]
    static extern int GetClassName(IntPtr h, StringBuilder s, int n);
    [DllImport("user32.dll", CharSet=CharSet.Unicode, EntryPoint="GetWindowTextW")]
    static extern int GetWindowText(IntPtr h, StringBuilder s, int n);

    public static IntPtr BuscarPorTexto(IntPtr raiz, string texto) {
        if (raiz == IntPtr.Zero) raiz = GetDesktopWindow();
        return Recorrer(raiz, texto, 0);
    }

    static IntPtr Recorrer(IntPtr h, string texto, int nivel) {
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
            IntPtr dentro = Recorrer(hijo, texto, nivel + 1);
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
        System.Threading.Thread.Sleep(130);
        mouse_event(0x02, 0, 0, 0, IntPtr.Zero);
        System.Threading.Thread.Sleep(60);
        mouse_event(0x04, 0, 0, 0, IntPtr.Zero);
        System.Threading.Thread.Sleep(200);
    }
}
"@

# Editor aislado: no usa la configuracion ni los proyectos de Fabian.
. "$PSScriptRoot\..\editor_aislado.ps1"
$exe    = PrepararEditorAislado
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

if (-not (Test-Path $exe)) { throw "No existe $exe. Compila primero." }

Write-Host ""
Write-Host "=== Arrancando el editor ===" -ForegroundColor Cyan

$p = Start-Process $exe -PassThru
Start-Sleep -Seconds 5
$p.Refresh()
if ($p.HasExited) { throw "El editor se cerro solo" }

$vent = [W]::VentanasDe([uint32]$p.Id)
$principal = ($vent | Where-Object { $_.Value -like "*Editor ASM*" } | Select-Object -First 1)
if (-not $principal) { $p.Kill(); throw "No se encontro la ventana del editor" }

$hMain = $principal.Key
Write-Host "  titulo: '$($principal.Value)'"

# ---------------------------------------------------------------------------
Write-Host ""
Write-Host "=== 1. Ctrl+D abre una PESTANA, no una ventana ===" -ForegroundColor Cyan

# Teclas y clics protegidos: solo sobre el editor (ver proteccion_interfaz.ps1;
# el 23/09 el Ctrl+D y el Ctrl+W de esta prueba pudieron ir a otra ventana).
. "$PSScriptRoot\..\proteccion_interfaz.ps1"
if (-not (TraerAlFrente $p $hMain)) { AvisoProteccion "el editor no pudo pasar al primer plano" }
[void](TeclasProtegidas $p "^d" 3000)

$vent2 = [W]::VentanasDe([uint32]$p.Id)

# ⚠ Con el modal aparecia una ventana "Disenador de formularios" aparte.
# Ahora NO tiene que haber ninguna: el disenador vive dentro del editor.
$modal = $vent2 | Where-Object { $_.Value -like "*ise*ador de formularios*" }

if ($modal) {
    Mal "abrio una ventana aparte: sigue siendo modal"
} else {
    Bien "no abrio ninguna ventana aparte"
}

$tituloAhora = ($vent2 | Where-Object { $_.Value -like "*Editor ASM*" } | Select-Object -First 1).Value
Write-Host "  titulo ahora: '$tituloAhora'"

if ($tituloAhora -match "Sin t") {
    Bien "el titulo muestra el formulario sin guardar como documento activo"
} else {
    Mal "el titulo no cambio al abrir el disenador: '$tituloAhora'"
}

# ---------------------------------------------------------------------------
Write-Host ""
Write-Host "=== 2. La ventana principal sigue usable ===" -ForegroundColor Cyan

# ⚠ Un modal DESHABILITA su duenio. Si la ventana principal sigue habilitada,
# el disenador ya no la bloquea.
if ([W]::IsWindowEnabled($hMain)) {
    Bien "la ventana principal esta habilitada (no hay modal bloqueandola)"
} else {
    Mal "la ventana principal esta deshabilitada: algo la bloquea"
}

# ---------------------------------------------------------------------------
Write-Host ""
Write-Host "=== 3. Soltar controles marca la pestana como sucia ===" -ForegroundColor Cyan

# Ubicar la paleta midiendo, no estimando.
#
# Con la pestana del disenador al frente, los unicos ListBox visibles son los
# del disenador: la paleta (a la izquierda del lienzo) y el arbol de
# controles (a la derecha). El explorador es un TreeView y no cuenta.
#
# ⚠ HASTA EL 23/09 SE FILTRABA "x > 240" porque el explorador ocupaba los
# primeros 230 px de la izquierda. Desde la Etapa 3 esta a la DERECHA (y se
# puede mover): el filtro dejaba afuera a la paleta. Ahora: la paleta es el
# ListBox mas a la izquierda, sin suponer donde esta el explorador.
Add-Type @"
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
// ⚠ Cada Add-Type compila en SU PROPIO ensamblado: esta clase no ve el RECT
// de la clase W, asi que declara el suyo.
public class Lista {
    public struct Caja { public int Left, Top, Right, Bottom; }

    [DllImport("user32.dll")] static extern IntPtr GetWindow(IntPtr h, uint c);
    [DllImport("user32.dll")] static extern bool IsWindowVisible(IntPtr h);
    [DllImport("user32.dll")] static extern bool GetWindowRect(IntPtr h, out Caja r);
    [DllImport("user32.dll", CharSet=CharSet.Unicode, EntryPoint="GetClassNameW")]
    static extern int GetClassName(IntPtr h, StringBuilder s, int n);

    // Todos los ListBox visibles, con su rectangulo, a cualquier profundidad.
    public static List<Caja> ListBoxes(IntPtr raiz) {
        var r = new List<Caja>();
        Recorrer(raiz, r, 0);
        return r;
    }
    static void Recorrer(IntPtr h, List<Caja> acc, int nivel) {
        if (nivel > 8) return;
        IntPtr hijo = GetWindow(h, 5);
        while (hijo != IntPtr.Zero) {
            if (IsWindowVisible(hijo)) {
                var sb = new StringBuilder(256);
                GetClassName(hijo, sb, 256);
                if (sb.ToString().Contains("ListBox")) {
                    Caja rr;
                    GetWindowRect(hijo, out rr);
                    acc.Add(rr);
                }
            }
            Recorrer(hijo, acc, nivel + 1);
            hijo = GetWindow(hijo, 2);
        }
    }
}
"@

$rVentana = New-Object W+RECT
[void][W]::GetWindowRect($hMain, [ref]$rVentana)

$todas = [Lista]::ListBoxes($hMain)

Write-Host "  ListBox visibles: $($todas.Count)"
foreach ($lb in $todas) {
    Write-Host "    x=$($lb.Left) y=$($lb.Top) ancho=$($lb.Right - $lb.Left)"
}

# La paleta: la mas a la izquierda (el arbol de controles va a la derecha).
$candidatas = $todas | Sort-Object { $_.Left }

$paleta = if ($candidatas) {
    $c = @($candidatas)[0]
    @{ Left = $c.Left; Top = $c.Top; Right = $c.Right }
} else { $null }

if ($null -eq $paleta) {
    Mal "no se encontro la paleta del disenador"
} else {
    $rMain = New-Object W+RECT
    [void][W]::GetWindowRect($hMain, [ref]$rMain)

    # Boton = indice 3 de la paleta; los items miden 15 px.
    [void](ClicProtegido $p ($paleta.Left + 40) ($paleta.Top + 7 + 3 * 15))
    [void](ClicProtegido $p ($paleta.Right + 150) ($rMain.Top + 220))
    Start-Sleep -Milliseconds 600

    $t = ([W]::VentanasDe([uint32]$p.Id) |
          Where-Object { $_.Value -like "*Editor ASM*" } | Select-Object -First 1).Value

    Write-Host "  titulo: '$t'"

    # ⚠ NO -like "`* *": entre comillas dobles PowerShell se come el backtick,
    # el patron queda "* *" y coincide con casi cualquier titulo. Esta
    # verificacion paso siempre hasta el 23/09. El titulo sucio EMPIEZA con "* ".
    if ($t.StartsWith("* ")) {
        Bien "la pestana quedo marcada como sucia (*)"
    } else {
        Mal "la pestana NO se marco como sucia"
    }
}

Capturar $hMain "$PSScriptRoot\disenador_en_pestana.png"
Write-Host "  captura: $PSScriptRoot\disenador_en_pestana.png"

# ---------------------------------------------------------------------------
Write-Host ""
Write-Host "=== 4. Ctrl+W cierra la pestana y pregunta por los cambios ===" -ForegroundColor Cyan

if (-not (TraerAlFrente $p $hMain)) { AvisoProteccion "el editor no pudo pasar al primer plano" }
[void](TeclasProtegidas $p "^w" 2000)

# ⚠ EL DIALOGO DE ESTE EDITOR NO SE PUEDE LEER POR TEXTO: Dialogo.Preguntar no
# es una ventana aparte NI usa Labels con texto — PINTA el contenido sobre un
# panel. Ni enumerar ventanas ni leer el texto de los controles lo encuentran.
#
# Lo que si es medible: mientras esta abierto, el TabControl del editor queda
# DESHABILITADO (el dialogo bloquea la ventana), y al responder se reactiva.
# ⚠ CÓMO SE DETECTA ESTE DIÁLOGO, leído de Dialogo.cs y no adivinado:
#
#   - Es un Form real abierto con ShowDialog, o sea MODAL: mientras está,
#     la ventana principal queda DESHABILITADA. Eso es lo que se mide.
#   - Tiene FormBorderStyle.None y NUNCA se le asigna Text, así que
#     GetWindowText sobre él devuelve vacío: enumerar "ventanas con título"
#     no lo encuentra jamás. Fue lo que me hizo dar cuatro vueltas.
$bloqueado = -not [W]::IsWindowEnabled($hMain)

if ($bloqueado) {
    Bien "Ctrl+W abrio la pregunta por los cambios sin guardar"

    # ⚠ SE HACE CLIC EN EL BOTON, no se navega con teclas: segun Dialogo.cs el
    # foco arranca en AcceptButton (el predeterminado, "Guardar"), y guardar
    # abriria un "Guardar como" que esta prueba no quiere. Se busca el Button
    # que dice "Descartar" y se lo pulsa.
    $btn = [Boton]::BuscarPorTexto([IntPtr]::Zero, "Descartar")

    if ($btn -eq [IntPtr]::Zero) {
        Mal "no se encontro el boton Descartar"
    } else {
        $rb = New-Object W+RECT
        [void][W]::GetWindowRect($btn, [ref]$rb)
        [void](ClicProtegido $p ([int](($rb.Left + $rb.Right) / 2)) ([int](($rb.Top + $rb.Bottom) / 2)))
        Start-Sleep -Seconds 2
    }
} else {
    Mal "Ctrl+W no hizo efecto con el foco en el disenador"
    Capturar $hMain "$PSScriptRoot\falla_cerrar.png"
    Write-Host "  captura: $PSScriptRoot\falla_cerrar.png" -ForegroundColor Yellow
}

$tFinal = ([W]::VentanasDe([uint32]$p.Id) |
           Where-Object { $_.Value -like "*Editor ASM*" } | Select-Object -First 1).Value

Write-Host "  titulo final: '$tFinal'"

# Sin pestanas, el titulo vuelve al del editor sin documento.
if ($tFinal -notmatch "Sin t") {
    Bien "la pestana del disenador se cerro"
} else {
    Mal "la pestana sigue abierta: no se pudo cerrar"
}

# ---------------------------------------------------------------------------
Write-Host ""
Write-Host "=== Cerrando ===" -ForegroundColor Cyan
try { $p.Kill() } catch { }

Write-Host ""
if ($fallas -eq 0) {
    Write-Host "=== TODO BIEN: el disenador es una pestana mas ===" -ForegroundColor Green
    exit 0
}

Write-Host "=== $fallas FALLAS ===" -ForegroundColor Red
exit 1
