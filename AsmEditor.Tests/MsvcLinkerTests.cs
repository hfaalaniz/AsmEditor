using AsmEditor.Core;

namespace AsmEditor.Tests;

/// <summary>
/// Reglas del enlazado con el link.exe de MSVC, todas verificadas contra la
/// herramienta real antes de escribirlas.
/// </summary>
public class MsvcLinkerTests
{
    private static BuildTarget Msvc64() => new()
    {
        Arch = TargetArch.Win64,
        Linker = LinkerKind.MsvcLink,
        Subsystem = TargetSubsystem.Windows,
        EntryPoint = "main",
        Libraries = "kernel32.dll user32.dll gdi32.dll"
    };

    private static BuildTarget Msvc32() => new()
    {
        Arch = TargetArch.Win32,
        Linker = LinkerKind.MsvcLink,
        Subsystem = TargetSubsystem.Windows,
        EntryPoint = "_main",
        Libraries = "kernel32.dll user32.dll"
    };

    // ---------------- Traducción de .dll a .lib ----------------

    [Fact]
    public void Librerias_SeTraducenDeDllALib()
    {
        // GoLink enlaza contra kernel32.dll; MSVC necesita kernel32.lib.
        var r = BuildTarget.TranslateLibrariesForMsvc("kernel32.dll user32.dll gdi32.dll");

        Assert.Equal("kernel32.lib user32.lib gdi32.lib", r);
    }

    [Fact]
    public void Librerias_LasQueYaSonLibNoSeTocan()
    {
        var r = BuildTarget.TranslateLibrariesForMsvc("kernel32.lib user32.dll");

        Assert.Equal("kernel32.lib user32.lib", r);
    }

    [Fact]
    public void Librerias_AdmiteComasYPuntoYComaComoSeparador()
    {
        var r = BuildTarget.TranslateLibrariesForMsvc("kernel32.dll, user32.dll; gdi32.dll");

        Assert.Equal("kernel32.lib user32.lib gdi32.lib", r);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Librerias_VaciasDevuelvenVacio(string libs)
    {
        Assert.Equal("", BuildTarget.TranslateLibrariesForMsvc(libs));
    }

    // ---------------- El guion bajo del punto de entrada ----------------

    [Fact]
    public void Entrada32Bits_SeQuitaElGuionBajo()
    {
        // Detalle verificado con la herramienta: NASM -f win32 ya decora la etiqueta
        // con '_', así que /ENTRY:_main haría que el linker busque '__main' y falle
        // con LNK2001. El punto de entrada correcto es 'main'.
        var args = Msvc32().BuildMsvcArguments("p.obj", "p.exe", null);

        Assert.Contains("/ENTRY:main", args);
        Assert.DoesNotContain("/ENTRY:_main", args);
    }

    [Fact]
    public void Entrada64Bits_NoSeToca()
    {
        // En 64 bits NASM no decora, así que el nombre va tal cual.
        var t = Msvc64();
        t.EntryPoint = "main";

        Assert.Contains("/ENTRY:main", t.BuildMsvcArguments("p.obj", "p.exe", null));
    }

    [Fact]
    public void Entrada64Bits_ConGuionBajoExplicitoSeRespeta()
    {
        // Si el usuario escribió '_arranque' en un target de 64 bits, es a propósito.
        var t = Msvc64();
        t.EntryPoint = "_arranque";

        Assert.Contains("/ENTRY:_arranque", t.BuildMsvcArguments("p.obj", "p.exe", null));
    }

    // ---------------- Estructura de los argumentos ----------------

    [Fact]
    public void Args_LlevanSubsistemaSalidaYObjeto()
    {
        var args = Msvc64().BuildMsvcArguments(@"C:\p\prog.obj", @"C:\p\prog.exe", null);

        Assert.Contains("/SUBSYSTEM:WINDOWS", args);
        Assert.Contains("/OUT:\"C:\\p\\prog.exe\"", args);
        Assert.Contains("\"C:\\p\\prog.obj\"", args);
        Assert.Contains("/NOLOGO", args);
    }

    [Fact]
    public void Args_SubsistemaConsola()
    {
        var t = Msvc64();
        t.Subsystem = TargetSubsystem.Console;

        Assert.Contains("/SUBSYSTEM:CONSOLE", t.BuildMsvcArguments("p.obj", "p.exe", null));
    }

    [Fact]
    public void Args_IncluyenLibPathDelSdk()
    {
        var sdk = @"C:\Program Files (x86)\Windows Kits\10\Lib\10.0.26100.0\um\x64";

        var args = Msvc64().BuildMsvcArguments("p.obj", "p.exe", sdk);

        Assert.Contains($"/LIBPATH:\"{sdk}\"", args);
    }

    [Fact]
    public void Args_ElLibPathDelTargetGanaSobreElDetectado()
    {
        var t = Msvc64();
        t.SdkLibPath = @"C:\mi\ruta\propia";

        var args = t.BuildMsvcArguments("p.obj", "p.exe", @"C:\detectada");

        Assert.Contains(@"/LIBPATH:""C:\mi\ruta\propia""", args);
        Assert.DoesNotContain(@"C:\detectada", args);
    }

    [Fact]
    public void Args_SinSdkNoPonenLibPathVacio()
    {
        var args = Msvc64().BuildMsvcArguments("p.obj", "p.exe", null);

        Assert.DoesNotContain("/LIBPATH", args);
    }

    [Fact]
    public void Args_LasLibreriasVanTraducidas()
    {
        var args = Msvc64().BuildMsvcArguments("p.obj", "p.exe", null);

        Assert.Contains("kernel32.lib", args);
        Assert.DoesNotContain("kernel32.dll", args);
    }

    [Fact]
    public void Args_RutasConEspaciosVanEntreComillas()
    {
        var args = Msvc64().BuildMsvcArguments(@"C:\mis cosas\p.obj", @"C:\mis cosas\p.exe", null);

        Assert.Contains(@"""C:\mis cosas\p.obj""", args);
        Assert.Contains(@"/OUT:""C:\mis cosas\p.exe""", args);
    }

    [Fact]
    public void Args_IncluyenLasBanderasExtra()
    {
        var t = Msvc64();
        t.ExtraLinkFlags = "/DEBUG /MAP";

        var args = t.BuildMsvcArguments("p.obj", "p.exe", null);

        Assert.Contains("/DEBUG", args);
        Assert.Contains("/MAP", args);
    }

    // ---------------- Que cada enlazador use su sintaxis ----------------

    [Fact]
    public void GoLinkYMsvcUsanSintaxisDistinta()
    {
        var golink = new BuildTarget { Linker = LinkerKind.GoLink, EntryPoint = "main", Libraries = "kernel32.dll" };
        var msvc = Msvc64();

        var gl = golink.BuildGoLinkArguments("p.obj");
        var ms = msvc.BuildMsvcArguments("p.obj", "p.exe", null);

        Assert.Contains("/entry main", gl);      // minúsculas, sin dos puntos
        Assert.Contains("kernel32.dll", gl);     // GoLink usa .dll

        Assert.Contains("/ENTRY:main", ms);      // mayúsculas, con dos puntos
        Assert.Contains("kernel32.lib", ms);     // MSVC usa .lib
    }

    [Fact]
    public void DisplayName_DiceQueEnlazadorUsa()
    {
        var m = Msvc64();
        m.Name = "Prueba";

        Assert.Contains("MSVC link", m.DisplayName);

        var g = new BuildTarget { Name = "Prueba", Linker = LinkerKind.GoLink };
        Assert.Contains("GoLink", g.DisplayName);
    }

    [Fact]
    public void Clone_CopiaLosCamposDelEnlazador()
    {
        var t = Msvc64();
        t.MsvcLinkerPath = @"C:\x\link.exe";
        t.SdkLibPath = @"C:\y";

        var c = t.Clone();

        Assert.Equal(LinkerKind.MsvcLink, c.Linker);
        Assert.Equal(TargetSubsystem.Windows, c.Subsystem);
        Assert.Equal(@"C:\x\link.exe", c.MsvcLinkerPath);
        Assert.Equal(@"C:\y", c.SdkLibPath);
    }

    [Fact]
    public void Predefinidos_IncluyenTargetsDeMsvcEnAmbasArquitecturas()
    {
        var d = BuildTarget.CreateDefaults();

        Assert.Contains(d, t => t.Linker == LinkerKind.MsvcLink && t.Arch == TargetArch.Win64);
        Assert.Contains(d, t => t.Linker == LinkerKind.MsvcLink && t.Arch == TargetArch.Win32);
        Assert.Contains(d, t => t.Linker == LinkerKind.GoLink);
    }
}
