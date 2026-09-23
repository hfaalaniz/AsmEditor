using AsmEditor.Core;

namespace AsmEditor;

/// <summary>
/// Lista de errores y advertencias de la compilación, al estilo del "Error List"
/// de Visual Studio. Doble clic o Enter salta al lugar del código.
///
/// No hay columna "Columna": NASM 3.02 no reporta la posición horizontal
/// (su formato es "archivo:línea: nivel: mensaje"). Ese espacio lo ocupa el Nivel,
/// que sí es dato real y permite filtrar.
/// </summary>
public class ErrorListPanel : UserControl
{
    private readonly ListView _list = new();
    private readonly ToolStrip _toolbar = new();
    private readonly ToolStripButton _showErrors = new();
    private readonly ToolStripButton _showWarnings = new();
    private readonly ToolStripLabel _summary = new();

    private readonly List<BuildDiagnostic> _all = new();

    /// <summary>Se dispara al activar un diagnóstico que tiene ubicación.</summary>
    public event Action<BuildDiagnostic>? DiagnosticActivated;

    public ErrorListPanel()
    {
        BuildLayout();
        WireEvents();
        RefreshList();
        Tema.TemaCambiado += AplicarTema;
    }

    private void BuildLayout()
    {
        Dock = DockStyle.Fill;
        BackColor = Tema.Superficie;

        _showErrors.Text = "Errores";
        _showErrors.CheckOnClick = true;
        _showErrors.Checked = true;
        _showErrors.ForeColor = Tema.Texto;

        _showWarnings.Text = "Advertencias";
        _showWarnings.CheckOnClick = true;
        _showWarnings.Checked = true;
        _showWarnings.ForeColor = Tema.Texto;

        _summary.ForeColor = Tema.Texto2;
        _summary.Alignment = ToolStripItemAlignment.Right;

        _toolbar.Dock = DockStyle.Top;
        _toolbar.GripStyle = ToolStripGripStyle.Hidden;
        _toolbar.BackColor = Tema.Superficie2;
        _toolbar.Renderer = new ToolStripProfessionalRenderer(new ColoresBarra());
        _toolbar.Items.Add(_showErrors);
        _toolbar.Items.Add(_showWarnings);
        _toolbar.Items.Add(_summary);

        _list.Dock = DockStyle.Fill;
        _list.View = View.Details;
        _list.FullRowSelect = true;
        _list.MultiSelect = false;
        _list.GridLines = false;
        _list.HideSelection = false;
        _list.BorderStyle = BorderStyle.None;
        _list.BackColor = Tema.Superficie;
        _list.ForeColor = Tema.Texto;
        _list.Font = new Font("Segoe UI", 9f);

        _list.Columns.Add("Nivel", 90);
        _list.Columns.Add("Archivo", 150);
        _list.Columns.Add("Línea", 55, HorizontalAlignment.Right);
        _list.Columns.Add("Mensaje", 640);
        _list.Columns.Add("Herramienta", 90);

        Controls.Add(_list);
        Controls.Add(_toolbar);
    }

    private void WireEvents()
    {
        _list.DoubleClick += (_, _) => ActivateSelected();
        _list.KeyDown += (_, e) =>
        {
            if (e.KeyCode == Keys.Enter)
            {
                ActivateSelected();
                e.Handled = true;
            }
        };

        _showErrors.CheckedChanged += (_, _) => RefreshList();
        _showWarnings.CheckedChanged += (_, _) => RefreshList();
    }

    /// <summary>Vacía la lista. Se llama al empezar una compilación.</summary>
    public void Clear()
    {
        _all.Clear();
        RefreshList();
    }

    /// <summary>Agrega un diagnóstico recién parseado y refresca la vista.</summary>
    public void Add(BuildDiagnostic diagnostic)
    {
        _all.Add(diagnostic);
        RefreshList();
    }

    public void AddRange(IEnumerable<BuildDiagnostic> diagnostics)
    {
        _all.AddRange(diagnostics);
        RefreshList();
    }

    public int ErrorCount => _all.Count(d => d.Level is DiagnosticLevel.Error or DiagnosticLevel.Fatal);
    public int WarningCount => _all.Count(d => d.Level == DiagnosticLevel.Warning);

    /// <summary>Texto de resumen para la barra de estado.</summary>
    public string SummaryText
    {
        get
        {
            int e = ErrorCount, w = WarningCount;
            if (e == 0 && w == 0) return "Sin errores";

            var partes = new List<string>();
            if (e > 0) partes.Add(e == 1 ? "1 error" : $"{e} errores");
            if (w > 0) partes.Add(w == 1 ? "1 advertencia" : $"{w} advertencias");
            return string.Join(", ", partes);
        }
    }

    private void RefreshList()
    {
        _list.BeginUpdate();
        _list.Items.Clear();

        foreach (var d in _all)
        {
            if (!PassesFilter(d)) continue;

            var item = new ListViewItem(d.LevelText)
            {
                Tag = d,
                ForeColor = ColorFor(d.Level)
            };
            item.SubItems.Add(d.FileName is null ? "" : Path.GetFileName(d.FileName));
            item.SubItems.Add(d.Line?.ToString() ?? "");
            item.SubItems.Add(d.Message);
            item.SubItems.Add(d.ToolText);

            _list.Items.Add(item);
        }

        _list.EndUpdate();

        _summary.Text = SummaryText + "   ";
    }

    private bool PassesFilter(BuildDiagnostic d) => d.Level switch
    {
        DiagnosticLevel.Warning => _showWarnings.Checked,
        _ => _showErrors.Checked
    };

    private static Color ColorFor(DiagnosticLevel level) => level switch
    {
        DiagnosticLevel.Warning => Tema.Aviso,
        // El fatal va en el rojo pleno y el error normal un tono más suave:
        // un fatal detiene todo, un error se puede acumular con otros.
        DiagnosticLevel.Fatal => Tema.Critico,
        _ => Tema.Realzar(Tema.Critico, 26)
    };

    private void ActivateSelected()
    {
        if (_list.SelectedItems.Count == 0) return;
        if (_list.SelectedItems[0].Tag is not BuildDiagnostic d) return;
        if (!d.HasLocation) return;

        DiagnosticActivated?.Invoke(d);
    }

    /// <summary>Selecciona el primer error con ubicación, para saltar con una tecla.</summary>
    public BuildDiagnostic? FirstWithLocation =>
        _all.FirstOrDefault(d => d.HasLocation && d.Level is DiagnosticLevel.Error or DiagnosticLevel.Fatal)
        ?? _all.FirstOrDefault(d => d.HasLocation);

    /// <summary>Colores de la barra de herramientas del panel, tomados del tema.</summary>
    private sealed class ColoresBarra : ProfessionalColorTable
    {
        public override Color ToolStripGradientBegin => Tema.Superficie2;
        public override Color ToolStripGradientMiddle => Tema.Superficie2;
        public override Color ToolStripGradientEnd => Tema.Superficie2;
        public override Color ButtonCheckedHighlight => Tema.Seleccion;
        public override Color ButtonSelectedHighlight => Tema.Realzar(Tema.Superficie2, 14);
    }

    /// <summary>
    /// Repinta con la paleta activa. La lista se reconstruye porque el color de
    /// cada fila se fija al crear el ListViewItem.
    /// </summary>
    private void AplicarTema()
    {
        BackColor = Tema.Superficie;
        _toolbar.BackColor = Tema.Superficie2;
        _toolbar.Renderer = new ToolStripProfessionalRenderer(new ColoresBarra());
        _showErrors.ForeColor = Tema.Texto;
        _showWarnings.ForeColor = Tema.Texto;
        _summary.ForeColor = Tema.Texto2;
        _list.BackColor = Tema.Superficie;
        _list.ForeColor = Tema.Texto;

        RefreshList();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) Tema.TemaCambiado -= AplicarTema;
        base.Dispose(disposing);
    }
}
