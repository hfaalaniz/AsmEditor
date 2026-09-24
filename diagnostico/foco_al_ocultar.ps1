# ============================================================================
# Mide DONDE queda el foco al ocultar un panel del acople con su X, dentro del
# proceso y SIN raton ni teclado (no toca ninguna ventana de Fabian).
#
# Arma un proyectito en diagnostico\banco_foco\ que referencia al editor, pone
# un AnfitrionAcople con un panel y un TextBox al centro, le da el foco a la X
# del panel, la aprieta con PerformClick y anota en cada paso: el control con
# foco (y si cuelga del formulario), Form.ActiveForm, ContainsFocus y
# ActiveControl.
#
# ⚠ CORRE CON dotnet (el editor es .NET 8).
#
# Es para que lo lea yo, no forma parte de la interfaz.
# ============================================================================

$ErrorActionPreference = 'Stop'

$proyecto = Split-Path $PSScriptRoot
$banco = Join-Path $PSScriptRoot 'banco_foco'
# Se reescriben los archivos, sin borrar la carpeta: el servidor de
# compilacion de dotnet la puede tener tomada (paso el 23/09).
if (-not (Test-Path $banco)) { New-Item -ItemType Directory -Path $banco | Out-Null }
Remove-Item (Join-Path $banco 'resultado.txt') -ErrorAction SilentlyContinue

@"
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>WinExe</OutputType>
    <TargetFramework>net8.0-windows</TargetFramework>
    <UseWindowsForms>true</UseWindowsForms>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <AssemblyName>bancofoco</AssemblyName>
  </PropertyGroup>
  <ItemGroup>
    <ProjectReference Include="$proyecto\AsmEditor.csproj" />
  </ItemGroup>
</Project>
"@ | Set-Content (Join-Path $banco 'bancofoco.csproj') -Encoding utf8

@"
using System.Runtime.InteropServices;
using AsmEditor;
using AsmEditor.Core.Acople;

ApplicationConfiguration.Initialize();
var salida = new System.Text.StringBuilder();

var form = new Form { Width = 900, Height = 600, StartPosition = FormStartPosition.Manual, Location = new Point(40, 40) };
var acople = new AnfitrionAcople { Dock = DockStyle.Fill };
var centro = new TextBox { Multiline = true, Dock = DockStyle.Fill, Name = "centro" };
var arbol = new TreeView { Name = "arbol" };
var ventana = new VentanaHerramienta { Id = "explorador", Titulo = "Explorador" };
ventana.Contenido = arbol;

form.Controls.Add(acople);
acople.Centro.Add(centro);
acople.Registrar(ventana, ZonaAcople.Derecha);

form.Shown += (_, _) =>
{
    Application.DoEvents();
    var x = ventana.Controls.Find("btnCerrar", true)[0];

    Anotar("al mostrar");
    x.Focus();
    Application.DoEvents();
    Anotar("foco en la X");

    ((Button)x).PerformClick();
    Anotar("justo despues de PerformClick");

    for (int i = 0; i < 5; i++) Application.DoEvents();
    Anotar("despues de procesar mensajes");

    salida.AppendLine("   acople.ActiveControl: " + (acople.ActiveControl?.GetType().Name ?? "(null)") + " " + (acople.ActiveControl?.Name ?? ""));
    salida.AppendLine("   centro.Focus() devolvio: " + centro.Focus());
    Application.DoEvents();
    Anotar("tras centro.Focus()");
    salida.AppendLine("   centro.CanFocus: " + centro.CanFocus + "  CanSelect: " + centro.CanSelect);
    acople.ActiveControl = null;
    salida.AppendLine("   acople.ActiveControl = null; centro.Focus() devolvio: " + centro.Focus());
    Application.DoEvents();
    Anotar("tras soltar acople.ActiveControl");

    File.WriteAllText(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "resultado.txt"), salida.ToString());
    form.Close();
};

Application.Run(form);

void Anotar(string paso)
{
    IntPtr f = GetFocus();
    var c = Control.FromHandle(f);
    string quien = c is null ? (f == IntPtr.Zero ? "(ninguno)" : "hwnd sin control") : c.GetType().Name + " '" + c.Name + "'";
    bool dentro = c is not null && c.FindForm() == form;
    salida.AppendLine(string.Format("{0,-32} foco: {1,-28} cuelga del form: {2,-5} ActiveForm==form: {3,-5} form.ContainsFocus: {4,-5} ActiveControl: {5} / acople: {6}",
        paso, quien, dentro, Form.ActiveForm == form, form.ContainsFocus, form.ActiveControl?.Name ?? "(null)", acople.ActiveControl?.GetType().Name ?? "(null)"));
}

[DllImport("user32.dll")] static extern IntPtr GetFocus();
"@ | Set-Content (Join-Path $banco 'Program.cs') -Encoding utf8

Push-Location $banco
try {
    $ErrorActionPreference = 'Continue'
    & dotnet run --project bancofoco.csproj 2>&1 | Out-String | Write-Host
    $ErrorActionPreference = 'Stop'
    Get-Content (Join-Path $banco 'resultado.txt')
}
finally { Pop-Location }
