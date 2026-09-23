# Abre el editor con un par de archivos y guarda una captura PNG de la ventana,
# para poder mirar el resultado real del dibujo en vez de deducirlo.
# Es para leer yo.

param(
    [string]$Salida = (Join-Path $PSScriptRoot 'captura.png')
)

$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Windows.Forms, System.Drawing

# Editor aislado: no usa la configuracion ni los proyectos de Fabian.
# Prepararlo cierra el aislado de una corrida anterior; el de Fabian, si esta
# abierto, no se toca (antes se hacia Stop-Process -Name AsmEditor y lo mataba).
. "$PSScriptRoot\editor_aislado.ps1"
$exe = PrepararEditorAislado

# COPIAS de dos archivos reales, con el mismo nombre para que las pestanas se
# vean igual. Los originales estan junto a MiPrograma.asmproj: abrirlos hacia
# preguntar "¿Abrir el proyecto?", el Enter siguiente contestaba que si, y el
# texto que se tipeaba despues terminaba dentro de un archivo de Fabian.
$archivo1 = CopiarFuenteABanco "C:\Users\Fabian\NASM\Win6Win.asm"
$archivo2 = CopiarFuenteABanco "C:\Users\Fabian\NASM\window_debug.asm"

# ⚠ LOS ARCHIVOS VAN POR LINEA DE COMANDOS, NO TIPEADOS EN CTRL+O. Medido el
# 23/09: con Ctrl+O + ruta + Enter el titulo final mostraba OTRO archivo
# (continuar.txt, de Fabian, modificado) y el proyecto MiPrograma abierto. No
# se averiguo por que camino; se dejo de depender del dialogo. Por linea de
# comandos el editor los abre en el arranque, sin dialogos ni preguntas
# (MainForm.AbrirAlIniciarInterno).
$p = Start-Process $exe -ArgumentList "`"$archivo1`"", "`"$archivo2`"" -PassThru
Start-Sleep -Seconds 5

$p.Refresh()
Write-Host "Titulo: '$($p.MainWindowTitle)'"

# Capturamos el rectángulo de la ventana
Add-Type @"
using System;
using System.Runtime.InteropServices;
public class Cap {
    [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr h, out RECT r);
    [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr h);
    [StructLayout(LayoutKind.Sequential)] public struct RECT { public int Left, Top, Right, Bottom; }
}
"@

[void][Cap]::SetForegroundWindow($p.MainWindowHandle)
Start-Sleep -Milliseconds 700

$r = New-Object Cap+RECT
[void][Cap]::GetWindowRect($p.MainWindowHandle, [ref]$r)
$w = $r.Right - $r.Left
$h = $r.Bottom - $r.Top
Write-Host "Ventana: ${w}x${h} en ($($r.Left),$($r.Top))"

$bmp = New-Object System.Drawing.Bitmap $w, $h
$g = [System.Drawing.Graphics]::FromImage($bmp)
$g.CopyFromScreen($r.Left, $r.Top, 0, 0, (New-Object System.Drawing.Size $w, $h))
$bmp.Save($Salida, [System.Drawing.Imaging.ImageFormat]::Png)
$g.Dispose(); $bmp.Dispose()

Write-Host "Captura guardada: $Salida"
CerrarEditorAislado
