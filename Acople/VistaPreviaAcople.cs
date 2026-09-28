namespace AsmEditor;

/// <summary>
/// La vista previa de acoplar arrastrando (3e del plan): un rectángulo
/// translúcido del color de acento sobre el lugar donde quedaría el panel si
/// se suelta ahora (<see cref="Core.Acople.GeometriaAcople.VistaPrevia"/>).
/// Como las guías, no se activa ni recibe el ratón.
/// </summary>
public partial class VistaPreviaAcople : Form
{
    private const int WS_EX_TRANSPARENT = 0x00000020;
    private const int WS_EX_TOOLWINDOW = 0x00000080;
    private const int WS_EX_NOACTIVATE = 0x08000000;

    public VistaPreviaAcople()
    {
        InitializeComponent();
    }

    protected override bool ShowWithoutActivation => true;

    protected override CreateParams CreateParams
    {
        get
        {
            var cp = base.CreateParams;
            cp.ExStyle |= WS_EX_TRANSPARENT | WS_EX_TOOLWINDOW | WS_EX_NOACTIVATE;
            return cp;
        }
    }

    /// <summary>La muestra en ese rectángulo (en pantalla), con el acento del tema de ahora.</summary>
    public void Mostrar(Rectangle enPantalla, Form duenio)
    {
        BackColor = Tema.Acento;
        if (Bounds != enPantalla) Bounds = enPantalla;
        if (!Visible) Show(duenio);
    }
}
