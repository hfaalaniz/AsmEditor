using AsmEditor.Core;
using AsmEditor.Core.Acople;

namespace AsmEditor;

/// <summary>
/// Las guías de acoplar arrastrando (3e del plan): una ventana transparente
/// sobre el anfitrión que dibuja las guías de los bordes y el rombo de los
/// documentos (<see cref="GeometriaAcople"/>). El fondo magenta es el color
/// transparente (TransparencyKey): solo se ven los cuadros.
///
/// No se activa ni recibe el ratón (WS_EX_NOACTIVATE | WS_EX_TRANSPARENT):
/// mientras se arrastra, el ratón lo tiene el movimiento nativo de la
/// ventana flotante, y las guías solo se dibujan encima. Técnica del
/// prototipo 0.4 (diagnostico\prototipos\acople).
///
/// No decide nada: el anfitrión le pasa las guías y cuál está bajo el ratón.
/// </summary>
public partial class GuiasAcople : Form
{
    private const int WS_EX_TRANSPARENT = 0x00000020;
    private const int WS_EX_TOOLWINDOW = 0x00000080;
    private const int WS_EX_NOACTIVATE = 0x08000000;

    private IReadOnlyList<GuiaAcople> _guias = Array.Empty<GuiaAcople>();
    private ScreenRect _centroRombo;
    private GuiaAcople? _resaltada;

    public GuiasAcople()
    {
        InitializeComponent();
        DoubleBuffered = true;
    }

    protected override bool ShowWithoutActivation => true;

    protected override CreateParams CreateParams
    {
        get
        {
            var cp = base.CreateParams;
            cp.ExStyle |= WS_EX_TRANSPARENT | WS_EX_TOOLWINDOW | WS_EX_NOACTIVATE;
            return cp;
        }
    }

    /// <summary>
    /// Las muestra sobre el anfitrión (<paramref name="anfitrion"/>, en
    /// pantalla). Las guías y el cuadro central también vienen en pantalla.
    /// </summary>
    public void Mostrar(Rectangle anfitrion, IReadOnlyList<GuiaAcople> guias, ScreenRect centroRombo, GuiaAcople? resaltada, Form duenio)
    {
        bool cambio = _resaltada != resaltada || _centroRombo != centroRombo || !_guias.SequenceEqual(guias);
        _guias = guias;
        _centroRombo = centroRombo;
        _resaltada = resaltada;

        if (Bounds != anfitrion) Bounds = anfitrion;
        if (!Visible) Show(duenio);
        else if (cambio) Invalidate();
    }

    private void GuiasAcople_Paint(object? sender, PaintEventArgs e)
    {
        var g = e.Graphics;

        // El cuadro central del rombo, apagado: no acopla, marca el grupo.
        Cuadro(g, AlCliente(_centroRombo), Tema.Superficie3, null);

        foreach (var guia in _guias)
        {
            var fondo = guia == _resaltada ? Tema.Acento : Tema.Superficie2;
            Cuadro(g, AlCliente(guia.Rect), fondo, guia.Zona);
        }
    }

    /// <summary>De pantalla a esta ventana, que está sobre el anfitrión.</summary>
    private Rectangle AlCliente(ScreenRect r) => new(r.X - Left, r.Y - Top, r.Width, r.Height);

    /// <summary>Un cuadro de guía, con una franja del lado donde quedaría el panel.</summary>
    private static void Cuadro(Graphics g, Rectangle r, Color fondo, ZonaAcople? zona)
    {
        using (var b = new SolidBrush(fondo)) g.FillRectangle(b, r);
        using (var p = new Pen(Tema.Linea)) g.DrawRectangle(p, r.X, r.Y, r.Width - 1, r.Height - 1);

        if (zona is null) return;

        var m = Rectangle.Inflate(r, -r.Width / 5, -r.Height / 5);
        var franja = zona switch
        {
            ZonaAcople.Izquierda => new Rectangle(m.X, m.Y, m.Width / 3, m.Height),
            ZonaAcople.Derecha => new Rectangle(m.Right - m.Width / 3, m.Y, m.Width / 3, m.Height),
            _ => new Rectangle(m.X, m.Bottom - m.Height / 3, m.Width, m.Height / 3)
        };

        using (var b = new SolidBrush(Tema.Texto2)) g.FillRectangle(b, franja);
        using (var p = new Pen(Tema.Texto2)) g.DrawRectangle(p, m.X, m.Y, m.Width - 1, m.Height - 1);
    }
}
