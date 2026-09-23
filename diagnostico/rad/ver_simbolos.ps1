# Lee la tabla de simbolos COFF de un .obj y lista los simbolos publicos.
# Sirve para ver con que nombre exacto quedo el punto de entrada.
param([Parameter(Mandatory=$true)][string]$Obj)

$b = [System.IO.File]::ReadAllBytes($Obj)

# IMAGE_FILE_HEADER: Machine(2) NumberOfSections(2) TimeDateStamp(4)
# PointerToSymbolTable(4) NumberOfSymbols(4) ...
$ptrSym = [BitConverter]::ToInt32($b, 8)
$nSym   = [BitConverter]::ToInt32($b, 12)
$strTab = $ptrSym + ($nSym * 18)

"archivo   : $(Split-Path $Obj -Leaf)"
"simbolos  : $nSym"
""

$i = 0
while ($i -lt $nSym) {
    $off = $ptrSym + ($i * 18)

    # Los primeros 8 bytes: nombre corto, o (0,0,0,0)+offset a la tabla de strings
    $nombre = ""
    if ([BitConverter]::ToInt32($b, $off) -eq 0) {
        $so = [BitConverter]::ToInt32($b, $off + 4)
        $p = $strTab + $so
        $fin = $p
        while ($fin -lt $b.Length -and $b[$fin] -ne 0) { $fin++ }
        $nombre = [System.Text.Encoding]::ASCII.GetString($b, $p, $fin - $p)
    } else {
        $raw = [System.Text.Encoding]::ASCII.GetString($b, $off, 8)
        $nombre = $raw.TrimEnd([char]0)
    }

    $valor    = [BitConverter]::ToInt32($b, $off + 8)
    $seccion  = [BitConverter]::ToInt16($b, $off + 12)
    $clase    = $b[$off + 16]
    $nAux     = $b[$off + 17]

    # Clase 2 = IMAGE_SYM_CLASS_EXTERNAL (publico)
    if ($clase -eq 2) {
        $tipo = if ($seccion -eq 0) { "EXTERNO (importado)" } else { "PUBLICO  (definido, seccion $seccion)" }
        "  {0,-24} valor=0x{1:X4}  {2}" -f $nombre, $valor, $tipo
    }

    $i += 1 + $nAux
}
