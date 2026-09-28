# ============================================================================
# Verifica que acople.ps1 FALLA cuando se rompe el acople (Acople\*.cs).
#
# Un defecto por vez: lo mete, compila (sin filtrar la salida: si no compila
# se muestra entera y se corta), corre la prueba por interfaz y mira que la
# SECCION que cubre ese defecto tenga un MAL. Restaura cada fuente byte por
# byte (finally) y recompila el original.
#
# -Solo "texto","otro": corre solo los defectos cuyo nombre contiene alguno
# (para repetir unos pocos sin esperar los ~25 minutos de todos).
# -Seccion "H.": solo los de esa seccion de acople.ps1 (la I, acoplar
# arrastrando, corre al final de acople.ps1 con su propio editor).
#
# Es para que lo lea yo, no forma parte de la interfaz.
# ============================================================================

param([string[]]$Solo = @(), [string]$Seccion = "")

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
    # Desde la 3c esa linea esta tambien en Plegar y en la chincheta: se
    # ancla con el Avisar() que la sigue, que solo tiene la de Ocultar.
    @{ Nombre = "el foco queda en la X oculta"; Seccion = "C."; Archivo = "Acople\AnfitrionAcople.cs"
       De = "if (IsHandleCreated) BeginInvoke(DevolverFocoSiQuedoAfuera);`n        Avisar();"; A = "Avisar();" },
    @{ Nombre = "el foco se elige pero no se da"; Seccion = "C."; Archivo = "Acople\AnfitrionAcople.cs"
       De = 'if (elegido is not null && elegido.Focus()) return;'; A = 'return;' },
    @{ Nombre = "pestanas con un solo panel"; Seccion = "D."; Archivo = "Acople\GrupoHerramientas.cs"
       De = 'tira.Visible = _ventanas.Count > 1;'; A = 'tira.Visible = _ventanas.Count > 0;' },
    @{ Nombre = "el divisor no guarda el ancho"; Seccion = "E."; Archivo = "Acople\AnfitrionAcople.cs"
       De = 'Diseno.FijarTamano(z, z == ZonaAcople.Abajo ? ZonaDe(z).Height : ZonaDe(z).Width);'; A = '' },

    # ---- Auto-ocultar (3c) ----
    @{ Nombre = "el foco queda en la chincheta estacionada"; Seccion = "F."; Archivo = "Acople\AnfitrionAcople.cs"
       De = '            if (IsHandleCreated) BeginInvoke(DevolverFocoSiQuedoAfuera);'; A = '' },
    # NO VA "plegar con el foco adentro lo deja afuera" (sacar el BeginInvoke
    # de Plegar): medido el 28/09, sin esa linea el foco IGUAL vuelve al IDE (a la
    # salida, donde estaba antes) y los atajos siguen vivos. El unico camino
    # que pliega con el foco adentro es Alternar (Ctrl+B / Ver), y ahi WinForms
    # ya lo devuelve al sacar el panel. Los otros caminos no llegan con el
    # foco: la X devuelve el suyo (Ocultar), la chincheta lo da (Fijar) y
    # tmrPlegar no pliega si el foco esta adentro. Queda como resguardo.
    @{ Nombre = "el raton encima no despliega"; Seccion = "F."; Archivo = "Acople\AnfitrionAcople.cs"
       De = 'else Desplegar(id, enfocar: false);'; A = 'else tmrDesplegar.Stop();' },
    # Las DOS lineas: sacar solo el Stop() es un defecto equivalente (con
    # _porDesplegar en null el Tick no despliega nada; medido el 28/09).
    @{ Nombre = "pasar rapido despliega igual"; Seccion = "F."; Archivo = "Acople\AnfitrionAcople.cs"
       De = "        if (_porDesplegar != id) return;`n        tmrDesplegar.Stop();`n        _porDesplegar = null;"; A = "        if (_porDesplegar != id) return;" },
    @{ Nombre = "no se pliega nunca"; Seccion = "F."; Archivo = "Acople\AnfitrionAcople.cs"
       De = 'if (BordeDe(zona).RectanguloEnPantalla(_desplegado).Contains(raton)) return;'; A = 'return;' },
    @{ Nombre = "se pliega aunque tenga el foco"; Seccion = "F."; Archivo = "Acople\AnfitrionAcople.cs"
       De = 'if (pnlDesplegado.ContainsFocus || v.MenuAbierto) return;'; A = 'if (v.MenuAbierto) return;' },
    @{ Nombre = "desplegado pegado al borde contrario"; Seccion = "F."; Archivo = "Acople\AnfitrionAcople.cs"
       De = 'new(a.Right - ancho, a.Top, ancho, a.Height)'; A = 'new(a.Left, a.Top, ancho, a.Height)' },
    @{ Nombre = "el clic en la pestana no despliega"; Seccion = "F."; Archivo = "Acople\AnfitrionAcople.cs"
       De = '=> Desplegar(id, enfocar: true);'; A = '=> _ = id;' },
    @{ Nombre = "Ctrl+B cierra el auto-oculto"; Seccion = "F."; Archivo = "Acople\AnfitrionAcople.cs"
       De = 'else if (!Diseno.EstaAutoOculto(id)) Ocultar(id);'; A = 'else if (true) Ocultar(id);' },
    @{ Nombre = "la chincheta del auto-oculto no lo fija"; Seccion = "F."; Archivo = "Acople\AnfitrionAcople.cs"
       De = '            Diseno.Fijar(v.Id);'; A = '            Diseno.AutoOcultar(v.Id);' },
    @{ Nombre = "sin franja abajo"; Seccion = "G."; Archivo = "Acople\AnfitrionAcople.cs"
       De = 'borde.Visible = pestanas.Count > 0;'; A = 'borde.Visible = pestanas.Count > 0 && z != ZonaAcople.Abajo;' },

    # ---- Flotar (3d) ----
    # NO VAN (equivalentes hoy, documentados el 28/09):
    #  - la excepcion de Alt+F4 en VentanaFlotante.ProcessCmdKey: el menu
    #    principal no tiene Alt+F4; importaria si algun dia "Salir" lo tuviera.
    #  - Soltar() en VentanaFlotante_FormClosed: solo llega cerrando el editor,
    #    cuando el panel ya no se usa (Cerrar() suelta antes en los demas casos).
    #  - ActivarFlotante en Mostrar: con enfocar, la rama siguiente le da el
    #    foco al panel y eso activa su ventana; sin enfocar (Ctrl+B con la
    #    flotante cerrada) la ventana es nueva y Show la activa. Medido.
    #  - FindForm().Activate() en AcoplarPanel: cerrar la flotante activa ya
    #    le devuelve la activacion al editor, y v.Enfocar() hace el resto.
    #    Medido (y "acoplar no le da el foco al panel" SI se detecta).
    @{ Nombre = "doble clic no flota"; Seccion = "H."; Archivo = "Acople\VentanaHerramienta.cs"
       De = '        else FlotarPedido?.Invoke(this, EventArgs.Empty);'; A = '' },
    @{ Nombre = "la flotante con titulo nativo"; Seccion = "H."; Archivo = "Acople\VentanaFlotante.cs"
       De = '                p.rgrc0.top = arriba;'; A = '' },
    @{ Nombre = "chincheta flotando"; Seccion = "H."; Archivo = "Acople\VentanaHerramienta.cs"
       De = 'btnChincheta.Visible = !value;'; A = 'btnChincheta.Visible = true;' },
    @{ Nombre = "sin reenvio de atajos"; Seccion = "H."; Archivo = "Acople\VentanaFlotante.cs"
       De = 'return pareceAtajo && Owner is IAtajosDelEditor editor && editor.EjecutarAtajo(keyData);'; A = 'return false;' },
    @{ Nombre = "Ctrl+B con foco en la flotante no vuelve al editor"; Seccion = "H."; Archivo = "Acople\AnfitrionAcople.cs"
       De = 'if (_flotantes.TryGetValue(id, out var f) && f.ContainsFocus) FindForm()?.Activate();'; A = 'if (false && _flotantes.TryGetValue(id, out var f) && f.ContainsFocus) FindForm()?.Activate();' },
    @{ Nombre = "Alt+F4 cierra la flotante de verdad"; Seccion = "H."; Archivo = "Acople\VentanaFlotante.cs"
       De = "        e.Cancel = true;`n        CierrePedido?.Invoke(this, EventArgs.Empty);"; A = "" },
    @{ Nombre = "no recuerda donde quedo"; Seccion = "H."; Archivo = "Acople\AnfitrionAcople.cs"
       De = '        Diseno.GuardarLimites(v.Id, f.Left, f.Top, f.Width, f.Height);'; A = '' },
    @{ Nombre = "el arrastre de la flotante queda atrasado"; Seccion = "H."; Archivo = "Acople\AnfitrionAcople.cs"
       De = 'movida.Left + enPantalla.X - v.PuntoApretado.X,'; A = 'movida.Left,' },
    @{ Nombre = "sacar arrastrando no la pone bajo el raton"; Seccion = "H."; Archivo = "Acople\AnfitrionAcople.cs"
       De = 'new Point(p.X - tamano.Width / 2, p.Y - 4 - 13)'; A = 'new Point(p.X - tamano.Width / 2, p.Y + 200)' },
    # Desde la 3e las dos lineas estan tambien al acoplar arrastrando: se
    # ancla con el comentario de la funcion que sigue a AcoplarPanel.
    @{ Nombre = "acoplar no le da el foco al panel"; Seccion = "H."; Archivo = "Acople\AnfitrionAcople.cs"
       De = "        FindForm()?.Activate();`n        v.Enfocar();`n        Avisar();`n    }`n`n    /// <summary>Su ventana al frente"
       A  = "        FindForm()?.Activate();`n        Avisar();`n    }`n`n    /// <summary>Su ventana al frente" },
    @{ Nombre = "cerrar el editor esconde el panel"; Seccion = "H."; Archivo = "Acople\VentanaFlotante.cs"
       De = 'if (_cerrandoDesdeAnfitrion || e.CloseReason != CloseReason.UserClosing) return;'; A = 'if (_cerrandoDesdeAnfitrion) return;' },

    # ---- Acoplar arrastrando (3e) ----
    # La geometria (donde van las guias, la vista previa) la cubren las
    # pruebas unitarias: romper_diseno_acople.ps1. Aca, lo que la une a la
    # interfaz.
    # NO VA "el guardado de la flotante no mira si se acoplo" (sacar el
    # "if (Panel is not null)" antes de Movida en VentanaFlotante): el
    # anfitrion ya descarta una flotante sin panel en Flotante_Movida. Es
    # equivalente; el orden (Soltada antes que Movida) si se rompe abajo.
    @{ Nombre = "las guias no aparecen al arrastrar"; Seccion = "I."; Archivo = "Acople\AnfitrionAcople.cs"
       De = '            f.Moviendo += Flotante_Moviendo;'; A = '' },
    @{ Nombre = "sin vista previa"; Seccion = "I."; Archivo = "Acople\AnfitrionAcople.cs"
       De = "        if (guia is { } g) _vistaPrevia.Mostrar(ARectangulo(GeometriaAcople.VistaPrevia(g.Zona, Disposicion())), duenio);`n        else _vistaPrevia.Hide();"
       A  = "        _vistaPrevia.Hide();" },
    @{ Nombre = "soltar sobre una guia no acopla"; Seccion = "I."; Archivo = "Acople\AnfitrionAcople.cs"
       De = '        Diseno.Mover(v.Id, guia.Zona);'; A = '' },
    @{ Nombre = "las guias quedan a la vista al soltar"; Seccion = "I."; Archivo = "Acople\AnfitrionAcople.cs"
       De = "        _guias.Hide();`n"; A = "" },
    @{ Nombre = "acoplar arrastrando no le da el foco al panel"; Seccion = "I."; Archivo = "Acople\AnfitrionAcople.cs"
       De = "        v.Enfocar();`n        Avisar();`n    }`n`n    private void OcultarGuias()"
       A  = "        Avisar();`n    }`n`n    private void OcultarGuias()" },
    @{ Nombre = "Esc acopla igual"; Seccion = "I."; Archivo = "Acople\VentanaFlotante.cs"
       De = 'Soltada?.Invoke(this, Bounds == _alEmpezar);'; A = 'Soltada?.Invoke(this, false);' },
    # Guarda donde se la solto ANTES de acoplarla: pisa el ultimo lugar
    # donde floto (I.5).
    @{ Nombre = "acoplar arrastrando pisa el ultimo lugar flotante"; Seccion = "I."; Archivo = "Acople\VentanaFlotante.cs"
       De = "                if (_arrastrada)`n                {`n                    _arrastrada = false;"
       A  = "                if (_arrastrada)`n                {`n                    Movida?.Invoke(this, EventArgs.Empty);`n                    _arrastrada = false;" }
)

if ($Solo) { $defectos = @($defectos | Where-Object { $n = $_.Nombre; @($Solo | Where-Object { $n -like "*$_*" }).Count -gt 0 }) }
if ($Seccion) { $defectos = @($defectos | Where-Object { $_.Seccion -eq $Seccion }) }
if ($defectos.Count -eq 0) { throw "Ningun defecto coincide con '$Solo' / '$Seccion'." }

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
        # EXACTAMENTE una vez: Replace cambia todas, y un texto repetido rompe
        # mas de lo que dice el nombre (paso con el foco de la X en la 3c).
        $veces = ([regex]::Matches($texto, [regex]::Escape($de))).Count
        if ($veces -ne 1) { throw "El texto a romper para '$($d.Nombre)' aparece $veces veces (tiene que ser 1): el fuente cambio." }

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
