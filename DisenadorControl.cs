using AsmEditor.Core;
using AsmEditor.Core.Disenador;

namespace AsmEditor;

/// <summary>
/// El diseñador de formularios metido en una pestaña, en vez de en una ventana
/// modal.
///
/// Es el mismo diseñador de antes —el canvas, la paleta y el PropertyGrid son
/// los mismos controles— pero vive dentro de una <see cref="TabPage"/> y se
/// comporta como cualquier otro documento: se guarda con Ctrl+S, se cierra con
/// Ctrl+W, avisa cuando está sucio y se restaura al abrir el editor.
///
/// ⚠ NO TIENE BARRA DE ARCHIVO PROPIA. Abrir, guardar y cerrar los hace
/// MainForm por los mismos menús que el resto de las pestañas: tener un
/// «Guardar» adentro de la pestaña Y otro en el menú Archivo dejaría dos
/// caminos para lo mismo, que es justo lo que se quiere evitar al integrarlo.
/// </summary>
public sealed class DisenadorControl : UserControl, IPestanaEditor
{
    private readonly LienzoDisenador _lienzo = new();
    private readonly ListBox _paleta = new();
    private readonly PropertyGrid _propiedades = new();
    private readonly ListBox _arbol = new();
    private readonly Label _estado = new();
    private readonly ToolStrip _barra = new();

    /// <summary>Evita que rellenar los paneles dispare los eventos de edición.</summary>
    private bool _cargando;

    /// <summary>Deshacer/rehacer, y qué estado cuenta como guardado.</summary>
    private readonly HistorialDisenador _historial;

    public DocumentState State { get; }

    public event EventHandler? DirtyChanged;

    /// <summary>Se pide generar el código del formulario.</summary>
    public event Action<DisenadorControl>? GenerarPedido;

    public DisenadorControl(string? rutaAsmform, FormularioDisenado? formulario = null)
    {
        State = new DocumentState(rutaAsmform);

        _lienzo.Formulario = formulario ?? new FormularioDisenado();
        _historial = new HistorialDisenador(_lienzo.Formulario);

        Dock = DockStyle.Fill;

        ArmarLayout();
        LlenarPaleta();
        RefrescarTodo();

        AplicarTema();
        Tema.TemaCambiado += AplicarTema;
    }

    // ------------------------------------------------------------------
    // IPestanaEditor
    // ------------------------------------------------------------------

    public string? FilePath => State.FilePath;

    public bool IsDirty => State.IsDirty;

    /// <summary>
    /// ⚠ UN DISEÑADOR NO TIENE TEXTO NI LÍNEAS. Se declara acá, y MainForm lo
    /// consulta en vez de asumir: sin esto, Ctrl+F abriría una ventana de
    /// búsqueda apuntando a un editor que no existe, y un doble clic en el
    /// panel de errores intentaría saltar a una línea inexistente.
    /// </summary>
    public bool AdmiteBusqueda => false;

    public bool AdmiteIrALinea => false;

    /// <summary>
    /// Deshacer propio, por copias del formulario: ver <see cref="HistorialDisenador"/>.
    /// </summary>
    public bool AdmiteDeshacer => true;

    public (int Linea, int Columna)? PosicionCursor => null;

    public void Deshacer()
    {
        // ⚠ NO SE DESHACE A MITAD DE UN ARRASTRE: el canvas seguiría moviendo
        // los controles del formulario viejo, que ya no está en pantalla.
        if (_lienzo.EnGesto) return;

        // Lo que haya quedado sin confirmar pasa a ser un paso, así Ctrl+Z
        // deshace ESO y no el cambio anterior, saltándoselo.
        ConfirmarCambio();

        Reponer(_historial.Deshacer());
    }

    public void Rehacer()
    {
        if (_lienzo.EnGesto) return;

        Reponer(_historial.Rehacer());
    }

    /// <summary>
    /// Pone en el canvas un estado sacado del historial.
    ///
    /// ⚠ EL ESTADO REPUESTO TRAE OBJETOS NUEVOS. La selección y el PropertyGrid
    /// apuntaban a los viejos: la selección se recupera por Id y el
    /// PropertyGrid se rearma, o quedaría editando un control que ya no está
    /// en el formulario.
    /// </summary>
    private void Reponer(FormularioDisenado? repuesto)
    {
        if (repuesto is null) return;

        var ids = _lienzo.Seleccionados.Select(c => c.Id).ToList();

        _lienzo.Formulario = repuesto;
        RefrescarArbol();
        _lienzo.SeleccionarPorIds(ids);
        _lienzo.Refrescar();
        RefrescarEstado();

        MarcarSucio(_historial.EstaSucio);
    }

    /// <summary>
    /// Terminó una acción: se registra como paso de deshacer si cambió algo, y
    /// el estado sucio pasa a decirlo el historial, que sabe si se volvió a lo
    /// guardado.
    /// </summary>
    private void ConfirmarCambio()
    {
        _historial.Confirmar(_lienzo.Formulario);
        MarcarSucio(_historial.EstaSucio);
    }

    public void TomarFoco() => _lienzo.Focus();

    /// <summary>El formulario que se está editando.</summary>
    public FormularioDisenado Formulario => _lienzo.Formulario;

    public bool Guardar()
    {
        if (State.FilePath is null) return false;

        try
        {
            ArchivoFormulario.Guardar(State.FilePath, _lienzo.Formulario);
            _historial.MarcarGuardado(_lienzo.Formulario);
            MarcarSucio(false);
            return true;
        }
        catch (Exception ex)
        {
            MessageBox.Show(this,
                $"No se pudo guardar el formulario:{Environment.NewLine}{Environment.NewLine}{ex.Message}",
                "Guardar", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return false;
        }
    }

    public bool GuardarComo(IWin32Window duenio)
    {
        using var dlg = new SaveFileDialog
        {
            Title = "Guardar formulario",
            Filter = "Formulario del diseñador (*.asmform)|*.asmform",
            FileName = State.FilePath is null
                ? _lienzo.Formulario.Nombre + ArchivoFormulario.Extension
                : Path.GetFileName(State.FilePath),
            InitialDirectory = State.FilePath is null ? null : Path.GetDirectoryName(State.FilePath)
        };

        if (dlg.ShowDialog(duenio) != DialogResult.OK) return false;

        State.FilePath = dlg.FileName;

        if (!Guardar()) return false;

        DirtyChanged?.Invoke(this, EventArgs.Empty);
        return true;
    }

    private void MarcarSucio(bool sucio)
    {
        if (State.IsDirty == sucio) return;

        State.IsDirty = sucio;
        DirtyChanged?.Invoke(this, EventArgs.Empty);
    }

    // ------------------------------------------------------------------
    // Layout
    // ------------------------------------------------------------------

    private void ArmarLayout()
    {
        // ---- Barra: SOLO acciones del diseñador ----
        //
        // Nuevo, abrir, guardar y cerrar son del menú Archivo, como en
        // cualquier pestaña. Acá quedan las que no tienen equivalente.
        _barra.Dock = DockStyle.Top;
        _barra.GripStyle = ToolStripGripStyle.Hidden;
        _barra.Renderer = new ToolStripProfessionalRenderer(new ColoresBarraDisenador());

        AgregarBoton("Generar código", "Escribir el .inc y, si no existe, el .asm",
            () => GenerarPedido?.Invoke(this));

        _barra.Items.Add(new ToolStripSeparator());

        AgregarBoton("Alinear izq.", "Alinear a la izquierda del primero seleccionado",
            () => _lienzo.Alinear(AlineacionDisenador.Izquierda));
        AgregarBoton("Alinear arriba", "Alinear con el borde de arriba del primero",
            () => _lienzo.Alinear(AlineacionDisenador.Arriba));
        AgregarBoton("Mismo ancho", "Darles el ancho del primero seleccionado",
            () => _lienzo.Alinear(AlineacionDisenador.MismoAncho));

        _barra.Items.Add(new ToolStripSeparator());

        AgregarBoton("Borrar", "Borrar lo seleccionado (Supr)", _lienzo.BorrarSeleccion);

        // ---- Panel izquierdo: paleta ----
        var izq = new Panel { Dock = DockStyle.Left, Width = 180, Padding = new Padding(8) };

        var lblPaleta = new Label
        {
            Text = "Controles",
            Dock = DockStyle.Top,
            Height = 22,
            Font = new Font("Segoe UI", 9f, FontStyle.Bold)
        };

        _paleta.Dock = DockStyle.Fill;
        _paleta.IntegralHeight = false;
        _paleta.BorderStyle = BorderStyle.FixedSingle;
        _paleta.SelectedIndexChanged += PaletaSeleccionada;

        var ayuda = new Label
        {
            Dock = DockStyle.Bottom,
            AutoSize = true,
            MaximumSize = new Size(160, 0),
            Padding = new Padding(0, 8, 0, 0),
            Text = "Elegí un control y hacé clic en el formulario.\n\n" +
                   "Supr borra\n" +
                   "Ctrl+Z deshace\n" +
                   "Ctrl+Y rehace\n" +
                   "Ctrl+clic suma a la selección\n" +
                   "Flechas mueven de a 1 px\n" +
                   "Shift+flechas, de a 4 px",
            ForeColor = SystemColors.GrayText
        };

        izq.Controls.Add(_paleta);
        izq.Controls.Add(lblPaleta);
        izq.Controls.Add(ayuda);

        // ---- Panel derecho: árbol y propiedades ----
        var der = new Panel { Dock = DockStyle.Right, Width = 290, Padding = new Padding(8) };

        var lblArbol = new Label
        {
            Text = "Controles del formulario",
            Dock = DockStyle.Top,
            Height = 22,
            Font = new Font("Segoe UI", 9f, FontStyle.Bold)
        };

        _arbol.Dock = DockStyle.Top;
        _arbol.Height = 150;
        _arbol.IntegralHeight = false;
        _arbol.BorderStyle = BorderStyle.FixedSingle;
        _arbol.SelectedIndexChanged += ArbolSeleccionado;

        var lblProps = new Label
        {
            Text = "Propiedades",
            Dock = DockStyle.Top,
            Height = 26,
            Font = new Font("Segoe UI", 9f, FontStyle.Bold),
            Padding = new Padding(0, 6, 0, 0)
        };

        _propiedades.Dock = DockStyle.Fill;
        _propiedades.ToolbarVisible = false;
        _propiedades.PropertySort = PropertySort.Categorized;
        _propiedades.PropertyValueChanged += PropiedadCambiada;

        der.Controls.Add(_propiedades);
        der.Controls.Add(lblProps);
        der.Controls.Add(_arbol);
        der.Controls.Add(lblArbol);

        // ---- Centro: el canvas ----
        //
        // El padding de arriba tiene que superar la barra de título que el
        // canvas dibuja sobre el formulario, o queda tapada por el borde.
        var centro = new Panel
        {
            Dock = DockStyle.Fill,
            AutoScroll = true,
            Padding = new Padding(8, 12, 8, 8)
        };

        _lienzo.FormularioCambiado += LienzoCambio;
        _lienzo.SeleccionCambiada += SeleccionCambio;
        _lienzo.HerramientaConsumida += () => _paleta.ClearSelected();
        _lienzo.CambioTerminado += ConfirmarCambio;

        centro.Controls.Add(_lienzo);

        // ---- Estado ----
        _estado.Dock = DockStyle.Bottom;
        _estado.Height = 22;
        _estado.TextAlign = ContentAlignment.MiddleLeft;
        _estado.Padding = new Padding(8, 0, 0, 0);

        Controls.Add(centro);
        Controls.Add(der);
        Controls.Add(izq);
        Controls.Add(_estado);
        Controls.Add(_barra);
    }

    private void AgregarBoton(string texto, string ayuda, Action accion)
    {
        var b = new ToolStripButton(texto)
        {
            DisplayStyle = ToolStripItemDisplayStyle.Text,
            ToolTipText = ayuda,
            AutoToolTip = true
        };

        b.Click += (_, _) => accion();
        _barra.Items.Add(b);
    }

    private void LlenarPaleta()
    {
        _paleta.Items.Clear();

        foreach (var tipo in Enum.GetValues<TipoControl>())
        {
            _paleta.Items.Add(new EntradaPaleta(tipo));
        }
    }

    // ------------------------------------------------------------------
    // Eventos
    // ------------------------------------------------------------------

    private void PaletaSeleccionada(object? sender, EventArgs e)
    {
        if (_cargando) return;

        _lienzo.UsarHerramienta(_paleta.SelectedItem is EntradaPaleta p ? p.Tipo : null);
    }

    private void LienzoCambio()
    {
        MarcarSucio(true);
        RefrescarArbol();
        RefrescarEstado();

        if (_propiedades.SelectedObject is not null) _propiedades.Refresh();
    }

    private void SeleccionCambio()
    {
        _cargando = true;

        try
        {
            if (_lienzo.Seleccionados.Count == 1)
            {
                _propiedades.SelectedObject =
                    new AdaptadorControl(_lienzo.Seleccionados[0], _lienzo.Formulario);

                var idx = _lienzo.Formulario.Controles.IndexOf(_lienzo.Seleccionados[0]);
                if (idx >= 0 && idx < _arbol.Items.Count) _arbol.SelectedIndex = idx;
            }
            else if (_lienzo.Seleccionados.Count == 0)
            {
                _propiedades.SelectedObject = new AdaptadorFormulario(_lienzo.Formulario);
                _arbol.ClearSelected();
            }
            else
            {
                // Con varios seleccionados no se muestran propiedades: editar
                // "la X" de cinco controles los amontonaría en el mismo lugar.
                _propiedades.SelectedObject = null;
                _arbol.ClearSelected();
            }
        }
        finally { _cargando = false; }

        RefrescarEstado();
    }

    private void ArbolSeleccionado(object? sender, EventArgs e)
    {
        if (_cargando) return;

        var i = _arbol.SelectedIndex;

        if (i >= 0 && i < _lienzo.Formulario.Controles.Count)
        {
            _lienzo.SeleccionarSolo(_lienzo.Formulario.Controles[i]);
        }
    }

    private void PropiedadCambiada(object? s, PropertyValueChangedEventArgs e)
    {
        _lienzo.Refrescar();
        RefrescarArbol();
        RefrescarEstado();

        // El PropertyGrid solo avisa cuando el valor se aceptó: un nombre
        // rechazado por el adaptador no llega acá ni deja paso.
        ConfirmarCambio();
    }

    // ------------------------------------------------------------------
    // Refresco
    // ------------------------------------------------------------------

    public void RefrescarTodo()
    {
        RefrescarArbol();
        SeleccionCambio();
        RefrescarEstado();
    }

    private void RefrescarArbol()
    {
        _cargando = true;

        try
        {
            var seleccionado = _arbol.SelectedIndex;

            _arbol.Items.Clear();

            foreach (var c in _lienzo.Formulario.Controles)
            {
                _arbol.Items.Add($"{c.Nombre}   ({InfoTipoControl.Nombre(c.Tipo)}, id {c.Id})");
            }

            if (seleccionado >= 0 && seleccionado < _arbol.Items.Count)
            {
                _arbol.SelectedIndex = seleccionado;
            }
        }
        finally { _cargando = false; }
    }

    private void RefrescarEstado()
    {
        var f = _lienzo.Formulario;
        var errores = f.Validar();

        if (errores.Count > 0)
        {
            _estado.ForeColor = Tema.Critico;
            _estado.Text = errores.Count == 1 ? errores[0] : $"{errores.Count} problemas: {errores[0]}";
            return;
        }

        _estado.ForeColor = Tema.Texto2;

        var tipo = f.EsVentanaPrincipal ? "ventana principal" : "diálogo";
        var arch = f.EsX64 ? "x64" : "x86";

        _estado.Text = $"{f.Controles.Count} controles · {f.Ancho}×{f.Alto} · {tipo} · {arch}" +
                       (_lienzo.Seleccionados.Count > 0
                           ? $" · {_lienzo.Seleccionados.Count} seleccionado(s)"
                           : "");
    }

    /// <summary>Deja constancia de que el formulario se guardó desde afuera.</summary>
    public void MarcarGuardado()
    {
        _historial.MarcarGuardado(_lienzo.Formulario);
        MarcarSucio(false);
    }

    // ------------------------------------------------------------------
    // Tema
    // ------------------------------------------------------------------

    private void AplicarTema()
    {
        BackColor = Tema.Fondo;
        ForeColor = Tema.Texto;

        _barra.BackColor = Tema.Superficie;
        _barra.ForeColor = Tema.Texto;

        _paleta.BackColor = Tema.Superficie;
        _paleta.ForeColor = Tema.Texto;
        _arbol.BackColor = Tema.Superficie;
        _arbol.ForeColor = Tema.Texto;

        // El PropertyGrid tiene sus propios colores y no hereda del padre.
        _propiedades.BackColor = Tema.Superficie;
        _propiedades.ViewBackColor = Tema.Superficie;
        _propiedades.ViewForeColor = Tema.Texto;
        _propiedades.LineColor = Tema.LineaSuave;
        _propiedades.CategoryForeColor = Tema.Acento;
        _propiedades.HelpBackColor = Tema.Superficie2;
        _propiedades.HelpForeColor = Tema.Texto2;

        _estado.BackColor = Tema.Superficie2;
        _estado.ForeColor = Tema.Texto2;

        foreach (Control c in Controls) AplicarTemaA(c);

        _lienzo.Refrescar();
        Invalidate(true);
    }

    private static void AplicarTemaA(Control c)
    {
        if (c is Panel) c.BackColor = Tema.Fondo;

        if (c is Label l && l.ForeColor != SystemColors.GrayText)
        {
            l.BackColor = Color.Transparent;
        }

        foreach (Control h in c.Controls) AplicarTemaA(h);
    }

    /// <summary>
    /// Deja pasar los atajos del editor que el canvas o el PropertyGrid podrían
    /// quedarse.
    ///
    /// ⚠ HACE FALTA PORQUE AHORA HAY CONTROLES QUE ATRAPAN TECLAS DENTRO DE UNA
    /// PESTAÑA. Con el diseñador modal esto no existía: la ventana era otra.
    /// Medido en el editor real: con el foco en el canvas, Ctrl+W no cerraba la
    /// pestaña y no pasaba nada, sin ningún aviso de por qué.
    ///
    /// Se devuelven en falso para que sigan su camino normal hasta el menú, que
    /// es quien los maneja: acá no se ejecuta la acción, solo se deja pasar.
    /// </summary>
    protected override bool ProcessCmdKey(ref Message msg, Keys teclas)
    {
        // Supr y Escape son del canvas mientras tenga el foco: borran la
        // selección y cancelan la herramienta.
        if (_lienzo.Focused && teclas is Keys.Delete or Keys.Escape)
        {
            return base.ProcessCmdKey(ref msg, teclas);
        }

        // Escribiendo un valor en el PropertyGrid, Ctrl+Z es de la casilla y
        // NO llega acá: el GridViewTextBox del PropertyGrid se lo queda antes.
        // Medido el 23/09 con rastro en este método (rastrear_ctrlz_propertygrid.ps1):
        // los Ctrl+Z del canvas llegan, el de la casilla no. Por eso no hace
        // falta —y no hay que agregar— una excepción para el PropertyGrid.

        // Los atajos del editor: que los atienda el menú, no los controles.
        bool esAtajoDelEditor = teclas switch
        {
            Keys.Control | Keys.Z => true,   // deshacer
            Keys.Control | Keys.Y => true,   // rehacer
            Keys.Control | Keys.W => true,   // cerrar pestaña
            Keys.Control | Keys.S => true,   // guardar
            Keys.Control | Keys.N => true,   // nuevo
            Keys.Control | Keys.O => true,   // abrir
            Keys.Control | Keys.D => true,   // nuevo formulario
            Keys.F7 => true,                 // compilar
            Keys.F5 => true,                 // compilar y ejecutar
            _ => false
        };

        if (esAtajoDelEditor && ParentForm?.MainMenuStrip is not null)
        {
            var item = BuscarPorAtajo(ParentForm.MainMenuStrip.Items, teclas);

            if (item is not null)
            {
                // Deshabilitado significa que ahora no corresponde: se respeta
                // en vez de forzarlo.
                if (item.Enabled) item.PerformClick();
                return true;
            }
        }

        return base.ProcessCmdKey(ref msg, teclas);
    }

    /// <summary>
    /// El ítem de menú que tiene ese atajo, buscando en todos los submenús.
    /// </summary>
    private static ToolStripMenuItem? BuscarPorAtajo(ToolStripItemCollection items, Keys teclas)
    {
        foreach (ToolStripItem it in items)
        {
            if (it is not ToolStripMenuItem menu) continue;

            if (menu.ShortcutKeys == teclas) return menu;

            var dentro = BuscarPorAtajo(menu.DropDownItems, teclas);
            if (dentro is not null) return dentro;
        }

        return null;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) Tema.TemaCambiado -= AplicarTema;
        base.Dispose(disposing);
    }

    // ------------------------------------------------------------------

    /// <summary>Una entrada de la paleta; el ToString es lo que se ve en la lista.</summary>
    private sealed record EntradaPaleta(TipoControl Tipo)
    {
        public override string ToString() => InfoTipoControl.Nombre(Tipo);
    }

    /// <summary>
    /// Colores de la barra, para que siga el tema del editor en vez de los del
    /// sistema.
    /// </summary>
    private sealed class ColoresBarraDisenador : ProfessionalColorTable
    {
        public override Color ToolStripGradientBegin => Tema.Superficie;
        public override Color ToolStripGradientMiddle => Tema.Superficie;
        public override Color ToolStripGradientEnd => Tema.Superficie;
        public override Color ButtonSelectedHighlight => Tema.Superficie3;
        public override Color ButtonSelectedBorder => Tema.Acento;
        public override Color ButtonPressedHighlight => Tema.AcentoTenue;
        public override Color SeparatorDark => Tema.LineaSuave;
        public override Color SeparatorLight => Tema.LineaSuave;
    }
}
