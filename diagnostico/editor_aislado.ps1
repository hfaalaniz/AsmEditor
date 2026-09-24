# ============================================================================
# Un editor AISLADO para los diagnosticos que abren la interfaz.
#
# Se usa con dot-sourcing:
#
#     . "$PSScriptRoot\..\editor_aislado.ps1"     (o la ruta que corresponda)
#     $exe = PrepararEditorAislado
#
# ⚠ POR QUE EXISTE: el editor guarda su configuracion en settings.json AL LADO
# DEL EJECUTABLE (BuildSettings.cs). Los diagnosticos arrancaban el .exe de
# bin\, o sea el de Fabian, y trabajaban sobre SU configuracion: abrian su
# proyecto, le agregaban archivos de prueba (MiPrograma.asmproj quedo con tres
# Prueba.* el 23/09) y le llenaban los recientes.
#
# Esto copia bin\Debug\net8.0-windows a diagnostico\editor_aislado\ (fresco en
# cada corrida, asi siempre es el ultimo build) y le escribe un settings.json
# propio, derivado del de Fabian:
#
#   - SE CONSERVAN las herramientas (NASM, GoLink, MSVC) y los targets, para
#     que ensamblar y enlazar funcione igual;
#   - SE VACIAN el proyecto abierto, los archivos abiertos y los recientes:
#     el editor aislado arranca sin proyecto y no ve nada de Fabian;
#   - la carpeta del explorador pasa a diagnostico\banco_fuentes\;
#   - el target activo pasa a "NASM 64 + GoLink", que es con el que corrieron
#     siempre los diagnosticos (lo elegia MiPrograma). Sin proyecto se tomaria
#     el global de Fabian, que puede ser otro, y la prueba cambiaria de
#     herramientas sin que nadie lo note.
#
# El settings.json de Fabian solo se LEE, nunca se escribe.
#
# Es para que lo lea yo, no forma parte de la interfaz.
# ============================================================================

# -ConservarTargetActivo: deja el target activo de Fabian en vez de pasar a
# "NASM 64 + GoLink". Lo usa compilar_por_interfaz.ps1, que no elige target:
# compila con el que este activo y lo informa, y asi funcionaba antes.
function PrepararEditorAislado([switch]$ConservarTargetActivo) {
    # Este archivo vive en diagnostico\, pero se lo carga desde subcarpetas:
    # se ubica por su propia ruta, no por la del que lo llama.
    $diag    = Split-Path -Parent $PSCommandPath
    $origen  = Join-Path $diag "..\bin\Debug\net8.0-windows"
    $destino = Join-Path $diag "editor_aislado"

    $origen = (Resolve-Path $origen).Path
    if (-not (Test-Path (Join-Path $origen "AsmEditor.exe"))) {
        throw "No existe $origen\AsmEditor.exe. Compila primero."
    }

    # Un editor aislado que quedo abierto de una corrida anterior tiene
    # tomados sus archivos: se cierra antes de pisar la carpeta.
    CerrarEditorAislado

    if (Test-Path $destino) { Remove-Item $destino -Recurse -Force }
    New-Item -ItemType Directory -Path $destino | Out-Null

    # Todo menos la configuracion y sus copias: esas son de Fabian.
    Get-ChildItem $origen -Recurse -File |
        Where-Object { $_.Name -notlike "settings.json*" } |
        ForEach-Object {
            $rel = $_.FullName.Substring($origen.Length).TrimStart('\')
            $dst = Join-Path $destino $rel
            $dir = Split-Path -Parent $dst
            if (-not (Test-Path $dir)) { New-Item -ItemType Directory -Path $dir | Out-Null }
            Copy-Item $_.FullName $dst
        }

    $cfgFabian = Join-Path $origen "settings.json"
    $cfgAislado = Join-Path $destino "settings.json"

    if (Test-Path $cfgFabian) {
        $cfg = Get-Content $cfgFabian -Raw -Encoding UTF8 | ConvertFrom-Json

        if ($null -ne $cfg.Ui) {
            $cfg.Ui.ProyectoAbierto = $null
            $cfg.Ui.OpenFiles       = @()
            $cfg.Ui.ActiveFileIndex = 0
            $cfg.Ui.RecentFiles     = @()
            $cfg.Ui.RecentProjects  = @()

            # Las fechas y sesiones son de los proyectos de Fabian: fuera.
            # Add-Member -Force porque un settings.json anterior a la ventana de
            # inicio no tiene estas claves.
            $cfg.Ui | Add-Member -NotePropertyName FechasProyectos -NotePropertyValue ([pscustomobject]@{}) -Force
            $cfg.Ui | Add-Member -NotePropertyName SesionesProyectos -NotePropertyValue ([pscustomobject]@{}) -Force

            # AlIniciar = 2 (EntornoVacio): sin la ventana de inicio, el IDE
            # arranca directo, como lo esperan las pruebas por interfaz. La que
            # prueba la ventana de inicio lo cambia a 0 en su propia copia.
            $cfg.Ui | Add-Member -NotePropertyName AlIniciar -NotePropertyValue 2 -Force
        }

        # La carpeta del explorador (y donde arrancan los dialogos) pasa a ser
        # banco_fuentes: con la de Fabian, el explorador del editor aislado
        # mostraba y abria SUS archivos.
        $banco = Join-Path $diag "banco_fuentes"
        if (-not (Test-Path $banco)) { New-Item -ItemType Directory -Path $banco | Out-Null }
        if ($cfg.PSObject.Properties.Name -contains "ProjectFolder") { $cfg.ProjectFolder = $banco }

        if (-not $ConservarTargetActivo) {
            $goLink = -1
            for ($i = 0; $i -lt @($cfg.Targets).Count; $i++) {
                if ($cfg.Targets[$i].Name -eq "NASM 64 + GoLink") { $goLink = $i; break }
            }
            if ($goLink -lt 0) { throw "No encuentro el target 'NASM 64 + GoLink' en la configuracion." }
            $cfg.ActiveTargetIndex = $goLink
        }

        $json = $cfg | ConvertTo-Json -Depth 20
        [IO.File]::WriteAllText($cfgAislado, $json, (New-Object System.Text.UTF8Encoding($false)))
    }

    return (Join-Path $destino "AsmEditor.exe")
}

# Los AsmEditor que corren desde la copia aislada. SOLO esos: el de Fabian,
# si esta abierto, no aparece aca.
function EditoresAislados {
    $carpeta = Join-Path (Split-Path -Parent $PSCommandPath) "editor_aislado"

    # ⚠ LA RUTA SE LEE UNA SOLA VEZ Y CON try. Process.Path se calcula en cada
    # lectura: si el proceso termina entre dos lecturas, la segunda da null y
    # "$_.Path -and $_.Path.StartsWith(...)" revienta. Paso el 23/09 al final de
    # probar_combinaciones.ps1, con un editor que se estaba cerrando.
    foreach ($proc in @(Get-Process AsmEditor -ErrorAction SilentlyContinue)) {
        $ruta = $null
        try { $ruta = $proc.Path } catch { }
        if ($ruta -and $ruta.StartsWith($carpeta, [StringComparison]::OrdinalIgnoreCase)) { $proc }
    }
}

# Cierra el editor aislado y espera a que muera de verdad.
#
# ⚠ NUNCA "Stop-Process -Name AsmEditor": eso mata TAMBIEN el editor de Fabian,
# con lo que tenga sin guardar. Los diagnosticos lo hacian asi hasta el 23/09.
function CerrarEditorAislado {
    EditoresAislados | ForEach-Object { try { $_.Kill() } catch { } }

    $espera = [Diagnostics.Stopwatch]::StartNew()
    while ((EditoresAislados) -and $espera.Elapsed.TotalSeconds -lt 10) {
        Start-Sleep -Milliseconds 200
    }
}

# Copia un fuente a diagnostico\banco_fuentes\ (mismo nombre) y devuelve la
# ruta de la copia.
#
# ⚠ LOS DIAGNOSTICOS NO TRABAJAN SOBRE LOS ARCHIVOS DE FABIAN. Compilar borra
# y regenera el .obj y el .exe al lado del fuente, y abrir un archivo que esta
# junto a un .asmproj hace preguntar "¿Abrir el proyecto?", que un Enter del
# script contesta que si. En banco_fuentes no hay ningun .asmproj.
function CopiarFuenteABanco([string]$fuente) {
    $banco = Join-Path (Split-Path -Parent $PSCommandPath) "banco_fuentes"
    if (-not (Test-Path $banco)) { New-Item -ItemType Directory -Path $banco | Out-Null }

    if (-not (Test-Path $fuente)) { throw "Falta el fuente $fuente" }

    $copia = Join-Path $banco (Split-Path $fuente -Leaf)
    Copy-Item $fuente $copia -Force
    return $copia
}
