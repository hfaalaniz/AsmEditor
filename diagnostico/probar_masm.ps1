# Comprueba que los comandos que ARMA EL EDITOR para MASM realmente ensamblan,
# enlazan y producen un .exe que corre. No automatiza la interfaz: ejecuta la
# misma cadena que el editor construye, que es lo que puede fallar.
#
# Es para leer yo.

$ErrorActionPreference = 'Stop'

$raiz  = Resolve-Path (Join-Path $PSScriptRoot '..\..')
$fuente = Join-Path $raiz 'masm64.asm'
$obj    = Join-Path $raiz 'masm64.obj'
$exe    = Join-Path $raiz 'masm64.exe'

if (-not (Test-Path $fuente)) { throw "Falta $fuente" }

Remove-Item $obj -ErrorAction SilentlyContinue
Remove-Item $exe -ErrorAction SilentlyContinue

$ml64 = Join-Path $raiz 'masm_x64\ml64.exe'
$link = Join-Path $raiz 'linker_x64\link.exe'
$sdk  = 'C:\Program Files (x86)\Windows Kits\10\Lib\10.0.26100.0\um\x64'

Write-Host "=== 1. Ensamblar (BuildMasmArguments) ==="
$argsAsm = "/nologo /c /Fo `"$obj`" `"$fuente`""
Write-Host "  ml64.exe $argsAsm"
$o = & cmd /c "`"$ml64`" $argsAsm 2>&1"
Write-Host "  exit: $LASTEXITCODE"
$o | ForEach-Object { "    $_" }

if (-not (Test-Path $obj)) { Write-Host "FALLA: no se genero el .obj" -ForegroundColor Red; exit 1 }
Write-Host "  OK: $((Get-Item $obj).Length) bytes" -ForegroundColor Green

Write-Host ""
Write-Host "=== 2. Enlazar (BuildMsvcArguments) ==="
# ⚠ CON /ENTRY: en MASM de 64 bits hace falta. Solo el de 32 bits lo omite,
#   porque ahí el «end main» del fuente ya lo declara en el .obj.
$argsLink = "/NOLOGO /SUBSYSTEM:WINDOWS /ENTRY:main /OUT:`"$exe`" /LIBPATH:`"$sdk`" `"$obj`" kernel32.lib user32.lib"
Write-Host "  link.exe $argsLink"
$o2 = & cmd /c "`"$link`" $argsLink 2>&1"
Write-Host "  exit: $LASTEXITCODE"
$o2 | ForEach-Object { "    $_" }

if (-not (Test-Path $exe)) { Write-Host "FALLA: no se genero el .exe" -ForegroundColor Red; exit 1 }
Write-Host "  OK: $((Get-Item $exe).Length) bytes" -ForegroundColor Green

Write-Host ""
Write-Host "=== 3. Cabecera PE ==="
$b = [System.IO.File]::ReadAllBytes($exe)
$pe = [BitConverter]::ToInt32($b, 0x3C)
$m = [BitConverter]::ToUInt16($b, $pe + 4)
switch ($m) {
    0x8664 { Write-Host "  AMD64 -> 64 bits OK" -ForegroundColor Green }
    0x014c { Write-Host "  i386 -> 32 bits" -ForegroundColor Yellow }
    default { Write-Host ("  maquina 0x{0:X}" -f $m) -ForegroundColor Red }
}

Write-Host ""
Write-Host "=== 4. Ejecutar ==="
$p = Start-Process $exe -PassThru
Start-Sleep -Seconds 2
$p.Refresh()
if ($p.HasExited) {
    Write-Host "  el exe salio con codigo $($p.ExitCode)" -ForegroundColor Yellow
} else {
    Write-Host "  EL EXE CORRE" -ForegroundColor Green
    Stop-Process -Id $p.Id -Force -ErrorAction SilentlyContinue
}
