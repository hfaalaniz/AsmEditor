using AsmEditor.Core;

namespace AsmEditor;

/// <summary>
/// Configuración → Opciones, como la de Visual Studio: categorías a la
/// izquierda y la página elegida a la derecha.
///
/// Recibe una COPIA de las opciones (<see cref="ValoresOpciones"/>): cada página
/// carga de ella al abrir y guarda en ella al aceptar. Esta ventana no toca la
/// configuración; quien la abre aplica <see cref="Valores"/> solo si devuelve
/// OK. Así Cancelar (o la X, o Esc) no cambia nada.
///
/// Las páginas son UserControls puestos en pnlPaginas desde el diseñador. El
/// árbol NO está en el diseñador: se arma de las páginas (Categoria, Titulo,
/// Orden), para que agregar una página sea solo ponerla en el panel. Ver
/// IPaginaOpciones.
/// </summary>
public partial class FormOpciones : Form
{
    private readonly List<IPaginaOpciones> _paginas;

    public ValoresOpciones Valores { get; }

    public FormOpciones(ValoresOpciones valores)
    {
        InitializeComponent();

        Valores = valores;
        Icon = LogoEditor.CrearIcono();

        _paginas = pnlPaginas.Controls.OfType<IPaginaOpciones>().OrderBy(p => p.Orden).ToList();
        foreach (var p in _paginas) p.Cargar(Valores);

        EstiloOpciones.Aplicar(this);
        ArmarArbol();
    }

    /// <summary>
    /// Un nodo por categoría, en el orden de su primera página, y adentro un
    /// nodo por página. Los nodos de página llevan la página en Tag.
    /// </summary>
    private void ArmarArbol()
    {
        tvCategorias.BeginUpdate();
        tvCategorias.Nodes.Clear();

        foreach (var grupo in _paginas.GroupBy(p => p.Categoria))
        {
            var categoria = tvCategorias.Nodes.Add(grupo.Key);
            foreach (var p in grupo) categoria.Nodes.Add(new TreeNode(p.Titulo) { Tag = p });
        }

        tvCategorias.ExpandAll();
        tvCategorias.EndUpdate();

        if (tvCategorias.Nodes.Count > 0 && tvCategorias.Nodes[0].Nodes.Count > 0)
            tvCategorias.SelectedNode = tvCategorias.Nodes[0].Nodes[0];
    }

    private void tvCategorias_AfterSelect(object? sender, TreeViewEventArgs e)
    {
        if (e.Node?.Tag is IPaginaOpciones pagina)
        {
            Mostrar(pagina);
        }
        else if (e.Node?.Nodes.Count > 0 && e.Node.Nodes[0].Tag is IPaginaOpciones primera)
        {
            // Una categoría no tiene página propia: muestra la primera suya.
            // ⚠ SIN MOVER LA SELECCIÓN a ese hijo: con las flechas, subir desde
            // él volvería a caer en la categoría y rebotaría hacia abajo, y no
            // se podría pasar a la categoría anterior.
            Mostrar(primera);
        }
    }

    private void Mostrar(IPaginaOpciones pagina)
    {
        foreach (var p in _paginas) ((Control)p).Visible = ReferenceEquals(p, pagina);
        lblTituloPagina.Text = pagina.Titulo;
    }

    private void btnAceptar_Click(object? sender, EventArgs e)
    {
        foreach (var p in _paginas) p.Guardar(Valores);
        DialogResult = DialogResult.OK;
    }

    /// <summary>Barra de título oscura en el tema oscuro (ver MarcoOscuro).</summary>
    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        MarcoOscuro.Aplicar(Handle);
    }
}
