# ============================================================================
# Verifica que DisenoAcopleTests FALLA cuando se rompe el modelo del acople
# (Core\Acople\DisenoAcople.cs).
#
# Un defecto por vez; restaura el fuente byte por byte (finally).
#
# Es para que lo lea yo, no forma parte de la interfaz.
# ============================================================================

$ErrorActionPreference = "Stop"

$raiz    = Split-Path $PSScriptRoot
$pruebas = "$raiz\AsmEditor.Tests"
$archivo = "$raiz\Core\Acople\DisenoAcople.cs"
$utf8SinBom = New-Object System.Text.UTF8Encoding($false)

$defectos = @(
    @{ Nombre = "registrar pisa lo guardado"
       De = '        if (Paneles.ContainsKey(id)) return;'; A = '' },
    @{ Nombre = "el activo no vuelve a la primera si se oculto"
       De = 'return Activos.TryGetValue(zona, out var id) && visibles.Contains(id) ? id : visibles[0];'
       A  = 'return Activos.TryGetValue(zona, out var id) ? id : visibles[0];' },
    @{ Nombre = "ocultar el ultimo activo no pasa a la anterior"
       De = 'else Activos[zona] = visibles[Math.Min(i, visibles.Count - 1)];'
       A  = 'else Activos[zona] = visibles[0];' },
    # Desde la 3c el texto de Mostrar cambio: el viejo ("u.Visible = true;" +
    # "Activos[u.Zona] = id;") paso a ser el de Fijar y rompia OTRO metodo.
    @{ Nombre = "mostrar no lo deja activo"
       De = '        if (Acoplado(u)) Activos[u.Zona] = id;'; A = '' },
    @{ Nombre = "sin tamano minimo"
       De = 'Tamanos[zona] = Math.Max(TamanoMinimo, pixeles);'; A = 'Tamanos[zona] = pixeles;' },
    @{ Nombre = "mover no lo pone ultimo"
       De = "            u.Zona = zona;`n            u.Orden = SiguienteOrden(zona);"; A = "            u.Zona = zona;" },
    @{ Nombre = "no renumera los ordenes"
       De = '                Paneles[id].Orden = n++;'; A = '                n++;' },
    @{ Nombre = "no descarta activos invalidos"
       De = "            if (!Enum.IsDefined(z) || !Paneles.TryGetValue(Activos[z], out var u) || u.Zona != z)`n                Activos.Remove(z);"
       A  = "            if (!Enum.IsDefined(z))`n                Activos.Remove(z);" },

    # ---- Auto-ocultar (3c) ----
    @{ Nombre = "los auto-ocultos siguen ocupando su zona"
       De = ' && p.Value.AutoOculto == autoOcultos)'; A = ')' },
    @{ Nombre = "auto-ocultar el activo no pasa a la vecina"
       De = "        if (u.Visible) SoltarActivo(u.Zona, id);`n        u.AutoOculto = true;"; A = "        u.AutoOculto = true;" },
    @{ Nombre = "auto-ocultar un auto-oculto cerrado lo vuelve a mostrar"
       De = 'out var u) || u.AutoOculto || u.Flotante) return;'; A = 'out var u) || u.Flotante) return;' },
    # Desde la 3d, Fijar es Acoplar (la chincheta y "Acoplar" hacen lo mismo).
    @{ Nombre = "fijar no lo acopla"
       De = "        u.AutoOculto = false;`n        u.Flotante = false;"; A = "        u.Flotante = false;" },
    @{ Nombre = "fijar no lo deja activo"
       De = "        u.Flotante = false;`n        u.Visible = true;`n        Activos[u.Zona] = id;"; A = "        u.Flotante = false;`n        u.Visible = true;" },
    @{ Nombre = "ocultar pierde el auto-oculto"
       De = "        u.Visible = false;`n    }"; A = "        u.AutoOculto = false;`n        u.Visible = false;`n    }" },
    # NO VA "ocultar un auto-oculto le suelta el activo" (sacar el
    # "&& !u.AutoOculto" de Ocultar): es un defecto EQUIVALENTE. SoltarActivo
    # solo actua si el oculto es el activo anotado de la zona, y un auto-oculto
    # no puede serlo (Activar, Mostrar y AutoOcultar lo impiden; verificado
    # arriba). Sin la guarda el resultado es el mismo: ninguna prueba puede
    # verlo (medido el 28/09: 0 fallas).
    @{ Nombre = "mostrar un auto-oculto lo anota como activo"
       De = '        if (Acoplado(u)) Activos[u.Zona] = id;'; A = '        Activos[u.Zona] = id;' },
    @{ Nombre = "mover no lo acopla"
       De = "        Acoplar(id);`n    }"; A = "        Mostrar(id);`n    }" },
    @{ Nombre = "activar anota un auto-oculto o un flotante"
       De = 'out var u) && Acoplado(u)) Activos[u.Zona] = id;'; A = 'out var u)) Activos[u.Zona] = id;' },

    # ---- Flotar (3d) ----
    @{ Nombre = "los flotantes siguen ocupando su zona"
       De = ' && !p.Value.Flotante && p.Value.AutoOculto'; A = ' && p.Value.AutoOculto' },
    @{ Nombre = "flotar el activo no pasa a la vecina"
       De = "        if (u.Visible && !u.AutoOculto) SoltarActivo(u.Zona, id);`n        u.AutoOculto = false;`n        u.Flotante = true;"
       A  = "        u.AutoOculto = false;`n        u.Flotante = true;" },
    @{ Nombre = "flotar no le saca el auto-oculto"
       De = "        u.AutoOculto = false;`n        u.Flotante = true;"; A = "        u.Flotante = true;" },
    @{ Nombre = "flotar un flotante cerrado lo vuelve a mostrar"
       De = 'out var u) || u.Flotante) return;'; A = 'out var u)) return;' },
    @{ Nombre = "acoplar no lo saca de su ventana"
       De = "        u.Flotante = false;`n        u.Visible = true;"; A = "        u.Visible = true;" },
    @{ Nombre = "acoplado no distingue al flotante"
       De = '=> !u.AutoOculto && !u.Flotante;'; A = '=> !u.AutoOculto;' },
    @{ Nombre = "auto-ocultar un flotante lo cambia"
       De = 'out var u) || u.AutoOculto || u.Flotante) return;'; A = 'out var u) || u.AutoOculto) return;' },
    @{ Nombre = "limites sin tamano minimo"
       De = 'Ancho = Math.Max(TamanoMinimo, ancho),'; A = 'Ancho = ancho,' },
    @{ Nombre = "no olvida limites imposibles"
       De = '                u.LimitesFlotante = null;'; A = '' },
    @{ Nombre = "flotante y auto-oculto a la vez"
       De = '            if (u.Flotante) u.AutoOculto = false;'; A = '' }
)

$original = [IO.File]::ReadAllBytes($archivo)
$sinDetectar = 0

try {
    foreach ($d in $defectos) {
        # El fuente se lee con los finales de linea del disco; los defectos
        # de varias lineas se escriben con `n y se adaptan a CRLF si hace falta.
        $texto = [Text.Encoding]::UTF8.GetString($original)
        $de = $d.De; $a = $d.A
        if ($texto.Contains("`r`n")) { $de = $de.Replace("`n", "`r`n"); $a = $a.Replace("`n", "`r`n") }
        # EXACTAMENTE una vez: Replace cambia todas, y un texto que aparece en
        # otro metodo rompe algo distinto de lo que dice el nombre (paso con
        # "mostrar no lo deja activo" al llegar Fijar, en la 3c).
        $veces = ([regex]::Matches($texto, [regex]::Escape($de))).Count
        if ($veces -ne 1) { throw "El texto a romper para '$($d.Nombre)' aparece $veces veces (tiene que ser 1): el fuente cambio." }

        [IO.File]::WriteAllText($archivo, $texto.Replace($de, $a), $utf8SinBom)

        $ErrorActionPreference = "Continue"
        $salida = & dotnet test $pruebas --nologo --filter "FullyQualifiedName~DisenoAcopleTests" 2>&1 | Out-String
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
