namespace AsmEditor.Core;

/// <summary>Gravedad de un diagnóstico, tal como la reportan las herramientas.</summary>
public enum DiagnosticLevel
{
    Warning,
    Error,
    Fatal
}

/// <summary>Herramienta que produjo el diagnóstico.</summary>
public enum BuildTool
{
    Nasm,
    Masm,
    GoLink,
    MsvcLink,
    Editor
}

/// <summary>
/// Un problema reportado por NASM o GoLink, ya interpretado.
///
/// Column existe porque el modelo lo admite, pero NASM 3.02 NO reporta columna:
/// su formato es "archivo:línea: nivel: mensaje" y nada más. Queda en null salvo
/// que alguna herramienta futura la emita. Por eso la grilla muestra Nivel y no Columna.
/// </summary>
public sealed record BuildDiagnostic(
    DiagnosticLevel Level,
    string? FileName,
    int? Line,
    string Message,
    BuildTool Tool,
    int? Column = null)
{
    /// <summary>True si el diagnóstico apunta a un lugar concreto del código.</summary>
    public bool HasLocation => FileName is not null && Line is > 0;

    public string LevelText => Level switch
    {
        DiagnosticLevel.Warning => "advertencia",
        DiagnosticLevel.Error => "error",
        DiagnosticLevel.Fatal => "fatal",
        _ => "?"
    };

    public string ToolText => Tool switch
    {
        BuildTool.Nasm => "NASM",
        BuildTool.Masm => "MASM",
        BuildTool.GoLink => "GoLink",
        BuildTool.MsvcLink => "MSVC link",
        BuildTool.Editor => "Editor",
        _ => ""
    };

    /// <summary>Texto de la ubicación para mostrar, o vacío si no la tiene.</summary>
    public string LocationText => HasLocation ? $"{Path.GetFileName(FileName)}:{Line}" : "";
}
