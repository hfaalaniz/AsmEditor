# ============================================================================
# Prueba del ACOPLE por la interfaz (PLAN_IDE.md, Etapa 3: 3a zonas y 3b
# pestanas por zona).
#
#   A. Disposicion: el explorador a la DERECHA; la lista de errores y la
#      salida ABAJO, como dos pestanas de la misma zona, con la lista activa.
#   B. Pestanas: clic en "Salida" la pasa al frente (su titulo arriba).
#   C. Ocultar y mostrar: X del explorador lo oculta (el centro se agranda);
#      Ctrl+B lo trae de vuelta, a la derecha.
#   D. Una zona con un solo panel no muestra pestanas: X de la salida deja la
#      lista sola; Ver > Salida (desde el buscador) la devuelve a su pestana.
#   E. Divisor: arrastrar el borde del explorador cambia su ancho.
#   F. Auto-ocultar (3c) el explorador: la chincheta lo pasa a una pestana
#      vertical en el borde derecho (y el foco sigue en el IDE); el raton
#      encima lo despliega y al irse se pliega; pasar rapido no despliega; el
#      clic lo despliega con el foco y se queda hasta que el foco se va;
#      Ctrl+B lo despliega y lo pliega (no lo cierra); la chincheta lo fija.
#   G. Auto-ocultar abajo: la lista de errores pasa a una pestana horizontal
#      en el borde de abajo, se despliega con clic y se fija con la chincheta.
#   H. Flotar (3d) el explorador: doble clic en el titulo lo pasa a su ventana
#      (sin titulo nativo, sin chincheta, con el foco); Ctrl+B desde ella
#      (atajo reenviado) devuelve el foco al editor y otra vez la trae; la X
#      y Alt+F4 la ocultan y Ctrl+B la trae donde estaba; arrastrar su titulo
#      la mueve y se recuerda; doble clic la acopla; arrastrar el titulo
#      acoplado la saca bajo el raton; "Acoplar" del menu la devuelve; y
#      cerrar el editor con una flotante: cancelar no pierde el panel, salir
#      termina limpio.
#
# Editor AISLADO y teclas/clics PROTEGIDOS. Solo se leen textos de ventanas
# DEL EDITOR (AC.Titulo lo exige). Guarda acople_*.png para mirarlas yo.
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
public static class AC {
    public struct RECT { public int Left, Top, Right, Bottom; }
    public struct POINT { public int X, Y; }
    delegate bool EnumProc(IntPtr h, IntPtr p);
    [DllImport("user32.dll")] static extern bool EnumWindows(EnumProc cb, IntPtr p);
    [DllImport("user32.dll")] static extern bool EnumChildWindows(IntPtr h, EnumProc cb, IntPtr p);
    [DllImport("user32.dll")] static extern uint GetWindowThreadProcessId(IntPtr h, out uint pid);
    [DllImport("user32.dll")] public static extern bool IsWindowVisible(IntPtr h);
    [DllImport("user32.dll", CharSet=CharSet.Unicode)] static extern int GetClassNameW(IntPtr h, StringBuilder s, int n);
    [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr h, out RECT r);
    [DllImport("user32.dll")] static extern bool PrintWindow(IntPtr h, IntPtr hdc, uint f);
    [DllImport("user32.dll", CharSet=CharSet.Unicode, EntryPoint="SendMessageTimeoutW")]
    static extern IntPtr SendMessageTimeoutTexto(IntPtr h, int m, IntPtr w, StringBuilder l, uint f, uint ms, out IntPtr r);
    [DllImport("user32.dll")] static extern bool SetCursorPos(int x, int y);
    [DllImport("user32.dll")] static extern void mouse_event(uint f, uint x, uint y, uint d, IntPtr e);
    [DllImport("user32.dll")] static extern IntPtr WindowFromPoint(POINT p);
    [DllImport("user32.dll")] static extern IntPtr GetAncestor(IntPtr h, uint f);
    [DllImport("user32.dll")] static extern IntPtr GetForegroundWindow();

    static uint Pid(IntPtr h) { uint p; GetWindowThreadProcessId(h, out p); return p; }

    public static List<IntPtr> Ventanas(uint pid) {
        var r = new List<IntPtr>();
        EnumWindows(delegate(IntPtr h, IntPtr p) {
            if (Pid(h) == pid && IsWindowVisible(h)) r.Add(h);
            return true;
        }, IntPtr.Zero);
        return r;
    }

    // SOLO de ventanas del proceso indicado, con WM_GETTEXT (entre procesos,
    // GetWindowText no le pregunta al control).
    public static string Titulo(IntPtr h, uint pid) {
        if (Pid(h) != pid) return "";
        var sb = new StringBuilder(1024); IntPtr r;
        SendMessageTimeoutTexto(h, 0x000D, (IntPtr)1024, sb, 0x2, 1000, out r);
        return sb.ToString();
    }

    public static string Clase(IntPtr h) { var sb = new StringBuilder(256); GetClassNameW(h, sb, 256); return sb.ToString(); }

    // Etiquetas visibles con ese texto (vacio = cualquiera: el null de
    // PowerShell llega como "").
    public static List<IntPtr> Etiquetas(IntPtr raiz, string texto) {
        var r = new List<IntPtr>(); uint pid = Pid(raiz);
        EnumChildWindows(raiz, delegate(IntPtr h, IntPtr p) {
            if (!IsWindowVisible(h)) return true;
            if (!Clase(h).ToUpperInvariant().Contains("STATIC")) return true;
            if (!string.IsNullOrEmpty(texto) && Titulo(h, pid) != texto) return true;
            r.Add(h); return true;
        }, IntPtr.Zero);
        return r;
    }

    public static List<IntPtr> Botones(IntPtr raiz) {
        var r = new List<IntPtr>();
        EnumChildWindows(raiz, delegate(IntPtr h, IntPtr p) {
            if (IsWindowVisible(h) && Clase(h).ToUpperInvariant().Contains("BUTTON")) r.Add(h);
            return true;
        }, IntPtr.Zero);
        return r;
    }

    public static RECT Rect(IntPtr h) { RECT r; GetWindowRect(h, out r); return r; }

    [DllImport("user32.dll")] public static extern bool IsWindowEnabled(IntPtr h);
    [DllImport("user32.dll")] public static extern bool IsWindow(IntPtr h);

    [StructLayout(LayoutKind.Sequential)]
    struct GUITHREADINFO {
        public int cbSize, flags;
        public IntPtr hwndActive, hwndFocus, hwndCapture, hwndMenuOwner, hwndMoveSize, hwndCaret;
        public RECT rcCaret;
    }
    [DllImport("user32.dll")] static extern bool GetGUIThreadInfo(uint hilo, ref GUITHREADINFO info);

    // Estado del hilo de la ventana: si esta en el bucle de mover/redimensionar
    // (GUI_INMOVESIZE) y quien tiene la captura del raton.
    public static string EstadoHilo(IntPtr ventana) {
        uint pid; uint hilo = GetWindowThreadProcessId(ventana, out pid);
        var i = new GUITHREADINFO(); i.cbSize = Marshal.SizeOf(typeof(GUITHREADINFO));
        if (!GetGUIThreadInfo(hilo, ref i)) return "(sin datos)";
        return "moviendo=" + ((i.flags & 0x2) != 0) + " captura=" + (i.hwndCapture == IntPtr.Zero ? "nadie" : Clase(i.hwndCapture).Replace("WindowsForms10.", "").Split('.')[0]);
    }
    [DllImport("user32.dll")] static extern bool PostMessage(IntPtr h, int m, IntPtr w, IntPtr l);

    // Doble clic SOLO si el punto es de una ventana del proceso.
    public static bool DobleClic(uint pid, int x, int y) {
        POINT p; p.X = x; p.Y = y;
        if (Pid(GetAncestor(WindowFromPoint(p), 2)) != pid) return false;
        SetCursorPos(x, y); System.Threading.Thread.Sleep(120);
        mouse_event(0x02, 0, 0, 0, IntPtr.Zero); mouse_event(0x04, 0, 0, 0, IntPtr.Zero);
        System.Threading.Thread.Sleep(60);
        mouse_event(0x02, 0, 0, 0, IntPtr.Zero); mouse_event(0x04, 0, 0, 0, IntPtr.Zero);
        System.Threading.Thread.Sleep(300);
        return true;
    }

    // Un control hijo visible con ese texto (cualquier clase), SOLO del proceso.
    public static IntPtr Hijo(IntPtr raiz, string texto) {
        IntPtr hallado = IntPtr.Zero; uint pid = Pid(raiz);
        EnumChildWindows(raiz, delegate(IntPtr h, IntPtr p) {
            if (IsWindowVisible(h) && Titulo(h, pid) == texto) { hallado = h; return false; }
            return true;
        }, IntPtr.Zero);
        return hallado;
    }

    // Pide cerrar (como la X) SOLO una ventana del proceso.
    public static bool PedirCierre(uint pid, IntPtr h) {
        if (Pid(h) != pid) return false;
        return PostMessage(h, 0x0010, IntPtr.Zero, IntPtr.Zero);
    }

    // Mueve el raton (sin clic) SOLO si el punto es de una ventana del
    // proceso. En dos pasos, para que el control reciba el movimiento.
    public static bool Mover(uint pid, int x, int y) {
        POINT p; p.X = x; p.Y = y;
        if (Pid(GetAncestor(WindowFromPoint(p), 2)) != pid) return false;
        SetCursorPos(x - 3, y); System.Threading.Thread.Sleep(60);
        SetCursorPos(x, y); System.Threading.Thread.Sleep(60);
        return true;
    }

    [DllImport("user32.dll")] static extern IntPtr GetFocus();
    [DllImport("user32.dll")] static extern bool AttachThreadInput(uint a, uint b, bool unir);
    [DllImport("kernel32.dll")] static extern uint GetCurrentThreadId();

    // Que control tiene el foco en la ventana (del editor), para rastrear.
    public static string Foco(IntPtr ventana) {
        uint pid; uint hilo = GetWindowThreadProcessId(ventana, out pid);
        uint mio = GetCurrentThreadId();
        AttachThreadInput(mio, hilo, true);
        IntPtr f = GetFocus();
        AttachThreadInput(mio, hilo, false);
        if (f == IntPtr.Zero) return "(ninguno)";
        // La cadena de padres, hasta la raiz: si no llega a la ventana del IDE,
        // el control quedo fuera del formulario.
        var cadena = new StringBuilder();
        IntPtr h = f; int n = 0;
        while (h != IntPtr.Zero && n++ < 12) {
            cadena.Append(Clase(h).Replace("WindowsForms10.", "").Split('.')[0]);
            cadena.Append(IsWindowVisible(h) ? "" : "(oculto)");
            cadena.Append(" < ");
            h = GetParent(h);
        }
        return cadena.ToString() + (GetAncestor(f, 2) == ventana ? " [dentro del IDE]" : " [FUERA del IDE]");
    }

    [DllImport("user32.dll")] static extern IntPtr GetParent(IntPtr h);

    public static int Visibles(IntPtr raiz, string clase) {
        int n = 0;
        EnumChildWindows(raiz, delegate(IntPtr h, IntPtr p) {
            if (IsWindowVisible(h) && Clase(h).ToUpperInvariant().Contains(clase)) n++;
            return true;
        }, IntPtr.Zero);
        return n;
    }

    // Arrastre con el boton apretado, en pasos. SOLO si el punto de partida
    // es de una ventana del proceso y el primer plano tambien.
    public static bool Arrastrar(uint pid, int x1, int y1, int x2, int y2) {
        POINT p; p.X = x1; p.Y = y1;
        if (Pid(GetAncestor(WindowFromPoint(p), 2)) != pid) return false;
        if (Pid(GetForegroundWindow()) != pid) return false;
        SetCursorPos(x1, y1); System.Threading.Thread.Sleep(150);
        mouse_event(0x02, 0, 0, 0, IntPtr.Zero); System.Threading.Thread.Sleep(120);
        for (int i = 1; i <= 12; i++) {
            SetCursorPos(x1 + (x2 - x1) * i / 12, y1 + (y2 - y1) * i / 12);
            System.Threading.Thread.Sleep(30);
        }
        System.Threading.Thread.Sleep(120);
        mouse_event(0x04, 0, 0, 0, IntPtr.Zero); System.Threading.Thread.Sleep(400);
        return true;
    }

    public static void Capturar(IntPtr h, string ruta) {
        RECT r; GetWindowRect(h, out r);
        using (var bmp = new Bitmap(r.Right - r.Left, r.Bottom - r.Top)) {
            using (var g = Graphics.FromImage(bmp)) { var dc = g.GetHdc(); PrintWindow(h, dc, 2); g.ReleaseHdc(dc); }
            bmp.Save(ruta, System.Drawing.Imaging.ImageFormat.Png);
        }
    }
}
"@

$fallas = 0
function Bien($t) { Write-Host "  OK   $t" -ForegroundColor Green }
function Mal($t)  { Write-Host "  MAL  $t" -ForegroundColor Red; $script:fallas++ }
function Titulo($t) { Write-Host ""; Write-Host "=== $t ===" -ForegroundColor Cyan }

$finTituloIDE = "Editor ASM (NASM + GoLink)"

function Buscar($p, [scriptblock]$filtro) {
    foreach ($h in [AC]::Ventanas([uint32]$p.Id)) { if (& $filtro $h ([AC]::Titulo($h, [uint32]$p.Id))) { return $h } }
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

# Las etiquetas visibles con ese texto, de arriba abajo.
function Etiquetas($ide, $texto) { return @([AC]::Etiquetas($ide, $texto) | Sort-Object { ([AC]::Rect($_)).Top }) }

function Centro($h) { $r = [AC]::Rect($h); return @([int](($r.Left + $r.Right) / 2), [int](($r.Top + $r.Bottom) / 2)) }

function Clic($p, $h) { $c = Centro $h; return (ClicProtegido $p $c[0] $c[1]) }

# La X de un panel: el boton visible MAS A LA DERECHA en la fila de su
# titulo, pegado a la etiqueta (a menos de 80 px de su borde derecho).
function CerrarPanel($p, $ide, $etiquetaTitulo) {
    $rt = [AC]::Rect($etiquetaTitulo)
    $y = [int](($rt.Top + $rt.Bottom) / 2)
    $x = @([AC]::Botones($ide) | ForEach-Object { [AC]::Rect($_) } |
        Where-Object { $_.Top -le $y -and $_.Bottom -ge $y -and $_.Left -ge $rt.Right -and $_.Left -le $rt.Right + 80 } |
        Sort-Object Left | Select-Object -Last 1)
    if ($x.Count -eq 0) { Mal "no encuentro la X del panel"; return $false }
    return (ClicProtegido $p ([int](($x[0].Left + $x[0].Right) / 2)) $y)
}

# La chincheta de un panel: en la fila de su titulo, la anteultima de
# menu, chincheta, X.
function Chincheta($p, $ide, $etiquetaTitulo) {
    $rt = [AC]::Rect($etiquetaTitulo)
    $y = [int](($rt.Top + $rt.Bottom) / 2)
    $b = @([AC]::Botones($ide) | ForEach-Object { [AC]::Rect($_) } |
        Where-Object { $_.Top -le $y -and $_.Bottom -ge $y -and $_.Left -ge $rt.Right -and $_.Left -le $rt.Right + 80 } |
        Sort-Object Left)
    if ($b.Count -lt 3) { Mal "no encuentro la chincheta (botones en la fila: $($b.Count))"; return $false }
    return (ClicProtegido $p ([int](($b[-2].Left + $b[-2].Right) / 2)) $y)
}

# La pestana de un auto-oculto en un borde lateral: etiqueta angosta y alta.
function PestanaLateral($ide, $texto) {
    foreach ($h in (Etiquetas $ide $texto)) {
        $r = [AC]::Rect($h)
        if (($r.Right - $r.Left) -le 30 -and ($r.Bottom - $r.Top) -gt ($r.Right - $r.Left)) { return $h }
    }
    return $null
}

function MoverRaton($p, $x, $y) {
    if ([AC]::Mover([uint32]$p.Id, [int]$x, [int]$y)) { return $true }
    AvisoProteccion "el punto ($x,$y) no es del editor: NO se mueve el raton"
    return $false
}

function FocoEnElIDE($ide) { return ([AC]::Foco($ide)).EndsWith("[dentro del IDE]") }

# La ventana flotante de un panel: ventana del proceso, con el titulo del
# panel, que no es el IDE. Zero si no esta.
function Flotante($p, $ide, $titulo) {
    return (Buscar $p { param($h, $t) $t -eq $titulo -and $h -ne $ide })
}

function Doble($p, $h) {
    $c = Centro $h
    if ([AC]::DobleClic([uint32]$p.Id, $c[0], $c[1])) { return $true }
    AvisoProteccion "el punto ($($c[0]),$($c[1])) no es del editor: NO se hace doble clic"
    return $false
}

# El boton del menu (v) de un panel: el primero de la fila de su titulo.
function BotonesDelTitulo($ventana, $etiquetaTitulo) {
    $rt = [AC]::Rect($etiquetaTitulo)
    $y = [int](($rt.Top + $rt.Bottom) / 2)
    return @([AC]::Botones($ventana) | ForEach-Object { [AC]::Rect($_) } |
        Where-Object { $_.Top -le $y -and $_.Bottom -ge $y -and $_.Left -ge $rt.Right -and $_.Left -le $rt.Right + 80 } |
        Sort-Object Left)
}

# Elige la PRIMERA opcion visible del menu (v) de un panel.
function PrimeraDelMenu($p, $ventana, $etiquetaTitulo) {
    $b = BotonesDelTitulo $ventana $etiquetaTitulo
    if ($b.Count -lt 2) { Mal "no encuentro el menu del panel"; return $false }
    if (-not (ClicProtegido $p ([int](($b[0].Left + $b[0].Right) / 2)) ([int](($b[0].Top + $b[0].Bottom) / 2)))) { return $false }
    Start-Sleep -Milliseconds 400
    return (TeclasProtegidas $p "{DOWN}{ENTER}" 800)
}

# ---------------------------------------------------------------------------
Titulo "Preparando"
$exe = PrepararEditorAislado
Write-Host "  editor aislado listo"

$p = Start-Process $exe -PassThru
try {
    $ide = Esperar $p { param($h, $t) $t.EndsWith($finTituloIDE) }
    if ($ide -eq [IntPtr]::Zero) { throw "no aparecio el IDE" }
    Start-Sleep -Milliseconds 1500
    if (-not (TraerAlFrente $p $ide)) { throw "no se pudo traer el IDE al frente" }
    $rv = [AC]::Rect($ide)
    $medio = ($rv.Left + $rv.Right) / 2
    [AC]::Capturar($ide, (Join-Path $PSScriptRoot "acople_inicio.png"))

    # -----------------------------------------------------------------------
    Titulo "A. Disposicion"

    $exp = Etiquetas $ide "Explorador"
    if ($exp.Count -eq 1 -and (Centro $exp[0])[0] -gt $medio) { Bien "el explorador esta a la derecha" } else { Mal "explorador: $($exp.Count) etiqueta(s), no a la derecha" }

    $err = Etiquetas $ide "Lista de errores"
    $sal = Etiquetas $ide "Salida"
    # La lista activa: su titulo arriba y su pestana abajo; la salida, solo pestana.
    if ($err.Count -eq 2 -and $sal.Count -eq 1) { Bien "abajo: la lista (titulo + pestana) y la salida (pestana)" } else { Mal "abajo: lista $($err.Count), salida $($sal.Count) etiquetas" }
    if ($err.Count -ge 1 -and (Centro $err[0])[1] -gt ($rv.Top + ($rv.Bottom - $rv.Top) / 2)) { Bien "la zona de errores esta abajo" } else { Mal "la zona de errores no esta abajo" }
    if ($sal.Count -eq 1 -and $err.Count -eq 2 -and [Math]::Abs((Centro $sal[0])[1] - (Centro $err[1])[1]) -le 2) { Bien "las dos pestanas estan en la misma fila" } else { Mal "las pestanas no estan alineadas" }
    if ($exp.Count -eq 1 -and $err.Count -ge 1 -and ([AC]::Rect($err[0])).Right -lt ([AC]::Rect($exp[0])).Left) { Bien "la zona de abajo queda ENTRE las laterales (no pasa bajo el explorador)" } else { Mal "la zona de abajo pasa bajo el explorador" }

    # -----------------------------------------------------------------------
    Titulo "B. Pestanas"

    if ($sal.Count -eq 1) {
        [void](Clic $p $sal[0])
        Start-Sleep -Milliseconds 500
        $err = Etiquetas $ide "Lista de errores"
        $sal = Etiquetas $ide "Salida"
        if ($sal.Count -eq 2 -and $err.Count -eq 1) { Bien "clic en 'Salida': la salida al frente (titulo + pestana)" } else { Mal "tras clic en Salida: lista $($err.Count), salida $($sal.Count)" }
        [AC]::Capturar($ide, (Join-Path $PSScriptRoot "acople_salida.png"))
        if ($err.Count -ge 1) { [void](Clic $p $err[0]); Start-Sleep -Milliseconds 400 }
        if ((Etiquetas $ide "Lista de errores").Count -eq 2) { Bien "clic en 'Lista de errores': vuelve al frente" } else { Mal "no volvio la lista de errores" }
    }

    # -----------------------------------------------------------------------
    Titulo "C. Ocultar y mostrar el explorador"

    $exp = Etiquetas $ide "Explorador"
    if ($exp.Count -eq 1) {
        Write-Host "    rastro foco antes de la X: $([AC]::Foco($ide))" -ForegroundColor DarkGray
        [void](CerrarPanel $p $ide $exp[0])
        Start-Sleep -Milliseconds 500
        Write-Host "    rastro foco despues de la X: $([AC]::Foco($ide))" -ForegroundColor DarkGray
        if ((Etiquetas $ide "Explorador").Count -eq 0) { Bien "la X oculto el explorador" } else { Mal "la X no oculto el explorador" }

        [void](TraerAlFrente $p $ide)
        Write-Host "    rastro foco antes de Ctrl+B: $([AC]::Foco($ide))" -ForegroundColor DarkGray
        [void](TeclasProtegidas $p "^b" 600)
        Write-Host "    rastro tras Ctrl+B: etiquetas 'Explorador' $((Etiquetas $ide 'Explorador').Count), arboles visibles $([AC]::Visibles($ide, 'TREEVIEW')), foco $([AC]::Foco($ide))" -ForegroundColor DarkGray
        $exp = Etiquetas $ide "Explorador"
        if ($exp.Count -eq 1 -and (Centro $exp[0])[0] -gt $medio) { Bien "Ctrl+B lo trajo de vuelta, a la derecha" } else { Mal "Ctrl+B no lo trajo de vuelta" }

        [void](TeclasProtegidas $p "^b" 600)
        if ((Etiquetas $ide "Explorador").Count -eq 0) { Bien "Ctrl+B otra vez lo oculta" } else { Mal "Ctrl+B no lo oculto" }
        [void](TeclasProtegidas $p "^b" 600)
    }

    # -----------------------------------------------------------------------
    Titulo "D. Una zona con un solo panel no muestra pestanas"

    $sal = Etiquetas $ide "Salida"
    if ($sal.Count -eq 1) {
        [void](Clic $p $sal[0]); Start-Sleep -Milliseconds 400
        $sal = Etiquetas $ide "Salida"
        if ($sal.Count -eq 2) {
            [void](CerrarPanel $p $ide $sal[0])
            Start-Sleep -Milliseconds 500
            $err = Etiquetas $ide "Lista de errores"
            if ((Etiquetas $ide "Salida").Count -eq 0 -and $err.Count -eq 1) { Bien "X de la salida: queda la lista sola, sin pestanas" } else { Mal "tras X de la salida: lista $($err.Count), salida $((Etiquetas $ide 'Salida').Count)" }
            [AC]::Capturar($ide, (Join-Path $PSScriptRoot "acople_sin_salida.png"))
        } else { Mal "no se pudo pasar la salida al frente" }

        [void](TraerAlFrente $p $ide)
        [void](TeclasProtegidas $p "^q" 500)
        [void](TeclasProtegidas $p "salida" 700)
        [void](TeclasProtegidas $p "{ENTER}" 700)
        $err = Etiquetas $ide "Lista de errores"
        $sal = Etiquetas $ide "Salida"
        if ($sal.Count -eq 2 -and $err.Count -eq 1) { Bien "Ver > Salida la devolvio, al frente y en su pestana" } else { Mal "tras Ver > Salida: lista $($err.Count), salida $($sal.Count)" }
    }

    # -----------------------------------------------------------------------
    Titulo "E. Divisor del explorador"

    $exp = Etiquetas $ide "Explorador"
    if ($exp.Count -eq 1) {
        $re = [AC]::Rect($exp[0])
        # El panel empieza 8 px antes de su etiqueta; el divisor (5 px) justo antes.
        $xDivisor = $re.Left - 8 - 3
        $yDivisor = $re.Top + 200
        $izqAntes = $re.Left
        [void](TraerAlFrente $p $ide)
        if ([AC]::Arrastrar([uint32]$p.Id, $xDivisor, $yDivisor, $xDivisor - 120, $yDivisor)) {
            $re2 = [AC]::Rect((Etiquetas $ide "Explorador")[0])
            $delta = $izqAntes - $re2.Left
            if ($delta -ge 100 -and $delta -le 140) { Bien "arrastrar el divisor 120 px agrando el explorador ($delta px)" } else { Mal "el explorador cambio $delta px (esperaba ~120)" }

            # El ancho queda en el modelo: ocultar y volver a mostrar lo respeta
            # (cada cambio recalcula las zonas desde el diseno).
            [void](TeclasProtegidas $p "^b" 500)
            [void](TeclasProtegidas $p "^b" 600)
            $e3 = Etiquetas $ide "Explorador"
            if ($e3.Count -eq 1 -and [Math]::Abs(([AC]::Rect($e3[0])).Left - $re2.Left) -le 2) { Bien "tras ocultar y mostrar, conserva el ancho nuevo" }
            else { Mal "tras ocultar y mostrar, el ancho volvio a otro valor" }
        } else { AvisoProteccion "el divisor no es del editor o el editor no esta al frente: no se arrastra"; Mal "no se pudo arrastrar el divisor" }
        [AC]::Capturar($ide, (Join-Path $PSScriptRoot "acople_final.png"))
    }

    # -----------------------------------------------------------------------
    Titulo "F. Auto-ocultar el explorador (chincheta)"

    # Un punto de los documentos, lejos de los paneles: para sacar el raton.
    $afueraX = [int]($rv.Left + ($rv.Right - $rv.Left) * 0.25)
    $afueraY = [int]($rv.Top + 200)

    $exp = Etiquetas $ide "Explorador"
    if ($exp.Count -eq 1) {
        [void](TraerAlFrente $p $ide)
        [void](Chincheta $p $ide $exp[0])
        Start-Sleep -Milliseconds 700
        $tab = PestanaLateral $ide "Explorador"
        $exp = Etiquetas $ide "Explorador"
        if ($tab -and $exp.Count -eq 1) { Bien "la chincheta lo paso a una pestana vertical" } else { Mal "tras la chincheta: $($exp.Count) etiqueta(s), pestana lateral: $([bool]$tab)" }
        if ($tab -and ([AC]::Rect($tab)).Right -ge $rv.Right - 40) { Bien "la pestana esta en el borde derecho" } else { Mal "la pestana no esta en el borde derecho" }
        Write-Host "    rastro foco tras la chincheta: $([AC]::Foco($ide))" -ForegroundColor DarkGray
        if (FocoEnElIDE $ide) { Bien "el foco sigue dentro del IDE (no quedo en la chincheta estacionada)" } else { Mal "el foco quedo FUERA del IDE" }
        [AC]::Capturar($ide, (Join-Path $PSScriptRoot "acople_autooculto.png"))

        if ($tab) {
            $ct = Centro $tab

            # Raton encima: se despliega (400 ms), sin foco.
            [void](MoverRaton $p $afueraX $afueraY)
            [void](MoverRaton $p $ct[0] $ct[1])
            Start-Sleep -Milliseconds 1000
            $exp = Etiquetas $ide "Explorador"
            $titulo = @($exp | Where-Object { $_ -ne $tab })
            # Pegado a su pestana: el titulo termina donde empiezan los botones
            # del panel (~80 px antes de su borde derecho), no lejos.
            $hueco = if ($titulo.Count -eq 1) { ([AC]::Rect($tab)).Left - ([AC]::Rect($titulo[0])).Right } else { -1 }
            if ($exp.Count -eq 2 -and $hueco -ge 0 -and $hueco -le 120) { Bien "el raton encima lo desplego, pegado a su pestana ($hueco px)" } else { Mal "raton encima: $($exp.Count) etiqueta(s), hueco con la pestana $hueco px" }
            [AC]::Capturar($ide, (Join-Path $PSScriptRoot "acople_desplegado.png"))

            # El raton se va: se pliega.
            [void](MoverRaton $p $afueraX $afueraY)
            Start-Sleep -Milliseconds 1000
            if ((Etiquetas $ide "Explorador").Count -eq 1) { Bien "al sacar el raton se plego" } else { Mal "al sacar el raton NO se plego" }

            # Pasar rapido: no se despliega. Se mira TODO el tramo, cada 50 ms:
            # si se desplegara (a los 400 ms) se volveria a plegar solo (a los
            # ~700), y mirar una vez al final no lo veia (romper_acople, 28/09).
            [void](MoverRaton $p $ct[0] $ct[1])
            Start-Sleep -Milliseconds 150
            [void](MoverRaton $p $afueraX $afueraY)
            $maximo = 0
            $reloj = [Diagnostics.Stopwatch]::StartNew()
            while ($reloj.ElapsedMilliseconds -lt 1200) {
                $n = (Etiquetas $ide "Explorador").Count
                if ($n -gt $maximo) { $maximo = $n }
                Start-Sleep -Milliseconds 50
            }
            if ($maximo -eq 1) { Bien "pasar rapido por la pestana no lo despliega" } else { Mal "pasar rapido lo desplego (llego a $maximo etiquetas)" }

            # Clic: se despliega con el foco y se queda aunque el raton se vaya.
            [void](Clic $p $tab)
            Start-Sleep -Milliseconds 600
            [void](MoverRaton $p $afueraX $afueraY)
            Start-Sleep -Milliseconds 1000
            Write-Host "    rastro foco tras el clic en la pestana: $([AC]::Foco($ide))" -ForegroundColor DarkGray
            if ((Etiquetas $ide "Explorador").Count -eq 2) { Bien "con clic se desplego y se queda con el raton afuera (tiene el foco)" } else { Mal "con clic no quedo desplegado" }

            # El foco se va a otro panel: se pliega.
            $sal = Etiquetas $ide "Salida"
            if ($sal.Count -ge 1) {
                $rs = [AC]::Rect($sal[0])
                # Cerca del borde izquierdo: el explorador desplegado tapa la derecha.
                [void](ClicProtegido $p ([int]($rs.Left + 40)) ([int]($rs.Bottom + 50)))
                Start-Sleep -Milliseconds 1000
                if ((Etiquetas $ide "Explorador").Count -eq 1) { Bien "clic en la salida: el foco se fue y se plego" } else { Mal "clic en la salida: NO se plego" }
            } else { Mal "no encuentro la salida para llevarle el foco" }

            # Ctrl+B: despliega (no cierra) y otra vez pliega.
            [void](TraerAlFrente $p $ide)
            [void](TeclasProtegidas $p "^b" 900)
            if ((Etiquetas $ide "Explorador").Count -eq 2) { Bien "Ctrl+B sobre el auto-oculto lo despliega (no lo cierra)" } else { Mal "Ctrl+B no lo desplego: $((Etiquetas $ide 'Explorador').Count) etiqueta(s)" }
            [void](TeclasProtegidas $p "^b" 900)
            Write-Host "    rastro foco tras plegar con Ctrl+B: $([AC]::Foco($ide))" -ForegroundColor DarkGray
            if ((Etiquetas $ide "Explorador").Count -eq 1 -and (PestanaLateral $ide "Explorador")) { Bien "Ctrl+B otra vez lo pliega y queda su pestana" } else { Mal "Ctrl+B otra vez no lo plego a su pestana" }
            if (FocoEnElIDE $ide) { Bien "plegado con el foco adentro: el foco vuelve al IDE" } else { Mal "plegado con el foco adentro: el foco quedo FUERA del IDE" }

            # Los atajos siguen vivos (la trampa de la 3a): Ctrl+B lo despliega otra vez.
            [void](TeclasProtegidas $p "^b" 900)
            $exp = Etiquetas $ide "Explorador"
            if ($exp.Count -eq 2) { Bien "los atajos siguen vivos tras plegar" } else { Mal "tras plegar, Ctrl+B ya no responde" }

            # La chincheta del desplegado lo fija de nuevo a la derecha.
            $titulo = @($exp | Where-Object { $_ -ne (PestanaLateral $ide "Explorador") })
            if ($titulo.Count -eq 1) {
                [void](Chincheta $p $ide $titulo[0])
                Start-Sleep -Milliseconds 700
                $exp = Etiquetas $ide "Explorador"
                if ($exp.Count -eq 1 -and -not (PestanaLateral $ide "Explorador") -and (Centro $exp[0])[0] -gt $medio) { Bien "la chincheta lo volvio a acoplar a la derecha, sin pestana" } else { Mal "tras fijar: $($exp.Count) etiqueta(s), pestana: $([bool](PestanaLateral $ide 'Explorador'))" }
            } else { Mal "no encuentro el titulo del explorador desplegado" }
        }
    } else { Mal "no esta el explorador para probar la chincheta" }

    # -----------------------------------------------------------------------
    Titulo "G. Auto-ocultar abajo (lista de errores)"

    $err = Etiquetas $ide "Lista de errores"
    if ($err.Count -eq 1) { [void](Clic $p $err[0]); Start-Sleep -Milliseconds 500 }
    $err = Etiquetas $ide "Lista de errores"
    if ($err.Count -eq 2) {
        [void](TraerAlFrente $p $ide)
        [void](Chincheta $p $ide $err[0])
        Start-Sleep -Milliseconds 700
        $err = Etiquetas $ide "Lista de errores"
        $sal = Etiquetas $ide "Salida"
        $re = if ($err.Count -eq 1) { [AC]::Rect($err[0]) } else { $null }
        if ($re -and ($re.Right - $re.Left) -gt ($re.Bottom - $re.Top) -and $sal.Count -eq 1 -and $re.Top -gt ([AC]::Rect($sal[0])).Bottom) {
            Bien "la lista paso a una pestana horizontal abajo; la salida quedo sola, sin pestanas"
        } else { Mal "tras la chincheta: lista $($err.Count), salida $($sal.Count)" }

        if ($err.Count -eq 1) {
            [void](Clic $p $err[0])
            Start-Sleep -Milliseconds 700
            $err = Etiquetas $ide "Lista de errores"
            if ($err.Count -eq 2) { Bien "clic en la pestana de abajo la desplego" } else { Mal "clic en la pestana de abajo: $($err.Count) etiqueta(s)" }
            [AC]::Capturar($ide, (Join-Path $PSScriptRoot "acople_abajo_desplegado.png"))

            if ($err.Count -eq 2) {
                [void](Chincheta $p $ide $err[0])
                Start-Sleep -Milliseconds 700
                $err = Etiquetas $ide "Lista de errores"
                $sal = Etiquetas $ide "Salida"
                if ($err.Count -eq 2 -and $sal.Count -eq 1) { Bien "fijada: vuelve abajo, activa y con sus pestanas" } else { Mal "tras fijar: lista $($err.Count), salida $($sal.Count)" }
            }
        }
        [AC]::Capturar($ide, (Join-Path $PSScriptRoot "acople_fin_3c.png"))
    } else { Mal "no se pudo poner la lista de errores al frente" }

    # -----------------------------------------------------------------------
    Titulo "H. Flotar el explorador"

    $exp = Etiquetas $ide "Explorador"
    if ($exp.Count -eq 1) {
        [void](TraerAlFrente $p $ide)

        # Doble clic en el titulo acoplado: a su ventana.
        [void](Doble $p $exp[0])
        $flot = Esperar $p { param($h, $t) $t -eq "Explorador" -and $h -ne $ide } 5
        if ($flot -ne [IntPtr]::Zero) { Bien "doble clic en el titulo: el explorador paso a su ventana" } else { Mal "doble clic en el titulo: no aparecio la ventana flotante" }
        if ((Etiquetas $ide "Explorador").Count -eq 0) { Bien "ya no esta en el IDE" } else { Mal "sigue en el IDE" }
    } else { $flot = [IntPtr]::Zero; Mal "no esta el explorador acoplado para flotarlo" }

    if ($flot -ne [IntPtr]::Zero) {
        $tituloFlot = @(Etiquetas $flot "Explorador")
        $rf = [AC]::Rect($flot)
        if ($tituloFlot.Count -eq 1 -and (([AC]::Rect($tituloFlot[0])).Top - $rf.Top) -le 10) { Bien "sin titulo nativo: la barra del panel esta arriba de todo" } else { Mal "la barra del panel no esta arriba (titulo nativo?)" }
        if ($tituloFlot.Count -eq 1 -and (BotonesDelTitulo $flot $tituloFlot[0]).Count -eq 2) { Bien "flotando no tiene chincheta (menu y X)" } else { Mal "botones en la barra flotante: $(if ($tituloFlot.Count -eq 1) { (BotonesDelTitulo $flot $tituloFlot[0]).Count } else { '?' })" }
        Write-Host "    rastro foco en la flotante: $([AC]::Foco($flot))" -ForegroundColor DarkGray
        if (([AC]::Foco($flot)).EndsWith("[dentro del IDE]")) { Bien "la flotante tiene el foco" } else { Mal "la flotante no tiene el foco" }
        [AC]::Capturar($flot, (Join-Path $PSScriptRoot "acople_flotante.png"))

        # Ctrl+B con el foco en la flotante: llega al menu (reenviado) y el foco vuelve al editor.
        [void](TeclasProtegidas $p "^b" 900)
        if ((FocoEnElIDE $ide) -and (Flotante $p $ide "Explorador") -ne [IntPtr]::Zero) { Bien "Ctrl+B desde la flotante (atajo reenviado): el foco vuelve al editor, la ventana queda" } else { Mal "Ctrl+B desde la flotante: foco en el IDE $(FocoEnElIDE $ide), flotante $((Flotante $p $ide 'Explorador') -ne [IntPtr]::Zero)" }
        [void](TeclasProtegidas $p "^b" 900)
        if ([ProteccionUI]::GetForegroundWindow() -eq $flot) { Bien "Ctrl+B otra vez: la flotante al frente" } else { Mal "Ctrl+B otra vez: la flotante no quedo al frente" }

        # La X la oculta; Ctrl+B la trae donde estaba.
        $antes = [AC]::Rect($flot)
        $hAntesX = $flot
        [void](CerrarPanel $p $flot (@(Etiquetas $flot "Explorador"))[0])
        Start-Sleep -Milliseconds 600
        Write-Host "    rastro tras la X: la ventana $hAntesX existe $([AC]::IsWindow($hAntesX)), visible $([AC]::IsWindowVisible($hAntesX))" -ForegroundColor DarkGray
        if ((Flotante $p $ide "Explorador") -eq [IntPtr]::Zero) { Bien "la X de la flotante la oculta" } else { Mal "la X no oculto la flotante" }
        if (FocoEnElIDE $ide) { Bien "tras la X, el foco esta en el editor" } else { Mal "tras la X, el foco quedo FUERA del editor" }
        [void](TraerAlFrente $p $ide)
        [void](TeclasProtegidas $p "^b" 900)
        $flot = Flotante $p $ide "Explorador"
        $r = if ($flot -ne [IntPtr]::Zero) { [AC]::Rect($flot) } else { $null }
        if ($r -and [Math]::Abs($r.Left - $antes.Left) -le 2 -and [Math]::Abs($r.Top - $antes.Top) -le 2) { Bien "Ctrl+B la trajo flotante, donde estaba" } else { Mal "Ctrl+B no la trajo donde estaba" }

        # Alt+F4 la oculta sin cerrar el editor. SOLO con la flotante al
        # frente: con la ventana principal al frente cerraria el editor.
        if ($flot -ne [IntPtr]::Zero -and [ProteccionUI]::GetForegroundWindow() -ne $flot) {
            Mal "la flotante no esta al frente: no se manda Alt+F4"
        } elseif ($flot -ne [IntPtr]::Zero) {
            [void](TeclasProtegidas $p "%{F4}" 900)
            if ((Flotante $p $ide "Explorador") -eq [IntPtr]::Zero -and -not $p.HasExited -and (Buscar $p { param($h, $t) $h -eq $ide }) -ne [IntPtr]::Zero) { Bien "Alt+F4 oculta la flotante (el editor sigue abierto)" } else { Mal "Alt+F4: flotante $((Flotante $p $ide 'Explorador') -ne [IntPtr]::Zero), editor cerrado $($p.HasExited)" }
            [void](TraerAlFrente $p $ide)
            [void](TeclasProtegidas $p "^b" 900)
            $flot = Flotante $p $ide "Explorador"
        }

        # Arrastrar su titulo la mueve, y el lugar se recuerda.
        # OJO: HACIA LA IZQUIERDA: la flotante nace donde estaba el explorador,
        # pegada al borde derecho; corrida a la derecha su X quedaba FUERA de
        # la pantalla y el clic (que Windows recorta al borde) no la cerraba.
        # Parecia un defecto del editor y era de la prueba (28/09).
        if ($flot -ne [IntPtr]::Zero) {
            $t = (@(Etiquetas $flot "Explorador"))[0]
            $c = Centro $t
            $antes = [AC]::Rect($flot)
            if ([AC]::Arrastrar([uint32]$p.Id, $c[0], $c[1], $c[0] - 120, $c[1] + 70)) {
                Write-Host "    rastro hilo tras arrastrar la flotante: $([AC]::EstadoHilo($flot))" -ForegroundColor DarkGray
                $r = [AC]::Rect($flot)
                if ([Math]::Abs(($r.Left - $antes.Left) + 120) -le 6 -and [Math]::Abs(($r.Top - $antes.Top) - 70) -le 6) { Bien "arrastrar su titulo la movio ($($r.Left - $antes.Left), +$($r.Top - $antes.Top))" } else { Mal "arrastrar su titulo: se movio ($($r.Left - $antes.Left), $($r.Top - $antes.Top)), esperaba (-120, 70)" }
                $hArrastrada = $flot
                $xOk = CerrarPanel $p $flot (@(Etiquetas $flot "Explorador"))[0]
                Start-Sleep -Milliseconds 500
                # Sin esto, si la X no llegaba, Ctrl+B traia al frente la MISMA
                # ventana y "vuelve donde se la dejo" pasaba sin probar nada
                # (romper_acople, 28/09: mismo handle antes y despues).
                Write-Host "    rastro X tras arrastrar: clic hecho $xOk, la ventana existe $([AC]::IsWindow($hArrastrada)), visible $([AC]::IsWindowVisible($hArrastrada)), $(if ([AC]::IsWindow($hArrastrada)) { [AC]::EstadoHilo($hArrastrada) }), foco $([AC]::Foco($hArrastrada))" -ForegroundColor DarkGray
                if (-not [AC]::IsWindow($hArrastrada)) { Bien "la X tras arrastrar la cerro" } else { Mal "la X tras arrastrar NO la cerro" }
                [void](TraerAlFrente $p $ide)
                [void](TeclasProtegidas $p "^b" 900)
                $flot = Flotante $p $ide "Explorador"
                $r2 = if ($flot -ne [IntPtr]::Zero) { [AC]::Rect($flot) } else { $null }
                Write-Host "    rastro posicion: antes ($($antes.Left),$($antes.Top))  arrastrada ($($r.Left),$($r.Top))  reabierta $(if ($r2) { "($($r2.Left),$($r2.Top))" } else { '-' })  handle $hArrastrada -> $flot" -ForegroundColor DarkGray
                if ($r2 -and [Math]::Abs($r2.Left - $r.Left) -le 2 -and [Math]::Abs($r2.Top - $r.Top) -le 2) { Bien "ocultarla y traerla: vuelve donde se la dejo" } else { Mal "ocultarla y traerla: no volvio donde se la dejo" }
            } else { AvisoProteccion "el titulo no es del editor o no esta al frente: no se arrastra"; Mal "no se pudo arrastrar la flotante" }
        }

        # Doble clic en su titulo: se acopla.
        if ($flot -ne [IntPtr]::Zero) {
            [void](Doble $p (@(Etiquetas $flot "Explorador"))[0])
            Start-Sleep -Milliseconds 500
            $exp = Etiquetas $ide "Explorador"
            if ((Flotante $p $ide "Explorador") -eq [IntPtr]::Zero -and $exp.Count -eq 1 -and (Centro $exp[0])[0] -gt $medio) { Bien "doble clic en la flotante: se acoplo a la derecha" } else { Mal "doble clic en la flotante: no se acoplo" }
            # En el PANEL, no solo en el editor: cerrar la flotante activa ya
            # devuelve el foco al editor (a lo que lo tenia antes), y eso solo
            # no prueba que se le dio al panel (romper_acople, 28/09).
            $foco = [AC]::Foco($ide)
            Write-Host "    rastro foco tras acoplar: $foco" -ForegroundColor DarkGray
            if ($foco.StartsWith("SysTreeView32") -and $foco.EndsWith("[dentro del IDE]")) { Bien "acoplada, el foco esta en el explorador" } else { Mal "acoplada, el foco no esta en el explorador" }
        }
    }

    # Arrastrar el titulo acoplado la saca bajo el raton; "Acoplar" del menu la devuelve.
    $exp = Etiquetas $ide "Explorador"
    if ($exp.Count -eq 1) {
        [void](TraerAlFrente $p $ide)
        $c = Centro $exp[0]
        $destino = @([int]($c[0] - 400), [int]($c[1] + 120))
        if ([AC]::Arrastrar([uint32]$p.Id, $c[0], $c[1], $destino[0], $destino[1])) {
            $flot = Esperar $p { param($h, $t) $t -eq "Explorador" -and $h -ne $ide } 3
            $tf = if ($flot -ne [IntPtr]::Zero) { @(Etiquetas $flot "Explorador") } else { @() }
            if ($tf.Count -eq 1 -and [Math]::Abs((Centro $tf[0])[1] - $destino[1]) -le 25 -and [Math]::Abs((Centro $flot)[0] - $destino[0]) -le 40) { Bien "arrastrar el titulo acoplado la saco, con su titulo bajo el raton" } else { Mal "arrastrar el titulo acoplado: flotante $($flot -ne [IntPtr]::Zero)" }
            [AC]::Capturar($ide, (Join-Path $PSScriptRoot "acople_arrastrada.png"))

            if ($tf.Count -eq 1) {
                [void](PrimeraDelMenu $p $flot $tf[0])
                Start-Sleep -Milliseconds 500
                $exp = Etiquetas $ide "Explorador"
                if ((Flotante $p $ide "Explorador") -eq [IntPtr]::Zero -and $exp.Count -eq 1) { Bien "'Acoplar' del menu la devolvio a su zona" } else { Mal "'Acoplar' del menu no la devolvio" }
            }
        } else { AvisoProteccion "el titulo no es del editor o no esta al frente: no se arrastra"; Mal "no se pudo arrastrar el titulo acoplado" }
    }

    # Los contadores de la barra de estado con la lista FLOTANTE y el editor
    # al frente: la traen al frente con el foco (Mostrar sobre una flotante
    # ya abierta: la ventana no se recrea, hay que activarla).
    $err = Etiquetas $ide "Lista de errores"
    if ($err.Count -eq 1) { [void](Clic $p $err[0]); Start-Sleep -Milliseconds 500; $err = Etiquetas $ide "Lista de errores" }
    if ($err.Count -eq 2) {
        [void](TraerAlFrente $p $ide)
        [void](Doble $p $err[0])
        $flotErr = Esperar $p { param($h, $t) $t -eq "Lista de errores" -and $h -ne $ide } 5
        [void](TraerAlFrente $p $ide)
        # El contador de errores: la primera etiqueta "0" de la barra de estado (abajo de todo).
        $contador = @(Etiquetas $ide "0" | Where-Object { ([AC]::Rect($_)).Top -gt $rv.Bottom - 40 } | Sort-Object { ([AC]::Rect($_)).Left })
        if ($flotErr -ne [IntPtr]::Zero -and $contador.Count -ge 1 -and [ProteccionUI]::GetForegroundWindow() -eq $ide) {
            [void](Clic $p $contador[0])
            Start-Sleep -Milliseconds 700
            if ([ProteccionUI]::GetForegroundWindow() -eq $flotErr -and ([AC]::Foco($flotErr)).EndsWith("[dentro del IDE]")) { Bien "el contador de errores trajo la lista flotante al frente, con el foco" } else { Mal "el contador de errores no trajo la lista flotante al frente" }
            [void](Doble $p (@(Etiquetas $flotErr "Lista de errores"))[0])
            Start-Sleep -Milliseconds 500
        } else { Mal "no se pudo preparar la lista flotante con el editor al frente (flotante $($flotErr -ne [IntPtr]::Zero), contadores $($contador.Count))" }
        if ((Flotante $p $ide "Lista de errores") -eq [IntPtr]::Zero) { Bien "la lista volvio a su zona" } else { Mal "la lista quedo flotando" }
    } else { Mal "no se pudo poner la lista de errores al frente para flotarla" }

    # Cerrar el editor con una flotante abierta.
    $exp = Etiquetas $ide "Explorador"
    if ($exp.Count -eq 1) {
        [void](TraerAlFrente $p $ide)
        [void](Doble $p $exp[0])
        $flot = Esperar $p { param($h, $t) $t -eq "Explorador" -and $h -ne $ide } 5
        [void](TraerAlFrente $p $ide)

        [void][AC]::PedirCierre([uint32]$p.Id, $ide)
        Start-Sleep -Milliseconds 900
        if (-not [AC]::IsWindowEnabled($ide)) { Bien "cerrar el editor pregunta (dialogo de salida)" } else { Mal "cerrar el editor no mostro el dialogo de salida" }
        [void](TeclasProtegidas $p "{ESC}" 900)
        $flot = Flotante $p $ide "Explorador"
        $tf = if ($flot -ne [IntPtr]::Zero) { @(Etiquetas $flot "Explorador") } else { @() }
        if (-not $p.HasExited -and $tf.Count -eq 1) { Bien "cancelar la salida: la flotante sigue, con el panel adentro" } else { Mal "cancelar la salida: flotante $($flot -ne [IntPtr]::Zero), panel $($tf.Count), editor cerrado $($p.HasExited)" }

        # OJO: Enter NO sale: el foco del dialogo esta en "Cancelar" (el primer
        # boton) y un boton con el foco gana sobre el AcceptButton. Es asi
        # tambien sin flotantes (cierre_con_flotante.ps1 -SinFlotante, 28/09).
        # Se hace clic en "Salir".
        [void](TraerAlFrente $p $ide)
        [void][AC]::PedirCierre([uint32]$p.Id, $ide)
        Start-Sleep -Milliseconds 900
        $dialogo = Buscar $p { param($h, $t) $h -ne $ide -and $t -eq "" }
        $salir = if ($dialogo -ne [IntPtr]::Zero) { [AC]::Hijo($dialogo, "Salir") } else { [IntPtr]::Zero }
        if ($salir -ne [IntPtr]::Zero) { [void](Clic $p $salir) } else { Mal "no encuentro el boton Salir del dialogo" }
        if ($p.WaitForExit(10000)) {
            if ($p.ExitCode -eq 0) { Bien "salir con la flotante abierta: el editor termino limpio (codigo 0)" } else { Mal "salir con la flotante abierta: codigo $($p.ExitCode)" }
        } else { Mal "salir con la flotante abierta: el editor NO termino (un error?)"; [AC]::Capturar($ide, (Join-Path $PSScriptRoot "acople_no_cerro.png")) }
    } else { Mal "no esta el explorador acoplado para probar el cierre" }
} finally {
    CerrarEditorAislado
}

Write-Host ""
if ($fallas -eq 0) { Write-Host "TODO BIEN" -ForegroundColor Green } else { Write-Host "$fallas FALLA(S)" -ForegroundColor Red }
