using System.Text.Json;
using System.Text.Json.Serialization;

namespace AsmEditor.Core;

/// <summary>
/// Configuración completa del editor: rutas de herramientas y la lista de targets.
/// Sin dependencias de UI para poder probarse; BuildSettings la envuelve para la app.
///
/// El formato viejo (un solo juego de EntryPoint/Libraries suelto) se migra al abrir:
/// esos valores se convierten en un target y no se pierde nada de lo configurado.
/// </summary>
public sealed class BuildConfig
{
    public string NasmPath { get; set; } = @"C:\Users\Fabian\NASM\nasm.exe";
    public string GoLinkPath { get; set; } = @"C:\Users\Fabian\NASM\GoLink.exe";
    public string ProjectFolder { get; set; } = @"C:\Users\Fabian\NASM";

    // ---- Herramientas de MSVC ----
    // Preferencia GLOBAL: qué juego de herramientas y qué SDK usar cuando el
    // target no fija los suyos. Vacío = autodetección (el primero por prioridad:
    // las carpetas del proyecto, después Visual Studio de más nuevo a más viejo).
    //
    // ⚠ SE GUARDA LA RUTA, NO UN ÍNDICE. Un índice se rompería en cuanto se
    // instale o se desinstale una versión de Visual Studio: la lista cambia de
    // orden y el editor quedaría apuntando a otro toolchain sin avisar.

    /// <summary>Carpeta bin del toolchain elegido (de ahí salen ml64.exe y link.exe, apareados).</summary>
    public string MsvcToolchainDir64 { get; set; } = "";

    /// <summary>Ídem para 32 bits: son dos juegos distintos.</summary>
    public string MsvcToolchainDir32 { get; set; } = "";

    /// <summary>Carpeta de los .lib del SDK para 64 bits.</summary>
    public string SdkLibPath64 { get; set; } = "";

    /// <summary>Ídem para 32 bits.</summary>
    public string SdkLibPath32 { get; set; } = "";

    public List<BuildTarget> Targets { get; set; } = new();

    /// <summary>Índice del target activo dentro de Targets.</summary>
    public int ActiveTargetIndex { get; set; }

    /// <summary>Lo que el editor recuerda entre sesiones: tema, recientes, ventana.</summary>
    public UiState Ui { get; set; } = new();

    // ---- Campos del formato viejo, solo para poder leerlos y migrarlos ----
    // Se serializan igual para que una versión anterior del editor siga entendiendo
    // el archivo, pero la fuente de verdad son los Targets.

    [JsonPropertyName("Libraries")]
    public string? LegacyLibraries { get; set; }

    [JsonPropertyName("EntryPoint")]
    public string? LegacyEntryPoint { get; set; }

    [JsonIgnore]
    public BuildTarget ActiveTarget
    {
        get
        {
            EnsureValid();
            return Targets[ActiveTargetIndex];
        }
    }

    /// <summary>
    /// Garantiza que siempre haya al menos un target y que el índice activo sea válido.
    /// Si el archivo venía del formato viejo, convierte sus valores en un target.
    /// </summary>
    public void EnsureValid()
    {
        if (Targets.Count == 0)
        {
            Targets = MigrateOrDefaults();
        }

        if (ActiveTargetIndex < 0 || ActiveTargetIndex >= Targets.Count)
        {
            ActiveTargetIndex = 0;
        }

        Ui ??= new UiState();
        Ui.EnsureValid();
    }

    /// <summary>
    /// Construye la lista inicial de targets. Si había configuración vieja suelta,
    /// esa configuración es el primer target (no se pierde lo que el usuario tenía).
    /// </summary>
    private List<BuildTarget> MigrateOrDefaults()
    {
        bool hasLegacy = !string.IsNullOrWhiteSpace(LegacyEntryPoint)
                      || !string.IsNullOrWhiteSpace(LegacyLibraries);

        if (!hasLegacy) return BuildTarget.CreateDefaults();

        var migrated = new BuildTarget
        {
            Name = "Configuración anterior",
            Arch = TargetArch.Win64,
            Output = TargetOutput.Executable,
            EntryPoint = string.IsNullOrWhiteSpace(LegacyEntryPoint) ? "main" : LegacyEntryPoint!.Trim(),
            Libraries = LegacyLibraries?.Trim() ?? ""
        };

        var list = new List<BuildTarget> { migrated };

        // Se agregan los predefinidos que no dupliquen al migrado.
        foreach (var def in BuildTarget.CreateDefaults())
        {
            bool sameSettings = def.Arch == migrated.Arch
                             && def.Output == migrated.Output
                             && string.Equals(def.EntryPoint, migrated.EntryPoint, StringComparison.OrdinalIgnoreCase)
                             && string.Equals(def.Libraries, migrated.Libraries, StringComparison.OrdinalIgnoreCase);

            if (!sameSettings) list.Add(def);
        }

        return list;
    }

    public static BuildConfig FromJson(string json)
    {
        var config = JsonSerializer.Deserialize<BuildConfig>(json) ?? new BuildConfig();
        config.EnsureValid();
        return config;
    }

    public string ToJson() =>
        JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });

    public BuildConfig Clone()
    {
        var copy = new BuildConfig
        {
            NasmPath = NasmPath,
            GoLinkPath = GoLinkPath,
            ProjectFolder = ProjectFolder,

            // ⚠ SI SE AGREGA UN CAMPO ARRIBA, VA TAMBIÉN ACÁ: lo que falte en
            // el Clone se pierde en silencio al guardar desde cualquier diálogo.
            MsvcToolchainDir64 = MsvcToolchainDir64,
            MsvcToolchainDir32 = MsvcToolchainDir32,
            SdkLibPath64 = SdkLibPath64,
            SdkLibPath32 = SdkLibPath32,

            ActiveTargetIndex = ActiveTargetIndex,
            LegacyEntryPoint = LegacyEntryPoint,
            LegacyLibraries = LegacyLibraries,
            Targets = Targets.Select(t => t.Clone()).ToList(),

            // El estado de UI se comparte por referencia a propósito: los
            // recientes y la geometría no se editan en el diálogo de targets,
            // y clonarlos haría que los cambios de la sesión se perdieran al
            // guardar desde ahí.
            Ui = Ui
        };
        copy.EnsureValid();
        return copy;
    }
}
