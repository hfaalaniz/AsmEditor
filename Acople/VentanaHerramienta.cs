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
/// <see cref="Activada"/>) y el AnfitrionAcople actualiza el modelo
/// (Core\Acople\DisenoAcople) y la acomoda.
///
/// ⚠ La chincheta está en el diseñador pero oculta: su función es la 3c del
/// plan (auto-ocultar). Un botón que no hace nada confunde más que uno que no
/// está.
/// </summary>
public partial class VentanaHerramienta : UserControl
{
    // Segoe MDL2 Assets, la misma fuente de los botones de la barra de título.
    // Se ponen acá y no en el diseñador: son glifos, no texto.
    private static readonly string GlifoMenu = ((char)0xE70D).ToString();       // ChevronDown
    private static readonly string GlifoChincheta = ((char)0xE718).ToString();  // Pin
    private static readonly string GlifoCerrar = ((char)0xE8BB).ToString();     // ChromeClose

    private Control? _contenido;
    private bool _activa;

    /// <summary>✕ o «Ocultar» del menú del panel.</summary>
    public event EventHandler? OcultarPedido;

    /// <summary>El foco entró al panel: pasa a ser el activo de su zona.</summary>
    public event EventHandler? Activada;

    public VentanaHerramienta()
    {
        InitializeComponent();

        btnMenu.Text = GlifoMenu;
        btnChincheta.Text = GlifoChincheta;
        btnCerrar.Text = GlifoCerrar;

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
        miOcultar.ForeColor = Tema.Texto;
    }

    private void VentanaHerramienta_Disposed(object? sender, EventArgs e) => Tema.TemaCambiado -= AplicarTema;
}
