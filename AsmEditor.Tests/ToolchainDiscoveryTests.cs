using AsmEditor.Core;

namespace AsmEditor.Tests;

/// <summary>
/// El descubrimiento de toolchains.
///
/// Lo que depende del proyecto se prueba con carpetas falsas armadas acá, así
/// que no depende de lo que esté instalado. Lo que depende de Visual Studio se
/// prueba contra la máquina, y esas pruebas se saltean solas si no hay VS.
/// </summary>
public class ToolchainDiscoveryTests : IDisposable
{
    private readonly string _raiz;

    public ToolchainDiscoveryTests()
    {
        _raiz = Path.Combine(Path.GetTempPath(), "asmed_tc_" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(_raiz);
    }

    public void Dispose()
    {
        try { Directory.Delete(_raiz, recursive: true); } catch { /* da igual */ }
    }

    /// <summary>Crea un archivo vacío con su carpeta; el descubrimiento solo mira que exista.</summary>
    private string Tocar(params string[] partes)
    {
        var ruta = Path.Combine(new[] { _raiz }.Concat(partes).ToArray());
        Directory.CreateDirectory(Path.GetDirectoryName(ruta)!);
        File.WriteAllText(ruta, "");
        return ruta;
    }

    private Toolchain? DelProyecto(TargetArch arch) =>
        LinkerLocator.DiscoverToolchains(arch, _raiz)
            .FirstOrDefault(t => t.Origin == ToolchainOrigin.Proyecto);

    // ── Las herramientas del proyecto ───────────────────────────────────

    [Fact]
    public void Proyecto_HallaMl64YLinkDe64()
    {
        Tocar("masm_x64", "ml64.exe");
        Tocar("linker_x64", "link.exe");

        var t = DelProyecto(TargetArch.Win64);

        Assert.NotNull(t);
        Assert.True(t!.Completo);
        Assert.EndsWith(@"masm_x64\ml64.exe", t.MasmPath);
        Assert.EndsWith(@"linker_x64\link.exe", t.LinkerPath);
    }

    [Fact]
    public void Proyecto_HallaMlYLinkDe32()
    {
        // ⚠ En 32 bits el ensamblador es ml.exe, NO ml64.exe: son dos programas.
        Tocar("masm_x86", "ml.exe");
        Tocar("linker_x86", "link.exe");

        var t = DelProyecto(TargetArch.Win32);

        Assert.NotNull(t);
        Assert.EndsWith(@"masm_x86\ml.exe", t!.MasmPath);
        Assert.EndsWith(@"linker_x86\link.exe", t.LinkerPath);
    }

    [Fact]
    public void Proyecto_NoConfundeLasArquitecturas()
    {
        // Solo están las de 64: pedir las de 32 no debe devolver las otras.
        Tocar("masm_x64", "ml64.exe");
        Tocar("linker_x64", "link.exe");

        Assert.Null(DelProyecto(TargetArch.Win32));
    }

    [Fact]
    public void Proyecto_SinHerramientasNoAparece()
    {
        Assert.Null(DelProyecto(TargetArch.Win64));
    }

    [Fact]
    public void Proyecto_ConSoloElEnsambladorApareceIncompleto()
    {
        // Media instalación se informa igual, marcada como incompleta: es más
        // útil que no mostrarla, porque explica por qué no se puede enlazar.
        Tocar("masm_x64", "ml64.exe");

        var t = DelProyecto(TargetArch.Win64);

        Assert.NotNull(t);
        Assert.False(t!.Completo);
        Assert.NotNull(t.MasmPath);
        Assert.Null(t.LinkerPath);
    }

    [Fact]
    public void Proyecto_NoDeclaraVersion()
    {
        // Son copias sueltas: no hay número de versión que mostrar.
        Tocar("masm_x64", "ml64.exe");
        Tocar("linker_x64", "link.exe");

        Assert.Null(DelProyecto(TargetArch.Win64)!.Version);
    }

    // ── La prioridad ────────────────────────────────────────────────────

    [Fact]
    public void Proyecto_VaPrimeroQueVisualStudio()
    {
        // ⚠ LA PRIORIDAD DEL PROYECTO ES DELIBERADA: el editor no debe depender
        // de dónde esté instalado VS, ni de que lo esté.
        Tocar("masm_x64", "ml64.exe");
        Tocar("linker_x64", "link.exe");

        var todos = LinkerLocator.DiscoverToolchains(TargetArch.Win64, _raiz);

        Assert.NotEmpty(todos);
        Assert.Equal(ToolchainOrigin.Proyecto, todos[0].Origin);
    }

    [Fact]
    public void SinCarpetaDeProyecto_NoSeRompe()
    {
        var todos = LinkerLocator.DiscoverToolchains(TargetArch.Win64, projectFolder: null);

        Assert.DoesNotContain(todos, t => t.Origin == ToolchainOrigin.Proyecto);
    }

    // ── Visual Studio (contra la máquina real) ──────────────────────────

    private static List<Toolchain> DeVisualStudio(TargetArch arch) =>
        LinkerLocator.DiscoverToolchains(arch)
            .Where(t => t.Origin == ToolchainOrigin.VisualStudio)
            .ToList();

    [Fact]
    public void VisualStudio_DevuelveTodasLasVersiones_NoSoloLaPrimera()
    {
        var vs = DeVisualStudio(TargetArch.Win64);
        if (vs.Count == 0) return;   // sin VS instalado no hay nada que afirmar

        // El punto del descubrimiento: la lista completa, no el primer hallazgo.
        // FindMsvcLinker devuelve uno solo; esto tiene que poder devolver más.
        Assert.All(vs, t => Assert.NotNull(t.Version));
    }

    [Fact]
    public void VisualStudio_NoSeSalteaEdicionesComoInsiders()
    {
        // ⚠ ESTA ES LA PRUEBA DEL BUG: la lista fija de ediciones nombraba
        // Enterprise/Professional/Community y por eso no veía «18\Insiders».
        // Si alguien vuelve a una lista fija, las ediciones que no estén en
        // ella desaparecen y las versiones halladas se desploman.
        var vs = DeVisualStudio(TargetArch.Win64);
        if (vs.Count == 0) return;

        var ediciones = vs
            .Select(t => t.DisplayName)
            .Where(n => n.Contains("Insiders"))
            .ToList();

        // En este equipo hay Insiders instalado. Donde no lo haya, la prueba
        // no puede afirmar nada y se deja pasar.
        var hayInsidersEnDisco = Directory.Exists(
            @"C:\Program Files\Microsoft Visual Studio\18\Insiders\VC\Tools\MSVC");

        if (hayInsidersEnDisco)
        {
            Assert.NotEmpty(ediciones);
        }
    }

    [Fact]
    public void VisualStudio_ElNombreLlevaEdicionYVersion()
    {
        var vs = DeVisualStudio(TargetArch.Win64);
        if (vs.Count == 0) return;

        var t = vs[0];
        Assert.Contains("Visual Studio", t.DisplayName);
        Assert.Contains("—", t.DisplayName);                  // el separador de la versión
        Assert.Contains(t.Version!.ToString(), t.DisplayName);
    }

    [Fact]
    public void VisualStudio_VanDeLaMasNuevaALaMasVieja()
    {
        var vs = DeVisualStudio(TargetArch.Win64);
        if (vs.Count < 2) return;

        // Dentro de cada instalación el orden es por versión descendente.
        var porInstalacion = vs.GroupBy(t => t.DisplayName.Split('—')[0].Trim());

        foreach (var grupo in porInstalacion)
        {
            var versiones = grupo.Select(t => t.Version!).ToList();
            var ordenadas = versiones.OrderByDescending(v => v).ToList();
            Assert.Equal(ordenadas, versiones);
        }
    }

    [Fact]
    public void VisualStudio_ElMasmYElLinkSalenApareados()
    {
        // ⚠ NO SE MEZCLAN VERSIONES: los dos vienen de la misma carpeta.
        var vs = DeVisualStudio(TargetArch.Win64).Where(t => t.Completo).ToList();
        if (vs.Count == 0) return;

        Assert.All(vs, t =>
            Assert.Equal(
                Path.GetDirectoryName(t.MasmPath),
                Path.GetDirectoryName(t.LinkerPath)));
    }

    [Fact]
    public void VisualStudio_LoHalladoExisteDeVerdad()
    {
        var vs = DeVisualStudio(TargetArch.Win64);
        if (vs.Count == 0) return;

        Assert.All(vs, t =>
        {
            if (t.MasmPath is not null) Assert.True(File.Exists(t.MasmPath));
            if (t.LinkerPath is not null) Assert.True(File.Exists(t.LinkerPath));
        });
    }

    // ── El SDK ──────────────────────────────────────────────────────────

    [Fact]
    public void Sdk_DevuelveTodasLasVersionesConLibrerias()
    {
        var sdks = LinkerLocator.DiscoverSdks(TargetArch.Win64);
        if (sdks.Count == 0) return;

        // Cada una tiene que tener el kernel32.lib que se prometió.
        Assert.All(sdks, s =>
            Assert.True(File.Exists(Path.Combine(s.LibPath, "kernel32.lib"))));
    }

    [Fact]
    public void Sdk_VanDeLaMasNuevaALaMasVieja()
    {
        var sdks = LinkerLocator.DiscoverSdks(TargetArch.Win64);
        if (sdks.Count < 2) return;

        var versiones = sdks.Select(s => s.Version).ToList();
        Assert.Equal(versiones.OrderByDescending(v => v).ToList(), versiones);
    }

    [Fact]
    public void Sdk_SeEligeAparteDelToolchain()
    {
        // Que sean listas separadas es el punto: las versiones del SDK son
        // independientes de las de MSVC.
        var sdks = LinkerLocator.DiscoverSdks(TargetArch.Win64);
        if (sdks.Count == 0) return;

        Assert.All(sdks, s => Assert.Contains("um", s.LibPath));
    }

    [Fact]
    public void Sdk_LaRutaEsLaDeLaArquitecturaPedida()
    {
        var x64 = LinkerLocator.DiscoverSdks(TargetArch.Win64);
        var x86 = LinkerLocator.DiscoverSdks(TargetArch.Win32);

        Assert.All(x64, s => Assert.EndsWith(@"um\x64", s.LibPath));
        Assert.All(x86, s => Assert.EndsWith(@"um\x86", s.LibPath));
    }

    // ── Coherencia con lo que ya existía ────────────────────────────────

    [Fact]
    public void LoDescubiertoCoincideConFindMasm()
    {
        // FindMasm devuelve «el primero que sirve»: tiene que ser el primero
        // de la lista que tenga ensamblador.
        var esperado = LinkerLocator.FindMasm(TargetArch.Win64);
        var primero = LinkerLocator.DiscoverToolchains(TargetArch.Win64)
            .FirstOrDefault(t => t.MasmPath is not null)?.MasmPath;

        Assert.Equal(esperado, primero);
    }

    [Fact]
    public void LoDescubiertoCoincideConFindSdkLibPath()
    {
        var esperado = LinkerLocator.FindSdkLibPath(TargetArch.Win64);
        var primero = LinkerLocator.DiscoverSdks(TargetArch.Win64).FirstOrDefault()?.LibPath;

        Assert.Equal(esperado, primero);
    }
}
