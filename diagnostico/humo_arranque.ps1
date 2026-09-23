# Verificación de humo del editor: arranca, mide y cierra.
# Detecta dos regresiones concretas que ya ocurrieron:
#   1. El documento nuevo aparecía marcado como sucio ('*' en el título).
#   2. El resaltado se realimentaba y quemaba CPU de forma permanente en reposo.
#
# Es para leer yo, no forma parte de la interfaz del editor.

$ErrorActionPreference = 'Stop'

# Editor aislado: no usa la configuracion ni los proyectos de Fabian.
. "$PSScriptRoot\editor_aislado.ps1"
$exe = PrepararEditorAislado
if (-not (Test-Path $exe)) { throw "No existe el ejecutable: $exe. Compilá primero." }

Write-Host "Arrancando $exe"
$p = Start-Process $exe -PassThru
Start-Sleep -Seconds 3
$p.Refresh()

if ($p.HasExited) { throw "El editor se cerró solo (codigo $($p.ExitCode))." }

$titulo = $p.MainWindowTitle
$cpuArranque = $p.CPU

Write-Host ""
Write-Host "Titulo de la ventana : '$titulo'"
Write-Host "CPU tras el arranque : $([math]::Round($cpuArranque,2)) s"

# Regresión 1: documento nuevo no debe estar sucio.
if ($titulo -like '`** *') {
    Write-Host "FALLA - el documento nuevo aparece como sucio (tiene '*')" -ForegroundColor Red
} else {
    Write-Host "OK    - el documento nuevo no esta marcado como sucio" -ForegroundColor Green
}

# Regresión 2: en reposo casi no debe consumir CPU.
Write-Host ""
Write-Host "Midiendo consumo en reposo (5 s sin tocar nada)..."
Start-Sleep -Seconds 5
$p.Refresh()
$cpuReposo = $p.CPU - $cpuArranque
Write-Host "CPU consumido en reposo: $([math]::Round($cpuReposo,2)) s"

if ($cpuReposo -gt 1.0) {
    Write-Host "FALLA - consume CPU en reposo: el resaltado probablemente se realimenta" -ForegroundColor Red
} else {
    Write-Host "OK    - en reposo no consume CPU apreciable" -ForegroundColor Green
}

Stop-Process -Id $p.Id -Force -ErrorAction SilentlyContinue
Write-Host ""
Write-Host "Editor cerrado."
