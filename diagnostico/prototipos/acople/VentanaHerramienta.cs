using System.ComponentModel;

namespace PruebaAcople;

/// <summary>
/// Un panel acoplable (el explorador, la salida...): barra de título con su
/// nombre y el botón de cerrar, y el contenido.
///
/// Arrastrar la barra de título más allá de la tolerancia de clic avisa
/// <see cref="ArrastreIniciado"/>: el anfitrión lo saca a una ventana flotante.
/// </summary>
public partial class VentanaHerramienta : UserControl
{
    private Point _puntoApretado;
    private bool _apretado;

    /// <summary>Se empezó a arrastrar la barra de título (punto en pantalla).</summary>
    public event Action<VentanaHerramienta, Point>? ArrastreIniciado;

    /// <summary>Se pidió cerrar el panel.</summary>
    public event Action<VentanaHerramienta>? CierrePedido;

    public VentanaHerramienta()
    {
        InitializeComponent();

        BackColor = Color.FromArgb(37, 37, 38);
        pnlTitulo.BackColor = Color.FromArgb(45, 45, 48);
        lblTitulo.ForeColor = Color.FromArgb(220, 220, 220);
        lblContenido.ForeColor = Color.FromArgb(170, 170, 170);
        btnCerrar.ForeColor = Color.FromArgb(200, 200, 200);
        btnCerrar.FlatAppearance.MouseOverBackColor = Color.FromArgb(70, 70, 74);
    }

    [Category("Acople")]
    [Description("El nombre que se ve en la barra de título.")]
    public string Titulo
    {
        get => lblTitulo.Text;
        set => lblTitulo.Text = value;
    }

    [Category("Acople")]
    [Description("Texto de muestra del contenido.")]
    public string Descripcion
    {
        get => lblContenido.Text;
        set => lblContenido.Text = value;
    }

    private void Titulo_MouseDown(object? sender, MouseEventArgs e)
    {
        if (e.Button != MouseButtons.Left) return;
        _apretado = true;
        _puntoApretado = Control.MousePosition;
    }

    private void Titulo_MouseMove(object? sender, MouseEventArgs e)
    {
        if (!_apretado) return;

        // Tolerancia de clic de Windows: un temblor no saca el panel.
        var t = SystemInformation.DragSize;
        var p = Control.MousePosition;

        if (Math.Abs(p.X - _puntoApretado.X) <= t.Width / 2 && Math.Abs(p.Y - _puntoApretado.Y) <= t.Height / 2) return;

        _apretado = false;
        ArrastreIniciado?.Invoke(this, p);
    }

    private void Titulo_MouseUp(object? sender, MouseEventArgs e) => _apretado = false;

    private void btnCerrar_Click(object? sender, EventArgs e) => CierrePedido?.Invoke(this);
}
