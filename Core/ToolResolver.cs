namespace AsmEditor.Core;

/// <summary>
/// Qué herramienta se usa de verdad para un target.
///
/// ⚠ ACÁ VIVE LA PRECEDENCIA, Y EN UN SOLO LUGAR. Son tres niveles:
///
///     1. Lo que fije el TARGET      (MasmPath, MsvcLinkerPath, SdkLibPath)
///     2. Lo que fije la CONFIGURACIÓN global (el toolchain y el SDK elegidos)
///     3. Autodetección              (proyecto primero, después Visual Studio)
///
/// Antes esta cadena estaba repartida entre MainForm, ToolReport y la vista
/// previa del editor de targets, cada uno con su propia versión: por eso la
/// vista previa mostraba un comando sin /LIBPATH que no era el que se ejecutaba.
/// Si se agrega un nivel, se agrega acá y lo heredan los tres.
///
/// Es una función pura: entra la configuración, sale una ruta. Lo único que
/// toca el disco es la autodetección de LinkerLocator.
/// </summary>
public static class ToolResolver
{
    /// <summary>El ensamblador del target: NASM de la configuración, o MASM por la cadena.</summary>
    public static string? Assembler(BuildTarget t, BuildConfig cfg)
    {
        if (!t.UsaMasm)
        {
            // NASM no participa de los toolchains: es una herramienta suelta.
            return string.IsNullOrWhiteSpace(cfg.NasmPath) ? null : cfg.NasmPath;
        }

        if (!string.IsNullOrWhiteSpace(t.MasmPath)) return t.MasmPath;

        var dir = ToolchainDir(t.Arch, cfg);
        if (!string.IsNullOrWhiteSpace(dir))
        {
            return Path.Combine(dir, MasmExe(t.Arch));
        }

        return LinkerLocator.FindMasm(t.Arch, cfg.ProjectFolder);
    }

    /// <summary>El enlazador del target: GoLink de la configuración, o link.exe por la cadena.</summary>
    public static string? Linker(BuildTarget t, BuildConfig cfg)
    {
        if (t.Linker != LinkerKind.MsvcLink)
        {
            return string.IsNullOrWhiteSpace(cfg.GoLinkPath) ? null : cfg.GoLinkPath;
        }

        if (!string.IsNullOrWhiteSpace(t.MsvcLinkerPath)) return t.MsvcLinkerPath;

        var dir = ToolchainDir(t.Arch, cfg);
        if (!string.IsNullOrWhiteSpace(dir))
        {
            // ⚠ EL link.exe SALE DE LA MISMA CARPETA QUE EL ml64.exe: van
            // apareados y no se mezclan versiones.
            return Path.Combine(dir, "link.exe");
        }

        return LinkerLocator.FindMsvcLinker(t.Arch, cfg.ProjectFolder);
    }

    /// <summary>
    /// Las librerías del SDK. Solo le sirven a link.exe: GoLink enlaza contra
    /// los .dll y no las necesita.
    /// </summary>
    public static string? SdkLib(BuildTarget t, BuildConfig cfg)
    {
        if (t.Linker != LinkerKind.MsvcLink) return null;

        if (!string.IsNullOrWhiteSpace(t.SdkLibPath)) return t.SdkLibPath;

        var global = t.Arch == TargetArch.Win32 ? cfg.SdkLibPath32 : cfg.SdkLibPath64;
        if (!string.IsNullOrWhiteSpace(global)) return global;

        return LinkerLocator.FindSdkLibPath(t.Arch);
    }

    /// <summary>La carpeta del toolchain elegido globalmente para esa arquitectura.</summary>
    public static string ToolchainDir(TargetArch arch, BuildConfig cfg) =>
        arch == TargetArch.Win32 ? cfg.MsvcToolchainDir32 : cfg.MsvcToolchainDir64;

    /// <summary>
    /// Fija el toolchain global de una arquitectura. Vacío o null vuelve a la
    /// autodetección.
    /// </summary>
    public static void SetToolchainDir(TargetArch arch, BuildConfig cfg, string? dir)
    {
        var v = dir ?? "";
        if (arch == TargetArch.Win32) cfg.MsvcToolchainDir32 = v;
        else cfg.MsvcToolchainDir64 = v;
    }

    /// <summary>Fija el SDK global de una arquitectura. Vacío o null vuelve a la autodetección.</summary>
    public static void SetSdkLibPath(TargetArch arch, BuildConfig cfg, string? path)
    {
        var v = path ?? "";
        if (arch == TargetArch.Win32) cfg.SdkLibPath32 = v;
        else cfg.SdkLibPath64 = v;
    }

    /// <summary>
    /// ml.exe o ml64.exe.
    /// ⚠ SON DOS PROGRAMAS DISTINTOS: MASM no tiene el -f de NASM y la
    /// arquitectura la decide cuál de los dos se invoca.
    /// </summary>
    public static string MasmExe(TargetArch arch) =>
        arch == TargetArch.Win32 ? "ml.exe" : "ml64.exe";

    /// <summary>De dónde salió la ruta: sirve para explicarlo en los diálogos.</summary>
    public static ToolSource SourceOfAssembler(BuildTarget t, BuildConfig cfg)
    {
        if (!t.UsaMasm) return ToolSource.Configuracion;
        if (!string.IsNullOrWhiteSpace(t.MasmPath)) return ToolSource.Target;
        return string.IsNullOrWhiteSpace(ToolchainDir(t.Arch, cfg))
            ? ToolSource.Automatico
            : ToolSource.Configuracion;
    }

    /// <summary>De dónde salió la ruta del enlazador.</summary>
    public static ToolSource SourceOfLinker(BuildTarget t, BuildConfig cfg)
    {
        if (t.Linker != LinkerKind.MsvcLink) return ToolSource.Configuracion;
        if (!string.IsNullOrWhiteSpace(t.MsvcLinkerPath)) return ToolSource.Target;
        return string.IsNullOrWhiteSpace(ToolchainDir(t.Arch, cfg))
            ? ToolSource.Automatico
            : ToolSource.Configuracion;
    }

    /// <summary>De dónde salió la carpeta del SDK.</summary>
    public static ToolSource SourceOfSdk(BuildTarget t, BuildConfig cfg)
    {
        if (!string.IsNullOrWhiteSpace(t.SdkLibPath)) return ToolSource.Target;
        var global = t.Arch == TargetArch.Win32 ? cfg.SdkLibPath32 : cfg.SdkLibPath64;
        return string.IsNullOrWhiteSpace(global) ? ToolSource.Automatico : ToolSource.Configuracion;
    }
}

/// <summary>Quién decidió la ruta de una herramienta.</summary>
public enum ToolSource
{
    /// <summary>La eligió el editor solo.</summary>
    Automatico,

    /// <summary>Sale de la configuración global.</summary>
    Configuracion,

    /// <summary>La fija este target en particular, pisando todo lo demás.</summary>
    Target
}
