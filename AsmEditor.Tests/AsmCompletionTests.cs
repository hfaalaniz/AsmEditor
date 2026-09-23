using AsmEditor.Core;

namespace AsmEditor.Tests;

public class AsmCompletionTests
{
    // ---------------- Extracción de etiquetas del archivo ----------------

    [Fact]
    public void ExtractLabels_EtiquetasConDosPuntos()
    {
        var src = "section .text\nmain:\n    ret\nWndProc:\n    ret\n";

        var labels = AsmCompletion.ExtractLabels(src);

        Assert.Contains("main", labels);
        Assert.Contains("WndProc", labels);
    }

    [Fact]
    public void ExtractLabels_EtiquetasLocalesConPunto()
    {
        var src = "main:\n    jmp .salida\n.salida:\n    ret\n";

        var labels = AsmCompletion.ExtractLabels(src);

        Assert.Contains(".salida", labels);
    }

    [Fact]
    public void ExtractLabels_EtiquetasDeDatosSinDosPuntos()
    {
        var src = "section .data\nmensaje db \"hola\", 0\nlargo   equ 4\nbuffer  resb 64\n";

        var labels = AsmCompletion.ExtractLabels(src);

        Assert.Contains("mensaje", labels);
        Assert.Contains("largo", labels);
        Assert.Contains("buffer", labels);
    }

    [Fact]
    public void ExtractLabels_IgnoraLoQueEstaEnComentarios()
    {
        var src = "main:\n    ret\n; fantasma:\n;   otra db 1\n";

        var labels = AsmCompletion.ExtractLabels(src);

        Assert.Contains("main", labels);
        Assert.DoesNotContain("fantasma", labels);
        Assert.DoesNotContain("otra", labels);
    }

    [Fact]
    public void ExtractLabels_NoConfundeUnPuntoYComaDentroDeUnaCadena()
    {
        // El ';' está dentro de la cadena: la línea siguiente NO es un comentario.
        var src = "texto db \"a;b\", 0\nreal_label:\n    ret\n";

        var labels = AsmCompletion.ExtractLabels(src);

        Assert.Contains("texto", labels);
        Assert.Contains("real_label", labels);
    }

    [Fact]
    public void ExtractLabels_NoDevuelvePalabrasReservadas()
    {
        // 'section' va seguido de .text, no es una etiqueta.
        var src = "section .text\nglobal main\nmain:\n    ret\n";

        var labels = AsmCompletion.ExtractLabels(src);

        Assert.DoesNotContain("section", labels);
        Assert.DoesNotContain("global", labels);
        Assert.Contains("main", labels);
    }

    [Fact]
    public void ExtractLabels_SinDuplicadosYEnOrdenDeAparicion()
    {
        var src = "primera:\n    ret\nsegunda:\n    ret\nprimera:\n    ret\n";

        var labels = AsmCompletion.ExtractLabels(src);

        Assert.Equal(new[] { "primera", "segunda" }, labels);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void ExtractLabels_TextoVacioDevuelveListaVacia(string? src)
    {
        Assert.Empty(AsmCompletion.ExtractLabels(src));
    }

    // ---------------- Sugerencias ----------------

    [Fact]
    public void Suggest_EncuentraInstrucciones()
    {
        var items = AsmCompletion.Suggest("mo");

        Assert.Contains(items, i => i.Text == "mov" && i.Kind == CompletionKind.Instruction);
        Assert.Contains(items, i => i.Text == "movzx");
    }

    [Fact]
    public void Suggest_EncuentraRegistros()
    {
        var items = AsmCompletion.Suggest("ra");

        Assert.Contains(items, i => i.Text == "rax" && i.Kind == CompletionKind.Register);
    }

    [Fact]
    public void Suggest_EncuentraDirectivas()
    {
        var items = AsmCompletion.Suggest("sec");

        Assert.Contains(items, i => i.Text == "section" && i.Kind == CompletionKind.Directive);
    }

    [Fact]
    public void Suggest_IncluyeLasEtiquetasDelArchivo()
    {
        var items = AsmCompletion.Suggest("Wnd", new[] { "WndProc", "WndClass" });

        Assert.Contains(items, i => i.Text == "WndProc" && i.Kind == CompletionKind.Label);
        Assert.Contains(items, i => i.Text == "WndClass" && i.Kind == CompletionKind.Label);
    }

    [Fact]
    public void Suggest_NoDistingueMayusculas()
    {
        var items = AsmCompletion.Suggest("MO");

        Assert.Contains(items, i => i.Text == "mov");
    }

    [Fact]
    public void Suggest_PrefijoVacioNoSugiereNada()
    {
        Assert.Empty(AsmCompletion.Suggest(""));
    }

    [Fact]
    public void Suggest_OrdenaLasMasCortasPrimero()
    {
        var items = AsmCompletion.Suggest("mov");

        Assert.Equal("mov", items[0].Text);
    }

    [Fact]
    public void Suggest_SinDuplicadosAunqueLaEtiquetaChoqueConUnaInstruccion()
    {
        // Si el archivo define una etiqueta llamada 'test' (que también es instrucción),
        // debe aparecer una sola vez.
        var items = AsmCompletion.Suggest("test", new[] { "test" });

        Assert.Single(items, i => i.Text.Equals("test", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Suggest_RespetaElMaximoDeResultados()
    {
        var items = AsmCompletion.Suggest("m", maxResults: 5);

        Assert.True(items.Count <= 5);
    }

    [Fact]
    public void Suggest_PrefijoSinCoincidenciasDevuelveVacio()
    {
        Assert.Empty(AsmCompletion.Suggest("zzzqqq"));
    }

    // ---------------- Palabra bajo el caret ----------------

    [Fact]
    public void GetWordBeforeCaret_TomaLaPalabraQueSeEstaTipeando()
    {
        var text = "    mo";
        var (start, prefix) = AsmCompletion.GetWordBeforeCaret(text, text.Length);

        Assert.Equal(4, start);
        Assert.Equal("mo", prefix);
    }

    [Fact]
    public void GetWordBeforeCaret_SeDetieneEnLaComa()
    {
        var text = "mov rax, rb";
        var (start, prefix) = AsmCompletion.GetWordBeforeCaret(text, text.Length);

        Assert.Equal("rb", prefix);
        Assert.Equal(9, start);
    }

    [Fact]
    public void GetWordBeforeCaret_IncluyeElPuntoDeLasEtiquetasLocales()
    {
        var text = "    jmp .sal";
        var (_, prefix) = AsmCompletion.GetWordBeforeCaret(text, text.Length);

        Assert.Equal(".sal", prefix);
    }

    [Fact]
    public void GetWordBeforeCaret_IncluyeElPorcentajeDeLasDirectivas()
    {
        var text = "%inc";
        var (start, prefix) = AsmCompletion.GetWordBeforeCaret(text, text.Length);

        Assert.Equal(0, start);
        Assert.Equal("%inc", prefix);
    }

    [Fact]
    public void GetWordBeforeCaret_CaretEnMedioDeLaPalabraSoloTomaLoAnterior()
    {
        var text = "movzx";
        var (_, prefix) = AsmCompletion.GetWordBeforeCaret(text, 3);

        Assert.Equal("mov", prefix);
    }

    [Fact]
    public void GetWordBeforeCaret_DespuesDeUnEspacioNoHayPrefijo()
    {
        var text = "mov ";
        var (start, prefix) = AsmCompletion.GetWordBeforeCaret(text, text.Length);

        Assert.Equal("", prefix);
        Assert.Equal(4, start);
    }

    [Theory]
    [InlineData(null, 0)]
    [InlineData("", 0)]
    public void GetWordBeforeCaret_TextoVacio(string? text, int caret)
    {
        var (_, prefix) = AsmCompletion.GetWordBeforeCaret(text, caret);
        Assert.Equal("", prefix);
    }

    [Fact]
    public void GetWordBeforeCaret_CaretFueraDeRangoNoRevienta()
    {
        var (_, prefix) = AsmCompletion.GetWordBeforeCaret("mov", 999);
        Assert.Equal("mov", prefix);

        var (_, prefix2) = AsmCompletion.GetWordBeforeCaret("mov", -5);
        Assert.Equal("", prefix2);
    }
}
