# ============================================================================
# Verifica que RecientesInicioTests FALLA cuando se rompe la logica de la
# ventana de inicio (Core\RecientesInicio.cs y lo agregado a Core\UiState.cs).
#
# Un defecto por vez; restaura cada fuente byte por byte (finally).
#
# Es para que lo lea yo, no forma parte de la interfaz.
# ============================================================================

$ErrorActionPreference = "Stop"

$raiz    = Split-Path $PSScriptRoot
$pruebas = "$raiz\AsmEditor.Tests"
$utf8SinBom = New-Object System.Text.UTF8Encoding($false)

$defectos = @(
    @{ Nombre = "la semana empieza el domingo"; Archivo = "Core\RecientesInicio.cs"
       De = 'int desdeLunes = ((int)hoy.DayOfWeek + 6) % 7;'; A = 'int desdeLunes = (int)hoy.DayOfWeek;' },
    @{ Nombre = "ignora la fecha guardada"; Archivo = "Core\RecientesInicio.cs"
       De = 'var fecha = BuscarFecha(fechas, ruta) ?? fechaDelArchivo(ruta);'; A = 'var fecha = fechaDelArchivo(ruta);' },
    @{ Nombre = "el filtro distingue acentos"; Archivo = "Core\RecientesInicio.cs"
       De = 'var texto = FiltroComandos.Normalizar(p.Nombre + " " + p.Carpeta);'; A = 'var texto = (p.Nombre + " " + p.Carpeta).ToLowerInvariant();' },
    @{ Nombre = "quitar de la lista deja la fecha"; Archivo = "Core\UiState.cs"
       De = "        QuitarClave(FechasProyectos, path);`n        QuitarClave(SesionesProyectos, path);"; A = "        QuitarClave(SesionesProyectos, path);" },
    @{ Nombre = "no poda fechas viejas"; Archivo = "Core\UiState.cs"
       De = 'foreach (var k in FechasProyectos.Keys.Where(k => !Vigente(k)).ToList()) FechasProyectos.Remove(k);'; A = '' }
)

$originales = @{}
foreach ($d in $defectos) { $f = "$raiz\$($d.Archivo)"; if (-not $originales.ContainsKey($f)) { $originales[$f] = [IO.File]::ReadAllBytes($f) } }

$sinDetectar = 0

try {
    foreach ($d in $defectos) {
        $f = "$raiz\$($d.Archivo)"
        $texto = [Text.Encoding]::UTF8.GetString($originales[$f])
        if (-not $texto.Contains($d.De)) { throw "No encuentro el texto a romper para '$($d.Nombre)': el fuente cambio." }

        [IO.File]::WriteAllText($f, $texto.Replace($d.De, $d.A), $utf8SinBom)

        $ErrorActionPreference = "Continue"
        $salida = & dotnet test $pruebas --nologo --filter "FullyQualifiedName~RecientesInicioTests" 2>&1 | Out-String
        $ErrorActionPreference = "Stop"

        [IO.File]::WriteAllBytes($f, $originales[$f])

        if ($salida -match "Con error:\s+([1-9]\d*)") { Write-Host "  OK   '$($d.Nombre)' -> fallan $($Matches[1])" -ForegroundColor Green }
        else {
            $resumen = ($salida -split "`n" | Where-Object { $_ -match "Con error:|error CS" } | Select-Object -First 2) -join " | "
            Write-Host "  MAL  '$($d.Nombre)' NO lo detecta ninguna prueba   [$($resumen.Trim())]" -ForegroundColor Red
            $sinDetectar++
        }
    }
}
finally {
    foreach ($f in $originales.Keys) { [IO.File]::WriteAllBytes($f, $originales[$f]) }
    Write-Host "  (originales restaurados)"
}

if ($sinDetectar -eq 0) { Write-Host "=== TODOS LOS DEFECTOS DETECTADOS ===" -ForegroundColor Green; exit 0 }
Write-Host "=== $sinDetectar DEFECTOS SIN DETECTAR ===" -ForegroundColor Red
exit 1
