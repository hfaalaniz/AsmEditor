# ============================================================================
# Prueba de extremo a extremo del generador del disenador.
#
# Genera un formulario con el codigo del editor, lo ensambla con NASM real, lo
# enlaza con GoLink real, lo EJECUTA y verifica que los controles aparecieron
# en la ventana con sus clases, textos e identificadores.
#
# Es la unica prueba que cubre la cadena entera: las de xUnit llegan hasta el
# .obj, esta llega hasta pixeles en pantalla.
#
# ⚠ SE INVOCA DESDE POWERSHELL, NUNCA DESDE GIT BASH: bash convierte el
#   "/entry" de GoLink en una ruta de Windows y el .exe sale con punto de
#   entrada 0 (muere con 0xC0000005).
#
# El formulario lo arma generarform.exe (una consola .NET 8 en generarform/),
# porque Windows PowerShell 5.1 corre sobre .NET Framework y no puede cargar
# un ensamblado de .NET 8 con Add-Type.
# ============================================================================

$ErrorActionPreference = "Stop"

$raiz   = "C:\Users\Fabian\NASM"
$nasm   = "$raiz\nasm.exe"
$golink = "$raiz\GoLink.exe"
$salida = "$PSScriptRoot\generado"
$gen    = "$PSScriptRoot\generarform"

Add-Type @"
using System;
using System.Runtime.InteropServices;
using System.Text;
public class V {
    [DllImport("user32.dll")] public static extern IntPtr GetWindow(IntPtr h, uint c);
    [DllImport("user32.dll")] public static extern int GetClassNameA(IntPtr h, StringBuilder s, int n);
    [DllImport("user32.dll")] public static extern int GetWindowTextA(IntPtr h, StringBuilder s, int n);
    [DllImport("user32.dll")] public static extern int GetDlgCtrlID(IntPtr h);
}
"@

if (Test-Path $salida) { Remove-Item $salida -Recurse -Force }
New-Item -ItemType Directory -Path $salida | Out-Null

# ---------------------------------------------------------------------------
# 1. Generar el formulario con el codigo del editor
# ---------------------------------------------------------------------------
Write-Host ""
Write-Host "=== 1. Generando el formulario con el codigo del editor ===" -ForegroundColor Cyan

Push-Location $gen
$compilacion = & dotnet build -v quiet --nologo 2>&1
$codigo = $LASTEXITCODE
Pop-Location

if ($codigo -ne 0) {
    # ⚠ La salida del compilador va entera: filtrarla esconde la causa.
    $compilacion | Write-Host
    throw "No compilo generarform"
}

$lineas = & "$gen\bin\Debug\net8.0\generarform.exe" $salida
if ($LASTEXITCODE -ne 0) { $lineas | Write-Host; throw "generarform fallo" }

$disenados = @()

foreach ($l in $lineas) {
    if ($l -like "CONTROL`t*") {
        $p = $l -split "`t"
        $disenados += [PSCustomObject]@{
            Id     = [int]$p[1]
            Nombre = $p[2]
            Clase  = $p[3]
            Texto  = if ($p.Count -gt 4) { $p[4] } else { "" }
        }
    } else {
        Write-Host "  $l"
    }
}

Write-Host "  controles disenados: $($disenados.Count)"

# ---------------------------------------------------------------------------
# 2. Ensamblar con NASM
# ---------------------------------------------------------------------------
Write-Host ""
Write-Host "=== 2. Ensamblando con NASM ===" -ForegroundColor Cyan

# ⚠ SE ENSAMBLA DESDE LA CARPETA DEL .asm: el %include del fuente nombra al
# .inc sin ruta, y NASM lo busca relativo al DIRECTORIO DE TRABAJO, no a la
# ubicacion del fuente. Desde otra carpeta falla con "unable to open include
# file". El editor tiene que hacer lo mismo al compilar un formulario.
$obj = "$salida\Prueba.obj"

Push-Location $salida
& $nasm -f win64 "Prueba.asm" -o "Prueba.obj"
$codigo = $LASTEXITCODE
Pop-Location

if ($codigo -ne 0) { throw "NASM fallo con codigo $codigo" }
Write-Host "  OK: $obj" -ForegroundColor Green

# ---------------------------------------------------------------------------
# 3. Enlazar con GoLink
# ---------------------------------------------------------------------------
Write-Host ""
Write-Host "=== 3. Enlazando con GoLink ===" -ForegroundColor Cyan

Push-Location $salida
& $golink /entry main "Prueba.obj" kernel32.dll user32.dll gdi32.dll
$codigo = $LASTEXITCODE
Pop-Location

if ($codigo -ne 0) { throw "GoLink fallo con codigo $codigo" }

$exe = "$salida\Prueba.exe"
if (-not (Test-Path $exe)) { throw "GoLink dijo OK pero no hay .exe" }

# El punto de entrada en 0 significa que el .exe no arranca: se verifica
# aca porque GoLink lo avisa solo como Warning y devuelve 0 igual.
$bytes = [System.IO.File]::ReadAllBytes($exe)
$pe = [BitConverter]::ToInt32($bytes, 0x3C)
$entry = [BitConverter]::ToInt32($bytes, $pe + 24 + 16)

if ($entry -eq 0) { throw "El .exe quedo con AddressOfEntryPoint = 0: no arranca" }

Write-Host ("  OK: {0}  (entry = 0x{1:X})" -f $exe, $entry) -ForegroundColor Green

# ---------------------------------------------------------------------------
# 4. Ejecutar y mirar la ventana
# ---------------------------------------------------------------------------
Write-Host ""
Write-Host "=== 4. Ejecutando y verificando la ventana ===" -ForegroundColor Cyan

$p = Start-Process $exe -PassThru
Start-Sleep -Milliseconds 1500

if ($p.HasExited) {
    throw ("El programa murio al arrancar. Codigo: 0x{0:X}" -f $p.ExitCode)
}

$p.Refresh()
$h = $p.MainWindowHandle

if ($h -eq [IntPtr]::Zero) {
    $p.Kill()
    throw "El programa corre pero no abrio ninguna ventana"
}

$sbT = New-Object System.Text.StringBuilder 256
[void][V]::GetWindowTextA($h, $sbT, 256)
Write-Host "  Ventana: '$($sbT.ToString())'"

$encontrados = @()
$hijo = [V]::GetWindow($h, 5)          # GW_CHILD

while ($hijo -ne [IntPtr]::Zero) {
    $cls = New-Object System.Text.StringBuilder 256
    [void][V]::GetClassNameA($hijo, $cls, 256)
    $txt = New-Object System.Text.StringBuilder 256
    [void][V]::GetWindowTextA($hijo, $txt, 256)

    $encontrados += [PSCustomObject]@{
        Clase = $cls.ToString()
        Texto = $txt.ToString()
        Id    = [V]::GetDlgCtrlID($hijo)
    }

    $hijo = [V]::GetWindow($hijo, 2)   # GW_HWNDNEXT
}

$p.Kill()

Write-Host ""
Write-Host "  Controles encontrados en la ventana real:"
$encontrados | Format-Table -AutoSize | Out-String | Write-Host

# ---------------------------------------------------------------------------
# 5. Comparar lo disenado contra lo que aparecio
# ---------------------------------------------------------------------------
Write-Host "=== 5. Comparando con el diseno ===" -ForegroundColor Cyan

$fallas = 0

if ($encontrados.Count -ne $disenados.Count) {
    Write-Host "  FALLA: se disenaron $($disenados.Count) controles y aparecieron $($encontrados.Count)" -ForegroundColor Red
    $fallas++
} else {
    Write-Host "  OK: aparecieron los $($disenados.Count) controles" -ForegroundColor Green
}

foreach ($c in $disenados) {
    $hallado = $encontrados | Where-Object { $_.Id -eq $c.Id }

    if (-not $hallado) {
        Write-Host "  FALLA: no aparecio '$($c.Nombre)' (id $($c.Id))" -ForegroundColor Red
        $fallas++
        continue
    }

    # Windows reporta la clase capitalizada ("Button" por "BUTTON").
    if ($hallado.Clase -ine $c.Clase) {
        Write-Host "  FALLA: '$($c.Nombre)' deberia ser clase $($c.Clase) y es $($hallado.Clase)" -ForegroundColor Red
        $fallas++
        continue
    }

    if ($hallado.Texto -ne $c.Texto) {
        Write-Host "  FALLA: '$($c.Nombre)' deberia decir '$($c.Texto)' y dice '$($hallado.Texto)'" -ForegroundColor Red
        $fallas++
        continue
    }

    Write-Host "  OK: $($c.Nombre.PadRight(16)) id=$($c.Id)  $($hallado.Clase.PadRight(10)) '$($hallado.Texto)'" -ForegroundColor Green
}

Write-Host ""
if ($fallas -eq 0) {
    Write-Host "=== TODO BIEN: el codigo generado compila, enlaza, corre y muestra los controles disenados ===" -ForegroundColor Green
    exit 0
} else {
    Write-Host "=== $fallas FALLAS ===" -ForegroundColor Red
    exit 1
}
