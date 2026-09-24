using AsmEditor.Core;

namespace AsmEditor;

/// <summary>
/// Opciones → Herramientas → MSVC: qué toolchain y qué SDK usar cuando el
/// target no fija los suyos. Antes estaba en «Rutas de herramientas»; la
/// lógica es la misma.
///
/// ⚠ EL TOOLCHAIN DE MSVC SE ELIGE ENTERO, NO POR HERRAMIENTA SUELTA. ml64.exe
/// y link.exe salen apareados de la misma carpeta: mezclar versiones mete
/// errores de enlazado difíciles de atribuir. El SDK sí es aparte, porque sus
/// versiones son independientes de las de MSVC.
/// </summary>
public partial class PaginaOpcionesMsvc : UserControl, IPaginaOpciones
{
    /// <summary>Lo que hay instalado, para no volver a recorrer el disco en cada cambio.</summary>
    private List<Toolchain> _tc64 = new();
    private List<Toolchain> _tc32 = new();
    private List<SdkVersion> _sdks64 = new();
    private List<SdkVersion> _sdks32 = new();

    /// <summary>Evita que rellenar los combos dispare los eventos de edición.</summary>
    private bool _cargando;

    public PaginaOpcionesMsvc()
    {
        InitializeComponent();
    }

    public string Categoria => "Herramientas";
    public string Titulo => "MSVC";
    public int Orden => 40;

    // ---------------------------------------------------------------
    // Cargar y guardar
    // ---------------------------------------------------------------

    public void Cargar(ValoresOpciones valores)
    {
        _cargando = true;

        // Se busca con la carpeta de trabajo guardada, como antes: ahí pueden
        // estar las herramientas copiadas al proyecto.
        DetectarHerramientas(valores.CarpetaProyecto);

        cmbToolchain64.SelectedIndex = IndiceDeToolchain(_tc64, valores.ToolchainMsvc64);
        cmbToolchain32.SelectedIndex = IndiceDeToolchain(_tc32, valores.ToolchainMsvc32);
        cmbSdk64.SelectedIndex = IndiceDeSdk(_sdks64, valores.SdkLib64);
        cmbSdk32.SelectedIndex = IndiceDeSdk(_sdks32, valores.SdkLib32);

        _cargando = false;
        ActualizarDetalle();
    }

    public void Guardar(ValoresOpciones valores)
    {
        // El índice 0 es «Automático»: guardar vacío es lo que devuelve la
        // autodetección, no un valor perdido.
        valores.ToolchainMsvc64 = RutaElegida(cmbToolchain64, _tc64);
        valores.ToolchainMsvc32 = RutaElegida(cmbToolchain32, _tc32);
        valores.SdkLib64 = RutaDeSdkElegida(cmbSdk64, _sdks64);
        valores.SdkLib32 = RutaDeSdkElegida(cmbSdk32, _sdks32);
    }

    private void Combo_SelectedIndexChanged(object? sender, EventArgs e)
    {
        if (!_cargando) ActualizarDetalle();
    }

    // ---------------------------------------------------------------
    // Qué hay instalado
    // ---------------------------------------------------------------

    private void DetectarHerramientas(string carpetaProyecto)
    {
        _tc64 = LinkerLocator.DiscoverToolchains(TargetArch.Win64, carpetaProyecto);
        _tc32 = LinkerLocator.DiscoverToolchains(TargetArch.Win32, carpetaProyecto);
        _sdks64 = LinkerLocator.DiscoverSdks(TargetArch.Win64);
        _sdks32 = LinkerLocator.DiscoverSdks(TargetArch.Win32);

        PoblarToolchains(cmbToolchain64, _tc64);
        PoblarToolchains(cmbToolchain32, _tc32);
        PoblarSdks(cmbSdk64, _sdks64);
        PoblarSdks(cmbSdk32, _sdks32);
    }

    /// <summary>
    /// La primera opción siempre es la automática: es la que estaba antes de
    /// que se pudiera elegir, y tiene que seguir siendo fácil volver a ella.
    /// </summary>
    private static void PoblarToolchains(ComboBox combo, List<Toolchain> lista)
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

    private static void PoblarSdks(ComboBox combo, List<SdkVersion> lista)
    {
        combo.Items.Clear();
        combo.Items.Add(TextoAutomatico(lista.Count));
        foreach (var s in lista) combo.Items.Add(s.DisplayName);
    }

    private static string TextoAutomatico(int cuantos) =>
        cuantos > 0
            ? $"Automático  ({cuantos} disponible{(cuantos == 1 ? "" : "s")})"
            : "Automático  (no se encontró ninguno)";

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
        var t64 = cmbToolchain64.SelectedIndex - 1;
        lblNotaDetalle.Text = t64 >= 0 && t64 < _tc64.Count
            ? $"64 bits: {_tc64[t64].MasmPath}"
            : _tc64.Count > 0
                ? $"64 bits: {_tc64[0].MasmPath}  (automático)"
                : "64 bits: no se encontró MASM";
    }
}
