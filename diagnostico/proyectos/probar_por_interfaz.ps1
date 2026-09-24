# ============================================================================
# Prueba de proyectos POR LA INTERFAZ del editor.
#
# Prepara un proyecto en disco, lo abre con el editor y verifica lo que solo se
# puede ver corriendo la aplicacion:
#
#   1. Que el editor reabre el proyecto de la sesion anterior
#   2. Que el titulo muestra el nombre del proyecto
#   3. Que el explorador muestra la LISTA del proyecto, no la carpeta
#   4. Que F7 compila el ARCHIVO PRINCIPAL aunque se este mirando otra pestana
#
# El punto 4 es el que justifica todo: sin el, mirar un .inc y apretar F7
# ensambla el .inc.
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

    // Todo el texto visible de la ventana, para buscar en el arbol y la barra.
    public static List<string> TextosDe(IntPtr raiz) {
        var r = new List<string>();
        Recorrer(raiz, r, 0);
        return r;
    }
    static void Recorrer(IntPtr h, List<string> acc, int nivel) {
        if (nivel > 6) return;
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
"@

$raiz   = "C:\Users\Fabian\NASM"
# Editor aislado: no usa la configuracion ni los proyectos de Fabian.
. "$PSScriptRoot\..\editor_aislado.ps1"
$exe    = PrepararEditorAislado
$banco  = "$PSScriptRoot\banco_ui"
$fallas = 0

function Bien($t) { Write-Host "  OK   $t" -ForegroundColor Green }
function Mal($t)  { Write-Host "  MAL  $t" -ForegroundColor Red; $script:fallas++ }

if (-not (Test-Path $exe)) { throw "No existe $exe. Compila primero." }
if (Test-Path $banco) { Remove-Item $banco -Recurse -Force }
New-Item -ItemType Directory -Path $banco | Out-Null

# ---------------------------------------------------------------------------
Write-Host ""
Write-Host "=== Preparando el proyecto en disco ===" -ForegroundColor Cyan

# El principal ensambla; el auxiliar NO es un programa (no tiene punto de
# entrada). Si F7 compilara la pestana activa en vez del principal, NASM y
# GoLink fallarian: eso es lo que detecta la prueba.
Set-Content "$banco\principal.asm" @'
default rel
extern ExitProcess
%include "auxiliar.inc"
section .text
global main
main:
    sub rsp, 40
    mov ecx, CODIGO_SALIDA
    call ExitProcess
'@ -Encoding ASCII

Set-Content "$banco\auxiliar.inc" @'
; Esto NO es un programa: es un include. Ensamblarlo solo no produce un .exe.
%define CODIGO_SALIDA 0
'@ -Encoding ASCII

$json = @{
    Version = 1
    Proyecto = @{
        Nombre = "Banco UI"
        ArchivoPrincipal = "principal.asm"
        CarpetaSalida = "bin"
        Archivos = @("principal.asm", "auxiliar.inc")
        Targets = @()
        TargetActivo = 0
    }
} | ConvertTo-Json -Depth 6

$rutaProy = "$banco\Banco UI.asmproj"
Set-Content $rutaProy $json -Encoding UTF8

Write-Host "  proyecto: $rutaProy"

# Dejar el editor apuntando a ese proyecto para la proxima sesion.
#
# ⚠ ES EL settings.json DEL EDITOR AISLADO, no el de Fabian. Antes se
# reescribia el suyo y se restauraba al final; si la prueba cortaba en el
# medio, su editor quedaba apuntando al proyecto de prueba.
$settings = Join-Path (Split-Path $exe -Parent) "settings.json"

if (Test-Path $settings) {
    Copy-Item $settings "$settings.respaldo" -Force
    $cfg = Get-Content $settings -Raw | ConvertFrom-Json

    if ($cfg.Ui) {
        $cfg.Ui | Add-Member -NotePropertyName ProyectoAbierto -NotePropertyValue $rutaProy -Force
        # AlIniciar = 1 (UltimoProyecto): desde la ventana de inicio el editor
        # ya no reabre solo el proyecto anterior; este modo es el que lo hace.
        $cfg.Ui | Add-Member -NotePropertyName AlIniciar -NotePropertyValue 1 -Force
        # Que no reabra archivos de sesiones anteriores y ensucie la prueba.
        $cfg.Ui.OpenFiles = @()
        $cfg | ConvertTo-Json -Depth 12 | Set-Content $settings -Encoding UTF8
        Write-Host "  settings.json apuntado al proyecto (con respaldo)"
    }
}

# ---------------------------------------------------------------------------
Write-Host ""
Write-Host "=== 1. El editor reabre el proyecto ===" -ForegroundColor Cyan

$p = Start-Process $exe -PassThru
Start-Sleep -Seconds 5
$p.Refresh()

if ($p.HasExited) { throw "El editor se cerro solo (codigo $($p.ExitCode))" }

$ventanas = [W]::VentanasDe([uint32]$p.Id)
$principal = $ventanas | Where-Object { $_.Value -like "*Editor ASM*" } | Select-Object -First 1

if (-not $principal) {
    foreach ($v in $ventanas) { Write-Host "    ventana: '$($v.Value)'" }
    try { $p.Kill() } catch { }
    throw "No se encontro la ventana del editor"
}

$titulo = $principal.Value
Write-Host "  titulo: '$titulo'"

if ($titulo -like "*[Banco UI]*") {
    Bien "el titulo muestra el proyecto abierto"
} else {
    Mal "el titulo NO muestra el proyecto: '$titulo'"
}

# ---------------------------------------------------------------------------
Write-Host ""
Write-Host "=== 2. El explorador muestra la lista del proyecto ===" -ForegroundColor Cyan

$textos = [W]::TextosDe($principal.Key)

if ($textos -match "Proyecto: Banco UI") {
    Bien "la cabecera del explorador nombra al proyecto"
} else {
    Mal "la cabecera no nombra al proyecto"
}

# ⚠ LOS NODOS DE UN TreeView NO SON VENTANAS: no aparecen al enumerar las
# ventanas hijas, y buscarlos ahi da siempre "no esta" aunque se vean en
# pantalla. Se leen con los mensajes del control (TVM_*), que es lo unico que
# los expone.
Add-Type @"
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
public class Arbol {
    [DllImport("user32.dll")] static extern IntPtr SendMessage(IntPtr h, uint m, IntPtr w, IntPtr l);
    [DllImport("user32.dll")] static extern IntPtr FindWindowExW(IntPtr p, IntPtr t, string c, string w);
    [DllImport("user32.dll")] static extern uint GetWindowThreadProcessId(IntPtr h, out uint pid);
    [DllImport("kernel32.dll")] static extern IntPtr OpenProcess(uint a, bool i, uint pid);
    [DllImport("kernel32.dll")] static extern IntPtr VirtualAllocEx(IntPtr p, IntPtr a, IntPtr s, uint t, uint pr);
    [DllImport("kernel32.dll")] static extern bool VirtualFreeEx(IntPtr p, IntPtr a, IntPtr s, uint t);
    [DllImport("kernel32.dll")] static extern bool WriteProcessMemory(IntPtr p, IntPtr a, byte[] b, IntPtr s, out IntPtr w);
    [DllImport("kernel32.dll")] static extern bool ReadProcessMemory(IntPtr p, IntPtr a, byte[] b, IntPtr s, out IntPtr r);
    [DllImport("kernel32.dll")] static extern bool CloseHandle(IntPtr h);

    const uint TVM_GETNEXTITEM = 0x110A, TVM_GETITEMW = 0x113E;
    const uint TVGN_ROOT = 0, TVGN_NEXT = 1, TVGN_CHILD = 4;
    const uint TVIF_TEXT = 0x0001;

    // Recorre el arbol leyendo el texto de cada nodo desde el proceso del editor.
    public static List<string> Nodos(IntPtr ventana) {
        var salida = new List<string>();

        IntPtr tv = Buscar(ventana);
        if (tv == IntPtr.Zero) return salida;

        uint pid; GetWindowThreadProcessId(tv, out pid);
        IntPtr proc = OpenProcess(0x1F0FFF, false, pid);
        if (proc == IntPtr.Zero) return salida;

        // TVITEMW (64 bits) + el buffer del texto, en memoria del otro proceso.
        IntPtr remoto = VirtualAllocEx(proc, IntPtr.Zero, (IntPtr)1024, 0x3000, 4);
        if (remoto == IntPtr.Zero) { CloseHandle(proc); return salida; }

        IntPtr textoRemoto = (IntPtr)(remoto.ToInt64() + 256);

        try {
            IntPtr raiz = SendMessage(tv, TVM_GETNEXTITEM, (IntPtr)TVGN_ROOT, IntPtr.Zero);
            Recorrer(tv, proc, remoto, textoRemoto, raiz, salida, 0);
        } finally {
            VirtualFreeEx(proc, remoto, IntPtr.Zero, 0x8000);
            CloseHandle(proc);
        }

        return salida;
    }

    static void Recorrer(IntPtr tv, IntPtr proc, IntPtr item, IntPtr texto,
                         IntPtr nodo, List<string> acc, int nivel) {
        if (nivel > 6) return;

        while (nodo != IntPtr.Zero) {
            acc.Add(TextoDe(tv, proc, item, texto, nodo));

            IntPtr hijo = SendMessage(tv, TVM_GETNEXTITEM, (IntPtr)TVGN_CHILD, nodo);
            if (hijo != IntPtr.Zero) Recorrer(tv, proc, item, texto, hijo, acc, nivel + 1);

            nodo = SendMessage(tv, TVM_GETNEXTITEM, (IntPtr)TVGN_NEXT, nodo);
        }
    }

    static string TextoDe(IntPtr tv, IntPtr proc, IntPtr item, IntPtr texto, IntPtr nodo) {
        var b = new byte[56];
        BitConverter.GetBytes(TVIF_TEXT).CopyTo(b, 0);         // mask
        BitConverter.GetBytes(nodo.ToInt64()).CopyTo(b, 8);    // hItem
        BitConverter.GetBytes(texto.ToInt64()).CopyTo(b, 24);  // pszText
        BitConverter.GetBytes(128).CopyTo(b, 32);              // cchTextMax

        IntPtr n;
        WriteProcessMemory(proc, item, b, (IntPtr)b.Length, out n);
        SendMessage(tv, TVM_GETITEMW, IntPtr.Zero, item);

        var buf = new byte[256];
        ReadProcessMemory(proc, texto, buf, (IntPtr)buf.Length, out n);

        string s = Encoding.Unicode.GetString(buf);
        int fin = s.IndexOf('\0');
        return fin >= 0 ? s.Substring(0, fin) : s;
    }

    static IntPtr Buscar(IntPtr padre) {
        IntPtr h = FindWindowExW(padre, IntPtr.Zero, null, null);
        while (h != IntPtr.Zero) {
            var sb = new StringBuilder(256);
            GetClassName(h, sb, 256);
            if (sb.ToString().Contains("SysTreeView32")) return h;
            IntPtr dentro = Buscar(h);
            if (dentro != IntPtr.Zero) return dentro;
            h = FindWindowExW(padre, h, null, null);
        }
        return IntPtr.Zero;
    }

    [DllImport("user32.dll", CharSet=CharSet.Unicode, EntryPoint="GetClassNameW")]
    static extern int GetClassName(IntPtr h, StringBuilder s, int n);
}
"@

$nodos = [Arbol]::Nodos($principal.Key)

Write-Host "  nodos del arbol del proyecto:"
foreach ($n in $nodos) { Write-Host "    '$n'" }
Write-Host ""

if ($nodos -match "principal\.asm") {
    Bien "el explorador lista principal.asm"
} else {
    Mal "el explorador no lista principal.asm"
}

if ($nodos -match "auxiliar\.inc") {
    Bien "el explorador lista auxiliar.inc"
} else {
    Mal "el explorador no lista auxiliar.inc"
}

# El principal se marca con un triangulo para poder distinguirlo de un vistazo.
if ($nodos -match "►\s*principal\.asm") {
    Bien "principal.asm esta marcado como el que compila"
} else {
    Mal "principal.asm no esta marcado como principal"
}

# ---------------------------------------------------------------------------
Write-Host ""
Write-Host "=== 3. F7 compila el PRINCIPAL, no la pestana activa ===" -ForegroundColor Cyan

# Se abre el auxiliar.inc (que no es un programa) y se compila. Si el editor
# compilara la pestana activa, ensamblaria el .inc y no habria .exe.
# Teclas protegidas: solo salen con el editor en primer plano (ver
# proteccion_interfaz.ps1; el 23/09 este Ctrl+Shift+B pudo ir a otra ventana).
. "$PSScriptRoot\..\proteccion_interfaz.ps1"
if (-not (TraerAlFrente $p $principal.Key)) { AvisoProteccion "el editor no pudo pasar al primer plano" }

# Abrir auxiliar.inc por el dialogo de abrir (Ctrl+O) seria fragil; se lo
# pasa por linea de comandos a una segunda instancia? No: se usa el explorador.
# Mas simple y robusto: se compila con Ctrl+Shift+B y se mira que produjo.
[void](TeclasProtegidas $p "^+b" 6000)

$objEsperado = "$banco\bin\principal.obj"
$exeEsperado = "$banco\bin\principal.exe"

if (Test-Path $objEsperado) {
    Bien "compilo el principal y dejo el .obj en bin\"
} else {
    Mal "no aparecio $objEsperado"
}

if (Test-Path $exeEsperado) {
    Bien "enlazo y dejo el .exe en bin\"
} else {
    Mal "no aparecio $exeEsperado"
}

# No tiene que haber ensamblado el .inc ni ensuciado la carpeta del fuente.
if (Test-Path "$banco\auxiliar.obj") {
    Mal "ensamblo el .inc: esta compilando la pestana activa"
} else {
    Bien "no ensamblo el .inc"
}

if (Test-Path "$banco\principal.obj") {
    Mal "dejo el .obj al lado del fuente, ignorando la carpeta de salida"
} else {
    Bien "respeto la carpeta de salida del proyecto"
}

# ---------------------------------------------------------------------------
Write-Host ""
Write-Host "=== Captura de la ventana ===" -ForegroundColor Cyan

$rc = New-Object W+RECT
[void][W]::GetWindowRect($principal.Key, [ref]$rc)
$ancho = $rc.Right - $rc.Left
$alto  = $rc.Bottom - $rc.Top

if ($ancho -gt 0 -and $alto -gt 0) {
    $bmp = New-Object System.Drawing.Bitmap $ancho, $alto
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.CopyFromScreen($rc.Left, $rc.Top, 0, 0, $bmp.Size)
    $cap = "$PSScriptRoot\editor_con_proyecto.png"
    $bmp.Save($cap, [System.Drawing.Imaging.ImageFormat]::Png)
    $g.Dispose(); $bmp.Dispose()
    Write-Host "  $cap"
}

Write-Host ""
Write-Host "=== Cerrando y restaurando settings.json ===" -ForegroundColor Cyan

try { $p.Kill() } catch { }
Start-Sleep -Milliseconds 800

if (Test-Path "$settings.respaldo") {
    Move-Item "$settings.respaldo" $settings -Force
    Write-Host "  settings.json restaurado"
}

Write-Host ""

if ($fallas -eq 0) {
    Write-Host "=== TODO BIEN: el editor abre proyectos y compila el principal ===" -ForegroundColor Green
    exit 0
}

Write-Host "=== $fallas FALLAS ===" -ForegroundColor Red
exit 1
