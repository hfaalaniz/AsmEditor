namespace AsmEditor;

/// <summary>
/// LO QUE SE VE CUANDO NO HAY NINGUNA PESTAÑA ABIERTA.
///
/// Antes el editor forzaba un documento nuevo al cerrar la última pestaña, así
/// que este estado no existía. Ahora se pueden cerrar todas, y sin esto quedaría
/// un hueco gris sin explicación.
///
/// Muestra el logo, los atajos para empezar y los archivos recientes, que es
/// justo lo que alguien necesita en ese momento.
/// </summary>
public sealed class PantallaBienvenida : UserControl
{
    /// <summary>Pide abrir un archivo reciente.</summary>
    public event Action<string>? ArchivoElegido;

    /// <summary>Pide crear un documento nuevo.</summary>
    public event Action? NuevoPedido;

    /// <summary>Pide abrir el diálogo de abrir.</summary>
    public event Action? AbrirPedido;

    private readonly LinkLabel _nuevo = new();
    private readonly LinkLabel _abrir = new();
    private readonly Label _tituloRecientes = new();
    private readonly FlowLayoutPanel _recientes = new();

    public PantallaBienvenida()
    {
        Dock = DockStyle.Fill;
        DoubleBuffered = true;
        Visible = false;

        _nuevo.Text = "Nuevo archivo          Ctrl+N";
        _abrir.Text = "Abrir archivo...       Ctrl+O";

        foreach (var link in new[] { _nuevo, _abrir })
        {
            link.AutoSize = true;
            link.Font = Tema.Cuerpo;
            link.LinkBehavior = LinkBehavior.HoverUnderline;
        }

        _nuevo.Click += (_, _) => NuevoPedido?.Invoke();
        _abrir.Click += (_, _) => AbrirPedido?.Invoke();

        _tituloRecientes.Text = "Recientes";
        _tituloRecientes.AutoSize = true;
        _tituloRecientes.Font = Tema.Etiqueta;

        _recientes.FlowDirection = FlowDirection.TopDown;
        _recientes.WrapContents = false;
        _recientes.AutoSize = true;
        _recientes.AutoScroll = false;

        Controls.Add(_nuevo);
        Controls.Add(_abrir);
        Controls.Add(_tituloRecientes);
        Controls.Add(_recientes);

        AplicarTema();
        Tema.TemaCambiado += AplicarTema;
    }

    /// <summary>Rellena la lista de recientes. Se llama al mostrar la pantalla.</summary>
    public void CargarRecientes(IReadOnlyList<string> rutas)
    {
        _recientes.Controls.Clear();

        foreach (var ruta in rutas.Take(8))
        {
            var link = new LinkLabel
            {
                Text = Path.GetFileName(ruta),
                AutoSize = true,
                Font = Tema.Cuerpo,
                LinkBehavior = LinkBehavior.HoverUnderline,
                LinkColor = Tema.Info,
                ActiveLinkColor = Tema.Acento,
                BackColor = Color.Transparent,
                Margin = new Padding(0, 2, 0, 2),
                Tag = ruta
            };

            // La ruta completa en el tooltip: los nombres de archivo se repiten
            // entre carpetas y solo el nombre no alcanza para elegir.
            var tip = new ToolTip();
            tip.SetToolTip(link, ruta);

            link.Click += (s, _) =>
            {
                if (s is LinkLabel l && l.Tag is string p) ArchivoElegido?.Invoke(p);
            };

            _recientes.Controls.Add(link);
        }

        _tituloRecientes.Visible = _recientes.Controls.Count > 0;
        AcomodarControles();
    }

    private void AplicarTema()
    {
        BackColor = Tema.Fondo;

        foreach (var link in new[] { _nuevo, _abrir })
        {
            link.LinkColor = Tema.Info;
            link.ActiveLinkColor = Tema.Acento;
            link.BackColor = Color.Transparent;
        }

        _tituloRecientes.ForeColor = Tema.Texto3;
        _tituloRecientes.BackColor = Color.Transparent;
        _recientes.BackColor = Color.Transparent;

        foreach (Control c in _recientes.Controls)
        {
            if (c is LinkLabel l)
            {
                l.LinkColor = Tema.Info;
                l.ActiveLinkColor = Tema.Acento;
            }
        }

        Invalidate();
    }

    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);
        AcomodarControles();
    }

    /// <summary>
    /// Centra el bloque de contenido. Se hace a mano y no con un TableLayout
    /// porque el logo se dibuja en OnPaint y tiene que quedar alineado con el
    /// resto.
    /// </summary>
    private void AcomodarControles()
    {
        int centroX = Math.Max(20, (Width - 420) / 2);
        int y = Math.Max(20, (Height - 300) / 2) + 110;   // debajo del logo

        _nuevo.Location = new Point(centroX, y);
        _abrir.Location = new Point(centroX, y + 26);

        _tituloRecientes.Location = new Point(centroX, y + 64);
        _recientes.Location = new Point(centroX, y + 86);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);

        int centroX = Math.Max(20, (Width - 420) / 2);
        int y = Math.Max(20, (Height - 300) / 2);

        Tema.DibujarLogo(e.Graphics, centroX, y, 56, Tema.Acento);

        using var brushTitulo = new SolidBrush(Tema.Texto);
        using var brushSub = new SolidBrush(Tema.Texto3);
        using var fTitulo = new Font("Segoe UI Light", 20f);

        e.Graphics.DrawString("Editor ASM", fTitulo, brushTitulo, centroX + 68, y + 6);
        e.Graphics.DrawString("NASM + GoLink / MSVC link", Tema.Chico, brushSub, centroX + 70, y + 42);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) Tema.TemaCambiado -= AplicarTema;
        base.Dispose(disposing);
    }
}
