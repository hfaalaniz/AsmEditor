namespace AsmEditor.Core;

/// <summary>
/// Referencia a una ubicación de código reportada por NASM o GoLink en su salida.
/// </summary>
public sealed record SourceReference(string FileName, int LineNumber);

/// <summary>
/// Interpreta las líneas de salida de NASM para encontrar referencias a archivo:línea.
/// Se mantiene sin dependencias de UI para poder probarse.
/// </summary>
public static class NasmErrorParser
{
    // NASM reporta:  ruta\archivo.asm:42: error: mensaje
    // La ruta puede ser absoluta, relativa, con espacios, y la extensión puede ser .asm o .inc.
    // El (?:[A-Za-z]:)? del principio es la letra de unidad de Windows, cuyo ':' no debe
    // confundirse con el separador que precede al número de línea.
    private static readonly System.Text.RegularExpressions.Regex Pattern = new(
        @"^\s*(?<file>(?:[A-Za-z]:)?[^:*?""<>|]+\.(?:asm|inc)):(?<line>\d+)(?::(?<col>\d+))?\s*:",
        System.Text.RegularExpressions.RegexOptions.IgnoreCase |
        System.Text.RegularExpressions.RegexOptions.Compiled);

    /// <summary>
    /// Extrae la referencia archivo:línea de una línea de salida, o null si no la hay.
    /// </summary>
    public static SourceReference? Parse(string? outputLine)
    {
        if (string.IsNullOrWhiteSpace(outputLine)) return null;

        var m = Pattern.Match(outputLine);
        if (!m.Success) return null;

        if (!int.TryParse(m.Groups["line"].Value, out int line)) return null;
        if (line < 1) return null;

        var file = m.Groups["file"].Value.Trim();
        if (file.Length == 0) return null;

        return new SourceReference(file, line);
    }

    /// <summary>
    /// True si la línea de salida parece un error o advertencia, para colorearla.
    /// </summary>
    public static bool LooksLikeProblem(string? outputLine)
    {
        if (string.IsNullOrWhiteSpace(outputLine)) return false;
        return outputLine.Contains("error", StringComparison.OrdinalIgnoreCase)
            || outputLine.Contains("warning", StringComparison.OrdinalIgnoreCase)
            || Parse(outputLine) is not null;
    }
}
