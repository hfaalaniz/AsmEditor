namespace AsmEditor.Core;

/// <summary>
/// Encuentra el link.exe de MSVC y las librerías del SDK de Windows que le
/// corresponden a cada arquitectura.
///
/// Todo lo de acá está verificado contra la instalación real de este equipo:
///
///   - Dentro de bin\ hay un link.exe por combinación Host/destino, y NO son el
///     mismo binario (hashes distintos). Host = dónde corre, la segunda carpeta =
///     qué arquitectura produce. Para una máquina x64:
///         64 bits -> bin\Hostx64\x64\link.exe
///         32 bits -> bin\Hostx64\x86\link.exe
///
///   - NO hace falta ejecutar vcvars64.bat: alcanza con pasarle /LIBPATH apuntando
///     a las librerías del SDK. Probado enlazando y ejecutando en ambas arquitecturas.
///
///   - Los .lib del SDK están separados por arquitectura: um\x64 y um\x86.
/// </summary>
public static class LinkerLocator
{
    /// <summary>
    /// Dónde vive Visual Studio. Solo las dos carpetas raíz: las ediciones y los
    /// años se ENUMERAN, no se listan.
    ///
    /// ⚠ NO VOLVER A UNA LISTA FIJA DE EDICIONES. La que había acá nombraba
    /// Enterprise/Professional/Community de 2019, 2022 y 18, y por eso no veía
    /// «18\Insiders»: en este equipo eso escondía 4 de los 8 toolchains
    /// instalados. Cualquier edición nueva volvería a quedar invisible.
    /// </summary>
    private static readonly string[] VisualStudioBases =
    {
        @"C:\Program Files\Microsoft Visual Studio",
        @"C:\Program Files (x86)\Microsoft Visual Studio"
    };

    /// <summary>
    /// Las instalaciones de Visual Studio que hay, enumerando dos niveles:
    /// año o versión («2022», «18») y después edición («Enterprise», «Insiders»).
    /// De la más nueva a la más vieja.
    /// </summary>
    private static IEnumerable<string> VisualStudioRoots()
    {
        foreach (var baseDir in VisualStudioBases)
        {
            if (!Directory.Exists(baseDir)) continue;

            IEnumerable<string> años;
            try { años = Directory.EnumerateDirectories(baseDir); }
            catch { continue; }

            // «Installer» y «Shared» también cuelgan de acá: no son instalaciones,
            // y quedan descartadas solas porque no tienen VC\Tools\MSVC adentro.
            foreach (var año in años.OrderByDescending(d => Path.GetFileName(d)))
            {
                IEnumerable<string> ediciones;
                try { ediciones = Directory.EnumerateDirectories(año); }
                catch { continue; }

                foreach (var edicion in ediciones.OrderBy(Path.GetFileName))
                {
                    if (Directory.Exists(Path.Combine(edicion, @"VC\Tools\MSVC")))
                    {
                        yield return edicion;
                    }
                }
            }
        }
    }

    private const string WindowsKitsLib = @"C:\Program Files (x86)\Windows Kits\10\Lib";

    /// <summary>
    /// Carpetas del propio proyecto donde viven las copias de link.exe, una por
    /// arquitectura, con sus DLL de dependencia al lado. Se prueban antes que
    /// Visual Studio para que el editor no dependa de dónde esté instalado VS.
    /// </summary>
    private static string ProjectLinkerFolder(TargetArch arch) =>
        arch == TargetArch.Win32 ? "linker_x86" : "linker_x64";

    /// <summary>Carpetas del proyecto donde viven las copias de MASM.</summary>
    private static string ProjectMasmFolder(TargetArch arch) =>
        arch == TargetArch.Win32 ? "masm_x86" : "masm_x64";

    /// <summary>
    /// El ejecutable de MASM según la arquitectura.
    ///
    /// ⚠ SON DOS PROGRAMAS DISTINTOS, no el mismo con una bandera: ml64.exe
    /// produce objetos de 64 bits y ml.exe de 32. MASM no tiene equivalente
    /// del -f de NASM.
    /// </summary>
    private static string MasmExeName(TargetArch arch) =>
        arch == TargetArch.Win32 ? "ml.exe" : "ml64.exe";

    /// <summary>
    /// Busca el ensamblador de MASM para la arquitectura dada: primero en las
    /// carpetas del proyecto, después en Visual Studio (la versión más nueva).
    /// </summary>
    public static string? FindMasm(TargetArch arch, string? projectFolder = null)
    {
        var exe = MasmExeName(arch);

        if (!string.IsNullOrWhiteSpace(projectFolder))
        {
            var propio = Path.Combine(projectFolder, ProjectMasmFolder(arch), exe);
            if (File.Exists(propio)) return propio;
        }

        foreach (var root in VisualStudioRoots())
        {
            var msvc = Path.Combine(root, @"VC\Tools\MSVC");
            if (!Directory.Exists(msvc)) continue;

            foreach (var version in EnumerateVersionsNewestFirst(msvc))
            {
                // El nombre de la carpeta Host cambia de mayúsculas entre
                // versiones (HostX64 en 14.29, Hostx64 en 14.44).
                foreach (var host in new[] { "Hostx64", "HostX64" })
                {
                    var candidato = Path.Combine(version, "bin", host, TargetFolder(arch), exe);
                    if (File.Exists(candidato)) return candidato;
                }
            }
        }

        return null;
    }

    public static bool IsMasmAvailable(TargetArch arch, string? projectFolder = null) =>
        FindMasm(arch, projectFolder) is not null;

    /// <summary>Subcarpeta de destino dentro de bin\HostX64 según la arquitectura.</summary>
    private static string TargetFolder(TargetArch arch) => arch == TargetArch.Win32 ? "x86" : "x64";

    /// <summary>
    /// Busca el link.exe para la arquitectura dada: primero en las carpetas del
    /// proyecto, después en las instalaciones de Visual Studio (la versión más nueva).
    /// Devuelve null si no aparece en ningún lado.
    /// </summary>
    public static string? FindMsvcLinker(TargetArch arch, string? projectFolder = null)
    {
        if (!string.IsNullOrWhiteSpace(projectFolder))
        {
            var own = Path.Combine(projectFolder, ProjectLinkerFolder(arch), "link.exe");
            if (File.Exists(own)) return own;
        }

        foreach (var root in VisualStudioRoots())
        {
            var msvc = Path.Combine(root, @"VC\Tools\MSVC");
            if (!Directory.Exists(msvc)) continue;

            foreach (var version in EnumerateVersionsNewestFirst(msvc))
            {
                // El nombre de la carpeta Host cambia de mayúsculas entre versiones
                // (HostX64 en 14.29, Hostx64 en 14.44), así que se prueban las dos.
                foreach (var host in new[] { "Hostx64", "HostX64" })
                {
                    var candidate = Path.Combine(version, "bin", host, TargetFolder(arch), "link.exe");
                    if (File.Exists(candidate)) return candidate;
                }
            }
        }

        return null;
    }

    /// <summary>
    /// Carpeta de los .lib del SDK de Windows para la arquitectura dada
    /// (kernel32.lib, user32.lib, gdi32.lib...). Null si no está el SDK.
    /// </summary>
    public static string? FindSdkLibPath(TargetArch arch)
    {
        if (!Directory.Exists(WindowsKitsLib)) return null;

        foreach (var version in EnumerateVersionsNewestFirst(WindowsKitsLib))
        {
            var candidate = Path.Combine(version, "um", TargetFolder(arch));
            if (File.Exists(Path.Combine(candidate, "kernel32.lib"))) return candidate;
        }

        return null;
    }

    // ═══════════════════════════════════════════════════════════════════
    //  Descubrimiento: TODO lo instalado, no solo lo primero que aparece
    // ═══════════════════════════════════════════════════════════════════

    /// <summary>
    /// Todos los juegos de herramientas disponibles para la arquitectura dada,
    /// en orden de prioridad: primero el del proyecto, después los de Visual
    /// Studio de la versión más nueva a la más vieja.
    ///
    /// ⚠ ml64 y link.exe SALEN APAREADOS de la misma carpeta. Están juntos en
    /// las 8 instalaciones de este equipo, así que el juego se arma entero y no
    /// se mezclan versiones.
    ///
    /// A diferencia de FindMasm / FindMsvcLinker —que devuelven el primero que
    /// sirve— esto devuelve la lista completa, para poder elegir.
    /// </summary>
    public static List<Toolchain> DiscoverToolchains(TargetArch arch, string? projectFolder = null)
    {
        var hallados = new List<Toolchain>();
        var exe = MasmExeName(arch);

        // ── El del proyecto va primero: es la prioridad ──────────────────
        if (!string.IsNullOrWhiteSpace(projectFolder))
        {
            var masm = Path.Combine(projectFolder, ProjectMasmFolder(arch), exe);
            var link = Path.Combine(projectFolder, ProjectLinkerFolder(arch), "link.exe");

            bool hayMasm = File.Exists(masm);
            bool hayLink = File.Exists(link);

            if (hayMasm || hayLink)
            {
                hallados.Add(new Toolchain
                {
                    Origin = ToolchainOrigin.Proyecto,
                    DisplayName = "Herramientas del proyecto",
                    Version = null,   // son copias sueltas: no declaran versión
                    MasmPath = hayMasm ? masm : null,
                    LinkerPath = hayLink ? link : null,
                    Arch = arch
                });
            }
        }

        // ── Los de Visual Studio ─────────────────────────────────────────
        foreach (var root in VisualStudioRoots())
        {
            var msvc = Path.Combine(root, @"VC\Tools\MSVC");

            foreach (var version in EnumerateVersionsNewestFirst(msvc))
            {
                // El nombre de la carpeta Host cambia de mayúsculas entre
                // versiones (HostX64 en 14.29, Hostx64 en 14.44).
                foreach (var host in new[] { "Hostx64", "HostX64" })
                {
                    var bin = Path.Combine(version, "bin", host, TargetFolder(arch));

                    var masm = Path.Combine(bin, exe);
                    var link = Path.Combine(bin, "link.exe");

                    bool hayMasm = File.Exists(masm);
                    bool hayLink = File.Exists(link);
                    if (!hayMasm && !hayLink) continue;

                    hallados.Add(new Toolchain
                    {
                        Origin = ToolchainOrigin.VisualStudio,
                        DisplayName = NombreDeInstalacion(root, Path.GetFileName(version)),
                        Version = ParseVersion(Path.GetFileName(version)),
                        MasmPath = hayMasm ? masm : null,
                        LinkerPath = hayLink ? link : null,
                        Arch = arch
                    });

                    break;   // ya se resolvió el Host de esta versión
                }
            }
        }

        return hallados;
    }

    /// <summary>
    /// Cómo se lee una instalación en el selector: «Visual Studio 18 Enterprise — 14.51.36231».
    /// El año y la edición salen de las dos últimas carpetas de la ruta.
    /// </summary>
    private static string NombreDeInstalacion(string root, string version)
    {
        var edicion = Path.GetFileName(root);
        var año = Path.GetFileName(Path.GetDirectoryName(root) ?? "");

        return $"Visual Studio {año} {edicion} — {version}";
    }

    /// <summary>
    /// Todas las versiones del SDK de Windows que tienen las librerías de la
    /// arquitectura dada, de la más nueva a la más vieja.
    ///
    /// El SDK se elige APARTE del toolchain: sus versiones son independientes
    /// de las de MSVC.
    /// </summary>
    public static List<SdkVersion> DiscoverSdks(TargetArch arch)
    {
        var hallados = new List<SdkVersion>();
        if (!Directory.Exists(WindowsKitsLib)) return hallados;

        foreach (var version in EnumerateVersionsNewestFirst(WindowsKitsLib))
        {
            var lib = Path.Combine(version, "um", TargetFolder(arch));

            // Que exista la carpeta no alcanza: un SDK puede estar instalado
            // para una arquitectura y no para la otra.
            if (!File.Exists(Path.Combine(lib, "kernel32.lib"))) continue;

            var nombre = Path.GetFileName(version);
            hallados.Add(new SdkVersion
            {
                DisplayName = $"SDK {nombre}",
                Version = ParseVersion(nombre),
                LibPath = lib
            });
        }

        return hallados;
    }

    /// <summary>Subcarpetas ordenadas por número de versión, de la más nueva a la más vieja.</summary>
    private static IEnumerable<string> EnumerateVersionsNewestFirst(string parent)
    {
        IEnumerable<string> dirs;
        try { dirs = Directory.EnumerateDirectories(parent); }
        catch { yield break; }

        var ordered = dirs
            .Select(d => (Path: d, Version: ParseVersion(Path.GetFileName(d))))
            .OrderByDescending(x => x.Version)
            .Select(x => x.Path);

        foreach (var d in ordered) yield return d;
    }

    private static Version ParseVersion(string name) =>
        Version.TryParse(name, out var v) ? v : new Version(0, 0);

    public static bool IsMsvcAvailable(TargetArch arch, string? projectFolder = null) =>
        FindMsvcLinker(arch, projectFolder) is not null && FindSdkLibPath(arch) is not null;
}
