using AsmEditor.Core;
using AsmEditor.Core.Acople;

namespace AsmEditor.Tests;

/// <summary>
/// La geometría de acoplar arrastrando (3e de PLAN_IDE.md): dónde van las
/// guías, cuál está bajo el ratón y la vista previa sacada de la disposición
/// real de las zonas.
/// </summary>
public class GeometriaAcopleTests
{
    private const int Lado = GeometriaAcople.LadoGuia;

    /// <summary>El anfitrión en pantalla, corrido del origen para que se note un cálculo relativo a 0,0.</summary>
    private static readonly ScreenRect Anfitrion = new(100, 50, 1200, 800);

    /// <summary>Los documentos: sin zona izquierda, el explorador a la derecha (280 + 5) y abajo (220 + 5).</summary>
    private static readonly ScreenRect Documentos = new(100, 50, 915, 575);

    private static IReadOnlyList<GuiaAcople> Guias() => GeometriaAcople.Guias(Anfitrion, Documentos, Lado);

    private static GuiaAcople Guia(ZonaAcople zona, TipoGuia tipo) =>
        Guias().Single(g => g.Zona == zona && g.Tipo == tipo);

    private static (int X, int Y) Medio(ScreenRect r) => (r.X + r.Width / 2, r.Y + r.Height / 2);

    // ---------------- Guías ----------------

    [Fact]
    public void Guias_SonSeis_UnaPorZonaEnCadaGrupo()
    {
        var guias = Guias();

        Assert.Equal(6, guias.Count);
        foreach (var tipo in Enum.GetValues<TipoGuia>())
            Assert.Equal(Enum.GetValues<ZonaAcople>(), guias.Where(g => g.Tipo == tipo).Select(g => g.Zona).OrderBy(z => z));
        Assert.All(guias, g => Assert.Equal((Lado, Lado), (g.Rect.Width, g.Rect.Height)));
    }

    [Fact]
    public void Bordes_PegadasASuBorde_YAlMedioDelOtroEje()
    {
        var izq = Guia(ZonaAcople.Izquierda, TipoGuia.Borde).Rect;
        var der = Guia(ZonaAcople.Derecha, TipoGuia.Borde).Rect;
        var aba = Guia(ZonaAcople.Abajo, TipoGuia.Borde).Rect;
        int margen = Lado / 4;

        Assert.Equal(Anfitrion.X + margen, izq.X);
        Assert.Equal(Anfitrion.Right - margen, der.Right);
        Assert.Equal(Anfitrion.Bottom - margen, aba.Bottom);

        int medioY = Anfitrion.Y + Anfitrion.Height / 2;
        Assert.Equal(medioY, Medio(izq).Y);
        Assert.Equal(medioY, Medio(der).Y);
        Assert.Equal(Anfitrion.X + Anfitrion.Width / 2, Medio(aba).X);
    }

    /// <summary>El rombo va sobre los DOCUMENTOS, no sobre todo el anfitrión: cada flecha a su lado del cuadro central.</summary>
    [Fact]
    public void Rombo_AlrededorDelCentroDeLosDocumentos()
    {
        var c = GeometriaAcople.CentroDelRombo(Documentos, Lado);
        Assert.Equal(Medio(Documentos), Medio(c));

        var izq = Guia(ZonaAcople.Izquierda, TipoGuia.Rombo).Rect;
        var der = Guia(ZonaAcople.Derecha, TipoGuia.Rombo).Rect;
        var aba = Guia(ZonaAcople.Abajo, TipoGuia.Rombo).Rect;

        Assert.True(izq.Right <= c.X && izq.Y == c.Y, $"izquierda {izq} / centro {c}");
        Assert.True(der.X >= c.Right && der.Y == c.Y, $"derecha {der} / centro {c}");
        Assert.True(aba.Y >= c.Bottom && aba.X == c.X, $"abajo {aba} / centro {c}");

        // Pegadas al cuadro, no en cualquier lado a la izquierda/derecha/abajo.
        Assert.True(c.X - izq.Right <= Lado / 4 && der.X - c.Right <= Lado / 4 && aba.Y - c.Bottom <= Lado / 4);
    }

    [Fact]
    public void Guias_NoSePisan_NiConElCuadroCentral()
    {
        var rects = Guias().Select(g => g.Rect).Append(GeometriaAcople.CentroDelRombo(Documentos, Lado)).ToList();

        // OverlapWith recorta cada eje por separado: se pisan solo si los DOS son > 0.
        for (int i = 0; i < rects.Count; i++)
            for (int j = i + 1; j < rects.Count; j++)
            {
                var (w, h) = rects[i].OverlapWith(rects[j]);
                Assert.True(w == 0 || h == 0, $"{rects[i]} pisa {rects[j]}");
            }
    }

    [Fact]
    public void Guias_EscalanConElLado()
    {
        var grandes = GeometriaAcople.Guias(Anfitrion, Documentos, Lado * 2);
        Assert.All(grandes, g => Assert.Equal((Lado * 2, Lado * 2), (g.Rect.Width, g.Rect.Height)));
        Assert.Equal(Anfitrion.X + Lado / 2, grandes.Single(g => g.Zona == ZonaAcople.Izquierda && g.Tipo == TipoGuia.Borde).Rect.X);
    }

    // ---------------- Bajo el ratón ----------------

    [Fact]
    public void GuiaEn_ElMedioDeCadaGuia_EsEsaGuia()
    {
        var guias = Guias();
        foreach (var g in guias)
        {
            var (x, y) = Medio(g.Rect);
            Assert.Equal(g, GeometriaAcople.GuiaEn(x, y, guias));
        }
    }

    /// <summary>El borde derecho e inferior NO son de la guía (como Rectangle.Contains): una guía no se come un píxel de la vecina.</summary>
    [Fact]
    public void GuiaEn_LosBordesDeLaGuia()
    {
        var guias = Guias();
        var r = Guia(ZonaAcople.Derecha, TipoGuia.Borde).Rect;

        Assert.NotNull(GeometriaAcople.GuiaEn(r.X, r.Y, guias));
        Assert.NotNull(GeometriaAcople.GuiaEn(r.Right - 1, r.Bottom - 1, guias));
        Assert.Null(GeometriaAcople.GuiaEn(r.Right, r.Y, guias));
        Assert.Null(GeometriaAcople.GuiaEn(r.X, r.Bottom, guias));
        Assert.Null(GeometriaAcople.GuiaEn(r.X - 1, r.Y, guias));
    }

    [Fact]
    public void GuiaEn_FueraDeLasGuias_EsNull()
    {
        var guias = Guias();
        var (cx, cy) = Medio(GeometriaAcople.CentroDelRombo(Documentos, Lado));

        Assert.Null(GeometriaAcople.GuiaEn(cx, cy, guias));                               // el cuadro central
        Assert.Null(GeometriaAcople.GuiaEn(Anfitrion.X + 2, Anfitrion.Y + 2, guias));      // una esquina
        Assert.Null(GeometriaAcople.GuiaEn(0, 0, guias));                                  // fuera del editor
    }

    // ---------------- Vista previa ----------------

    /// <summary>La disposición de fábrica: sin izquierda, el explorador a la derecha y la zona de abajo entre las laterales.</summary>
    private static DisposicionZonas DeFabrica(ScreenRect? izquierda = null, ScreenRect? derecha = null, ScreenRect? abajo = null) => new()
    {
        Area = Anfitrion,
        Izquierda = izquierda,
        Derecha = derecha,
        Abajo = abajo,
        Divisor = 5,
        AnchoIzquierda = 240,
        AnchoDerecha = 280,
        AltoAbajo = 220
    };

    private static readonly ScreenRect ZonaDerecha = new(1020, 50, 280, 800);
    private static readonly ScreenRect ZonaAbajo = new(100, 630, 915, 220);

    /// <summary>
    /// Una zona con paneles: el panel entra como pestaña, así que la vista
    /// previa es la zona entera, COMO ESTÁ, aunque no mida lo guardado.
    ///
    /// ⚠ Las zonas de acá miden DISTINTO de lo que daría el cálculo de una
    /// zona vacía (la derecha más angosta, la de abajo más alta): con
    /// ZonaDerecha y ZonaAbajo, que coinciden con ese cálculo, la prueba
    /// pasaba igual sin mirar la zona ocupada (romper_diseno_acople, 28/09).
    /// </summary>
    [Fact]
    public void VistaPrevia_ZonaOcupada_EsLaZona()
    {
        var derecha = new ScreenRect(1150, 50, 150, 800);
        var abajo = new ScreenRect(100, 550, 1045, 300);
        var d = DeFabrica(derecha: derecha, abajo: abajo);

        Assert.Equal(abajo, GeometriaAcople.VistaPrevia(ZonaAcople.Abajo, d));
        Assert.Equal(derecha, GeometriaAcople.VistaPrevia(ZonaAcople.Derecha, d));
    }

    /// <summary>La izquierda vacía toma todo el alto (la de abajo queda entre las laterales), con su ancho guardado.</summary>
    [Fact]
    public void VistaPrevia_IzquierdaVacia_TodoElAlto_PegadaALaIzquierda()
    {
        var d = DeFabrica(derecha: ZonaDerecha, abajo: ZonaAbajo);

        Assert.Equal(new ScreenRect(Anfitrion.X, Anfitrion.Y, 240, Anfitrion.Height), GeometriaAcople.VistaPrevia(ZonaAcople.Izquierda, d));
    }

    [Fact]
    public void VistaPrevia_DerechaVacia_TodoElAlto_PegadaALaDerecha()
    {
        var d = DeFabrica(abajo: ZonaAbajo);

        Assert.Equal(new ScreenRect(Anfitrion.Right - 280, Anfitrion.Y, 280, Anfitrion.Height), GeometriaAcople.VistaPrevia(ZonaAcople.Derecha, d));
    }

    /// <summary>La de abajo vacía va ENTRE las laterales que se ven (y sus divisores), no a todo el ancho.</summary>
    [Fact]
    public void VistaPrevia_AbajoVacia_EntreLasLateralesVisibles()
    {
        var izquierda = new ScreenRect(100, 50, 240, 800);
        var d = DeFabrica(izquierda: izquierda, derecha: ZonaDerecha);

        var r = GeometriaAcople.VistaPrevia(ZonaAcople.Abajo, d);

        Assert.Equal(izquierda.Right + 5, r.X);
        Assert.Equal(ZonaDerecha.X - 5, r.Right);
        Assert.Equal(Anfitrion.Bottom, r.Bottom);
        Assert.Equal(220, r.Height);
    }

    [Fact]
    public void VistaPrevia_AbajoVacia_SinLaterales_TodoElAncho()
    {
        var r = GeometriaAcople.VistaPrevia(ZonaAcople.Abajo, DeFabrica());

        Assert.Equal(new ScreenRect(Anfitrion.X, Anfitrion.Bottom - 220, Anfitrion.Width, 220), r);
    }

    /// <summary>El área son las zonas, no el anfitrión: con franjas de auto-ocultos, la vista previa queda adentro.</summary>
    [Fact]
    public void VistaPrevia_UsaElAreaDeLasZonas()
    {
        var area = new ScreenRect(124, 50, 1152, 776);   // franjas de 24 px a la izquierda, derecha y abajo
        var d = new DisposicionZonas { Area = area, Divisor = 5, AnchoIzquierda = 240, AnchoDerecha = 280, AltoAbajo = 220 };

        Assert.Equal(new ScreenRect(124, 50, 240, 776), GeometriaAcople.VistaPrevia(ZonaAcople.Izquierda, d));
        Assert.Equal(new ScreenRect(area.Right - 280, 50, 280, 776), GeometriaAcople.VistaPrevia(ZonaAcople.Derecha, d));
        Assert.Equal(area.Bottom, GeometriaAcople.VistaPrevia(ZonaAcople.Abajo, d).Bottom);
    }

    /// <summary>Un tamaño guardado más grande que el área (la ventana se achicó) se recorta al área.</summary>
    [Fact]
    public void VistaPrevia_TamanoMayorQueElArea_SeRecorta()
    {
        var chica = new ScreenRect(0, 0, 200, 150);
        var d = new DisposicionZonas { Area = chica, Divisor = 5, AnchoIzquierda = 240, AnchoDerecha = 280, AltoAbajo = 220 };

        Assert.Equal(chica, GeometriaAcople.VistaPrevia(ZonaAcople.Izquierda, d));
        Assert.Equal(chica, GeometriaAcople.VistaPrevia(ZonaAcople.Derecha, d));
        Assert.Equal(chica, GeometriaAcople.VistaPrevia(ZonaAcople.Abajo, d));
    }
}
