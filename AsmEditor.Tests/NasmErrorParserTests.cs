using AsmEditor.Core;

namespace AsmEditor.Tests;

public class NasmErrorParserTests
{
    [Fact]
    public void Parse_ArchivoYLineaSimple()
    {
        var r = NasmErrorParser.Parse("programa.asm:42: error: symbol `foo' undefined");

        Assert.NotNull(r);
        Assert.Equal("programa.asm", r!.FileName);
        Assert.Equal(42, r.LineNumber);
    }

    [Fact]
    public void Parse_RutaAbsolutaDeWindows()
    {
        var r = NasmErrorParser.Parse(@"C:\Users\Fabian\NASM\Win6Win.asm:117: error: invalid operand");

        Assert.NotNull(r);
        Assert.Equal(@"C:\Users\Fabian\NASM\Win6Win.asm", r!.FileName);
        Assert.Equal(117, r.LineNumber);
    }

    [Fact]
    public void Parse_RutaConEspacios()
    {
        var r = NasmErrorParser.Parse(@"C:\mis cosas\mi programa.asm:7: warning: label alone on a line");

        Assert.NotNull(r);
        Assert.Equal(@"C:\mis cosas\mi programa.asm", r!.FileName);
        Assert.Equal(7, r.LineNumber);
    }

    [Fact]
    public void Parse_ArchivoInclude()
    {
        var r = NasmErrorParser.Parse("macros.inc:3: error: unterminated macro");

        Assert.NotNull(r);
        Assert.Equal("macros.inc", r!.FileName);
        Assert.Equal(3, r.LineNumber);
    }

    [Fact]
    public void Parse_ConColumnaAdicional()
    {
        // Algunas versiones agregan la columna: archivo.asm:42:9: error
        var r = NasmErrorParser.Parse("programa.asm:42:9: error: algo");

        Assert.NotNull(r);
        Assert.Equal("programa.asm", r!.FileName);
        Assert.Equal(42, r.LineNumber);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    [InlineData("> nasm.exe -f win64 programa.asm")]
    [InlineData("(código de salida: 0)")]
    [InlineData("programa.asm: error sin numero de linea")]
    [InlineData("programa.txt:12: esto no es un fuente asm")]
    [InlineData("programa.asm:0: linea cero no es valida")]
    public void Parse_DevuelveNullCuandoNoEsUnaReferencia(string? linea)
    {
        Assert.Null(NasmErrorParser.Parse(linea));
    }

    [Fact]
    public void LooksLikeProblem_DetectaErroresYAdvertencias()
    {
        Assert.True(NasmErrorParser.LooksLikeProblem("programa.asm:42: error: algo"));
        Assert.True(NasmErrorParser.LooksLikeProblem("ERROR: no se encontró el ejecutable"));
        Assert.True(NasmErrorParser.LooksLikeProblem("programa.asm:9: warning: cuidado"));
    }

    [Fact]
    public void LooksLikeProblem_NoMarcaLineasNormales()
    {
        Assert.False(NasmErrorParser.LooksLikeProblem("> nasm.exe -f win64 programa.asm"));
        Assert.False(NasmErrorParser.LooksLikeProblem("(código de salida: 0)"));
        Assert.False(NasmErrorParser.LooksLikeProblem(""));
        Assert.False(NasmErrorParser.LooksLikeProblem(null));
    }
}
