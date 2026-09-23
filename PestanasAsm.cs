namespace AsmEditor;

/// <summary>
/// LAS PESTAÑAS DEL EDITOR, CON LA PALETA DEL PROGRAMA.
///
/// ⚠ EL <see cref="TabControl"/> DE WINFORMS NO SE DEJA PINTAR POR PROPIEDADES,
/// y por eso hace falta un control propio:
///   - <c>BackColor</c> lo IGNORA: la franja la dibuja el tema del sistema;
///   - <c>DrawMode = OwnerDrawFixed</c> deja pintar solo la ETIQUETA, no el
///     fondo de la franja ni el marco;
///   - el borde del área de contenido no se quita por ninguna propiedad.
///
/// La solución es pintar el control entero en <see cref="OnPaint"/>, tapando lo
/// que Windows haya puesto. La técnica está portada de `Pestanas.cs` del
/// launcher de Trapezoide, donde ya estaba resuelta.
///
/// Agrega sobre aquella: la X para cerrar cada pestaña, el punto de «sin
/// guardar» y el cierre con el botón del medio.
/// </summary>
public sealed class PestanasAsm : TabControl
{
    /// <summary>Alto de la franja de pestañas.</summary>
    public const int AltoPestana = 28;

    /// <summary>Se pide cerrar la pestaña de ese índice.</summary>
    public event Action<int>? CierrePedido;

    /// <summary>
    /// Si las pestañas se pueden cerrar. En falso no se dibuja la X ni se
    /// escucha el botón del medio: es lo que corresponde para solapas fijas
    /// como «Errores» y «Salida», donde una X no haría nada.
    /// </summary>
    public bool PermiteCerrar { get; set; } = true;

    /// <summary>
    /// De dónde sale el color del área de contenido. El del editor usa el fondo
    /// del código; el panel inferior, la superficie de la interfaz. Tiene que
    /// coincidir con el de las TabPage o se ve un escalón entre marco y contenido.
    /// </summary>
    public bool UsaFondoDeCodigo { get; set; } = true;

    private Color FondoContenido => UsaFondoDeCodigo ? Tema.CodigoFondo : Tema.Superficie;

    /// <summary>Índice sobre el que está el mouse, para resaltar su X.</summary>
    private int _indiceHover = -1;
    private bool _sobreLaX;

    public PestanasAsm()
    {
        DrawMode = TabDrawMode.OwnerDrawFixed;
        SizeMode = TabSizeMode.Fixed;
        Appearance = TabAppearance.Normal;
        ItemSize = new Size(190, AltoPestana);
        Padding = new Point(0, 0);

        SetStyle(ControlStyles.AllPaintingInWmPaint
               | ControlStyles.OptimizedDoubleBuffer
               | ControlStyles.ResizeRedraw
               | ControlStyles.UserPaint, true);

        Tema.TemaCambiado += AlCambiarTema;
    }

    private void AlCambiarTema()
    {
        foreach (TabPage p in TabPages)
        {
            p.BackColor = FondoContenido;
            p.ForeColor = Tema.Texto;
        }
        Invalidate();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.Clear(Tema.Superficie2);

        // ── El área de contenido ──
        // Mismo color que las TabPage: si se pintara otro, se vería un escalón
        // entre el marco y el contenido.
        // Con Alignment.Bottom la franja va abajo y el área arriba.
        bool abajo = Alignment == TabAlignment.Bottom;
        var area = abajo
            ? new Rectangle(0, 0, Width - 1, Height - AltoPestana - 1)
            : new Rectangle(0, AltoPestana, Width - 1, Height - AltoPestana - 1);
        using (var b = new SolidBrush(FondoContenido))
            g.FillRectangle(b, area);
        using (var p = new Pen(Tema.LineaSuave))
            g.DrawRectangle(p, area);

        for (int i = 0; i < TabCount; i++) DibujarSolapa(g, i, area);
    }

    private void DibujarSolapa(Graphics g, int i, Rectangle area)
    {
        var r = GetTabRect(i);
        bool activa = i == SelectedIndex;
        bool hover = i == _indiceHover;
        bool abajo = Alignment == TabAlignment.Bottom;

        // La activa se dibuja un pelo más alta y del color del contenido: se lee
        // como una carpeta abierta, sin necesidad de un subrayado. Con la franja
        // abajo, «más alta» significa crecer hacia arriba.
        var caja = activa
            ? (abajo ? new Rectangle(r.X, r.Y - 1, r.Width, r.Height + 1)
                     : new Rectangle(r.X, r.Y, r.Width, r.Height + 1))
            : (abajo ? new Rectangle(r.X, r.Y, r.Width, r.Height - 2)
                     : new Rectangle(r.X, r.Y + 2, r.Width, r.Height - 2));

        var fondo = activa ? FondoContenido
                  : hover ? Tema.Realzar(Tema.Superficie2, 10)
                  : Tema.Superficie2;

        using (var b = new SolidBrush(fondo))
            g.FillRectangle(b, caja);

        using (var p = new Pen(Tema.LineaSuave))
        {
            // El lado que da al contenido no se traza: ahí la solapa se une al área.
            if (abajo) g.DrawLine(p, caja.Left, caja.Bottom - 1, caja.Right - 1, caja.Bottom - 1);
            else g.DrawLine(p, caja.Left, caja.Top, caja.Right - 1, caja.Top);

            g.DrawLine(p, caja.Left, caja.Top, caja.Left, caja.Bottom - 1);
            g.DrawLine(p, caja.Right - 1, caja.Top, caja.Right - 1, caja.Bottom - 1);
        }

        // ⚠ LA ACTIVA TAPA EL MARCO DEL ÁREA. Sin esto queda un trazo que la
        // separa del contenido y las dos zonas se ven despegadas.
        if (activa)
            using (var b = new SolidBrush(FondoContenido))
                g.FillRectangle(b, caja.Left + 1, abajo ? area.Bottom - 1 : area.Top,
                                caja.Width - 2, 2);

        // Una marca de acento en el borde exterior, y no el texto en color: el
        // acento sobre texto chico baja el contraste, y acá se leen varias de corrido.
        if (activa)
            using (var b = new SolidBrush(Tema.Acento))
                g.FillRectangle(b, caja.Left + 1, abajo ? caja.Bottom - 3 : caja.Top + 1,
                                caja.Width - 2, 2);

        // ── Texto ──
        var texto = TabPages[i].Text;
        bool sucio = texto.EndsWith(" *", StringComparison.Ordinal);
        if (sucio) texto = texto[..^2];

        // Sin X el texto se centra y usa todo el ancho; con X hay que dejarle lugar.
        var rectTexto = PermiteCerrar
            ? new Rectangle(caja.Left + 10, caja.Top, caja.Width - 42, caja.Height)
            : new Rectangle(caja.Left + 8, caja.Top, caja.Width - 16, caja.Height);

        var alineacion = PermiteCerrar ? TextFormatFlags.Left : TextFormatFlags.HorizontalCenter;

        TextRenderer.DrawText(g, texto, Font, rectTexto,
            activa ? Tema.Texto : Tema.Texto3,
            alineacion | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);

        if (!PermiteCerrar) return;

        // ── La X, o el punto de «sin guardar» ──
        var cierre = RectCierre(r);

        if (sucio && !(hover && _sobreLaX))
        {
            // Un punto en lugar de la X mientras no se apunta: dice «sin
            // guardar» sin quitar la posibilidad de cerrar.
            using var b = new SolidBrush(Tema.Acento);
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            g.FillEllipse(b, cierre.Left + 1, cierre.Top + 1, cierre.Width - 2, cierre.Height - 2);
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.Default;
        }
        else
        {
            bool resaltada = hover && _sobreLaX;

            if (resaltada)
            {
                using var bg = new SolidBrush(Tema.Critico);
                g.FillRectangle(bg, cierre.Left - 3, cierre.Top - 3, cierre.Width + 6, cierre.Height + 6);
            }

            // Blanco fijo a propósito: va sobre el rojo de «cerrar», que es el
            // mismo en los dos temas. Un token del tema acá daría bajo contraste
            // en claro, donde el texto normal es casi negro.
            var color = resaltada ? Color.White
                      : activa ? Tema.Texto2
                      : Tema.Texto3;

            using var pen = new Pen(color, 1.4f);
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            g.DrawLine(pen, cierre.Left, cierre.Top, cierre.Right, cierre.Bottom);
            g.DrawLine(pen, cierre.Right, cierre.Top, cierre.Left, cierre.Bottom);
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.Default;
        }
    }

    /// <summary>Rectángulo de la X de una pestaña.</summary>
    private static Rectangle RectCierre(Rectangle solapa)
    {
        const int lado = 8;
        int x = solapa.Right - lado - 9;
        int y = solapa.Top + (solapa.Height - lado) / 2 + 1;
        return new Rectangle(x, y, lado, lado);
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);

        int nuevoIndice = -1;
        bool sobreX = false;

        for (int i = 0; i < TabCount; i++)
        {
            var r = GetTabRect(i);
            if (!r.Contains(e.Location)) continue;

            nuevoIndice = i;

            var cierre = RectCierre(r);
            cierre.Inflate(4, 4);
            sobreX = cierre.Contains(e.Location);
            break;
        }

        if (nuevoIndice != _indiceHover || sobreX != _sobreLaX)
        {
            _indiceHover = nuevoIndice;
            _sobreLaX = sobreX;
            Invalidate();
        }
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        base.OnMouseLeave(e);
        if (_indiceHover != -1 || _sobreLaX)
        {
            _indiceHover = -1;
            _sobreLaX = false;
            Invalidate();
        }
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        if (!PermiteCerrar) { base.OnMouseDown(e); return; }

        // El clic en la X cierra y NO cambia de pestaña: por eso se resuelve
        // antes de llamar a base, que es quien cambia la selección.
        for (int i = 0; i < TabCount; i++)
        {
            var r = GetTabRect(i);
            if (!r.Contains(e.Location)) continue;

            var cierre = RectCierre(r);
            cierre.Inflate(4, 4);

            if (e.Button == MouseButtons.Left && cierre.Contains(e.Location))
            {
                CierrePedido?.Invoke(i);
                return;
            }

            // Botón del medio cierra, como en los navegadores.
            if (e.Button == MouseButtons.Middle)
            {
                CierrePedido?.Invoke(i);
                return;
            }

            break;
        }

        base.OnMouseDown(e);
    }

    /// <summary>
    /// ⚠ EL FONDO DE CADA PÁGINA TAMBIÉN: una TabPage nace con el color del
    /// sistema y quedaría un rectángulo claro dentro de una ventana oscura.
    /// </summary>
    protected override void OnControlAdded(ControlEventArgs e)
    {
        base.OnControlAdded(e);
        if (e.Control is TabPage p)
        {
            p.BackColor = FondoContenido;
            p.ForeColor = Tema.Texto;
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) Tema.TemaCambiado -= AlCambiarTema;
        base.Dispose(disposing);
    }
}
