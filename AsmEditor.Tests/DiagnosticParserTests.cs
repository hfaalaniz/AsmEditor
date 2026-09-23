using AsmEditor.Core;

namespace AsmEditor.Tests;

/// <summary>
/// Las cadenas de estas pruebas son salida REAL capturada de las herramientas del
/// proyecto (NASM 3.02 y GoLink 1.0.4.6), no inventadas ni sacadas de la documentación.
/// </summary>
public class DiagnosticParserTests
{
    // ---------------- Formato 1: NASM con archivo y línea ----------------

    [Fact]
    public void Nasm_ErrorConArchivoYLinea()
    {
        var d = DiagnosticParser.ParseAll(
            "err.asm:6: error: instruction expected, found `xyzinvalida rax'");

        var x = Assert.Single(d);
        Assert.Equal(DiagnosticLevel.Error, x.Level);
        Assert.Equal("err.asm", x.FileName);
        Assert.Equal(6, x.Line);
        Assert.Equal("instruction expected, found `xyzinvalida rax'", x.Message);
        Assert.Equal(BuildTool.Nasm, x.Tool);
        Assert.True(x.HasLocation);
    }

    [Fact]
    public void Nasm_AdvertenciaConBanderaAlFinal()
    {
        var d = DiagnosticParser.ParseAll(
            "warn.asm:5: warning: byte exceeds bounds [-w+number-overflow]");

        var x = Assert.Single(d);
        Assert.Equal(DiagnosticLevel.Warning, x.Level);
        Assert.Equal(5, x.Line);
        // La bandera [-w+...] se limpia: no aporta nada en la grilla.
        Assert.Equal("byte exceeds bounds", x.Message);
    }

    [Fact]
    public void Nasm_RutaAbsolutaDeWindows()
    {
        var d = DiagnosticParser.ParseAll(
            @"C:\Users\Fabian\NASM\Win6Win.asm:117: error: invalid operand type");

        var x = Assert.Single(d);
        Assert.Equal(@"C:\Users\Fabian\NASM\Win6Win.asm", x.FileName);
        Assert.Equal(117, x.Line);
    }

    [Fact]
    public void Nasm_VariosErroresEnUnaSalida()
    {
        var salida =
            "err.asm:6: error: instruction expected, found `xyzinvalida rax'\n" +
            "err.asm:7: error: expecting ] at end of memory operand\n";

        var d = DiagnosticParser.ParseAll(salida);

        Assert.Equal(2, d.Count);
        Assert.Equal(6, d[0].Line);
        Assert.Equal(7, d[1].Line);
    }

    [Fact]
    public void Nasm_ArchivoInclude()
    {
        var d = DiagnosticParser.ParseAll("macros.inc:3: error: unterminated macro");

        var x = Assert.Single(d);
        Assert.Equal("macros.inc", x.FileName);
    }

    [Fact]
    public void Nasm_ColumnaQuedaNulaPorqueNasmNoLaReporta()
    {
        // NASM 3.02 emite "archivo:línea:" y nada más. Verificado corriendo la herramienta.
        var d = DiagnosticParser.ParseAll("err.asm:6: error: algo");

        Assert.Null(Assert.Single(d).Column);
    }

    [Fact]
    public void Nasm_SiApareceUnaColumnaSeInterpreta()
    {
        // El modelo la admite por si otra herramienta la emite.
        var d = DiagnosticParser.ParseAll("err.asm:6:14: error: algo");

        var x = Assert.Single(d);
        Assert.Equal(6, x.Line);
        Assert.Equal(14, x.Column);
    }

    // ---------------- Formato 2: NASM fatal sin ubicación ----------------

    [Fact]
    public void Nasm_FatalSinArchivoNiLinea()
    {
        var d = DiagnosticParser.ParseAll(
            "nasm: fatal: unable to open input file `nohay.asm' No such file or directory");

        var x = Assert.Single(d);
        Assert.Equal(DiagnosticLevel.Fatal, x.Level);
        Assert.Null(x.FileName);
        Assert.Null(x.Line);
        Assert.False(x.HasLocation);
        Assert.Contains("unable to open input file", x.Message);
    }

    [Fact]
    public void Nasm_FatalDeFormatoInvalido()
    {
        var d = DiagnosticParser.ParseAll(
            "nasm: fatal: unrecognised output format `nofmt' - use -hf for a list");

        var x = Assert.Single(d);
        Assert.Equal(DiagnosticLevel.Fatal, x.Level);
    }

    // ---------------- Formato 3: bloque multilínea de GoLink ----------------

    [Fact]
    public void GoLink_BloqueDeErrorSeJuntaEnUnSoloDiagnostico()
    {
        // Salida real de GoLink ante un símbolo no definido.
        var salida =
            "\uFEFF\n" +
            "GoLink.Exe Version 1.0.4.6  Copyright Jeremy Gordon 2002-2025   info@goprog.com\n" +
            "\n" +
            "Error!\n" +
            "The following symbol was not defined in the object file or files:-\n" +
            "NoExisteEstaFuncion\n" +
            "Output file not made\n";

        var d = DiagnosticParser.ParseAll(salida, BuildTool.GoLink);

        var x = Assert.Single(d);
        Assert.Equal(DiagnosticLevel.Error, x.Level);
        Assert.Equal(BuildTool.GoLink, x.Tool);
        Assert.Contains("NoExisteEstaFuncion", x.Message);
        Assert.False(x.HasLocation);
    }

    [Fact]
    public void GoLink_ElEncabezadoSueltoNoSePierdeSiNoHayDetalle()
    {
        var d = DiagnosticParser.ParseAll("Error!\n", BuildTool.GoLink);

        var x = Assert.Single(d);
        Assert.Equal(DiagnosticLevel.Error, x.Level);
        Assert.False(string.IsNullOrWhiteSpace(x.Message));
    }

    [Fact]
    public void GoLink_SinFlushElUltimoBloqueSePerderia()
    {
        // Sin línea siguiente que lo cierre, el bloque solo se cierra con Flush().
        var parser = new DiagnosticParser { CurrentTool = BuildTool.GoLink };
        parser.Feed("Error!");
        parser.Feed("The following symbol was not defined in the object file or files:-");
        parser.Feed("NoExisteEstaFuncion");

        Assert.Empty(parser.Diagnostics);   // todavía abierto

        parser.Flush();

        Assert.Single(parser.Diagnostics);
        Assert.Contains("NoExisteEstaFuncion", parser.Diagnostics[0].Message);
    }

    [Fact]
    public void GoLink_LaSalidaExitosaNoProduceDiagnosticos()
    {
        var salida =
            "\uFEFF\n" +
            "GoLink.Exe Version 1.0.4.6  Copyright Jeremy Gordon 2002-2025   info@goprog.com\n" +
            "Output file: C:\\Users\\Fabian\\NASM\\Win6Win.exe\n" +
            "Format: X64   Size: 3,072 bytes\n";

        var d = DiagnosticParser.ParseAll(salida, BuildTool.GoLink);

        Assert.Empty(d);
    }

    // ---------------- Formato 4: link.exe de MSVC ----------------
    // Cadenas capturadas de la herramienta real (link.exe 14.44.35222).

    [Fact]
    public void Msvc_LibreriaNoEncontrada()
    {
        var d = DiagnosticParser.ParseAll(
            "LINK : fatal error LNK1181: cannot open input file 'kernel32.lib'",
            BuildTool.MsvcLink);

        var x = Assert.Single(d);
        Assert.Equal(DiagnosticLevel.Fatal, x.Level);
        Assert.Equal(BuildTool.MsvcLink, x.Tool);
        Assert.Contains("LNK1181", x.Message);
        Assert.Contains("kernel32.lib", x.Message);
        Assert.False(x.HasLocation);
    }

    [Fact]
    public void Msvc_SimboloSinResolver()
    {
        var d = DiagnosticParser.ParseAll(
            "LINK : error LNK2001: unresolved external symbol noexiste",
            BuildTool.MsvcLink);

        var x = Assert.Single(d);
        // Sin 'fatal' delante es error, no fatal.
        Assert.Equal(DiagnosticLevel.Error, x.Level);
        Assert.Contains("LNK2001", x.Message);
    }

    [Fact]
    public void Msvc_ErrorConNombreDeArchivoAlPrincipio()
    {
        // El origen puede ser un archivo en vez de "LINK".
        var d = DiagnosticParser.ParseAll(
            "e.exe : fatal error LNK1120: 1 unresolved externals",
            BuildTool.MsvcLink);

        var x = Assert.Single(d);
        Assert.Equal(DiagnosticLevel.Fatal, x.Level);
        Assert.Contains("LNK1120", x.Message);
    }

    [Fact]
    public void Msvc_SalidaCompletaDeUnEnlazadoFallido()
    {
        var salida =
            "LINK : error LNK2001: unresolved external symbol noexiste\n" +
            "e.exe : fatal error LNK1120: 1 unresolved externals\n";

        var parser = new DiagnosticParser { CurrentTool = BuildTool.MsvcLink };
        foreach (var l in salida.Split('\n')) parser.Feed(l);
        parser.Flush();

        Assert.Equal(2, parser.Diagnostics.Count);
        Assert.Equal(2, parser.ErrorCount);
    }

    [Fact]
    public void Msvc_LaSalidaExitosaNoProduceDiagnosticos()
    {
        // Con /NOLOGO un enlazado correcto no imprime nada.
        Assert.Empty(DiagnosticParser.ParseAll("", BuildTool.MsvcLink));
    }

    [Fact]
    public void Msvc_ElEncabezadoDeVersionNoEsUnError()
    {
        var salida =
            "Microsoft (R) Incremental Linker Version 14.44.35222.0\n" +
            "Copyright (C) Microsoft Corporation.  All rights reserved.\n";

        Assert.Empty(DiagnosticParser.ParseAll(salida, BuildTool.MsvcLink));
    }

    // ---------------- Ruido que NO debe producir diagnósticos ----------------

    [Theory]
    [InlineData("> nasm.exe -f win64 programa.asm -o programa.obj")]
    [InlineData("(código de salida: 0)")]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("GoLink.Exe Version 1.0.4.6  Copyright Jeremy Gordon 2002-2025")]
    [InlineData("Format: X64   Size: 3,072 bytes")]
    public void LineasNormalesNoProducenDiagnosticos(string linea)
    {
        Assert.Empty(DiagnosticParser.ParseAll(linea));
    }

    // ---------------- Conteos y limpieza ----------------

    [Fact]
    public void Conteos_SeparanErroresDeAdvertencias()
    {
        var salida =
            "a.asm:1: error: uno\n" +
            "a.asm:2: warning: dos\n" +
            "a.asm:3: error: tres\n" +
            "nasm: fatal: cuatro\n";

        var parser = new DiagnosticParser();
        foreach (var l in salida.Split('\n')) parser.Feed(l);
        parser.Flush();

        // Los fatales cuentan como errores.
        Assert.Equal(3, parser.ErrorCount);
        Assert.Equal(1, parser.WarningCount);
    }

    [Fact]
    public void Clear_DejaElParserComoNuevo()
    {
        var parser = new DiagnosticParser();
        parser.Feed("a.asm:1: error: uno");
        Assert.Single(parser.Diagnostics);

        parser.Clear();

        Assert.Empty(parser.Diagnostics);
        Assert.Equal(0, parser.ErrorCount);
    }

    [Fact]
    public void Clear_TambienDescartaUnBloqueDeGoLinkAbierto()
    {
        var parser = new DiagnosticParser();
        parser.Feed("Error!");
        parser.Feed("detalle");

        parser.Clear();
        parser.Flush();

        Assert.Empty(parser.Diagnostics);
    }

    // ---------------- Texto para mostrar ----------------

    [Fact]
    public void LocationText_MuestraSoloElNombreDelArchivo()
    {
        var d = DiagnosticParser.ParseAll(@"C:\ruta\larga\prog.asm:42: error: algo");

        Assert.Equal("prog.asm:42", Assert.Single(d).LocationText);
    }

    [Fact]
    public void LocationText_VacioCuandoNoHayUbicacion()
    {
        var d = DiagnosticParser.ParseAll("nasm: fatal: algo");

        Assert.Equal("", Assert.Single(d).LocationText);
    }

    [Fact]
    public void LevelText_EnEspanol()
    {
        Assert.Equal("error", DiagnosticParser.ParseAll("a.asm:1: error: x")[0].LevelText);
        Assert.Equal("advertencia", DiagnosticParser.ParseAll("a.asm:1: warning: x")[0].LevelText);
        Assert.Equal("fatal", DiagnosticParser.ParseAll("nasm: fatal: x")[0].LevelText);
    }
}
