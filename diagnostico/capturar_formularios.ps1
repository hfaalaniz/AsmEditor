# Abre los dos formularios de configuración y los captura, para verlos de verdad.
#
# No usa la interfaz del editor: instancia los Form directamente y los dibuja.
# Así se ve el layout —que es lo que el compilador no verifica— sin pelear con
# los menús de WinForms.
#
# ⚠ CORRE CON dotnet, NO CON powershell.exe: el assembly es .NET 8 y Windows
#   PowerShell 5.1 no lo puede cargar.
#
# Es para leer yo.

param([string]$Salida = 'C:\Users\Fabian\NASM\AsmEditor\diagnostico')

$ErrorActionPreference = 'Stop'

$proyecto = 'C:\Users\Fabian\NASM\AsmEditor'
$tmp = Join-Path $env:TEMP ('formshot_' + [Guid]::NewGuid().ToString('N').Substring(0,8))
New-Item -ItemType Directory -Path $tmp -Force | Out-Null

# Un proyecto de consola mínimo que referencia al editor y dibuja los forms.
@"
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net8.0-windows</TargetFramework>
    <UseWindowsForms>true</UseWindowsForms>
    <Nullable>enable</Nullable>
    <!-- Sin esto no entran los using implícitos de WinForms (Form, Application,
         Point, Bitmap...) y no compila. -->
    <ImplicitUsings>enable</ImplicitUsings>
    <AssemblyName>formshot</AssemblyName>
  </PropertyGroup>
  <ItemGroup>
    <ProjectReference Include="$proyecto\AsmEditor.csproj" />
  </ItemGroup>
</Project>
"@ | Set-Content (Join-Path $tmp 'formshot.csproj') -Encoding utf8

@"
using System.Drawing.Imaging;
using AsmEditor;
using AsmEditor.Core;

var salida = args.Length > 0 ? args[0] : ".";
ApplicationConfiguration.Initialize();

var settings = BuildSettings.Load();

Capturar(new SettingsForm(settings), Path.Combine(salida, "form_configuracion.png"));
Capturar(new TargetEditorForm(settings), Path.Combine(salida, "form_targets.png"));

Console.WriteLine("listo");

static void Capturar(Form f, string ruta)
{
    f.StartPosition = FormStartPosition.Manual;
    f.Location = new Point(-2000, -2000);   // fuera de pantalla
    f.Show();
    Application.DoEvents();
    Thread.Sleep(600);
    Application.DoEvents();

    var bmp = new Bitmap(f.Width, f.Height);
    f.DrawToBitmap(bmp, new Rectangle(0, 0, f.Width, f.Height));
    bmp.Save(ruta, ImageFormat.Png);
    bmp.Dispose();

    Console.WriteLine($"{Path.GetFileName(ruta)}  {f.Width}x{f.Height}");
    f.Close();
    f.Dispose();
}
"@ | Set-Content (Join-Path $tmp 'Program.cs') -Encoding utf8

Push-Location $tmp
try {
    & dotnet run --project formshot.csproj -- $Salida
}
finally {
    Pop-Location
    Remove-Item $tmp -Recurse -Force -ErrorAction SilentlyContinue
}
