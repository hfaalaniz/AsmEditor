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
       De = 'else Activos[u.Zona] = visibles[Math.Min(i, visibles.Count - 1)];'
       A  = 'else Activos[u.Zona] = visibles[0];' },
    @{ Nombre = "mostrar no lo deja activo"
       De = "        u.Visible = true;`n        Activos[u.Zona] = id;"; A = "        u.Visible = true;" },
    @{ Nombre = "sin tamano minimo"
       De = 'Tamanos[zona] = Math.Max(TamanoMinimo, pixeles);'; A = 'Tamanos[zona] = pixeles;' },
    @{ Nombre = "mover no lo pone ultimo"
       De = "            u.Zona = zona;`n            u.Orden = SiguienteOrden(zona);"; A = "            u.Zona = zona;" },
    @{ Nombre = "no renumera los ordenes"
       De = '                Paneles[id].Orden = n++;'; A = '                n++;' },
    @{ Nombre = "no descarta activos invalidos"
       De = "            if (!Enum.IsDefined(z) || !Paneles.TryGetValue(Activos[z], out var u) || u.Zona != z)`n                Activos.Remove(z);"
       A  = "            if (!Enum.IsDefined(z))`n                Activos.Remove(z);" }
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
        if (-not $texto.Contains($de)) { throw "No encuentro el texto a romper para '$($d.Nombre)': el fuente cambio." }

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
