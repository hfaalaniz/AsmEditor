using AsmEditor.Core;

namespace AsmEditor.Tests;

public class OpenDocumentsTests
{
    private static DocumentState Doc(string? path = null, bool dirty = false) =>
        new(path) { IsDirty = dirty };

    // ---------------- Alta y activo ----------------

    [Fact]
    public void Nueva_ColeccionVaciaNoTieneActivo()
    {
        var docs = new OpenDocuments();

        Assert.Equal(0, docs.Count);
        Assert.Equal(-1, docs.ActiveIndex);
        Assert.Null(docs.Active);
        Assert.False(docs.AnyDirty);
    }

    [Fact]
    public void Add_DejaElNuevoDocumentoActivo()
    {
        var docs = new OpenDocuments();
        docs.Add(Doc(@"C:\a\uno.asm"));
        var segundo = Doc(@"C:\a\dos.asm");
        docs.Add(segundo);

        Assert.Equal(2, docs.Count);
        Assert.Equal(1, docs.ActiveIndex);
        Assert.Same(segundo, docs.Active);
    }

    // ---------------- La regla central: no duplicar pestañas ----------------

    [Fact]
    public void TryActivateExisting_ArchivoYaAbiertoActivaSuPestana()
    {
        var docs = new OpenDocuments();
        docs.Add(Doc(@"C:\a\uno.asm"));
        docs.Add(Doc(@"C:\a\dos.asm"));
        docs.Add(Doc(@"C:\a\tres.asm"));

        bool yaEstaba = docs.TryActivateExisting(@"C:\a\uno.asm");

        Assert.True(yaEstaba);
        Assert.Equal(0, docs.ActiveIndex);
        Assert.Equal(3, docs.Count); // no se agregó nada
    }

    [Fact]
    public void TryActivateExisting_ArchivoNuevoDevuelveFalseYNoCambiaElActivo()
    {
        var docs = new OpenDocuments();
        docs.Add(Doc(@"C:\a\uno.asm"));

        bool yaEstaba = docs.TryActivateExisting(@"C:\a\otro.asm");

        Assert.False(yaEstaba);
        Assert.Equal(0, docs.ActiveIndex);
    }

    [Fact]
    public void TryActivateExisting_NoDistingueMayusculasNiSeparadores()
    {
        var docs = new OpenDocuments();
        docs.Add(Doc(@"C:\Users\Fabian\NASM\Win6Win.asm"));

        Assert.True(docs.TryActivateExisting(@"c:\users\fabian\nasm\win6win.asm"));
        Assert.True(docs.TryActivateExisting(@"C:/Users/Fabian/NASM/Win6Win.asm"));
    }

    [Fact]
    public void TryActivateExisting_ResuelveRutasRelativas()
    {
        var docs = new OpenDocuments();
        docs.Add(Doc(@"C:\a\b\..\uno.asm"));

        Assert.True(docs.TryActivateExisting(@"C:\a\uno.asm"));
    }

    [Fact]
    public void IndexOfPath_DocumentosSinGuardarNuncaCoinciden()
    {
        var docs = new OpenDocuments();
        docs.Add(Doc());  // sin título
        docs.Add(Doc());  // sin título

        Assert.Equal(-1, docs.IndexOfPath(@"C:\a\uno.asm"));
        Assert.Equal(-1, docs.IndexOfPath(null));
    }

    [Fact]
    public void DosDocumentosSinTituloSonDistintos()
    {
        var docs = new OpenDocuments();
        var a = Doc();
        var b = Doc();
        docs.Add(a);
        docs.Add(b);

        Assert.NotEqual(a.DisplayName, b.DisplayName);
    }

    // ---------------- Cierre de pestañas y reajuste del activo ----------------

    [Fact]
    public void RemoveAt_CerrarLaDelMedioDejaActivaLaQueOcupaSuLugar()
    {
        var docs = new OpenDocuments();
        docs.Add(Doc(@"C:\a\uno.asm"));
        docs.Add(Doc(@"C:\a\dos.asm"));
        docs.Add(Doc(@"C:\a\tres.asm"));
        docs.SetActive(1); // activa 'dos'

        docs.RemoveAt(1);

        Assert.Equal(2, docs.Count);
        Assert.Equal(1, docs.ActiveIndex);
        Assert.Equal("tres.asm", docs.Active!.DisplayName);
    }

    [Fact]
    public void RemoveAt_CerrarLaUltimaRetrocedeElActivo()
    {
        var docs = new OpenDocuments();
        docs.Add(Doc(@"C:\a\uno.asm"));
        docs.Add(Doc(@"C:\a\dos.asm"));
        docs.SetActive(1);

        docs.RemoveAt(1);

        Assert.Equal(0, docs.ActiveIndex);
        Assert.Equal("uno.asm", docs.Active!.DisplayName);
    }

    [Fact]
    public void RemoveAt_CerrarUnaAnteriorMantieneElMismoDocumentoActivo()
    {
        var docs = new OpenDocuments();
        docs.Add(Doc(@"C:\a\uno.asm"));
        docs.Add(Doc(@"C:\a\dos.asm"));
        docs.Add(Doc(@"C:\a\tres.asm"));
        docs.SetActive(2);
        var activoAntes = docs.Active;

        docs.RemoveAt(0);

        Assert.Equal(1, docs.ActiveIndex);
        Assert.Same(activoAntes, docs.Active);
    }

    [Fact]
    public void RemoveAt_CerrarLaUnicaDejaLaColeccionSinActivo()
    {
        var docs = new OpenDocuments();
        docs.Add(Doc(@"C:\a\uno.asm"));

        docs.RemoveAt(0);

        Assert.Equal(0, docs.Count);
        Assert.Equal(-1, docs.ActiveIndex);
        Assert.Null(docs.Active);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(5)]
    public void RemoveAt_IndiceInvalidoNoHaceNada(int indice)
    {
        var docs = new OpenDocuments();
        docs.Add(Doc(@"C:\a\uno.asm"));

        docs.RemoveAt(indice);

        Assert.Equal(1, docs.Count);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(9)]
    public void SetActive_IndiceInvalidoNoCambiaElActivo(int indice)
    {
        var docs = new OpenDocuments();
        docs.Add(Doc(@"C:\a\uno.asm"));
        docs.Add(Doc(@"C:\a\dos.asm"));
        docs.SetActive(0);

        docs.SetActive(indice);

        Assert.Equal(0, docs.ActiveIndex);
    }

    // ---------------- Estado sucio ----------------

    [Fact]
    public void AnyDirty_DetectaCualquierDocumentoSinGuardar()
    {
        var docs = new OpenDocuments();
        docs.Add(Doc(@"C:\a\uno.asm"));
        docs.Add(Doc(@"C:\a\dos.asm", dirty: true));

        Assert.True(docs.AnyDirty);
        Assert.Single(docs.Dirty);
    }

    [Fact]
    public void AnyDirty_FalsoCuandoTodoEstaGuardado()
    {
        var docs = new OpenDocuments();
        docs.Add(Doc(@"C:\a\uno.asm"));
        docs.Add(Doc(@"C:\a\dos.asm"));

        Assert.False(docs.AnyDirty);
        Assert.Empty(docs.Dirty);
    }

    // ---------------- Nombre visible en la pestaña ----------------

    [Fact]
    public void DisplayName_ArchivoGuardadoMuestraSoloElNombre()
    {
        var d = Doc(@"C:\Users\Fabian\NASM\Win6Win.asm");

        Assert.Equal("Win6Win.asm", d.DisplayName);
    }

    [Fact]
    public void DisplayName_MarcaConAsteriscoLoSucio()
    {
        var d = Doc(@"C:\a\uno.asm", dirty: true);

        Assert.Equal("uno.asm *", d.DisplayName);
    }

    [Fact]
    public void DisplayName_SinTituloUsaSuNumero()
    {
        var d = Doc();

        Assert.StartsWith("Sin título ", d.DisplayName);
    }

    [Fact]
    public void DisplayName_ElNumeroDeSinTituloNoCambiaAlEnsuciarse()
    {
        var d = Doc();
        var limpio = d.DisplayName;

        d.IsDirty = true;

        Assert.Equal(limpio + " *", d.DisplayName);
    }

    // ---------------- Comparación de rutas ----------------

    [Fact]
    public void SamePath_CasosBasicos()
    {
        Assert.True(PathComparer.SamePath(@"C:\a\uno.asm", @"C:\a\uno.asm"));
        Assert.True(PathComparer.SamePath(@"C:\a\uno.asm", @"C:\A\UNO.ASM"));
        Assert.False(PathComparer.SamePath(@"C:\a\uno.asm", @"C:\a\dos.asm"));
        Assert.False(PathComparer.SamePath(null, @"C:\a\uno.asm"));
        Assert.False(PathComparer.SamePath(@"C:\a\uno.asm", null));
        Assert.False(PathComparer.SamePath("", ""));
    }
}
