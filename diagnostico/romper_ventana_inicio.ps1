# ============================================================================
# Verifica que ventana_inicio.ps1 FALLA cuando se rompe el arranque con la
# ventana de inicio (Program.cs y MainForm.cs).
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
    @{ Nombre = "la eleccion de la ventana de inicio no se aplica"; Seccion = "A."; Archivo = "MainForm.cs"
       De = 'Shown += MainForm_Shown;'; A = '' },
    @{ Nombre = "no reabre los archivos de la sesion del proyecto"; Seccion = "A."; Archivo = "MainForm.cs"
       De = 'foreach (var archivo in sesion.Archivos.Where(File.Exists)) OpenPath(archivo);'; A = '' },
    @{ Nombre = "no guarda la sesion del proyecto al salir"; Seccion = "A."; Archivo = "MainForm.cs"
       De = 'Ui.GuardarSesionDeProyecto(ruta, archivos, Math.Max(0, _tabs.SelectedIndex));'; A = '' },
    @{ Nombre = "Esc no cierra el editor"; Seccion = "B."; Archivo = "Program.cs"
       De = 'if (inicio.ShowDialog() != DialogResult.OK || inicio.Eleccion is null)'; A = 'if (inicio.ShowDialog() != DialogResult.OK && false)' },
    @{ Nombre = "Al iniciar = ultimo proyecto no abre nada"; Seccion = "F."; Archivo = "Program.cs"
       De = 'ventana.PrepararInicio(new EleccionInicio(AccionInicio.UltimoProyecto));'; A = '' },
    @{ Nombre = "la linea de comandos no evita la ventana de inicio"; Seccion = "H."; Archivo = "MainForm.cs"
       De = 'AbrioArchivosDeLineaDeComandos = abrioAlguno;'; A = 'AbrioArchivosDeLineaDeComandos = false;' }
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

# El texto de la seccion ("=== A. ..." hasta la proxima "===").
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
        if (-not $texto.Contains($d.De)) { throw "No encuentro el texto a romper para '$($d.Nombre)': el fuente cambio." }

        Write-Host "--- $($d.Nombre)"
        [IO.File]::WriteAllText($f, $texto.Replace($d.De, $d.A), $utf8SinBom)
        Compilar

        $ErrorActionPreference = "Continue"
        $salida = & powershell -NoProfile -ExecutionPolicy Bypass -File "$PSScriptRoot\ventana_inicio.ps1" *>&1 | Out-String
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
