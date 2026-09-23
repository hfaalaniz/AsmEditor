namespace AsmEditor;

/// <summary>Los dos temas del editor.</summary>
public enum ModoTema
{
    Oscuro,
    Claro
}

/// <summary>
/// LA PALETA DEL EDITOR, EN UN SOLO LUGAR.
///
/// La estructura y los tokens del tema oscuro salen de `Estilo.cs` del launcher
/// de Trapezoide, donde los colores están medidos contra el fondo y anotados con
/// su relación de contraste. Copiarlos en vez de elegir unos nuevos a ojo evita
/// repetir ese trabajo y evita equivocarlo.
///
/// ⚠ EL ACENTO DORADO (#D4A017) ES A PROPÓSITO EL DE TRAPEZOIDE: identifica los
/// sistemas de Fabián. El logo y el ícono, en cambio, son propios del editor
/// (ver <see cref="DibujarLogo"/>): el trapecio de los cabrestantes pertenece a
/// otro producto y acá confundiría.
///
/// ⚠ EL TEMA CLARO NO SALE DEL LAUNCHER: el launcher es solo oscuro (su
/// FormConfig era el único claro y lo pasaron a oscuro justo para no tener dos
/// estéticas). Los tokens claros de acá son nuevos, elegidos para mantener las
/// mismas relaciones de contraste invertidas.
///
/// ⚠ AL CAMBIAR DE TEMA hay que volver a aplicar el estilo a los formularios
/// abiertos: los colores se leen una vez al construir cada control, no hay
/// enlace vivo. Ver <see cref="TemaCambiado"/>.
/// </summary>
public static class Tema
{
    /// <summary>Se dispara cuando cambia el modo, para que las ventanas se repinten.</summary>
    public static event Action? TemaCambiado;

    private static ModoTema _modo = ModoTema.Oscuro;

    public static ModoTema Modo
    {
        get => _modo;
        set
        {
            if (_modo == value) return;
            _modo = value;
            TemaCambiado?.Invoke();
        }
    }

    public static bool EsOscuro => _modo == ModoTema.Oscuro;

    // ── Fondos ───────────────────────────────────────────────────────────
    /// <summary>Fondo de la ventana.</summary>
    public static Color Fondo => EsOscuro ? Hex(0x0B0D10) : Hex(0xF4F5F7);

    /// <summary>Superficie elevada (tarjetas, cuerpo).</summary>
    public static Color Superficie => EsOscuro ? Hex(0x161A20) : Hex(0xFFFFFF);

    /// <summary>Superficie un escalón más arriba.</summary>
    public static Color Superficie2 => EsOscuro ? Hex(0x1D222A) : Hex(0xE9EBEF);

    /// <summary>Superficie de los controles.</summary>
    public static Color Superficie3 => EsOscuro ? Hex(0x242B35) : Hex(0xDDE1E7);

    // ── Líneas ───────────────────────────────────────────────────────────
    /// <summary>Borde visible.</summary>
    public static Color Linea => EsOscuro ? Hex(0x68758A) : Hex(0x9AA3B0);

    /// <summary>Borde discreto, separadores.</summary>
    public static Color LineaSuave => EsOscuro ? Hex(0x454F5E) : Hex(0xC5CBD4);

    // ── Texto ────────────────────────────────────────────────────────────
    /// <summary>Texto principal.</summary>
    public static Color Texto => EsOscuro ? Hex(0xE8EAED) : Hex(0x12161C);

    /// <summary>Texto secundario.</summary>
    public static Color Texto2 => EsOscuro ? Hex(0xA2ABB9) : Hex(0x4A5361);

    /// <summary>Texto terciario, el mínimo legible.</summary>
    public static Color Texto3 => EsOscuro ? Hex(0x8B94A2) : Hex(0x646D7B);

    // ── Estado ───────────────────────────────────────────────────────────
    /// <summary>El acento de la marca: el dorado de Trapezoide.</summary>
    public static Color Acento => EsOscuro ? Hex(0xD4A017) : Hex(0x9A7410);

    /// <summary>Acento apagado.</summary>
    public static Color AcentoTenue => EsOscuro ? Hex(0x8A6A10) : Hex(0xC2A24E);

    /// <summary>Todo bien.</summary>
    public static Color Ok => EsOscuro ? Hex(0x3FB950) : Hex(0x1A7F37);

    /// <summary>Advertencia.</summary>
    public static Color Aviso => EsOscuro ? Hex(0xD29922) : Hex(0x9A6700);

    /// <summary>Error, y lo que no se puede deshacer.</summary>
    public static Color Critico => EsOscuro ? Hex(0xF85149) : Hex(0xCF222E);

    /// <summary>Información.</summary>
    public static Color Info => EsOscuro ? Hex(0x58A6FF) : Hex(0x0969DA);

    /// <summary>Selección: fondo de lo elegido en listas y pestañas.</summary>
    public static Color Seleccion => EsOscuro ? Hex(0x094771) : Hex(0xCCE4F7);

    // ── Colores del editor de código ─────────────────────────────────────
    // El resaltado de sintaxis necesita su propio juego: los tokens de arriba
    // son para la interfaz, no para el código.

    public static Color CodigoFondo => EsOscuro ? Hex(0x1E1E1E) : Hex(0xFFFFFF);
    public static Color CodigoTexto => EsOscuro ? Hex(0xDCDCDC) : Hex(0x1F1F1F);
    public static Color CodigoMargen => EsOscuro ? Hex(0x252526) : Hex(0xF0F0F0);
    public static Color CodigoNumeroLinea => EsOscuro ? Hex(0x858585) : Hex(0x6E7681);

    public static Color CodigoInstruccion => EsOscuro ? Hex(0x569CD6) : Hex(0x0000C0);
    public static Color CodigoRegistro => EsOscuro ? Hex(0x4EC9B0) : Hex(0x0F7B6C);
    public static Color CodigoDirectiva => EsOscuro ? Hex(0xC586C0) : Hex(0xA31DB1);
    public static Color CodigoEtiqueta => EsOscuro ? Hex(0xDCDCAA) : Hex(0x8A6D00);
    public static Color CodigoNumero => EsOscuro ? Hex(0xB5CEA8) : Hex(0x098658);
    public static Color CodigoCadena => EsOscuro ? Hex(0xCE9178) : Hex(0xA31515);
    public static Color CodigoComentario => EsOscuro ? Hex(0x6A9955) : Hex(0x008000);

    /// <summary>Fondo del panel de salida de las herramientas.</summary>
    public static Color SalidaFondo => EsOscuro ? Hex(0x0C0C0C) : Hex(0xFBFBFB);
    public static Color SalidaTexto => EsOscuro ? Hex(0xCCCCCC) : Hex(0x2A2A2A);

    // ── Tipografía ───────────────────────────────────────────────────────
    // Segoe UI es la del sistema en Windows 10/11: no hay que instalar nada y
    // se ve nativa.
    public static Font Titulo => new("Segoe UI Semibold", 10f);
    public static Font Cuerpo => new("Segoe UI", 9f);
    public static Font Chico => new("Segoe UI", 8.25f);
    public static Font Etiqueta => new("Segoe UI Semibold", 9f);

    /// <summary>La del editor de código y la salida: monoespaciada.</summary>
    public static Font Codigo => new("Consolas", 11f);
    public static Font CodigoChico => new("Consolas", 9.5f);

    /// <summary>Radio de las esquinas redondeadas.</summary>
    public const int Radio = 6;

    private static Color Hex(int rgb) =>
        Color.FromArgb((rgb >> 16) & 0xFF, (rgb >> 8) & 0xFF, rgb & 0xFF);

    /// <summary>
    /// Aclara u oscurece un color en <paramref name="paso"/> puntos por canal.
    ///
    /// Así se construyen los estados de hover y de clic: no hay un token aparte
    /// para la superficie un poco más clara, se calcula desde la base, y así el
    /// hover sigue al color si la paleta cambia.
    ///
    /// ⚠ EN TEMA CLARO EL HOVER OSCURECE, no aclara: sobre un fondo casi blanco,
    /// aclarar no se ve. El signo lo decide el tema, no quien llama.
    /// </summary>
    public static Color Realzar(Color c, int paso)
    {
        int d = EsOscuro ? paso : -paso;
        return Color.FromArgb(
            Math.Clamp(c.R + d, 0, 255),
            Math.Clamp(c.G + d, 0, 255),
            Math.Clamp(c.B + d, 0, 255));
    }

    /// <summary>
    /// El logo del editor. El dibujo vive en <see cref="LogoEditor"/>; acá queda
    /// el atajo que le pasa los colores del tema activo.
    /// </summary>
    public static void DibujarLogo(Graphics g, float x, float y, float lado, Color color)
        => LogoEditor.Dibujar(g, x, y, lado, color, Texto3, detalle: lado >= 32);

    /// <summary>
    /// Ícono de la aplicación, con todos los tamaños que Windows necesita.
    /// Ver <see cref="LogoEditor.CrearIcono"/>.
    /// </summary>
    public static Icon CrearIcono(int lado = 32) => LogoEditor.CrearIcono();

    /// <summary>
    /// Rectángulo de esquinas redondeadas, para fondos y bordes.
    /// </summary>
    public static System.Drawing.Drawing2D.GraphicsPath Redondeado(RectangleF r, float radio)
    {
        var p = new System.Drawing.Drawing2D.GraphicsPath();
        var d = radio * 2;

        // Un radio mayor que la mitad del lado corto da un arco imposible y
        // GDI+ dibuja cualquier cosa: se acota.
        d = Math.Min(d, Math.Min(r.Width, r.Height));
        if (d <= 0) { p.AddRectangle(r); return p; }

        p.AddArc(r.X, r.Y, d, d, 180, 90);
        p.AddArc(r.Right - d, r.Y, d, d, 270, 90);
        p.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
        p.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
        p.CloseFigure();
        return p;
    }
}
