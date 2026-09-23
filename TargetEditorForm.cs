using AsmEditor.Core;

namespace AsmEditor;

/// <summary>
/// Crear, duplicar, editar y borrar los targets de compilación, para no tener que
/// tocar settings.json a mano.
/// </summary>
public class TargetEditorForm : Form
{
    public BuildSettings Settings { get; }

    private readonly ListBox _targetList = new();
    private readonly TextBox _nameBox = new();
    private readonly ComboBox _asmBox = new();
    private readonly ComboBox _archBox = new();
    private readonly ComboBox _outputBox = new();
    private readonly ComboBox _linkBox = new();
    private readonly ComboBox _subsysBox = new();
    private readonly TextBox _entryBox = new();
    private readonly TextBox _libsBox = new();
    private readonly TextBox _asmFlagsBox = new();
    private readonly TextBox _linkFlagsBox = new();

    // Rutas propias de este target. Vacías = usar lo de Configuración, y si ahí
    // tampoco hay nada, la autodetección. El modelo ya las tenía; lo que
    // faltaba era poder editarlas.
    private readonly ComboBox _masmBox = new();
    private readonly ComboBox _msvcLinkBox = new();
    private readonly ComboBox _sdkBox = new();

    private readonly Label _preview = new();

    /// <summary>Lo instalado, por arquitectura; se recalcula al cambiarla.</summary>
    private List<Toolchain> _toolchains = new();
    private List<SdkVersion> _sdks = new();

    /// <summary>Evita que rellenar los campos dispare los eventos de edición.</summary>
    private bool _loading;

    public TargetEditorForm(BuildSettings current)
    {
        Settings = current.CloneForEditing();

        Text = "Administrar targets de compilación";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterParent;
        MaximizeBox = false;
        MinimizeBox = false;
        ClientSize = new Size(830, 620);

        BuildLayout();
        ReloadTargetList(Settings.ActiveTargetIndex);
    }

    private void BuildLayout()
    {
        // ---- Lista de targets ----
        var lblList = new Label { Text = "Targets:", Left = 15, Top = 12, Width = 200 };
        _targetList.Left = 15;
        _targetList.Top = 34;
        _targetList.Width = 250;
        _targetList.Height = 480;
        _targetList.SelectedIndexChanged += (_, _) => LoadSelectedIntoFields();

        var btnNew = new Button { Text = "Nuevo", Left = 15, Top = 522, Width = 78 };
        var btnDup = new Button { Text = "Duplicar", Left = 101, Top = 522, Width = 78 };
        var btnDel = new Button { Text = "Borrar", Left = 187, Top = 522, Width = 78 };

        btnNew.Click += (_, _) => NewTarget();
        btnDup.Click += (_, _) => DuplicateTarget();
        btnDel.Click += (_, _) => DeleteTarget();

        Controls.Add(lblList);
        Controls.Add(_targetList);
        Controls.Add(btnNew);
        Controls.Add(btnDup);
        Controls.Add(btnDel);

        // ---- Campos del target seleccionado ----
        int x = 290, y = 34, labelW = 130, boxX = 430, boxW = 310;

        void Row(string label, Control control, int height = 23)
        {
            Controls.Add(new Label { Text = label, Left = x, Top = y + 3, Width = labelW });
            control.Left = boxX;
            control.Top = y;
            control.Width = boxW;
            control.Height = height;
            Controls.Add(control);
            y += 34;
        }

        Controls.Add(new Label
        {
            Text = "Configuración del target:",
            Left = x, Top = 12, Width = 300, Font = new Font(Font, FontStyle.Bold)
        });

        Row("Nombre:", _nameBox);

        _asmBox.DropDownStyle = ComboBoxStyle.DropDownList;
        _asmBox.Items.AddRange(new object[] { "NASM", "MASM (ml64 / ml)" });
        Row("Ensamblador:", _asmBox);

        _archBox.DropDownStyle = ComboBoxStyle.DropDownList;
        _archBox.Items.AddRange(new object[] { "32 bits (win32)", "64 bits (win64)" });
        Row("Arquitectura:", _archBox);

        _outputBox.DropDownStyle = ComboBoxStyle.DropDownList;
        _outputBox.Items.AddRange(new object[] { "Solo ensamblar (.obj)", "Ensamblar y enlazar (.exe)" });
        Row("Genera:", _outputBox);

        _linkBox.DropDownStyle = ComboBoxStyle.DropDownList;
        _linkBox.Items.AddRange(new object[] { "GoLink (.dll)", "MSVC link.exe (.lib)" });
        Row("Enlazador:", _linkBox);

        _subsysBox.DropDownStyle = ComboBoxStyle.DropDownList;
        _subsysBox.Items.AddRange(new object[] { "Ventana", "Consola" });
        Row("Subsistema:", _subsysBox);

        Row("Punto de entrada:", _entryBox);
        Row("Librerías:", _libsBox);
        Row("Banderas del asm:", _asmFlagsBox);
        Row("Banderas del link:", _linkFlagsBox);

        // ⚠ ESTAS TRES PISAN LA CONFIGURACIÓN GLOBAL. Sirven para que un target
        // use una versión distinta de la elegida en Configuración —por ejemplo
        // para reproducir un enlazado viejo— sin cambiársela a todos los demás.
        _masmBox.DropDownStyle = ComboBoxStyle.DropDownList;
        Row("MASM del target:", _masmBox);

        _msvcLinkBox.DropDownStyle = ComboBoxStyle.DropDownList;
        Row("link.exe del target:", _msvcLinkBox);

        _sdkBox.DropDownStyle = ComboBoxStyle.DropDownList;
        Row("SDK del target:", _sdkBox);

        _preview.Left = x;
        _preview.Top = y + 6;
        _preview.Width = 450;
        _preview.Height = 60;
        _preview.ForeColor = SystemColors.GrayText;
        _preview.Font = new Font("Consolas", 8f);
        Controls.Add(_preview);

        // Cualquier cambio se guarda en el target seleccionado al vuelo.
        _nameBox.TextChanged += (_, _) => ApplyFieldsToSelected(refreshList: true);
        _entryBox.TextChanged += (_, _) => ApplyFieldsToSelected();
        _libsBox.TextChanged += (_, _) => ApplyFieldsToSelected();
        _asmFlagsBox.TextChanged += (_, _) => ApplyFieldsToSelected();
        _linkFlagsBox.TextChanged += (_, _) => ApplyFieldsToSelected();
        _asmBox.SelectedIndexChanged += (_, _) => ApplyFieldsToSelected(refreshList: true);
        // ⚠ AL CAMBIAR LA ARQUITECTURA HAY QUE REPOBLAR: las herramientas de 32
        // y de 64 bits son juegos distintos (ml.exe contra ml64.exe), y dejar
        // los combos como estaban mostraría rutas de la otra arquitectura.
        _archBox.SelectedIndexChanged += (_, _) => CambiarArquitectura();
        _outputBox.SelectedIndexChanged += (_, _) => ApplyFieldsToSelected(refreshList: true);
        _linkBox.SelectedIndexChanged += (_, _) => ApplyFieldsToSelected(refreshList: true);
        _subsysBox.SelectedIndexChanged += (_, _) => ApplyFieldsToSelected();
        _masmBox.SelectedIndexChanged += (_, _) => ApplyFieldsToSelected();
        _msvcLinkBox.SelectedIndexChanged += (_, _) => ApplyFieldsToSelected();
        _sdkBox.SelectedIndexChanged += (_, _) => ApplyFieldsToSelected();

        // ---- Botones ----
        var ok = new Button
        {
            Text = "Guardar", DialogResult = DialogResult.OK,
            Left = 630, Top = 578, Width = 90
        };
        var cancel = new Button
        {
            Text = "Cancelar", DialogResult = DialogResult.Cancel,
            Left = 728, Top = 578, Width = 90
        };

        Controls.Add(ok);
        Controls.Add(cancel);
        AcceptButton = ok;
        CancelButton = cancel;
    }

    // ---------------------------------------------------------------
    // Lista
    // ---------------------------------------------------------------

    private void ReloadTargetList(int selectIndex)
    {
        _loading = true;
        _targetList.BeginUpdate();
        _targetList.Items.Clear();

        foreach (var t in Settings.Targets)
        {
            _targetList.Items.Add(t.DisplayName);
        }

        _targetList.EndUpdate();
        _loading = false;

        if (_targetList.Items.Count > 0)
        {
            _targetList.SelectedIndex = Math.Clamp(selectIndex, 0, _targetList.Items.Count - 1);
        }
        else
        {
            ClearFields();
        }
    }

    private BuildTarget? Selected =>
        _targetList.SelectedIndex >= 0 && _targetList.SelectedIndex < Settings.Targets.Count
            ? Settings.Targets[_targetList.SelectedIndex]
            : null;

    private void LoadSelectedIntoFields()
    {
        var t = Selected;
        if (t is null) { ClearFields(); return; }

        // Lo instalado depende de la arquitectura del target, así que se
        // recalcula antes de rellenar los combos.
        PoblarHerramientas(t.Arch);

        _loading = true;
        _nameBox.Text = t.Name;
        _asmBox.SelectedIndex = t.Assembler == AssemblerKind.Masm ? 1 : 0;
        _archBox.SelectedIndex = t.Arch == TargetArch.Win32 ? 0 : 1;
        _outputBox.SelectedIndex = t.Output == TargetOutput.ObjectOnly ? 0 : 1;
        _linkBox.SelectedIndex = t.Linker == LinkerKind.MsvcLink ? 1 : 0;
        _subsysBox.SelectedIndex = t.Subsystem == TargetSubsystem.Console ? 1 : 0;
        _entryBox.Text = t.EntryPoint;
        _libsBox.Text = t.Libraries;
        _asmFlagsBox.Text = t.ExtraAsmFlags;
        _linkFlagsBox.Text = t.ExtraLinkFlags;

        _masmBox.SelectedIndex = IndiceDeRuta(_masmBox, t.MasmPath);
        _msvcLinkBox.SelectedIndex = IndiceDeRuta(_msvcLinkBox, t.MsvcLinkerPath);
        _sdkBox.SelectedIndex = IndiceDeRuta(_sdkBox, t.SdkLibPath);
        _loading = false;

        UpdatePreview();
    }

    private void ClearFields()
    {
        _loading = true;
        _nameBox.Text = "";
        _asmBox.SelectedIndex = -1;
        _archBox.SelectedIndex = -1;
        _outputBox.SelectedIndex = -1;
        _linkBox.SelectedIndex = -1;
        _subsysBox.SelectedIndex = -1;
        _entryBox.Text = "";
        _libsBox.Text = "";
        _asmFlagsBox.Text = "";
        _linkFlagsBox.Text = "";
        _masmBox.SelectedIndex = -1;
        _msvcLinkBox.SelectedIndex = -1;
        _sdkBox.SelectedIndex = -1;
        _preview.Text = "";
        _loading = false;
    }

    private void ApplyFieldsToSelected(bool refreshList = false)
    {
        if (_loading) return;

        var t = Selected;
        if (t is null) return;

        t.Name = _nameBox.Text;
        t.Assembler = _asmBox.SelectedIndex == 1 ? AssemblerKind.Masm : AssemblerKind.Nasm;
        t.Arch = _archBox.SelectedIndex == 0 ? TargetArch.Win32 : TargetArch.Win64;
        t.Output = _outputBox.SelectedIndex == 0 ? TargetOutput.ObjectOnly : TargetOutput.Executable;
        t.Linker = _linkBox.SelectedIndex == 1 ? LinkerKind.MsvcLink : LinkerKind.GoLink;
        t.Subsystem = _subsysBox.SelectedIndex == 1 ? TargetSubsystem.Console : TargetSubsystem.Windows;
        t.EntryPoint = _entryBox.Text.Trim();
        t.Libraries = _libsBox.Text.Trim();
        t.ExtraAsmFlags = _asmFlagsBox.Text.Trim();
        t.ExtraLinkFlags = _linkFlagsBox.Text.Trim();

        t.MasmPath = RutaDelCombo(_masmBox);
        t.MsvcLinkerPath = RutaDelCombo(_msvcLinkBox);
        t.SdkLibPath = RutaDelCombo(_sdkBox);

        UpdatePreview();

        if (refreshList)
        {
            int i = _targetList.SelectedIndex;
            _loading = true;
            _targetList.Items[i] = t.DisplayName;
            _loading = false;
        }
    }

    // ---------------------------------------------------------------
    // Las herramientas que puede fijar el target
    // ---------------------------------------------------------------

    /// <summary>
    /// Llena los tres combos con lo instalado para esa arquitectura.
    ///
    /// La primera opción siempre es «Usar la de Configuración»: es lo que hace
    /// el target si no fija nada, y tiene que ser fácil volver ahí.
    /// </summary>
    private void PoblarHerramientas(TargetArch arch)
    {
        _toolchains = LinkerLocator.DiscoverToolchains(arch, Settings.ProjectFolder);
        _sdks = LinkerLocator.DiscoverSdks(arch);

        bool estabaCargando = _loading;
        _loading = true;

        const string usarGlobal = "Usar la de Configuración";

        _masmBox.Items.Clear();
        _masmBox.Items.Add(usarGlobal);
        foreach (var t in _toolchains)
        {
            if (t.MasmPath is not null) _masmBox.Items.Add(t.DisplayName);
        }

        _msvcLinkBox.Items.Clear();
        _msvcLinkBox.Items.Add(usarGlobal);
        foreach (var t in _toolchains)
        {
            if (t.LinkerPath is not null) _msvcLinkBox.Items.Add(t.DisplayName);
        }

        _sdkBox.Items.Clear();
        _sdkBox.Items.Add(usarGlobal);
        foreach (var s in _sdks) _sdkBox.Items.Add(s.DisplayName);

        _loading = estabaCargando;
    }

    /// <summary>
    /// Cambiar la arquitectura rehace las listas de herramientas.
    ///
    /// Las rutas que el target tuviera fijadas eran de la otra arquitectura, así
    /// que se sueltan: quedan en «usar la de Configuración». Conservarlas
    /// dejaría un target de 32 bits apuntando a un ml64.exe.
    /// </summary>
    private void CambiarArquitectura()
    {
        if (_loading) return;

        var t = Selected;
        if (t is null) return;

        var nueva = _archBox.SelectedIndex == 0 ? TargetArch.Win32 : TargetArch.Win64;

        if (nueva != t.Arch)
        {
            t.MasmPath = "";
            t.MsvcLinkerPath = "";
            t.SdkLibPath = "";
        }

        t.Arch = nueva;
        PoblarHerramientas(nueva);

        _loading = true;
        _masmBox.SelectedIndex = IndiceDeRuta(_masmBox, t.MasmPath);
        _msvcLinkBox.SelectedIndex = IndiceDeRuta(_msvcLinkBox, t.MsvcLinkerPath);
        _sdkBox.SelectedIndex = IndiceDeRuta(_sdkBox, t.SdkLibPath);
        _loading = false;

        ApplyFieldsToSelected(refreshList: true);
    }

    /// <summary>Las rutas que ofrece cada combo, en el mismo orden que sus ítems.</summary>
    private List<string> RutasDe(ComboBox combo)
    {
        if (ReferenceEquals(combo, _masmBox))
            return _toolchains.Where(t => t.MasmPath is not null).Select(t => t.MasmPath!).ToList();

        if (ReferenceEquals(combo, _msvcLinkBox))
            return _toolchains.Where(t => t.LinkerPath is not null).Select(t => t.LinkerPath!).ToList();

        return _sdks.Select(s => s.LibPath).ToList();
    }

    /// <summary>
    /// Qué ítem corresponde a la ruta guardada. 0 es «usar la de Configuración».
    ///
    /// ⚠ SE BUSCA POR RUTA, NO POR ÍNDICE: instalar o desinstalar una versión
    /// de Visual Studio cambia el orden de la lista.
    ///
    /// Si la ruta guardada ya no está entre las instaladas —una versión que se
    /// desinstaló, o una ruta puesta a mano— se agrega como ítem propio en vez
    /// de volver en silencio a la global: el target la sigue usando, y
    /// esconderla haría que la pantalla mintiera.
    /// </summary>
    private int IndiceDeRuta(ComboBox combo, string guardada)
    {
        if (string.IsNullOrWhiteSpace(guardada)) return 0;

        var rutas = RutasDe(combo);
        for (int i = 0; i < rutas.Count; i++)
        {
            if (string.Equals(rutas[i], guardada, StringComparison.OrdinalIgnoreCase))
            {
                return i + 1;
            }
        }

        combo.Items.Add($"{guardada}  (fijada a mano)");
        return combo.Items.Count - 1;
    }

    /// <summary>La ruta que corresponde a lo elegido. Vacío = usar la global.</summary>
    private string RutaDelCombo(ComboBox combo)
    {
        int i = combo.SelectedIndex - 1;
        if (i < 0) return "";

        var rutas = RutasDe(combo);
        if (i < rutas.Count) return rutas[i];

        // Es el ítem «fijada a mano» que se agregó al cargar: se conserva tal cual.
        var texto = combo.Items[combo.SelectedIndex]?.ToString() ?? "";
        int marca = texto.IndexOf("  (fijada a mano)", StringComparison.Ordinal);
        return marca > 0 ? texto[..marca] : "";
    }

    /// <summary>Muestra los comandos exactos que se van a ejecutar con este target.</summary>
    private void UpdatePreview()
    {
        var t = Selected;
        if (t is null) { _preview.Text = ""; return; }

        // El nombre del ejecutable acompaña al ensamblador elegido: con MASM la
        // arquitectura la decide cuál de los dos se invoca, no una bandera.
        var exeAsm = t.UsaMasm
            ? (t.Arch == TargetArch.Win32 ? "ml.exe" : "ml64.exe")
            : "nasm.exe";

        var texto = $"{exeAsm} {t.BuildAssemblerArguments("programa.asm", "programa.obj")}";

        if (t.ProducesExecutable)
        {
            // ⚠ EL SDK SE RESUELVE DE VERDAD, NO SE PASA null. Pasando null la
            // vista previa omitía el /LIBPATH y mostraba un comando que no era
            // el que se iba a ejecutar: enseñaba un enlazado sin librerías del
            // SDK que en la realidad fallaría con LNK1181.
            var sdk = string.IsNullOrWhiteSpace(t.SdkLibPath)
                ? LinkerLocator.FindSdkLibPath(t.Arch)
                : t.SdkLibPath;

            texto += Environment.NewLine + (t.Linker == LinkerKind.MsvcLink
                ? $"link.exe {t.BuildMsvcArguments("programa.obj", "programa.exe", sdk)}"
                : $"GoLink.exe {t.BuildGoLinkArguments("programa.obj")}");
        }
        else
        {
            texto += Environment.NewLine + "(no enlaza: este target solo genera el .obj)";
        }

        _preview.Text = texto;
    }

    // ---------------------------------------------------------------
    // Alta, duplicado y baja
    // ---------------------------------------------------------------

    private void NewTarget()
    {
        Settings.Targets.Add(new BuildTarget { Name = "Nuevo target" });
        ReloadTargetList(Settings.Targets.Count - 1);
        _nameBox.Focus();
        _nameBox.SelectAll();
    }

    private void DuplicateTarget()
    {
        var t = Selected;
        if (t is null) return;

        var copia = t.Clone();
        copia.Name = t.Name + " (copia)";
        Settings.Targets.Insert(_targetList.SelectedIndex + 1, copia);
        ReloadTargetList(_targetList.SelectedIndex + 1);
    }

    private void DeleteTarget()
    {
        if (Selected is null) return;

        // Siempre tiene que quedar al menos un target, o no se puede compilar.
        if (Settings.Targets.Count <= 1)
        {
            Dialogo.Aviso(this, "No se puede borrar",
                "Tiene que quedar al menos un target.", Dialogo.Tono.Aviso);
            return;
        }

        bool borrar = Dialogo.Confirmar(this, "Confirmar",
            $"¿Borrar el target '{Selected.Name}'?", si: "Borrar", no: "Cancelar");
        if (!borrar) return;

        int index = _targetList.SelectedIndex;
        Settings.Targets.RemoveAt(index);

        if (Settings.ActiveTargetIndex >= Settings.Targets.Count)
        {
            Settings.ActiveTargetIndex = Settings.Targets.Count - 1;
        }

        ReloadTargetList(Math.Min(index, Settings.Targets.Count - 1));
    }
}
