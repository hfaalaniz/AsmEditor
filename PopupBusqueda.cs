using AsmEditor.Core;

namespace AsmEditor;

/// <summary>
/// La lista de resultados del buscador de comandos de la barra de título.
///
/// ⚠ NO SE ACTIVA NUNCA. Si tomara el foco al aparecer, lo siguiente que el
/// usuario tipea iría a parar a la lista en vez de al campo de búsqueda, y la
/// búsqueda se cortaría en la primera letra. El foco se queda en el campo; las
/// flechas y el Enter se los reenvía la barra (<see cref="Mover"/>,
/// <see cref="Seleccionado"/>), igual que hace el autocompletado del editor.
///
/// Es una ventana aparte y no una lista hija porque la barra mide 32 px: una
/// lista adentro quedaría recortada.
/// </summary>
public partial class PopupBusqueda : Form
{
    private const int WS_EX_TOOLWINDOW = 0x00000080;
    private const int WS_EX_NOACTIVATE = 0x08000000;

    /// <summary>Filas que se ven como máximo.</summary>
    private const int FilasVisibles = 12;

    /// <summary>Se eligió un comando con el ratón.</summary>
    public event Action<ComandoMenu>? Elegido;

    public PopupBusqueda()
    {
        InitializeComponent();
        AplicarTema();
        Tema.TemaCambiado += AplicarTema;
        Disposed += PopupBusqueda_Disposed;
    }

    protected override bool ShowWithoutActivation => true;

    protected override CreateParams CreateParams
    {
        get
        {
            var cp = base.CreateParams;
            // Sin activarse al hacer clic, y fuera de Alt+Tab.
            cp.ExStyle |= WS_EX_NOACTIVATE | WS_EX_TOOLWINDOW;
            return cp;
        }
    }

    /// <summary>El comando marcado, o null si no hay ninguno (o no hubo coincidencias).</summary>
    public ComandoMenu? Seleccionado => lstResultados.SelectedItem as ComandoMenu;

    /// <summary>
    /// Muestra los resultados debajo del rectángulo dado (en pantalla).
    /// Sin resultados muestra una fila que lo dice, para que no parezca que el
    /// buscador no hizo nada.
    /// </summary>
    public void Mostrar(IReadOnlyList<ComandoMenu> resultados, Rectangle debajoDe, Form duenio)
    {
        lstResultados.BeginUpdate();
        lstResultados.Items.Clear();

        if (resultados.Count == 0) lstResultados.Items.Add("Sin coincidencias");
        else foreach (var r in resultados) lstResultados.Items.Add(r);

        lstResultados.EndUpdate();
        lstResultados.SelectedIndex = resultados.Count > 0 ? 0 : -1;

        int filas = Math.Min(Math.Max(1, lstResultados.Items.Count), FilasVisibles);
        ClientSize = new Size(Math.Max(debajoDe.Width, 400), filas * lstResultados.ItemHeight + 2);
        Location = new Point(debajoDe.Left, debajoDe.Bottom + 2);

        if (!Visible)
        {
            Owner = duenio;
            Show(duenio);
        }
    }

    public void Ocultar()
    {
        if (Visible) Hide();
    }

    /// <summary>Mueve la marca hacia abajo (+1) o arriba (-1).</summary>
    public void Mover(int delta)
    {
        if (!Visible || Seleccionado is null) return;

        int i = lstResultados.SelectedIndex + delta;
        lstResultados.SelectedIndex = Math.Clamp(i, 0, lstResultados.Items.Count - 1);
    }

    private void AplicarTema()
    {
        // El fondo del formulario asoma 1 px alrededor de la lista: es el borde.
        BackColor = Tema.LineaSuave;
        lstResultados.BackColor = Tema.Superficie2;
        lstResultados.ForeColor = Tema.Texto;
        lstResultados.Font = Tema.Cuerpo;
        lstResultados.Invalidate();
    }

    private void lstResultados_DrawItem(object? sender, DrawItemEventArgs e)
    {
        if (e.Index < 0) return;

        var item = lstResultados.Items[e.Index];
        bool marcado = (e.State & DrawItemState.Selected) != 0 && item is ComandoMenu;

        using (var fondo = new SolidBrush(marcado ? Tema.Seleccion : Tema.Superficie2))
        {
            e.Graphics.FillRectangle(fondo, e.Bounds);
        }

        var caja = new Rectangle(e.Bounds.X + 10, e.Bounds.Y, e.Bounds.Width - 20, e.Bounds.Height);
        var izquierda = TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis;
        var derecha = TextFormatFlags.Right | TextFormatFlags.VerticalCenter;

        if (item is ComandoMenu c)
        {
            TextRenderer.DrawText(e.Graphics, c.Ruta, lstResultados.Font, caja, Tema.Texto, izquierda);
            if (c.Atajo.Length > 0)
            {
                TextRenderer.DrawText(e.Graphics, c.Atajo, lstResultados.Font, caja, Tema.Texto3, derecha);
            }
        }
        else
        {
            TextRenderer.DrawText(e.Graphics, item.ToString(), lstResultados.Font, caja, Tema.Texto3, izquierda);
        }
    }

    /// <summary>
    /// Se ejecuta al APRETAR el botón y no al soltarlo: después del clic el
    /// campo de búsqueda pierde el foco y la barra cierra esta lista, y un
    /// MouseUp llegaría tarde.
    /// </summary>
    private void lstResultados_MouseDown(object? sender, MouseEventArgs e)
    {
        int i = lstResultados.IndexFromPoint(e.Location);
        if (i < 0 || lstResultados.Items[i] is not ComandoMenu c) return;

        Elegido?.Invoke(c);
    }

    /// <summary>La marca sigue al ratón, como en cualquier lista desplegable.</summary>
    private void lstResultados_MouseMove(object? sender, MouseEventArgs e)
    {
        int i = lstResultados.IndexFromPoint(e.Location);
        if (i >= 0 && i != lstResultados.SelectedIndex && lstResultados.Items[i] is ComandoMenu)
        {
            lstResultados.SelectedIndex = i;
        }
    }

    private void PopupBusqueda_Disposed(object? sender, EventArgs e) => Tema.TemaCambiado -= AplicarTema;
}
