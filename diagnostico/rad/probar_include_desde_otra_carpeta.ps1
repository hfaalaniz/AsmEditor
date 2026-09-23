# ============================================================================
# Verifica que un .asm con %include SOLO ensambla si el directorio de trabajo
# es la carpeta del fuente.
#
# Importa porque el editor compila con WorkingDirectory =
# Path.GetDirectoryName(archivo) (MainForm.CompileStepAsync). Esta prueba
# confirma que esa eleccion es la correcta y que cambiarla romperia todos los
# formularios del disenador.
# ============================================================================

$ErrorActionPreference = "Stop"

$nasm   = "C:\Users\Fabian\NASM\nasm.exe"
$fuente = "$PSScriptRoot\generado\Prueba.asm"

if (-not (Test-Path $fuente)) {
    throw "Falta $fuente. Corre antes probar_generador_extremo_a_extremo.ps1"
}

Write-Host ""
Write-Host "=== A. Desde la carpeta del .asm (lo que hace el editor) ===" -ForegroundColor Cyan

Push-Location "$PSScriptRoot\generado"
& $nasm -f win64 "Prueba.asm" -o "$env:TEMP\inc_ok.obj" 2>&1 | Write-Host
$codigoA = $LASTEXITCODE
Pop-Location

if ($codigoA -eq 0) {
    Write-Host "  OK: ensamblo" -ForegroundColor Green
} else {
    Write-Host "  FALLO (no deberia)" -ForegroundColor Red
}

Write-Host ""
Write-Host "=== B. Desde otra carpeta, con ruta absoluta al fuente ===" -ForegroundColor Cyan

# ⚠ Se apaga ErrorActionPreference alrededor de esta llamada: se ESPERA que
# NASM falle, y con "Stop" PowerShell trata su stderr como excepcion y corta el
# script justo en el caso que se quiere medir.
Push-Location $env:TEMP
$previo = $ErrorActionPreference
$ErrorActionPreference = "Continue"
$salidaB = & $nasm -f win64 $fuente -o "$env:TEMP\inc_mal.obj" 2>&1
$codigoB = $LASTEXITCODE
$ErrorActionPreference = $previo
Pop-Location

$salidaB | ForEach-Object { Write-Host "  $_" }

if ($codigoB -ne 0) {
    Write-Host "  Fallo, como se esperaba: NASM busca el .inc en el directorio de trabajo" -ForegroundColor Yellow
} else {
    Write-Host "  Ensamblo igual: la premisa sobre el %include estaba equivocada" -ForegroundColor Red
}

Write-Host ""

if ($codigoA -eq 0 -and $codigoB -ne 0) {
    Write-Host "=== CONFIRMADO: el %include se resuelve contra el DIRECTORIO DE TRABAJO. ===" -ForegroundColor Green
    Write-Host "=== El editor ya usa la carpeta del .asm, asi que los formularios compilan. ===" -ForegroundColor Green
    exit 0
}

Write-Host "=== El resultado no es el esperado: revisar la premisa ===" -ForegroundColor Red
exit 1
