using System.Diagnostics;
using System.Text;
using AsmEditor.Core;
using AsmEditor.Core.Disenador;
using AsmEditor.Core.Proyecto;

namespace AsmEditor;

public class MainForm : Form
{
    private readonly PestanasAsm _tabs = new();
    private readonly RichTextBox _output = new();
    private readonly ProjectExplorer _explorer = new();

    private readonly SplitContainer _outerSplit = new();   // explorador | resto
    private readonly SplitContainer _innerSplit = new();   // pestañas   | salida

    private readonly StatusStrip _statusStrip = new();
    private readonly ToolStripStatusLabel _statusLabel = new();
    private readonly ToolStripStatusLabel _caretLabel = new();

    private readonly OpenDocuments _documents = new();
    private BuildSettings _settings = BuildSettings.Load();
    private readonly BuildRunner _runner = new();

    /// <summary>
    /// El proyecto abierto, o null si se está trabajando con archivos sueltos.
    ///
    /// ⚠ NULL ES UN ESTADO VÁLIDO Y CORRIENTE, no una falla. El editor tiene
    /// que seguir abriendo y compilando un .asm suelto exactamente como antes
    /// de que existieran los proyectos: los .asm de la raíz y los scripts de
    /// afuera dependen de eso. Todo lo que consulte este campo tiene que
    /// funcionar igual con null.
    /// </summary>
    private ProyectoAsm? _proyecto;

    /// <summary>
    /// True mientras se abren los archivos del arranque. Sirve para no
    /// interrumpir con diálogos durante la restauración de la sesión.
    /// </summary>
    private bool _abriendoAlIniciar;

    private ToolStripMenuItem? _proyectosRecientesMenu;

    /// <summary>Comandos que no tienen sentido sin un proyecto abierto.</summary>
    private readonly List<ToolStripMenuItem> _proyectoMenuItems = new();

    /// <summary>Comandos que necesitan una pestaña con texto donde buscar.</summary>
    private readonly List<ToolStripMenuItem> _busquedaMenuItems = new();

    /// <summary>Comandos que necesitan una pestaña con deshacer propio.</summary>
    private readonly List<ToolStripMenuItem> _deshacerMenuItems = new();

    // ---- Panel de errores y parseo de diagnósticos ----
    private readonly ErrorListPanel _errorList = new();

    // También PestanasAsm: un TabControl estándar acá dejaría una franja con el
    // color del sistema justo debajo del editor.
    private readonly PestanasAsm _bottomTabs = new();
    private readonly DiagnosticParser _parser = new();

    // ---- Estado de la compilación en curso ----
    private CancellationTokenSource? _buildCts;
    private bool _buildInProgress;
    private readonly List<ToolStripMenuItem> _buildMenuItems = new();
    private ToolStripMenuItem? _stopMenuItem;

    /// <summary>Comandos que no tienen sentido sin un documento abierto.</summary>
    private readonly List<ToolStripMenuItem> _documentMenuItems = new();

    /// <summary>Pantalla que se ve cuando no hay ninguna pestaña abierta.</summary>
    private readonly PantallaBienvenida _bienvenida = new();

    private ToolStripMenuItem? _recientesMenu;
    private ToolStripMenuItem? _temaMenu;
    private ToolStripMenuItem? _temaOscuroItem;
    private ToolStripMenuItem? _temaClaroItem;

    // ---- Barra de herramientas con el selector de target ----
    private ToolStrip? _toolbar;
    private ToolStripComboBox? _targetCombo;
    private ToolStripButton? _stopButton;
    private ToolStripButton? _temaBoton;
    private bool _suppressTargetChange;

    /// <summary>Botones de la barra que necesitan un documento abierto.</summary>
    private readonly List<ToolStripButton> _toolbarDocItems = new();

    /// <summary>Botones de la barra que necesitan texto donde buscar.</summary>
    private readonly List<ToolStripButton> _toolbarBusquedaItems = new();

    /// <summary>Botones de la barra que necesitan deshacer propio.</summary>
    private readonly List<ToolStripButton> _toolbarDeshacerItems = new();

    /// <summary>Botones de la barra que se deshabilitan mientras compila.</summary>
    private readonly List<ToolStripButton> _toolbarBuildItems = new();

    private FindReplaceForm? _findReplaceForm;
    private ToolStripMenuItem? _explorerToggle;

    public MainForm(string[]? filesToOpen = null, FormSplash? splash = null)
    {
        // El tema se aplica ANTES de construir los controles: cada uno lee los
        // colores al crearse y no hay enlace vivo con la paleta.
        splash?.Informar("Leyendo la configuración...");
        Tema.Modo = Ui.Theme == ModoTemaGuardado.Claro ? ModoTema.Claro : ModoTema.Oscuro;

        MinimumSize = new Size(820, 520);
        Icon = LogoEditor.CrearIcono();
        Text = "Editor ASM";

        splash?.Informar("Construyendo la interfaz...");
        BuildMenu();
        BuildLayout();
        WireEvents();

        RestaurarGeometria();
        AplicarTema();
        Tema.TemaCambiado += AplicarTema;

        // Los recientes que ya no existen se descartan al cargar: una lista que
        // ofrece archivos borrados es peor que una corta.
        splash?.Informar("Revisando los archivos recientes...");
        Ui.PurgeMissingRecents(File.Exists);
        ReconstruirMenuRecientes();

        ReconstruirMenuProyectosRecientes();

        splash?.Informar("Explorando la carpeta del proyecto...");
        _explorer.SetRoot(SafeProjectFolder());

        // El proyecto de la sesión anterior, si sigue estando. Si no, se arranca
        // sin proyecto, que es un estado perfectamente válido.
        if (!string.IsNullOrWhiteSpace(Ui.ProyectoAbierto) && File.Exists(Ui.ProyectoAbierto))
        {
            splash?.Informar("Abriendo el proyecto...");
            AbrirProyecto(Ui.ProyectoAbierto);
        }

        ActualizarEstadoDelProyecto();

        splash?.Informar("Verificando las herramientas de compilación...");
        VerificarHerramientasAlIniciar();

        AbrirAlIniciar(filesToOpen, splash);
    }

    // ---------------------------------------------------------------
    // Menú
    // ---------------------------------------------------------------

    private void BuildMenu()
    {
        var menu = new MenuStrip();

        var fileMenu = new ToolStripMenuItem("&Archivo");
        AddItem(fileMenu, "&Nuevo", Keys.Control | Keys.N, (_, _) => NewFile());
        AddItem(fileMenu, "&Abrir...", Keys.Control | Keys.O, (_, _) => OpenFileDialog());
        _documentMenuItems.Add(AddItem(fileMenu, "&Guardar", Keys.Control | Keys.S, (_, _) => SaveActive()));
        _documentMenuItems.Add(AddItem(fileMenu, "Guardar &como...", Keys.Control | Keys.Shift | Keys.S, (_, _) => SaveActiveAs()));
        _documentMenuItems.Add(AddItem(fileMenu, "Guardar &todo", Keys.Control | Keys.Alt | Keys.S, (_, _) => SaveAll()));
        fileMenu.DropDownItems.Add(new ToolStripSeparator());

        _recientesMenu = new ToolStripMenuItem("&Recientes");
        fileMenu.DropDownItems.Add(_recientesMenu);

        fileMenu.DropDownItems.Add(new ToolStripSeparator());
        _documentMenuItems.Add(AddItem(fileMenu, "&Cerrar pestaña", Keys.Control | Keys.W, (_, _) => CloseActiveTab()));
        _documentMenuItems.Add(AddItem(fileMenu, "Cerrar &todas", Keys.Control | Keys.Shift | Keys.W, (_, _) => CloseAllTabs()));
        _documentMenuItems.Add(AddItem(fileMenu, "Cerrar las &demás", Keys.None, (_, _) => CloseOtherTabs()));
        fileMenu.DropDownItems.Add(new ToolStripSeparator());
        AddItem(fileMenu, "&Salir", Keys.None, (_, _) => Close());

        // ⚠ ESTOS COMANDOS SON DE TEXTO Y NO TODA PESTAÑA LO ES. Se guardan en
        // listas aparte para poder deshabilitarlos en una pestaña de diseño: si
        // quedaran habilitados, Ctrl+F abriría una búsqueda sobre nada y Ctrl+Z
        // parecería deshacer algo sin hacerlo.
        var editMenu = new ToolStripMenuItem("&Editar");

        _deshacerMenuItems.Add(AddItem(editMenu, "&Deshacer", Keys.Control | Keys.Z,
            (_, _) => PestanaActiva?.Deshacer()));
        _deshacerMenuItems.Add(AddItem(editMenu, "&Rehacer", Keys.Control | Keys.Y,
            (_, _) => PestanaActiva?.Rehacer()));

        editMenu.DropDownItems.Add(new ToolStripSeparator());

        _busquedaMenuItems.Add(AddItem(editMenu, "&Buscar...", Keys.Control | Keys.F,
            (_, _) => ShowFindReplace(showReplace: false)));
        _busquedaMenuItems.Add(AddItem(editMenu, "&Reemplazar...", Keys.Control | Keys.H,
            (_, _) => ShowFindReplace(showReplace: true)));

        editMenu.DropDownItems.Add(new ToolStripSeparator());

        _busquedaMenuItems.Add(AddItem(editMenu, "&Autocompletar", Keys.Control | Keys.Space,
            (_, _) => Active?.FocusEditor()));

        var viewMenu = new ToolStripMenuItem("&Ver");
        _explorerToggle = new ToolStripMenuItem("&Explorador de archivos", null, (_, _) => ToggleExplorer())
        {
            Checked = true,
            CheckOnClick = false,
            ShortcutKeys = Keys.Control | Keys.B
        };
        viewMenu.DropDownItems.Add(_explorerToggle);
        // Shift+F5 está reservado para detener la compilación: acá iría en conflicto.
        AddItem(viewMenu, "&Actualizar explorador", Keys.Control | Keys.R, (_, _) => _explorer.Refresh_());
        viewMenu.DropDownItems.Add(new ToolStripSeparator());
        AddItem(viewMenu, "Pestaña &siguiente", Keys.Control | Keys.Tab, (_, _) => CycleTab(1));
        AddItem(viewMenu, "Pestaña &anterior", Keys.Control | Keys.Shift | Keys.Tab, (_, _) => CycleTab(-1));
        viewMenu.DropDownItems.Add(new ToolStripSeparator());
        AddItem(viewMenu, "&Limpiar salida", Keys.None, (_, _) => _output.Clear());
        viewMenu.DropDownItems.Add(new ToolStripSeparator());

        _temaMenu = new ToolStripMenuItem("&Tema");
        _temaOscuroItem = new ToolStripMenuItem("&Oscuro", null, (_, _) => CambiarTema(ModoTema.Oscuro));
        _temaClaroItem = new ToolStripMenuItem("&Claro", null, (_, _) => CambiarTema(ModoTema.Claro));
        _temaMenu.DropDownItems.Add(_temaOscuroItem);
        _temaMenu.DropDownItems.Add(_temaClaroItem);
        viewMenu.DropDownItems.Add(_temaMenu);

        var buildMenu = new ToolStripMenuItem("&Compilar");
        _buildMenuItems.Add(AddItem(buildMenu, "&Compilar (NASM)", Keys.F7,
            async (_, _) => await CompileOnlyAsync()));
        _buildMenuItems.Add(AddItem(buildMenu, "&Enlazar (GoLink)", Keys.Control | Keys.F7,
            async (_, _) => await LinkOnlyAsync()));
        _buildMenuItems.Add(AddItem(buildMenu, "Compilar y &Enlazar", Keys.Control | Keys.Shift | Keys.B,
            async (_, _) => await BuildOnlyAsync()));
        buildMenu.DropDownItems.Add(new ToolStripSeparator());
        _buildMenuItems.Add(AddItem(buildMenu, "Compilar, Enlazar y &Ejecutar", Keys.F5,
            async (_, _) => await BuildAndRunAsync()));
        _buildMenuItems.Add(AddItem(buildMenu, "&Ejecutar solamente", Keys.Control | Keys.F5,
            (_, _) => RunExecutable()));
        buildMenu.DropDownItems.Add(new ToolStripSeparator());

        _stopMenuItem = AddItem(buildMenu, "&Detener compilación", Keys.Shift | Keys.F5,
            (_, _) => StopBuild());
        _stopMenuItem.Enabled = false;

        buildMenu.DropDownItems.Add(new ToolStripSeparator());
        AddItem(buildMenu, "&Administrar targets...", Keys.None, (_, _) => ShowTargetEditor());

        var proyectoMenu = new ToolStripMenuItem("&Proyecto");
        AddItem(proyectoMenu, "&Nuevo proyecto...", Keys.None, (_, _) => NuevoProyecto());
        AddItem(proyectoMenu, "&Abrir proyecto...", Keys.None, (_, _) => AbrirProyectoDialogo());

        _proyectosRecientesMenu = new ToolStripMenuItem("Proyectos &recientes");
        proyectoMenu.DropDownItems.Add(_proyectosRecientesMenu);

        proyectoMenu.DropDownItems.Add(new ToolStripSeparator());

        _proyectoMenuItems.Add(AddItem(proyectoMenu, "A&gregar archivo al proyecto...", Keys.None,
            (_, _) => AgregarArchivoAlProyecto()));
        _proyectoMenuItems.Add(AddItem(proyectoMenu, "&Propiedades del proyecto...", Keys.None,
            (_, _) => MostrarPropiedadesDelProyecto()));
        _proyectoMenuItems.Add(AddItem(proyectoMenu, "&Cerrar proyecto", Keys.None,
            (_, _) => CerrarProyecto()));

        var toolsMenu = new ToolStripMenuItem("&Herramientas");
        AddItem(toolsMenu, "&Diseñador de formularios...", Keys.Control | Keys.D,
            (_, _) => MostrarDisenador());

        var configMenu = new ToolStripMenuItem("&Configuración");
        AddItem(configMenu, "&Rutas de herramientas...", Keys.None, (_, _) => ShowSettings());
        AddItem(configMenu, "&Verificar herramientas", Keys.None, (_, _) => CheckTools());

        var helpMenu = new ToolStripMenuItem("A&yuda");
        AddItem(helpMenu, "&Documentación de NASM", Keys.F1, (_, _) => AbrirDocumentacionNasm());
        AddItem(helpMenu, "&Sitio del autor", Keys.None, (_, _) => AbrirEnNavegador(InfoApp.Sitio));
        helpMenu.DropDownItems.Add(new ToolStripSeparator());
        AddItem(helpMenu, "&Acerca de...", Keys.None, (_, _) => MostrarAcercaDe());

        menu.Items.Add(fileMenu);
        menu.Items.Add(editMenu);
        menu.Items.Add(viewMenu);
        menu.Items.Add(proyectoMenu);
        menu.Items.Add(buildMenu);
        menu.Items.Add(toolsMenu);
        menu.Items.Add(configMenu);
        menu.Items.Add(helpMenu);

        MainMenuStrip = menu;
        Controls.Add(menu);
    }

    /// <summary>Agrega un ítem al menú y lo devuelve, para poder habilitarlo o deshabilitarlo después.</summary>
    private static ToolStripMenuItem AddItem(ToolStripMenuItem parent, string text, Keys shortcut, EventHandler handler)
    {
        var item = new ToolStripMenuItem(text, null, handler);
        if (shortcut != Keys.None) item.ShortcutKeys = shortcut;
        parent.DropDownItems.Add(item);
        return item;
    }

    // ---------------------------------------------------------------
    // Layout
    // ---------------------------------------------------------------

    private void BuildLayout()
    {
        // El aspecto de las pestañas lo define PestanasAsm; acá solo va dónde se ubica.
        _tabs.Dock = DockStyle.Fill;

        _output.Dock = DockStyle.Fill;
        _output.Font = Tema.CodigoChico;
        _output.ReadOnly = true;
        _output.WordWrap = false;
        _output.BackColor = Tema.SalidaFondo;
        _output.ForeColor = Tema.SalidaTexto;
        _output.BorderStyle = BorderStyle.None;

        // El panel inferior tiene dos solapas: la lista de errores y la salida cruda.
        // La salida en texto plano no se pierde, queda en su propia solapa.
        _bottomTabs.Dock = DockStyle.Fill;
        // Solapas fijas: una X no tendría qué cerrar.
        _bottomTabs.PermiteCerrar = false;
        _bottomTabs.UsaFondoDeCodigo = false;
        _bottomTabs.Alignment = TabAlignment.Bottom;
        _bottomTabs.ItemSize = new Size(110, PestanasAsm.AltoPestana);

        var errorPage = new TabPage("Errores") { BackColor = Tema.Superficie };
        errorPage.Controls.Add(_errorList);

        var outputPage = new TabPage("Salida") { BackColor = Tema.SalidaFondo };
        outputPage.Controls.Add(_output);

        _bottomTabs.TabPages.Add(errorPage);
        _bottomTabs.TabPages.Add(outputPage);

        _innerSplit.Dock = DockStyle.Fill;
        _innerSplit.Orientation = Orientation.Horizontal;

        // La bienvenida se agrega PRIMERO para que las pestañas queden encima:
        // con Dock, lo agregado después se acomoda primero.
        _innerSplit.Panel1.Controls.Add(_bienvenida);
        _innerSplit.Panel1.Controls.Add(_tabs);
        _innerSplit.Panel2.Controls.Add(_bottomTabs);

        _bienvenida.NuevoPedido += NewFile;
        _bienvenida.AbrirPedido += OpenFileDialog;
        _bienvenida.ArchivoElegido += OpenPath;

        _outerSplit.Dock = DockStyle.Fill;
        _outerSplit.Orientation = Orientation.Vertical;
        _outerSplit.Panel1.Controls.Add(_explorer);
        _outerSplit.Panel2.Controls.Add(_innerSplit);
        _outerSplit.Panel1MinSize = 140;

        BuildToolbar();

        _statusLabel.Text = "Doble clic en un error del panel de errores para saltar a la línea";
        _statusLabel.Spring = true;
        _statusLabel.TextAlign = ContentAlignment.MiddleLeft;
        _caretLabel.Text = "Ln 1, Col 1";
        _statusStrip.Items.Add(_statusLabel);
        _statusStrip.Items.Add(_caretLabel);

        // Orden de z-order de WinForms: con Dock, lo que se agrega DESPUÉS se acomoda
        // primero y queda por encima. El Fill va primero; las barras Top/Bottom
        // después, o el split las tapa y se come la fila de pestañas.
        Controls.Add(_outerSplit);
        Controls.Add(_statusStrip);

        if (_toolbar is not null) Controls.Add(_toolbar);

        if (MainMenuStrip is not null)
        {
            Controls.Remove(MainMenuStrip);
            Controls.Add(MainMenuStrip);
        }

        Shown += (_, _) =>
        {
            _outerSplit.SplitterDistance = 230;
            _innerSplit.SplitterDistance = (int)(_innerSplit.Height * 0.68);
        };
    }

    /// <summary>
    /// Barra con el selector de target (siempre visible, para no tener que entrar
    /// al menú cada vez) y los comandos de compilación más usados.
    /// </summary>
    /// <summary>
    /// La barra de herramientas: los comandos de todos los días a un clic.
    ///
    /// Los botones llevan un glifo dibujado con GDI+ y no un .png, por lo mismo
    /// que el logo: vale para cualquier tamaño y no hay recursos que empaquetar.
    /// El texto va solo en los que se usan de a poco; los frecuentes van con
    /// ícono y el nombre en el tooltip, para que la barra entre en pantallas
    /// angostas.
    /// </summary>
    private void BuildToolbar()
    {
        _toolbar = new ToolStrip
        {
            Dock = DockStyle.Top,
            GripStyle = ToolStripGripStyle.Hidden,
            BackColor = Tema.Superficie2,
            Renderer = new RendererBarras(),
            ImageScalingSize = new Size(18, 18),
            Padding = new Padding(6, 2, 6, 2)
        };

        // ── Archivo ──
        AgregarBoton(Glifo.Nuevo, "Nuevo", "Nuevo archivo (Ctrl+N)", (_, _) => NewFile());
        AgregarBoton(Glifo.Abrir, "Abrir", "Abrir archivo (Ctrl+O)", (_, _) => OpenFileDialog());
        _toolbarDocItems.Add(AgregarBoton(Glifo.Guardar, "Guardar", "Guardar (Ctrl+S)", (_, _) => SaveActive()));
        _toolbarDocItems.Add(AgregarBoton(Glifo.GuardarTodo, "Guardar todo", "Guardar todo (Ctrl+Alt+S)", (_, _) => SaveAll()));

        _toolbar.Items.Add(new ToolStripSeparator());

        // ── Edición ──
        // Van en sus propias listas, no en _toolbarDocItems: dependen de lo que
        // la pestaña admita, no de que haya una pestaña.
        _toolbarDeshacerItems.Add(AgregarBoton(Glifo.Deshacer, "Deshacer", "Deshacer (Ctrl+Z)",
            (_, _) => PestanaActiva?.Deshacer()));
        _toolbarDeshacerItems.Add(AgregarBoton(Glifo.Rehacer, "Rehacer", "Rehacer (Ctrl+Y)",
            (_, _) => PestanaActiva?.Rehacer()));

        _toolbar.Items.Add(new ToolStripSeparator());

        _toolbarBusquedaItems.Add(AgregarBoton(Glifo.Buscar, "Buscar", "Buscar (Ctrl+F)", (_, _) => ShowFindReplace(false)));
        _toolbarBusquedaItems.Add(AgregarBoton(Glifo.Reemplazar, "Reemplazar", "Reemplazar (Ctrl+H)", (_, _) => ShowFindReplace(true)));

        _toolbar.Items.Add(new ToolStripSeparator());

        // ── Compilación ──
        _toolbarBuildItems.Add(AgregarBoton(Glifo.Compilar, "Compilar", "Compilar con NASM (F7)",
            async (_, _) => await CompileOnlyAsync()));
        _toolbarBuildItems.Add(AgregarBoton(Glifo.Enlazar, "Enlazar", "Enlazar (Ctrl+F7)",
            async (_, _) => await LinkOnlyAsync()));
        _toolbarBuildItems.Add(AgregarBoton(Glifo.Construir, "Compilar y enlazar", "Compilar y enlazar (Ctrl+Shift+B)",
            async (_, _) => await BuildOnlyAsync()));
        _toolbarBuildItems.Add(AgregarBoton(Glifo.Ejecutar, "Ejecutar", "Compilar, enlazar y ejecutar (F5)",
            async (_, _) => await BuildAndRunAsync()));

        _stopButton = AgregarBoton(Glifo.Detener, "Detener", "Detener la compilación (Shift+F5)",
            (_, _) => StopBuild());
        _stopButton.Enabled = false;

        _toolbar.Items.Add(new ToolStripSeparator());

        // ── Target ──
        _toolbar.Items.Add(new ToolStripLabel("Target:") { ForeColor = Tema.Texto2 });

        _targetCombo = new ToolStripComboBox
        {
            DropDownStyle = ComboBoxStyle.DropDownList,
            Width = 230,
            AutoSize = false,
            ToolTipText = "Configuración de compilación activa"
        };
        _targetCombo.ComboBox!.SelectedIndexChanged += (_, _) => OnTargetComboChanged();
        _toolbar.Items.Add(_targetCombo);

        AgregarBoton(Glifo.Configurar, "Administrar targets", "Administrar targets de compilación...",
            (_, _) => ShowTargetEditor());

        // ── A la derecha: vista ──
        var explorador = AgregarBoton(Glifo.Explorador, "Explorador", "Mostrar u ocultar el explorador (Ctrl+B)",
            (_, _) => ToggleExplorer());
        explorador.Alignment = ToolStripItemAlignment.Right;

        _temaBoton = AgregarBoton(Glifo.Tema, "Tema", "Cambiar entre tema claro y oscuro",
            (_, _) => CambiarTema(Tema.EsOscuro ? ModoTema.Claro : ModoTema.Oscuro));
        _temaBoton.Alignment = ToolStripItemAlignment.Right;

        ReloadTargetCombo();
    }

    /// <summary>Agrega un botón con su glifo dibujado y lo devuelve.</summary>
    private ToolStripButton AgregarBoton(Glifo glifo, string texto, string tooltip, EventHandler handler)
    {
        var b = new ToolStripButton
        {
            Text = texto,
            ToolTipText = tooltip,
            DisplayStyle = ToolStripItemDisplayStyle.Image,
            Image = IconosBarra.Dibujar(glifo, 18),
            ImageScaling = ToolStripItemImageScaling.None,
            ForeColor = Tema.Texto,
            AutoSize = false,
            Size = new Size(30, 26),
            // El glifo queda guardado para poder redibujarlo al cambiar de tema.
            Tag = glifo
        };

        b.Click += handler;
        _toolbar!.Items.Add(b);
        return b;
    }

    /// <summary>Redibuja los íconos de la barra con los colores del tema activo.</summary>
    private void RedibujarIconosBarra()
    {
        if (_toolbar is null) return;

        foreach (ToolStripItem item in _toolbar.Items)
        {
            if (item is ToolStripButton b && b.Tag is Glifo g)
            {
                b.Image?.Dispose();
                b.Image = IconosBarra.Dibujar(g, 18);
            }
        }
    }


    private void WireEvents()
    {
        _tabs.SelectedIndexChanged += OnTabChanged;
        _tabs.CierrePedido += CloseTabAt;

        _output.DoubleClick += Output_DoubleClick;
        _runner.OutputReceived += AppendOutput;
        _runner.LineReceived += OnBuildLine;

        _errorList.DiagnosticActivated += JumpToDiagnostic;

        _explorer.FileActivated += path => OpenPath(path);
        _explorer.AgregarAlProyectoPedido += AgregarArchivoAlProyecto;
        _explorer.QuitarDelProyectoPedido += QuitarArchivoDelProyecto;
        _explorer.MarcarPrincipalPedido += MarcarComoPrincipal;

        FormClosing += OnFormClosing;
    }

    private void CycleTab(int delta)
    {
        if (_tabs.TabPages.Count < 2) return;
        int next = (_tabs.SelectedIndex + delta + _tabs.TabPages.Count) % _tabs.TabPages.Count;
        _tabs.SelectedIndex = next;
    }

    private void OnTabChanged(object? sender, EventArgs e)
    {
        _documents.SetActive(_tabs.SelectedIndex);
        UpdateTitle();
        UpdateCaretLabel();
        _explorer.HighlightFile(Active?.FilePath);

        // La ventana de buscar debe operar sobre el editor de la pestaña activa.
        if (_findReplaceForm is not null && Active is not null)
        {
            _findReplaceForm.SetTarget(Active.Editor);
        }

        Active?.FocusEditor();
    }

    // ---------------------------------------------------------------
    // Documento activo
    // ---------------------------------------------------------------

    /// <summary>
    /// El documento de CÓDIGO activo, o null si la pestaña activa es de otro
    /// tipo (un diseñador) o no hay ninguna.
    ///
    /// ⚠ DEVUELVE NULL EN UNA PESTAÑA DE DISEÑO, Y ESO ESTÁ BIEN: lo usan las
    /// cosas que solo tienen sentido sobre texto (buscar, ir a una línea,
    /// autocompletar). Lo que vale para cualquier pestaña va por
    /// <see cref="PestanaActiva"/>.
    /// </summary>
    private AsmDocumentControl? Active =>
        _tabs.SelectedTab?.Controls.OfType<AsmDocumentControl>().FirstOrDefault();

    /// <summary>La pestaña activa, sea de código o de diseño.</summary>
    private IPestanaEditor? PestanaActiva =>
        _tabs.SelectedTab?.Controls.OfType<IPestanaEditor>().FirstOrDefault();

    private IPestanaEditor? PestanaEn(int index) =>
        index >= 0 && index < _tabs.TabPages.Count
            ? _tabs.TabPages[index].Controls.OfType<IPestanaEditor>().FirstOrDefault()
            : null;

    private AsmDocumentControl? DocumentAt(int index) =>
        index >= 0 && index < _tabs.TabPages.Count
            ? _tabs.TabPages[index].Controls.OfType<AsmDocumentControl>().FirstOrDefault()
            : null;

    /// <summary>
    /// TODAS las pestañas, de cualquier tipo.
    ///
    /// ⚠ TIENE QUE VERLAS TODAS: lo usan cerrar, guardar y la pregunta por los
    /// sucios al salir. Si dejara afuera las de diseño, un formulario con
    /// cambios se perdería sin que nadie preguntara nada.
    /// </summary>
    private IEnumerable<IPestanaEditor> AllDocuments =>
        _tabs.TabPages.Cast<TabPage>()
            .Select(p => p.Controls.OfType<IPestanaEditor>().FirstOrDefault())
            .Where(d => d is not null)!;

    /// <summary>Solo las pestañas de código, para lo que necesita el editor de texto.</summary>
    private IEnumerable<AsmDocumentControl> AllCodeDocuments =>
        _tabs.TabPages.Cast<TabPage>()
            .Select(p => p.Controls.OfType<AsmDocumentControl>().FirstOrDefault())
            .Where(d => d is not null)!;

    private AsmDocumentControl AddDocumentTab(string? filePath)
    {
        var doc = new AsmDocumentControl(filePath);
        var page = new TabPage(doc.State.DisplayName)
        {
            BackColor = Tema.CodigoFondo,
            Padding = new Padding(0)
        };

        page.Controls.Add(doc);

        doc.DirtyChanged += (_, _) => RefreshTabText(doc);
        doc.CaretMoved += (_, _) => { if (ReferenceEquals(doc, Active)) UpdateCaretLabel(); };

        _tabs.TabPages.Add(page);
        _documents.Add(doc.State);
        _tabs.SelectedTab = page;

        return doc;
    }

    /// <summary>
    /// Crea una pestaña con el diseñador de formularios.
    ///
    /// Comparte con <see cref="AddDocumentTab"/> el registro en
    /// <see cref="_documents"/>: para todo lo demás del editor es una pestaña
    /// como cualquier otra.
    /// </summary>
    private DisenadorControl AgregarPestanaDisenador(
        string? rutaAsmform, FormularioDisenado? formulario = null)
    {
        var dis = new DisenadorControl(rutaAsmform, formulario);

        var page = new TabPage(dis.State.DisplayName)
        {
            BackColor = Tema.Fondo,
            Padding = new Padding(0)
        };

        page.Controls.Add(dis);

        dis.DirtyChanged += (_, _) => RefreshTabText(dis);
        dis.GenerarPedido += d => GenerarCodigoDelFormulario(d);

        _tabs.TabPages.Add(page);
        _documents.Add(dis.State);
        _tabs.SelectedTab = page;

        ActualizarPantallaVacia();
        ActualizarComandosSegunDocumento();
        UpdateTitle();

        return dis;
    }

    private void RefreshTabText(IPestanaEditor doc)
    {
        if (doc is not Control control) return;

        var page = _tabs.TabPages.Cast<TabPage>()
            .FirstOrDefault(p => p.Controls.Contains(control));

        if (page is not null && page.Text != doc.State.DisplayName)
        {
            page.Text = doc.State.DisplayName;
        }

        // El título se recalcula siempre (no solo si cambió el texto de la pestaña):
        // el '*' del título depende del flag sucio, que puede cambiar sin cambiar el nombre.
        if (ReferenceEquals(doc, PestanaActiva))
        {
            UpdateTitle();
            UpdateCaretLabel();
        }

        _tabs.Invalidate();
    }

    private void UpdateTitle()
    {
        // ⚠ EL PROYECTO VA EN EL TÍTULO: con proyecto abierto, F7 compila su
        // archivo principal y no la pestaña que se está mirando. Si eso no se
        // ve en ningún lado, compilar parece hacer cosas al azar.
        var proyecto = _proyecto is null ? "" : $"[{_proyecto.Nombre}] ";

        // Por PestanaActiva: con Active, una pestaña de diseñador dejaba el
        // título como si no hubiera nada abierto.
        var doc = PestanaActiva;
        if (doc is null)
        {
            Text = $"{proyecto}Editor ASM (NASM + GoLink)";
            return;
        }

        var name = doc.FilePath ?? doc.State.DisplayName.TrimEnd('*', ' ');
        Text = $"{(doc.IsDirty ? "* " : "")}{proyecto}{name} - Editor ASM (NASM + GoLink)";
    }

    private void UpdateCaretLabel()
    {
        // Un diseñador no tiene cursor de texto: devuelve null y la barra queda
        // en blanco, en vez de mostrar una posición inventada.
        var pos = PestanaActiva?.PosicionCursor;

        _caretLabel.Text = pos is null ? "" : $"Ln {pos.Value.Linea}, Col {pos.Value.Columna}";
    }

    // ---------------------------------------------------------------
    // Archivo
    // ---------------------------------------------------------------

    private void NewFile()
    {
        var doc = AddDocumentTab(null);
        // LoadContent deja el documento limpio; el título y la pestaña se refrescan
        // después para no quedar con el '*' que puso el TextChanged de la carga.
        doc.LoadContent(DefaultTemplate, null);
        RefreshTabText(doc);
        ActualizarPantallaVacia();
        doc.FocusEditor();
    }

    private void OpenFileDialog()
    {
        using var dlg = new OpenFileDialog
        {
            InitialDirectory = SafeProjectFolder(),
            Filter = "Archivos ASM (*.asm;*.inc)|*.asm;*.inc|" +
                     "Proyecto del editor (*.asmproj)|*.asmproj|" +
                     "Todos los archivos (*.*)|*.*",
            Multiselect = true
        };

        if (dlg.ShowDialog(this) != DialogResult.OK) return;

        foreach (var file in dlg.FileNames) OpenPath(file);
    }

    /// <summary>
    /// Abre un archivo. Si ya está abierto en otra pestaña, activa esa en vez de duplicarlo.
    /// </summary>
    private void OpenPath(string path)
    {
        var extension = Path.GetExtension(path);

        // Un .asmproj no es un documento de texto: se abre como proyecto.
        if (extension.Equals(ArchivoProyecto.Extension, StringComparison.OrdinalIgnoreCase))
        {
            AbrirProyecto(path);
            return;
        }

        // Un .asmform tampoco: se abre en el diseñador. Abrirlo como texto
        // mostraría su JSON, que no es lo que nadie quiere ver ni editar.
        if (extension.Equals(ArchivoFormulario.Extension, StringComparison.OrdinalIgnoreCase))
        {
            AbrirFormulario(path);
            return;
        }

        int existing = _documents.IndexOfPath(path);
        if (existing >= 0)
        {
            _tabs.SelectedIndex = existing;
            Active?.FocusEditor();
            return;
        }

        string text;
        try
        {
            text = File.ReadAllText(path);
        }
        catch (Exception ex)
        {
            AppendOutput($"ERROR al abrir '{path}': {ex.Message}{Environment.NewLine}");
            return;
        }

        var doc = AddDocumentTab(path);
        doc.LoadContent(text, path);
        RefreshTabText(doc);
        UpdateTitle();
        RegistrarReciente(path);
        ActualizarPantallaVacia();
        _explorer.HighlightFile(path);
        doc.FocusEditor();

        OfrecerProyectoDe(path);
    }

    /// <summary>
    /// Si el archivo que se abrió pertenece a un proyecto y no hay ninguno
    /// abierto, lo ofrece.
    ///
    /// ⚠ SE PREGUNTA, NO SE ABRE SOLO. Abrir un proyecto cambia qué compila F7
    /// y qué targets hay: hacerlo sin avisar, solo porque se abrió un archivo,
    /// sería cambiarle el entorno al usuario a sus espaldas.
    /// </summary>
    private void OfrecerProyectoDe(string rutaArchivo)
    {
        if (_proyecto is not null) return;

        // ⚠ NO DURANTE EL ARRANQUE: restaurar la sesión llama a OpenPath por
        // cada archivo, y sin esta guarda el editor arrancaría preguntando una
        // vez por archivo antes de mostrarse.
        if (_abriendoAlIniciar) return;

        var candidato = ArchivoProyecto.BuscarProyectoDe(rutaArchivo);
        if (candidato is null) return;

        var r = MessageBox.Show(this,
            $"'{Path.GetFileName(rutaArchivo)}' está en la carpeta del proyecto " +
            $"'{Path.GetFileNameWithoutExtension(candidato)}'.{Environment.NewLine}{Environment.NewLine}" +
            $"¿Abrir el proyecto?{Environment.NewLine}{Environment.NewLine}" +
            $"Con el proyecto abierto, F7 compila su archivo principal y los " +
            $"targets salen de él.",
            "Abrir proyecto", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

        if (r == DialogResult.Yes) AbrirProyecto(candidato);
    }

    /// <summary>
    /// Ctrl+S. Guarda la pestaña activa, sea de código o de diseño: cada tipo
    /// sabe cómo guardarse (un .asm escribe texto; un formulario escribe su
    /// .asmform).
    /// </summary>
    private bool SaveActive()
    {
        var pestana = PestanaActiva;
        if (pestana is null) return false;
        if (pestana.FilePath is null) return SaveActiveAs();

        try
        {
            if (!pestana.Guardar()) return false;

            RefreshTabText(pestana);

            // Guardar un formulario regenera su .inc: si no, el código quedaría
            // describiendo un formulario que ya no es el que está en pantalla.
            if (pestana is DisenadorControl d) RegenerarInclude(d, avisar: false);

            return true;
        }
        catch (Exception ex)
        {
            AppendOutput($"ERROR al guardar: {ex.Message}{Environment.NewLine}");
            return false;
        }
    }

    private bool SaveActiveAs()
    {
        // Un formulario tiene su propio diálogo, con el filtro de .asmform.
        if (PestanaActiva is DisenadorControl disenador)
        {
            if (!disenador.GuardarComo(this)) return false;

            RefreshTabText(disenador);
            UpdateTitle();
            RegenerarInclude(disenador, avisar: false);

            if (disenador.FilePath is not null)
            {
                RegistrarReciente(disenador.FilePath);
                AgregarAlProyectoSiCorresponde(disenador.FilePath);
            }

            return true;
        }

        var doc = Active;
        if (doc is null) return false;

        var carpeta = doc.FilePath is not null
            ? Path.GetDirectoryName(doc.FilePath) ?? SafeProjectFolder()
            : SafeProjectFolder();

        using var dlg = new DialogoGuardar(
            carpeta,
            doc.FilePath is null ? "programa.asm" : Path.GetFileName(doc.FilePath));

        if (dlg.ShowDialog(this) != DialogResult.OK) return false;

        try
        {
            doc.SaveAs(dlg.RutaElegida);
            RefreshTabText(doc);
            UpdateTitle();
            RegistrarReciente(dlg.RutaElegida);
            _explorer.Refresh_();
            _explorer.HighlightFile(dlg.RutaElegida);
            return true;
        }
        catch (Exception ex)
        {
            AppendOutput($"ERROR al guardar: {ex.Message}{Environment.NewLine}");
            Dialogo.Error(this, "No se pudo guardar", ex.Message);
            return false;
        }
    }

    private void SaveAll()
    {
        int previo = _tabs.SelectedIndex;

        foreach (var doc in AllDocuments.Where(d => d.IsDirty).ToList())
        {
            var page = PaginaDe(doc);
            if (page is null) continue;

            // Se activa la pestaña y se guarda por la vía normal: así cada tipo
            // hace lo suyo (un formulario también regenera su .inc) sin repetir
            // esa lógica acá.
            _tabs.SelectedTab = page;

            try
            {
                if (doc.FilePath is null) SaveActiveAs();
                else SaveActive();
            }
            catch (Exception ex)
            {
                AppendOutput($"ERROR al guardar '{doc.FilePath}': {ex.Message}{Environment.NewLine}");
            }
        }

        if (previo >= 0 && previo < _tabs.TabPages.Count) _tabs.SelectedIndex = previo;
    }

    // ---------------------------------------------------------------
    // Cierre de pestañas
    // ---------------------------------------------------------------

    private void CloseActiveTab() => CloseTabAt(_tabs.SelectedIndex);

    private void CloseTabAt(int index)
    {
        // ⚠ POR IPestanaEditor, NO POR AsmDocumentControl: con el tipo concreto,
        // una pestaña de diseñador daba null acá y NO SE PODÍA CERRAR NUNCA.
        var doc = PestanaEn(index);
        if (doc is null) return;

        if (!ConfirmCloseDocument(doc, index)) return;

        var page = _tabs.TabPages[index];
        _tabs.TabPages.RemoveAt(index);
        _documents.RemoveAt(index);
        page.Dispose();
        (doc as Control)?.Dispose();

        // Se pueden cerrar TODAS las pestañas: antes se forzaba un documento
        // nuevo al cerrar la última, que era justo lo que no se quería.
        // Sin pestañas se muestra la pantalla de bienvenida.
        if (_tabs.TabPages.Count > 0)
        {
            _tabs.SelectedIndex = Math.Min(_documents.ActiveIndex, _tabs.TabPages.Count - 1);
        }

        ActualizarPantallaVacia();
        UpdateTitle();
        UpdateCaretLabel();
    }

    /// <summary>Cierra todas las pestañas, preguntando por cada una sin guardar.</summary>
    private void CloseAllTabs()
    {
        for (int i = _tabs.TabPages.Count - 1; i >= 0; i--)
        {
            var doc = DocumentAt(i);
            if (doc is null) continue;

            if (!ConfirmCloseDocument(doc, i)) return;   // el usuario canceló

            var page = _tabs.TabPages[i];
            _tabs.TabPages.RemoveAt(i);
            _documents.RemoveAt(i);
            page.Dispose();
            doc.Dispose();
        }

        ActualizarPantallaVacia();
        UpdateTitle();
        UpdateCaretLabel();
    }

    /// <summary>Cierra todas menos la activa.</summary>
    private void CloseOtherTabs()
    {
        var activa = Active;
        if (activa is null) return;

        for (int i = _tabs.TabPages.Count - 1; i >= 0; i--)
        {
            var doc = DocumentAt(i);
            if (doc is null || ReferenceEquals(doc, activa)) continue;

            if (!ConfirmCloseDocument(doc, i)) return;

            var page = _tabs.TabPages[i];
            _tabs.TabPages.RemoveAt(i);
            _documents.RemoveAt(i);
            page.Dispose();
            doc.Dispose();
        }

        ActualizarPantallaVacia();
        UpdateTitle();
    }

    /// <summary>
    /// Muestra u oculta la pantalla de bienvenida según haya o no pestañas.
    /// Sin esto, al cerrar la última quedaría un hueco gris sin explicación.
    /// </summary>
    private void ActualizarPantallaVacia()
    {
        bool vacio = _tabs.TabPages.Count == 0;

        _bienvenida.Visible = vacio;
        _tabs.Visible = !vacio;

        if (vacio)
        {
            // Los recientes se recargan cada vez que la pantalla aparece: el
            // archivo recién cerrado tiene que estar en la lista.
            _bienvenida.CargarRecientes(Ui.RecentFiles);
            _bienvenida.BringToFront();
        }

        // Sin documento no hay nada que compilar ni que buscar.
        ActualizarComandosSegunDocumento();
    }

    /// <summary>Habilita solo lo que tiene sentido con o sin documento abierto.</summary>
    private void ActualizarComandosSegunDocumento()
    {
        var pestana = PestanaActiva;

        // Guardar, cerrar y demás valen para CUALQUIER pestaña: un formulario
        // también se guarda y se cierra.
        bool hayDoc = pestana is not null;

        foreach (var item in _documentMenuItems) item.Enabled = hayDoc;
        foreach (var b in _toolbarDocItems) b.Enabled = hayDoc;

        // ⚠ Buscar y deshacer se preguntan, no se asumen: un diseñador no tiene
        // texto donde buscar, y cada pestaña declara si tiene deshacer propio.
        bool busqueda = pestana?.AdmiteBusqueda == true;
        bool deshacer = pestana?.AdmiteDeshacer == true;

        foreach (var item in _busquedaMenuItems) item.Enabled = busqueda;
        foreach (var item in _deshacerMenuItems) item.Enabled = deshacer;
        foreach (var b in _toolbarBusquedaItems) b.Enabled = busqueda;
        foreach (var b in _toolbarDeshacerItems) b.Enabled = deshacer;

        if (!_buildInProgress)
        {
            // ⚠ CON PROYECTO SE PUEDE COMPILAR SIN NINGUNA PESTAÑA ABIERTA: lo
            // que se ensambla es el archivo principal, que vive en el disco y no
            // necesita estar abierto. Atarlo a que haya documento dejaba F7
            // muerto al abrir un proyecto en una ventana limpia.
            bool hayQueCompilar = hayDoc || _proyecto?.RutaPrincipal is not null;

            foreach (var item in _buildMenuItems) item.Enabled = hayQueCompilar;
            foreach (var b in _toolbarBuildItems) b.Enabled = hayQueCompilar;
        }
    }

    /// <summary>
    /// Pregunta por un documento sucio: guardar, descartar o cancelar el cierre.
    /// Devuelve false si el usuario canceló.
    /// </summary>
    private bool ConfirmCloseDocument(IPestanaEditor doc, int index)
    {
        if (!doc.IsDirty) return true;

        _tabs.SelectedIndex = index;

        int res = Dialogo.Preguntar(this, "Cambios sin guardar",
            $"'{doc.State.DisplayName.TrimEnd('*', ' ')}' tiene cambios sin guardar.\n\n¿Qué querés hacer?",
            new[] { "Guardar", "Descartar", "Cancelar" },
            porDefecto: 0, Dialogo.Tono.Aviso);

        return res switch
        {
            0 => SaveActive(),
            1 => true,
            _ => false
        };
    }

    private void OnFormClosing(object? sender, FormClosingEventArgs e)
    {
        // Un solo diálogo para toda la salida, en vez de uno por archivo: con
        // varias pestañas sucias, encadenar preguntas es tedioso y se pierde de
        // vista cuántas faltan.
        var sucios = AllDocuments
            .Where(d => d.IsDirty)
            .Select(d => d.State.DisplayName.TrimEnd('*', ' '))
            .ToList();

        var eleccion = DialogoSalida.Preguntar(this, sucios);

        if (eleccion == DialogoSalida.Resultado.Cancelar)
        {
            e.Cancel = true;
            return;
        }

        if (eleccion == DialogoSalida.Resultado.GuardarYSalir && !GuardarTodoParaSalir())
        {
            // No se pudo guardar todo (o el usuario canceló un «guardar como»):
            // se queda abierto en vez de cerrar perdiendo trabajo.
            e.Cancel = true;
            return;
        }

        // El estado se guarda recién acá, cuando ya está decidido que se cierra:
        // si el usuario cancela, la sesión anterior queda intacta.
        GuardarGeometria();
        GuardarSesion();
        _settings.Save();

        // Tema.TemaCambiado es estático: sin darse de baja, el formulario queda
        // enganchado en el evento y no se libera.
        Tema.TemaCambiado -= AplicarTema;
    }

    /// <summary>
    /// Guarda todos los documentos con cambios antes de salir. Devuelve false si
    /// alguno no se pudo guardar, para no cerrar perdiendo ese trabajo.
    ///
    /// Los documentos sin archivo abren «Guardar como»: cancelar ahí aborta la
    /// salida, que es lo que el usuario está pidiendo al cancelar.
    /// </summary>
    private bool GuardarTodoParaSalir()
    {
        foreach (var doc in AllDocuments.Where(d => d.IsDirty).ToList())
        {
            var page = PaginaDe(doc);
            if (page is not null) _tabs.SelectedTab = page;

            bool guardado = doc.FilePath is null ? SaveActiveAs() : SaveActive();
            if (!guardado) return false;
        }

        return true;
    }

    /// <summary>La TabPage que contiene esa pestaña, o null si ya no está.</summary>
    private TabPage? PaginaDe(IPestanaEditor pestana) =>
        pestana is Control c
            ? _tabs.TabPages.Cast<TabPage>().FirstOrDefault(p => p.Controls.Contains(c))
            : null;

    // ---------------------------------------------------------------
    // Explorador
    // ---------------------------------------------------------------

    private void ToggleExplorer()
    {
        _outerSplit.Panel1Collapsed = !_outerSplit.Panel1Collapsed;
        if (_explorerToggle is not null)
        {
            _explorerToggle.Checked = !_outerSplit.Panel1Collapsed;
        }
    }

    // ---------------------------------------------------------------
    // Salida
    // ---------------------------------------------------------------

    private void AppendOutput(string text)
    {
        if (_output.InvokeRequired) _output.Invoke(() => AppendOutputInternal(text));
        else AppendOutputInternal(text);
    }

    private void AppendOutputInternal(string text)
    {
        _output.SelectionStart = _output.TextLength;
        _output.SelectionLength = 0;
        _output.SelectionColor = NasmErrorParser.LooksLikeProblem(text) ? Color.OrangeRed : Color.LightGray;
        _output.AppendText(text);
        _output.ScrollToCaret();
    }

    /// <summary>
    /// Cada línea de salida de las herramientas pasa por el parser; los diagnósticos
    /// que salgan van al panel de errores. Llega desde el hilo del proceso, así que
    /// hay que saltar al hilo de la interfaz antes de tocar los controles.
    /// </summary>
    private void OnBuildLine(string line)
    {
        if (InvokeRequired) { BeginInvoke(() => OnBuildLineInternal(line)); }
        else { OnBuildLineInternal(line); }
    }

    private void OnBuildLineInternal(string line)
    {
        foreach (var d in _parser.Feed(line))
        {
            _errorList.Add(d);
        }

        UpdateStatusFromDiagnostics();
    }

    /// <summary>Salta al código desde el panel de errores, abriendo el archivo si hace falta.</summary>
    private void JumpToDiagnostic(BuildDiagnostic diagnostic)
    {
        if (!diagnostic.HasLocation) return;

        var resolved = ResolveReferencePath(diagnostic.FileName!);

        if (resolved is not null && File.Exists(resolved) &&
            !PathComparer.SamePath(Active?.FilePath, resolved))
        {
            OpenPath(resolved);
        }

        // ⚠ Active da null si la pestaña activa es un diseñador: ahí no hay
        // líneas a las que saltar y el salto simplemente no ocurre, en vez de
        // fallar. Es el comportamiento correcto — un error de NASM apunta a un
        // .asm, no a un formulario.
        Active?.GoToLine(diagnostic.Line!.Value);
    }

    private void Output_DoubleClick(object? sender, EventArgs e)
    {
        int lineIndex = _output.GetLineFromCharIndex(_output.SelectionStart);
        if (lineIndex < 0 || lineIndex >= _output.Lines.Length) return;

        JumpToError(_output.Lines[lineIndex]);
    }

    /// <summary>
    /// Salta a la línea del error. Si el error pertenece a otro archivo, lo abre
    /// (o activa su pestaña) en vez de descartar el salto.
    /// </summary>
    private void JumpToError(string outputLine)
    {
        var reference = NasmErrorParser.Parse(outputLine);
        if (reference is null) return;

        var doc = Active;

        bool esOtroArchivo = doc?.FilePath is not null &&
                             !PathComparer.SamePath(doc.FilePath, ResolveReferencePath(reference.FileName));

        if (esOtroArchivo)
        {
            var resolved = ResolveReferencePath(reference.FileName);
            if (resolved is not null && File.Exists(resolved))
            {
                OpenPath(resolved);
                doc = Active;
            }
        }

        doc?.GoToLine(reference.LineNumber);
    }

    /// <summary>
    /// NASM puede reportar rutas relativas: las resolvemos contra la carpeta del
    /// documento activo y, si no, contra la carpeta del proyecto.
    /// </summary>
    private string? ResolveReferencePath(string fileName)
    {
        if (Path.IsPathRooted(fileName)) return fileName;

        var activeDir = Active?.FilePath is not null ? Path.GetDirectoryName(Active.FilePath) : null;
        if (activeDir is not null)
        {
            var candidate = Path.Combine(activeDir, fileName);
            if (File.Exists(candidate)) return candidate;
        }

        var projectCandidate = Path.Combine(SafeProjectFolder(), fileName);
        return File.Exists(projectCandidate) ? projectCandidate : fileName;
    }

    // ---------------------------------------------------------------
    // Buscar / Reemplazar
    // ---------------------------------------------------------------

    private void ShowFindReplace(bool showReplace)
    {
        var doc = Active;
        if (doc is null) return;

        _findReplaceForm ??= new FindReplaceForm(doc.Editor);
        _findReplaceForm.SetTarget(doc.Editor);
        _findReplaceForm.SetReplaceVisible(showReplace);

        if (!_findReplaceForm.Visible) _findReplaceForm.Show(this);
        _findReplaceForm.FocusFindBox();
    }

    // ---------------------------------------------------------------
    // Compilación / enlazado / ejecución
    //
    // ⚠ QUÉ SE COMPILA DEPENDE DE SI HAY PROYECTO ABIERTO:
    //
    //   sin proyecto  → la pestaña activa, y la salida al lado del fuente.
    //                   Es el comportamiento de siempre y NO PUEDE CAMBIAR:
    //                   los .asm sueltos y los scripts de afuera dependen de él.
    //
    //   con proyecto  → el archivo principal declarado, y la salida donde el
    //                   proyecto diga. Así mirar un .inc y apretar F7 no
    //                   ensambla el .inc.
    //
    // Las tres propiedades de abajo son el ÚNICO lugar donde se decide esto:
    // los seis puntos que compilan, enlazan y ejecutan las usan y no saben si
    // hay proyecto.
    // ---------------------------------------------------------------

    /// <summary>El fuente que se ensambla: el principal del proyecto, o la pestaña activa.</summary>
    private string? SourcePath => _proyecto?.RutaPrincipal ?? Active?.FilePath;

    private string? ObjPath => RutaDeSalida(".obj");
    private string? ExePath => RutaDeSalida(".exe");

    private string? RutaDeSalida(string extension)
    {
        var fuente = SourcePath;
        if (fuente is null) return null;

        return _proyecto is null
            ? Path.ChangeExtension(fuente, extension)
            : _proyecto.RutaDeSalida(fuente, extension);
    }

    /// <summary>
    /// Target con el que se compila: el del proyecto si hay uno (que puede
    /// heredar los globales), o el elegido en la barra.
    /// </summary>
    private BuildTarget CurrentTarget =>
        _proyecto?.TargetEfectivo(_settings.Targets) ?? _settings.ActiveTarget;

    /// <summary>
    /// Deja en disco lo que se va a compilar.
    ///
    /// ⚠ CON PROYECTO NO ALCANZA CON GUARDAR LA PESTAÑA ACTIVA: lo que se
    /// ensambla es el archivo principal, que puede estar en otra pestaña —o en
    /// ninguna—, y sus .inc también pueden estar abiertos y sucios. Si se
    /// guardara solo la activa, NASM leería del disco una versión vieja y los
    /// cambios no aparecerían en el .exe, sin ningún aviso.
    /// </summary>
    private bool EnsureSaved()
    {
        if (_proyecto is not null) return GuardarPestanasSucias();

        var doc = Active;
        if (doc is null) return false;
        if (doc.FilePath is null) return SaveActiveAs();
        if (doc.IsDirty) return SaveActive();
        return true;
    }

    /// <summary>
    /// Guarda todas las pestañas con cambios que ya tengan archivo. Devuelve
    /// false si alguna no se pudo guardar.
    ///
    /// Las pestañas sin archivo (un «Sin título» que nunca se guardó) se dejan
    /// como están: no forman parte del proyecto, así que no participan de su
    /// compilación, y abrir un diálogo de guardar por cada una al apretar F7
    /// sería una interrupción sin sentido.
    /// </summary>
    private bool GuardarPestanasSucias()
    {
        int previo = _documents.ActiveIndex;

        try
        {
            for (int i = 0; i < _documents.Count; i++)
            {
                var doc = _documents.Documents[i];
                if (!doc.IsDirty || doc.FilePath is null) continue;

                _documents.SetActive(i);
                _tabs.SelectedIndex = i;

                if (!SaveActive()) return false;
            }
        }
        finally
        {
            if (previo >= 0 && previo < _documents.Count)
            {
                _documents.SetActive(previo);
                _tabs.SelectedIndex = previo;
            }
        }

        return true;
    }

    /// <summary>
    /// Prepara una compilación: bloquea otra simultánea, limpia paneles y arma el token.
    /// Devuelve false si ya hay una corriendo o si no se pudo guardar el archivo.
    /// </summary>
    private bool BeginBuild(bool clearOutput)
    {
        if (_buildInProgress)
        {
            AppendOutput("Ya hay una compilación en curso. Detenela con Shift+F5." + Environment.NewLine);
            return false;
        }

        if (!EnsureSaved()) return false;

        _buildInProgress = true;
        _buildCts = new CancellationTokenSource();

        if (clearOutput) _output.Clear();
        _errorList.Clear();
        _parser.Clear();

        UpdateBuildUiState();
        return true;
    }

    private void EndBuild()
    {
        // Cierra cualquier bloque de diagnóstico que quedara abierto (GoLink multilínea).
        foreach (var d in _parser.Flush()) _errorList.Add(d);

        _buildCts?.Dispose();
        _buildCts = null;
        _buildInProgress = false;

        UpdateBuildUiState();
        UpdateStatusFromDiagnostics();
    }

    /// <summary>Habilita o deshabilita los comandos según haya o no una compilación en curso.</summary>
    private void UpdateBuildUiState()
    {
        if (_stopMenuItem is not null) _stopMenuItem.Enabled = _buildInProgress;
        if (_stopButton is not null) _stopButton.Enabled = _buildInProgress;
        if (_targetCombo is not null) _targetCombo.Enabled = !_buildInProgress;

        // Mientras compila, los comandos de compilar se apagan; al terminar solo
        // vuelven si hay un documento sobre el cual compilar.
        bool habilitar = !_buildInProgress && Active is not null;

        foreach (var item in _buildMenuItems) item.Enabled = habilitar;
        foreach (var b in _toolbarBuildItems) b.Enabled = habilitar;
    }

    private void UpdateStatusFromDiagnostics()
    {
        _statusLabel.Text = _errorList.SummaryText;
    }

    /// <summary>Detiene la compilación en curso matando el proceso externo.</summary>
    private void StopBuild()
    {
        if (!_buildInProgress || _buildCts is null) return;

        try { _buildCts.Cancel(); }
        catch (ObjectDisposedException) { /* terminó justo ahora */ }
    }

    private async Task<bool> RunToolAsync(string exePath, string arguments, string workingDir, BuildTool tool)
    {
        _parser.CurrentTool = tool;
        var token = _buildCts?.Token ?? CancellationToken.None;

        var result = await _runner.RunAsync(exePath, arguments, workingDir, token);
        return result.Succeeded;
    }

    private async Task CompileOnlyAsync()
    {
        if (!BeginBuild(clearOutput: true)) return;
        try
        {
            await CompileStepAsync();
        }
        finally
        {
            EndBuild();
        }
    }

    private async Task<bool> CompileStepAsync()
    {
        var target = CurrentTarget;
        var file = SourcePath!;

        // ⚠ EL DIRECTORIO DE TRABAJO ES EL DEL FUENTE: los %include se resuelven
        // contra él, no contra la ubicación del .asm. Un formulario del
        // diseñador incluye su .inc sin ruta y desde otra carpeta falla con
        // «unable to open include file».
        var dir = Path.GetDirectoryName(file)!;

        var ensamblador = RutaDelEnsamblador(target);
        if (ensamblador is null) return false;

        // La carpeta de salida del proyecto puede no existir todavía; NASM no
        // la crea y falla con un error de apertura que no dice cuál es el problema.
        _proyecto?.AsegurarCarpetaDeSalida();

        var args = target.BuildAssemblerArguments(file, ObjPath!);
        var herramienta = target.UsaMasm ? BuildTool.Masm : BuildTool.Nasm;

        return await RunToolAsync(ensamblador, args, dir, herramienta);
    }

    /// <summary>
    /// Dónde está el ensamblador del target. NASM sale de la configuración;
    /// MASM se busca solo (primero en masm_x64/masm_x86 del proyecto, después en
    /// Visual Studio) salvo que el target fije una ruta.
    ///
    /// Devuelve null y explica en la salida si no aparece, en vez de dejar que
    /// el proceso falle con un mensaje del sistema.
    /// </summary>
    private string? RutaDelEnsamblador(BuildTarget target)
    {
        if (!target.UsaMasm)
        {
            if (File.Exists(_settings.NasmPath)) return _settings.NasmPath;

            AppendOutput($"ERROR: no se encontró NASM en '{_settings.NasmPath}'. " +
                         $"Revisá Configuración > Rutas de herramientas.{Environment.NewLine}");
            return null;
        }

        // La precedencia (target > configuración global > automático) vive en
        // ToolResolver, no acá: así la compilación, la verificación y la vista
        // previa usan exactamente la misma cadena.
        var masm = ToolResolver.Assembler(target, _settings.Config);

        if (masm is not null && File.Exists(masm)) return masm;

        var carpeta = target.Arch == TargetArch.Win32 ? "masm_x86" : "masm_x64";
        var exe = target.Arch == TargetArch.Win32 ? "ml.exe" : "ml64.exe";

        AppendOutput(
            $"ERROR: no se encontró {exe} (MASM para {target.ArchText}). " +
            $"Esperado en '{Path.Combine(SafeProjectFolder(), carpeta)}' " +
            $"o en una instalación de Visual Studio.{Environment.NewLine}");

        return null;
    }

    private async Task LinkOnlyAsync()
    {
        var doc = Active;
        if (doc?.FilePath is null)
        {
            AppendOutput("Primero guardá y compilá el archivo." + Environment.NewLine);
            return;
        }

        if (!BeginBuild(clearOutput: true)) return;
        try
        {
            await LinkStepAsync();
        }
        finally
        {
            EndBuild();
        }
    }

    private async Task<bool> LinkStepAsync()
    {
        var target = CurrentTarget;

        if (!target.ProducesExecutable)
        {
            AppendOutput($"El target '{target.Name}' solo genera .obj: no se enlaza." + Environment.NewLine);
            return true;
        }

        if (ObjPath is null || !File.Exists(ObjPath))
        {
            AppendOutput($"No se encontró el .obj. Compilá primero (F7).{Environment.NewLine}");
            return false;
        }

        var dir = Path.GetDirectoryName(SourcePath!)!;

        if (target.Linker == LinkerKind.MsvcLink)
        {
            return await LinkWithMsvcAsync(target, dir);
        }

        var args = target.BuildGoLinkArguments(ObjPath);
        return await RunToolAsync(_settings.GoLinkPath, args, dir, BuildTool.GoLink);
    }

    /// <summary>
    /// Enlaza con el link.exe de MSVC. La ruta del enlazador y la de los .lib del SDK
    /// se resuelven solas si el target no las fija: se busca primero en las carpetas
    /// linker_x64 / linker_x86 del proyecto, y después en Visual Studio.
    /// </summary>
    private async Task<bool> LinkWithMsvcAsync(BuildTarget target, string workingDir)
    {
        var linker = ToolResolver.Linker(target, _settings.Config);

        if (linker is null || !File.Exists(linker))
        {
            AppendOutput(
                $"ERROR: no se encontró el link.exe de MSVC para {target.ArchText}. " +
                $"Esperado en '{Path.Combine(SafeProjectFolder(), target.Arch == TargetArch.Win32 ? "linker_x86" : "linker_x64")}' " +
                $"o en una instalación de Visual Studio.{Environment.NewLine}");
            return false;
        }

        var sdk = ToolResolver.SdkLib(target, _settings.Config);

        if (sdk is null)
        {
            AppendOutput(
                $"ADVERTENCIA: no se encontraron las librerías del SDK de Windows para {target.ArchText}. " +
                $"El enlazado puede fallar con LNK1181.{Environment.NewLine}");
        }

        var args = target.BuildMsvcArguments(ObjPath!, ExePath!, sdk);
        return await RunToolAsync(linker, args, workingDir, BuildTool.MsvcLink);
    }

    private async Task<bool> BuildAsync()
    {
        if (!await CompileStepAsync()) return false;
        if (WasCanceled) return false;
        return await LinkStepAsync();
    }

    private bool WasCanceled => _buildCts?.IsCancellationRequested ?? false;

    private async Task BuildOnlyAsync()
    {
        if (!BeginBuild(clearOutput: true)) return;
        try
        {
            await BuildAsync();
        }
        finally
        {
            EndBuild();
        }
    }

    private async Task BuildAndRunAsync()
    {
        if (!BeginBuild(clearOutput: true)) return;

        bool ok;
        try
        {
            ok = await BuildAsync();
        }
        finally
        {
            EndBuild();
        }

        if (ok && CurrentTarget.ProducesExecutable) RunExecutable();
    }

    private void RunExecutable()
    {
        if (!CurrentTarget.ProducesExecutable)
        {
            AppendOutput($"El target '{CurrentTarget.Name}' no genera ejecutable." + Environment.NewLine);
            return;
        }

        var exe = ExePath;
        if (exe is null || !File.Exists(exe))
        {
            AppendOutput("No se encontró el ejecutable. Compilá y enlazá primero." + Environment.NewLine);
            return;
        }

        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = exe,
                WorkingDirectory = Path.GetDirectoryName(exe)!,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            AppendOutput($"ERROR al ejecutar: {ex.Message}{Environment.NewLine}");
        }
    }

    // ---------------------------------------------------------------
    // Targets
    // ---------------------------------------------------------------

    /// <summary>
    /// Llena el desplegable con los targets que valen: los del proyecto si hay
    /// uno abierto (que pueden ser los globales heredados), o los globales.
    /// </summary>
    private void ReloadTargetCombo()
    {
        if (_targetCombo is null) return;

        _suppressTargetChange = true;
        _targetCombo.Items.Clear();

        var lista = _proyecto?.TargetsEfectivos(_settings.Targets) ?? _settings.Targets;

        foreach (var t in lista)
        {
            _targetCombo.Items.Add(t.DisplayName);
        }

        if (_targetCombo.Items.Count > 0)
        {
            var indice = _proyecto?.TargetActivo ?? _settings.ActiveTargetIndex;
            _targetCombo.SelectedIndex = Math.Clamp(indice, 0, _targetCombo.Items.Count - 1);
        }

        // Con proyecto, el desplegable muestra OTRA lista que la de la
        // configuración general: se dice de dónde sale, o parece que el editor
        // perdió los targets.
        _targetCombo.ToolTipText = _proyecto is null
            ? "Target de compilación"
            : _proyecto.HeredaTargets
                ? $"Targets de la configuración general (el proyecto '{_proyecto.Nombre}' los hereda)"
                : $"Targets propios del proyecto '{_proyecto.Nombre}'";

        _suppressTargetChange = false;
    }

    private void OnTargetComboChanged()
    {
        if (_suppressTargetChange || _targetCombo is null) return;
        if (_targetCombo.SelectedIndex < 0) return;

        // Con proyecto, la elección se guarda EN EL PROYECTO: es suya, y
        // escribirla en la configuración general se la impondría a todos los
        // demás proyectos y a los archivos sueltos.
        if (_proyecto is not null)
        {
            _proyecto.TargetActivo = _targetCombo.SelectedIndex;
            GuardarProyecto();
            return;
        }

        _settings.ActiveTargetIndex = _targetCombo.SelectedIndex;
        _settings.Save();
    }

    private void ShowTargetEditor()
    {
        using var form = new TargetEditorForm(_settings);
        if (form.ShowDialog(this) != DialogResult.OK) return;

        _settings = form.Settings;
        _settings.Save();
        ReloadTargetCombo();
    }

    // ---------------------------------------------------------------
    // Proyectos
    //
    // ⚠ TODO ESTO ES OPCIONAL. Sin proyecto abierto el editor funciona como
    // siempre: abre archivos sueltos, compila la pestaña activa y deja la
    // salida al lado del fuente.
    // ---------------------------------------------------------------

    private void NuevoProyecto()
    {
        using var dlg = new SaveFileDialog
        {
            Title = "Nuevo proyecto",
            Filter = "Proyecto del editor (*.asmproj)|*.asmproj",
            InitialDirectory = SafeProjectFolder(),
            FileName = "MiPrograma" + ArchivoProyecto.Extension
        };

        if (dlg.ShowDialog(this) != DialogResult.OK) return;

        try
        {
            var nombre = Path.GetFileNameWithoutExtension(dlg.FileName);

            // Los archivos que ya estén abiertos y guardados entran al proyecto:
            // es lo que el usuario espera si venía trabajando en ellos.
            var abiertos = _documents.Documents
                .Where(d => d.FilePath is not null)
                .Select(d => d.FilePath!)
                .ToList();

            var p = ArchivoProyecto.Crear(dlg.FileName, nombre, abiertos);

            UsarProyecto(p);

            AppendOutput($"Proyecto '{p.Nombre}' creado en {dlg.FileName}{Environment.NewLine}");
        }
        catch (Exception ex)
        {
            MessageBox.Show(this,
                $"No se pudo crear el proyecto:{Environment.NewLine}{Environment.NewLine}{ex.Message}",
                "Nuevo proyecto", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private void AbrirProyectoDialogo()
    {
        using var dlg = new OpenFileDialog
        {
            Title = "Abrir proyecto",
            Filter = "Proyecto del editor (*.asmproj)|*.asmproj|Todos los archivos (*.*)|*.*",
            InitialDirectory = SafeProjectFolder()
        };

        if (dlg.ShowDialog(this) != DialogResult.OK) return;

        AbrirProyecto(dlg.FileName);
    }

    /// <summary>
    /// Abre un proyecto y sus archivos. Si falla, el editor queda como estaba:
    /// sin proyecto es un estado válido, así que no hay nada que revertir.
    /// </summary>
    private void AbrirProyecto(string ruta)
    {
        try
        {
            var p = ArchivoProyecto.Cargar(ruta);

            UsarProyecto(p);

            // Los problemas se informan pero NO impiden abrir: un proyecto al
            // que le falta un archivo se sigue pudiendo editar, y esconderlo
            // sería peor.
            var problemas = p.Validar();

            if (problemas.Count > 0)
            {
                AppendOutput($"El proyecto '{p.Nombre}' tiene avisos:{Environment.NewLine}");
                foreach (var x in problemas) AppendOutput($"  - {x}{Environment.NewLine}");
            }
        }
        catch (Exception ex)
        {
            Ui.RemoveRecentProject(ruta);
            ReconstruirMenuProyectosRecientes();

            MessageBox.Show(this,
                $"No se pudo abrir el proyecto:{Environment.NewLine}{Environment.NewLine}{ex.Message}",
                "Abrir proyecto", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    /// <summary>Deja un proyecto como el activo y actualiza todo lo que depende de él.</summary>
    private void UsarProyecto(ProyectoAsm p)
    {
        _proyecto = p;

        if (p.RutaArchivo is not null)
        {
            Ui.ProyectoAbierto = p.RutaArchivo;
            Ui.AddRecentProject(p.RutaArchivo);
            ReconstruirMenuProyectosRecientes();
        }

        GuardarProyecto();

        _explorer.MostrarProyecto(p);
        ReloadTargetCombo();
        ActualizarEstadoDelProyecto();
        UpdateTitle();
    }

    private void CerrarProyecto()
    {
        if (_proyecto is null) return;

        GuardarProyecto();

        var nombre = _proyecto.Nombre;
        _proyecto = null;
        Ui.ProyectoAbierto = null;

        // Se vuelve al árbol de carpeta: sin proyecto, el explorador muestra la
        // carpeta configurada, como hacía antes de que esto existiera.
        _explorer.SetRoot(SafeProjectFolder());
        ReloadTargetCombo();
        ActualizarEstadoDelProyecto();
        UpdateTitle();

        AppendOutput($"Proyecto '{nombre}' cerrado.{Environment.NewLine}");
    }

    /// <summary>
    /// Escribe el .asmproj si hay proyecto. Se llama después de cada cambio:
    /// son archivos chicos y perder la lista por no haber guardado sería
    /// desproporcionado.
    /// </summary>
    private void GuardarProyecto()
    {
        if (_proyecto?.RutaArchivo is null) return;

        try
        {
            ArchivoProyecto.Guardar(_proyecto.RutaArchivo, _proyecto);
        }
        catch (Exception ex)
        {
            AppendOutput($"ERROR al guardar el proyecto: {ex.Message}{Environment.NewLine}");
        }
    }

    private void AgregarArchivoAlProyecto()
    {
        if (_proyecto is null) return;

        using var dlg = new OpenFileDialog
        {
            Title = "Agregar al proyecto",
            Filter = "Archivos del editor (*.asm;*.inc;*.asmform)|*.asm;*.inc;*.asmform|Todos los archivos (*.*)|*.*",
            InitialDirectory = _proyecto.Carpeta ?? SafeProjectFolder(),
            Multiselect = true
        };

        if (dlg.ShowDialog(this) != DialogResult.OK) return;

        int agregados = 0;
        foreach (var f in dlg.FileNames)
        {
            if (_proyecto.Agregar(f)) agregados++;
        }

        if (agregados == 0)
        {
            AppendOutput($"Esos archivos ya estaban en el proyecto.{Environment.NewLine}");
            return;
        }

        // Si el proyecto no tenía qué compilar y entró un .asm, se toma ese.
        if (string.IsNullOrWhiteSpace(_proyecto.ArchivoPrincipal))
        {
            var asm = dlg.FileNames.FirstOrDefault(
                f => Path.GetExtension(f).Equals(".asm", StringComparison.OrdinalIgnoreCase));

            if (asm is not null) _proyecto.MarcarComoPrincipal(asm);
        }

        GuardarProyecto();
        _explorer.MostrarProyecto(_proyecto);
        ActualizarEstadoDelProyecto();
    }

    /// <summary>Saca un archivo del proyecto. NO lo borra del disco.</summary>
    private void QuitarArchivoDelProyecto(string ruta)
    {
        if (_proyecto is null) return;

        var nombre = Path.GetFileName(ruta);

        var r = MessageBox.Show(this,
            $"¿Sacar '{nombre}' del proyecto?{Environment.NewLine}{Environment.NewLine}" +
            $"El archivo NO se borra del disco: sigue estando en la carpeta.",
            "Quitar del proyecto", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

        if (r != DialogResult.Yes) return;

        if (_proyecto.Quitar(ruta))
        {
            GuardarProyecto();
            _explorer.MostrarProyecto(_proyecto);
            ActualizarEstadoDelProyecto();
        }
    }

    private void MarcarComoPrincipal(string ruta)
    {
        if (_proyecto is null) return;

        if (_proyecto.MarcarComoPrincipal(ruta))
        {
            GuardarProyecto();
            _explorer.MostrarProyecto(_proyecto);
            ActualizarEstadoDelProyecto();

            AppendOutput($"'{Path.GetFileName(ruta)}' es ahora el archivo que compila F7.{Environment.NewLine}");
        }
    }

    private void MostrarPropiedadesDelProyecto()
    {
        if (_proyecto is null) return;

        using var form = new ProyectoPropiedadesForm(_proyecto, _settings.Targets);

        if (form.ShowDialog(this) != DialogResult.OK) return;

        GuardarProyecto();
        _explorer.MostrarProyecto(_proyecto);
        ReloadTargetCombo();
        ActualizarEstadoDelProyecto();
        UpdateTitle();
    }

    /// <summary>Habilita o deshabilita lo que depende de que haya proyecto.</summary>
    private void ActualizarEstadoDelProyecto()
    {
        foreach (var item in _proyectoMenuItems) item.Enabled = _proyecto is not null;

        // Abrir o cerrar un proyecto cambia si hay algo que compilar, aunque no
        // se haya tocado ninguna pestaña.
        ActualizarComandosSegunDocumento();
    }

    private void ReconstruirMenuProyectosRecientes()
    {
        if (_proyectosRecientesMenu is null) return;

        _proyectosRecientesMenu.DropDownItems.Clear();

        if (Ui.RecentProjects.Count == 0)
        {
            _proyectosRecientesMenu.DropDownItems.Add(
                new ToolStripMenuItem("(ninguno)") { Enabled = false });
            return;
        }

        foreach (var ruta in Ui.RecentProjects)
        {
            var copia = ruta;
            _proyectosRecientesMenu.DropDownItems.Add(
                new ToolStripMenuItem(Path.GetFileNameWithoutExtension(ruta))
                {
                    ToolTipText = ruta
                });

            ((ToolStripMenuItem)_proyectosRecientesMenu.DropDownItems[^1]).Click +=
                (_, _) => AbrirProyecto(copia);
        }

        _proyectosRecientesMenu.DropDownItems.Add(new ToolStripSeparator());
        _proyectosRecientesMenu.DropDownItems.Add(
            new ToolStripMenuItem("&Vaciar la lista", null, (_, _) =>
            {
                Ui.ClearRecentProjects();
                _settings.Save();
                ReconstruirMenuProyectosRecientes();
            }));
    }

    // ---------------------------------------------------------------
    // Diseñador de formularios
    // ---------------------------------------------------------------

    /// <summary>
    /// Abre una pestaña con un formulario nuevo.
    ///
    /// El formulario arranca con la arquitectura del target activo: casi
    /// siempre es la que se quiere, y equivocarse ahí genera código con la
    /// convención de llamada de la otra arquitectura.
    /// </summary>
    private void MostrarDisenador()
    {
        var f = new FormularioDisenado { Arquitectura = CurrentTarget.Arch };

        var dis = AgregarPestanaDisenador(null, f);
        dis.TomarFoco();
    }

    /// <summary>
    /// Abre un .asmform en una pestaña de diseñador. Si ya está abierto, activa
    /// esa pestaña en vez de duplicarlo.
    /// </summary>
    private void AbrirFormulario(string ruta)
    {
        int existente = _documents.IndexOfPath(ruta);

        if (existente >= 0)
        {
            _tabs.SelectedIndex = existente;
            PestanaActiva?.TomarFoco();
            return;
        }

        try
        {
            var formulario = ArchivoFormulario.Cargar(ruta);

            var dis = AgregarPestanaDisenador(ruta, formulario);
            dis.TomarFoco();

            RegistrarReciente(ruta);
            _explorer.HighlightFile(ruta);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this,
                $"No se pudo abrir el formulario:{Environment.NewLine}{Environment.NewLine}{ex.Message}",
                "Abrir formulario", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    /// <summary>
    /// Escribe el .inc y —si no existe— el .asm de un formulario, y los abre.
    ///
    /// ⚠ EL .asm NO SE PISA NUNCA: si ya existe es del usuario. Cuando hay
    /// controles nuevos sin manejador, el texto se copia al portapapeles para
    /// que lo pegue él, en vez de meterle código en su archivo.
    /// </summary>
    private void GenerarCodigoDelFormulario(DisenadorControl dis)
    {
        var errores = dis.Formulario.Validar();

        if (errores.Count > 0)
        {
            MessageBox.Show(this,
                "El formulario tiene problemas que impiden generar el código:" +
                Environment.NewLine + Environment.NewLine +
                string.Join(Environment.NewLine, errores.Select(e => "• " + e)),
                "Generar código", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        // Sin archivo no hay dónde poner lo generado: se pide primero.
        if (dis.FilePath is null && !SaveActiveAs()) return;

        try
        {
            dis.Guardar();
            RefreshTabText(dis);

            var r = ArchivoFormulario.GenerarArchivos(dis.FilePath!, dis.Formulario);

            AppendOutput($"Generado: {Path.GetFileName(r.RutaInclude)}{Environment.NewLine}");

            AgregarAlProyectoSiCorresponde(dis.FilePath!);
            AgregarAlProyectoSiCorresponde(r.RutaInclude);

            if (r.SeCreoElAsm)
            {
                AppendOutput($"Creado: {Path.GetFileName(r.RutaAsm)} (es tuyo, el diseñador no lo vuelve a tocar){Environment.NewLine}");
                AgregarAlProyectoSiCorresponde(r.RutaAsm);

                // El .asm nuevo es el que se compila, salvo que el proyecto ya
                // tenga uno elegido.
                if (_proyecto is not null && string.IsNullOrWhiteSpace(_proyecto.ArchivoPrincipal))
                {
                    _proyecto.MarcarComoPrincipal(r.RutaAsm);
                    GuardarProyecto();
                }
            }

            if (r.HayManejadoresPendientes)
            {
                try
                {
                    Clipboard.SetText(r.ManejadoresFaltantes);
                    AppendOutput($"Hay controles nuevos sin manejador: el código para pegarlos " +
                                 $"quedó en el portapapeles.{Environment.NewLine}");
                }
                catch (Exception)
                {
                    AppendOutput($"Hay controles nuevos sin manejador (no se pudo usar el portapapeles).{Environment.NewLine}");
                }
            }

            OpenPath(r.RutaAsm);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this,
                $"No se pudo generar el código:{Environment.NewLine}{Environment.NewLine}{ex.Message}",
                "Generar código", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    /// <summary>
    /// Regenera el .inc de un formulario ya guardado, sin tocar el .asm.
    ///
    /// Se llama al guardar: el .inc describe el formulario, y dejarlo viejo
    /// haría que el código compilado no se corresponda con lo que está en
    /// pantalla.
    /// </summary>
    private void RegenerarInclude(DisenadorControl dis, bool avisar)
    {
        if (dis.FilePath is null) return;
        if (dis.Formulario.Validar().Count > 0) return;

        try
        {
            var r = ArchivoFormulario.GenerarArchivos(dis.FilePath, dis.Formulario);

            if (avisar) AppendOutput($"Regenerado: {Path.GetFileName(r.RutaInclude)}{Environment.NewLine}");

            RefrescarSiEstaAbierto(r.RutaInclude);
        }
        catch (Exception ex)
        {
            AppendOutput($"ERROR al regenerar el .inc: {ex.Message}{Environment.NewLine}");
        }
    }

    /// <summary>
    /// Si un archivo que se acaba de reescribir en disco está abierto en una
    /// pestaña, la recarga.
    ///
    /// ⚠ HACE FALTA PORQUE EL DISEÑADOR YA NO ES MODAL: con el modal, nadie
    /// podía tener el .inc abierto mientras se regeneraba. Ahora sí, y sin esto
    /// la pestaña seguiría mostrando la versión vieja.
    ///
    /// Una pestaña con cambios sin guardar NO se pisa: se avisa y se deja al
    /// usuario decidir.
    /// </summary>
    private void RefrescarSiEstaAbierto(string ruta)
    {
        var doc = AllCodeDocuments.FirstOrDefault(d => PathComparer.SamePath(d.FilePath, ruta));
        if (doc is null) return;

        if (doc.IsDirty)
        {
            AppendOutput($"AVISO: '{Path.GetFileName(ruta)}' se regeneró en el disco, pero la " +
                         $"pestaña tiene cambios sin guardar y no se recargó.{Environment.NewLine}");
            return;
        }

        try
        {
            doc.LoadContent(File.ReadAllText(ruta), ruta);
            RefreshTabText(doc);
        }
        catch (Exception ex)
        {
            AppendOutput($"No se pudo recargar '{Path.GetFileName(ruta)}': {ex.Message}{Environment.NewLine}");
        }
    }

    /// <summary>Suma un archivo al proyecto abierto, si hay uno y no estaba.</summary>
    private void AgregarAlProyectoSiCorresponde(string ruta)
    {
        if (_proyecto is null) return;
        if (!_proyecto.Agregar(ruta)) return;

        GuardarProyecto();
        _explorer.MostrarProyecto(_proyecto);
    }

    // ---------------------------------------------------------------
    // Configuración
    // ---------------------------------------------------------------

    private void ShowSettings()
    {
        using var form = new SettingsForm(_settings);
        if (form.ShowDialog(this) == DialogResult.OK)
        {
            _settings = form.Settings;
            _settings.Save();
            _explorer.SetRoot(SafeProjectFolder());
            ReloadTargetCombo();
        }
    }

    private void CheckTools()
    {
        var texto = ToolReport.Construir(CurrentTarget, RutasDelTarget(CurrentTarget));

        Dialogo.Aviso(this, "Verificación de herramientas", texto);
    }

    /// <summary>
    /// Dónde está cada herramienta que el target va a usar de verdad. Es la
    /// misma resolución que hace la compilación —primero lo que fije el target,
    /// después las carpetas del proyecto, después Visual Studio— pero SIN
    /// escribir en el panel de salida: acá el resultado va a un diálogo.
    ///
    /// Devuelve null en lo que no aparezca; el informe lo reporta como faltante.
    /// </summary>
    private ToolReport.Rutas RutasDelTarget(BuildTarget t)
    {
        var cfg = _settings.Config;

        // La cadena de precedencia sale de ToolResolver; acá solo se comprueba
        // que lo resuelto exista de verdad en el disco, que es lo que el
        // informe reporta como OK o MAL.
        var ensamblador = ToolResolver.Assembler(t, cfg);
        if (ensamblador is not null && !File.Exists(ensamblador)) ensamblador = null;

        var enlazador = ToolResolver.Linker(t, cfg);
        if (enlazador is not null && !File.Exists(enlazador)) enlazador = null;

        return new ToolReport.Rutas
        {
            Ensamblador = ensamblador,
            Enlazador = enlazador,
            SdkLib = ToolResolver.SdkLib(t, cfg),
            FuenteEnsamblador = ToolResolver.SourceOfAssembler(t, cfg),
            FuenteEnlazador = ToolResolver.SourceOfLinker(t, cfg),
            FuenteSdk = ToolResolver.SourceOfSdk(t, cfg),
            ToolchainsDisponibles =
                LinkerLocator.DiscoverToolchains(t.Arch, SafeProjectFolder()).Count,
            SdksDisponibles = LinkerLocator.DiscoverSdks(t.Arch).Count
        };
    }

    private string SafeProjectFolder() =>
        Directory.Exists(_settings.ProjectFolder) ? _settings.ProjectFolder : AppContext.BaseDirectory;

    // ---------------------------------------------------------------
    // Persistencia entre sesiones
    // ---------------------------------------------------------------

    private UiState Ui => _settings.Config.Ui;

    /// <summary>
    /// Comprueba que existan las herramientas del target activo y lo avisa en la
    /// barra de estado. Es mejor enterarse al abrir que al apretar F7 y que la
    /// compilación falle por una ruta mal configurada.
    /// </summary>
    private void VerificarHerramientasAlIniciar()
    {
        var faltantes = new List<string>();
        var target = _settings.ActiveTarget;

        // Solo se avisa del ensamblador que el target activo va a usar: quejarse
        // de que falta MASM cuando se compila con NASM sería ruido.
        if (target.UsaMasm)
        {
            if (LinkerLocator.FindMasm(target.Arch, SafeProjectFolder()) is null)
            {
                faltantes.Add($"MASM ({target.ArchText})");
            }
        }
        else if (!File.Exists(_settings.NasmPath))
        {
            faltantes.Add("NASM");
        }

        if (target.ProducesExecutable)
        {
            if (target.Linker == LinkerKind.GoLink)
            {
                if (!File.Exists(_settings.GoLinkPath)) faltantes.Add("GoLink");
            }
            else if (LinkerLocator.FindMsvcLinker(target.Arch, SafeProjectFolder()) is null)
            {
                faltantes.Add($"link.exe de MSVC ({target.ArchText})");
            }
        }

        if (faltantes.Count > 0)
        {
            _statusLabel.Text = "Falta: " + string.Join(", ", faltantes) +
                                "  —  revisá Configuración > Rutas de herramientas";
            _statusLabel.ForeColor = Tema.Aviso;
        }
    }

    /// <summary>
    /// Restaura la geometría guardada, pero SOLO si sigue siendo alcanzable con
    /// los monitores de ahora. Un monitor desconectado o una resolución distinta
    /// dejarían la ventana fuera de la pantalla sin forma de agarrarla.
    /// </summary>
    private void RestaurarGeometria()
    {
        var g = Ui.Window;

        if (!g.Saved)
        {
            // Primera vez: tamaño cómodo y centrada.
            Size = new Size(g.Width, g.Height);
            StartPosition = FormStartPosition.CenterScreen;
            return;
        }

        var areas = Screen.AllScreens
            .Select(s => new ScreenRect(s.WorkingArea.X, s.WorkingArea.Y,
                                        s.WorkingArea.Width, s.WorkingArea.Height))
            .ToList();

        if (!g.EsVisibleEn(areas))
        {
            // Quedó fuera de alcance: se vuelve al centro con el tamaño por defecto.
            StartPosition = FormStartPosition.CenterScreen;
            return;
        }

        StartPosition = FormStartPosition.Manual;
        Location = new Point(g.X, g.Y);
        Size = new Size(g.Width, g.Height);

        if (g.Maximized) WindowState = FormWindowState.Maximized;
    }

    /// <summary>
    /// Guarda la geometría. ⚠ Si está maximizada o minimizada, Bounds devuelve el
    /// tamaño de ese estado, no el que tenía como ventana normal: hay que usar
    /// RestoreBounds, o al reabrir aparecería del tamaño de la pantalla entera.
    /// </summary>
    private void GuardarGeometria()
    {
        var r = WindowState == FormWindowState.Normal ? Bounds : RestoreBounds;

        Ui.Window.X = r.X;
        Ui.Window.Y = r.Y;
        Ui.Window.Width = r.Width;
        Ui.Window.Height = r.Height;
        Ui.Window.Maximized = WindowState == FormWindowState.Maximized;
        Ui.Window.Saved = true;
    }

    /// <summary>Anota qué archivos quedan abiertos, para reabrirlos la próxima vez.</summary>
    private void GuardarSesion()
    {
        Ui.OpenFiles = AllDocuments
            .Select(d => d.FilePath)
            .Where(p => !string.IsNullOrWhiteSpace(p))
            .Select(p => p!)
            .ToList();

        Ui.ActiveFileIndex = Math.Max(0, _tabs.SelectedIndex);

        // El proyecto ya se anota al abrirlo y al cerrarlo, pero se confirma acá
        // para que el estado guardado no dependa de que aquello haya pasado.
        Ui.ProyectoAbierto = _proyecto?.RutaArchivo;
    }

    /// <summary>
    /// Abre lo que corresponda al arrancar: lo de la línea de comandos manda;
    /// si no, lo que había abierto la sesión anterior.
    /// </summary>
    private void AbrirAlIniciar(string[]? argumentos, FormSplash? splash = null)
    {
        _abriendoAlIniciar = true;

        try
        {
            AbrirAlIniciarInterno(argumentos, splash);
        }
        finally
        {
            _abriendoAlIniciar = false;
        }
    }

    private void AbrirAlIniciarInterno(string[]? argumentos, FormSplash? splash)
    {
        bool abrioAlguno = false;

        if (argumentos is not null)
        {
            foreach (var file in argumentos)
            {
                if (string.IsNullOrWhiteSpace(file) || !File.Exists(file)) continue;
                splash?.Informar($"Abriendo {Path.GetFileName(file)}...");
                OpenPath(file);
                abrioAlguno = true;
            }
        }

        if (!abrioAlguno && Ui.RestoreSession)
        {
            foreach (var file in Ui.OpenFiles.ToList())
            {
                if (!File.Exists(file)) continue;
                splash?.Informar($"Restaurando {Path.GetFileName(file)}...");
                OpenPath(file);
                abrioAlguno = true;
            }

            if (abrioAlguno && Ui.ActiveFileIndex < _tabs.TabPages.Count)
            {
                _tabs.SelectedIndex = Ui.ActiveFileIndex;
            }
        }

        // Sin nada que abrir se muestra la bienvenida, no un documento forzado:
        // el editor arranca igual que queda al cerrar todas las pestañas.
        ActualizarPantallaVacia();
        if (!abrioAlguno) _bienvenida.CargarRecientes(Ui.RecentFiles);
    }

    // ---------------------------------------------------------------
    // Archivos recientes
    // ---------------------------------------------------------------

    private void RegistrarReciente(string? path)
    {
        if (string.IsNullOrWhiteSpace(path)) return;

        Ui.AddRecent(path);
        ReconstruirMenuRecientes();
        _settings.Save();
    }

    private void ReconstruirMenuRecientes()
    {
        if (_recientesMenu is null) return;

        _recientesMenu.DropDownItems.Clear();

        if (Ui.RecentFiles.Count == 0)
        {
            var vacio = new ToolStripMenuItem("(ninguno)") { Enabled = false };
            _recientesMenu.DropDownItems.Add(vacio);
            return;
        }

        int i = 1;
        foreach (var ruta in Ui.RecentFiles)
        {
            // El número al principio da un atajo por teclado (Alt, R, 1).
            var item = new ToolStripMenuItem($"&{i} {Path.GetFileName(ruta)}")
            {
                ToolTipText = ruta,
                Tag = ruta
            };
            item.Click += (s, _) =>
            {
                if (s is ToolStripMenuItem m && m.Tag is string p) AbrirReciente(p);
            };
            _recientesMenu.DropDownItems.Add(item);
            i++;
        }

        _recientesMenu.DropDownItems.Add(new ToolStripSeparator());
        var limpiar = new ToolStripMenuItem("&Vaciar la lista", null, (_, _) =>
        {
            Ui.ClearRecent();
            ReconstruirMenuRecientes();
            _bienvenida.CargarRecientes(Ui.RecentFiles);
            _settings.Save();
        });
        _recientesMenu.DropDownItems.Add(limpiar);
    }

    /// <summary>
    /// Abre un reciente. Si el archivo ya no está, se lo saca de la lista en vez
    /// de dejar una entrada que falla cada vez que se la toca.
    /// </summary>
    private void AbrirReciente(string path)
    {
        if (!File.Exists(path))
        {
            Ui.RemoveRecent(path);
            ReconstruirMenuRecientes();
            _bienvenida.CargarRecientes(Ui.RecentFiles);
            _settings.Save();

            Dialogo.Aviso(this, "Archivo no encontrado",
                $"'{Path.GetFileName(path)}' ya no está en:\n{Path.GetDirectoryName(path)}\n\n" +
                "Se quitó de la lista de recientes.");
            return;
        }

        OpenPath(path);
    }

    // ---------------------------------------------------------------
    // Tema
    // ---------------------------------------------------------------

    private void CambiarTema(ModoTema modo)
    {
        Tema.Modo = modo;
        Ui.Theme = modo == ModoTema.Claro ? ModoTemaGuardado.Claro : ModoTemaGuardado.Oscuro;
        _settings.Save();
        ActualizarMarcaDeTema();
    }

    private void ActualizarMarcaDeTema()
    {
        if (_temaOscuroItem is not null) _temaOscuroItem.Checked = Tema.EsOscuro;
        if (_temaClaroItem is not null) _temaClaroItem.Checked = !Tema.EsOscuro;
    }

    /// <summary>
    /// Aplica la paleta a la ventana y a los controles que se pintan por
    /// propiedades. Los controles propios (pestañas, panel de errores,
    /// bienvenida) se suscriben solos a Tema.TemaCambiado.
    /// </summary>
    private void AplicarTema()
    {
        BackColor = Tema.Fondo;
        ForeColor = Tema.Texto;

        _output.BackColor = Tema.SalidaFondo;
        _output.ForeColor = Tema.SalidaTexto;

        if (_toolbar is not null)
        {
            _toolbar.BackColor = Tema.Superficie2;
            _toolbar.Renderer = new RendererBarras();
        }

        // ⚠ EL RENDERER, NO SOLO EL BackColor: el ToolStripRenderer por defecto
        // pinta su propio fondo encima y la barra queda con el color del sistema.
        _statusStrip.BackColor = Tema.Superficie2;
        _statusStrip.ForeColor = Tema.Texto;
        _statusStrip.Renderer = new RendererBarras();
        _statusLabel.ForeColor = Tema.Texto2;
        _caretLabel.ForeColor = Tema.Texto2;

        if (MainMenuStrip is not null)
        {
            MainMenuStrip.BackColor = Tema.Superficie2;
            MainMenuStrip.ForeColor = Tema.Texto;
            MainMenuStrip.Renderer = new RendererBarras();

            // ⚠ CADA ÍTEM POR SEPARADO: los ToolStripMenuItem NO heredan el
            // ForeColor del MenuStrip, así que sin esto el menú queda con texto
            // negro sobre fondo oscuro. Hay que bajar por todo el árbol, porque
            // los submenús tampoco lo heredan de su padre.
            foreach (ToolStripItem item in MainMenuStrip.Items) PintarItemDeMenu(item);
        }

        if (_toolbar is not null)
        {
            foreach (ToolStripItem item in _toolbar.Items)
            {
                item.ForeColor = Tema.Texto;
                if (item is ToolStripComboBox combo)
                {
                    combo.BackColor = Tema.Superficie3;
                    combo.ForeColor = Tema.Texto;
                }
            }

            // Los íconos están dibujados con el color del tema anterior: hay que
            // rehacerlos o quedan claros sobre fondo claro.
            RedibujarIconosBarra();
        }

        _bottomTabs.BackColor = Tema.Superficie;

        ActualizarMarcaDeTema();

        // ⚠ Invalidate(true) NO alcanza para los ToolStrip: pintan con su
        // renderer en un ciclo propio y hay que pedirles el repintado a cada uno.
        MainMenuStrip?.Refresh();
        _toolbar?.Refresh();
        _statusStrip.Refresh();

        Invalidate(true);
        Update();
    }

    /// <summary>
    /// Pinta un ítem de menú y sus submenús. Se hace en una pasada recursiva
    /// porque los ToolStripMenuItem no heredan colores de su contenedor.
    /// </summary>
    private static void PintarItemDeMenu(ToolStripItem item)
    {
        item.BackColor = Tema.Superficie2;
        item.ForeColor = item.Enabled ? Tema.Texto : Tema.Texto3;

        if (item is ToolStripMenuItem menu)
        {
            foreach (ToolStripItem hijo in menu.DropDownItems) PintarItemDeMenu(hijo);
        }
    }

    /// <summary>
    /// Renderer de las barras (menú, herramientas, estado).
    ///
    /// ⚠ EL ProfessionalRenderer SOLO NO ALCANZA: aunque se le pase una
    /// ColorTable, sigue pintando el fondo de la barra y el borde con el estilo
    /// del sistema, y sobre un tema propio se ve el parche. Por eso se
    /// sobrescriben los tres métodos de fondo y se pinta a mano.
    /// </summary>
    private sealed class RendererBarras : ToolStripProfessionalRenderer
    {
        public RendererBarras() : base(new MenuColores()) { }

        protected override void OnRenderToolStripBackground(ToolStripRenderEventArgs e)
        {
            using var b = new SolidBrush(Tema.Superficie2);
            e.Graphics.FillRectangle(b, e.AffectedBounds);
        }

        protected override void OnRenderToolStripBorder(ToolStripRenderEventArgs e)
        {
            // Una línea sola separando la barra del contenido; el borde completo
            // del sistema se ve como un marco de más.
            using var p = new Pen(Tema.LineaSuave);
            var r = e.AffectedBounds;
            e.Graphics.DrawLine(p, r.Left, r.Bottom - 1, r.Right, r.Bottom - 1);
        }

        protected override void OnRenderMenuItemBackground(ToolStripItemRenderEventArgs e)
        {
            var r = new Rectangle(Point.Empty, e.Item.Size);

            if (e.Item.Selected || (e.Item is ToolStripMenuItem m && m.DropDown.Visible))
            {
                using var b = new SolidBrush(Tema.Seleccion);
                e.Graphics.FillRectangle(b, r);
            }
            else
            {
                using var b = new SolidBrush(Tema.Superficie2);
                e.Graphics.FillRectangle(b, r);
            }
        }

        protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
        {
            e.TextColor = e.Item.Enabled ? Tema.Texto : Tema.Texto3;
            base.OnRenderItemText(e);
        }

        protected override void OnRenderSeparator(ToolStripSeparatorRenderEventArgs e)
        {
            using var p = new Pen(Tema.LineaSuave);
            var r = e.Item.Bounds;
            int y = r.Height / 2;
            e.Graphics.DrawLine(p, 4, y, r.Width - 4, y);
        }

        protected override void OnRenderButtonBackground(ToolStripItemRenderEventArgs e)
        {
            var r = new Rectangle(Point.Empty, e.Item.Size);
            var boton = e.Item as ToolStripButton;

            Color fondo = (boton?.Checked ?? false) ? Tema.Seleccion
                        : e.Item.Pressed ? Tema.Realzar(Tema.Superficie2, 24)
                        : e.Item.Selected ? Tema.Realzar(Tema.Superficie2, 14)
                        : Tema.Superficie2;

            using var b = new SolidBrush(fondo);
            e.Graphics.FillRectangle(b, r);
        }
    }

    /// <summary>Colores del menú principal, que WinForms no toma de BackColor.</summary>
    private sealed class MenuColores : ProfessionalColorTable
    {
        public override Color MenuStripGradientBegin => Tema.Superficie2;
        public override Color MenuStripGradientEnd => Tema.Superficie2;
        public override Color MenuItemSelected => Tema.Seleccion;
        public override Color MenuItemSelectedGradientBegin => Tema.Seleccion;
        public override Color MenuItemSelectedGradientEnd => Tema.Seleccion;
        public override Color MenuItemBorder => Tema.LineaSuave;
        public override Color MenuBorder => Tema.LineaSuave;
        public override Color ToolStripDropDownBackground => Tema.Superficie2;
        public override Color ImageMarginGradientBegin => Tema.Superficie2;
        public override Color ImageMarginGradientMiddle => Tema.Superficie2;
        public override Color ImageMarginGradientEnd => Tema.Superficie2;
        public override Color MenuItemPressedGradientBegin => Tema.Superficie3;
        public override Color MenuItemPressedGradientEnd => Tema.Superficie3;
        public override Color SeparatorDark => Tema.LineaSuave;
        public override Color SeparatorLight => Tema.LineaSuave;
    }

    // ---------------------------------------------------------------
    // Ayuda
    // ---------------------------------------------------------------

    private void MostrarAcercaDe() => FormAcercaDe.Mostrar(this, _settings);

    /// <summary>
    /// Abre el manual de NASM. Primero busca el PDF que viene con la
    /// instalación —junto a nasm.exe o en la carpeta del proyecto—; si no está,
    /// va a la documentación en línea.
    /// </summary>
    private void AbrirDocumentacionNasm()
    {
        var candidatos = new List<string>();

        var dirNasm = Path.GetDirectoryName(_settings.NasmPath);
        if (!string.IsNullOrEmpty(dirNasm))
        {
            candidatos.Add(Path.Combine(dirNasm, "nasmdoc.pdf"));
        }

        candidatos.Add(Path.Combine(SafeProjectFolder(), "nasmdoc.pdf"));

        foreach (var pdf in candidatos)
        {
            if (!File.Exists(pdf)) continue;

            try
            {
                Process.Start(new ProcessStartInfo { FileName = pdf, UseShellExecute = true });
                return;
            }
            catch
            {
                // Sin lector de PDF asociado: se sigue con el respaldo web.
            }
        }

        AbrirEnNavegador("https://www.nasm.us/docs.php");
    }

    private void AbrirEnNavegador(string url)
    {
        try
        {
            Process.Start(new ProcessStartInfo { FileName = url, UseShellExecute = true });
        }
        catch (Exception ex)
        {
            Dialogo.Error(this, "No se pudo abrir el enlace", ex.Message);
        }
    }

    // ---------------------------------------------------------------
    // Plantilla inicial
    // ---------------------------------------------------------------

    private const string DefaultTemplate =
        "; Nuevo archivo .asm\n" +
        "default rel\n" +
        "extern ExitProcess\n\n" +
        "section .text\n" +
        "global main\n\n" +
        "main:\n" +
        "    sub rsp, 40\n" +
        "    xor ecx, ecx\n" +
        "    call ExitProcess\n";
}
