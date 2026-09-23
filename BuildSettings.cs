using AsmEditor.Core;

namespace AsmEditor;

/// <summary>
/// Persistencia de la configuración en settings.json, junto al ejecutable del editor.
/// La lógica (targets, migración del formato viejo) vive en Core.BuildConfig, que no
/// depende de la UI y está cubierta por pruebas; acá solo se lee y se escribe el archivo.
/// </summary>
public class BuildSettings
{
    public BuildConfig Config { get; private set; } = new();

    public string NasmPath
    {
        get => Config.NasmPath;
        set => Config.NasmPath = value;
    }

    public string GoLinkPath
    {
        get => Config.GoLinkPath;
        set => Config.GoLinkPath = value;
    }

    public string ProjectFolder
    {
        get => Config.ProjectFolder;
        set => Config.ProjectFolder = value;
    }

    public List<BuildTarget> Targets => Config.Targets;

    public BuildTarget ActiveTarget => Config.ActiveTarget;

    public int ActiveTargetIndex
    {
        get => Config.ActiveTargetIndex;
        set
        {
            Config.ActiveTargetIndex = value;
            Config.EnsureValid();
        }
    }

    private static string SettingsFile =>
        Path.Combine(AppContext.BaseDirectory, "settings.json");

    public static BuildSettings Load()
    {
        var settings = new BuildSettings();

        try
        {
            if (File.Exists(SettingsFile))
            {
                settings.Config = BuildConfig.FromJson(File.ReadAllText(SettingsFile));
                return settings;
            }
        }
        catch
        {
            // Si el archivo está corrupto o no se puede leer, usamos valores por defecto.
        }

        settings.Config.EnsureValid();
        return settings;
    }

    public void Save()
    {
        Config.EnsureValid();
        try
        {
            File.WriteAllText(SettingsFile, Config.ToJson());
        }
        catch
        {
            // No hay nada útil que hacer si el disco falla al guardar la configuración.
        }
    }

    public BuildSettings CloneForEditing() => new() { Config = Config.Clone() };
}
