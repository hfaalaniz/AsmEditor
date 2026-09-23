# ============================================================================
# Verifica que las pruebas del historial de deshacer FALLAN cuando el
# historial esta roto.
#
# Mete un defecto por vez en HistorialDisenador.cs, corre solo sus pruebas y
# restaura el original. Cada defecto tiene que hacer fallar al menos una
# prueba: si alguno pasa en verde, esas pruebas no protegen lo que dicen.
#
# El original se restaura SIEMPRE (finally), aunque se corte a mitad.
#
# Es para que lo lea yo, no forma parte de la interfaz.
# ============================================================================

$ErrorActionPreference = "Stop"

$raiz    = Split-Path (Split-Path $PSScriptRoot)
$fuente  = "$raiz\Core\Disenador\HistorialDisenador.cs"
$pruebas = "$raiz\AsmEditor.Tests"
$bytesOriginales = [IO.File]::ReadAllBytes($fuente)
$original = [Text.Encoding]::UTF8.GetString($bytesOriginales)
$utf8SinBom = New-Object System.Text.UTF8Encoding($false)

$defectos = [ordered]@{
    "registra aunque no haya cambios" =
        @('if (nuevo == _confirmado) return false;', '');
    "un cambio nuevo NO borra el rehacer" =
        @('_rehacer.Clear();', '');
    "al pasar el limite tira el paso MAS NUEVO" =
        @('_deshacer.RemoveAt(0);', '_deshacer.RemoveAt(_deshacer.Count - 1);');
    "nunca esta sucio" =
        @('public bool EstaSucio => _confirmado != _guardado;', 'public bool EstaSucio => false;');
    "guardar no confirma lo pendiente" =
        @("        Confirmar(actual);`n        _guardado = _confirmado;", "        _guardado = _confirmado;")
}

$sinDetectar = 0

try {
    foreach ($nombre in $defectos.Keys) {
        $de, $a = $defectos[$nombre]

        if (-not $original.Contains($de)) {
            throw "No encuentro el texto a romper para '$nombre': el fuente cambio."
        }

        [IO.File]::WriteAllText($fuente, $original.Replace($de, $a), $utf8SinBom)

        # ⚠ xUnit escribe las pruebas que fallan en stderr, y con "Stop" eso
        # cortaba el script justo cuando el defecto SI se detectaba.
        $ErrorActionPreference = "Continue"
        $salida = & dotnet test $pruebas --nologo --filter "FullyQualifiedName~DisenadorHistorialTests" 2>&1 | Out-String
        $ErrorActionPreference = "Stop"
        $resumen = ($salida -split "`n" | Where-Object { $_ -match "Con error:|error CS" } | Select-Object -First 3) -join " | "

        if ($salida -match "Con error:\s+([1-9]\d*)") {
            Write-Host "  OK   '$nombre' -> fallan $($Matches[1])   [$($resumen.Trim())]" -ForegroundColor Green
        } else {
            Write-Host "  MAL  '$nombre' NO lo detecta ninguna prueba   [$($resumen.Trim())]" -ForegroundColor Red
            $sinDetectar++
        }
    }
}
finally {
    # Se restauran los BYTES, no el texto: asi no cambia ni la codificacion ni los saltos de linea.
    [IO.File]::WriteAllBytes($fuente, $bytesOriginales)
    Write-Host "  (original restaurado)"
}

if ($sinDetectar -eq 0) {
    Write-Host "=== TODOS LOS DEFECTOS DETECTADOS ===" -ForegroundColor Green
    exit 0
}

Write-Host "=== $sinDetectar DEFECTOS SIN DETECTAR ===" -ForegroundColor Red
exit 1
