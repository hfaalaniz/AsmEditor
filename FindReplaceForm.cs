namespace AsmEditor;

public class FindReplaceForm : Form
{
    // No es readonly: cambia cuando el usuario cambia de pestaña.
    private RichTextBox _target;

    private readonly Label _lblFind = new() { Text = "Buscar:", AutoSize = true, Left = 15, Top = 18 };
    private readonly TextBox _txtFind = new() { Left = 100, Top = 15, Width = 220 };

    private readonly Label _lblReplace = new() { Text = "Reemplazar:", AutoSize = true, Left = 15, Top = 50 };
    private readonly TextBox _txtReplace = new() { Left = 100, Top = 47, Width = 220 };

    private readonly CheckBox _chkMatchCase = new()
    { Text = "Coincidir mayúsculas/minúsculas", AutoSize = true, Left = 15, Top = 80 };

    private readonly Button _btnFindNext = new() { Text = "Buscar siguiente", Left = 15, Top = 110, Width = 130 };
    private readonly Button _btnReplace = new() { Text = "Reemplazar", Left = 150, Top = 110, Width = 130 };
    private readonly Button _btnReplaceAll = new() { Text = "Reemplazar todo", Left = 15, Top = 140, Width = 130 };
    private readonly Button _btnClose = new() { Text = "Cerrar", Left = 150, Top = 140, Width = 130 };

    public FindReplaceForm(RichTextBox target)
    {
        _target = target;

        Text = "Buscar";
        FormBorderStyle = FormBorderStyle.FixedToolWindow;
        StartPosition = FormStartPosition.CenterParent;
        ShowInTaskbar = false;
        MaximizeBox = false;
        MinimizeBox = false;
        ClientSize = new Size(380, 175);

        _btnFindNext.Click += (_, _) => FindNext();
        _btnReplace.Click += (_, _) => ReplaceCurrent();
        _btnReplaceAll.Click += (_, _) => ReplaceAll();
        _btnClose.Click += (_, _) => Hide();

        Controls.AddRange(new Control[]
        {
            _lblFind, _txtFind, _lblReplace, _txtReplace, _chkMatchCase,
            _btnFindNext, _btnReplace, _btnReplaceAll, _btnClose
        });

        AcceptButton = _btnFindNext;

        // No destruimos la ventana al cerrar: la ocultamos, para reabrir instantáneo con Ctrl+F/Ctrl+H
        FormClosing += (_, e) =>
        {
            if (e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                Hide();
            }
        };
    }

    /// <summary>
    /// Apunta la búsqueda al editor de la pestaña activa. Se llama al cambiar de pestaña
    /// para que Buscar/Reemplazar no siga operando sobre un documento que ya no se ve.
    /// </summary>
    public void SetTarget(RichTextBox target)
    {
        _target = target;
    }

    public void SetReplaceVisible(bool visible)
    {
        _lblReplace.Visible = visible;
        _txtReplace.Visible = visible;
        _btnReplace.Visible = visible;
        _btnReplaceAll.Visible = visible;
        Text = visible ? "Reemplazar" : "Buscar";
        ClientSize = new Size(380, visible ? 175 : 110);
    }

    public void FocusFindBox()
    {
        _txtFind.Focus();
        _txtFind.SelectAll();
    }

    private RichTextBoxFinds BuildOptions() =>
        _chkMatchCase.Checked ? RichTextBoxFinds.MatchCase : RichTextBoxFinds.None;

    private void FindNext()
    {
        string term = _txtFind.Text;
        if (string.IsNullOrEmpty(term)) return;

        int start = _target.SelectionStart + _target.SelectionLength;
        int idx = _target.Find(term, start, BuildOptions());
        if (idx == -1)
        {
            idx = _target.Find(term, 0, BuildOptions()); // dio la vuelta completa
        }

        if (idx >= 0)
        {
            _target.Select(idx, term.Length);
            _target.ScrollToCaret();
        }
        else
        {
            Dialogo.Aviso(this, "Buscar", $"No se encontró '{term}'.");
        }
    }

    private void ReplaceCurrent()
    {
        string term = _txtFind.Text;
        if (string.IsNullOrEmpty(term)) return;

        bool matchCase = _chkMatchCase.Checked;
        string selected = _target.SelectedText;
        bool selectionMatches = matchCase
            ? selected == term
            : string.Equals(selected, term, StringComparison.OrdinalIgnoreCase);

        if (selectionMatches)
        {
            _target.SelectedText = _txtReplace.Text;
        }

        FindNext();
    }

    private void ReplaceAll()
    {
        string term = _txtFind.Text;
        string repl = _txtReplace.Text;
        if (string.IsNullOrEmpty(term)) return;

        var options = BuildOptions();
        int count = 0;
        int idx = 0;
        int originalCaret = _target.SelectionStart;

        while (true)
        {
            idx = _target.Find(term, idx, options);
            if (idx == -1) break;

            _target.Select(idx, term.Length);
            _target.SelectedText = repl;
            idx += repl.Length;
            count++;
        }

        _target.SelectionStart = Math.Min(originalCaret, _target.TextLength);
        _target.SelectionLength = 0;

        Dialogo.Aviso(this, "Reemplazar todo",
            count == 1 ? "1 reemplazo realizado." : $"{count} reemplazos realizados.");
    }
}
