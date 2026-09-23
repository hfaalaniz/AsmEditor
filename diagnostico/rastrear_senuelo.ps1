# ============================================================================
# ¿Por que muere el editor senuelo durante proyectos\probar_por_interfaz.ps1?
#
# En verificar_aislamiento.ps1 el senuelo (un AsmEditor fuera de
# editor_aislado\, que hace de "el editor de Fabian") murio en esa prueba.
# Esto la corre sola con el senuelo abierto y registra CUANDO muere y CON QUE
# CODIGO:
#
#   -1           -> lo mataron (Process.Kill / TerminateProcess)
#   0            -> se cerro normalmente (alguien le mando cerrar)
#   otro         -> se cayo (excepcion)
#
# Un vigilante en segundo plano anota la ventana en primer plano cada 250 ms,
# para ver a quien le llegan las teclas.
#
# Es para que lo lea yo, no forma parte de la interfaz.
# ============================================================================

$ErrorActionPreference = "Stop"
$diag = $PSScriptRoot

Add-Type @"
using System;
using System.Runtime.InteropServices;
using System.Text;
public class Frente {
    [DllImport("user32.dll")] static extern IntPtr GetForegroundWindow();
    [DllImport("user32.dll")] static extern uint GetWindowThreadProcessId(IntPtr h, out uint pid);
    [DllImport("user32.dll", CharSet=CharSet.Unicode)] static extern int GetWindowText(IntPtr h, StringBuilder s, int n);
    public static string Ahora() {
        IntPtr h = GetForegroundWindow();
        uint pid; GetWindowThreadProcessId(h, out pid);
        var sb = new StringBuilder(256); GetWindowText(h, sb, 256);
        return pid + " '" + sb.ToString() + "'";
    }
}
"@

. "$diag\editor_aislado.ps1"
$aislado = PrepararEditorAislado

$senueloDir = Join-Path $diag "senuelo_editor"
if (Test-Path $senueloDir) { Remove-Item $senueloDir -Recurse -Force }
Copy-Item (Split-Path $aislado -Parent) $senueloDir -Recurse

$senuelo = Start-Process (Join-Path $senueloDir "AsmEditor.exe") -WindowStyle Minimized -PassThru
Start-Sleep -Seconds 4
Write-Host "senuelo: pid $($senuelo.Id)"

$registro = Join-Path $diag "rastro_senuelo.txt"
if (Test-Path $registro) { Remove-Item $registro -Force }

# La prueba corre en segundo plano; aca se vigila.
$trabajo = Start-Job -ScriptBlock {
    param($s)
    & powershell -NoProfile -ExecutionPolicy Bypass -File $s *>&1 | Out-String
} -ArgumentList "$diag\proyectos\probar_por_interfaz.ps1"

$ultimoFrente = ""
$murio = $null
while ($trabajo.State -eq "Running") {
    $f = [Frente]::Ahora()
    if ($f -ne $ultimoFrente) {
        "$(Get-Date -Format HH:mm:ss.fff) frente: $f" | Out-File -Append -Encoding utf8 $registro
        $ultimoFrente = $f
    }
    $senuelo.Refresh()
    if ($senuelo.HasExited -and -not $murio) {
        $murio = Get-Date -Format HH:mm:ss.fff
        "$murio SENUELO MURIO, codigo $($senuelo.ExitCode)" | Out-File -Append -Encoding utf8 $registro
    }
    Start-Sleep -Milliseconds 250
}

$salida = Receive-Job $trabajo
Remove-Job $trabajo

Write-Host "=== Salida de la prueba ==="
$salida -split "`n" | Where-Object { $_ -match "OK|MAL|===|titulo" } | ForEach-Object { Write-Host "  $($_.TrimEnd())" }

Write-Host "=== Rastro ==="
Get-Content $registro | ForEach-Object { Write-Host "  $_" }

$senuelo.Refresh()
if (-not $senuelo.HasExited) { Write-Host "SENUELO VIVO"; $senuelo.Kill() }
Remove-Item $senueloDir -Recurse -Force -ErrorAction SilentlyContinue
