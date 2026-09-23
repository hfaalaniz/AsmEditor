using System.ComponentModel;
using AsmEditor.Core.Disenador;

namespace AsmEditor;

/// <summary>
/// El canvas donde se dibuja el formulario: se sueltan controles, se arrastran
/// y se redimensionan.
///
/// ⚠ NO CREA CONTROLES DE WINFORMS PARA REPRESENTAR LOS CONTROLES DISEÑADOS.
/// Los DIBUJA. Un Button de WinForms adentro del canvas se comportaría como un
/// botón —tomaría el foco, se pintaría con el tema del sistema, respondería a
/// los clics— y habría que pelearle cada uno de esos comportamientos para que
/// se deje arrastrar. Dibujarlos deja el control del ratón entero acá y hace
/// que lo que se ve sea exactamente lo que dice el modelo.
///
/// El canvas no conoce los generadores: solo edita un
/// <see cref="FormularioDisenado"/> y avisa cuando cambia.
/// </summary>
public sealed class LienzoDisenador : Control
{
    /// <summary>Paso de la grilla, en píxeles. Los controles se ajustan a esta medida.</summary>
    public const int PasoGrilla = 4;

    /// <summary>Lado de las manijas de redimensión.</summary>
    private const int LadoManija = 7;

    /// <summary>Alto de la barra de título que se dibuja sobre el formulario.</summary>
    private const int AltoBarraTitulo = 28;

    /// <summary>
    /// Margen entre el borde del canvas y el ÁREA DE CLIENTE del formulario.
    ///
    /// ⚠ TIENE QUE SER MAYOR QUE LA BARRA DE TÍTULO: la barra se dibuja por
    /// encima del origen (en Y negativo respecto del área de cliente), así que
    /// con un margen menor quedaba cortada contra el borde del canvas.
    /// </summary>
    private const int Margen = AltoBarraTitulo + 12;

    private FormularioDisenado _formulario = new();
    private readonly List<ControlDisenado> _seleccion = new();

    private Modo _modo = Modo.Ninguno;
    private Manija _manijaActiva = Manija.Ninguna;
    private Point _puntoInicial;
    private readonly Dictionary<ControlDisenado, Rectangle> _rectanglesIniciales = new();
    private Rectangle _rectInicialFormulario;

    /// <summary>
    /// True cuando el ratón ya se alejó lo suficiente del punto donde se
    /// apretó como para que sea un arrastre y no un clic.
    ///
    /// ⚠ SIN ESTO UN CLIC MOVÍA EL CONTROL. El primer WM_MOUSEMOVE, aunque sea
    /// de 0 px (o el temblor de 1 px de cualquier clic real), recalculaba la
    /// posición pasándola por la grilla: un botón en X=110 quedaba en 112 con
    /// solo hacerle clic. Se usa la misma tolerancia que Windows usa para
    /// distinguir un clic de un arrastre.
    /// </summary>
    private bool _arrastreIniciado;

    /// <summary>Tipo que se va a crear con el próximo clic, o null para seleccionar.</summary>
    private TipoControl? _herramienta;

    private enum Modo { Ninguno, Moviendo, Redimensionando, Seleccionando, RedimensionandoFormulario }

    private enum Manija
    {
        Ninguna, NO, N, NE, E, SE, S, SO, O
    }

    // ------------------------------------------------------------------
    // Eventos
    // ------------------------------------------------------------------

    /// <summary>El formulario cambió (se movió algo, se agregó o se borró).</summary>
    public event Action? FormularioCambiado;

    /// <summary>Cambió qué está seleccionado.</summary>
    public event Action? SeleccionCambiada;

    /// <summary>Se soltó un control de la paleta y hay que volver a modo selección.</summary>
    public event Action? HerramientaConsumida;

    /// <summary>
    /// Terminó una acción del usuario: es el momento de registrar un paso de
    /// deshacer.
    ///
    /// ⚠ NO ES LO MISMO QUE <see cref="FormularioCambiado"/>. Ese se dispara en
    /// cada movimiento del ratón durante un arrastre; si el historial lo
    /// escuchara, arrastrar un botón serían cincuenta pasos de deshacer. Este
    /// llega UNA vez: al soltar el ratón, al soltar la flecha, al terminar de
    /// borrar o de alinear.
    /// </summary>
    public event Action? CambioTerminado;

    /// <summary>
    /// True mientras el usuario tiene un arrastre en curso. Deshacer en ese
    /// momento dejaría el arrastre moviendo controles que ya no están en el
    /// formulario.
    /// </summary>
    [Browsable(false)]
    public bool EnGesto => _modo != Modo.Ninguno;

    /// <summary>
    /// Selecciona los controles que tengan esos Ids. Se usa al deshacer: el
    /// formulario repuesto trae objetos nuevos, y sin esto la selección se
    /// perdería en cada Ctrl+Z.
    /// </summary>
    public void SeleccionarPorIds(IEnumerable<int> ids)
    {
        var buscados = new HashSet<int>(ids);

        _seleccion.Clear();
        _seleccion.AddRange(_formulario.Controles.Where(c => buscados.Contains(c.Id)));

        SeleccionCambiada?.Invoke();
        Invalidate();
    }

    // ------------------------------------------------------------------

    public LienzoDisenador()
    {
        // Sin esto el canvas parpadea entero en cada arrastre.
        SetStyle(ControlStyles.AllPaintingInWmPaint |
                 ControlStyles.UserPaint |
                 ControlStyles.OptimizedDoubleBuffer |
                 ControlStyles.ResizeRedraw |
                 ControlStyles.Selectable, true);

        TabStop = true;
    }

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public FormularioDisenado Formulario
    {
        get => _formulario;
        set
        {
            _formulario = value ?? new FormularioDisenado();
            _seleccion.Clear();
            SeleccionCambiada?.Invoke();
            AjustarTamano();
            Invalidate();
        }
    }

    /// <summary>Los controles seleccionados. Vacío si está seleccionado el formulario.</summary>
    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public IReadOnlyList<ControlDisenado> Seleccionados => _seleccion;

    /// <summary>Mostrar la grilla de puntos.</summary>
    public bool MostrarGrilla { get; set; } = true;

    /// <summary>Ajustar posiciones y tamaños al paso de la grilla.</summary>
    public bool AjustarAGrilla { get; set; } = true;

    /// <summary>
    /// Pone la herramienta de creación: el próximo clic suelta un control de
    /// ese tipo. Null vuelve a modo selección.
    /// </summary>
    public void UsarHerramienta(TipoControl? tipo)
    {
        _herramienta = tipo;
        Cursor = tipo is null ? Cursors.Default : Cursors.Cross;
    }

    public void SeleccionarSolo(ControlDisenado? c)
    {
        _seleccion.Clear();
        if (c is not null) _seleccion.Add(c);
        SeleccionCambiada?.Invoke();
        Invalidate();
    }

    /// <summary>Borra los controles seleccionados.</summary>
    public void BorrarSeleccion()
    {
        if (_seleccion.Count == 0) return;

        foreach (var c in _seleccion) _formulario.Controles.Remove(c);

        _seleccion.Clear();
        SeleccionCambiada?.Invoke();
        FormularioCambiado?.Invoke();
        CambioTerminado?.Invoke();
        Invalidate();
    }

    /// <summary>
    /// Vuelve a dibujar porque cambió algo desde afuera (por ejemplo el panel
    /// de propiedades).
    /// </summary>
    public void Refrescar()
    {
        AjustarTamano();
        Invalidate();
    }

    /// <summary>
    /// Hace que el canvas sea lo bastante grande para el formulario y sus
    /// márgenes, para que el panel que lo contiene le dé barras de scroll
    /// cuando el formulario no entra en la ventana.
    /// </summary>
    private void AjustarTamano()
    {
        var ancho = _formulario.Ancho + Margen * 2;
        var alto = _formulario.Alto + Margen * 2;

        // Los controles pueden estar fuera del área de cliente (el usuario los
        // arrastró de más): el canvas los tiene que seguir mostrando, o
        // quedarían inalcanzables.
        foreach (var c in _formulario.Controles)
        {
            ancho = Math.Max(ancho, c.X + c.Ancho + Margen * 2);
            alto = Math.Max(alto, c.Y + c.Alto + Margen * 2);
        }

        if (Size != new Size(ancho, alto)) Size = new Size(ancho, alto);
    }

    // ------------------------------------------------------------------
    // Coordenadas
    //
    // El formulario se dibuja a escala 1:1 con un margen fijo. Las coordenadas
    // del modelo son las del área de cliente de la ventana real.
    // ------------------------------------------------------------------

    private Rectangle RectFormulario =>
        new(Margen, Margen, _formulario.Ancho, _formulario.Alto);

    private Point ALienzo(int x, int y) => new(x + Margen, y + Margen);

    private Point ALienzo(Point p) => ALienzo(p.X, p.Y);

    private Point AModelo(Point p) => new(p.X - Margen, p.Y - Margen);

    private Rectangle RectDe(ControlDisenado c) =>
        new(c.X + Margen, c.Y + Margen, c.Ancho, c.Alto);

    private static int Ajustar(int v, bool ajustar) =>
        ajustar ? (int)Math.Round(v / (double)PasoGrilla) * PasoGrilla : v;

    // ------------------------------------------------------------------
    // Dibujo
    // ------------------------------------------------------------------

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.Clear(Tema.Fondo);

        var rf = RectFormulario;

        DibujarSuperficieFormulario(g, rf);

        if (MostrarGrilla) DibujarGrilla(g, rf);

        foreach (var c in _formulario.Controles) DibujarControl(g, c);

        foreach (var c in _seleccion) DibujarSeleccion(g, RectDe(c));

        if (_seleccion.Count == 0) DibujarManijasFormulario(g, rf);

        if (_modo == Modo.Seleccionando) DibujarRectanguloDeSeleccion(g);
    }

    private void DibujarSuperficieFormulario(Graphics g, Rectangle rf)
    {
        // La barra de título se dibuja por fuera del área de cliente, igual que
        // en la ventana real: así se ve dónde termina lo que se está diseñando.
        var barra = new Rectangle(rf.X, rf.Y - AltoBarraTitulo, rf.Width, AltoBarraTitulo);

        using (var b = new SolidBrush(Tema.Superficie2)) g.FillRectangle(b, barra);
        using (var b = new SolidBrush(Tema.Superficie)) g.FillRectangle(b, rf);
        using (var p = new Pen(Tema.Linea)) g.DrawRectangle(p, rf);
        using (var p = new Pen(Tema.Linea)) g.DrawRectangle(p, barra);

        using var f = new Font("Segoe UI", 8.5f);
        using var bt = new SolidBrush(Tema.Texto);

        var sf = new StringFormat
        {
            LineAlignment = StringAlignment.Center,
            Trimming = StringTrimming.EllipsisCharacter,
            FormatFlags = StringFormatFlags.NoWrap
        };

        g.DrawString(_formulario.Titulo, f, bt,
            new RectangleF(barra.X + 8, barra.Y, barra.Width - 60, barra.Height), sf);
    }

    private void DibujarGrilla(Graphics g, Rectangle rf)
    {
        // Cada 4 px hay demasiados puntos para que sirvan de referencia: se
        // dibuja uno cada 4 pasos (16 px).
        const int cada = PasoGrilla * 4;

        using var b = new SolidBrush(Tema.LineaSuave);

        for (int x = 0; x <= rf.Width; x += cada)
        {
            for (int y = 0; y <= rf.Height; y += cada)
            {
                g.FillRectangle(b, rf.X + x, rf.Y + y, 1, 1);
            }
        }
    }

    /// <summary>
    /// Dibuja un control con la pinta que va a tener en Windows.
    ///
    /// No pretende ser idéntico —el aspecto real lo pone el tema del sistema
    /// operativo— pero sí reconocible: un botón tiene que verse botón.
    /// </summary>
    private void DibujarControl(Graphics g, ControlDisenado c)
    {
        var r = RectDe(c);

        using var fuente = new Font("Segoe UI", 8.5f);
        using var pincelTexto = new SolidBrush(c.DeshabilitadoAlInicio ? Tema.Texto2 : Tema.Texto);

        // Un control oculto se dibuja traslúcido: hay que poder agarrarlo, pero
        // tiene que notarse que no se va a ver al correr el programa.
        if (c.OcultoAlInicio)
        {
            using var bh = new SolidBrush(Color.FromArgb(60, Tema.Texto2));
            g.FillRectangle(bh, r);
        }

        switch (c.Tipo)
        {
            case TipoControl.Boton:
                DibujarCaja(g, r, Tema.Superficie3, Tema.Linea);
                DibujarTextoCentrado(g, c.Texto, fuente, pincelTexto, r);
                break;

            case TipoControl.Campo:
            case TipoControl.CampoMultilinea:
                DibujarCaja(g, r, Tema.Superficie, Tema.Linea);
                DibujarTextoIzquierda(g, c.Texto, fuente, pincelTexto, r, 4);
                break;

            case TipoControl.Etiqueta:
                DibujarTextoIzquierda(g, c.Texto, fuente, pincelTexto, r, 0);
                break;

            case TipoControl.Casilla:
                DibujarMarcaCuadrada(g, r);
                DibujarTextoIzquierda(g, c.Texto, fuente, pincelTexto, r, 20);
                break;

            case TipoControl.Opcion:
                DibujarMarcaRedonda(g, r);
                DibujarTextoIzquierda(g, c.Texto, fuente, pincelTexto, r, 20);
                break;

            case TipoControl.Grupo:
                DibujarMarco(g, r, c.Texto, fuente, pincelTexto);
                break;

            case TipoControl.Lista:
                DibujarCaja(g, r, Tema.Superficie, Tema.Linea);
                DibujarBarraDesplazamiento(g, r);
                break;

            case TipoControl.Desplegable:
                DibujarCaja(g, r, Tema.Superficie, Tema.Linea);
                DibujarFlechaAbajo(g, r);
                DibujarTextoIzquierda(g, c.Texto, fuente, pincelTexto, r, 4);
                break;

            case TipoControl.Personalizado:
                DibujarControlPersonalizado(g, r, c, fuente);
                break;
        }
    }

    private static void DibujarCaja(Graphics g, Rectangle r, Color relleno, Color borde)
    {
        using var b = new SolidBrush(relleno);
        using var p = new Pen(borde);
        g.FillRectangle(b, r);
        g.DrawRectangle(p, r.X, r.Y, r.Width - 1, r.Height - 1);
    }

    private static void DibujarTextoCentrado(
        Graphics g, string texto, Font f, Brush b, Rectangle r)
    {
        var sf = new StringFormat
        {
            Alignment = StringAlignment.Center,
            LineAlignment = StringAlignment.Center,
            Trimming = StringTrimming.EllipsisCharacter,
            FormatFlags = StringFormatFlags.NoWrap
        };

        g.DrawString(texto, f, b, r, sf);
    }

    private static void DibujarTextoIzquierda(
        Graphics g, string texto, Font f, Brush b, Rectangle r, int sangria)
    {
        var sf = new StringFormat
        {
            LineAlignment = StringAlignment.Center,
            Trimming = StringTrimming.EllipsisCharacter,
            FormatFlags = StringFormatFlags.NoWrap
        };

        var caja = new RectangleF(r.X + sangria, r.Y, r.Width - sangria, r.Height);
        g.DrawString(texto, f, b, caja, sf);
    }

    private static void DibujarMarcaCuadrada(Graphics g, Rectangle r)
    {
        var lado = 13;
        var caja = new Rectangle(r.X + 1, r.Y + (r.Height - lado) / 2, lado, lado);

        using var b = new SolidBrush(Tema.Superficie);
        using var p = new Pen(Tema.Linea);
        g.FillRectangle(b, caja);
        g.DrawRectangle(p, caja);
    }

    private static void DibujarMarcaRedonda(Graphics g, Rectangle r)
    {
        var lado = 13;
        var caja = new Rectangle(r.X + 1, r.Y + (r.Height - lado) / 2, lado, lado);

        using var b = new SolidBrush(Tema.Superficie);
        using var p = new Pen(Tema.Linea);
        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        g.FillEllipse(b, caja);
        g.DrawEllipse(p, caja);
        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.None;
    }

    private static void DibujarMarco(
        Graphics g, Rectangle r, string texto, Font f, Brush b)
    {
        using var p = new Pen(Tema.Linea);

        var y = r.Y + 7;
        g.DrawRectangle(p, r.X, y, r.Width - 1, r.Height - 8);

        if (string.IsNullOrEmpty(texto)) return;

        // El texto del marco va encima de la línea, con un hueco: por eso se
        // tapa el tramo de línea antes de escribirlo.
        var tam = g.MeasureString(texto, f);

        using (var bf = new SolidBrush(Tema.Superficie))
        {
            g.FillRectangle(bf, r.X + 8, y - tam.Height / 2, tam.Width + 4, tam.Height);
        }

        g.DrawString(texto, f, b, r.X + 10, y - tam.Height / 2);
    }

    private static void DibujarBarraDesplazamiento(Graphics g, Rectangle r)
    {
        if (r.Width < 20) return;

        var barra = new Rectangle(r.Right - 15, r.Y + 1, 14, r.Height - 2);

        using var b = new SolidBrush(Tema.Superficie2);
        using var p = new Pen(Tema.LineaSuave);
        g.FillRectangle(b, barra);
        g.DrawRectangle(p, barra);
    }

    private static void DibujarFlechaAbajo(Graphics g, Rectangle r)
    {
        if (r.Width < 20) return;

        var caja = new Rectangle(r.Right - 17, r.Y + 1, 16, Math.Min(r.Height - 2, 22));

        using var b = new SolidBrush(Tema.Superficie3);
        g.FillRectangle(b, caja);

        var cx = caja.X + caja.Width / 2;
        var cy = caja.Y + caja.Height / 2;

        using var bf = new SolidBrush(Tema.Texto);
        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        g.FillPolygon(bf, new[]
        {
            new Point(cx - 4, cy - 2),
            new Point(cx + 4, cy - 2),
            new Point(cx, cy + 3)
        });
        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.None;
    }

    /// <summary>
    /// Un control de clase propia se dibuja como un recuadro punteado con el
    /// nombre de la clase: el editor no sabe cómo se ve, y fingir que sí sería
    /// mentirle al usuario sobre lo que va a aparecer en pantalla.
    /// </summary>
    private static void DibujarControlPersonalizado(
        Graphics g, Rectangle r, ControlDisenado c, Font f)
    {
        using var b = new SolidBrush(Tema.Superficie2);
        g.FillRectangle(b, r);

        using var p = new Pen(Tema.Acento) { DashStyle = System.Drawing.Drawing2D.DashStyle.Dash };
        g.DrawRectangle(p, r.X, r.Y, r.Width - 1, r.Height - 1);

        var etiqueta = string.IsNullOrWhiteSpace(c.ClasePersonalizada)
            ? "(sin clase)"
            : c.ClasePersonalizada;

        using var bt = new SolidBrush(Tema.Texto2);
        DibujarTextoCentrado(g, etiqueta, f, bt, r);
    }

    private void DibujarSeleccion(Graphics g, Rectangle r)
    {
        using var p = new Pen(Tema.Acento);
        g.DrawRectangle(p, r.X - 1, r.Y - 1, r.Width + 1, r.Height + 1);

        // Con varios seleccionados no se dibujan manijas: redimensionar en
        // grupo necesita decidir qué pasa con cada uno, y eso confunde más de
        // lo que ayuda.
        if (_seleccion.Count != 1) return;

        foreach (var m in ManijasDe(r)) DibujarManija(g, m.Value);
    }

    private void DibujarManijasFormulario(Graphics g, Rectangle rf)
    {
        // El formulario se redimensiona solo por la derecha, abajo y la esquina:
        // su origen es fijo.
        using var p = new Pen(Tema.AcentoTenue);
        g.DrawRectangle(p, rf.X - 1, rf.Y - 1, rf.Width + 1, rf.Height + 1);

        DibujarManija(g, ManijaFormularioE(rf));
        DibujarManija(g, ManijaFormularioS(rf));
        DibujarManija(g, ManijaFormularioSE(rf));
    }

    private static void DibujarManija(Graphics g, Rectangle r)
    {
        using var b = new SolidBrush(Tema.Acento);
        using var p = new Pen(Tema.Fondo);
        g.FillRectangle(b, r);
        g.DrawRectangle(p, r);
    }

    private void DibujarRectanguloDeSeleccion(Graphics g)
    {
        var r = RectanguloEntre(_puntoInicial, PointToClient(MousePosition));

        using var b = new SolidBrush(Color.FromArgb(40, Tema.Acento));
        using var p = new Pen(Tema.Acento) { DashStyle = System.Drawing.Drawing2D.DashStyle.Dot };

        g.FillRectangle(b, r);
        g.DrawRectangle(p, r);
    }

    // ------------------------------------------------------------------
    // Manijas
    // ------------------------------------------------------------------

    private static Dictionary<Manija, Rectangle> ManijasDe(Rectangle r)
    {
        int m = LadoManija;
        int h = m / 2;

        Rectangle En(int x, int y) => new(x - h, y - h, m, m);

        return new Dictionary<Manija, Rectangle>
        {
            [Manija.NO] = En(r.Left, r.Top),
            [Manija.N]  = En(r.Left + r.Width / 2, r.Top),
            [Manija.NE] = En(r.Right, r.Top),
            [Manija.E]  = En(r.Right, r.Top + r.Height / 2),
            [Manija.SE] = En(r.Right, r.Bottom),
            [Manija.S]  = En(r.Left + r.Width / 2, r.Bottom),
            [Manija.SO] = En(r.Left, r.Bottom),
            [Manija.O]  = En(r.Left, r.Top + r.Height / 2)
        };
    }

    private static Rectangle ManijaFormularioE(Rectangle rf) =>
        new(rf.Right - LadoManija / 2, rf.Top + rf.Height / 2 - LadoManija / 2, LadoManija, LadoManija);

    private static Rectangle ManijaFormularioS(Rectangle rf) =>
        new(rf.Left + rf.Width / 2 - LadoManija / 2, rf.Bottom - LadoManija / 2, LadoManija, LadoManija);

    private static Rectangle ManijaFormularioSE(Rectangle rf) =>
        new(rf.Right - LadoManija / 2, rf.Bottom - LadoManija / 2, LadoManija, LadoManija);

    private static Cursor CursorDe(Manija m) => m switch
    {
        Manija.NO or Manija.SE => Cursors.SizeNWSE,
        Manija.NE or Manija.SO => Cursors.SizeNESW,
        Manija.N or Manija.S => Cursors.SizeNS,
        Manija.E or Manija.O => Cursors.SizeWE,
        _ => Cursors.Default
    };

    // ------------------------------------------------------------------
    // Ratón
    // ------------------------------------------------------------------

    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        Focus();

        if (e.Button != MouseButtons.Left) return;

        // 1. ¿Está soltando un control nuevo?
        if (_herramienta is not null)
        {
            SoltarControlNuevo(e.Location);
            return;
        }

        // 2. ¿Agarró una manija del control seleccionado?
        if (_seleccion.Count == 1)
        {
            var manija = ManijaEn(RectDe(_seleccion[0]), e.Location);

            if (manija != Manija.Ninguna)
            {
                IniciarRedimension(manija, e.Location);
                return;
            }
        }

        // 3. ¿Una manija del formulario?
        if (_seleccion.Count == 0)
        {
            var mf = ManijaFormularioEn(e.Location);

            if (mf != Manija.Ninguna)
            {
                _modo = Modo.RedimensionandoFormulario;
                _manijaActiva = mf;
                _puntoInicial = e.Location;
                _rectInicialFormulario = RectFormulario;
                _arrastreIniciado = false;
                return;
            }
        }

        // 4. ¿Clic sobre un control?
        var bajoElCursor = ControlEn(e.Location);

        if (bajoElCursor is not null)
        {
            bool conCtrl = (ModifierKeys & Keys.Control) == Keys.Control;

            if (conCtrl)
            {
                if (_seleccion.Contains(bajoElCursor)) _seleccion.Remove(bajoElCursor);
                else _seleccion.Add(bajoElCursor);

                SeleccionCambiada?.Invoke();
                Invalidate();
                return;
            }

            if (!_seleccion.Contains(bajoElCursor))
            {
                _seleccion.Clear();
                _seleccion.Add(bajoElCursor);
                SeleccionCambiada?.Invoke();
            }

            IniciarMovimiento(e.Location);
            Invalidate();
            return;
        }

        // 5. Clic en el vacío: empieza una selección por rectángulo.
        _seleccion.Clear();
        SeleccionCambiada?.Invoke();

        _modo = Modo.Seleccionando;
        _puntoInicial = e.Location;
        Invalidate();
    }

    private void SoltarControlNuevo(Point p)
    {
        var m = AModelo(p);

        var c = _formulario.AgregarControl(
            _herramienta!.Value,
            Ajustar(m.X, AjustarAGrilla),
            Ajustar(m.Y, AjustarAGrilla));

        if (_herramienta == TipoControl.Personalizado)
        {
            c.ClasePersonalizada = "MiClase";
        }

        _seleccion.Clear();
        _seleccion.Add(c);

        UsarHerramienta(null);
        HerramientaConsumida?.Invoke();
        SeleccionCambiada?.Invoke();
        FormularioCambiado?.Invoke();
        CambioTerminado?.Invoke();
        Invalidate();
    }

    private void IniciarMovimiento(Point p)
    {
        _modo = Modo.Moviendo;
        _puntoInicial = p;
        _arrastreIniciado = false;
        GuardarRectangulosIniciales();
    }

    private void IniciarRedimension(Manija m, Point p)
    {
        _modo = Modo.Redimensionando;
        _manijaActiva = m;
        _puntoInicial = p;
        _arrastreIniciado = false;
        GuardarRectangulosIniciales();
    }

    /// <summary>
    /// True si el ratón ya salió de la tolerancia de clic alrededor del punto
    /// inicial. Una vez que salió, queda en true hasta soltar el botón: volver
    /// cerca del punto inicial a mitad de un arrastre sigue siendo arrastre.
    /// </summary>
    private bool EsArrastre(Point p)
    {
        if (_arrastreIniciado) return true;

        var tolerancia = SystemInformation.DragSize;

        _arrastreIniciado =
            Math.Abs(p.X - _puntoInicial.X) > tolerancia.Width / 2 ||
            Math.Abs(p.Y - _puntoInicial.Y) > tolerancia.Height / 2;

        return _arrastreIniciado;
    }

    private void GuardarRectangulosIniciales()
    {
        _rectanglesIniciales.Clear();

        foreach (var c in _seleccion)
        {
            _rectanglesIniciales[c] = new Rectangle(c.X, c.Y, c.Ancho, c.Alto);
        }
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);

        switch (_modo)
        {
            case Modo.Ninguno:
                ActualizarCursor(e.Location);
                return;

            // Mientras el ratón no salga de la tolerancia de clic, no se toca
            // nada: un clic no puede mover ni redimensionar.
            case Modo.Moviendo:
                if (EsArrastre(e.Location)) MoverSeleccion(e.Location);
                return;

            case Modo.Redimensionando:
                if (EsArrastre(e.Location)) RedimensionarSeleccion(e.Location);
                return;

            case Modo.RedimensionandoFormulario:
                if (EsArrastre(e.Location)) RedimensionarFormulario(e.Location);
                return;

            case Modo.Seleccionando:
                Invalidate();
                return;
        }
    }

    private void ActualizarCursor(Point p)
    {
        if (_herramienta is not null) return;

        if (_seleccion.Count == 1)
        {
            var m = ManijaEn(RectDe(_seleccion[0]), p);

            if (m != Manija.Ninguna)
            {
                Cursor = CursorDe(m);
                return;
            }
        }

        if (_seleccion.Count == 0)
        {
            var mf = ManijaFormularioEn(p);

            if (mf != Manija.Ninguna)
            {
                Cursor = CursorDe(mf);
                return;
            }
        }

        Cursor = ControlEn(p) is not null ? Cursors.SizeAll : Cursors.Default;
    }

    private void MoverSeleccion(Point p)
    {
        int dx = p.X - _puntoInicial.X;
        int dy = p.Y - _puntoInicial.Y;

        foreach (var c in _seleccion)
        {
            if (!_rectanglesIniciales.TryGetValue(c, out var r0)) continue;

            c.X = Ajustar(r0.X + dx, AjustarAGrilla);
            c.Y = Ajustar(r0.Y + dy, AjustarAGrilla);
        }

        FormularioCambiado?.Invoke();
        Invalidate();
    }

    private void RedimensionarSeleccion(Point p)
    {
        if (_seleccion.Count != 1) return;

        var c = _seleccion[0];
        if (!_rectanglesIniciales.TryGetValue(c, out var r0)) return;

        int dx = p.X - _puntoInicial.X;
        int dy = p.Y - _puntoInicial.Y;

        // ⚠ SOLO SE AJUSTA A LA GRILLA LO QUE LA MANIJA CAMBIA. Antes se
        // ajustaban X, Y, ancho y alto siempre: estirar el borde derecho de un
        // botón en X=110 lo corría a X=112 aunque su borde izquierdo no se
        // hubiera tocado. El borde que no se arrastra queda exactamente donde
        // estaba.
        //
        // ⚠ EL TAMAÑO NUNCA BAJA DEL MÍNIMO: un control de ancho 0 no se puede
        // volver a agarrar con el ratón, y el modelo lo rechaza al validar.
        const int minimo = PasoGrilla;

        int x = r0.X, y = r0.Y, w = r0.Width, h = r0.Height;

        // Borde oeste: se ajusta SU posición y el borde este queda fijo.
        if (_manijaActiva is Manija.NO or Manija.O or Manija.SO)
        {
            x = Math.Min(Ajustar(r0.X + dx, AjustarAGrilla), r0.Right - minimo);
            w = r0.Right - x;
        }

        // Borde este: el origen no se toca, se ajusta el ancho.
        if (_manijaActiva is Manija.NE or Manija.E or Manija.SE)
        {
            w = Math.Max(minimo, Ajustar(r0.Width + dx, AjustarAGrilla));
        }

        // Borde norte: se ajusta su posición y el borde sur queda fijo.
        if (_manijaActiva is Manija.NO or Manija.N or Manija.NE)
        {
            y = Math.Min(Ajustar(r0.Y + dy, AjustarAGrilla), r0.Bottom - minimo);
            h = r0.Bottom - y;
        }

        // Borde sur: el origen no se toca, se ajusta el alto.
        if (_manijaActiva is Manija.SO or Manija.S or Manija.SE)
        {
            h = Math.Max(minimo, Ajustar(r0.Height + dy, AjustarAGrilla));
        }

        c.X = x;
        c.Y = y;
        c.Ancho = w;
        c.Alto = h;

        FormularioCambiado?.Invoke();
        Invalidate();
    }

    private void RedimensionarFormulario(Point p)
    {
        int dx = p.X - _puntoInicial.X;
        int dy = p.Y - _puntoInicial.Y;

        const int minimo = 40;

        if (_manijaActiva is Manija.E or Manija.SE)
        {
            _formulario.Ancho = Math.Max(minimo,
                Ajustar(_rectInicialFormulario.Width + dx, AjustarAGrilla));
        }

        if (_manijaActiva is Manija.S or Manija.SE)
        {
            _formulario.Alto = Math.Max(minimo,
                Ajustar(_rectInicialFormulario.Height + dy, AjustarAGrilla));
        }

        AjustarTamano();
        FormularioCambiado?.Invoke();
        Invalidate();
    }

    protected override void OnMouseUp(MouseEventArgs e)
    {
        base.OnMouseUp(e);

        if (_modo == Modo.Seleccionando)
        {
            SeleccionarDentroDe(RectanguloEntre(_puntoInicial, e.Location));
        }

        // Seleccionar no cambia el formulario; mover y redimensionar sí. Si el
        // arrastre terminó donde empezó, el historial lo nota y no deja paso.
        bool eraEdicion = _modo is Modo.Moviendo or Modo.Redimensionando or Modo.RedimensionandoFormulario;

        _modo = Modo.Ninguno;
        _manijaActiva = Manija.Ninguna;
        _arrastreIniciado = false;
        _rectanglesIniciales.Clear();

        // Se avisa DESPUÉS de volver a Modo.Ninguno: quien escucha puede
        // consultar EnGesto, y tiene que ver el gesto ya terminado.
        if (eraEdicion) CambioTerminado?.Invoke();

        Invalidate();
    }

    private void SeleccionarDentroDe(Rectangle r)
    {
        _seleccion.Clear();

        foreach (var c in _formulario.Controles)
        {
            if (r.IntersectsWith(RectDe(c))) _seleccion.Add(c);
        }

        SeleccionCambiada?.Invoke();
    }

    private static Rectangle RectanguloEntre(Point a, Point b) => new(
        Math.Min(a.X, b.X),
        Math.Min(a.Y, b.Y),
        Math.Abs(a.X - b.X),
        Math.Abs(a.Y - b.Y));

    /// <summary>
    /// El control que está bajo el punto. Se recorre AL REVÉS porque los
    /// últimos de la lista se dibujan encima: el que se ve arriba es el que
    /// tiene que agarrar el clic.
    /// </summary>
    private ControlDisenado? ControlEn(Point p)
    {
        for (int i = _formulario.Controles.Count - 1; i >= 0; i--)
        {
            if (RectDe(_formulario.Controles[i]).Contains(p)) return _formulario.Controles[i];
        }

        return null;
    }

    private static Manija ManijaEn(Rectangle r, Point p)
    {
        foreach (var m in ManijasDe(r))
        {
            if (m.Value.Contains(p)) return m.Key;
        }

        return Manija.Ninguna;
    }

    private Manija ManijaFormularioEn(Point p)
    {
        var rf = RectFormulario;

        // La esquina va primero: se superpone con las otras dos y es la que
        // más sirve.
        if (ManijaFormularioSE(rf).Contains(p)) return Manija.SE;
        if (ManijaFormularioE(rf).Contains(p)) return Manija.E;
        if (ManijaFormularioS(rf).Contains(p)) return Manija.S;

        return Manija.Ninguna;
    }

    // ------------------------------------------------------------------
    // Teclado
    // ------------------------------------------------------------------

    protected override bool IsInputKey(Keys keyData) => keyData switch
    {
        // Sin esto WinForms se queda las flechas para navegar entre controles
        // y nunca llegan a OnKeyDown.
        Keys.Left or Keys.Right or Keys.Up or Keys.Down => true,
        _ => base.IsInputKey(keyData)
    };

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);

        if (e.KeyCode == Keys.Delete)
        {
            BorrarSeleccion();
            e.Handled = true;
            return;
        }

        if (e.KeyCode == Keys.Escape)
        {
            UsarHerramienta(null);
            HerramientaConsumida?.Invoke();
            SeleccionarSolo(null);
            e.Handled = true;
            return;
        }

        if (e.Control && e.KeyCode == Keys.A)
        {
            _seleccion.Clear();
            _seleccion.AddRange(_formulario.Controles);
            SeleccionCambiada?.Invoke();
            Invalidate();
            e.Handled = true;
            return;
        }

        var (dx, dy) = e.KeyCode switch
        {
            Keys.Left => (-1, 0),
            Keys.Right => (1, 0),
            Keys.Up => (0, -1),
            Keys.Down => (0, 1),
            _ => (0, 0)
        };

        if (dx == 0 && dy == 0) return;

        if (_seleccion.Count == 0) return;

        // Con Shift se mueve de a un paso de grilla; sin Shift, de a un píxel,
        // que es lo que hace falta para ajustar algo que quedó casi bien.
        int paso = e.Shift ? PasoGrilla : 1;

        foreach (var c in _seleccion)
        {
            c.X += dx * paso;
            c.Y += dy * paso;
        }

        FormularioCambiado?.Invoke();
        Invalidate();
        e.Handled = true;
    }

    /// <summary>
    /// Mover con flechas se confirma al SOLTAR la tecla, no en cada pulsación:
    /// mantenerla apretada 40 píxeles es un solo paso de deshacer, no 40.
    /// </summary>
    protected override void OnKeyUp(KeyEventArgs e)
    {
        base.OnKeyUp(e);

        if (e.KeyCode is Keys.Left or Keys.Right or Keys.Up or Keys.Down)
        {
            CambioTerminado?.Invoke();
        }
    }

    // ------------------------------------------------------------------
    // Alineación de varios controles
    // ------------------------------------------------------------------

    /// <summary>
    /// Alinea los seleccionados contra el PRIMERO de la selección, que es el
    /// que se clickeó primero. Con menos de dos no hace nada.
    /// </summary>
    public void Alinear(AlineacionDisenador modo)
    {
        if (_seleccion.Count < 2) return;

        var refc = _seleccion[0];

        foreach (var c in _seleccion.Skip(1))
        {
            switch (modo)
            {
                case AlineacionDisenador.Izquierda: c.X = refc.X; break;
                case AlineacionDisenador.Derecha: c.X = refc.X + refc.Ancho - c.Ancho; break;
                case AlineacionDisenador.Arriba: c.Y = refc.Y; break;
                case AlineacionDisenador.Abajo: c.Y = refc.Y + refc.Alto - c.Alto; break;
                case AlineacionDisenador.MismoAncho: c.Ancho = refc.Ancho; break;
                case AlineacionDisenador.MismoAlto: c.Alto = refc.Alto; break;
            }
        }

        FormularioCambiado?.Invoke();
        CambioTerminado?.Invoke();
        Invalidate();
    }
}

/// <summary>Cómo alinear varios controles seleccionados.</summary>
public enum AlineacionDisenador
{
    Izquierda,
    Derecha,
    Arriba,
    Abajo,
    MismoAncho,
    MismoAlto
}
