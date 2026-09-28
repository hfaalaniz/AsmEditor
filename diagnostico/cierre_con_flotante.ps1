# ============================================================================
# Rastro: cerrar el editor con una ventana flotante abierta (3d).
#
# acople.ps1 (seccion H) vio que, tras cancelar una vez la salida, la segunda
# salida con Enter no terminaba el editor. Esto repite SOLO ese camino y
# anota cada 100 ms las ventanas del proceso (clase, habilitada, y el texto
# solo si es del editor), cual esta al frente y cuando termina el proceso.
#
# -SinFlotante: lo mismo sin sacar el explorador (para separar lo que es de
# la flotante de lo que es del dialogo de salida).
#
# Editor AISLADO y teclas/clics PROTEGIDOS. Es para que lo lea yo.
# ============================================================================

param([switch]$SinFlotante)

$ErrorActionPreference = "Stop"

. "$PSScriptRoot\editor_aislado.ps1"
. "$PSScriptRoot\proteccion_interfaz.ps1"

Add-Type @"
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
public static class CF {
    public struct RECT { public int Left, Top, Right, Bottom; }
    public struct POINT { public int X, Y; }
    delegate bool EnumProc(IntPtr h, IntPtr p);
    [DllImport("user32.dll")] static extern bool EnumWindows(EnumProc cb, IntPtr p);
    [DllImport("user32.dll")] static extern bool EnumChildWindows(IntPtr h, EnumProc cb, IntPtr p);
    [DllImport("user32.dll")] static extern uint GetWindowThreadProcessId(IntPtr h, out uint pid);
    [DllImport("user32.dll")] public static extern bool IsWindowVisible(IntPtr h);
    [DllImport("user32.dll")] public static extern bool IsWindowEnabled(IntPtr h);
    [DllImport("user32.dll")] public static extern IntPtr GetForegroundWindow();
    [DllImport("user32.dll", CharSet=CharSet.Unicode)] static extern int GetClassNameW(IntPtr h, StringBuilder s, int n);
    [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr h, out RECT r);
    [DllImport("user32.dll", CharSet=CharSet.Unicode, EntryPoint="SendMessageTimeoutW")]
    static extern IntPtr SendMessageTimeoutTexto(IntPtr h, int m, IntPtr w, StringBuilder l, uint f, uint ms, out IntPtr r);
    [DllImport("user32.dll")] static extern bool PostMessage(IntPtr h, int m, IntPtr w, IntPtr l);
    [DllImport("user32.dll")] static extern bool SetCursorPos(int x, int y);
    [DllImport("user32.dll")] static extern void mouse_event(uint f, uint x, uint y, uint d, IntPtr e);
    [DllImport("user32.dll")] static extern IntPtr WindowFromPoint(POINT p);
    [DllImport("user32.dll")] static extern IntPtr GetAncestor(IntPtr h, uint f);

    public static uint Pid(IntPtr h) { uint p; GetWindowThreadProcessId(h, out p); return p; }
    public static string Clase(IntPtr h) { var sb = new StringBuilder(256); GetClassNameW(h, sb, 256); return sb.ToString(); }

    // SOLO de ventanas del proceso indicado.
    public static string Texto(IntPtr h, uint pid) {
        if (Pid(h) != pid) return "";
        var sb = new StringBuilder(512); IntPtr r;
        SendMessageTimeoutTexto(h, 0x000D, (IntPtr)512, sb, 0x2, 500, out r);
        return sb.ToString();
    }

    public static List<IntPtr> Ventanas(uint pid) {
        var r = new List<IntPtr>();
        EnumWindows(delegate(IntPtr h, IntPtr p) { if (Pid(h) == pid && IsWindowVisible(h)) r.Add(h); return true; }, IntPtr.Zero);
        return r;
    }

    public static IntPtr Etiqueta(IntPtr raiz, string texto) {
        IntPtr hallada = IntPtr.Zero; uint pid = Pid(raiz);
        EnumChildWindows(raiz, delegate(IntPtr h, IntPtr p) {
            if (IsWindowVisible(h) && Clase(h).ToUpperInvariant().Contains("STATIC") && Texto(h, pid) == texto) { hallada = h; return false; }
            return true;
        }, IntPtr.Zero);
        return hallada;
    }

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

    public static bool PedirCierre(uint pid, IntPtr h) { return Pid(h) == pid && PostMessage(h, 0x0010, IntPtr.Zero, IntPtr.Zero); }

    // Un control hijo visible con ese texto (cualquier clase), SOLO del proceso.
    public static IntPtr Hijo(IntPtr raiz, string texto) {
        IntPtr hallado = IntPtr.Zero; uint pid = Pid(raiz);
        EnumChildWindows(raiz, delegate(IntPtr h, IntPtr p) {
            if (IsWindowVisible(h) && Texto(h, pid) == texto) { hallado = h; return false; }
            return true;
        }, IntPtr.Zero);
        return hallado;
    }
}
"@

$finTituloIDE = "Editor ASM (NASM + GoLink)"

function Foto($p, $ide, $momento) {
    $pid_ = [uint32]$p.Id
    if ($p.HasExited) { Write-Host ("  {0,-26} PROCESO TERMINADO (codigo {1})" -f $momento, $p.ExitCode); return }
    $frente = [CF]::GetForegroundWindow()
    $partes = foreach ($h in [CF]::Ventanas($pid_)) {
        $quien = if ($h -eq $ide) { "IDE" } else { "'" + [CF]::Texto($h, $pid_) + "'" }
        $clase = ([CF]::Clase($h)).Replace("WindowsForms10.", "").Split('.')[0]
        "{0}[{1}{2}{3}]" -f $quien, $clase, $(if ([CF]::IsWindowEnabled($h)) { "" } else { ",DESHAB" }), $(if ($h -eq $frente) { ",FRENTE" } else { "" })
    }
    $frenteAjeno = if ([CF]::Pid($frente) -ne $pid_) { "  (al frente: otro programa, $([ProteccionUI]::Programa($frente)))" } else { "" }
    Write-Host ("  {0,-26} {1}{2}" -f $momento, ($partes -join "  "), $frenteAjeno)
}

function Rastrear($p, $ide, $etiqueta, $ms) {
    $reloj = [Diagnostics.Stopwatch]::StartNew()
    $ultima = ""
    while ($reloj.ElapsedMilliseconds -lt $ms) {
        $foto = (Foto $p $ide ("{0} +{1}ms" -f $etiqueta, $reloj.ElapsedMilliseconds) 6>&1 | Out-String).Trim()
        # Solo cuando cambia algo (sin contar el tiempo).
        $sinTiempo = $foto -replace '^\S+ \+\d+ms\s+', ''
        if ($sinTiempo -ne $ultima) { Write-Host $foto; $ultima = $sinTiempo }
        if ($p.HasExited) { return }
        Start-Sleep -Milliseconds 100
    }
}

Write-Host "=== Preparando ==="
$exe = PrepararEditorAislado
$p = Start-Process $exe -PassThru
try {
    $ide = [IntPtr]::Zero
    $reloj = [Diagnostics.Stopwatch]::StartNew()
    while ($ide -eq [IntPtr]::Zero -and $reloj.Elapsed.TotalSeconds -lt 25) {
        foreach ($h in [CF]::Ventanas([uint32]$p.Id)) { if (([CF]::Texto($h, [uint32]$p.Id)).EndsWith($finTituloIDE)) { $ide = $h } }
        Start-Sleep -Milliseconds 250
    }
    if ($ide -eq [IntPtr]::Zero) { throw "no aparecio el IDE" }
    Start-Sleep -Milliseconds 1500
    if (-not (TraerAlFrente $p $ide)) { throw "no se pudo traer el IDE al frente" }

    if (-not $SinFlotante) {
        Write-Host "=== Flotar el explorador (doble clic en su titulo) ==="
        $exp = [CF]::Etiqueta($ide, "Explorador")
        if ($exp -eq [IntPtr]::Zero) { throw "no esta el explorador" }
        $r = New-Object CF+RECT; [void][CF]::GetWindowRect($exp, [ref]$r)
        if (-not [CF]::DobleClic([uint32]$p.Id, [int](($r.Left + $r.Right) / 2), [int](($r.Top + $r.Bottom) / 2))) { throw "el titulo no es del editor" }
        Rastrear $p $ide "flotada" 1000
    }

    Write-Host "=== Primer cierre: Esc (cancelar) ==="
    [void](TraerAlFrente $p $ide)
    [void][CF]::PedirCierre([uint32]$p.Id, $ide)
    Rastrear $p $ide "cierre-1" 1200
    [void](TeclasProtegidas $p "{ESC}" 100)
    Rastrear $p $ide "tras-esc" 1200

    # OJO: Enter NO sale: el foco del dialogo esta en "Cancelar" (el primer
    # boton) y un boton con el foco se queda con el Enter antes que el
    # AcceptButton (medido el 28/09 con -SinFlotante: igual). Se hace clic
    # en "Salir".
    Write-Host "=== Segundo cierre: clic en Salir ==="
    [void](TraerAlFrente $p $ide)
    Rastrear $p $ide "tras-traer" 500
    [void][CF]::PedirCierre([uint32]$p.Id, $ide)
    Rastrear $p $ide "cierre-2" 1200
    $dialogo = [CF]::Ventanas([uint32]$p.Id) | Where-Object { $_ -ne $ide -and ([CF]::Texto($_, [uint32]$p.Id)) -eq "" } | Select-Object -First 1
    $salir = if ($dialogo) { [CF]::Hijo($dialogo, "Salir") } else { [IntPtr]::Zero }
    if ($salir -eq [IntPtr]::Zero) { throw "no encuentro el boton Salir del dialogo" }
    $r = New-Object CF+RECT; [void][CF]::GetWindowRect($salir, [ref]$r)
    [void](ClicProtegido $p ([int](($r.Left + $r.Right) / 2)) ([int](($r.Top + $r.Bottom) / 2)))
    Rastrear $p $ide "tras-salir" 8000
} finally {
    CerrarEditorAislado
}
