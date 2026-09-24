using AsmEditor.Core;

namespace AsmEditor;

/// <summary>
/// Opciones → Herramientas → NASM y GoLink: las rutas del ensamblador y el
/// enlazador sueltos. Antes estaban en «Rutas de herramientas».
/// </summary>
public partial class PaginaOpcionesNasmGoLink : UserControl, IPaginaOpciones
{
    public PaginaOpcionesNasmGoLink()
    {
        InitializeComponent();
    }

    public string Categoria => "Herramientas";
    public string Titulo => "NASM y GoLink";
    public int Orden => 30;

    public void Cargar(ValoresOpciones valores)
    {
        txtNasm.Text = valores.RutaNasm;
        txtGoLink.Text = valores.RutaGoLink;
    }

    public void Guardar(ValoresOpciones valores)
    {
        valores.RutaNasm = txtNasm.Text.Trim();
        valores.RutaGoLink = txtGoLink.Text.Trim();
    }

    private void btnNasm_Click(object? sender, EventArgs e) => ElegirEjecutable(txtNasm);

    private void btnGoLink_Click(object? sender, EventArgs e) => ElegirEjecutable(txtGoLink);

    private void ElegirEjecutable(TextBox destino)
    {
        using var dlg = new OpenFileDialog { Filter = "Ejecutables (*.exe)|*.exe" };
        if (dlg.ShowDialog(this) == DialogResult.OK) destino.Text = dlg.FileName;
    }
}
