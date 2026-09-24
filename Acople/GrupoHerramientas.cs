namespace AsmEditor;

/// <summary>
/// Los paneles de UNA zona del acople: muestra el activo, y abajo las
/// pestañas para pasar a los otros (solo si hay más de uno), como en Visual
/// Studio.
///
/// No decide cuáles ni cuál: se lo dice el AnfitrionAcople desde el modelo
/// (<see cref="Mostrar"/>), y avisa el clic en una pestaña
/// (<see cref="PestanaElegida"/>).
///
/// ⚠ Usa Dock (el panel de ventanas y la tira): es parte de la excepción del
/// acople a la regla del diseñador, documentada en AnfitrionAcople.
/// </summary>
public partial class GrupoHerramientas : UserControl
{
    private List<VentanaHerramienta> _ventanas = new();

    /// <summary>Se eligió una pestaña: ese panel pasa a ser el activo.</summary>
    public event EventHandler<VentanaHerramienta>? PestanaElegida;

    public GrupoHerramientas()
    {
        InitializeComponent();
        tira.Visible = false;
    }

    /// <summary>Los paneles que tiene ahora, en el orden de sus pestañas.</summary>
    public IReadOnlyList<VentanaHerramienta> Ventanas => _ventanas;

    /// <summary>
    /// Pone estos paneles (en este orden) y deja visible el activo. Los que
    /// tenía y ya no van se sacan (siguen vivos: se ocultaron o se movieron).
    /// </summary>
    public void Mostrar(IReadOnlyList<VentanaHerramienta> ventanas, VentanaHerramienta? activa)
    {
        SuspendLayout();

        foreach (var sobra in pnlVentanas.Controls.OfType<VentanaHerramienta>().Where(v => !ventanas.Contains(v)).ToList())
            pnlVentanas.Controls.Remove(sobra);

        foreach (var v in ventanas)
        {
            if (v.Parent != pnlVentanas)
            {
                v.Dock = DockStyle.Fill;
                pnlVentanas.Controls.Add(v);
            }
            v.Visible = ReferenceEquals(v, activa);
        }

        _ventanas = ventanas.ToList();

        tira.Visible = _ventanas.Count > 1;
        tira.Mostrar(_ventanas.Select(v => v.Titulo).ToList(), activa is null ? -1 : _ventanas.IndexOf(activa));

        ResumeLayout();
    }

    private void tira_PestanaElegida(object? sender, int indice)
    {
        if (indice >= 0 && indice < _ventanas.Count) PestanaElegida?.Invoke(this, _ventanas[indice]);
    }
}
