# Lanza un .exe, espera, y reporta si abrio una ventana visible con titulo.
# Sirve para distinguir "el proceso vive" de "el programa realmente arranco".
param([Parameter(Mandatory=$true)][string]$Exe)

Add-Type @"
using System;
using System.Runtime.InteropServices;
using System.Text;
public class W {
    [DllImport("user32.dll")] public static extern bool IsWindowVisible(IntPtr h);
    [DllImport("user32.dll")] public static extern int GetWindowTextA(IntPtr h, StringBuilder s, int n);
    [DllImport("user32.dll")] public static extern IntPtr GetWindow(IntPtr h, uint c);
    [DllImport("user32.dll")] public static extern int GetClassNameA(IntPtr h, StringBuilder s, int n);
}
"@

$p = Start-Process $Exe -PassThru
Start-Sleep -Milliseconds 1200

if ($p.HasExited) {
    "RESULTADO: el proceso murio. exit code = 0x{0:X}" -f $p.ExitCode
    exit
}

$p.Refresh()
$h = $p.MainWindowHandle

if ($h -eq [IntPtr]::Zero) {
    "RESULTADO: proceso VIVO pero SIN VENTANA (arranco mal o quedo colgado)"
} else {
    $sb = New-Object System.Text.StringBuilder 256
    [void][W]::GetWindowTextA($h, $sb, 256)
    $cls = New-Object System.Text.StringBuilder 256
    [void][W]::GetClassNameA($h, $cls, 256)
    "RESULTADO: VENTANA ABIERTA"
    "  titulo = '$($sb.ToString())'"
    "  clase  = '$($cls.ToString())'"

    # Contar los controles hijos que creo la tabla
    $n = 0
    $hijo = [W]::GetWindow($h, 5)   # GW_CHILD
    while ($hijo -ne [IntPtr]::Zero) {
        $c = New-Object System.Text.StringBuilder 256
        [void][W]::GetClassNameA($hijo, $c, 256)
        $t = New-Object System.Text.StringBuilder 256
        [void][W]::GetWindowTextA($hijo, $t, 256)
        "  hijo: $($c.ToString().PadRight(10)) '$($t.ToString())'"
        $n++
        $hijo = [W]::GetWindow($hijo, 2)   # GW_HWNDNEXT
    }
    "  total de controles hijos = $n"
}

$p.Kill()
