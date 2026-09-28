namespace AsmEditor.Core.Acople;

/// <summary>De qué grupo es una guía: las de los bordes del editor o el rombo sobre los documentos.</summary>
public enum TipoGuia
{
    Borde = 0,
    Rombo = 1
}

/// <summary>Una guía de acople: a qué zona lleva, de qué grupo es y dónde se dibuja (en pantalla).</summary>
public readonly record struct GuiaAcople(ZonaAcople Zona, TipoGuia Tipo, ScreenRect Rect);

/// <summary>
/// Cómo están las zonas en este momento, en pantalla: lo que hace falta para
/// saber dónde quedaría un panel acoplado en cada una (la vista previa).
/// </summary>
public sealed class DisposicionZonas
{
    /// <summary>Donde viven las zonas: el anfitrión menos las franjas de los auto-ocultos.</summary>
    public ScreenRect Area { get; init; }

    /// <summary>Cada zona que ocupa lugar, o null si está vacía.</summary>
    public ScreenRect? Izquierda { get; init; }
    public ScreenRect? Derecha { get; init; }
    public ScreenRect? Abajo { get; init; }

    /// <summary>Ancho de un divisor (el Splitter entre una zona y el centro).</summary>
    public int Divisor { get; init; }

    /// <summary>El tamaño que tomaría cada zona vacía: el guardado, o el de fábrica.</summary>
    public int AnchoIzquierda { get; init; }
    public int AnchoDerecha { get; init; }
    public int AltoAbajo { get; init; }

    public ScreenRect? Ocupada(ZonaAcople zona) => zona switch
    {
        ZonaAcople.Izquierda => Izquierda,
        ZonaAcople.Derecha => Derecha,
        _ => Abajo
    };
}

/// <summary>
/// La geometría de acoplar arrastrando (3e de PLAN_IDE.md), sin ventanas:
/// dónde se dibujan las guías, cuál está bajo el ratón y qué rectángulo
/// ocuparía el panel si se suelta ahí. La interfaz (AnfitrionAcople,
/// GuiasAcople) solo la dibuja.
///
/// Las guías son como las de Visual Studio (decisión de Fabián, 28/09): una
/// en el medio de cada borde del editor y un rombo en el centro de los
/// documentos, con una flecha por zona alrededor de un cuadro central que no
/// acopla. ⚠ Con tres zonas (sin «arriba» ni zonas divididas por dentro), la
/// guía de borde y la flecha del rombo de un mismo lado llevan al MISMO
/// lugar: la zona de ese lado. Es a propósito (el aspecto de VS), no un error.
///
/// Todo en coordenadas de pantalla, con <see cref="ScreenRect"/>: Core no
/// usa System.Drawing (ver UiState.cs).
/// </summary>
public static class GeometriaAcople
{
    /// <summary>Lado de cada guía, en píxeles lógicos (a 96 ppp): la interfaz lo escala.</summary>
    public const int LadoGuia = 32;

    /// <summary>
    /// Las seis guías: las tres flechas del rombo sobre <paramref name="centro"/>
    /// (los documentos) y las tres de los bordes de <paramref name="anfitrion"/>.
    /// El margen al borde y la separación del rombo son proporcionales al lado,
    /// para que se vean igual con cualquier escala.
    ///
    /// El rombo va primero: si con una ventana muy chica una flecha pisara una
    /// guía de borde, gana la flecha, que es la que el usuario ve encima.
    /// </summary>
    public static IReadOnlyList<GuiaAcople> Guias(ScreenRect anfitrion, ScreenRect centro, int lado)
    {
        int margen = lado / 4;
        int separacion = lado / 8;
        var c = CentroDelRombo(centro, lado);

        return new[]
        {
            new GuiaAcople(ZonaAcople.Izquierda, TipoGuia.Rombo, new ScreenRect(c.X - separacion - lado, c.Y, lado, lado)),
            new GuiaAcople(ZonaAcople.Derecha, TipoGuia.Rombo, new ScreenRect(c.Right + separacion, c.Y, lado, lado)),
            new GuiaAcople(ZonaAcople.Abajo, TipoGuia.Rombo, new ScreenRect(c.X, c.Bottom + separacion, lado, lado)),

            new GuiaAcople(ZonaAcople.Izquierda, TipoGuia.Borde, new ScreenRect(anfitrion.X + margen, anfitrion.Y + anfitrion.Height / 2 - lado / 2, lado, lado)),
            new GuiaAcople(ZonaAcople.Derecha, TipoGuia.Borde, new ScreenRect(anfitrion.Right - margen - lado, anfitrion.Y + anfitrion.Height / 2 - lado / 2, lado, lado)),
            new GuiaAcople(ZonaAcople.Abajo, TipoGuia.Borde, new ScreenRect(anfitrion.X + anfitrion.Width / 2 - lado / 2, anfitrion.Bottom - margen - lado, lado, lado))
        };
    }

    /// <summary>El cuadro central del rombo, en el medio de los documentos. No acopla: marca el grupo.</summary>
    public static ScreenRect CentroDelRombo(ScreenRect centro, int lado) =>
        new(centro.X + centro.Width / 2 - lado / 2, centro.Y + centro.Height / 2 - lado / 2, lado, lado);

    /// <summary>La guía bajo el punto (en pantalla), o null si no hay ninguna.</summary>
    public static GuiaAcople? GuiaEn(int x, int y, IReadOnlyList<GuiaAcople> guias)
    {
        foreach (var g in guias)
        {
            if (x >= g.Rect.X && x < g.Rect.Right && y >= g.Rect.Y && y < g.Rect.Bottom) return g;
        }
        return null;
    }

    /// <summary>
    /// Dónde quedaría el panel si se suelta en esa zona: la vista previa.
    ///
    /// ⚠ SALE DE LA DISPOSICIÓN REAL (lección del prototipo 0.4, que usaba
    /// proporciones fijas). Una zona que ya tiene paneles es esa zona: el
    /// panel entra como una pestaña más. Una vacía se calcula como la armaría
    /// el anfitrión: las laterales a todo el alto del área y la de abajo ENTRE
    /// las laterales que se ven, cada una con su tamaño guardado.
    /// </summary>
    public static ScreenRect VistaPrevia(ZonaAcople zona, DisposicionZonas d)
    {
        if (d.Ocupada(zona) is { } ocupada) return ocupada;

        var a = d.Area;
        switch (zona)
        {
            case ZonaAcople.Izquierda:
                return new ScreenRect(a.X, a.Y, Math.Min(d.AnchoIzquierda, a.Width), a.Height);

            case ZonaAcople.Derecha:
                int ancho = Math.Min(d.AnchoDerecha, a.Width);
                return new ScreenRect(a.Right - ancho, a.Y, ancho, a.Height);

            default:
                int izquierda = d.Izquierda is { } i ? i.Right + d.Divisor : a.X;
                int derecha = d.Derecha is { } r ? r.X - d.Divisor : a.Right;
                int alto = Math.Min(d.AltoAbajo, a.Height);
                return new ScreenRect(izquierda, a.Bottom - alto, Math.Max(0, derecha - izquierda), alto);
        }
    }
}
