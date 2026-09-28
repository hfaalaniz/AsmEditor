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
} finally {
    CerrarEditorAislado
}

Write-Host ""
if ($fallas -eq 0) { Write-Host "TODO BIEN" -ForegroundColor Green } else { Write-Host "$fallas FALLA(S)" -ForegroundColor Red }
