using AsmEditor.Core;

namespace AsmEditor;

/// <summary>
/// Rutas de las herramientas y carpeta del proyecto.
///
/// Acá se elige QUÉ herramientas usa el editor cuando el target no fija las
/// suyas. Las librerías y el punto de entrada no viven acá: son parte de cada
/// target y se editan en Compilar &gt; Administrar targets.
///
/// ⚠ EL TOOLCHAIN DE MSVC SE ELIGE ENTERO, NO POR HERRAMIENTA SUELTA. ml64.exe
/// y link.exe salen apareados de la misma carpeta: mezclar versiones mete
/// errores de enlazado difíciles de atribuir. El SDK sí es aparte, porque sus
/// versiones son independientes de las de MSVC.
/// </summary>
public class SettingsForm : Form
{
    public BuildSettings Settings { get; private set; }

    private readonly TextBox _nasmBox = new();
    private readonly TextBox _golinkBox = new();
    private readonly TextBox _projectBox = new();

    // Un juego por arquitectura: son herramientas distintas, no la misma con
    // una bandera.
    private readonly ComboBox _toolchain64 = new();
    private readonly ComboBox _toolchain32 = new();
    private readonly ComboBox _sdk64 = new();
    private readonly ComboBox _sdk32 = new();

    private readonly Label _detalle = new();

    /// <summary>Lo que hay instalado, para no volver a recorrer el disco en cada cambio.</summary>
    private List<Toolchain> _tc64 = new();
    private List<Toolchain> _tc32 = new();
    private List<SdkVersion> _sdks64 = new();
    private List<SdkVersion> _sdks32 = new();

    /// <summary>Evita que rellenar los combos dispare los eventos de edición.</summary>
    private bool _loading;

    public SettingsForm(BuildSettings current)
    {
        Settings = current.CloneForEditing();

        Text = "Rutas de herramientas";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterParent;
        MaximizeBox = false;
        MinimizeBox = false;
        ClientSize = new Size(700, 500);

        BuildLayout();
        DetectarHerramientas();
        CargarValores();
    }

    private void BuildLayout()
    {
        int y = 18;

        Titulo("Ensamblador y enlazador sueltos", ref y);
        AddRow("Ruta de nasm.exe:", _nasmBox, ref y, BrowseKind.File);
        AddRow("Ruta de GoLink.exe:", _golinkBox, ref y, BrowseKind.File);

        y += 8;
        Titulo("Herramientas de MSVC (ml64/ml y link.exe van apareados)", ref y);
        AddCombo("Toolchain 64 bits:", _toolchain64, ref y);
        AddCombo("Toolchain 32 bits:", _toolchain32, ref y);
        AddCombo("SDK 64 bits:", _sdk64, ref y);
        AddCombo("SDK 32 bits:", _sdk32, ref y);

        y += 8;
        Titulo("Proyecto", ref y);
        AddRow("Carpeta del proyecto:", _projectBox, ref y, BrowseKind.Folder);

        _detalle.Left = 20;
        _detalle.Top = y + 4;
        _detalle.Width = 660;
        _detalle.Height = 46;
        _detalle.ForeColor = SystemColors.GrayText;
        _detalle.Font = new Font("Consolas", 8f);
        Controls.Add(_detalle);

        var nota = new Label
        {
            Text = "Las librerías y el punto de entrada se configuran por target, " +
                   "en Compilar > Administrar targets.",
            Left = 20,
            Top = y + 54,
            Width = 660,
            Height = 18,
            ForeColor = SystemColors.GrayText
        };
        Controls.Add(nota);

        var okButton = new Button
        {
            Text = "Guardar", DialogResult = DialogResult.OK,
            Left = 490, Top = y + 80, Width = 90
        };
        var cancelButton = new Button
        {
            Text = "Cancelar", DialogResult = DialogResult.Cancel,
            Left = 590, Top = y + 80, Width = 90
        };

        okButton.Click += (_, _) => GuardarValores();

        Controls.Add(okButton);
        Controls.Add(cancelButton);
        AcceptButton = okButton;
        CancelButton = cancelButton;
    }

    private void Titulo(string texto, ref int y)
    {
        Controls.Add(new Label
        {
            Text = texto,
            Left = 20,
            Top = y,
            Width = 660,
            Font = new Font(Font, FontStyle.Bold)
        });
        y += 26;
    }

    // ---------------------------------------------------------------
    // Qué hay instalado
    // ---------------------------------------------------------------

    private void DetectarHerramientas()
    {
        var proyecto = Settings.ProjectFolder;

        _tc64 = LinkerLocator.DiscoverToolchains(TargetArch.Win64, proyecto);
        _tc32 = LinkerLocator.DiscoverToolchains(TargetArch.Win32, proyecto);
        _sdks64 = LinkerLocator.DiscoverSdks(TargetArch.Win64);
        _sdks32 = LinkerLocator.DiscoverSdks(TargetArch.Win32);

        PoblarToolchains(_toolchain64, _tc64);
        PoblarToolchains(_toolchain32, _tc32);
        PoblarSdks(_sdk64, _sdks64);
        PoblarSdks(_sdk32, _sdks32);
    }

    /// <summary>
    /// La primera opción siempre es la automática: es la que estaba antes de
    /// que se pudiera elegir, y tiene que seguir siendo fácil volver a ella.
    /// </summary>
    private void PoblarToolchains(ComboBox combo, List<Toolchain> lista)
    {
        combo.Items.Clear();
        combo.Items.Add(TextoAutomatico(lista.Count));

        foreach (var t in lista)
        {
            // Lo incompleto se muestra igual, marcado: explica por qué no se
            // puede enlazar con esa instalación en vez de esconderla.
            combo.Items.Add(t.Completo ? t.DisplayName : $"{t.DisplayName}  (incompleto)");
        }
    }

    private void PoblarSdks(ComboBox combo, List<SdkVersion> lista)
    {
        combo.Items.Clear();
        combo.Items.Add(TextoAutomatico(lista.Count));
        foreach (var s in lista) combo.Items.Add(s.DisplayName);
    }

    private static string TextoAutomatico(int cuantos) =>
        cuantos > 0
            ? $"Automático  ({cuantos} disponible{(cuantos == 1 ? "" : "s")})"
            : "Automático  (no se encontró ninguno)";

    // ---------------------------------------------------------------
    // Cargar y guardar
    // ---------------------------------------------------------------

    private void CargarValores()
    {
        _loading = true;

        _nasmBox.Text = Settings.NasmPath;
        _golinkBox.Text = Settings.GoLinkPath;
        _projectBox.Text = Settings.ProjectFolder;

        var cfg = Settings.Config;
        _toolchain64.SelectedIndex = IndiceDeToolchain(_tc64, cfg.MsvcToolchainDir64);
        _toolchain32.SelectedIndex = IndiceDeToolchain(_tc32, cfg.MsvcToolchainDir32);
        _sdk64.SelectedIndex = IndiceDeSdk(_sdks64, cfg.SdkLibPath64);
        _sdk32.SelectedIndex = IndiceDeSdk(_sdks32, cfg.SdkLibPath32);

        _loading = false;
        ActualizarDetalle();
    }

    /// <summary>
    /// Qué opción del combo corresponde a lo guardado. 0 es «automático».
    ///
    /// ⚠ SE BUSCA POR RUTA, NO POR ÍNDICE: si se instaló o se desinstaló una
    /// versión de Visual Studio la lista cambió de orden, y un índice guardado
    /// apuntaría a otro toolchain sin avisar. Si la ruta ya no existe, se
    /// vuelve al automático en vez de quedar apuntando a algo que no está.
    /// </summary>
    private static int IndiceDeToolchain(List<Toolchain> lista, string guardado)
    {
        if (string.IsNullOrWhiteSpace(guardado)) return 0;

        for (int i = 0; i < lista.Count; i++)
        {
            var dir = CarpetaDe(lista[i]);
            if (dir is not null &&
                string.Equals(dir, guardado, StringComparison.OrdinalIgnoreCase))
            {
                return i + 1;   // el 0 lo ocupa «Automático»
            }
        }

        return 0;
    }

    private static int IndiceDeSdk(List<SdkVersion> lista, string guardado)
    {
        if (string.IsNullOrWhiteSpace(guardado)) return 0;

        for (int i = 0; i < lista.Count; i++)
        {
            if (string.Equals(lista[i].LibPath, guardado, StringComparison.OrdinalIgnoreCase))
            {
                return i + 1;
            }
        }

        return 0;
    }

    /// <summary>
    /// La carpeta de la que salen las dos herramientas del juego. Se toma del
    /// ensamblador, y si esa instalación no lo tiene, del enlazador.
    /// </summary>
    private static string? CarpetaDe(Toolchain t) =>
        Path.GetDirectoryName(t.MasmPath ?? t.LinkerPath ?? "");

    private void GuardarValores()
    {
        Settings.NasmPath = _nasmBox.Text.Trim();
        Settings.GoLinkPath = _golinkBox.Text.Trim();
        Settings.ProjectFolder = _projectBox.Text.Trim();

        var cfg = Settings.Config;

        // El índice 0 es «Automático»: guardar vacío es lo que devuelve la
        // autodetección, no un valor perdido.
        cfg.MsvcToolchainDir64 = RutaElegida(_toolchain64, _tc64);
        cfg.MsvcToolchainDir32 = RutaElegida(_toolchain32, _tc32);
        cfg.SdkLibPath64 = RutaDeSdkElegida(_sdk64, _sdks64);
        cfg.SdkLibPath32 = RutaDeSdkElegida(_sdk32, _sdks32);
    }

    private static string RutaElegida(ComboBox combo, List<Toolchain> lista)
    {
        int i = combo.SelectedIndex - 1;
        if (i < 0 || i >= lista.Count) return "";
        return CarpetaDe(lista[i]) ?? "";
    }

    private static string RutaDeSdkElegida(ComboBox combo, List<SdkVersion> lista)
    {
        int i = combo.SelectedIndex - 1;
        if (i < 0 || i >= lista.Count) return "";
        return lista[i].LibPath;
    }

    /// <summary>Qué se va a usar de verdad con lo que está elegido ahora.</summary>
    private void ActualizarDetalle()
    {
        var t64 = _toolchain64.SelectedIndex - 1;
        var texto = t64 >= 0 && t64 < _tc64.Count
            ? $"64 bits: {_tc64[t64].MasmPath}"
            : _tc64.Count > 0
                ? $"64 bits: {_tc64[0].MasmPath}  (automático)"
                : "64 bits: no se encontró MASM";

        _detalle.Text = texto;
    }

    private void AddCombo(string label, ComboBox combo, ref int y)
    {
        var lbl = new Label { Text = label, Left = 20, Top = y + 3, Width = 160 };
        combo.Left = 190;
        combo.Top = y;
        combo.Width = 490;
        combo.DropDownStyle = ComboBoxStyle.DropDownList;
        combo.SelectedIndexChanged += (_, _) => { if (!_loading) ActualizarDetalle(); };

        Controls.Add(lbl);
        Controls.Add(combo);
        y += 32;
    }

    private enum BrowseKind { None, File, Folder }

    private void AddRow(string label, TextBox box, ref int y, BrowseKind browse = BrowseKind.None)
    {
        var lbl = new Label { Text = label, Left = 20, Top = y + 3, Width = 160 };
        box.Left = 190;
        box.Top = y;
        box.Width = browse == BrowseKind.None ? 490 : 440;
        Controls.Add(lbl);
        Controls.Add(box);

        if (browse != BrowseKind.None)
        {
            var btn = new Button { Text = "...", Left = 640, Top = y - 1, Width = 40 };
            btn.Click += (_, _) => Browse(box, browse);
            Controls.Add(btn);
        }

        y += 32;
    }

    private void Browse(TextBox box, BrowseKind kind)
    {
        if (kind == BrowseKind.Folder)
        {
            using var dlg = new FolderBrowserDialog();
            if (Directory.Exists(box.Text)) dlg.SelectedPath = box.Text;
            if (dlg.ShowDialog(this) == DialogResult.OK) box.Text = dlg.SelectedPath;
        }
        else
        {
            using var dlg = new OpenFileDialog { Filter = "Ejecutables (*.exe)|*.exe" };
            if (dlg.ShowDialog(this) == DialogResult.OK) box.Text = dlg.FileName;
        }
    }
}
