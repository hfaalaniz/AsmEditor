# Regenera los recursos de marca a partir del código de LogoEditor.cs:
#   recursos/AsmEditor.ico   (7 tamaños, el que Windows embebe en el .exe)
#   recursos/logo-*.png      (para documentación y web)
#   diagnostico/logo_prueba.png  (hoja de prueba para mirar el logo)
#
# Hay que correrlo cuando se toca el dibujo. NO hace falta Inkscape ni
# ImageMagick: el .ico sale del mismo código que dibuja en pantalla.
#
# ⚠ Los SVG de recursos/ son la FUENTE DE DISEÑO y NO se generan: si se cambió
#   el dibujo, hay que actualizarlos a mano para que no queden desfasados.
#   Ver recursos/LEEME.md.

$ErrorActionPreference = 'Stop'

$raiz = Resolve-Path (Join-Path $PSScriptRoot '..')
$tmp = Join-Path $env:TEMP "asmeditor_recursos_$PID"

Write-Host "Generando en una carpeta temporal: $tmp"
New-Item -ItemType Directory -Force $tmp | Out-Null

try {
    # Proyecto mínimo que compila solo LogoEditor.cs
    @"
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net8.0-windows</TargetFramework>
    <UseWindowsForms>true</UseWindowsForms>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>
  <ItemGroup>
    <Compile Include="$raiz\LogoEditor.cs" />
  </ItemGroup>
</Project>
"@ | Set-Content (Join-Path $tmp 'gen.csproj') -Encoding utf8

    @"
using AsmEditor;

var recursos = @"$raiz\recursos";
var diag = @"$raiz\diagnostico";
Directory.CreateDirectory(recursos);
Directory.CreateDirectory(diag);

var rutaIco = Path.Combine(recursos, "AsmEditor.ico");
using (var ico = LogoEditor.CrearIcono())
using (var fs = File.Create(rutaIco))
    ico.Save(fs);
Console.WriteLine(`$"  .ico  {new FileInfo(rutaIco).Length} bytes");

foreach (int lado in new[] { 32, 128, 256 })
{
    using var png = LogoEditor.RenderizarPng(lado);
    png.Save(Path.Combine(recursos, `$"logo-{lado}.png"), System.Drawing.Imaging.ImageFormat.Png);
    Console.WriteLine(`$"  logo-{lado}.png");
}

int[] tamanos = { 16, 20, 24, 32, 48, 64, 128 };
using (var bmp = new Bitmap(660, 440))
{
    using var g = Graphics.FromImage(bmp);
    g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
    using (var b1 = new SolidBrush(Color.FromArgb(11, 13, 16))) g.FillRectangle(b1, 0, 0, 660, 220);
    using (var b2 = new SolidBrush(Color.FromArgb(244, 245, 247))) g.FillRectangle(b2, 0, 220, 660, 220);

    using var f = new Font("Segoe UI", 8f);
    using var tc = new SolidBrush(Color.Gainsboro);
    using var to = new SolidBrush(Color.FromArgb(30, 30, 30));

    for (int fila = 0; fila < 2; fila++)
    {
        int baseY = fila == 0 ? 24 : 244;
        var pincel = fila == 0 ? tc : to;
        int x = 18;
        foreach (int t in tamanos)
        {
            using var img = LogoEditor.RenderizarPng(t);
            g.DrawImage(img, x, baseY);
            g.DrawString(t.ToString(), f, pincel, x, baseY + t + 3);
            x += Math.Max(t, 26) + 18;
        }
    }

    using (var grande = LogoEditor.RenderizarPng(160)) g.DrawImage(grande, 470, 34);
    g.DrawString("160 (splash)", f, tc, 470, 200);
    using (var g2 = LogoEditor.RenderizarPng(160)) g.DrawImage(g2, 470, 250);

    bmp.Save(Path.Combine(diag, "logo_prueba.png"), System.Drawing.Imaging.ImageFormat.Png);
    Console.WriteLine("  logo_prueba.png");
}
"@ | Set-Content (Join-Path $tmp 'Program.cs') -Encoding utf8

    Push-Location $tmp
    & dotnet run 2>&1 | Where-Object { $_ -notmatch '^\s*$' }
    Pop-Location
}
finally {
    Remove-Item $tmp -Recurse -Force -ErrorAction SilentlyContinue
}

Write-Host ""
Write-Host "Listo. Recursos en: $raiz\recursos"
