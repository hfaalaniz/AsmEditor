namespace PruebaAcople;

/// <summary>
/// Ventana de prueba con tres paneles: el explorador a la derecha, la salida
/// abajo y la lista de errores a la izquierda.
///
/// Los paneles se ven en el diseñador sueltos sobre el formulario; al cargar,
/// el anfitrión los toma y los ubica en su zona (Registrar).
/// </summary>
public partial class FormPrueba : Form
{
    public FormPrueba()
    {
        InitializeComponent();
        BackColor = Color.FromArgb(30, 30, 30);
    }

    public AnfitrionAcople Anfitrion => anfitrion;
    public VentanaHerramienta Explorador => ventanaExplorador;
    public VentanaHerramienta Salida => ventanaSalida;
    public VentanaHerramienta Errores => ventanaErrores;

    private void FormPrueba_Load(object? sender, EventArgs e)
    {
        anfitrion.Registrar(ventanaExplorador, Zona.Derecha);
        anfitrion.Registrar(ventanaSalida, Zona.Abajo);
        anfitrion.Registrar(ventanaErrores, Zona.Izquierda);
    }
}
