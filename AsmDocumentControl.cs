using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using AsmEditor.Core;

namespace AsmEditor;

/// <summary>
/// Un documento .asm abierto: el editor, sus números de línea, su historial de
/// deshacer/rehacer, su resaltado y su autocompletado. Cada pestaña del editor
/// tiene una instancia de este control, con su propio estado independiente.
/// </summary>
public class AsmDocumentControl : UserControl, IPestanaEditor
{
    private readonly ScrollAwareRichTextBox _editor = new();
    private readonly RichTextBox _lineNumbers = new();
    private readonly CompletionPopup _completion = new();

    private readonly System.Windows.Forms.Timer _highlightTimer = new() { Interval = 300 };
    private readonly System.Windows.Forms.Timer _undoSnapshotTimer = new() { Interval = 600 };

    private int _cachedLineCount = -1;

    // ---- Undo/Redo propio: evita que el resaltado de sintaxis (SelectionColor)
    // contamine el undo nativo de RichTextBox. ----
    private readonly Stack<(string Text, int Caret)> _undoStack = new();
    private readonly Stack<(string Text, int Caret)> _redoStack = new();
    private string _undoBaseline = "";
    private int _undoBaselineCaret;
    private bool _isApplyingUndoRedo;

    // El resaltado usa SelectionColor, que dispara TextChanged aunque el texto no cambie.
    // Sin esta guarda, TextChanged rearma el timer de resaltado y se entra en un bucle
    // infinito que además marca el documento como sucio solo.
    private bool _isHighlighting;

    // Las etiquetas del archivo se recalculan al pausar el tipeo, no en cada tecla.
    private List<string> _fileLabels = new();

    private const int EM_LINESCROLL = 0x00B6;
    private const int EM_GETFIRSTVISIBLELINE = 0x00CE;

    [DllImport("user32.dll")]
    private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

    /// <summary>Estado lógico del documento (ruta, si está sucio, nombre de pestaña).</summary>
    public DocumentState State { get; }

    /// <summary>Se dispara cuando cambia el estado sucio o la ruta, para refrescar la pestaña.</summary>
    public event EventHandler? DirtyChanged;

    /// <summary>Se dispara al mover el caret, para actualizar la barra de estado.</summary>
    public event EventHandler? CaretMoved;

    public RichTextBox Editor => _editor;

    public string? FilePath
    {
        get => State.FilePath;
        private set => State.FilePath = value;
    }

    public bool IsDirty => State.IsDirty;

    public AsmDocumentControl(string? filePath = null)
    {
        State = new DocumentState(filePath);

        BuildLayout();
        WireEvents();
    }

    // ---------------------------------------------------------------
    // UI
    // ---------------------------------------------------------------

    private void BuildLayout()
    {
        Dock = DockStyle.Fill;

        _editor.Dock = DockStyle.Fill;
        _editor.Font = Tema.Codigo;
        _editor.AcceptsTab = true;
        _editor.WordWrap = false;
        _editor.BackColor = Tema.CodigoFondo;
        _editor.ForeColor = Tema.CodigoTexto;
        _editor.BorderStyle = BorderStyle.None;
        _editor.HideSelection = false;

        _lineNumbers.Dock = DockStyle.Left;
        _lineNumbers.Width = 55;
        _lineNumbers.Font = _editor.Font;
        _lineNumbers.ReadOnly = true;
        _lineNumbers.WordWrap = false;
        _lineNumbers.ScrollBars = RichTextBoxScrollBars.None;
        _lineNumbers.BorderStyle = BorderStyle.None;
        _lineNumbers.BackColor = Tema.CodigoMargen;
        _lineNumbers.ForeColor = Tema.CodigoNumeroLinea;
        _lineNumbers.TabStop = false;
        _lineNumbers.Cursor = Cursors.Default;
        _lineNumbers.Click += (_, _) => _editor.Focus();

        Controls.Add(_editor);       // Fill, agregado primero
        Controls.Add(_lineNumbers);  // Left, agregado después -> toma prioridad de layout

        // El popup vive sobre el editor, encima de todo.
        _completion.Parent = _editor;
        _completion.Visible = false;

        Tema.TemaCambiado += AplicarTema;
    }

    private void WireEvents()
    {
        _editor.TextChanged += OnEditorTextChanged;
        _editor.ContentScrolled += (_, _) => { SyncLineNumberScroll(); _completion.HidePopup(); };
        _editor.Resize += (_, _) => SyncLineNumberScroll();
        _editor.KeyDown += OnEditorKeyDown;
        _editor.KeyUp += OnEditorKeyUp;
        _editor.SelectionChanged += (_, _) => CaretMoved?.Invoke(this, EventArgs.Empty);
        _editor.MouseDown += (_, _) => _completion.HidePopup();
        _editor.LostFocus += (_, _) => _completion.HidePopup();

        _highlightTimer.Tick += (_, _) =>
        {
            _highlightTimer.Stop();
            ApplyHighlight();
        };

        _undoSnapshotTimer.Tick += (_, _) => FlushUndoSnapshot();

        _completion.Click += (_, _) => AcceptCompletion();
        _completion.DoubleClick += (_, _) => AcceptCompletion();
    }

    private void OnEditorTextChanged(object? sender, EventArgs e)
    {
        // El propio resaltado dispara TextChanged al pintar: ignorarlo, o el timer
        // se rearma solo y el documento queda marcado como sucio sin que nadie escriba.
        if (_isHighlighting) return;

        MarkDirty(true);
        RebuildLineNumbersIfNeeded();

        _highlightTimer.Stop();
        _highlightTimer.Start();

        if (!_isApplyingUndoRedo)
        {
            _undoSnapshotTimer.Stop();
            _undoSnapshotTimer.Start();
        }
    }

    /// <summary>
    /// Aplica el resaltado y recalcula las etiquetas del archivo, sin que el pintado
    /// se interprete como una edición del usuario.
    /// </summary>
    /// <summary>
    /// Vuelve a pintar el documento con la paleta activa. El resaltado se
    /// rehace completo porque los colores del código los pone SelectionColor
    /// carácter por carácter: cambiar el tema no los actualiza solo.
    /// </summary>
    private void AplicarTema()
    {
        _editor.BackColor = Tema.CodigoFondo;
        _editor.ForeColor = Tema.CodigoTexto;
        _lineNumbers.BackColor = Tema.CodigoMargen;
        _lineNumbers.ForeColor = Tema.CodigoNumeroLinea;

        ApplyHighlight();
    }

    private void ApplyHighlight()
    {
        if (_isHighlighting) return;

        _isHighlighting = true;
        try
        {
            AsmSyntaxHighlighter.Highlight(_editor);
            _fileLabels = AsmCompletion.ExtractLabels(_editor.Text);
        }
        finally
        {
            _isHighlighting = false;
        }
    }

    private void MarkDirty(bool dirty)
    {
        if (State.IsDirty == dirty) return;
        State.IsDirty = dirty;
        DirtyChanged?.Invoke(this, EventArgs.Empty);
    }

    // ---------------------------------------------------------------
    // Números de línea
    // ---------------------------------------------------------------

    private void RebuildLineNumbersIfNeeded()
    {
        int lineCount = Math.Max(1, _editor.Lines.Length);
        if (lineCount == _cachedLineCount) return;
        _cachedLineCount = lineCount;

        var sb = new StringBuilder();
        for (int i = 1; i <= lineCount; i++)
        {
            if (i > 1) sb.Append('\n');
            sb.Append(i);
        }

        _lineNumbers.Text = sb.ToString();
        _lineNumbers.SelectAll();
        _lineNumbers.SelectionAlignment = HorizontalAlignment.Right;
        _lineNumbers.DeselectAll();

        SyncLineNumberScroll();
    }

    private void SyncLineNumberScroll()
    {
        if (!_editor.IsHandleCreated || !_lineNumbers.IsHandleCreated) return;

        int editorFirstVisible = (int)SendMessage(_editor.Handle, EM_GETFIRSTVISIBLELINE, IntPtr.Zero, IntPtr.Zero);
        int numbersFirstVisible = (int)SendMessage(_lineNumbers.Handle, EM_GETFIRSTVISIBLELINE, IntPtr.Zero, IntPtr.Zero);
        int delta = editorFirstVisible - numbersFirstVisible;
        if (delta != 0)
        {
            SendMessage(_lineNumbers.Handle, EM_LINESCROLL, IntPtr.Zero, delta);
        }
    }

    // ---------------------------------------------------------------
    // Teclado
    // ---------------------------------------------------------------

    private void OnEditorKeyDown(object? sender, KeyEventArgs e)
    {
        // El popup tiene prioridad sobre todo lo demás mientras está visible.
        if (_completion.Visible && HandleCompletionKey(e)) return;

        if (e.Control && e.KeyCode == Keys.Z)
        {
            Undo();
            e.SuppressKeyPress = true;
            e.Handled = true;
            return;
        }
        if (e.Control && e.KeyCode == Keys.Y)
        {
            Redo();
            e.SuppressKeyPress = true;
            e.Handled = true;
            return;
        }

        // Ctrl+Espacio fuerza el autocompletado, como en cualquier IDE.
        if (e.Control && e.KeyCode == Keys.Space)
        {
            e.SuppressKeyPress = true;
            e.Handled = true;
            TryShowCompletion(forced: true);
            return;
        }

        if (e.KeyCode == Keys.Enter && !e.Shift)
        {
            e.SuppressKeyPress = true;
            InsertAutoIndentedNewline();
            return;
        }

        if (e.KeyCode == Keys.Escape)
        {
            _completion.HidePopup();
        }
    }

    /// <summary>
    /// Teclas que consume el popup cuando está abierto. Devuelve true si se consumió.
    /// </summary>
    private bool HandleCompletionKey(KeyEventArgs e)
    {
        switch (e.KeyCode)
        {
            case Keys.Down:
                _completion.MoveSelection(1);
                break;
            case Keys.Up:
                _completion.MoveSelection(-1);
                break;
            case Keys.PageDown:
                _completion.MovePage(1);
                break;
            case Keys.PageUp:
                _completion.MovePage(-1);
                break;
            case Keys.Enter:
            case Keys.Tab:
                AcceptCompletion();
                break;
            case Keys.Escape:
                _completion.HidePopup();
                break;
            default:
                return false;
        }

        e.SuppressKeyPress = true;
        e.Handled = true;
        return true;
    }

    /// <summary>
    /// Después de que la tecla ya modificó el texto, decide si abrir, actualizar o cerrar
    /// el popup. Va en KeyUp (no en KeyDown) para que el prefijo incluya el carácter tipeado.
    /// </summary>
    private void OnEditorKeyUp(object? sender, KeyEventArgs e)
    {
        // Las teclas que el popup ya consumió, y los modificadores, no vuelven a disparar nada.
        if (e.Control || e.Alt) return;

        switch (e.KeyCode)
        {
            case Keys.Escape:
            case Keys.Enter:
            case Keys.Tab:
            case Keys.Up:
            case Keys.Down:
            case Keys.PageUp:
            case Keys.PageDown:
            case Keys.Home:
            case Keys.End:
            case Keys.Left:
            case Keys.Right:
                return;

            // El separador cierra la palabra: el popup ya no aplica.
            case Keys.Space:
            case Keys.Oemcomma:
                _completion.HidePopup();
                return;
        }

        bool isTyping = char.IsLetterOrDigit((char)e.KeyValue)
                        || e.KeyCode == Keys.Back
                        || e.KeyCode == Keys.OemPeriod
                        || e.KeyCode == Keys.Decimal
                        || e.KeyCode == Keys.OemMinus
                        || e.KeyCode == Keys.D5    // '%' con Shift
                        || _completion.Visible;

        if (isTyping) TryShowCompletion();
    }

    /// <summary>
    /// Después de que el carácter ya entró en el editor, decide si abrir/actualizar el popup.
    /// Se llama desde el KeyUp para que el prefijo incluya la tecla recién tipeada.
    /// </summary>
    private void TryShowCompletion(bool forced = false)
    {
        var (start, prefix) = AsmCompletion.GetWordBeforeCaret(_editor.Text, _editor.SelectionStart);

        if (prefix.Length == 0 || (!forced && prefix.Length < CompletionPopup.MinPrefixLength))
        {
            _completion.HidePopup();
            return;
        }

        var items = AsmCompletion.Suggest(prefix, _fileLabels);

        // Si lo único que hay es exactamente lo ya tipeado, no vale la pena estorbar.
        if (items.Count == 1 && string.Equals(items[0].Text, prefix, StringComparison.OrdinalIgnoreCase))
        {
            _completion.HidePopup();
            return;
        }

        _completion.ShowSuggestions(items, start, CaretPopupLocation(), _editor);
    }

    /// <summary>Punto donde dibujar el popup: justo debajo del caret.</summary>
    private Point CaretPopupLocation()
    {
        var p = _editor.GetPositionFromCharIndex(_editor.SelectionStart);
        int lineHeight = (int)Math.Ceiling(_editor.Font.GetHeight()) + 2;
        return new Point(p.X, p.Y + lineHeight);
    }

    private void AcceptCompletion()
    {
        var item = _completion.Current;
        if (item is null)
        {
            _completion.HidePopup();
            return;
        }

        int start = _completion.WordStart;
        int caret = _editor.SelectionStart;
        int length = Math.Max(0, caret - start);

        _completion.HidePopup();

        _editor.Select(start, length);
        _editor.SelectedText = item.Text;
        _editor.SelectionStart = start + item.Text.Length;
        _editor.SelectionLength = 0;
        _editor.Focus();
    }

    private void InsertAutoIndentedNewline()
    {
        int caret = _editor.SelectionStart;
        int lineIndex = _editor.GetLineFromCharIndex(caret);
        string currentLine = lineIndex < _editor.Lines.Length ? _editor.Lines[lineIndex] : "";
        string indent = Regex.Match(currentLine, "^[ \t]*").Value;

        _editor.SelectedText = "\n" + indent;
    }

    // ---------------------------------------------------------------
    // Undo/Redo propio
    // ---------------------------------------------------------------

    private void FlushUndoSnapshot()
    {
        _undoSnapshotTimer.Stop();
        if (_undoBaseline == _editor.Text) return;

        _undoStack.Push((_undoBaseline, _undoBaselineCaret));
        _redoStack.Clear();
        _undoBaseline = _editor.Text;
        _undoBaselineCaret = _editor.SelectionStart;
    }

    public void Undo()
    {
        FlushUndoSnapshot();
        if (_undoStack.Count == 0) return;

        var current = (_editor.Text, _editor.SelectionStart);
        var prev = _undoStack.Pop();
        _redoStack.Push(current);
        ApplyState(prev);
    }

    public void Redo()
    {
        if (_redoStack.Count == 0) return;

        var current = (_editor.Text, _editor.SelectionStart);
        var next = _redoStack.Pop();
        _undoStack.Push(current);
        ApplyState(next);
    }

    private void ApplyState((string Text, int Caret) state)
    {
        _isApplyingUndoRedo = true;
        _completion.HidePopup();
        _editor.Text = state.Text;
        _editor.SelectionStart = Math.Min(state.Caret, _editor.TextLength);
        _editor.SelectionLength = 0;
        _undoBaseline = state.Text;
        _undoBaselineCaret = _editor.SelectionStart;
        _highlightTimer.Stop();
        ApplyHighlight();
        _isApplyingUndoRedo = false;
    }

    private void ResetUndoHistory()
    {
        _undoStack.Clear();
        _redoStack.Clear();
        _undoBaseline = _editor.Text;
        _undoBaselineCaret = _editor.SelectionStart;
    }

    // ---------------------------------------------------------------
    // Contenido y archivo
    // ---------------------------------------------------------------

    /// <summary>Carga texto en el editor y lo deja como estado limpio de partida.</summary>
    public void LoadContent(string text, string? filePath, bool markClean = true)
    {
        _editor.Text = text;
        FilePath = filePath;
        _cachedLineCount = -1;

        _highlightTimer.Stop();
        ApplyHighlight();
        RebuildLineNumbersIfNeeded();
        ResetUndoHistory();

        // El resaltado recorre el texto con Select(), lo que arrastra la vista hasta la
        // última coincidencia. Sin el ScrollToCaret el archivo abre por el medio.
        _editor.SelectionStart = 0;
        _editor.SelectionLength = 0;
        _editor.ScrollToCaret();
        SyncLineNumberScroll();

        // El flag se limpia al final: cualquier paso anterior pudo disparar TextChanged.
        if (markClean)
        {
            State.IsDirty = false;
            DirtyChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public bool Save()
    {
        if (FilePath is null) return false;

        File.WriteAllText(FilePath, _editor.Text);
        MarkDirty(false);
        return true;
    }

    public void SaveAs(string path)
    {
        FilePath = path;
        File.WriteAllText(path, _editor.Text);
        State.IsDirty = false;
        DirtyChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>Lleva el caret y la vista a una línea (base 1, como reporta NASM).</summary>
    public void GoToLine(int lineNumber1Based)
    {
        int target = lineNumber1Based - 1;
        if (target < 0 || target >= _editor.Lines.Length) return;

        int charIndex = _editor.GetFirstCharIndexFromLine(target);
        int lineLength = _editor.Lines[target].Length;
        _editor.Select(charIndex, lineLength);
        _editor.ScrollToCaret();
        _editor.Focus();
    }

    /// <summary>Línea y columna del caret, base 1, para la barra de estado.</summary>
    public (int Line, int Column) CaretPosition
    {
        get
        {
            int caret = _editor.SelectionStart;
            int line = _editor.GetLineFromCharIndex(caret);
            int lineStart = _editor.GetFirstCharIndexFromLine(line);
            return (line + 1, caret - lineStart + 1);
        }
    }

    public void FocusEditor() => _editor.Focus();

    protected override void OnLoad(EventArgs e)
    {
        base.OnLoad(e);
        RebuildLineNumbersIfNeeded();
        SyncLineNumberScroll();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            Tema.TemaCambiado -= AplicarTema;
            _highlightTimer.Dispose();
            _undoSnapshotTimer.Dispose();
            _completion.Dispose();
        }
        base.Dispose(disposing);
    }

    // ------------------------------------------------------------------
    // IPestanaEditor
    //
    // Casi todo ya existía con otro nombre: acá solo se lo expone con el de la
    // interfaz, para que MainForm trate igual a una pestaña de código y a una
    // de diseño.
    // ------------------------------------------------------------------

    bool IPestanaEditor.AdmiteBusqueda => true;
    bool IPestanaEditor.AdmiteDeshacer => true;
    bool IPestanaEditor.AdmiteIrALinea => true;

    (int Linea, int Columna)? IPestanaEditor.PosicionCursor => CaretPosition;

    bool IPestanaEditor.Guardar() => Save();

    void IPestanaEditor.TomarFoco() => FocusEditor();

    void IPestanaEditor.Deshacer() => Undo();

    void IPestanaEditor.Rehacer() => Redo();

    /// <summary>
    /// Pide la ruta y guarda ahí. El diálogo vive acá y no en MainForm para que
    /// cada tipo de pestaña ofrezca el filtro que le corresponde: un documento
    /// de código guarda .asm, y un formulario guarda .asmform.
    /// </summary>
    bool IPestanaEditor.GuardarComo(IWin32Window duenio)
    {
        using var dlg = new SaveFileDialog
        {
            Filter = "Archivos ASM (*.asm)|*.asm|Includes (*.inc)|*.inc|Todos los archivos (*.*)|*.*",
            FileName = FilePath is null ? State.DisplayName.TrimEnd('*', ' ') : Path.GetFileName(FilePath),
            InitialDirectory = FilePath is null ? null : Path.GetDirectoryName(FilePath)
        };

        if (dlg.ShowDialog(duenio) != DialogResult.OK) return false;

        SaveAs(dlg.FileName);
        return true;
    }
}
