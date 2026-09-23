# Qué ve el descubrimiento de LinkerLocator en esta máquina.
#
# Sirve para comprobar que las pruebas no están pasando en vacío: varias se
# saltean solas si no hallan Visual Studio, así que hay que mirar el número.
#
# ⚠ NO USA Add-Type CONTRA EL DLL DEL EDITOR. El assembly es .NET 8 y Windows
#   PowerShell 5.1 corre sobre .NET Framework: no lo puede cargar y falla con
#   ReflectionTypeLoadException. Por eso compila un proyecto de consola mínimo
#   que referencia al editor y lo ejecuta con dotnet, igual que
#   capturar_formularios.ps1.
#
# Es para leer yo.

$ErrorActionPreference = 'Stop'

$proyecto = 'C:\Users\Fabian\NASM\AsmEditor'
$carpetaProyecto = 'C:\Users\Fabian\NASM'

$tmp = Join-Path $env:TEMP ('tclist_' + [Guid]::NewGuid().ToString('N').Substring(0,8))
New-Item -ItemType Directory -Path $tmp -Force | Out-Null

@"
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net8.0-windows</TargetFramework>
    <UseWindowsForms>true</UseWindowsForms>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <AssemblyName>tclist</AssemblyName>
  </PropertyGroup>
  <ItemGroup>
    <ProjectReference Include="$proyecto\AsmEditor.csproj" />
  </ItemGroup>
</Project>
"@ | Set-Content (Join-Path $tmp 'tclist.csproj') -Encoding utf8

@"
using AsmEditor.Core;

var proyecto = @"$carpetaProyecto";

foreach (var arch in new[] { TargetArch.Win64, TargetArch.Win32 })
{
    var tc = LinkerLocator.DiscoverToolchains(arch, proyecto);
    Console.WriteLine();
    Console.WriteLine(`$"=== TOOLCHAINS {arch}: {tc.Count} ===");

    foreach (var t in tc)
    {
        var estado = t.Completo ? "completo  " : "INCOMPLETO";
        Console.WriteLine(`$"  [{t.Origin,-13}] {estado} {t.DisplayName}");
    }

    var sdks = LinkerLocator.DiscoverSdks(arch);
    Console.WriteLine(`$"=== SDK {arch}: {sdks.Count} ===");
    foreach (var s in sdks) Console.WriteLine(`$"  {s.DisplayName}  ->  {s.LibPath}");
}

// El bug que motivó el descubrimiento: la lista fija de ediciones no veía
// «18\Insiders», y con eso se perdían 4 toolchains en este equipo.
Console.WriteLine();
var ins = LinkerLocator.DiscoverToolchains(TargetArch.Win64, proyecto)
    .Where(t => t.DisplayName.Contains("Insiders")).ToList();
Console.WriteLine(`$"=== Ediciones que una lista fija se perdería: {ins.Count} ===");
foreach (var i in ins) Console.WriteLine(`$"  {i.DisplayName}");

// Lo que el editor elegiría sin configuración: el primero por prioridad.
Console.WriteLine();
Console.WriteLine("=== Lo que se usa si no se elige nada ===");
foreach (var arch in new[] { TargetArch.Win64, TargetArch.Win32 })
{
    Console.WriteLine(`$"  {arch} MASM : {LinkerLocator.FindMasm(arch, proyecto) ?? "(no hay)"}");
    Console.WriteLine(`$"  {arch} link : {LinkerLocator.FindMsvcLinker(arch, proyecto) ?? "(no hay)"}");
    Console.WriteLine(`$"  {arch} SDK  : {LinkerLocator.FindSdkLibPath(arch) ?? "(no hay)"}");
}
"@ | Set-Content (Join-Path $tmp 'Program.cs') -Encoding utf8

Push-Location $tmp
try {
    & dotnet run --project tclist.csproj
}
finally {
    Pop-Location
    Remove-Item $tmp -Recurse -Force -ErrorAction SilentlyContinue
}
