# ============================================================================
# Verifica que las pruebas del buscador de comandos (FiltroComandosTests)
# FALLAN cuando el filtro esta roto.
#
# Mete un defecto por vez en Core\FiltroComandos.cs, corre solo esas pruebas y
# restaura el fuente byte por byte (finally).
#
# Es para que lo lea yo, no forma parte de la interfaz.
# ============================================================================

$ErrorActionPreference = "Stop"

$raiz    = Split-Path $PSScriptRoot
$fuente  = "$raiz\Core\FiltroComandos.cs"
$pruebas = "$raiz\AsmEditor.Tests"
$bytesOriginales = [IO.File]::ReadAllBytes($fuente)
$original = [Text.Encoding]::UTF8.GetString($bytesOriginales)
$utf8SinBom = New-Object System.Text.UTF8Encoding($false)

$defectos = [ordered]@{
    "distingue acentos" =
        @('if (CharUnicodeInfo.GetUnicodeCategory(ch) == UnicodeCategory.NonSpacingMark) continue;', '');
    "muestra los deshabilitados" =
        @('.Where(x => x.c.Habilitado && palabras.All(p => x.ruta.Contains(p)))', '.Where(x => palabras.All(p => x.ruta.Contains(p)))');
    "no ordena por coincidencia" =
        @('.OrderBy(x => Puntaje(x.nombre, q, palabras[0]))', '.OrderBy(x => 0)');
    "deja la marca de tecla de acceso" =
        @("if (texto[i] == '&')", "if (texto[i] == '\0')")
}

$sinDetectar = 0

try {
    foreach ($nombre in $defectos.Keys) {
        $de, $a = $defectos[$nombre]
        if (-not $original.Contains($de)) { throw "No encuentro el texto a romper para '$nombre': el fuente cambio." }

        [IO.File]::WriteAllText($fuente, $original.Replace($de, $a), $utf8SinBom)

        $ErrorActionPreference = "Continue"
        $salida = & dotnet test $pruebas --nologo --filter "FullyQualifiedName~FiltroComandosTests" 2>&1 | Out-String
        $ErrorActionPreference = "Stop"

        if ($salida -match "Con error:\s+([1-9]\d*)") {
            Write-Host "  OK   '$nombre' -> fallan $($Matches[1])" -ForegroundColor Green
        } else {
            $resumen = ($salida -split "`n" | Where-Object { $_ -match "Con error:|error CS" } | Select-Object -First 2) -join " | "
            Write-Host "  MAL  '$nombre' NO lo detecta ninguna prueba   [$($resumen.Trim())]" -ForegroundColor Red
            $sinDetectar++
        }
    }
}
finally {
    [IO.File]::WriteAllBytes($fuente, $bytesOriginales)
    Write-Host "  (original restaurado)"
}

if ($sinDetectar -eq 0) { Write-Host "=== TODOS LOS DEFECTOS DETECTADOS ===" -ForegroundColor Green; exit 0 }
Write-Host "=== $sinDetectar DEFECTOS SIN DETECTAR ===" -ForegroundColor Red
exit 1
