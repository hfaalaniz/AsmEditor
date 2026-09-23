namespace PruebaBarraTitulo;

/// <summary>
/// La barra de título propia: menú integrado, título y los tres botones de la
/// ventana.
///
/// ⚠ SU SUPERFICIE VACÍA TIENE QUE SER "TRANSPARENTE" PARA WINDOWS. Cuando el
/// ratón está sobre la barra, Windows le pregunta a ESTE control qué hay ahí
/// (WM_NCHITTEST). Si contesta lo normal ("área de cliente"), la ventana no se
/// puede arrastrar ni maximizar con doble clic. Contestando HTTRANSPARENT la
/// pregunta pasa al formulario, que responde "título" o "borde de arriba".
/// El menú y los botones no lo hacen: ellos sí tienen que recibir el clic.
/// </summary>
public partial class BarraTitulo : UserControl
{
    private const int WM_NCHITTEST = 0x0084;
    private const int HTTRANSPARENT = -1;

    public BarraTitulo()
    {
        InitializeComponent();
        AplicarColores();
    }

    public string Titulo
    {
        get => lblTitulo.Text;
        set => lblTitulo.Text = value;
    }

    /// <summary>Cambia el glifo según la ventana esté maximizada o no.</summary>
    public void ActualizarBotonMaximizar(bool maximizada)
    {
        // Segoe MDL2 Assets: E922 = maximizar, E923 = restaurar.
        btnMaximizar.Text = maximizada ? "\uE923" : "\uE922";
    }

    /// <summary>
    /// Colores del tema oscuro. Van acá y no en el Designer.cs porque en el
    /// editor real cambian en vivo con el tema.
    /// </summary>
    private void AplicarColores()
    {
        var fondo = Color.FromArgb(31, 31, 31);
        var texto = Color.FromArgb(204, 204, 204);

        BackColor = fondo;
        lblTitulo.ForeColor = texto;
        menu.BackColor = fondo;
        menu.ForeColor = texto;

        foreach (var b in new[] { btnMinimizar, btnMaximizar, btnCerrar })
        {
            b.BackColor = fondo;
            b.ForeColor = texto;
            b.FlatAppearance.MouseOverBackColor = Color.FromArgb(62, 62, 64);
            b.FlatAppearance.MouseDownBackColor = Color.FromArgb(80, 80, 82);
        }

        // Como en Windows: el de cerrar se pone rojo.
        btnCerrar.FlatAppearance.MouseOverBackColor = Color.FromArgb(232, 17, 35);
        btnCerrar.FlatAppearance.MouseDownBackColor = Color.FromArgb(241, 112, 122);
    }

    protected override void WndProc(ref Message m)
    {
        if (m.Msg == WM_NCHITTEST)
        {
            m.Result = (IntPtr)HTTRANSPARENT;
            return;
        }

        base.WndProc(ref m);
    }

    private void btnMinimizar_Click(object? sender, EventArgs e)
    {
        if (FindForm() is Form f) f.WindowState = FormWindowState.Minimized;
    }

    private void btnMaximizar_Click(object? sender, EventArgs e)
    {
        if (FindForm() is not Form f) return;

        f.WindowState = f.WindowState == FormWindowState.Maximized
            ? FormWindowState.Normal
            : FormWindowState.Maximized;
    }

    private void btnCerrar_Click(object? sender, EventArgs e) => FindForm()?.Close();

    private void salirToolStripMenuItem_Click(object? sender, EventArgs e) => FindForm()?.Close();
}

/// <summary>
/// La etiqueta del título. Igual que la barra, deja pasar la pregunta de
/// Windows al formulario: si no, sobre el texto del título no se podría
/// arrastrar la ventana, que es justo donde uno la agarra.
/// </summary>
public class EtiquetaTitulo : Label
{
    private const int WM_NCHITTEST = 0x0084;
    private const int HTTRANSPARENT = -1;

    protected override void WndProc(ref Message m)
    {
        if (m.Msg == WM_NCHITTEST)
        {
            m.Result = (IntPtr)HTTRANSPARENT;
            return;
        }

        base.WndProc(ref m);
    }
}
