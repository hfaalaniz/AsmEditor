using AsmEditor.Core;

namespace AsmEditor.Tests;

/// <summary>
/// Soporte de MASM. Todo lo de acá está verificado contra ml64.exe / ml.exe
/// 14.44.35222 antes de escribirlo, no sacado de la documentación.
/// </summary>
public class MasmArgumentosTests
{
    private static BuildTarget Masm64() => new()
    {
        Assembler = AssemblerKind.Masm,
        Arch = TargetArch.Win64,
        Linker = LinkerKind.MsvcLink,
        EntryPoint = "main",
        Libraries = "kernel32.dll user32.dll"
    };

    private static BuildTarget Masm32() => new()
    {
        Assembler = AssemblerKind.Masm,
        Arch = TargetArch.Win32,
        Linker = LinkerKind.MsvcLink,
        EntryPoint = "main",
        Libraries = "kernel32.dll user32.dll"
    };

    [Fact]
    public void Args_LlevanSoloEnsamblarYSalida()
    {
        var args = Masm64().BuildMasmArguments(@"C:\p\prog.asm", @"C:\p\prog.obj");

        // /c es «solo ensamblar»: sin eso MASM intenta enlazar por su cuenta.
        Assert.Contains("/c", args);
        Assert.Contains("/nologo", args);
        Assert.Contains("/Fo \"C:\\p\\prog.obj\"", args);
        Assert.Contains("\"C:\\p\\prog.asm\"", args);
    }

    [Fact]
    public void Args_NoLlevanElFormatoDeNasm()
    {
        // ⚠ MASM NO TIENE EQUIVALENTE DE -f: la arquitectura la decide cuál de
        // los dos ejecutables se invoca (ml64 o ml).
        var args = Masm64().BuildMasmArguments("a.asm", "a.obj");

        Assert.DoesNotContain("-f", args);
        Assert.DoesNotContain("win64", args);
    }

    [Fact]
    public void Args_ElFuenteVaAlFinal()
    {
        var args = Masm64().BuildMasmArguments("prog.asm", "prog.obj");

        Assert.EndsWith("\"prog.asm\"", args);
    }

    [Fact]
    public void Args_RutasConEspaciosVanEntreComillas()
    {
        var args = Masm64().BuildMasmArguments(@"C:\mis cosas\p.asm", @"C:\mis cosas\p.obj");

        Assert.Contains(@"""C:\mis cosas\p.asm""", args);
        Assert.Contains(@"""C:\mis cosas\p.obj""", args);
    }

    [Fact]
    public void Args_IncluyenLasBanderasExtra()
    {
        var t = Masm64();
        t.ExtraAsmFlags = "/Zi /Fl";

        var args = t.BuildMasmArguments("a.asm", "a.obj");

        Assert.Contains("/Zi", args);
        Assert.Contains("/Fl", args);
    }

    [Fact]
    public void BuildAssemblerArguments_ElijeSegunElEnsamblador()
    {
        var masm = Masm64().BuildAssemblerArguments("a.asm", "a.obj");
        var nasm = new BuildTarget { Arch = TargetArch.Win64 }.BuildAssemblerArguments("a.asm", "a.obj");

        Assert.Contains("/c", masm);
        Assert.DoesNotContain("-f win64", masm);

        Assert.Contains("-f win64", nasm);
        Assert.DoesNotContain("/c", nasm);
    }

    // ---------------- La regla del /ENTRY ----------------

    [Fact]
    public void Enlazado_MasmDe64BitsSiLlevaEntry()
    {
        // ⚠ MEDIDO ENLAZANDO DE VERDAD: en 64 bits MASM no admite «end main»,
        // así que el .obj no declara el punto de entrada y sin /ENTRY el linker
        // busca el arranque del runtime de C:
        //   error LNK2001: unresolved external symbol WinMainCRTStartup
        var args = Masm64().BuildMsvcArguments("p.obj", "p.exe", null);

        Assert.Contains("/ENTRY:main", args);
    }

    [Fact]
    public void Enlazado_MasmDe32BitsNoLlevaEntry()
    {
        // En 32 bits el «end main» del fuente hace que MASM declare el punto de
        // entrada decorado («main@0»), y pasar /ENTRY:main choca con él:
        //   warning LNK4258: directive '/ENTRY:main@0' not compatible
        var args = Masm32().BuildMsvcArguments("p.obj", "p.exe", null);

        Assert.DoesNotContain("/ENTRY", args);
    }

    [Theory]
    // La tabla completa, medida enlazando de verdad. El único caso sin /ENTRY
    // es MASM de 32 bits; generalizarlo a «MASM no lleva ENTRY» rompió el
    // enlazado de 64 bits con LNK2001.
    [InlineData(AssemblerKind.Masm, TargetArch.Win32, false)]
    [InlineData(AssemblerKind.Masm, TargetArch.Win64, true)]
    [InlineData(AssemblerKind.Nasm, TargetArch.Win32, true)]
    [InlineData(AssemblerKind.Nasm, TargetArch.Win64, true)]
    public void Enlazado_CuandoCorrespondeElEntry(AssemblerKind asm, TargetArch arch, bool esperaEntry)
    {
        var t = new BuildTarget
        {
            Assembler = asm,
            Arch = arch,
            Linker = LinkerKind.MsvcLink,
            EntryPoint = "main"
        };

        var args = t.BuildMsvcArguments("p.obj", "p.exe", null);

        if (esperaEntry) Assert.Contains("/ENTRY", args);
        else Assert.DoesNotContain("/ENTRY", args);
    }

    [Fact]
    public void Enlazado_ConNasmSiSePasaEntry()
    {
        // Con NASM sigue haciendo falta: ahí el .obj no lo declara.
        var t = new BuildTarget
        {
            Assembler = AssemblerKind.Nasm,
            Arch = TargetArch.Win64,
            Linker = LinkerKind.MsvcLink,
            EntryPoint = "main"
        };

        Assert.Contains("/ENTRY:main", t.BuildMsvcArguments("p.obj", "p.exe", null));
    }

    [Fact]
    public void Enlazado_ConMasmSiguenElSubsistemaYLasLibrerias()
    {
        // Quitar el /ENTRY no debe llevarse puesto el resto.
        var args = Masm64().BuildMsvcArguments("p.obj", "p.exe", @"C:\sdk");

        Assert.Contains("/SUBSYSTEM:WINDOWS", args);
        Assert.Contains("/OUT:\"p.exe\"", args);
        Assert.Contains(@"/LIBPATH:""C:\sdk""", args);
        Assert.Contains("kernel32.lib", args);
    }

    // ---------------- Propiedades ----------------

    [Fact]
    public void UsaMasm_DistingueLosDos()
    {
        Assert.True(Masm64().UsaMasm);
        Assert.False(new BuildTarget().UsaMasm);
    }

    [Fact]
    public void DisplayName_NombraElEnsamblador()
    {
        var m = Masm64();
        m.Name = "Prueba";
        Assert.Contains("MASM", m.DisplayName);

        var n = new BuildTarget { Name = "Prueba" };
        Assert.Contains("NASM", n.DisplayName);
    }

    [Fact]
    public void Clone_CopiaElEnsambladorYSuRuta()
    {
        var t = Masm64();
        t.MasmPath = @"C:\x\ml64.exe";

        var c = t.Clone();

        Assert.Equal(AssemblerKind.Masm, c.Assembler);
        Assert.Equal(@"C:\x\ml64.exe", c.MasmPath);
    }

    [Fact]
    public void Predefinidos_IncluyenMasmEnAmbasArquitecturas()
    {
        var d = BuildTarget.CreateDefaults();

        Assert.Contains(d, t => t.UsaMasm && t.Arch == TargetArch.Win64);
        Assert.Contains(d, t => t.UsaMasm && t.Arch == TargetArch.Win32);
        Assert.Contains(d, t => !t.UsaMasm);
    }

    [Fact]
    public void Predefinidos_LosDeMasmEnlazanConMsvc()
    {
        // GoLink enlaza contra .dll y MASM produce objetos que esperan .lib:
        // los predefinidos de MASM tienen que venir con el enlazador correcto.
        var d = BuildTarget.CreateDefaults().Where(t => t.UsaMasm && t.ProducesExecutable);

        Assert.All(d, t => Assert.Equal(LinkerKind.MsvcLink, t.Linker));
    }
}

/// <summary>
/// Los errores de MASM, con las cadenas capturadas de ml64.exe.
/// </summary>
public class MasmDiagnosticoTests
{
    [Fact]
    public void ErrorConArchivoYLinea()
    {
        // ⚠ LA LÍNEA VA ENTRE PARÉNTESIS, no con dos puntos como NASM.
        var d = DiagnosticParser.ParseAll(
            "malo.asm(4) : error A2008:syntax error : rax", BuildTool.Masm);

        var x = Assert.Single(d);
        Assert.Equal(DiagnosticLevel.Error, x.Level);
        Assert.Equal("malo.asm", x.FileName);
        Assert.Equal(4, x.Line);
        Assert.Equal(BuildTool.Masm, x.Tool);
        Assert.Contains("A2008", x.Message);
        Assert.Contains("syntax error", x.Message);
        Assert.True(x.HasLocation);
    }

    [Fact]
    public void SimboloNoDefinido()
    {
        var d = DiagnosticParser.ParseAll(
            "malo.asm(3) : error A2006:undefined symbol : noexiste", BuildTool.Masm);

        var x = Assert.Single(d);
        Assert.Equal(3, x.Line);
        Assert.Contains("noexiste", x.Message);
    }

    [Fact]
    public void FatalSinUbicacion()
    {
        var d = DiagnosticParser.ParseAll(
            "MASM : fatal error A1000:cannot open file : nohay.asm", BuildTool.Masm);

        var x = Assert.Single(d);
        Assert.Equal(DiagnosticLevel.Fatal, x.Level);
        Assert.Null(x.FileName);
        Assert.False(x.HasLocation);
        Assert.Contains("A1000", x.Message);
    }

    [Fact]
    public void Advertencia()
    {
        var d = DiagnosticParser.ParseAll(
            "hola.asm(7) : warning A4014:cambio de alineación", BuildTool.Masm);

        Assert.Equal(DiagnosticLevel.Warning, Assert.Single(d).Level);
    }

    [Fact]
    public void SalidaCompletaDeUnEnsambladoFallido()
    {
        // Tal cual la imprime ml64.exe, encabezado incluido.
        var salida =
            " Assembling: malo.asm\n" +
            "malo.asm(4) : error A2008:syntax error : rax\n" +
            "malo.asm(3) : error A2006:undefined symbol : noexiste\n" +
            "malo.asm(5) : error A2207:missing right parenthesis in expression\n";

        var d = DiagnosticParser.ParseAll(salida, BuildTool.Masm);

        Assert.Equal(3, d.Count);
        Assert.All(d, x => Assert.Equal(BuildTool.Masm, x.Tool));
    }

    [Fact]
    public void ElEncabezadoDeEnsambladoNoEsUnError()
    {
        Assert.Empty(DiagnosticParser.ParseAll(" Assembling: hola.asm", BuildTool.Masm));
    }

    [Fact]
    public void RutaAbsolutaDeWindows()
    {
        var d = DiagnosticParser.ParseAll(
            @"C:\Users\Fabian\NASM\prog.asm(42) : error A2008:algo", BuildTool.Masm);

        var x = Assert.Single(d);
        Assert.Equal(@"C:\Users\Fabian\NASM\prog.asm", x.FileName);
        Assert.Equal(42, x.Line);
    }

    [Fact]
    public void NoSeConfundeConElFormatoDeNasm()
    {
        // Las dos líneas en la misma salida: cada una con su herramienta.
        var salida =
            "prog.asm(4) : error A2008:algo de MASM\n" +
            "prog.asm:4: error: algo de NASM\n";

        var d = DiagnosticParser.ParseAll(salida);

        Assert.Equal(2, d.Count);
        Assert.Equal(BuildTool.Masm, d[0].Tool);
        Assert.Equal(BuildTool.Nasm, d[1].Tool);
    }
}
