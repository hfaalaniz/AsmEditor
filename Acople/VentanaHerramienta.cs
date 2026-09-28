using System.ComponentModel;

namespace AsmEditor;

/// <summary>
/// Un panel acoplable (el explorador, la lista de errores, la salida...), como
/// las ventanas de herramientas de Visual Studio: barra de título con el
/// nombre, ▾ (menú del panel), chincheta y ✕, y el contenido debajo.
///
/// El contenido es un control cualquiera que pone quien lo usa
/// (<see cref="Contenido"/>): la ventana no sabe qué muestra.
///
/// La ventana no decide dónde está: avisa (<see cref="OcultarPedido"/>,
/// <see cref="ChinchetaPedida"/>, <see cref="Activada"/>) y el
/// AnfitrionAcople actualiza el modelo (Core\Acople\DisenoAcople) y la
/// acomoda.
///
/// La chincheta (3c del plan), como en VS: acoplado se ve vertical («fijo»)
/// y al apretarla el panel se repliega al borde; replegado se ve acostada y
/// al apretarla vuelve a su zona.
/// </summary>
public partial class VentanaHerramienta : UserControl
{
    // Segoe MDL2 Assets, la misma fuente de los botones de la barra de título.
    // Se ponen acá y no en el diseñador: son glifos, no texto.
    private static readonly string GlifoMenu = ((char)0xE70D).ToString();          // ChevronDown
    private static readonly string GlifoFijo = ((char)0xE840).ToString();          // Pinned: acoplado
    private static readonly string GlifoAutoOculto = ((char)0xE718).ToString();    // Pin: replegado
    private static readonly string GlifoCerrar = ((char)0xE8BB).ToString();        // ChromeClose

    // Los tooltips van acá y no en el diseñador (regla del diseñador: VS los
    // mandaría al .resx, lejos del código que los explica).
    private readonly ToolTip _ayuda = new();

    private Control? _contenido;
    private bool _activa;
    private bool _autoOculta;

    /// <summary>✕ o «Ocultar» del menú del panel.</summary>
    public event EventHandler? OcultarPedido;

    /// <summary>La chincheta (o «Ocultar automáticamente»): replegar o volver a acoplar.</summary>
    public event EventHandler? ChinchetaPedida;

    /// <summary>El foco entró al panel: pasa a ser el activo de su zona.</summary>
    public event EventHandler? Activada;

    public VentanaHerramienta()
    {
        InitializeComponent();

        btnMenu.Text = GlifoMenu;
        btnCerrar.Text = GlifoCerrar;
        _ayuda.SetToolTip(btnMenu, "Opciones del panel");
        _ayuda.SetToolTip(btnCerrar, "Ocultar");
        MostrarEstadoChincheta();

        AplicarTema();
        Tema.TemaCambiado += AplicarTema;
        Disposed += VentanaHerramienta_Disposed;
    }

    /// <summary>
    /// El nombre fijo del panel ("explorador", "errores"): con él se lo guarda
    /// en el diseño. No es el título, que se puede cambiar.
    /// </summary>
    [Category("Acople")]
    [Description("Identificador fijo del panel en el diseño guardado.")]
    [DefaultValue("")]
    public string Id { get; set; } = "";

    [Category("Acople")]
    [Description("El nombre que se ve en la barra de título y en su pestaña.")]
    public string Titulo
    {
        get => lblTitulo.Text;
        set => lblTitulo.Text = value;
    }

    /// <summary>Lo que muestra el panel. Se ajusta a todo el espacio.</summary>
    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Control? Contenido
    {
        get => _contenido;
        set
        {
            if (_contenido is not null) pnlContenido.Controls.Remove(_contenido);
            _contenido = value;
            if (value is null) return;

            value.Dock = DockStyle.Fill;
            pnlContenido.Controls.Add(value);
        }
    }

    /// <summary>El renderer de los menús (lo pasa el anfitrión, para que se vean como los del editor).</summary>
    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public ToolStripRenderer? RendererMenus
    {
        get => cmsVentana.Renderer;
        set { if (value is not null) cmsVentana.Renderer = value; }
    }

    /// <summary>True si el foco está en el panel: la barra de título se resalta, como en VS.</summary>
    [Browsable(false)]
    public bool Activa
    {
        get => _activa;
        private set
        {
            if (_activa == value) return;
            _activa = value;
            AplicarTema();
        }
    }

    /// <summary>Replegado al borde (lo pone el anfitrión): cambia la chincheta y la marca del menú.</summary>
    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool AutoOculta
    {
        get => _autoOculta;
        set
        {
            if (_autoOculta == value) return;
            _autoOculta = value;
            MostrarEstadoChincheta();
        }
    }

    /// <summary>
    /// El menú ▾ está abierto. Lo mira el anfitrión para no plegar un panel
    /// desplegado mientras se elige algo del menú (el menú queda fuera del
    /// panel, y el ratón también).
    /// </summary>
    [Browsable(false)]
    public bool MenuAbierto => cmsVentana.Visible;

    private void MostrarEstadoChincheta()
    {
        btnChincheta.Text = _autoOculta ? GlifoAutoOculto : GlifoFijo;
        _ayuda.SetToolTip(btnChincheta, _autoOculta ? "Acoplar" : "Ocultar automáticamente");
        miAutoOcultar.Checked = _autoOculta;
    }

    /// <summary>Pone el foco en el contenido.</summary>
    public void Enfocar()
    {
        if (_contenido is { CanFocus: true } c) c.Focus();
        else pnlContenido.SelectNextControl(null, true, true, true, true);
    }

    // ------------------------------------------------------------------
    // Eventos
    // ------------------------------------------------------------------

    private void VentanaHerramienta_Enter(object? sender, EventArgs e)
    {
        Activa = true;
        Activada?.Invoke(this, EventArgs.Empty);
    }

    private void VentanaHerramienta_Leave(object? sender, EventArgs e) => Activa = false;

    /// <summary>Un clic en la barra de título lleva el foco al contenido, como en VS.</summary>
    private void Titulo_MouseDown(object? sender, MouseEventArgs e) => Enfocar();

    private void btnMenu_Click(object? sender, EventArgs e) =>
        cmsVentana.Show(btnMenu, new Point(0, btnMenu.Height));

    private void btnCerrar_Click(object? sender, EventArgs e) => OcultarPedido?.Invoke(this, EventArgs.Empty);

    private void miOcultar_Click(object? sender, EventArgs e) => OcultarPedido?.Invoke(this, EventArgs.Empty);

    private void btnChincheta_Click(object? sender, EventArgs e) => ChinchetaPedida?.Invoke(this, EventArgs.Empty);

    private void miAutoOcultar_Click(object? sender, EventArgs e) => ChinchetaPedida?.Invoke(this, EventArgs.Empty);

    // ------------------------------------------------------------------
    // Aspecto
    // ------------------------------------------------------------------

    private void AplicarTema()
    {
        var fondoTitulo = _activa ? Tema.Seleccion : Tema.Superficie2;
        var textoTitulo = _activa ? Tema.Texto : Tema.Texto2;

        BackColor = Tema.Superficie;
        pnlContenido.BackColor = Tema.Superficie;
        pnlTitulo.BackColor = fondoTitulo;
        lblTitulo.ForeColor = textoTitulo;

        foreach (var b in new[] { btnMenu, btnChincheta, btnCerrar })
        {
            b.BackColor = fondoTitulo;
            b.ForeColor = textoTitulo;
            b.FlatAppearance.MouseOverBackColor = Tema.Realzar(fondoTitulo, 18);
            b.FlatAppearance.MouseDownBackColor = Tema.Realzar(fondoTitulo, 30);
        }

        cmsVentana.BackColor = Tema.Superficie2;
        miAutoOcultar.ForeColor = Tema.Texto;
        miOcultar.ForeColor = Tema.Texto;
    }

    private void VentanaHerramienta_Disposed(object? sender, EventArgs e)
    {
        Tema.TemaCambiado -= AplicarTema;
        _ayuda.Dispose();
    }
}
