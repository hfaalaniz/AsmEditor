# ============================================================================
# ¿Llega Ctrl+Z a DisenadorControl.ProcessCmdKey mientras se escribe en una
# casilla del PropertyGrid?
#
# Motivo: con la excepcion del PropertyGrid ANULADA, deshacer_en_disenador.ps1
# seguia en verde. O la tecla no llega nunca (y la excepcion es codigo
# muerto), o la prueba no ejercita lo que parece. Esto lo mide.
#
# Mete una linea de rastro al principio de ProcessCmdKey que anota cada Ctrl+Z
# y a que control iba dirigido, corre el diagnostico y restaura el fuente byte
# por byte (finally), recompilando el original.
#
# Es para que lo lea yo, no forma parte de la interfaz.
# ============================================================================

$ErrorActionPreference = "Stop"

$raiz    = Split-Path (Split-Path $PSScriptRoot)
$fuente  = "$raiz\DisenadorControl.cs"
$rastro  = "$PSScriptRoot\rastro_ctrlz.txt"
$bytesOriginales = [IO.File]::ReadAllBytes($fuente)
$original = [Text.Encoding]::UTF8.GetString($bytesOriginales)

$ancla = "    protected override bool ProcessCmdKey(ref Message msg, Keys teclas)`n    {`n"
if (-not $original.Contains($ancla)) { throw "No encuentro ProcessCmdKey: el fuente cambio." }

$linea  = "        if ((teclas & Keys.KeyCode) == Keys.Z) System.IO.File.AppendAllText(@`"$rastro`", " +
          "`$`"{DateTime.Now:HH:mm:ss.fff} {teclas} destino={FromHandle(msg.HWnd)?.GetType().FullName ?? msg.HWnd.ToString()}\n`");`n"

function Compilar {
    $ErrorActionPreference = "Continue"
    $s = & dotnet build "$raiz\AsmEditor.csproj" -nologo -v q 2>&1 | Out-String
    $ErrorActionPreference = "Stop"
    if ($s -notmatch "0 Errores") { Write-Host $s; throw "No compilo" }
}

if (Test-Path $rastro) { Remove-Item $rastro -Force }

try {
    [IO.File]::WriteAllText($fuente, $original.Replace($ancla, $ancla + $linea), (New-Object System.Text.UTF8Encoding($false)))
    Compilar
    Write-Host "  (compilado CON rastro)"

    $ErrorActionPreference = "Continue"
    $salida = & "$PSScriptRoot\deshacer_en_disenador.ps1" *>&1 | Out-String
    $ErrorActionPreference = "Stop"

    $salida -split "`n" | Where-Object { $_ -match "^\s*(OK|MAL|===)" } | ForEach-Object { Write-Host "    $($_.Trim())" }
}
finally {
    [IO.File]::WriteAllBytes($fuente, $bytesOriginales)
    Compilar
    Write-Host "  (original restaurado y recompilado)"
}

Write-Host ""
Write-Host "=== Ctrl+Z que llegaron a DisenadorControl.ProcessCmdKey ===" -ForegroundColor Cyan
if (Test-Path $rastro) { Get-Content $rastro | ForEach-Object { Write-Host "  $_" } }
else { Write-Host "  (ninguno)" }
