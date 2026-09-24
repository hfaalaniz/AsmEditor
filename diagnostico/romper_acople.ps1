# ============================================================================
# Verifica que acople.ps1 FALLA cuando se rompe el acople (Acople\*.cs).
#
# Un defecto por vez: lo mete, compila (sin filtrar la salida: si no compila
# se muestra entera y se corta), corre la prueba por interfaz y mira que la
# SECCION que cubre ese defecto tenga un MAL. Restaura cada fuente byte por
# byte (finally) y recompila el original.
#
# Es para que lo lea yo, no forma parte de la interfaz.
# ============================================================================

$ErrorActionPreference = "Stop"

$raiz = Split-Path $PSScriptRoot
$dll  = "$raiz\bin\Debug\net8.0-windows\AsmEditor.dll"
$utf8SinBom = New-Object System.Text.UTF8Encoding($false)

$defectos = @(
    @{ Nombre = "la zona de abajo pasa bajo el explorador"; Seccion = "A."; Archivo = "Acople\AnfitrionAcople.Designer.cs"
       De = "            this.Controls.Add(this.zonaIzquierda);"
       A  = "            this.Controls.Add(this.zonaIzquierda);`n            this.Controls.Add(this.divisorAbajo);`n            this.Controls.Add(this.zonaAbajo);" },
    @{ Nombre = "nunca se ven las pestanas"; Seccion = "A."; Archivo = "Acople\GrupoHerramientas.cs"
       De = 'tira.Visible = _ventanas.Count > 1;'; A = 'tira.Visible = false;' },
    @{ Nombre = "la pestana no cambia el panel activo"; Seccion = "B."; Archivo = "Acople\AnfitrionAcople.cs"
       De = "        Diseno.Activar(v.Id);`n        Actualizar();"; A = "        Actualizar();" },
    @{ Nombre = "el foco queda en la X oculta"; Seccion = "C."; Archivo = "Acople\AnfitrionAcople.cs"
       De = 'if (IsHandleCreated) BeginInvoke(DevolverFocoSiQuedoAfuera);'; A = '' },
    @{ Nombre = "el foco se elige pero no se da"; Seccion = "C."; Archivo = "Acople\AnfitrionAcople.cs"
       De = 'if (elegido is not null && elegido.Focus()) return;'; A = 'return;' },
    @{ Nombre = "pestanas con un solo panel"; Seccion = "D."; Archivo = "Acople\GrupoHerramientas.cs"
       De = 'tira.Visible = _ventanas.Count > 1;'; A = 'tira.Visible = _ventanas.Count > 0;' },
    @{ Nombre = "el divisor no guarda el ancho"; Seccion = "E."; Archivo = "Acople\AnfitrionAcople.cs"
       De = 'Diseno.FijarTamano(z, z == ZonaAcople.Abajo ? ZonaDe(z).Height : ZonaDe(z).Width);'; A = '' }
)

function Compilar {
    $antes = if (Test-Path $dll) { (Get-Item $dll).LastWriteTime } else { [datetime]::MinValue }
    $ErrorActionPreference = "Continue"
    $salida = & dotnet build "$raiz\AsmEditor.csproj" -nologo -v q 2>&1 | Out-String
    $codigo = $LASTEXITCODE
    $ErrorActionPreference = "Stop"
    $despues = (Get-Item $dll).LastWriteTime
    if ($codigo -ne 0 -or $despues -le $antes) {
        Write-Host $salida
        throw "No compilo (codigo $codigo, dll $($despues.ToString('HH:mm:ss')))."
    }
}

function Seccion($salida, $letra) {
    $partes = $salida -split "(?m)^=== "
    return ($partes | Where-Object { $_.StartsWith($letra) } | Select-Object -First 1)
}

$originales = @{}
foreach ($d in $defectos) { $f = "$raiz\$($d.Archivo)"; if (-not $originales.ContainsKey($f)) { $originales[$f] = [IO.File]::ReadAllBytes($f) } }

$sinDetectar = 0

try {
    foreach ($d in $defectos) {
        $f = "$raiz\$($d.Archivo)"
        $texto = [Text.Encoding]::UTF8.GetString($originales[$f])
        $de = $d.De; $a = $d.A
        if ($texto.Contains("`r`n")) { $de = $de.Replace("`n", "`r`n"); $a = $a.Replace("`n", "`r`n") }
        if (-not $texto.Contains($de)) { throw "No encuentro el texto a romper para '$($d.Nombre)': el fuente cambio." }

        Write-Host "--- $($d.Nombre)"
        [IO.File]::WriteAllText($f, $texto.Replace($de, $a), $utf8SinBom)
        Compilar

        $ErrorActionPreference = "Continue"
        $salida = & powershell -NoProfile -ExecutionPolicy Bypass -File "$PSScriptRoot\acople.ps1" *>&1 | Out-String
        $ErrorActionPreference = "Stop"

        [IO.File]::WriteAllBytes($f, $originales[$f])

        $sec = Seccion $salida $d.Seccion
        $males = @(($sec -split "`n") | Where-Object { $_ -match "^\s+MAL " })
        if ($males.Count -gt 0) { Write-Host "  OK   seccion $($d.Seccion) lo detecta: $($males[0].Trim())" -ForegroundColor Green }
        else {
            Write-Host "  MAL  seccion $($d.Seccion) NO lo detecta" -ForegroundColor Red
            Write-Host $sec
            $sinDetectar++
        }
    }
}
finally {
    foreach ($f in $originales.Keys) { [IO.File]::WriteAllBytes($f, $originales[$f]) }
    Write-Host "  (originales restaurados; recompilando)"
    Compilar
}

if ($sinDetectar -eq 0) { Write-Host "=== TODOS LOS DEFECTOS DETECTADOS ===" -ForegroundColor Green; exit 0 }
Write-Host "=== $sinDetectar DEFECTOS SIN DETECTAR ===" -ForegroundColor Red
exit 1
