using System.Drawing.Drawing2D;

namespace PruebaAcople;

/// <summary>
/// Las guías de acople: una ventana transparente sobre el anfitrión con los
/// cuadros de cada zona. El fondo magenta es el color transparente
/// (TransparencyKey): solo se ven los cuadros.
///
/// No se activa ni recibe el ratón (WS_EX_NOACTIVATE | WS_EX_TRANSPARENT):
/// mientras se arrastra, el ratón lo tiene el movimiento nativo de la
/// ventana flotante, y estas guías solo se dibujan encima.
/// </summary>
public partial class GuiasAcople : Form
{
    private const int WS_EX_TRANSPARENT = 0x00000020;
    private const int WS_EX_TOOLWINDOW = 0x00000080;
    private const int WS_EX_NOACTIVATE = 0x08000000;

    private Zona? _resaltada;
    private IReadOnlyCollection<Zona> _disponibles = Array.Empty<Zona>();

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

    /// <summary>Muestra las guías sobre el anfitrión (rectángulo en pantalla).</summary>
    public void Mostrar(Rectangle anfitrionEnPantalla, IReadOnlyCollection<Zona> disponibles, Zona? resaltada, Form duenio)
    {
        _disponibles = disponibles;
        _resaltada = resaltada;

        if (Bounds != anfitrionEnPantalla) Bounds = anfitrionEnPantalla;
        if (!Visible) Show(duenio);

        Invalidate();
    }

    private void GuiasAcople_Paint(object? sender, PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.None;

        var anfitrion = new Rectangle(Point.Empty, ClientSize);

        // El cuadro central, apagado: marca el grupo.
        Cuadro(g, GeometriaAcople.Centro(anfitrion), Color.FromArgb(60, 60, 64), null);

        foreach (var z in Enum.GetValues<Zona>())
        {
            if (!_disponibles.Contains(z)) continue;

            var fondo = z == _resaltada ? Color.FromArgb(212, 160, 23) : Color.FromArgb(45, 45, 48);
            Cuadro(g, GeometriaAcople.Guia(z, anfitrion), fondo, z);
        }
    }

    /// <summary>Un cuadro de guía con una marca que dice hacia dónde acopla.</summary>
    private static void Cuadro(Graphics g, Rectangle r, Color fondo, Zona? zona)
    {
        using (var b = new SolidBrush(fondo)) g.FillRectangle(b, r);
        using (var p = new Pen(Color.FromArgb(150, 150, 150))) g.DrawRectangle(p, r.X, r.Y, r.Width - 1, r.Height - 1);

        if (zona is null) return;

        // La franja muestra de qué lado queda el panel.
        var m = Rectangle.Inflate(r, -7, -7);
        var franja = zona switch
        {
            Zona.Izquierda => new Rectangle(m.X, m.Y, m.Width / 3, m.Height),
            Zona.Derecha => new Rectangle(m.Right - m.Width / 3, m.Y, m.Width / 3, m.Height),
            _ => new Rectangle(m.X, m.Bottom - m.Height / 3, m.Width, m.Height / 3)
        };

        using (var b = new SolidBrush(Color.FromArgb(220, 220, 220))) g.FillRectangle(b, franja);
        using (var p = new Pen(Color.FromArgb(220, 220, 220))) g.DrawRectangle(p, m);
    }
}
