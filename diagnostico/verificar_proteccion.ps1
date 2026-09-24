# ============================================================================
# Verifica que proteccion_interfaz.ps1 FRENA de verdad.
#
#   1. Con OTRA ventana al frente (un Bloc de notas que abre este script),
#      TeclasProtegidas no manda nada: el Bloc de notas no recibe texto.
#   2. ClicProtegido sobre un punto tapado por el Bloc de notas no hace clic.
#   3. Con el editor al frente (TraerAlFrente), la tecla SI sale: Ctrl+N
#      abre un documento nuevo.
#
# El Bloc de notas es propio de la prueba y se cierra por su Id al final.
#
# Es para que lo lea yo, no forma parte de la interfaz.
# ============================================================================

$ErrorActionPreference = "Stop"
. "$PSScriptRoot\editor_aislado.ps1"
. "$PSScriptRoot\proteccion_interfaz.ps1"

Add-Type @"
using System; using System.Runtime.InteropServices;
public class RN { public struct R { public int L, T, Ri, B; } [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr h, out R r); }
"@

$fallas = 0
function Bien($t) { Write-Host "  OK   $t" -ForegroundColor Green }
function Mal($t)  { Write-Host "  MAL  $t" -ForegroundColor Red; $script:fallas++ }

$exe = PrepararEditorAislado
$editor = Start-Process $exe -PassThru
Start-Sleep -Seconds 5; $editor.Refresh()

$notas = Start-Process notepad.exe -PassThru
Start-Sleep -Seconds 2; $notas.Refresh()
[void][ProteccionUI]::SetForegroundWindow($notas.MainWindowHandle); Start-Sleep -Milliseconds 500

try {
    Write-Host ""; Write-Host "=== 1. Teclas con otra ventana al frente ===" -ForegroundColor Cyan
    if (EditorAlFrente $editor) { Mal "el editor quedo al frente: la prueba no sirve"; throw "prueba invalida" }

    $salio = TeclasProtegidas $editor "abc"
    $notas.Refresh()
    if (-not $salio) { Bien "TeclasProtegidas se nego a mandar" } else { Mal "TeclasProtegidas mando las teclas" }
    if (-not $notas.MainWindowTitle.StartsWith("*")) { Bien "el Bloc de notas no recibio nada ('$($notas.MainWindowTitle)')" }
    else { Mal "el Bloc de notas recibio texto ('$($notas.MainWindowTitle)')" }

    Write-Host ""; Write-Host "=== 2. Clic sobre un punto tapado ===" -ForegroundColor Cyan
    $r = New-Object RN+R; [void][RN]::GetWindowRect($notas.MainWindowHandle, [ref]$r)
    $x = [int](($r.L + $r.Ri) / 2); $y = [int](($r.T + $r.B) / 2)
    if (-not (ClicProtegido $editor $x $y)) { Bien "ClicProtegido se nego a hacer clic en el Bloc de notas" } else { Mal "ClicProtegido hizo clic en otra ventana" }

    Write-Host ""; Write-Host "=== 3. Con el editor al frente, la tecla sale ===" -ForegroundColor Cyan
    if (TraerAlFrente $editor $editor.MainWindowHandle) { Bien "TraerAlFrente trajo el editor" } else { Mal "TraerAlFrente no pudo" }
    $antes = $editor.MainWindowTitle
    [void](TeclasProtegidas $editor "^n" 900)
    $editor.Refresh()
    if ($editor.MainWindowTitle -match "Sin t") { Bien "Ctrl+N salio: '$($editor.MainWindowTitle)'" } else { Mal "Ctrl+N no salio: '$($editor.MainWindowTitle)'" }
}
finally {
    try { $notas.Kill() } catch { }
    CerrarEditorAislado
}

Write-Host ""
if ($fallas -eq 0) { Write-Host "=== TODO BIEN: la proteccion frena y deja pasar cuando corresponde ===" -ForegroundColor Green; exit 0 }
Write-Host "=== $fallas FALLAS ===" -ForegroundColor Red
exit 1
