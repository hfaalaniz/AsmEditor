using System.Text.RegularExpressions;

namespace AsmEditor.Core;

/// <summary>
/// Interpreta la salida de NASM y GoLink y la convierte en diagnósticos.
///
/// Los tres formatos están tomados de la salida real de las herramientas del proyecto
/// (NASM 3.02 y GoLink 1.0.4.6), no de la documentación:
///
///   1. NASM con ubicación:  archivo.asm:6: error: instruction expected, found `x'
///                           archivo.asm:5: warning: byte exceeds bounds [-w+number-overflow]
///   2. NASM fatal:          nasm: fatal: unable to open input file `nohay.asm'
///   3. GoLink:              Error!
///                           The following symbol was not defined in the object file or files:-
///                           NoExisteEstaFuncion
///
/// El formato 3 abarca varias líneas, así que el parser guarda estado: hay que
/// alimentarlo línea por línea con Feed() y cerrar con Flush() al terminar el proceso.
/// </summary>
public sealed class DiagnosticParser
{
    // archivo.asm:42: error: mensaje      (la letra de unidad de Windows lleva ':' propio)
    private static readonly Regex NasmWithLocation = new(
        @"^\s*(?<file>(?:[A-Za-z]:)?[^:*?""<>|]+\.(?:asm|inc|s)):(?<line>\d+)(?::(?<col>\d+))?:\s*" +
        @"(?<level>error|warning|fatal)\s*:\s*(?<msg>.*)$",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    // nasm: fatal: unable to open input file `nohay.asm'
    private static readonly Regex NasmToolLevel = new(
        @"^\s*nasm:\s*(?<level>error|warning|fatal)\s*:\s*(?<msg>.*)$",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    // Las advertencias de NASM traen la bandera al final: "... [-w+number-overflow]"
    private static readonly Regex WarningFlagSuffix = new(
        @"\s*\[-w[+-][^\]]*\]\s*$",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    // MASM (ml64.exe / ml.exe). Formatos reales capturados de la herramienta:
    //   malo.asm(4) : error A2008:syntax error : rax
    //   malo.asm(3) : error A2006:undefined symbol : noexiste
    //   hola.asm(7) : warning A4014:...
    //
    // ⚠ LA LÍNEA VA ENTRE PARÉNTESIS, no separada por dos puntos como en NASM.
    // Por eso hace falta un patrón propio y no alcanza con el de NASM.
    private static readonly Regex MasmWithLocation = new(
        @"^\s*(?<file>(?:[A-Za-z]:)?[^:*?""<>|()]+\.(?:asm|inc|s))\((?<line>\d+)\)\s*:\s*" +
        @"(?:(?<fatal>fatal)\s+)?(?<level>error|warning)\s+(?<code>A\d+)\s*:\s*(?<msg>.*)$",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    // MASM : fatal error A1000:cannot open file : nohay.asm
    private static readonly Regex MasmToolLevel = new(
        @"^\s*MASM\s*:\s*(?:(?<fatal>fatal)\s+)?(?<level>error|warning)\s+(?<code>A\d+)\s*:\s*(?<msg>.*)$",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    // link.exe de MSVC. Formatos reales capturados de la herramienta:
    //   LINK : fatal error LNK1181: cannot open input file 'kernel32.lib'
    //   LINK : error LNK2001: unresolved external symbol noexiste
    //   e.exe : fatal error LNK1120: 1 unresolved externals
    private static readonly Regex MsvcLink = new(
        @"^\s*(?<origin>[^:]+?)\s*:\s*(?:(?<fatal>fatal)\s+)?(?<level>error|warning)\s+" +
        @"(?<code>LNK\d+)\s*:\s*(?<msg>.*)$",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private readonly List<BuildDiagnostic> _diagnostics = new();

    /// <summary>Herramienta a la que se atribuyen las líneas que no dicen quién las emitió.</summary>
    public BuildTool CurrentTool { get; set; } = BuildTool.Nasm;

    // --- Estado del bloque multilínea de GoLink ---
    private bool _inGoLinkError;
    private string? _goLinkHeader;
    private readonly List<string> _goLinkDetails = new();

    public IReadOnlyList<BuildDiagnostic> Diagnostics => _diagnostics;

    public int ErrorCount => _diagnostics.Count(d => d.Level is DiagnosticLevel.Error or DiagnosticLevel.Fatal);
    public int WarningCount => _diagnostics.Count(d => d.Level == DiagnosticLevel.Warning);

    public void Clear()
    {
        _diagnostics.Clear();
        ResetGoLinkBlock();
    }

    /// <summary>
    /// Procesa una línea de salida. Devuelve los diagnósticos que esa línea completó
    /// (normalmente 0 o 1; el bloque de GoLink puede cerrar uno al llegar la línea siguiente).
    /// </summary>
    public IReadOnlyList<BuildDiagnostic> Feed(string? rawLine)
    {
        var produced = new List<BuildDiagnostic>();
        if (rawLine is null) return produced;

        var line = rawLine.TrimEnd('\r', '\n');

        // El BOM de GoLink al inicio de su salida no debe confundir a los patrones.
        line = line.TrimStart('﻿');

        if (string.IsNullOrWhiteSpace(line))
        {
            // Una línea en blanco cierra el bloque de GoLink que estuviera abierto.
            var closed = CloseGoLinkBlock();
            if (closed is not null) produced.Add(closed);
            return produced;
        }

        // 1) NASM con archivo y línea
        var m = NasmWithLocation.Match(line);
        if (m.Success)
        {
            var closed = CloseGoLinkBlock();
            if (closed is not null) produced.Add(closed);

            var diag = new BuildDiagnostic(
                Level: ParseLevel(m.Groups["level"].Value),
                FileName: m.Groups["file"].Value.Trim(),
                Line: int.TryParse(m.Groups["line"].Value, out var ln) ? ln : null,
                Message: CleanMessage(m.Groups["msg"].Value),
                Tool: BuildTool.Nasm,
                Column: m.Groups["col"].Success && int.TryParse(m.Groups["col"].Value, out var c) ? c : null);

            _diagnostics.Add(diag);
            produced.Add(diag);
            return produced;
        }

        // 2) NASM fatal sin ubicación
        m = NasmToolLevel.Match(line);
        if (m.Success)
        {
            var closed = CloseGoLinkBlock();
            if (closed is not null) produced.Add(closed);

            var diag = new BuildDiagnostic(
                Level: ParseLevel(m.Groups["level"].Value),
                FileName: null,
                Line: null,
                Message: CleanMessage(m.Groups["msg"].Value),
                Tool: BuildTool.Nasm);

            _diagnostics.Add(diag);
            produced.Add(diag);
            return produced;
        }

        // 3) MASM con archivo y línea: archivo.asm(4) : error A2008:...
        m = MasmWithLocation.Match(line);
        if (m.Success)
        {
            var closed = CloseGoLinkBlock();
            if (closed is not null) produced.Add(closed);

            var diag = new BuildDiagnostic(
                Level: NivelDe(m),
                FileName: m.Groups["file"].Value.Trim(),
                Line: int.TryParse(m.Groups["line"].Value, out var lm) ? lm : null,
                Message: $"{m.Groups["code"].Value.ToUpperInvariant()}: {m.Groups["msg"].Value.Trim()}",
                Tool: BuildTool.Masm);

            _diagnostics.Add(diag);
            produced.Add(diag);
            return produced;
        }

        // 4) MASM sin ubicación: MASM : fatal error A1000:...
        m = MasmToolLevel.Match(line);
        if (m.Success)
        {
            var closed = CloseGoLinkBlock();
            if (closed is not null) produced.Add(closed);

            var diag = new BuildDiagnostic(
                Level: NivelDe(m),
                FileName: null,
                Line: null,
                Message: $"{m.Groups["code"].Value.ToUpperInvariant()}: {m.Groups["msg"].Value.Trim()}",
                Tool: BuildTool.Masm);

            _diagnostics.Add(diag);
            produced.Add(diag);
            return produced;
        }

        // 5) link.exe de MSVC: una línea, con código LNK y sin número de línea del fuente.
        m = MsvcLink.Match(line);
        if (m.Success)
        {
            var closed = CloseGoLinkBlock();
            if (closed is not null) produced.Add(closed);

            bool isFatal = m.Groups["fatal"].Success;
            bool isWarning = m.Groups["level"].Value.Equals("warning", StringComparison.OrdinalIgnoreCase);

            var code = m.Groups["code"].Value.ToUpperInvariant();

            var diag = new BuildDiagnostic(
                Level: isWarning ? DiagnosticLevel.Warning
                     : isFatal ? DiagnosticLevel.Fatal
                     : DiagnosticLevel.Error,
                FileName: null,
                Line: null,
                Message: $"{code}: {m.Groups["msg"].Value.Trim()}",
                Tool: BuildTool.MsvcLink);

            _diagnostics.Add(diag);
            produced.Add(diag);
            return produced;
        }

        // 4) Bloque de GoLink: "Error!" abre, las líneas siguientes son el detalle.
        if (IsGoLinkErrorHeader(line))
        {
            var closed = CloseGoLinkBlock();
            if (closed is not null) produced.Add(closed);

            _inGoLinkError = true;
            _goLinkHeader = line.Trim();
            _goLinkDetails.Clear();
            return produced;
        }

        if (_inGoLinkError)
        {
            // "Output file not made" es el cierre del bloque, no parte del detalle.
            if (line.Contains("Output file not made", StringComparison.OrdinalIgnoreCase))
            {
                var closed = CloseGoLinkBlock();
                if (closed is not null) produced.Add(closed);
                return produced;
            }

            _goLinkDetails.Add(line.Trim());
            return produced;
        }

        return produced;
    }

    /// <summary>
    /// Cierra cualquier bloque pendiente. Hay que llamarlo al terminar el proceso,
    /// o el último error de GoLink se pierde por no haber línea siguiente.
    /// </summary>
    public IReadOnlyList<BuildDiagnostic> Flush()
    {
        var produced = new List<BuildDiagnostic>();
        var closed = CloseGoLinkBlock();
        if (closed is not null) produced.Add(closed);
        return produced;
    }

    private static bool IsGoLinkErrorHeader(string line)
    {
        var t = line.Trim();
        return t.Equals("Error!", StringComparison.OrdinalIgnoreCase)
            || t.Equals("Error !", StringComparison.OrdinalIgnoreCase)
            || t.StartsWith("Error:", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Convierte el bloque acumulado de GoLink en un diagnóstico. El encabezado suelto
    /// ("Error!") no dice nada útil, así que el mensaje es el detalle de las líneas siguientes.
    /// </summary>
    private BuildDiagnostic? CloseGoLinkBlock()
    {
        if (!_inGoLinkError) return null;

        var message = _goLinkDetails.Count > 0
            ? string.Join(" ", _goLinkDetails)
            : (_goLinkHeader ?? "Error de enlazado");

        ResetGoLinkBlock();

        var diag = new BuildDiagnostic(
            Level: DiagnosticLevel.Error,
            FileName: null,
            Line: null,
            Message: message.Trim(),
            Tool: BuildTool.GoLink);

        _diagnostics.Add(diag);
        return diag;
    }

    private void ResetGoLinkBlock()
    {
        _inGoLinkError = false;
        _goLinkHeader = null;
        _goLinkDetails.Clear();
    }

    /// <summary>
    /// Nivel a partir de los grupos «fatal» y «level» de un patrón. Lo comparten
    /// MASM y el link.exe de MSVC, que marcan lo fatal con una palabra aparte
    /// («fatal error A1000») en vez de un nivel propio como NASM.
    /// </summary>
    private static DiagnosticLevel NivelDe(Match m)
    {
        if (m.Groups["level"].Value.Equals("warning", StringComparison.OrdinalIgnoreCase))
            return DiagnosticLevel.Warning;

        return m.Groups["fatal"].Success ? DiagnosticLevel.Fatal : DiagnosticLevel.Error;
    }

    private static DiagnosticLevel ParseLevel(string text) => text.ToLowerInvariant() switch
    {
        "warning" => DiagnosticLevel.Warning,
        "fatal" => DiagnosticLevel.Fatal,
        _ => DiagnosticLevel.Error
    };

    /// <summary>Quita la bandera "[-w+algo]" que NASM agrega al final de sus advertencias.</summary>
    private static string CleanMessage(string message) =>
        WarningFlagSuffix.Replace(message, "").Trim();

    /// <summary>
    /// Atajo para procesar una salida completa de una sola vez (usado en las pruebas
    /// y para reprocesar texto ya capturado).
    /// </summary>
    public static IReadOnlyList<BuildDiagnostic> ParseAll(string output, BuildTool tool = BuildTool.Nasm)
    {
        var parser = new DiagnosticParser { CurrentTool = tool };
        foreach (var line in output.Split('\n'))
        {
            parser.Feed(line);
        }
        parser.Flush();
        return parser.Diagnostics;
    }
}
