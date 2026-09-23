using System.ComponentModel;
using System.Drawing.Drawing2D;

namespace AsmEditor;

/// <summary>Lo que está contando la barra de estado.</summary>
public enum EstadoBarra
{
    Listo,
    Compilando,
    Correcto,
    ConErrores,
    ConAdvertencias,
    Aviso
}

/// <summary>
/// La barra de estado de la ventana principal, como la de Visual Studio.
///
///   - Izquierda: qué está pasando («Listo», «Ensamblando...», el resultado
///     de la última compilación, un aviso) con su ícono. Mientras compila, la
///     barra entera toma el color de acento: se ve de reojo que está ocupado.
///   - Derecha: errores y advertencias (clic: abre la lista), la posición del
///     cursor, y el target activo (clic: se elige otro).
///
/// No sabe compilar ni elegir targets: muestra lo que le dice MainForm y le
/// avisa los clics con eventos.
/// </summary>
public partial class BarraEstado : UserControl
{
    /// <summary>Clic en el contador de errores o advertencias.</summary>
    public event Action? DiagnosticosPedido;

    /// <summary>Se eligió otro target en el menú de la barra (índice).</summary>
    public event Action<int>? TargetElegido;

    private EstadoBarra _estado = EstadoBarra.Listo;
    private int _errores;
    private int _advertencias;

    /// <summary>Fase de la animación mientras compila.</summary>
    private int _fase;

    private IReadOnlyList<string> _targets = Array.Empty<string>();
    private int _targetActivo = -1;
    private bool _targetsHabilitados = true;

    public BarraEstado()
    {
        InitializeComponent();

        AplicarTema();
        Tema.TemaCambiado += AplicarTema;
        Disposed += BarraEstado_Disposed;

        MostrarListo();
        MostrarDiagnosticos(0, 0);
    }

    /// <summary>
    /// El renderer del menú de targets. Lo pone MainForm, para que se vea igual
    /// que el resto de los menús del editor.
    /// </summary>
    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public ToolStripRenderer? RendererMenus
    {
        get => menuTargets.Renderer;
        set { if (value is not null) menuTargets.Renderer = value; }
    }

    [Browsable(false)]
    public EstadoBarra Estado => _estado;

    [Browsable(false)]
    public string TextoEstado => lblEstado.Text;

    // ------------------------------------------------------------------
    // Lo que se muestra
    // ------------------------------------------------------------------

    public void MostrarListo(string texto = "Listo") => Cambiar(EstadoBarra.Listo, texto);

    /// <summary>Compilando: barra con el color de acento y el ícono girando.</summary>
    public void MostrarCompilando(string texto) => Cambiar(EstadoBarra.Compilando, texto);

    /// <summary>El resultado de una compilación, con su resumen y los contadores.</summary>
    public void MostrarResultado(int errores, int advertencias, string resumen)
    {
        var estado = errores > 0 ? EstadoBarra.ConErrores
                   : advertencias > 0 ? EstadoBarra.ConAdvertencias
                   : EstadoBarra.Correcto;

        MostrarDiagnosticos(errores, advertencias);
        Cambiar(estado, resumen);
    }

    /// <summary>Algo que el usuario tiene que atender (herramientas faltantes).</summary>
    public void MostrarAviso(string texto) => Cambiar(EstadoBarra.Aviso, texto);

    public void MostrarDiagnosticos(int errores, int advertencias)
    {
        _errores = errores;
        _advertencias = advertencias;

        lblErrores.Text = errores.ToString();
        lblAdvertencias.Text = advertencias.ToString();

        toolTip.SetToolTip(lblErrores, errores == 1 ? "1 error — clic para ver la lista" : $"{errores} errores — clic para ver la lista");
        toolTip.SetToolTip(lblAdvertencias, advertencias == 1 ? "1 advertencia — clic para ver la lista" : $"{advertencias} advertencias — clic para ver la lista");

        PintarContadores();
    }

    /// <summary>Posición del cursor; null la oculta (un diseñador no tiene cursor de texto).</summary>
    public void MostrarPosicion(int? linea, int? columna)
    {
        lblPosicion.Visible = linea is not null;
        if (linea is not null) lblPosicion.Text = $"Ln {linea}, Col {columna}";
    }

    /// <summary>Los targets disponibles y cuál está activo.</summary>
    public void MostrarTargets(IReadOnlyList<string> nombres, int activo, string? detalle)
    {
        _targets = nombres;
        _targetActivo = activo;

        lblTarget.Visible = nombres.Count > 0;
        lblTarget.Text = activo >= 0 && activo < nombres.Count ? nombres[activo] : "";
        toolTip.SetToolTip(lblTarget, (detalle ?? "Target de compilación") + " — clic para cambiarlo");
    }

    /// <summary>Mientras compila no se cambia de target, igual que el combo de la barra.</summary>
    public void HabilitarTargets(bool habilitar)
    {
        _targetsHabilitados = habilitar;
        lblTarget.Cursor = habilitar ? Cursors.Hand : Cursors.Default;
    }

    private void Cambiar(EstadoBarra estado, string texto)
    {
        _estado = estado;
        lblEstado.Text = texto;
        toolTip.SetToolTip(lblEstado, texto);

        if (estado == EstadoBarra.Compilando) tmrActividad.Start();
        else tmrActividad.Stop();

        _fase = 0;
        PonerIcono();
        AplicarColores();
    }

    // ------------------------------------------------------------------
    // Clics
    // ------------------------------------------------------------------

    private void lblDiagnosticos_Click(object? sender, EventArgs e) => DiagnosticosPedido?.Invoke();

    private void lblTarget_Click(object? sender, EventArgs e)
    {
        if (!_targetsHabilitados || _targets.Count == 0) return;

        menuTargets.Items.Clear();

        for (int i = 0; i < _targets.Count; i++)
        {
            var item = new ToolStripMenuItem(_targets[i])
            {
                Checked = i == _targetActivo,
                Tag = i,
                ForeColor = Tema.Texto,
                BackColor = Tema.Superficie2
            };
            item.Click += itemTarget_Click;
            menuTargets.Items.Add(item);
        }

        // Hacia arriba: la barra está al pie de la ventana.
        menuTargets.Show(lblTarget, new Point(0, 0), ToolStripDropDownDirection.AboveLeft);
    }

    private void itemTarget_Click(object? sender, EventArgs e)
    {
        if (sender is ToolStripMenuItem { Tag: int i } && i != _targetActivo) TargetElegido?.Invoke(i);
    }

    // ------------------------------------------------------------------
    // Aspecto
    // ------------------------------------------------------------------

    private void tmrActividad_Tick(object? sender, EventArgs e)
    {
        _fase = (_fase + 1) % 12;
        PonerIcono();
    }

    private void BarraEstado_Resize(object? sender, EventArgs e)
    {
        // El mensaje ocupa todo lo que dejan libre los indicadores de la derecha.
        lblEstado.Width = Math.Max(40, lblErrores.Left - 12 - lblEstado.Left);
    }

    /// <summary>Una línea arriba separa la barra del contenido, como las demás barras.</summary>
    private void BarraEstado_Paint(object? sender, PaintEventArgs e)
    {
        if (_estado == EstadoBarra.Compilando) return;

        using var p = new Pen(Tema.LineaSuave);
        e.Graphics.DrawLine(p, 0, 0, Width, 0);
    }

    private void AplicarTema()
    {
        Font = Tema.Chico;
        AplicarColores();
        PonerIcono();
        PintarContadores();
    }

    private void AplicarColores()
    {
        bool ocupado = _estado == EstadoBarra.Compilando;

        BackColor = ocupado ? Tema.Acento : Tema.Superficie2;

        // Sobre el dorado, texto oscuro en el tema oscuro y blanco en el claro
        // (el acento del claro es más oscuro): los dos con contraste suficiente.
        var textoFijo = ocupado ? (Tema.EsOscuro ? Tema.Fondo : Color.White) : Tema.Texto2;

        lblEstado.ForeColor = _estado switch
        {
            EstadoBarra.ConErrores => Tema.Critico,
            EstadoBarra.Aviso => Tema.Aviso,
            _ => textoFijo
        };

        lblPosicion.ForeColor = textoFijo;
        lblTarget.ForeColor = textoFijo;
        lblErrores.ForeColor = textoFijo;
        lblAdvertencias.ForeColor = textoFijo;

        Invalidate(true);
    }

    private void PintarContadores()
    {
        lblErrores.Image?.Dispose();
        lblAdvertencias.Image?.Dispose();

        lblErrores.Image = IconoError(12, _errores > 0 ? Tema.Critico : Tema.Texto3);
        lblAdvertencias.Image = IconoAviso(12, _advertencias > 0 ? Tema.Aviso : Tema.Texto3);
    }

    private void PonerIcono()
    {
        lblIcono.Image?.Dispose();

        lblIcono.Image = _estado switch
        {
            EstadoBarra.Compilando => IconoActividad(16, _fase, Tema.EsOscuro ? Tema.Fondo : Color.White),
            EstadoBarra.Correcto => IconoCorrecto(16, Tema.Ok),
            EstadoBarra.ConErrores => IconoError(16, Tema.Critico),
            EstadoBarra.ConAdvertencias or EstadoBarra.Aviso => IconoAviso(16, Tema.Aviso),
            _ => null
        };
    }

    // ------------------------------------------------------------------
    // Íconos, dibujados con GDI+ como los de la barra de herramientas: valen
    // para cualquier tamaño y siguen al tema sin recursos que empaquetar.
    // ------------------------------------------------------------------

    private static Bitmap Lienzo(int lado, out Graphics g)
    {
        var bmp = new Bitmap(lado, lado);
        g = Graphics.FromImage(bmp);
        g.SmoothingMode = SmoothingMode.AntiAlias;
        return bmp;
    }

    /// <summary>Arco que gira: se dibuja otra fase en cada tic del temporizador.</summary>
    private static Bitmap IconoActividad(int lado, int fase, Color color)
    {
        var bmp = Lienzo(lado, out var g);
        using (g)
        using (var p = new Pen(color, 2f) { StartCap = LineCap.Round, EndCap = LineCap.Round })
        {
            g.DrawArc(p, 2, 2, lado - 5, lado - 5, fase * 30, 270);
        }
        return bmp;
    }

    private static Bitmap IconoCorrecto(int lado, Color color)
    {
        var bmp = Lienzo(lado, out var g);
        using (g)
        using (var p = new Pen(color, 2f) { StartCap = LineCap.Round, EndCap = LineCap.Round, LineJoin = LineJoin.Round })
        {
            float s = lado / 16f;
            g.DrawLines(p, new[] { new PointF(3 * s, 8.5f * s), new PointF(6.5f * s, 12 * s), new PointF(13 * s, 4.5f * s) });
        }
        return bmp;
    }

    private static Bitmap IconoError(int lado, Color color)
    {
        var bmp = Lienzo(lado, out var g);
        using (g)
        using (var b = new SolidBrush(color))
        using (var p = new Pen(Color.White, Math.Max(1.4f, lado / 9f)) { StartCap = LineCap.Round, EndCap = LineCap.Round })
        {
            g.FillEllipse(b, 0.5f, 0.5f, lado - 1.5f, lado - 1.5f);
            float a = lado * 0.32f, z = lado * 0.66f;
            g.DrawLine(p, a, a, z, z);
            g.DrawLine(p, z, a, a, z);
        }
        return bmp;
    }

    private static Bitmap IconoAviso(int lado, Color color)
    {
        var bmp = Lienzo(lado, out var g);
        using (g)
        using (var b = new SolidBrush(color))
        using (var p = new Pen(Tema.EsOscuro ? Tema.Fondo : Color.White, Math.Max(1.4f, lado / 9f)) { StartCap = LineCap.Round, EndCap = LineCap.Round })
        {
            float m = lado / 2f;
            g.FillPolygon(b, new[] { new PointF(m, 0.5f), new PointF(lado - 0.5f, lado - 1f), new PointF(0.5f, lado - 1f) });
            g.DrawLine(p, m, lado * 0.36f, m, lado * 0.62f);
            g.DrawLine(p, m, lado * 0.80f, m, lado * 0.81f);
        }
        return bmp;
    }

    private void BarraEstado_Disposed(object? sender, EventArgs e) => Tema.TemaCambiado -= AplicarTema;
}
