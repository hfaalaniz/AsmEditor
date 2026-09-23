# ============================================================================
# Verifica que probar_barras.ps1 FALLA cuando se rompen las barras.
#
# Mete un defecto por vez en MainForm.cs, recompila, corre el diagnostico y
# busca la falla que ese defecto tiene que provocar. Restaura el fuente byte
# por byte y recompila el original (finally).
#
# Es para que lo lea yo, no forma parte de la interfaz.
# ============================================================================

$ErrorActionPreference = "Stop"

$raiz   = Split-Path $PSScriptRoot
$fuente = "$raiz\MainForm.cs"
$bytesOriginales = [IO.File]::ReadAllBytes($fuente)
$original = [Text.Encoding]::UTF8.GetString($bytesOriginales)
$utf8SinBom = New-Object System.Text.UTF8Encoding($false)

$defectos = [ordered]@{
    "la barra contesta 'cliente' en vez de 'titulo'" = @(
        "        return HTCAPTION;",
        "        return HTCLIENT;",
        "MAL\s+(maximizada|se movio|no se restauro|restauro la)");
    "la barra de estado no recibe el resultado de la compilacion" = @(
        "        _barraEstado.MostrarResultado(_errorList.ErrorCount, _errorList.WarningCount, _errorList.SummaryText);",
        "",
        "MAL\s+tras compilar roto\.asm")
}

function Compilar {
    $ErrorActionPreference = "Continue"
    $s = & dotnet build "$raiz\AsmEditor.csproj" -nologo -v q 2>&1 | Out-String
    $ErrorActionPreference = "Stop"
    if ($s -notmatch "0 Errores") { Write-Host $s; throw "No compilo" }
}

$sinDetectar = 0

try {
    foreach ($nombre in $defectos.Keys) {
        $de, $a, $esperado = $defectos[$nombre]
        if (-not $original.Contains($de)) { throw "No encuentro el texto a romper para '$nombre': el fuente cambio." }

        [IO.File]::WriteAllText($fuente, $original.Replace($de, $a), $utf8SinBom)
        Compilar

        $ErrorActionPreference = "Continue"
        $salida = & "$PSScriptRoot\probar_barras.ps1" *>&1 | Out-String
        $ErrorActionPreference = "Stop"

        if ($salida -match $esperado) { Write-Host "  OK   '$nombre' -> lo detecta" -ForegroundColor Green }
        else { Write-Host "  MAL  '$nombre' NO lo detecta" -ForegroundColor Red; $sinDetectar++ }

        $salida -split "`n" | Where-Object { $_ -match "MAL" } | ForEach-Object { Write-Host "         $($_.Trim())" }
    }
}
finally {
    [IO.File]::WriteAllBytes($fuente, $bytesOriginales)
    Compilar
    Write-Host "  (original restaurado y recompilado)"
}

if ($sinDetectar -eq 0) { Write-Host "=== TODOS LOS DEFECTOS DETECTADOS ===" -ForegroundColor Green; exit 0 }
Write-Host "=== $sinDetectar DEFECTOS SIN DETECTAR ===" -ForegroundColor Red
exit 1
