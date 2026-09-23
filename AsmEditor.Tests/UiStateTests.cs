using AsmEditor.Core;

namespace AsmEditor.Tests;

public class RecentFilesTests
{
    [Fact]
    public void AddRecent_ElUltimoUsadoQuedaPrimero()
    {
        var s = new UiState();

        s.AddRecent(@"C:\a\uno.asm");
        s.AddRecent(@"C:\a\dos.asm");

        Assert.Equal(@"C:\a\dos.asm", s.RecentFiles[0]);
        Assert.Equal(@"C:\a\uno.asm", s.RecentFiles[1]);
    }

    [Fact]
    public void AddRecent_ReabrirUnoExistenteLoSubeSinDuplicar()
    {
        var s = new UiState();
        s.AddRecent(@"C:\a\uno.asm");
        s.AddRecent(@"C:\a\dos.asm");
        s.AddRecent(@"C:\a\tres.asm");

        s.AddRecent(@"C:\a\uno.asm");

        Assert.Equal(3, s.RecentFiles.Count);
        Assert.Equal(@"C:\a\uno.asm", s.RecentFiles[0]);
    }

    [Fact]
    public void AddRecent_NoDuplicaPorMayusculasNiSeparadores()
    {
        var s = new UiState();
        s.AddRecent(@"C:\Users\Fabian\NASM\Win6Win.asm");

        s.AddRecent(@"c:/users/fabian/nasm/win6win.asm");

        Assert.Single(s.RecentFiles);
    }

    [Fact]
    public void AddRecent_RespetaElTope()
    {
        var s = new UiState();

        for (int i = 0; i < UiState.MaxRecientes + 5; i++)
        {
            s.AddRecent($@"C:\a\archivo{i}.asm");
        }

        Assert.Equal(UiState.MaxRecientes, s.RecentFiles.Count);
        // El más nuevo quedó y el más viejo se cayó.
        Assert.Equal($@"C:\a\archivo{UiState.MaxRecientes + 4}.asm", s.RecentFiles[0]);
        Assert.DoesNotContain(@"C:\a\archivo0.asm", s.RecentFiles);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void AddRecent_IgnoraRutasVacias(string? path)
    {
        var s = new UiState();

        s.AddRecent(path);

        Assert.Empty(s.RecentFiles);
    }

    [Fact]
    public void RemoveRecent_QuitaElArchivo()
    {
        var s = new UiState();
        s.AddRecent(@"C:\a\uno.asm");
        s.AddRecent(@"C:\a\dos.asm");

        s.RemoveRecent(@"C:\a\uno.asm");

        Assert.Single(s.RecentFiles);
        Assert.Equal(@"C:\a\dos.asm", s.RecentFiles[0]);
    }

    [Fact]
    public void PurgeMissingRecents_SacaLosQueYaNoExisten()
    {
        var s = new UiState();
        s.AddRecent(@"C:\a\existe.asm");
        s.AddRecent(@"C:\a\borrado.asm");

        s.PurgeMissingRecents(p => p.Contains("existe"));

        Assert.Single(s.RecentFiles);
        Assert.Equal(@"C:\a\existe.asm", s.RecentFiles[0]);
    }

    [Fact]
    public void EnsureValid_QuitaDuplicadosConservandoElMasNuevo()
    {
        // Un settings.json editado a mano puede traer duplicados.
        var s = new UiState
        {
            RecentFiles = new List<string>
            {
                @"C:\a\uno.asm",
                @"C:\a\dos.asm",
                @"C:\A\UNO.ASM"
            }
        };

        s.EnsureValid();

        Assert.Equal(2, s.RecentFiles.Count);
        Assert.Equal(@"C:\a\uno.asm", s.RecentFiles[0]);
    }

    [Fact]
    public void EnsureValid_ToleraListasNulas()
    {
        var s = new UiState { RecentFiles = null!, OpenFiles = null!, Window = null! };

        s.EnsureValid();

        Assert.NotNull(s.RecentFiles);
        Assert.NotNull(s.OpenFiles);
        Assert.NotNull(s.Window);
    }

    [Fact]
    public void EnsureValid_CorrigeElIndiceActivoFueraDeRango()
    {
        var s = new UiState
        {
            OpenFiles = new List<string> { @"C:\a\uno.asm", @"C:\a\dos.asm" },
            ActiveFileIndex = 99
        };

        s.EnsureValid();

        Assert.Equal(1, s.ActiveFileIndex);
    }

    [Fact]
    public void EnsureValid_SinArchivosAbiertosElIndiceEsCero()
    {
        var s = new UiState { OpenFiles = new List<string>(), ActiveFileIndex = 5 };

        s.EnsureValid();

        Assert.Equal(0, s.ActiveFileIndex);
    }
}

public class WindowGeometryTests
{
    /// <summary>Un monitor 1920×1080 en el origen.</summary>
    private static readonly ScreenRect Monitor = new(0, 0, 1920, 1080);

    /// <summary>Un segundo monitor a la izquierda, con coordenadas negativas.</summary>
    private static readonly ScreenRect MonitorIzquierdo = new(-1920, 0, 1920, 1080);

    [Fact]
    public void VentanaEnElCentroEsVisible()
    {
        var g = new WindowGeometry { X = 300, Y = 200, Width = 800, Height = 600 };

        Assert.True(g.EsVisibleEn(new[] { Monitor }));
    }

    [Fact]
    public void VentanaCompletamenteFueraNoEsVisible()
    {
        // El caso real: se desconectó el monitor donde estaba.
        var g = new WindowGeometry { X = 3000, Y = 200, Width = 800, Height = 600 };

        Assert.False(g.EsVisibleEn(new[] { Monitor }));
    }

    [Fact]
    public void VentanaEnUnSegundoMonitorConCoordenadasNegativas()
    {
        var g = new WindowGeometry { X = -1500, Y = 100, Width = 800, Height = 600 };

        Assert.True(g.EsVisibleEn(new[] { Monitor, MonitorIzquierdo }));
    }

    [Fact]
    public void EsaMismaVentanaNoEsVisibleSiSeDesconectaElMonitor()
    {
        var g = new WindowGeometry { X = -1500, Y = 100, Width = 800, Height = 600 };

        Assert.False(g.EsVisibleEn(new[] { Monitor }));
    }

    [Fact]
    public void UnaFranjaVisibleAlcanza()
    {
        // Arrastrada casi entera fuera del borde derecho, pero queda un pedazo
        // suficiente para agarrarla con el mouse.
        var g = new WindowGeometry { X = 1820, Y = 100, Width = 800, Height = 600 };

        Assert.True(g.EsVisibleEn(new[] { Monitor }, minimoVisible: 80));
    }

    [Fact]
    public void UnaFranjaDemasiadoChicaNoAlcanza()
    {
        var g = new WindowGeometry { X = 1900, Y = 100, Width = 800, Height = 600 };

        Assert.False(g.EsVisibleEn(new[] { Monitor }, minimoVisible: 80));
    }

    [Theory]
    [InlineData(0, 600)]
    [InlineData(800, 0)]
    [InlineData(-10, 600)]
    public void TamanoInvalidoNoEsVisible(int ancho, int alto)
    {
        var g = new WindowGeometry { X = 100, Y = 100, Width = ancho, Height = alto };

        Assert.False(g.EsVisibleEn(new[] { Monitor }));
    }

    [Fact]
    public void SinMonitoresNoEsVisible()
    {
        var g = new WindowGeometry { X = 100, Y = 100, Width = 800, Height = 600 };

        Assert.False(g.EsVisibleEn(Array.Empty<ScreenRect>()));
    }

    [Fact]
    public void PorDefectoNoEstaGuardada()
    {
        Assert.False(new WindowGeometry().Saved);
    }
}

public class ScreenRectTests
{
    [Fact]
    public void Overlap_DeDosQueSeCruzan()
    {
        var a = new ScreenRect(0, 0, 100, 100);
        var b = new ScreenRect(50, 50, 100, 100);

        var (w, h) = a.OverlapWith(b);

        Assert.Equal(50, w);
        Assert.Equal(50, h);
    }

    [Fact]
    public void Overlap_DeDosQueNoSeTocanEsCero()
    {
        var a = new ScreenRect(0, 0, 100, 100);
        var b = new ScreenRect(500, 500, 100, 100);

        var (w, h) = a.OverlapWith(b);

        Assert.Equal(0, w);
        Assert.Equal(0, h);
    }

    [Fact]
    public void Overlap_NuncaEsNegativo()
    {
        var a = new ScreenRect(0, 0, 10, 10);
        var b = new ScreenRect(100, 100, 10, 10);

        var (w, h) = a.OverlapWith(b);

        Assert.True(w >= 0 && h >= 0);
    }
}
