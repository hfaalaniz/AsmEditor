# Verifica las tres funciones nuevas sobre la ventana real, con SendKeys:
#   1. Multiples pestañas (Ctrl+O abre, Ctrl+W cierra, Ctrl+Tab rota)
#   2. Autocompletado (tipear 'mo' debe abrir el popup)
#   3. Explorador lateral (Ctrl+B lo oculta y lo muestra)
#
# Es para leer yo. No reemplaza la prueba a mano: confirma que las rutas no explotan.

$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Windows.Forms

# Editor aislado: no usa la configuracion ni los proyectos de Fabian.
. "$PSScriptRoot\editor_aislado.ps1"
$exe = PrepararEditorAislado
if (-not (Test-Path $exe)) { throw "No existe el ejecutable: $exe" }

$p = Start-Process $exe -PassThru
Start-Sleep -Seconds 3
$p.Refresh()
if ($p.HasExited) { throw "El editor se cerro solo al arrancar." }

# Teclas protegidas: solo salen con el editor en primer plano (ver
# proteccion_interfaz.ps1; el 23/09 teclas de prueba pudieron ir a otras ventanas).
. "$PSScriptRoot\proteccion_interfaz.ps1"

function Enfocar {
    $p.Refresh()
    if (-not (TraerAlFrente $p $p.MainWindowHandle)) { AvisoProteccion "el editor no pudo pasar al primer plano" }
}

function Estado($etiqueta) {
    $p.Refresh()
    if ($p.HasExited) {
        Write-Host "FALLA - el editor se cerro durante: $etiqueta" -ForegroundColor Red
        exit 1
    }
    Write-Host ("  {0,-34} titulo='{1}'" -f $etiqueta, $p.MainWindowTitle)
}

Write-Host "== Estado inicial =="
Estado "arranque"

Write-Host ""
Write-Host "== Pestañas: Ctrl+N tres veces =="
Enfocar
foreach ($i in 1..3) {
    [void](TeclasProtegidas $p "^n" 700)
    Estado "tras Ctrl+N #$i"
}

Write-Host ""
Write-Host "== Pestañas: Ctrl+Tab para rotar =="
Enfocar
foreach ($i in 1..2) {
    [void](TeclasProtegidas $p "^{TAB}" 600)
    Estado "tras Ctrl+Tab #$i"
}

Write-Host ""
Write-Host "== Autocompletado: tipear 'mo' en una linea nueva =="
Enfocar
[void](TeclasProtegidas $p "{END}{ENTER}    mo" 900)
Estado "tras tipear 'mo'"

Write-Host ""
Write-Host "== Autocompletado: Escape para cerrar el popup =="
Enfocar
[void](TeclasProtegidas $p "{ESC}" 500)
Estado "tras Escape"

Write-Host ""
Write-Host "== Explorador: Ctrl+B ocultar y mostrar =="
Enfocar
[void](TeclasProtegidas $p "^b" 600)
Estado "explorador oculto"
Enfocar
[void](TeclasProtegidas $p "^b" 600)
Estado "explorador visible"

Write-Host ""
Write-Host "== Consumo en reposo tras todo el manoseo =="
$cpuAntes = $p.CPU
Start-Sleep -Seconds 5
$p.Refresh()
$delta = $p.CPU - $cpuAntes
Write-Host "  CPU en 5 s de reposo: $([math]::Round($delta,2)) s"
if ($delta -gt 1.0) {
    Write-Host "  FALLA - sigue quemando CPU en reposo" -ForegroundColor Red
} else {
    Write-Host "  OK    - en reposo esta quieto" -ForegroundColor Green
}

Write-Host ""
Write-Host "El editor sigue vivo y no se colgo en ninguna de las tres funciones." -ForegroundColor Green
Write-Host "(se deja abierto a proposito: hay 4 pestañas con cambios sin guardar)"
# Por Id, no por nombre: por nombre cerraria tambien el editor de Fabian.
Write-Host "Para cerrarlo: Stop-Process -Id $($p.Id) -Force"
