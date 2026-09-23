namespace AsmEditor.Core;

/// <summary>Arquitectura de salida, que determina el -f de NASM.</summary>
public enum TargetArch
{
    Win32,
    Win64
}

/// <summary>Hasta dónde llega la compilación.</summary>
public enum TargetOutput
{
    /// <summary>Solo ensamblar: produce el .obj y se detiene.</summary>
    ObjectOnly,

    /// <summary>Ensamblar y enlazar: produce el .obj y el .exe.</summary>
    Executable
}

/// <summary>
/// Qué ensamblador usa el target.
///
/// ⚠ NO SON INTERCAMBIABLES: la sintaxis del fuente es distinta. Un .asm
/// escrito para NASM (section .text / global main) no ensambla con MASM
/// (.code / main proc), ni al revés. Cambiar este campo en un target NO
/// convierte el código; hay que escribirlo para el ensamblador elegido.
/// </summary>
public enum AssemblerKind
{
    /// <summary>NASM: sintaxis Intel propia, la arquitectura va en -f.</summary>
    Nasm,

    /// <summary>MASM de Microsoft: ml64.exe para 64 bits, ml.exe para 32.</summary>
    Masm
}

/// <summary>Qué enlazador usa el target.</summary>
public enum LinkerKind
{
    /// <summary>GoLink: sintaxis "/entry main", enlaza contra .dll.</summary>
    GoLink,

    /// <summary>link.exe de MSVC: sintaxis "/ENTRY:main /OUT:...", enlaza contra .lib.</summary>
    MsvcLink
}

/// <summary>Subsistema del ejecutable, para el /SUBSYSTEM de MSVC.</summary>
public enum TargetSubsystem
{
    Windows,
    Console
}

/// <summary>
/// Una configuración de compilación con nombre: arquitectura, qué produce,
/// punto de entrada, librerías de enlazado y banderas extra.
/// Reemplaza a tener que editar settings.json a mano.
/// </summary>
public sealed class BuildTarget
{
    public string Name { get; set; } = "Nuevo target";
    public TargetArch Arch { get; set; } = TargetArch.Win64;
    public TargetOutput Output { get; set; } = TargetOutput.Executable;
    public string EntryPoint { get; set; } = "main";
    public string Libraries { get; set; } = "kernel32.dll user32.dll gdi32.dll";

    /// <summary>Qué ensamblador usa este target.</summary>
    public AssemblerKind Assembler { get; set; } = AssemblerKind.Nasm;

    /// <summary>
    /// Ruta al ml64.exe / ml.exe de MASM. Vacío = buscarlo automáticamente
    /// (primero en masm_x64/masm_x86 del proyecto, después en Visual Studio).
    /// </summary>
    public string MasmPath { get; set; } = "";

    /// <summary>Qué enlazador usa este target.</summary>
    public LinkerKind Linker { get; set; } = LinkerKind.GoLink;

    /// <summary>Subsistema para el /SUBSYSTEM de MSVC (GoLink lo deduce solo).</summary>
    public TargetSubsystem Subsystem { get; set; } = TargetSubsystem.Windows;

    /// <summary>
    /// Ruta al link.exe de MSVC. Vacío = buscarlo automáticamente
    /// (primero en linker_x64/linker_x86 del proyecto, después en Visual Studio).
    /// </summary>
    public string MsvcLinkerPath { get; set; } = "";

    /// <summary>
    /// Carpeta de los .lib del SDK de Windows. Vacío = buscarla automáticamente.
    /// </summary>
    public string SdkLibPath { get; set; } = "";

    /// <summary>Banderas extra para NASM (por ejemplo "-g -Wall"). Puede quedar vacío.</summary>
    public string ExtraAsmFlags { get; set; } = "";

    /// <summary>Banderas extra para GoLink (por ejemplo "/console"). Puede quedar vacío.</summary>
    public string ExtraLinkFlags { get; set; } = "";

    /// <summary>El -f que le corresponde a NASM.</summary>
    public string NasmFormat => Arch == TargetArch.Win32 ? "win32" : "win64";

    public bool ProducesExecutable => Output == TargetOutput.Executable;

    public string ArchText => Arch == TargetArch.Win32 ? "32 bits" : "64 bits";

    public string OutputText => Output == TargetOutput.ObjectOnly ? ".obj" : ".obj + .exe";

    public string LinkerText => Linker == LinkerKind.MsvcLink ? "MSVC link" : "GoLink";

    public string AssemblerText => Assembler == AssemblerKind.Masm ? "MASM" : "NASM";

    public bool UsaMasm => Assembler == AssemblerKind.Masm;

    /// <summary>Texto para el desplegable de la barra.</summary>
    public string DisplayName => Output == TargetOutput.ObjectOnly
        ? $"{Name}  ({AssemblerText}, {ArchText}, solo .obj)"
        : $"{Name}  ({AssemblerText}, {ArchText}, {LinkerText})";

    public BuildTarget Clone() => new()
    {
        Name = Name,
        Arch = Arch,
        Output = Output,
        EntryPoint = EntryPoint,
        Libraries = Libraries,
        ExtraAsmFlags = ExtraAsmFlags,
        ExtraLinkFlags = ExtraLinkFlags,
        Assembler = Assembler,
        MasmPath = MasmPath,
        Linker = Linker,
        Subsystem = Subsystem,
        MsvcLinkerPath = MsvcLinkerPath,
        SdkLibPath = SdkLibPath
    };

    /// <summary>
    /// Argumentos para NASM. Las rutas van entre comillas porque pueden tener espacios.
    /// </summary>
    public string BuildNasmArguments(string sourcePath, string objPath)
    {
        var extra = string.IsNullOrWhiteSpace(ExtraAsmFlags) ? "" : ExtraAsmFlags.Trim() + " ";
        return $"-f {NasmFormat} {extra}\"{sourcePath}\" -o \"{objPath}\"";
    }

    /// <summary>
    /// Argumentos para MASM (ml64.exe / ml.exe).
    ///
    /// Detalles verificados contra la herramienta real:
    ///   - NO LLEVA EQUIVALENTE DE -f: la arquitectura la decide cuál de los dos
    ///     ejecutables se invoca (ml64 produce 64 bits, ml produce 32).
    ///   - /c es «solo ensamblar»: sin eso MASM intenta enlazar por su cuenta
    ///     llamando a link.exe, que puede no estar en el PATH.
    ///   - /Fo lleva la salida PEGADA o separada; se usa separada por claridad.
    ///   - El fuente va AL FINAL, después de las opciones.
    /// </summary>
    public string BuildMasmArguments(string sourcePath, string objPath)
    {
        var extra = string.IsNullOrWhiteSpace(ExtraAsmFlags) ? "" : ExtraAsmFlags.Trim() + " ";
        return $"/nologo /c {extra}/Fo \"{objPath}\" \"{sourcePath}\"";
    }

    /// <summary>Los argumentos del ensamblador que corresponda al target.</summary>
    public string BuildAssemblerArguments(string sourcePath, string objPath) =>
        UsaMasm ? BuildMasmArguments(sourcePath, objPath)
                : BuildNasmArguments(sourcePath, objPath);

    /// <summary>
    /// Argumentos para GoLink. El enlazado de 32 bits necesita /mix o las libs
    /// correspondientes; eso queda a cargo de las banderas extra del target.
    /// </summary>
    public string BuildGoLinkArguments(string objPath)
    {
        var extra = string.IsNullOrWhiteSpace(ExtraLinkFlags) ? "" : ExtraLinkFlags.Trim() + " ";
        var libs = string.IsNullOrWhiteSpace(Libraries) ? "" : " " + Libraries.Trim();
        return $"/entry {EntryPoint} {extra}\"{objPath}\"{libs}";
    }

    /// <summary>
    /// Argumentos para el link.exe de MSVC.
    ///
    /// Detalles verificados contra la herramienta real:
    ///   - El punto de entrada va SIN guion bajo aunque el fuente de 32 bits declare
    ///     "_main": NASM con -f win32 ya agrega el guion, y pasar /ENTRY:_main hace
    ///     que el linker busque "__main" y falle con LNK2001.
    ///   - Enlaza contra .lib, no contra .dll; los nombres .dll se traducen.
    ///   - No hace falta vcvars: alcanza con /LIBPATH a las libs del SDK.
    /// </summary>
    public string BuildMsvcArguments(string objPath, string exePath, string? sdkLibPath)
    {
        var subsystem = Subsystem == TargetSubsystem.Console ? "CONSOLE" : "WINDOWS";

        var args = $"/NOLOGO /SUBSYSTEM:{subsystem}";

        // ⚠ EL /ENTRY VA SIEMPRE, MENOS EN MASM DE 32 BITS. Los tres casos,
        // medidos enlazando de verdad:
        //
        //                        sin /ENTRY          con /ENTRY
        //   MASM 32 bits         funciona            funciona, con LNK4258
        //   MASM 64 bits         FALLA (LNK2001)     funciona
        //   NASM                 FALLA               funciona
        //
        // En 32 bits MASM declara el punto de entrada en el .obj con su
        // decoración stdcall («main@0»), por el «end main» del fuente, y pasar
        // /ENTRY:main choca con esa directiva:
        //   warning LNK4258: directive '/ENTRY:main@0' not compatible
        //   with switch '/ENTRY:main'; ignored
        //
        // En 64 bits no hay «end main» —MASM no lo admite— así que sin /ENTRY
        // el linker busca el arranque del runtime de C y no lo encuentra:
        //   error LNK2001: unresolved external symbol WinMainCRTStartup
        bool masm32 = UsaMasm && Arch == TargetArch.Win32;

        if (!masm32)
        {
            args += $" /ENTRY:{NormalizeMsvcEntry(EntryPoint)}";
        }

        args += $" /OUT:\"{exePath}\"";

        var libPath = string.IsNullOrWhiteSpace(SdkLibPath) ? sdkLibPath : SdkLibPath;
        if (!string.IsNullOrWhiteSpace(libPath))
        {
            args += $" /LIBPATH:\"{libPath}\"";
        }

        if (!string.IsNullOrWhiteSpace(ExtraLinkFlags))
        {
            args += " " + ExtraLinkFlags.Trim();
        }

        args += $" \"{objPath}\"";

        var libs = TranslateLibrariesForMsvc(Libraries);
        if (!string.IsNullOrWhiteSpace(libs)) args += " " + libs;

        return args;
    }

    /// <summary>
    /// MSVC busca el símbolo tal cual se lo nombra, y NASM con -f win32 ya decora
    /// las etiquetas con un guion bajo. Se lo sacamos para que no busque "__main".
    /// </summary>
    private string NormalizeMsvcEntry(string entry)
    {
        var e = entry.Trim();
        if (Arch == TargetArch.Win32 && e.StartsWith('_') && e.Length > 1)
        {
            return e[1..];
        }
        return e;
    }

    /// <summary>
    /// GoLink usa "kernel32.dll" y MSVC usa "kernel32.lib". Para que el mismo campo
    /// de librerías sirva con los dos enlazadores, se traduce la extensión.
    /// </summary>
    public static string TranslateLibrariesForMsvc(string libraries)
    {
        if (string.IsNullOrWhiteSpace(libraries)) return "";

        var partes = libraries.Split(new[] { ' ', '\t', ',', ';' }, StringSplitOptions.RemoveEmptyEntries);

        return string.Join(" ", partes.Select(p =>
            p.EndsWith(".dll", StringComparison.OrdinalIgnoreCase)
                ? string.Concat(p.AsSpan(0, p.Length - 4), ".lib")
                : p));
    }

    /// <summary>Los targets que se crean cuando no hay configuración previa.</summary>
    public static List<BuildTarget> CreateDefaults() => new()
    {
        new BuildTarget
        {
            Name = "Win64 ventana",
            Arch = TargetArch.Win64,
            Output = TargetOutput.Executable,
            EntryPoint = "main",
            Libraries = "kernel32.dll user32.dll gdi32.dll"
        },
        new BuildTarget
        {
            Name = "Win64 consola",
            Arch = TargetArch.Win64,
            Output = TargetOutput.Executable,
            EntryPoint = "main",
            Libraries = "kernel32.dll",
            ExtraLinkFlags = "/console"
        },
        new BuildTarget
        {
            Name = "Win32 ventana",
            Arch = TargetArch.Win32,
            Output = TargetOutput.Executable,
            EntryPoint = "_main",
            Libraries = "kernel32.dll user32.dll gdi32.dll"
        },
        new BuildTarget
        {
            Name = "MSVC 64 ventana",
            Arch = TargetArch.Win64,
            Output = TargetOutput.Executable,
            Linker = LinkerKind.MsvcLink,
            Subsystem = TargetSubsystem.Windows,
            EntryPoint = "main",
            Libraries = "kernel32.dll user32.dll gdi32.dll"
        },
        new BuildTarget
        {
            Name = "MSVC 32 ventana",
            Arch = TargetArch.Win32,
            Output = TargetOutput.Executable,
            Linker = LinkerKind.MsvcLink,
            Subsystem = TargetSubsystem.Windows,
            EntryPoint = "_main",
            Libraries = "kernel32.dll user32.dll"
        },
        new BuildTarget
        {
            Name = "MSVC 64 consola",
            Arch = TargetArch.Win64,
            Output = TargetOutput.Executable,
            Linker = LinkerKind.MsvcLink,
            Subsystem = TargetSubsystem.Console,
            EntryPoint = "main",
            Libraries = "kernel32.dll"
        },
        new BuildTarget
        {
            Name = "MASM 64 ventana",
            Assembler = AssemblerKind.Masm,
            Arch = TargetArch.Win64,
            Output = TargetOutput.Executable,
            Linker = LinkerKind.MsvcLink,
            Subsystem = TargetSubsystem.Windows,
            EntryPoint = "main",
            Libraries = "kernel32.dll user32.dll"
        },
        new BuildTarget
        {
            Name = "MASM 32 ventana",
            Assembler = AssemblerKind.Masm,
            Arch = TargetArch.Win32,
            Output = TargetOutput.Executable,
            Linker = LinkerKind.MsvcLink,
            Subsystem = TargetSubsystem.Windows,
            EntryPoint = "main",
            Libraries = "kernel32.dll user32.dll"
        },
        new BuildTarget
        {
            Name = "MASM 64 consola",
            Assembler = AssemblerKind.Masm,
            Arch = TargetArch.Win64,
            Output = TargetOutput.Executable,
            Linker = LinkerKind.MsvcLink,
            Subsystem = TargetSubsystem.Console,
            EntryPoint = "main",
            Libraries = "kernel32.dll"
        },
        new BuildTarget
        {
            Name = "Solo ensamblar (64)",
            Arch = TargetArch.Win64,
            Output = TargetOutput.ObjectOnly,
            EntryPoint = "main",
            Libraries = ""
        }
    };
}
