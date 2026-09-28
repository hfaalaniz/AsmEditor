using System.ComponentModel;
using System.Drawing.Text;
using AsmEditor.Core.Acople;

namespace AsmEditor;

/// <summary>
/// La pestaña de un panel auto-oculto, en el borde del acople: en los
/// costados, con el texto vertical (se lee de arriba abajo), como en Visual
/// Studio; abajo, horizontal. Una barrita del lado de los documentos marca la
/// pestaña, en acento si su panel está desplegado.
///
/// ⚠ ES UNA ETIQUETA DE VERDAD (hereda de Label) y conserva su Text: así las
/// pruebas por interfaz la encuentran por el texto (WM_GETTEXT) aunque se
/// dibuje girada.
/// </summary>
public partial class PestanaBorde : Label
{
    private const int GrosorBarra = 3;

    private ZonaAcople _lado = ZonaAcople.Derecha;
    private bool _desplegada;
    private bool _encima;

    public PestanaBorde()
    {
        InitializeComponent();
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
    }

    /// <summary>De qué lado está: define si el texto va vertical y dónde va la barrita.</summary>
    [Category("Acople")]
    [DefaultValue(ZonaAcople.Derecha)]
    public ZonaAcople Lado
    {
        get => _lado;
        set { _lado = value; Invalidate(); }
    }

    /// <summary>Su panel está desplegado: la barrita va en acento.</summary>
    [Browsable(false)]
    public bool Desplegada
    {
        get => _desplegada;
        set { _desplegada = value; Invalidate(); }
    }

    [Browsable(false)]
    public bool Vertical => _lado != ZonaAcople.Abajo;

    /// <summary>El largo que necesita para su texto (alto si es vertical, ancho si no).</summary>
    public int LargoNecesario => TextRenderer.MeasureText(Text, Font).Width + 20;

    protected override void OnMouseEnter(EventArgs e) { _encima = true; Invalidate(); base.OnMouseEnter(e); }

    protected override void OnMouseLeave(EventArgs e) { _encima = false; Invalidate(); base.OnMouseLeave(e); }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.Clear(BackColor);

        // La barrita, del lado de los documentos.
        var barra = _lado switch
        {
            ZonaAcople.Izquierda => new Rectangle(Width - GrosorBarra, 0, GrosorBarra, Height),
            ZonaAcople.Derecha => new Rectangle(0, 0, GrosorBarra, Height),
            _ => new Rectangle(0, 0, Width, GrosorBarra)
        };
        using (var b = new SolidBrush(_desplegada ? Tema.Acento : Tema.LineaSuave)) g.FillRectangle(b, barra);

        var color = _desplegada || _encima ? Tema.Texto : Tema.Texto2;

        if (!Vertical)
        {
            TextRenderer.DrawText(g, Text, Font, new Rectangle(0, GrosorBarra, Width, Height - GrosorBarra), color,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
            return;
        }

        // Vertical: se gira el lienzo 90° (el texto baja). TextRenderer no
        // respeta las transformaciones, así que acá va DrawString.
        g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
        var estado = g.Save();
        g.TranslateTransform(Width, 0);
        g.RotateTransform(90);

        // Girado, el eje Y nuevo va hacia la IZQUIERDA de la pantalla: y' = 0
        // es el borde derecho del control. La barrita del lado derecho queda
        // al final del rango (se descuenta el alto) y la del izquierdo al
        // principio (se corre el origen).
        float origen = _lado == ZonaAcople.Izquierda ? GrosorBarra : 0;
        var zona = new RectangleF(0, origen, Height, Width - GrosorBarra);
        using (var pincel = new SolidBrush(color))
        using (var formato = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
        {
            g.DrawString(Text, Font, pincel, zona, formato);
        }

        g.Restore(estado);
    }
}
