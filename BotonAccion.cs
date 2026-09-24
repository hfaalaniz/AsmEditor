using System.ComponentModel;

namespace AsmEditor;

/// <summary>
/// Una acción de la columna derecha de la ventana de inicio ("Crear un
/// proyecto", "Abrir una carpeta"...): ícono y texto, que se resalta al pasar
/// el ratón, como en Visual Studio.
///
/// Un clic en cualquier parte (ícono, texto o fondo) dispara el Click del
/// control: quien lo usa se suscribe a Click como a un botón.
/// </summary>
public partial class BotonAccion : UserControl
{
    private Glifo _glifo = Glifo.Nuevo;
    private bool _encima;

    public BotonAccion()
    {
        InitializeComponent();
        AplicarTema();
        Tema.TemaCambiado += AplicarTema;
        Disposed += BotonAccion_Disposed;
    }

    [Category("Acción")]
    [Description("El texto de la acción.")]
    public string Texto
    {
        get => lblTexto.Text;
        set => lblTexto.Text = value;
    }

    [Category("Acción")]
    [Description("El ícono (los mismos glifos de la barra de herramientas).")]
    [DefaultValue(Glifo.Nuevo)]
    public Glifo Glifo
    {
        get => _glifo;
        set { _glifo = value; PonerIcono(); }
    }

    private void Parte_Click(object? sender, EventArgs e) => OnClick(e);

    // Entrar en el ícono o el texto dispara MouseLeave del control: se mira si
    // el cursor sigue adentro en vez de confiar en los eventos sueltos.
    private void Parte_MouseEnter(object? sender, EventArgs e) => Resaltar(true);

    private void Parte_MouseLeave(object? sender, EventArgs e) =>
        Resaltar(ClientRectangle.Contains(PointToClient(MousePosition)));

    private void Resaltar(bool encima)
    {
        if (_encima == encima) return;
        _encima = encima;
        AplicarTema();
    }

    private void AplicarTema()
    {
        BackColor = _encima ? Tema.Realzar(Tema.Superficie2, 14) : Tema.Superficie2;
        lblTexto.ForeColor = Tema.Texto;
        lblTexto.Font = Tema.Cuerpo;
        PonerIcono();
    }

    private void PonerIcono()
    {
        lblIcono.Image?.Dispose();
        lblIcono.Image = IconosBarra.Dibujar(_glifo, 18);
    }

    private void BotonAccion_Disposed(object? sender, EventArgs e) => Tema.TemaCambiado -= AplicarTema;
}
