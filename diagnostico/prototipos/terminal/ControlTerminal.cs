namespace PruebaTerminal;

/// <summary>
/// La terminal en pantalla: dibuja la <see cref="PantallaTerminal"/> y le
/// manda a la sesión lo que se tipea, traducido a lo que espera una consola
/// (flechas como ESC [ A, Retroceso como DEL, Ctrl+C como 0x03...).
/// </summary>
public partial class ControlTerminal : UserControl
{
    private SesionConPty? _sesion;
    private PantallaTerminal _pantalla = new(80, 24);
    private InterpreteVT _vt;

    private readonly Font _fuente = new("Consolas", 10f);
    private Size _celda;

    /// <summary>Para no pedir un repintado por cada lectura del caño: uno pendiente alcanza.</summary>
    private int _repintadoPendiente;

    /// <summary>Cambió el tamaño en celdas o el título (para la barra de estado del formulario).</summary>
    public event Action? EstadoCambiado;

    /// <summary>El programa de la terminal terminó.</summary>
    public event Action? ProgramaTerminado;

    public ControlTerminal()
    {
        InitializeComponent();

        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint |
                 ControlStyles.OptimizedDoubleBuffer | ControlStyles.Selectable, true);
        TabStop = true;

        _vt = new InterpreteVT(_pantalla);
        _celda = MedirCelda();
        BackColor = Paleta.Color(PantallaTerminal.FondoPorDefecto);

        Disposed += ControlTerminal_Disposed;
    }

    public PantallaTerminal Pantalla => _pantalla;
    public int Columnas => _pantalla.Columnas;
    public int Filas => _pantalla.Filas;

    /// <summary>Arranca el programa con el tamaño que entra en el control.</summary>
    public void Arrancar(string lineaDeComandos, string? carpeta, IDictionary<string, string>? variables)
    {
        var (cols, filas) = CeldasQueEntran();

        _pantalla = new PantallaTerminal(cols, filas);
        _vt = new InterpreteVT(_pantalla);

        _sesion = new SesionConPty();
        _sesion.DatosRecibidos += Sesion_DatosRecibidos;
        _sesion.Terminado += Sesion_Terminado;
        _sesion.Iniciar(lineaDeComandos, (short)cols, (short)filas, carpeta, variables);

        EstadoCambiado?.Invoke();
    }

    private void Sesion_DatosRecibidos(byte[] datos, int n)
    {
        _vt.Procesar(datos, n);

        // Llega desde el hilo lector: el repintado se pide al de la interfaz.
        if (Interlocked.Exchange(ref _repintadoPendiente, 1) == 0 && IsHandleCreated)
        {
            BeginInvoke(new Action(Repintar));
        }
    }

    private void Repintar()
    {
        Interlocked.Exchange(ref _repintadoPendiente, 0);
        Invalidate();
        EstadoCambiado?.Invoke();
    }

    private void Sesion_Terminado()
    {
        if (IsHandleCreated) BeginInvoke(new Action(() => ProgramaTerminado?.Invoke()));
    }

    // ------------------------------------------------------------------
    // Tamaño
    // ------------------------------------------------------------------

    private Size MedirCelda()
    {
        // El ancho de 10 "M" dividido 10: TextRenderer agrega márgenes a una sola letra.
        var s = TextRenderer.MeasureText("MMMMMMMMMM", _fuente, Size.Empty, TextFormatFlags.NoPadding);
        return new Size(Math.Max(1, (int)Math.Round(s.Width / 10.0)), Math.Max(1, s.Height));
    }

    private (int cols, int filas) CeldasQueEntran() =>
        (Math.Max(20, ClientSize.Width / _celda.Width), Math.Max(5, ClientSize.Height / _celda.Height));

    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);
        if (_celda.Width == 0) return;

        var (cols, filas) = CeldasQueEntran();
        if (cols == _pantalla.Columnas && filas == _pantalla.Filas) return;

        lock (_pantalla.Candado) _pantalla.Redimensionar(cols, filas);

        // El programa se entera y repinta con el ancho nuevo.
        _sesion?.Redimensionar((short)cols, (short)filas);

        Invalidate();
        EstadoCambiado?.Invoke();
    }

    // ------------------------------------------------------------------
    // Dibujo
    // ------------------------------------------------------------------

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.Clear(Paleta.Color(PantallaTerminal.FondoPorDefecto));

        lock (_pantalla.Candado)
        {
            for (int f = 0; f < _pantalla.Filas; f++)
            {
                int y = f * _celda.Height;
                int c = 0;

                // Se dibuja por tramos con los mismos colores: letra por letra
                // es muchísimo más lento y el texto sale con saltos.
                while (c < _pantalla.Columnas)
                {
                    var celda = _pantalla[f, c];
                    int inicio = c;
                    var sb = new System.Text.StringBuilder();

                    while (c < _pantalla.Columnas)
                    {
                        var otra = _pantalla[f, c];
                        if (otra.Frente != celda.Frente || otra.Fondo != celda.Fondo || otra.Negrita != celda.Negrita) break;
                        sb.Append(otra.Caracter == '\0' ? ' ' : otra.Caracter);
                        c++;
                    }

                    var rect = new Rectangle(inicio * _celda.Width, y, (c - inicio) * _celda.Width, _celda.Height);

                    if (celda.Fondo != PantallaTerminal.FondoPorDefecto)
                    {
                        using var b = new SolidBrush(Paleta.Color(celda.Fondo));
                        g.FillRectangle(b, rect);
                    }

                    var texto = sb.ToString();
                    if (texto.Trim().Length > 0)
                    {
                        TextRenderer.DrawText(g, texto, _fuente, rect.Location, Paleta.Color(celda.Frente),
                                              TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix);
                    }
                }
            }

            if (_pantalla.CursorVisible && Focused)
            {
                var r = new Rectangle(_pantalla.CursorColumna * _celda.Width,
                                      _pantalla.CursorFila * _celda.Height + _celda.Height - 3,
                                      _celda.Width, 3);
                using var b = new SolidBrush(Paleta.Color(PantallaTerminal.FrentePorDefecto));
                g.FillRectangle(b, r);
            }
        }
    }

    protected override void OnGotFocus(EventArgs e) { base.OnGotFocus(e); Invalidate(); }
    protected override void OnLostFocus(EventArgs e) { base.OnLostFocus(e); Invalidate(); }

    // ------------------------------------------------------------------
    // Teclado
    // ------------------------------------------------------------------

    protected override bool IsInputKey(Keys keyData)
    {
        // Sin esto WinForms se queda con flechas y Tab para mover el foco.
        switch (keyData & Keys.KeyCode)
        {
            case Keys.Up: case Keys.Down: case Keys.Left: case Keys.Right:
            case Keys.Tab: case Keys.Home: case Keys.End: case Keys.Escape:
                return true;
        }
        return base.IsInputKey(keyData);
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);

        string? secuencia = e.KeyCode switch
        {
            Keys.Up => "\x1b[A",
            Keys.Down => "\x1b[B",
            Keys.Right => "\x1b[C",
            Keys.Left => "\x1b[D",
            Keys.Home => "\x1b[H",
            Keys.End => "\x1b[F",
            Keys.Delete => "\x1b[3~",
            Keys.PageUp => "\x1b[5~",
            Keys.PageDown => "\x1b[6~",
            _ => null
        };

        if (secuencia is null) return;

        _sesion?.Escribir(secuencia);
        e.Handled = true;
        e.SuppressKeyPress = true;
    }

    protected override void OnKeyPress(KeyPressEventArgs e)
    {
        base.OnKeyPress(e);

        // Ctrl+letra ya llega como carácter de control (Ctrl+C = 0x03). El
        // Retroceso llega como 0x08; las consolas esperan DEL (0x7F).
        var texto = e.KeyChar == '\b' ? "\x7f" : e.KeyChar.ToString();

        _sesion?.Escribir(texto);
        e.Handled = true;
    }

    /// <summary>
    /// Al cerrar: la sesión (termina el programa) y la fuente. Va por el evento
    /// porque Dispose(bool) ya lo define el Designer.cs.
    /// </summary>
    private void ControlTerminal_Disposed(object? sender, EventArgs e)
    {
        _sesion?.Dispose();
        _fuente.Dispose();
    }
}

/// <summary>
/// La paleta de colores: los 16 ANSI (esquema "Campbell", el de Windows
/// Terminal), el cubo de 6×6×6 y los 24 grises de la paleta de 256, y el color
/// directo RGB.
/// </summary>
public static class Paleta
{
    private static readonly int[] Ansi16 =
    {
        0x0C0C0C, 0xC50F1F, 0x13A10E, 0xC19C00, 0x0037DA, 0x881798, 0x3A96DD, 0xCCCCCC,
        0x767676, 0xE74856, 0x16C60C, 0xF9F1A5, 0x3B78FF, 0xB4009E, 0x61D6D6, 0xF2F2F2
    };

    public static Color Color(int valor)
    {
        if ((valor & PantallaTerminal.MarcaRgb) != 0) return Rgb(valor & 0xFFFFFF);

        if (valor < 16) return Rgb(Ansi16[Math.Max(0, valor)]);

        if (valor < 232)
        {
            int i = valor - 16;
            int r = i / 36, g = i / 6 % 6, b = i % 6;
            static int N(int x) => x == 0 ? 0 : 55 + x * 40;
            return System.Drawing.Color.FromArgb(N(r), N(g), N(b));
        }

        int gris = 8 + (Math.Min(valor, 255) - 232) * 10;
        return System.Drawing.Color.FromArgb(gris, gris, gris);
    }

    private static Color Rgb(int rgb) => System.Drawing.Color.FromArgb((rgb >> 16) & 0xFF, (rgb >> 8) & 0xFF, rgb & 0xFF);
}
