# ============================================================================
# Prueba de los proyectos, de punta a punta y SIN interfaz.
#
# Verifica lo que mas importa y que las pruebas unitarias no cubren: que un
# proyecto de verdad, con su .asmproj en disco, produce un .exe que corre; y
# —sobre todo— que un .asm SUELTO, sin proyecto, sigue compilando igual que
# siempre.
#
# ⚠ DESDE POWERSHELL, NUNCA DESDE GIT BASH: bash convierte el "/entry" de
#   GoLink en una ruta de Windows y el .exe sale con punto de entrada 0.
#
# Es para que lo lea yo, no forma parte de la interfaz.
# ============================================================================

$ErrorActionPreference = "Stop"

$raiz   = "C:\Users\Fabian\NASM"
$nasm   = "$raiz\nasm.exe"
$golink = "$raiz\GoLink.exe"
$banco  = "$PSScriptRoot\banco"
$fallas = 0

if (Test-Path $banco) { Remove-Item $banco -Recurse -Force }
New-Item -ItemType Directory -Path $banco | Out-Null

function Bien($t) { Write-Host "  OK   $t" -ForegroundColor Green }
function Mal($t)  { Write-Host "  MAL  $t" -ForegroundColor Red; $script:fallas++ }

# Un programa minimo que solo sale: alcanza para probar la cadena completa.
$fuente = @'
default rel
extern ExitProcess
section .text
global main
main:
    sub rsp, 40
    xor ecx, ecx
    call ExitProcess
'@

# ---------------------------------------------------------------------------
Write-Host ""
Write-Host "=== 1. Un proyecto con salida en bin\ ===" -ForegroundColor Cyan

$p1 = "$banco\conbin"
New-Item -ItemType Directory -Path $p1 | Out-Null
Set-Content "$p1\prog.asm" $fuente -Encoding ASCII

# El .asmproj se escribe a mano, como lo escribiria el editor.
$proyecto = @{
    Version = 1
    Proyecto = @{
        Nombre = "Con bin"
        ArchivoPrincipal = "prog.asm"
        CarpetaSalida = "bin"
        Archivos = @("prog.asm")
        Targets = @()
        TargetActivo = 0
    }
} | ConvertTo-Json -Depth 6

Set-Content "$p1\Con bin.asmproj" $proyecto -Encoding UTF8

# Compilar como lo hace el editor con proyecto: salida en bin\.
New-Item -ItemType Directory -Path "$p1\bin" -Force | Out-Null

Push-Location $p1
& $nasm -f win64 "prog.asm" -o "bin\prog.obj"
$c = $LASTEXITCODE
Pop-Location

if ($c -ne 0) { Mal "NASM fallo"; }
elseif (-not (Test-Path "$p1\bin\prog.obj")) { Mal "no quedo el .obj en bin\" }
else { Bien "el .obj quedo en bin\" }

Push-Location $p1
& $golink /entry main "bin\prog.obj" kernel32.dll | Out-Null
$c = $LASTEXITCODE
Pop-Location

# GoLink deja el .exe donde esta el .obj salvo que se le diga otra cosa.
if ($c -ne 0) { Mal "GoLink fallo" }
elseif (Test-Path "$p1\bin\prog.exe") { Bien "el .exe quedo en bin\" }
else { Mal "no quedo el .exe en bin\" }

# El fuente no tiene que haberse ensuciado con binarios.
if (Test-Path "$p1\prog.obj") { Mal "quedo un .obj al lado del fuente" }
else { Bien "la carpeta del fuente quedo limpia" }

# ---------------------------------------------------------------------------
Write-Host ""
Write-Host "=== 2. Un .asm SUELTO, sin proyecto (como siempre) ===" -ForegroundColor Cyan

# ⚠ ESTE ES EL CASO QUE NO SE PUEDE ROMPER: los .asm de la raiz y build.ps1
#   dependen de que la salida quede al lado del fuente.
$p2 = "$banco\suelto"
New-Item -ItemType Directory -Path $p2 | Out-Null
Set-Content "$p2\suelto.asm" $fuente -Encoding ASCII

Push-Location $p2
& $nasm -f win64 "suelto.asm" -o "suelto.obj"
$c1 = $LASTEXITCODE
& $golink /entry main "suelto.obj" kernel32.dll | Out-Null
$c2 = $LASTEXITCODE
Pop-Location

if ($c1 -ne 0 -or $c2 -ne 0) { Mal "no compilo un .asm suelto" }
elseif ((Test-Path "$p2\suelto.obj") -and (Test-Path "$p2\suelto.exe")) {
    Bien "el .obj y el .exe quedaron AL LADO del fuente, como siempre"
} else {
    Mal "la salida no quedo al lado del fuente"
}

if (Test-Path "$p2\bin") { Mal "se creo un bin\ que no corresponde" }
else { Bien "no se creo ningun bin\ de mas" }

# ---------------------------------------------------------------------------
Write-Host ""
Write-Host "=== 3. Los .exe generados corren ===" -ForegroundColor Cyan

foreach ($exe in @("$p1\bin\prog.exe", "$p2\suelto.exe")) {
    if (-not (Test-Path $exe)) { Mal "falta $exe"; continue }

    # Punto de entrada 0 = no arranca. GoLink solo lo avisa como Warning.
    $b = [System.IO.File]::ReadAllBytes($exe)
    $pe = [BitConverter]::ToInt32($b, 0x3C)
    $entry = [BitConverter]::ToInt32($b, $pe + 24 + 16)

    if ($entry -eq 0) { Mal "$(Split-Path $exe -Leaf): punto de entrada 0"; continue }

    $proc = Start-Process $exe -PassThru -Wait
    if ($proc.ExitCode -eq 0) {
        Bien "$(Split-Path $exe -Leaf) corrio y salio con 0"
    } else {
        Mal "$(Split-Path $exe -Leaf) salio con 0x$('{0:X}' -f $proc.ExitCode)"
    }
}

# ---------------------------------------------------------------------------
Write-Host ""
Write-Host "=== 4. Mover la carpeta del proyecto ===" -ForegroundColor Cyan

# ⚠ Con rutas absolutas en el .asmproj esto se romperia. Es la garantia que
#   justifica guardarlas relativas.
$movido = "$banco\movido"
Move-Item $p1 $movido

$json = Get-Content "$movido\Con bin.asmproj" -Raw | ConvertFrom-Json
$principal = Join-Path $movido $json.Proyecto.ArchivoPrincipal

if (Test-Path $principal) {
    Bien "el archivo principal se encuentra despues de mover la carpeta"
} else {
    Mal "no se encuentra el principal despues de mover: '$principal'"
}

if ($json.Proyecto.Archivos[0] -match '^[A-Za-z]:') {
    Mal "el .asmproj guardo una ruta ABSOLUTA"
} else {
    Bien "el .asmproj guarda rutas relativas"
}

# ---------------------------------------------------------------------------
Write-Host ""

if ($fallas -eq 0) {
    Write-Host "=== TODO BIEN: proyectos y archivos sueltos conviven ===" -ForegroundColor Green
    exit 0
}

Write-Host "=== $fallas FALLAS ===" -ForegroundColor Red
exit 1
