using AsmEditor.Core;

namespace AsmEditor;

/// <summary>
/// Opciones → Entorno → Proyectos: la carpeta de trabajo
/// (BuildConfig.ProjectFolder). Antes estaba en «Rutas de herramientas».
/// </summary>
public partial class PaginaOpcionesProyectos : UserControl, IPaginaOpciones
{
    public PaginaOpcionesProyectos()
    {
        InitializeComponent();
    }

    public string Categoria => "Entorno";
    public string Titulo => "Proyectos";
    public int Orden => 20;

    public void Cargar(ValoresOpciones valores) => txtCarpeta.Text = valores.CarpetaProyecto;

    public void Guardar(ValoresOpciones valores) => valores.CarpetaProyecto = txtCarpeta.Text.Trim();

    private void btnCarpeta_Click(object? sender, EventArgs e)
    {
        using var dlg = new FolderBrowserDialog();
        if (Directory.Exists(txtCarpeta.Text)) dlg.SelectedPath = txtCarpeta.Text;
        if (dlg.ShowDialog(this) == DialogResult.OK) txtCarpeta.Text = dlg.SelectedPath;
    }
}
