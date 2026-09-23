using AsmEditor.Core;

namespace AsmEditor;

/// <summary>
/// Lista flotante de sugerencias que se dibuja sobre el editor.
/// No toma el foco nunca: el usuario sigue tipeando en el RichTextBox y esta lista
/// solo reacciona a las teclas que el editor le reenvía.
/// </summary>
public class CompletionPopup : ListBox
{
    /// <summary>Cantidad mínima de caracteres tipeados para que aparezca.</summary>
    public const int MinPrefixLength = 2;

    private const int MaxVisibleItems = 9;

    /// <summary>Posición en el texto donde empieza la palabra que se está completando.</summary>
    public int WordStart { get; private set; }

    public CompletionPopup()
    {
        Visible = false;
        TabStop = false;
        BorderStyle = BorderStyle.FixedSingle;
        Font = Tema.CodigoChico;
        BackColor = Tema.Superficie2;
        ForeColor = Tema.Texto;
        IntegralHeight = false;
        DrawMode = DrawMode.OwnerDrawFixed;
        ItemHeight = 18;
        DrawItem += OnDrawItem;
    }

    /// <summary>
    /// Muestra las sugerencias para el prefijo dado, ubicando la lista bajo el caret.
    /// Si no hay nada que sugerir, se oculta.
    /// </summary>
    public void ShowSuggestions(IReadOnlyList<CompletionItem> items, int wordStart, Point caretLocation, Control host)
    {
        if (items.Count == 0)
        {
            HidePopup();
            return;
        }

        WordStart = wordStart;

        BeginUpdate();
        Items.Clear();
        foreach (var item in items) Items.Add(item);
        EndUpdate();

        SelectedIndex = 0;

        int visible = Math.Min(items.Count, MaxVisibleItems);
        Height = visible * ItemHeight + 2;
        Width = MeasureWidth(items);

        Location = ClampToHost(caretLocation, host);

        if (!Visible)
        {
            Visible = true;
            BringToFront();
        }
    }

    /// <summary>Ancho suficiente para el texto más largo, más el espacio de la etiqueta de tipo.</summary>
    private int MeasureWidth(IReadOnlyList<CompletionItem> items)
    {
        int widest = 0;
        using var g = CreateGraphics();
        foreach (var item in items)
        {
            int w = (int)g.MeasureString(item.Text, Font).Width;
            if (w > widest) widest = w;
        }
        return Math.Max(160, Math.Min(widest + 80, 420));
    }

    /// <summary>Evita que la lista se dibuje fuera del área visible del editor.</summary>
    private Point ClampToHost(Point desired, Control host)
    {
        int x = desired.X;
        int y = desired.Y;

        if (x + Width > host.ClientSize.Width) x = Math.Max(0, host.ClientSize.Width - Width);

        // Si no cabe abajo, la dibujamos por encima de la línea del caret.
        if (y + Height > host.ClientSize.Height)
        {
            int above = desired.Y - Height - 18;
            y = above >= 0 ? above : Math.Max(0, host.ClientSize.Height - Height);
        }

        return new Point(x, y);
    }

    public void HidePopup()
    {
        if (Visible) Visible = false;
        Items.Clear();
    }

    public CompletionItem? Current => SelectedItem as CompletionItem;

    public void MoveSelection(int delta)
    {
        if (Items.Count == 0) return;
        int next = SelectedIndex + delta;
        SelectedIndex = Math.Clamp(next, 0, Items.Count - 1);
    }

    public void MovePage(int direction)
    {
        MoveSelection(direction * MaxVisibleItems);
    }

    private void OnDrawItem(object? sender, DrawItemEventArgs e)
    {
        if (e.Index < 0 || e.Index >= Items.Count) return;

        var item = (CompletionItem)Items[e.Index]!;
        bool selected = (e.State & DrawItemState.Selected) == DrawItemState.Selected;

        var back = selected ? Tema.Seleccion : Tema.Superficie2;
        using (var brush = new SolidBrush(back)) e.Graphics.FillRectangle(brush, e.Bounds);

        var textColor = item.Kind switch
        {
            CompletionKind.Label => Tema.CodigoEtiqueta,
            CompletionKind.Register => Tema.CodigoRegistro,
            CompletionKind.Directive => Tema.CodigoDirectiva,
            _ => Tema.CodigoInstruccion
        };

        using (var brush = new SolidBrush(textColor))
        {
            e.Graphics.DrawString(item.Text, Font, brush, e.Bounds.Left + 4, e.Bounds.Top + 1);
        }

        using (var brush = new SolidBrush(Tema.Texto3))
        using (var fmt = new StringFormat { Alignment = StringAlignment.Far })
        {
            var rect = new RectangleF(e.Bounds.Left, e.Bounds.Top + 1, e.Bounds.Width - 6, e.Bounds.Height);
            e.Graphics.DrawString(item.KindLabel, Font, brush, rect, fmt);
        }
    }
}
