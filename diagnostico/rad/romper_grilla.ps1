# ============================================================================
# Verifica que el paso 7 de deshacer_en_disenador.ps1 FALLA cuando se deshacen
# las correcciones de la grilla del 23/09.
#
# Mete un defecto por vez en LienzoDisenador.cs, recompila, corre el
# diagnostico y busca la linea MAL que ese defecto tiene que provocar. Al
# final restaura el fuente byte por byte (finally) y recompila el original.
#
# Es para que lo lea yo, no forma parte de la interfaz.
# ============================================================================

$ErrorActionPreference = "Stop"

$raiz   = Split-Path (Split-Path $PSScriptRoot)
$fuente = "$raiz\LienzoDisenador.cs"
$bytesOriginales = [IO.File]::ReadAllBytes($fuente)
$original = [Text.Encoding]::UTF8.GetString($bytesOriginales)
$utf8SinBom = New-Object System.Text.UTF8Encoding($false)

# Cada defecto: texto a reemplazar, reemplazo, y la falla que se espera ver.
$defectos = [ordered]@{
    "sin tolerancia de clic (todo movimiento es arrastre)" = @(
        "        if (_arrastreIniciado) return true;",
        "        if (_arrastreIniciado || true) return true;",
        "MAL\s+un clic con temblor de 1 px ensucio");
    "la manija ajusta el origen aunque no lo mueva (como antes)" = @(
        "        c.X = x;",
        "        c.X = Ajustar(x, AjustarAGrilla);",
        "MAL\s+la manija derecha corrio el origen")
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
        $salida = & "$PSScriptRoot\deshacer_en_disenador.ps1" *>&1 | Out-String
        $ErrorActionPreference = "Stop"

        $males = $salida -split "`n" | Where-Object { $_ -match "MAL" } | ForEach-Object { $_.Trim() }

        if ($salida -match $esperado) {
            Write-Host "  OK   '$nombre' -> lo detecta" -ForegroundColor Green
        } else {
            Write-Host "  MAL  '$nombre' NO lo detecta" -ForegroundColor Red
            $sinDetectar++
        }
        $males | ForEach-Object { Write-Host "         $_" }
    }
}
finally {
    [IO.File]::WriteAllBytes($fuente, $bytesOriginales)
    Compilar
    Write-Host "  (original restaurado y recompilado)"
}

if ($sinDetectar -eq 0) {
    Write-Host "=== TODOS LOS DEFECTOS DETECTADOS ===" -ForegroundColor Green
    exit 0
}

Write-Host "=== $sinDetectar DEFECTOS SIN DETECTAR ===" -ForegroundColor Red
exit 1
