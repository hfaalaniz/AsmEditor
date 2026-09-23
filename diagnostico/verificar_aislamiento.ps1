# ============================================================================
# Verifica que los diagnosticos que abren el editor NO tocan nada de Fabian.
#
#   1. Toma el hash de sus archivos en juego (configuracion, proyecto, los
#      fuentes que los diagnosticos copian y sus .obj/.exe, continuar.txt).
#   2. Deja abierto un editor SENUELO, minimizado y fuera de editor_aislado\,
#      que hace de "el editor de Fabian": ningun diagnostico lo puede cerrar.
#   3. Corre los nueve diagnosticos que abren el editor.
#   4. Compara: los hashes tienen que ser identicos y el senuelo tiene que
#      seguir vivo.
#
# Es para que lo lea yo, no forma parte de la interfaz.
# ============================================================================

$ErrorActionPreference = "Stop"

$diag = $PSScriptRoot
$nasm = "C:\Users\Fabian\NASM"

$vigilados = @(
    "$nasm\AsmEditor\bin\Debug\net8.0-windows\settings.json",
    "$nasm\MiPrograma.asmproj",
    "$nasm\continuar.txt",
    "$nasm\Win6Win.asm", "$nasm\Win6Win.obj", "$nasm\Win6Win.exe",
    "$nasm\masm64.asm",  "$nasm\masm64.obj",  "$nasm\masm64.exe",
    "$nasm\window_debug.asm", "$nasm\window_debug.obj", "$nasm\window_debug.exe"
)

function Huella($f) {
    if (-not (Test-Path $f)) { return "(no existe)" }
    return (Get-FileHash $f).Hash
}

$antes = [ordered]@{}
foreach ($f in $vigilados) { $antes[$f] = Huella $f }

# ---- El senuelo --------------------------------------------------------------
. "$diag\editor_aislado.ps1"
$aislado = PrepararEditorAislado

$senueloDir = Join-Path $diag "senuelo_editor"
if (Test-Path $senueloDir) { Remove-Item $senueloDir -Recurse -Force }
Copy-Item (Split-Path $aislado -Parent) $senueloDir -Recurse

$senuelo = Start-Process (Join-Path $senueloDir "AsmEditor.exe") -WindowStyle Minimized -PassThru
Start-Sleep -Seconds 4
Write-Host "senuelo: pid $($senuelo.Id)"

# ---- Los nueve ---------------------------------------------------------------
$scripts = @(
    "humo_arranque.ps1", "humo_funciones.ps1", "captura.ps1",
    "compilar_por_interfaz.ps1", "probar_combinaciones.ps1",
    "proyectos\probar_por_interfaz.ps1",
    "rad\abrir_asmform_y_generar.ps1", "rad\disenador_en_pestanas.ps1",
    "rad\deshacer_en_disenador.ps1"
)

foreach ($s in $scripts) {
    Write-Host ""
    Write-Host "##### $s" -ForegroundColor Cyan
    $ErrorActionPreference = "Continue"
    $salida = & powershell -NoProfile -ExecutionPolicy Bypass -File "$diag\$s" *>&1 | Out-String
    $codigo = $LASTEXITCODE
    $ErrorActionPreference = "Stop"

    $salida -split "`n" |
        Where-Object { $_ -match "MAL|FALLA|FALLAS|TODO BIEN|OK\b|COMBINACIONES|COMPILADO|target|fuente|Titulo|titulo:|EXE|OBJ" } |
        ForEach-Object { Write-Host "    $($_.TrimEnd())" }
    Write-Host "##### $s -> salida $codigo"

    # Se informa UNA vez, en el diagnostico donde murio, con su codigo:
    # -1 = lo mataron (Kill), 0 = se cerro normalmente, otro = se cayo.
    $senuelo.Refresh()
    if ($senuelo.HasExited -and -not $script:senueloInformado) {
        Write-Host "  !!! el senuelo murio durante $s (codigo $($senuelo.ExitCode), $($senuelo.ExitTime.ToString('HH:mm:ss')))" -ForegroundColor Red
        $script:senueloInformado = $true
    }
}

# ---- Resultado ---------------------------------------------------------------
Write-Host ""
Write-Host "=== Archivos de Fabian ===" -ForegroundColor Cyan
$cambiaron = 0
foreach ($f in $vigilados) {
    $ahora = Huella $f
    if ($ahora -eq $antes[$f]) { Write-Host "  igual    $f" -ForegroundColor Green }
    else { Write-Host "  CAMBIO   $f" -ForegroundColor Red; $cambiaron++ }
}

$senuelo.Refresh()
$vivo = -not $senuelo.HasExited
if ($vivo) { Write-Host "  el senuelo sigue vivo" -ForegroundColor Green; $senuelo.Kill() }
else { Write-Host "  EL SENUELO MURIO: algun diagnostico cierra editores ajenos" -ForegroundColor Red }

Remove-Item $senueloDir -Recurse -Force -ErrorAction SilentlyContinue

if ($cambiaron -eq 0 -and $vivo) {
    Write-Host "=== AISLADO: ningun diagnostico toco nada de Fabian ===" -ForegroundColor Green
    exit 0
}
Write-Host "=== NO AISLADO: $cambiaron archivos cambiaron, senuelo vivo=$vivo ===" -ForegroundColor Red
exit 1
