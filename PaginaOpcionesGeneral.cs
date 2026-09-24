using AsmEditor.Core;

namespace AsmEditor;

/// <summary>
/// Opciones → Entorno → General: el tema y qué se abre al arrancar.
///
/// ⚠ EL ORDEN DE LOS COMBOS ES EL DE LOS ENUM: el índice elegido se convierte
/// directo a <see cref="ModoTemaGuardado"/> (Oscuro 0, Claro 1) y a
/// <see cref="AlIniciar"/> (Ventana de inicio 0, Último proyecto 1, Entorno
/// vacío 2). Si se agrega un valor al enum, va en el combo en la misma
/// posición.
/// </summary>
public partial class PaginaOpcionesGeneral : UserControl, IPaginaOpciones
{
    public PaginaOpcionesGeneral()
    {
        InitializeComponent();
    }

    public string Categoria => "Entorno";
    public string Titulo => "General";
    public int Orden => 10;

    public void Cargar(ValoresOpciones valores)
    {
        cmbTema.SelectedIndex = Indice(cmbTema, (int)valores.Tema);
        cmbAlIniciar.SelectedIndex = Indice(cmbAlIniciar, (int)valores.AlIniciar);
    }

    public void Guardar(ValoresOpciones valores)
    {
        if (cmbTema.SelectedIndex >= 0) valores.Tema = (ModoTemaGuardado)cmbTema.SelectedIndex;
        if (cmbAlIniciar.SelectedIndex >= 0) valores.AlIniciar = (AlIniciar)cmbAlIniciar.SelectedIndex;
    }

    /// <summary>Un valor fuera del combo (settings.json editado a mano) muestra el primero.</summary>
    private static int Indice(ComboBox combo, int valor) =>
        valor >= 0 && valor < combo.Items.Count ? valor : 0;
}
