# ============================================================================
# Dialogo de salida sin archivos modificados: el foco arranca en "Salir" (el
# boton por defecto), Esc cancela y Enter sale (codigo 0).
#
# Hasta el 28/09 el foco arrancaba en "Cancelar" y Enter cancelaba.
# Editor AISLADO y teclas PROTEGIDAS. Es para que lo lea yo.
# ============================================================================

$ErrorActionPreference = "Stop"

. "$PSScriptRoot\editor_aislado.ps1"
. "$PSScriptRoot\proteccion_interfaz.ps1"

Add-Type @"
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
public static class DS {
    delegate bool EnumProc(IntPtr h, IntPtr p);
    [DllImport("user32.dll")] static extern bool EnumWindows(EnumProc cb, IntPtr p);
    [DllImport("user32.dll")] static extern uint GetWindowThreadProcessId(IntPtr h, out uint pid);
    [DllImport("user32.dll")] public static extern bool IsWindowVisible(IntPtr h);
    [DllImport("user32.dll")] public static extern bool IsWindowEnabled(IntPtr h);
    [DllImport("user32.dll")] static extern IntPtr GetFocus();
    [DllImport("user32.dll")] static extern bool AttachThreadInput(uint a, uint b, bool unir);
    [DllImport("kernel32.dll")] static extern uint GetCurrentThreadId();
    [DllImport("user32.dll")] static extern bool PostMessage(IntPtr h, int m, IntPtr w, IntPtr l);
    [DllImport("user32.dll", CharSet=CharSet.Unicode, EntryPoint="SendMessageTimeoutW")]
    static extern IntPtr SendMessageTimeoutTexto(IntPtr h, int m, IntPtr w, StringBuilder l, uint f, uint ms, out IntPtr r);

    static uint Pid(IntPtr h) { uint p; GetWindowThreadProcessId(h, out p); return p; }

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

    // El texto del control con el foco en el hilo de esa ventana (del proceso).
    public static string TextoDelFoco(IntPtr ventana) {
        uint pid; uint hilo = GetWindowThreadProcessId(ventana, out pid);
        uint mio = GetCurrentThreadId();
        AttachThreadInput(mio, hilo, true);
        IntPtr f = GetFocus();
        AttachThreadInput(mio, hilo, false);
        return f == IntPtr.Zero ? "(ninguno)" : "'" + Texto(f, pid) + "'";
    }

    public static bool PedirCierre(uint pid, IntPtr h) { return Pid(h) == pid && PostMessage(h, 0x0010, IntPtr.Zero, IntPtr.Zero); }
}
"@

$fallas = 0
function Bien($t) { Write-Host "  OK   $t" -ForegroundColor Green }
function Mal($t)  { Write-Host "  MAL  $t" -ForegroundColor Red; $script:fallas++ }

$finTituloIDE = "Editor ASM (NASM + GoLink)"

$exe = PrepararEditorAislado
$p = Start-Process $exe -PassThru
try {
    $ide = [IntPtr]::Zero
    $reloj = [Diagnostics.Stopwatch]::StartNew()
    while ($ide -eq [IntPtr]::Zero -and $reloj.Elapsed.TotalSeconds -lt 25) {
        foreach ($h in [DS]::Ventanas([uint32]$p.Id)) { if (([DS]::Texto($h, [uint32]$p.Id)).EndsWith($finTituloIDE)) { $ide = $h } }
        Start-Sleep -Milliseconds 250
    }
    if ($ide -eq [IntPtr]::Zero) { throw "no aparecio el IDE" }
    Start-Sleep -Milliseconds 1500
    if (-not (TraerAlFrente $p $ide)) { throw "no se pudo traer el IDE al frente" }

    # Esc cancela.
    [void][DS]::PedirCierre([uint32]$p.Id, $ide)
    Start-Sleep -Milliseconds 900
    if (-not [DS]::IsWindowEnabled($ide)) { Bien "cerrar muestra el dialogo de salida" } else { Mal "no aparecio el dialogo de salida" }
    $foco = [DS]::TextoDelFoco($ide)
    if ($foco -eq "'Salir'") { Bien "el foco arranca en 'Salir'" } else { Mal "el foco arranca en $foco (tenia que ser 'Salir')" }
    [void](TeclasProtegidas $p "{ESC}" 900)
    if (-not $p.HasExited -and [DS]::IsWindowEnabled($ide)) { Bien "Esc cancela: el editor sigue abierto" } else { Mal "Esc no cancelo" }

    # Enter sale.
    [void](TraerAlFrente $p $ide)
    [void][DS]::PedirCierre([uint32]$p.Id, $ide)
    Start-Sleep -Milliseconds 900
    [void](TeclasProtegidas $p "{ENTER}" 100)
    if ($p.WaitForExit(10000)) {
        if ($p.ExitCode -eq 0) { Bien "Enter sale: el editor termino (codigo 0)" } else { Mal "Enter: el editor termino con codigo $($p.ExitCode)" }
    } else { Mal "Enter NO salio (el editor sigue abierto)" }
} finally {
    CerrarEditorAislado
}

Write-Host ""
if ($fallas -eq 0) { Write-Host "TODO BIEN" -ForegroundColor Green } else { Write-Host "$fallas FALLA(S)" -ForegroundColor Red }
