namespace AsmEditor.Core;

/// <summary>
/// Un juego de herramientas de MSVC: el ensamblador y el enlazador que vienen
/// de la MISMA instalación y la MISMA versión.
///
/// ⚠ ml64.exe Y link.exe VAN APAREADOS, NO SE MEZCLAN VERSIONES. Están en la
/// misma carpeta —verificado en las 8 instalaciones de este equipo— y forman un
/// juego: combinar el ml64 de una versión con el link.exe de otra mete
/// diferencias de formato de objeto que aparecen como errores de enlazado
/// difíciles de atribuir. Por eso el toolchain se elige entero.
///
/// El SDK de Windows NO es parte del toolchain: sus versiones son
/// independientes de las de MSVC y se eligen aparte (ver <see cref="SdkVersion"/>).
/// </summary>
public sealed class Toolchain
{
    /// <summary>De dónde salió: las carpetas del proyecto o una instalación de VS.</summary>
    public required ToolchainOrigin Origin { get; init; }

    /// <summary>
    /// Cómo se muestra en los selectores. Para VS incluye edición y versión
    /// («Visual Studio 18 Enterprise — 14.51.36231»); para el proyecto dice
    /// de dónde sale, porque no tiene número de versión.
    /// </summary>
    public required string DisplayName { get; init; }

    /// <summary>
    /// Versión de MSVC (14.51.36231). Null en las herramientas del proyecto:
    /// son copias sueltas y no declaran versión.
    /// </summary>
    public Version? Version { get; init; }

    /// <summary>ml.exe o ml64.exe según la arquitectura. Null si esa arquitectura no está.</summary>
    public string? MasmPath { get; init; }

    /// <summary>link.exe de la misma versión. Null si esa arquitectura no está.</summary>
    public string? LinkerPath { get; init; }

    /// <summary>La arquitectura que produce este juego.</summary>
    public required TargetArch Arch { get; init; }

    /// <summary>Sirve para ensamblar y enlazar: están las dos herramientas.</summary>
    public bool Completo => MasmPath is not null && LinkerPath is not null;

    public override string ToString() => DisplayName;
}

/// <summary>De dónde viene un toolchain. El orden importa: define la prioridad.</summary>
public enum ToolchainOrigin
{
    /// <summary>
    /// Las carpetas masm_x64 / linker_x64 del propio proyecto.
    /// ⚠ TIENEN PRIORIDAD sobre Visual Studio, para que el editor no dependa de
    /// dónde esté instalado VS ni de que esté instalado.
    /// </summary>
    Proyecto = 0,

    /// <summary>Una instalación de Visual Studio.</summary>
    VisualStudio = 1
}

/// <summary>
/// Una versión del SDK de Windows (10.0.26100.0). Se elige aparte del toolchain
/// porque sus versiones son independientes de las de MSVC.
/// </summary>
public sealed class SdkVersion
{
    public required string DisplayName { get; init; }
    public required Version Version { get; init; }

    /// <summary>Carpeta con kernel32.lib y compañía para la arquitectura pedida.</summary>
    public required string LibPath { get; init; }

    public override string ToString() => DisplayName;
}
