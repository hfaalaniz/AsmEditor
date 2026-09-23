namespace AsmEditor;

/// <summary>
/// «GUARDAR COMO» PROPIO, con la paleta del editor.
///
/// ⚠ ES DELIBERADAMENTE SIMPLE, y esa fue la decisión: guardar es elegir una
/// carpeta y escribir un nombre. Un explorador de archivos completo —renombrar,
/// crear carpetas, vistas, unidades de red— es mucho trabajo y es fácil que
/// quede peor que el del sistema.
///
/// Para ABRIR no hay diálogo propio: está el explorador lateral, que es la vía
/// principal, y el OpenFileDialog del sistema como salida secundaria.
///
/// Lo que sí resuelve, porque es lo que hace falta:
///   - navegar carpetas hacia adentro y hacia arriba
///   - ver los .asm que ya están en la carpeta, para no pisar uno sin querer
///   - escribir el nombre, con la extensión puesta sola
///   - avisar antes de sobrescribir
/// </summary>
public sealed class DialogoGuardar : Form
{
    private readonly TextBox _ruta = new();
    private readonly ListBox _lista = new();
    private readonly TextBox _nombre = new();
    private readonly Label _aviso = new();

    private string _carpeta = "";

    /// <summary>Ruta completa elegida, válida solo si el diálogo devolvió OK.</summary>
    public string RutaElegida { get; private set; } = "";

    /// <summary>Extensión que se agrega si el usuario no escribe ninguna.</summary>
    public string ExtensionPorDefecto { get; set; } = ".asm";

    private const int WM_NCLBUTTONDOWN = 0x00A1;
    private const int HTCAPTION = 0x0002;

    [System.Runtime.InteropServices.DllImport("user32.dll")]
    private static extern bool ReleaseCapture();

    [System.Runtime.InteropServices.DllImport("user32.dll")]
    private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

    public DialogoGuardar(string carpetaInicial, string nombreSugerido)
    {
        FormBorderStyle = FormBorderStyle.None;
        StartPosition = FormStartPosition.CenterParent;
        ShowInTaskbar = false;
        ClientSize = new Size(560, 440);
        BackColor = Tema.Superficie;
        ForeColor = Tema.Texto;
        Font = Tema.Cuerpo;
        KeyPreview = true;

        ConstruirUi();

        _nombre.Text = nombreSugerido;
        IrA(Directory.Exists(carpetaInicial) ? carpetaInicial : AppContext.BaseDirectory);

        Shown += (_, _) =>
        {
            _nombre.Focus();
            // Seleccionar solo el nombre, sin la extensión: es lo que se cambia.
            int punto = _nombre.Text.LastIndexOf('.');
            _nombre.Select(0, punto > 0 ? punto : _nombre.Text.Length);
        };
    }

    private void ConstruirUi()
    {
        // ── Franja de título ──
        var franja = new Panel { Dock = DockStyle.Top, Height = 34, BackColor = Tema.Superficie2 };
        var titulo = new Label
        {
            Text = "Guardar como",
            AutoSize = true,
            Font = Tema.Etiqueta,
            ForeColor = Tema.Acento,
            Location = new Point(14, 9),
            BackColor = Color.Transparent
        };
        franja.Controls.Add(titulo);
        franja.MouseDown += Arrastrar;
        titulo.MouseDown += Arrastrar;

        // ── Barra de carpeta ──
        var subir = new Button
        {
            Text = "↑",
            Location = new Point(14, 46),
            Size = new Size(32, 26),
            FlatStyle = FlatStyle.Flat,
            BackColor = Tema.Superficie3,
            ForeColor = Tema.Texto,
            UseVisualStyleBackColor = false
        };
        subir.FlatAppearance.BorderColor = Tema.LineaSuave;
        subir.Click += (_, _) => Subir();

        _ruta.Location = new Point(52, 47);
        _ruta.Size = new Size(494, 24);
        _ruta.BackColor = Tema.Fondo;
        _ruta.ForeColor = Tema.Texto2;
        _ruta.BorderStyle = BorderStyle.FixedSingle;
        _ruta.ReadOnly = true;

        // ── Lista de carpetas y archivos ──
        _lista.Location = new Point(14, 80);
        _lista.Size = new Size(532, 248);
        _lista.BackColor = Tema.Fondo;
        _lista.ForeColor = Tema.Texto;
        _lista.BorderStyle = BorderStyle.FixedSingle;
        _lista.Font = Tema.Cuerpo;
        _lista.DrawMode = DrawMode.OwnerDrawFixed;
        _lista.ItemHeight = 20;
        _lista.DrawItem += DibujarItem;
        _lista.DoubleClick += (_, _) => ActivarSeleccion();
        _lista.KeyDown += (_, e) =>
        {
            if (e.KeyCode == Keys.Enter) { ActivarSeleccion(); e.Handled = true; }
        };
        _lista.SelectedIndexChanged += (_, _) =>
        {
            // Elegir un archivo de la lista copia su nombre al campo.
            if (_lista.SelectedItem is Entrada en && !en.EsCarpeta) _nombre.Text = en.Nombre;
        };

        // ── Nombre ──
        var lblNombre = new Label
        {
            Text = "Nombre:",
            Location = new Point(14, 342),
            AutoSize = true,
            ForeColor = Tema.Texto2,
            BackColor = Color.Transparent
        };

        _nombre.Location = new Point(75, 339);
        _nombre.Size = new Size(471, 24);
        _nombre.BackColor = Tema.Fondo;
        _nombre.ForeColor = Tema.Texto;
        _nombre.BorderStyle = BorderStyle.FixedSingle;

        _aviso.Location = new Point(75, 368);
        _aviso.Size = new Size(471, 18);
        _aviso.ForeColor = Tema.Aviso;
        _aviso.BackColor = Color.Transparent;
        _aviso.Text = "";

        // ── Botones ──
        var guardar = CrearBoton("Guardar", 350, principal: true);
        var cancelar = CrearBoton("Cancelar", 452, principal: false);

        guardar.Click += (_, _) => Confirmar();
        cancelar.Click += (_, _) => { DialogResult = DialogResult.Cancel; Close(); };

        AcceptButton = guardar;

        KeyDown += (_, e) =>
        {
            if (e.KeyCode == Keys.Escape) { DialogResult = DialogResult.Cancel; Close(); }
        };

        Controls.Add(subir);
        Controls.Add(_ruta);
        Controls.Add(_lista);
        Controls.Add(lblNombre);
        Controls.Add(_nombre);
        Controls.Add(_aviso);
        Controls.Add(guardar);
        Controls.Add(cancelar);
        Controls.Add(franja);
    }

    private Button CrearBoton(string texto, int x, bool principal)
    {
        var b = new Button
        {
            Text = texto,
            Location = new Point(x, 396),
            Size = new Size(94, 30),
            FlatStyle = FlatStyle.Flat,
            BackColor = principal ? Tema.Acento : Tema.Superficie3,
            ForeColor = principal ? Color.Black : Tema.Texto,
            UseVisualStyleBackColor = false
        };
        b.FlatAppearance.BorderColor = principal ? Tema.Acento : Tema.LineaSuave;
        b.FlatAppearance.MouseOverBackColor = Tema.Realzar(b.BackColor, 16);
        return b;
    }

    /// <summary>Una fila de la lista: carpeta o archivo.</summary>
    private sealed record Entrada(string Nombre, string Ruta, bool EsCarpeta)
    {
        public override string ToString() => EsCarpeta ? "[" + Nombre + "]" : Nombre;
    }

    private void DibujarItem(object? sender, DrawItemEventArgs e)
    {
        if (e.Index < 0 || e.Index >= _lista.Items.Count) return;

        var entrada = (Entrada)_lista.Items[e.Index]!;
        bool sel = (e.State & DrawItemState.Selected) == DrawItemState.Selected;

        using (var fondo = new SolidBrush(sel ? Tema.Seleccion : Tema.Fondo))
        {
            e.Graphics.FillRectangle(fondo, e.Bounds);
        }

        var color = entrada.EsCarpeta ? Tema.Acento : Tema.Texto;
        using var brush = new SolidBrush(color);
        e.Graphics.DrawString(entrada.ToString(), _lista.Font, brush, e.Bounds.Left + 4, e.Bounds.Top + 2);
    }

    private void IrA(string carpeta)
    {
        try
        {
            _carpeta = Path.GetFullPath(carpeta);
            _ruta.Text = _carpeta;

            _lista.BeginUpdate();
            _lista.Items.Clear();

            foreach (var dir in Directory.EnumerateDirectories(_carpeta).OrderBy(d => d))
            {
                var nombre = Path.GetFileName(dir);
                if (nombre.StartsWith('.')) continue;
                _lista.Items.Add(new Entrada(nombre, dir, EsCarpeta: true));
            }

            foreach (var f in Directory.EnumerateFiles(_carpeta, "*" + ExtensionPorDefecto).OrderBy(f => f))
            {
                _lista.Items.Add(new Entrada(Path.GetFileName(f), f, EsCarpeta: false));
            }

            _lista.EndUpdate();
        }
        catch (Exception ex)
        {
            _lista.EndUpdate();
            _aviso.Text = "No se pudo leer la carpeta: " + ex.Message;
        }
    }

    private void Subir()
    {
        var padre = Directory.GetParent(_carpeta);
        if (padre is not null) IrA(padre.FullName);
    }

    private void ActivarSeleccion()
    {
        if (_lista.SelectedItem is not Entrada en) return;

        if (en.EsCarpeta) IrA(en.Ruta);
        else { _nombre.Text = en.Nombre; Confirmar(); }
    }

    private void Confirmar()
    {
        var nombre = _nombre.Text.Trim();

        if (string.IsNullOrWhiteSpace(nombre))
        {
            _aviso.Text = "Escribí un nombre de archivo.";
            _nombre.Focus();
            return;
        }

        if (nombre.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
        {
            _aviso.Text = "El nombre tiene caracteres que no se pueden usar.";
            _nombre.Focus();
            return;
        }

        if (!Path.HasExtension(nombre)) nombre += ExtensionPorDefecto;

        var destino = Path.Combine(_carpeta, nombre);

        // Avisar antes de pisar: perder un archivo por no preguntar es el peor
        // resultado posible de un diálogo de guardar.
        if (File.Exists(destino))
        {
            bool pisar = Dialogo.Confirmar(this, "El archivo ya existe",
                $"'{nombre}' ya existe en esta carpeta.\n\n¿Reemplazarlo?",
                si: "Reemplazar", no: "Cancelar");

            if (!pisar) { _nombre.Focus(); return; }
        }

        RutaElegida = destino;
        DialogResult = DialogResult.OK;
        Close();
    }

    private void Arrastrar(object? sender, MouseEventArgs e)
    {
        if (e.Button != MouseButtons.Left) return;
        ReleaseCapture();
        SendMessage(Handle, WM_NCLBUTTONDOWN, HTCAPTION, IntPtr.Zero);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        using var pen = new Pen(Tema.Linea, 1);
        e.Graphics.DrawRectangle(pen, 0, 0, ClientSize.Width - 1, ClientSize.Height - 1);
    }
}
