# ============================================================================
# Verifica que ValoresOpcionesTests FALLA cuando se rompe Core\ValoresOpciones.cs.
#
# El ultimo defecto es el que importa para el futuro: agregar una opcion NUEVA
# sin su linea en Leer y en Aplicar tiene que hacer fallar la prueba.
#
# Un defecto por vez; restaura el fuente byte por byte (finally).
#
# Es para que lo lea yo, no forma parte de la interfaz.
# ============================================================================

$ErrorActionPreference = "Stop"

$raiz    = Split-Path $PSScriptRoot
$pruebas = "$raiz\AsmEditor.Tests"
$archivo = "$raiz\Core\ValoresOpciones.cs"
$utf8SinBom = New-Object System.Text.UTF8Encoding($false)

$defectos = @(
    @{ Nombre = "Leer olvida el SDK de 32 bits"
       De = '        SdkLib32 = cfg.SdkLibPath32'; A = '        SdkLib32 = ""' },
    @{ Nombre = "Aplicar olvida Al iniciar"
       De = '        cfg.Ui.AlIniciar = AlIniciar;'; A = '' },
    @{ Nombre = "Aplicar no valida"
       De = '        cfg.EnsureValid();'; A = '' },
    @{ Nombre = "opcion nueva sin conectar"
       De = '    public string SdkLib32 { get; set; } = "";'
       A  = "    public string SdkLib32 { get; set; } = `"`";`n`n    public string OpcionNueva { get; set; } = `"`";" }
)

$original = [IO.File]::ReadAllBytes($archivo)
$sinDetectar = 0

try {
    foreach ($d in $defectos) {
        $texto = [Text.Encoding]::UTF8.GetString($original)
        if (-not $texto.Contains($d.De)) { throw "No encuentro el texto a romper para '$($d.Nombre)': el fuente cambio." }

        [IO.File]::WriteAllText($archivo, $texto.Replace($d.De, $d.A), $utf8SinBom)

        $ErrorActionPreference = "Continue"
        $salida = & dotnet test $pruebas --nologo --filter "FullyQualifiedName~ValoresOpcionesTests" 2>&1 | Out-String
        $ErrorActionPreference = "Stop"

        [IO.File]::WriteAllBytes($archivo, $original)

        if ($salida -match "Con error:\s+([1-9]\d*)") { Write-Host "  OK   '$($d.Nombre)' -> fallan $($Matches[1])" -ForegroundColor Green }
        else {
            $resumen = ($salida -split "`n" | Where-Object { $_ -match "Con error:|error CS" } | Select-Object -First 2) -join " | "
            Write-Host "  MAL  '$($d.Nombre)' NO lo detecta ninguna prueba   [$($resumen.Trim())]" -ForegroundColor Red
            $sinDetectar++
        }
    }
}
finally {
    [IO.File]::WriteAllBytes($archivo, $original)
    Write-Host "  (original restaurado)"
}

if ($sinDetectar -eq 0) { Write-Host "=== TODOS LOS DEFECTOS DETECTADOS ===" -ForegroundColor Green; exit 0 }
Write-Host "=== $sinDetectar DEFECTOS SIN DETECTAR ===" -ForegroundColor Red
exit 1
