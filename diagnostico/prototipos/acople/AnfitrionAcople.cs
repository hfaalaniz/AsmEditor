namespace PruebaAcople;

/// <summary>
/// El anfitrión del acople: tres zonas (izquierda, derecha, abajo) alrededor
/// del área de documentos, con divisores para redimensionarlas.
///
/// Coordina las dos operaciones difíciles:
///   - SACAR: arrastrar el título de un panel lo pasa a una
///     <see cref="VentanaFlotante"/> que sigue al ratón (el arrastre lo hace
///     Windows).
///   - ACOPLAR: mientras la flotante se mueve se muestran las
///     <see cref="GuiasAcople"/>; si se suelta sobre una guía, el panel vuelve
///     a una zona, con <see cref="VistaPreviaAcople"/> antes de soltar.
///
/// ⚠ EXCEPCIÓN A LA REGLA DEL DISEÑADOR ("Dock solo para cabecera y pie"): las
/// zonas y los divisores usan Dock. Un Splitter no funciona sin Dock y las
/// zonas tienen que redimensionarse con él; con Location/Anchor no hay forma
/// de que el usuario las agrande arrastrando el borde. Es el único lugar.
///
/// Prototipo: una zona tiene UN panel (las pestañas por zona son la 3b del
/// plan). Acoplar en una zona ocupada no se ofrece: su guía no aparece.
/// </summary>
public partial class AnfitrionAcople : UserControl
{
    private readonly GuiasAcople _guias = new();
    private readonly VistaPreviaAcople _vista = new();

    /// <summary>La última zona de cada panel: adonde vuelve con doble clic en la flotante.</summary>
    private readonly Dictionary<VentanaHerramienta, Zona> _ultimaZona = new();

    /// <summary>Las ventanas flotantes abiertas.</summary>
    private readonly List<VentanaFlotante> _flotantes = new();

    public AnfitrionAcople()
    {
        InitializeComponent();

        BackColor = Color.FromArgb(30, 30, 30);
        zonaCentro.BackColor = Color.FromArgb(30, 30, 30);
        lblCentro.ForeColor = Color.FromArgb(150, 150, 150);
        foreach (var d in new[] { divisorIzquierda, divisorDerecha, divisorAbajo }) d.BackColor = Color.FromArgb(63, 63, 70);

        ActualizarZonas();
        Disposed += AnfitrionAcople_Disposed;
    }

    /// <summary>Las ventanas flotantes abiertas (para las pruebas).</summary>
    public IReadOnlyList<VentanaFlotante> Flotantes => _flotantes;

    /// <summary>Agrega un panel al acople, en su zona inicial.</summary>
    public void Registrar(VentanaHerramienta v, Zona zona)
    {
        v.ArrastreIniciado += Ventana_ArrastreIniciado;
        v.CierrePedido += Ventana_CierrePedido;
        Acoplar(v, zona);
    }

    public bool ZonaOcupada(Zona z) => ContenedorDe(z).Controls.OfType<VentanaHerramienta>().Any();

    /// <summary>La zona donde está acoplado el panel, o null si está flotando (o cerrado).</summary>
    public Zona? ZonaDe(VentanaHerramienta v)
    {
        foreach (var z in Enum.GetValues<Zona>())
        {
            if (ContenedorDe(z).Controls.Contains(v)) return z;
        }
        return null;
    }

    /// <summary>
    /// Acopla el panel en la zona. False si la zona ya tiene otro panel.
    /// </summary>
    public bool Acoplar(VentanaHerramienta v, Zona zona)
    {
        if (ZonaOcupada(zona) && ZonaDe(v) != zona) return false;

        v.Parent = ContenedorDe(zona);
        v.Dock = DockStyle.Fill;
        _ultimaZona[v] = zona;

        ActualizarZonas();
        return true;
    }

    /// <summary>
    /// Saca el panel de su zona a una ventana flotante centrada bajo el punto
    /// (en pantalla), del mismo tamaño que tenía.
    /// </summary>
    public VentanaFlotante Desacoplar(VentanaHerramienta v, Point enPantalla)
    {
        var tamano = new Size(Math.Max(240, v.Width), Math.Max(180, v.Height));

        v.Parent = null;
        ActualizarZonas();

        var f = new VentanaFlotante();
        f.Contener(v);
        f.ClientSize = tamano;

        // El ratón queda sobre la barra de título de la flotante, a la mitad:
        // así el arrastre nativo arranca "agarrándola" del título.
        int altoTitulo = SystemInformation.ToolWindowCaptionHeight;
        f.Location = new Point(enPantalla.X - f.Width / 2, enPantalla.Y - altoTitulo / 2 - SystemInformation.FrameBorderSize.Height);

        f.Moviendo += Flotante_Moviendo;
        f.Soltada += Flotante_Soltada;
        f.VolverPedido += Flotante_VolverPedido;
        f.FormClosed += Flotante_FormClosed;

        _flotantes.Add(f);
        f.Show(FindForm());
        return f;
    }

    /// <summary>
    /// Qué guía está bajo el punto (en pantalla), contando solo las zonas
    /// libres: una zona ocupada no ofrece guía.
    /// </summary>
    public Zona? ZonaBajoGuia(Point enPantalla)
    {
        var z = GeometriaAcople.ZonaEn(enPantalla, RectangleToScreen(ClientRectangle));
        return z is not null && !ZonaOcupada(z.Value) ? z : null;
    }

    // ------------------------------------------------------------------
    // Arrastre
    // ------------------------------------------------------------------

    private void Ventana_ArrastreIniciado(VentanaHerramienta v, Point enPantalla)
    {
        var f = Desacoplar(v, enPantalla);

        // El botón del ratón sigue apretado: Windows sigue el arrastre desde acá.
        f.EmpezarArrastre();
    }

    private void Flotante_Moviendo(VentanaFlotante f, Point enPantalla)
    {
        var anfitrion = RectangleToScreen(ClientRectangle);
        var libres = Enum.GetValues<Zona>().Where(z => !ZonaOcupada(z)).ToList();
        var z = ZonaBajoGuia(enPantalla);

        var duenio = FindForm();
        if (duenio is null) return;

        _guias.Mostrar(anfitrion, libres, z, duenio);

        if (z is not null) _vista.Mostrar(GeometriaAcople.Destino(z.Value, anfitrion), duenio);
        else _vista.Hide();
    }

    private void Flotante_Soltada(VentanaFlotante f, Point enPantalla)
    {
        var z = ZonaBajoGuia(enPantalla);
        OcultarGuias();

        if (z is null) return;   // soltada en cualquier otro lado: queda flotando

        var v = f.Soltar();
        if (v is not null) Acoplar(v, z.Value);
        f.Close();
    }

    private void Flotante_VolverPedido(VentanaFlotante f)
    {
        if (f.Panel is not { } v || !_ultimaZona.TryGetValue(v, out var z) || ZonaOcupada(z)) return;

        f.Soltar();
        Acoplar(v, z);
        f.Close();
    }

    private void Flotante_FormClosed(object? sender, FormClosedEventArgs e)
    {
        if (sender is VentanaFlotante f) _flotantes.Remove(f);
    }

    private void Ventana_CierrePedido(VentanaHerramienta v)
    {
        v.Parent = null;
        ActualizarZonas();
    }

    private void OcultarGuias()
    {
        _guias.Hide();
        _vista.Hide();
    }

    // ------------------------------------------------------------------
    // Zonas
    // ------------------------------------------------------------------

    private Panel ContenedorDe(Zona z) => z switch
    {
        Zona.Izquierda => zonaIzquierda,
        Zona.Derecha => zonaDerecha,
        _ => zonaAbajo
    };

    private Splitter DivisorDe(Zona z) => z switch
    {
        Zona.Izquierda => divisorIzquierda,
        Zona.Derecha => divisorDerecha,
        _ => divisorAbajo
    };

    /// <summary>Una zona sin panel se oculta con su divisor: el centro ocupa el lugar.</summary>
    private void ActualizarZonas()
    {
        foreach (var z in Enum.GetValues<Zona>())
        {
            bool ocupada = ZonaOcupada(z);
            ContenedorDe(z).Visible = ocupada;
            DivisorDe(z).Visible = ocupada;
        }
    }

    private void AnfitrionAcople_Disposed(object? sender, EventArgs e)
    {
        _guias.Dispose();
        _vista.Dispose();
    }
}
