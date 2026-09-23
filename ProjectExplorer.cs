using AsmEditor.Core.Proyecto;

namespace AsmEditor;

/// <summary>
/// El panel de la izquierda. Tiene DOS MODOS, según haya proyecto abierto:
///
///   - <b>Carpeta</b> (<see cref="SetRoot"/>): árbol del disco, con carga
///     perezosa de subcarpetas. Es el modo de siempre y el que se usa cuando
///     se trabaja con archivos sueltos.
///
///   - <b>Proyecto</b> (<see cref="MostrarProyecto"/>): la LISTA EXPLÍCITA de
///     archivos del .asmproj. No muestra lo que hay en la carpeta y no está en
///     la lista, que es justamente el sentido de una lista explícita.
///
/// ⚠ EL MODO CARPETA NO SE TOCA. Es de lo que depende el editor sin proyecto,
/// que tiene que seguir funcionando igual que antes.
/// </summary>
public class ProjectExplorer : UserControl
{
    private readonly TreeView _tree = new();
    private readonly Label _header = new();

    /// <summary>El proyecto mostrado, o null si está en modo carpeta.</summary>
    private ProyectoAsm? _proyecto;

    /// <summary>Se pide sacar ese archivo del proyecto (no borrarlo del disco).</summary>
    public event Action<string>? QuitarDelProyectoPedido;

    /// <summary>Se pide marcar ese archivo como el que compila F7.</summary>
    public event Action<string>? MarcarPrincipalPedido;

    /// <summary>Se pide agregar un archivo al proyecto.</summary>
    public event Action? AgregarAlProyectoPedido;

    private static readonly string[] SourceExtensions = { ".asm", ".inc", ".s" };
    private static readonly string[] OtherExtensions =
        { ".obj", ".exe", ".bat", ".ps1", ".txt", ".md", ".asmproj", ".asmform" };

    /// <summary>Marcador para saber que una carpeta todavía no se leyó.</summary>
    private const string PlaceholderTag = "__placeholder__";

    public string? RootFolder { get; private set; }

    /// <summary>Se dispara al hacer doble clic (o Enter) sobre un archivo.</summary>
    public event Action<string>? FileActivated;

    public ProjectExplorer()
    {
        BuildLayout();
        WireEvents();
        Tema.TemaCambiado += AplicarTema;
    }

    /// <summary>
    /// Repinta con la paleta activa. El árbol se recarga porque el color de cada
    /// nodo se fija al crearlo: no hay forma de actualizarlos sin rehacerlos.
    /// </summary>
    private void AplicarTema()
    {
        BackColor = Tema.Superficie;
        _header.BackColor = Tema.Superficie2;
        _header.ForeColor = Tema.Texto2;
        _tree.BackColor = Tema.Superficie;
        _tree.ForeColor = Tema.Texto;

        Refresh_();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) Tema.TemaCambiado -= AplicarTema;
        base.Dispose(disposing);
    }

    private void BuildLayout()
    {
        Dock = DockStyle.Fill;
        BackColor = Tema.Superficie;

        _header.Dock = DockStyle.Top;
        _header.Height = 22;
        _header.TextAlign = ContentAlignment.MiddleLeft;
        _header.Padding = new Padding(6, 0, 0, 0);
        _header.BackColor = Tema.Superficie2;
        _header.ForeColor = Tema.Texto2;
        _header.Text = "Proyecto";

        _tree.Dock = DockStyle.Fill;
        _tree.BackColor = Tema.Superficie;
        _tree.ForeColor = Tema.Texto;
        _tree.BorderStyle = BorderStyle.None;
        _tree.Font = new Font("Segoe UI", 9f);
        _tree.HideSelection = false;
        _tree.ShowLines = true;
        _tree.PathSeparator = Path.DirectorySeparatorChar.ToString();

        var menu = new ContextMenuStrip();

        // Los ítems se arman en cada apertura: cuáles tienen sentido depende
        // del modo y de sobre qué nodo se hizo clic.
        menu.Opening += (_, _) => ArmarMenuContextual(menu);

        _tree.ContextMenuStrip = menu;

        Controls.Add(_tree);
        Controls.Add(_header);
    }

    /// <summary>
    /// Arma el menú contextual según el modo y el nodo bajo el cursor.
    ///
    /// En modo carpeta queda como estaba siempre; en modo proyecto se agregan
    /// las acciones sobre la lista.
    /// </summary>
    private void ArmarMenuContextual(ContextMenuStrip menu)
    {
        menu.Items.Clear();

        // El clic derecho no cambia la selección por sí solo: se la fija acá,
        // o el menú actuaría sobre el nodo seleccionado antes, que no es el
        // que el usuario está señalando.
        var punto = _tree.PointToClient(Cursor.Position);
        var nodo = _tree.GetNodeAt(punto);
        if (nodo is not null) _tree.SelectedNode = nodo;

        var ruta = nodo?.Tag as string;
        bool esArchivo = ruta is not null && !Directory.Exists(ruta);

        if (_proyecto is not null)
        {
            menu.Items.Add("Agregar archivo al proyecto...", null,
                (_, _) => AgregarAlProyectoPedido?.Invoke());

            if (esArchivo)
            {
                menu.Items.Add(new ToolStripSeparator());

                var principal = menu.Items.Add("Compilar este archivo (marcar como principal)", null,
                    (_, _) => MarcarPrincipalPedido?.Invoke(ruta!));

                // Solo un .asm se puede ensamblar: ofrecerlo sobre un .inc
                // llevaría a un error de NASM difícil de atribuir.
                principal.Enabled = Path.GetExtension(ruta!)
                    .Equals(".asm", StringComparison.OrdinalIgnoreCase);

                menu.Items.Add("Quitar del proyecto", null,
                    (_, _) => QuitarDelProyectoPedido?.Invoke(ruta!));
            }

            menu.Items.Add(new ToolStripSeparator());
        }

        menu.Items.Add("Actualizar", null, (_, _) => Refresh_());
        menu.Items.Add("Abrir carpeta en el Explorador", null, (_, _) => OpenInShell());
    }

    private void WireEvents()
    {
        _tree.BeforeExpand += OnBeforeExpand;
        _tree.NodeMouseDoubleClick += (_, e) => Activate(e.Node);
        _tree.KeyDown += (_, e) =>
        {
            if (e.KeyCode == Keys.Enter && _tree.SelectedNode is not null)
            {
                Activate(_tree.SelectedNode);
                e.Handled = true;
            }
            else if (e.KeyCode == Keys.F5)
            {
                Refresh_();
                e.Handled = true;
            }
        };
    }

    /// <summary>
    /// Apunta el árbol a una carpeta y la carga. Sale del modo proyecto si
    /// estaba en él.
    /// </summary>
    public void SetRoot(string? folder)
    {
        _proyecto = null;
        RootFolder = folder;
        Refresh_();
    }

    /// <summary>
    /// Muestra la lista de archivos de un proyecto en vez de una carpeta.
    /// </summary>
    public void MostrarProyecto(ProyectoAsm proyecto)
    {
        _proyecto = proyecto;
        RootFolder = proyecto.Carpeta;
        Refresh_();
    }

    public void Refresh_()
    {
        if (_proyecto is not null)
        {
            RefrescarProyecto();
            return;
        }

        _tree.BeginUpdate();
        _tree.Nodes.Clear();

        if (string.IsNullOrWhiteSpace(RootFolder) || !Directory.Exists(RootFolder))
        {
            _header.Text = "Proyecto (carpeta no encontrada)";
            _tree.EndUpdate();
            return;
        }

        _header.Text = "Proyecto: " + Path.GetFileName(RootFolder.TrimEnd(
            Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));

        // El nodo raíz no repite el texto de la cabecera: muestra solo el nombre de la carpeta.
        var rootName = Path.GetFileName(RootFolder.TrimEnd(
            Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
        var root = new TreeNode(rootName) { Tag = RootFolder, ForeColor = Tema.Acento };
        _tree.Nodes.Add(root);
        PopulateDirectory(root, RootFolder);
        root.Expand();

        _tree.EndUpdate();
    }

    /// <summary>
    /// Dibuja el árbol con la lista del proyecto.
    ///
    /// Los archivos se agrupan por subcarpeta para que un proyecto con inc\ y
    /// src\ se lea, pero las carpetas que se muestran salen de la LISTA, no del
    /// disco: no se enumera nada.
    /// </summary>
    private void RefrescarProyecto()
    {
        if (_proyecto is null) return;

        _tree.BeginUpdate();
        _tree.Nodes.Clear();

        _header.Text = "Proyecto: " + _proyecto.Nombre;

        var raiz = new TreeNode(_proyecto.Nombre)
        {
            Tag = _proyecto.Carpeta,
            ForeColor = Tema.Acento
        };

        _tree.Nodes.Add(raiz);

        // Un nodo por subcarpeta que aparezca en la lista; "" es la raíz.
        var porCarpeta = _proyecto.ListarArchivos()
            .GroupBy(a => a.SubCarpeta)
            .OrderBy(g => g.Key, StringComparer.OrdinalIgnoreCase);

        foreach (var grupo in porCarpeta)
        {
            var destino = raiz;

            if (!string.IsNullOrEmpty(grupo.Key))
            {
                destino = new TreeNode(grupo.Key) { ForeColor = Tema.Acento };
                raiz.Nodes.Add(destino);
            }

            foreach (var archivo in grupo.OrderBy(a => a.NombreDeArchivo, StringComparer.OrdinalIgnoreCase))
            {
                destino.Nodes.Add(NodoDeArchivo(archivo));
            }
        }

        raiz.ExpandAll();

        if (_proyecto.Archivos.Count == 0)
        {
            raiz.Nodes.Add(new TreeNode("(sin archivos — agregá con el botón derecho)")
            {
                ForeColor = Tema.Texto3
            });
        }

        _tree.EndUpdate();
    }

    /// <summary>
    /// El nodo de un archivo del proyecto.
    ///
    /// El principal se marca con ► porque es el que compila F7, y no poder
    /// distinguirlo obliga a abrir las propiedades para saberlo. Los que faltan
    /// van en rojo: el archivo sigue en la lista y hay que poder verlo.
    /// </summary>
    private static TreeNode NodoDeArchivo(ArchivoDeProyecto archivo)
    {
        var texto = archivo.NombreDeArchivo;

        if (archivo.EsPrincipal) texto = "► " + texto;
        if (!archivo.Existe) texto += "   (falta)";

        var color = !archivo.Existe ? Tema.Critico
                  : archivo.EsPrincipal ? Tema.Acento
                  : SourceExtensions.Contains(archivo.Extension) ? Tema.Info
                  : Tema.Texto3;

        return new TreeNode(texto)
        {
            Tag = archivo.Absoluta,
            ForeColor = color,
            ToolTipText = archivo.Absoluta ?? archivo.Relativa
        };
    }

    /// <summary>
    /// Llena un nodo de carpeta con sus subcarpetas y archivos. Las subcarpetas
    /// reciben un nodo marcador para que se lean solo cuando se expandan.
    /// </summary>
    private void PopulateDirectory(TreeNode node, string path)
    {
        node.Nodes.Clear();

        try
        {
            foreach (var dir in Directory.EnumerateDirectories(path).OrderBy(d => d))
            {
                var name = Path.GetFileName(dir);
                if (ShouldSkipDirectory(name)) continue;

                var child = new TreeNode(name) { Tag = dir, ForeColor = Tema.Acento };
                child.Nodes.Add(new TreeNode(PlaceholderTag) { Tag = PlaceholderTag });
                node.Nodes.Add(child);
            }

            var files = Directory.EnumerateFiles(path)
                .Where(f => IsInteresting(f))
                .OrderBy(f => f);

            foreach (var file in files)
            {
                var ext = Path.GetExtension(file).ToLowerInvariant();
                var child = new TreeNode(Path.GetFileName(file))
                {
                    Tag = file,
                    ForeColor = SourceExtensions.Contains(ext) ? Tema.Info : Tema.Texto3
                };
                node.Nodes.Add(child);
            }
        }
        catch (UnauthorizedAccessException)
        {
            node.Nodes.Add(new TreeNode("(sin permisos)") { ForeColor = Tema.Critico });
        }
        catch (IOException)
        {
            node.Nodes.Add(new TreeNode("(no se pudo leer)") { ForeColor = Tema.Critico });
        }
    }

    private static bool ShouldSkipDirectory(string name) =>
        name is "bin" or "obj" or ".vs" or ".git" or "node_modules" ||
        name.StartsWith('.');

    private static bool IsInteresting(string file)
    {
        var ext = Path.GetExtension(file).ToLowerInvariant();
        return SourceExtensions.Contains(ext) || OtherExtensions.Contains(ext);
    }

    private void OnBeforeExpand(object? sender, TreeViewCancelEventArgs e)
    {
        var node = e.Node;
        if (node is null) return;

        // Todavía tiene el marcador: hay que leer la carpeta de verdad.
        bool needsLoad = node.Nodes.Count == 1 &&
                         (node.Nodes[0].Tag as string) == PlaceholderTag;

        if (needsLoad && node.Tag is string dir && Directory.Exists(dir))
        {
            PopulateDirectory(node, dir);
        }
    }

    private void Activate(TreeNode? node)
    {
        if (node?.Tag is not string path) return;

        if (Directory.Exists(path))
        {
            node.Toggle();
            return;
        }

        // Un archivo de la lista del proyecto que ya no está: se avisa en vez de
        // no hacer nada, que dejaría al usuario haciendo doble clic sin efecto.
        if (!File.Exists(path))
        {
            if (_proyecto is not null)
            {
                MessageBox.Show(this,
                    $"El archivo no está en el disco:{Environment.NewLine}{Environment.NewLine}{path}" +
                    $"{Environment.NewLine}{Environment.NewLine}" +
                    $"Sigue en la lista del proyecto por si lo movieron o falta traerlo. " +
                    $"Si ya no lo necesitás, sacalo con el botón derecho.",
                    "Archivo no encontrado", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }

            return;
        }

        // Solo abrimos en el editor lo que es texto fuente; el resto lo deja al
        // shell. El .asmproj y el .asmform van incluidos: MainForm los reconoce
        // y los abre como proyecto y como formulario, no como texto.
        var ext = Path.GetExtension(path).ToLowerInvariant();
        if (SourceExtensions.Contains(ext) ||
            ext is ".txt" or ".md" or ".bat" or ".ps1" or ".asmproj" or ".asmform")
        {
            FileActivated?.Invoke(path);
        }
    }

    private void OpenInShell()
    {
        var target = _tree.SelectedNode?.Tag as string ?? RootFolder;
        if (string.IsNullOrEmpty(target)) return;

        var folder = Directory.Exists(target) ? target : Path.GetDirectoryName(target);
        if (string.IsNullOrEmpty(folder) || !Directory.Exists(folder)) return;

        try
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = folder,
                UseShellExecute = true
            });
        }
        catch
        {
            // Si el shell falla no hay nada útil que hacer acá.
        }
    }

    /// <summary>Selecciona en el árbol el nodo del archivo dado, si está visible.</summary>
    public void HighlightFile(string? path)
    {
        if (string.IsNullOrEmpty(path)) return;
        var node = FindNode(_tree.Nodes, path);
        if (node is not null) _tree.SelectedNode = node;
    }

    private static TreeNode? FindNode(TreeNodeCollection nodes, string path)
    {
        foreach (TreeNode node in nodes)
        {
            if (node.Tag is string p && Core.PathComparer.SamePath(p, path)) return node;
            var found = FindNode(node.Nodes, path);
            if (found is not null) return found;
        }
        return null;
    }
}
