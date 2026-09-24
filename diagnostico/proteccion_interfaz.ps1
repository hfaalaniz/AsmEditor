# ============================================================================
# Proteccion para los diagnosticos que manejan la interfaz con teclas y clics.
#
# Se usa con dot-sourcing:
#
#     . "$PSScriptRoot\proteccion_interfaz.ps1"          (o ..\ desde una subcarpeta)
#     if (-not (TraerAlFrente $p $h)) { ... cortar sin tocar nada ... }
#     TeclasProtegidas $p "^n"
#     ClicProtegido $p $x $y
#
# ⚠ POR QUE EXISTE. SendKeys y mouse_event actuan sobre LO QUE ESTE en primer
# plano o bajo el cursor, sea o no el editor. El 23/09 las pruebas corrieron
# con Chrome, Paint, la Calculadora y Configuracion abiertos: el editor no
# llego al frente, las teclas (Ctrl+W, Ctrl+D, Ctrl+Shift+B...) pudieron ir a
# otra ventana, y una prueba le hizo clic a Chrome. Con esto:
#
#   - una TECLA solo sale si la ventana en primer plano es del proceso del
#     editor (la principal o un dialogo suyo);
#   - un CLIC solo se hace si la ventana visible en ese punto es del editor;
#   - si no, NO se toca nada: se avisa en amarillo y la funcion devuelve
#     $false. La verificacion siguiente del script falla sola.
#
# Es para que lo lea yo, no forma parte de la interfaz.
# ============================================================================

Add-Type -AssemblyName System.Windows.Forms

if (-not ("ProteccionUI" -as [type])) {
Add-Type @"
using System;
using System.Runtime.InteropServices;
using System.Text;
public static class ProteccionUI {
    public struct POINT { public int X, Y; }
    [DllImport("user32.dll")] public static extern IntPtr GetForegroundWindow();
    [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr h);
    [DllImport("user32.dll")] public static extern bool ShowWindow(IntPtr h, int cmd);
    [DllImport("user32.dll")] static extern IntPtr WindowFromPoint(POINT p);
    [DllImport("user32.dll")] static extern IntPtr GetAncestor(IntPtr h, uint f);
    [DllImport("user32.dll")] static extern uint GetWindowThreadProcessId(IntPtr h, out uint pid);
    [DllImport("user32.dll", CharSet=CharSet.Unicode)] static extern int GetWindowText(IntPtr h, StringBuilder s, int n);
    [DllImport("user32.dll")] static extern bool SetCursorPos(int x, int y);
    [DllImport("user32.dll")] static extern void mouse_event(uint f, uint x, uint y, uint d, IntPtr e);
    [DllImport("user32.dll")] static extern void keybd_event(byte vk, byte scan, uint f, IntPtr e);

    public static uint ProcesoDe(IntPtr h) { uint pid; GetWindowThreadProcessId(h, out pid); return pid; }

    // ⚠ SOLO EL NOMBRE DEL PROGRAMA, NUNCA EL TITULO DE LA VENTANA. Los avisos
    // quedan en los registros de las pruebas, y el titulo de una ventana ajena
    // puede mostrar lo que Fabian tiene abierto (una pagina, un documento). El
    // 23/09 un aviso anoto el titulo de una pestana de su navegador.
    public static string Programa(IntPtr h) {
        try { return System.Diagnostics.Process.GetProcessById((int)ProcesoDe(h)).ProcessName; }
        catch { return "(desconocido)"; }
    }

    public static IntPtr RaizEn(int x, int y) { POINT p; p.X = x; p.Y = y; return GetAncestor(WindowFromPoint(p), 2); }

    public static void Clic(int x, int y) {
        SetCursorPos(x, y); System.Threading.Thread.Sleep(130);
        mouse_event(0x02, 0, 0, 0, IntPtr.Zero); System.Threading.Thread.Sleep(60);
        mouse_event(0x04, 0, 0, 0, IntPtr.Zero); System.Threading.Thread.Sleep(250);
    }

    // Tecla MANTENIDA: varias pulsaciones y UNA sola liberacion.
    public static void Mantener(byte vk, int repeticiones) {
        for (int i = 0; i < repeticiones; i++) { keybd_event(vk, 0, 0x1, IntPtr.Zero); System.Threading.Thread.Sleep(50); }
        keybd_event(vk, 0, 0x1 | 0x2, IntPtr.Zero);
        System.Threading.Thread.Sleep(300);
    }
}
"@
}

# El primer plano es de una ventana del proceso (principal o dialogo suyo).
function EditorAlFrente($proceso) {
    return ([ProteccionUI]::ProcesoDe([ProteccionUI]::GetForegroundWindow()) -eq [uint32]$proceso.Id)
}

# Trae la ventana al frente. Si Windows no lo deja (otra ventana tiene el
# primer plano), minimizar y restaurar la trae. Devuelve si lo logro.
function TraerAlFrente($proceso, $ventana) {
    [void][ProteccionUI]::SetForegroundWindow($ventana); Start-Sleep -Milliseconds 400
    if (EditorAlFrente $proceso) { return $true }

    [void][ProteccionUI]::ShowWindow($ventana, 6); Start-Sleep -Milliseconds 500
    [void][ProteccionUI]::ShowWindow($ventana, 9); Start-Sleep -Milliseconds 800
    [void][ProteccionUI]::SetForegroundWindow($ventana); Start-Sleep -Milliseconds 400
    return (EditorAlFrente $proceso)
}

function AvisoProteccion($t) { Write-Host "  PROTECCION  $t" -ForegroundColor Yellow }

# Manda teclas SOLO con el editor en primer plano.
function TeclasProtegidas($proceso, $teclas, $esperaMs = 700) {
    if (-not (EditorAlFrente $proceso)) {
        $f = [ProteccionUI]::GetForegroundWindow()
        AvisoProteccion "en primer plano esta otro programa ($([ProteccionUI]::Programa($f))), no el editor: NO se manda '$teclas'"
        return $false
    }
    [System.Windows.Forms.SendKeys]::SendWait($teclas)
    Start-Sleep -Milliseconds $esperaMs
    return $true
}

# Hace clic SOLO si la ventana visible en el punto es del editor.
function ClicProtegido($proceso, $x, $y) {
    $raiz = [ProteccionUI]::RaizEn($x, $y)
    if ([ProteccionUI]::ProcesoDe($raiz) -ne [uint32]$proceso.Id) {
        AvisoProteccion "el punto ($x,$y) esta tapado por otro programa ($([ProteccionUI]::Programa($raiz))): NO se hace clic"
        return $false
    }
    [ProteccionUI]::Clic($x, $y)
    return $true
}

# Mantiene una tecla SOLO con el editor en primer plano.
function MantenerProtegido($proceso, [byte]$vk, $repeticiones) {
    if (-not (EditorAlFrente $proceso)) {
        AvisoProteccion "el editor no esta en primer plano: NO se mantiene la tecla $vk"
        return $false
    }
    [ProteccionUI]::Mantener($vk, $repeticiones)
    return $true
}
