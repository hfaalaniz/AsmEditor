using AsmEditor.Core;
using AsmEditor.Core.Proyecto;

namespace AsmEditor.Tests;

/// <summary>
/// El modelo de proyecto: lista explícita de archivos, rutas relativas,
/// archivo principal, carpeta de salida y herencia de targets.
/// </summary>
public class ProyectoTests : IDisposable
{
    private readonly string _dir;

    public ProyectoTests()
    {
        _dir = Path.Combine(Path.GetTempPath(), "asmproy_" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(_dir);
    }

    public void Dispose()
    {
        try { Directory.Delete(_dir, true); } catch (IOException) { }
        GC.SuppressFinalize(this);
    }

    // ---------------- Ayuda ----------------

    private string Ruta(string nombre) => Path.Combine(_dir, nombre);

    private string CrearArchivo(string nombre, string contenido = "; vacio")
    {
        var ruta = Ruta(nombre);
        var carpeta = Path.GetDirectoryName(ruta)!;
        Directory.CreateDirectory(carpeta);
        File.WriteAllText(ruta, contenido);
        return ruta;
    }

    private ProyectoAsm ProyectoConArchivos(params string[] nombres)
    {
        var p = ProyectoAsm.Nuevo("Prueba", Ruta("Prueba.asmproj"));

        foreach (var n in nombres) p.Agregar(CrearArchivo(n));

        return p;
    }

    // ---------------- Rutas relativas ----------------

    /// <summary>
    /// ⚠ Las rutas se guardan RELATIVAS: con absolutas, mover o copiar la
    /// carpeta del proyecto lo rompe, y eso se hace todo el tiempo.
    /// </summary>
    [Fact]
    public void Agregar_GuardaLaRutaRelativa()
    {
        var p = ProyectoAsm.Nuevo("P", Ruta("P.asmproj"));

        p.Agregar(CrearArchivo("ventana.asm"));

        Assert.Equal("ventana.asm", p.Archivos[0]);
    }

    [Fact]
    public void Agregar_ArchivoEnSubcarpetaGuardaLaRutaRelativaCompleta()
    {
        var p = ProyectoAsm.Nuevo("P", Ruta("P.asmproj"));

        p.Agregar(CrearArchivo(Path.Combine("inc", "macros.inc")));

        Assert.Equal(Path.Combine("inc", "macros.inc"), p.Archivos[0]);
    }

    /// <summary>
    /// Un archivo fuera de la carpeta del proyecto se guarda absoluto: una
    /// relativa con "..\..\.." no es más portable, y entre unidades distintas
    /// ni siquiera se puede expresar.
    /// </summary>
    [Fact]
    public void Agregar_ArchivoFueraDeLaCarpetaSeGuardaAbsoluto()
    {
        var p = ProyectoAsm.Nuevo("P", Ruta("P.asmproj"));

        var afuera = Path.Combine(Path.GetTempPath(), "suelto_" + Guid.NewGuid().ToString("N")[..6] + ".asm");
        File.WriteAllText(afuera, "; suelto");

        try
        {
            p.Agregar(afuera);

            Assert.True(Path.IsPathRooted(p.Archivos[0]),
                $"Debería haberse guardado absoluto, y quedó '{p.Archivos[0]}'");
        }
        finally { File.Delete(afuera); }
    }

    /// <summary>
    /// La prueba que justifica todo lo anterior: si se mueve la carpeta del
    /// proyecto, los archivos se siguen encontrando.
    /// </summary>
    [Fact]
    public void MoverLaCarpetaDelProyecto_NoRompeLasRutas()
    {
        var p = ProyectoConArchivos("ventana.asm", "ventana.inc");
        p.MarcarComoPrincipal(Ruta("ventana.asm"));
        ArchivoProyecto.Guardar(Ruta("Prueba.asmproj"), p);

        // ⚠ SE MUEVEN DE VERDAD, NO SE COPIAN. Copiando, los originales siguen
        // existiendo y un proyecto con rutas ABSOLUTAS al origen encuentra los
        // archivos igual: la prueba pasaba en verde con el modelo roto.
        // Moviéndolos, la ruta vieja deja de existir y solo funciona si las
        // rutas son relativas, que es lo que esta prueba tiene que proteger.
        var destino = Path.Combine(Path.GetTempPath(), "movido_" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(destino);

        try
        {
            foreach (var f in Directory.GetFiles(_dir))
            {
                File.Move(f, Path.Combine(destino, Path.GetFileName(f)));
            }

            var cargado = ArchivoProyecto.Cargar(Path.Combine(destino, "Prueba.asmproj"));

            Assert.Empty(cargado.Validar());
            Assert.All(cargado.ListarArchivos(), a => Assert.True(a.Existe));
            Assert.Equal(Path.Combine(destino, "ventana.asm"), cargado.RutaPrincipal);
        }
        finally { Directory.Delete(destino, true); }
    }

    // ---------------- La lista ----------------

    [Fact]
    public void Contiene_ReconoceLaMismaRutaEscritaDistinto()
    {
        var p = ProyectoConArchivos("ventana.asm");

        Assert.True(p.Contiene(Ruta("ventana.asm")));
        Assert.True(p.Contiene(Ruta("VENTANA.ASM")));
        Assert.True(p.Contiene(Ruta("ventana.asm").Replace('\\', '/')));
    }

    [Fact]
    public void Agregar_NoDuplica()
    {
        var p = ProyectoConArchivos("ventana.asm");

        Assert.False(p.Agregar(Ruta("ventana.asm")));
        Assert.Single(p.Archivos);
    }

    [Fact]
    public void Quitar_SacaDeLaListaYDevuelveTrue()
    {
        var p = ProyectoConArchivos("a.asm", "b.inc");

        Assert.True(p.Quitar(Ruta("b.inc")));
        Assert.Single(p.Archivos);
        Assert.False(p.Contiene(Ruta("b.inc")));
    }

    /// <summary>
    /// ⚠ Quitar del proyecto y borrar del disco son cosas distintas.
    /// Confundirlas destruye trabajo.
    /// </summary>
    [Fact]
    public void Quitar_NoBorraElArchivoDelDisco()
    {
        var p = ProyectoConArchivos("a.asm");
        var ruta = Ruta("a.asm");

        p.Quitar(ruta);

        Assert.True(File.Exists(ruta), "Quitar del proyecto NO debe borrar el archivo.");
    }

    [Fact]
    public void Quitar_ElPrincipalDejaElProyectoSinPrincipal()
    {
        var p = ProyectoConArchivos("a.asm");
        p.MarcarComoPrincipal(Ruta("a.asm"));

        p.Quitar(Ruta("a.asm"));

        Assert.Equal("", p.ArchivoPrincipal);
        Assert.Contains(p.Validar(), e => e.Contains("no tiene archivo principal"));
    }

    /// <summary>
    /// Es una LISTA, no una carpeta: un archivo que está en el disco pero no en
    /// la lista no pertenece al proyecto.
    /// </summary>
    [Fact]
    public void UnArchivoEnLaCarpetaQueNoEstaEnLaListaNoEsDelProyecto()
    {
        var p = ProyectoConArchivos("ventana.asm");

        CrearArchivo("descarte.asm");

        Assert.False(p.Contiene(Ruta("descarte.asm")));
        Assert.Single(p.Archivos);
    }

    // ---------------- Archivos faltantes ----------------

    /// <summary>
    /// ⚠ Un archivo que falta se INFORMA, no se saca de la lista. Puede ser que
    /// alguien lo movió o que falta traerlo de un respaldo: sacarlo solo
    /// convierte un problema visible en una pérdida silenciosa.
    /// </summary>
    [Fact]
    public void UnArchivoQueFaltaSeInformaPeroNoSeQuitaDeLaLista()
    {
        var p = ProyectoConArchivos("ventana.asm", "util.inc");

        File.Delete(Ruta("util.inc"));

        Assert.Equal(2, p.Archivos.Count);

        var lista = p.ListarArchivos();
        Assert.True(lista[0].Existe);
        Assert.False(lista[1].Existe);

        Assert.Contains(p.Validar(), e => e.Contains("Falta el archivo") && e.Contains("util.inc"));
    }

    [Fact]
    public void Validar_DetectaDuplicados()
    {
        var p = ProyectoConArchivos("a.asm");

        // Un duplicado solo se puede meter editando el archivo a mano.
        p.Archivos.Add("a.asm");

        Assert.Contains(p.Validar(), e => e.Contains("repetido"));
    }

    // ---------------- Archivo principal ----------------

    [Fact]
    public void MarcarComoPrincipal_SoloAceptaArchivosDeLaLista()
    {
        var p = ProyectoConArchivos("a.asm");

        Assert.False(p.MarcarComoPrincipal(Ruta("otro.asm")));
        Assert.True(p.MarcarComoPrincipal(Ruta("a.asm")));
        Assert.Equal("a.asm", p.ArchivoPrincipal);
    }

    [Fact]
    public void RutaPrincipal_EsAbsoluta()
    {
        var p = ProyectoConArchivos("ventana.asm");
        p.MarcarComoPrincipal(Ruta("ventana.asm"));

        Assert.Equal(Ruta("ventana.asm"), p.RutaPrincipal);
    }

    [Fact]
    public void ListarArchivos_MarcaCualEsElPrincipal()
    {
        var p = ProyectoConArchivos("a.asm", "b.asm");
        p.MarcarComoPrincipal(Ruta("b.asm"));

        var lista = p.ListarArchivos();

        Assert.False(lista[0].EsPrincipal);
        Assert.True(lista[1].EsPrincipal);
    }

    [Fact]
    public void Crear_TomaElPrimerAsmComoPrincipal()
    {
        var inc = CrearArchivo("macros.inc");
        var asm = CrearArchivo("ventana.asm");

        var p = ArchivoProyecto.Crear(Ruta("P.asmproj"), "P", new[] { inc, asm });

        Assert.Equal("ventana.asm", p.ArchivoPrincipal);
    }

    // ---------------- Carpeta de salida ----------------

    /// <summary>
    /// ⚠ Por defecto, la salida va AL LADO DEL FUENTE, como siempre. De eso
    /// dependen los scripts de afuera: build.ps1 busca el .exe al lado del .asm.
    /// </summary>
    [Fact]
    public void RutaDeSalida_SinCarpetaVaAlLadoDelFuente()
    {
        var p = ProyectoConArchivos("ventana.asm");
        p.CarpetaSalida = "";

        var obj = p.RutaDeSalida(Ruta("ventana.asm"), ".obj");

        Assert.Equal(Ruta("ventana.obj"), obj);
    }

    [Fact]
    public void RutaDeSalida_ConCarpetaVaAEsaCarpeta()
    {
        var p = ProyectoConArchivos("ventana.asm");
        p.CarpetaSalida = "bin";

        var obj = p.RutaDeSalida(Ruta("ventana.asm"), ".obj");
        var exe = p.RutaDeSalida(Ruta("ventana.asm"), ".exe");

        Assert.Equal(Path.Combine(_dir, "bin", "ventana.obj"), obj);
        Assert.Equal(Path.Combine(_dir, "bin", "ventana.exe"), exe);
    }

    [Fact]
    public void AsegurarCarpetaDeSalida_LaCreaSiFalta()
    {
        var p = ProyectoConArchivos("ventana.asm");
        p.CarpetaSalida = "bin";

        Assert.False(Directory.Exists(Path.Combine(_dir, "bin")));

        p.AsegurarCarpetaDeSalida();

        Assert.True(Directory.Exists(Path.Combine(_dir, "bin")));
    }

    [Fact]
    public void AsegurarCarpetaDeSalida_SinCarpetaNoCreaNada()
    {
        var p = ProyectoConArchivos("ventana.asm");
        p.CarpetaSalida = "";

        p.AsegurarCarpetaDeSalida();

        Assert.Empty(Directory.GetDirectories(_dir));
    }

    [Fact]
    public void ProyectoNuevo_NaceConSalidaEnBin()
    {
        var p = ProyectoAsm.Nuevo("P", Ruta("P.asmproj"));

        Assert.Equal("bin", p.CarpetaSalida);
    }

    // ---------------- Targets ----------------

    /// <summary>
    /// ⚠ Sin targets propios se HEREDAN los globales, no se copian: copiarlos al
    /// crear el proyecto dejaría los proyectos viejos sin enterarse de un
    /// cambio posterior en la configuración global.
    /// </summary>
    [Fact]
    public void SinTargetsPropios_HeredaLosGlobales()
    {
        var p = ProyectoAsm.Nuevo("P", Ruta("P.asmproj"));
        var globales = BuildTarget.CreateDefaults();

        Assert.True(p.HeredaTargets);
        Assert.Same(globales, p.TargetsEfectivos(globales));
        Assert.Equal(globales[0].Name, p.TargetEfectivo(globales).Name);
    }

    [Fact]
    public void ConTargetsPropios_UsaLosSuyos()
    {
        var p = ProyectoAsm.Nuevo("P", Ruta("P.asmproj"));
        p.Targets.Add(new BuildTarget { Name = "El mio", Arch = TargetArch.Win32 });

        var globales = BuildTarget.CreateDefaults();

        Assert.False(p.HeredaTargets);
        Assert.Equal("El mio", p.TargetEfectivo(globales).Name);
    }

    [Fact]
    public void TargetEfectivo_IndiceFueraDeRangoCaeEnElPrimero()
    {
        var p = ProyectoAsm.Nuevo("P", Ruta("P.asmproj"));
        p.Targets.Add(new BuildTarget { Name = "Uno" });
        p.TargetActivo = 99;

        Assert.Equal("Uno", p.TargetEfectivo(new List<BuildTarget>()).Name);
    }

    /// <summary>Compilar sin ningún target no es una opción: se vuelve a los de fábrica.</summary>
    [Fact]
    public void TargetEfectivo_SinNingunTargetEnNingunLadoDevuelveLosDeFabrica()
    {
        var p = ProyectoAsm.Nuevo("P", Ruta("P.asmproj"));

        var t = p.TargetEfectivo(new List<BuildTarget>());

        Assert.NotNull(t);
        Assert.NotEmpty(p.Targets);
    }

    // ---------------- Guardado ----------------

    [Fact]
    public void Serializar_IdaYVueltaConservaTodo()
    {
        var p = ProyectoConArchivos("ventana.asm", "ventana.inc");
        p.Nombre = "Mi proyecto con acentos: ñ á";
        p.CarpetaSalida = "salida";
        p.MarcarComoPrincipal(Ruta("ventana.asm"));
        p.Targets.Add(new BuildTarget { Name = "Propio", Arch = TargetArch.Win32 });
        p.TargetActivo = 0;

        var ruta = Ruta("P.asmproj");
        ArchivoProyecto.Guardar(ruta, p);

        var r = ArchivoProyecto.Cargar(ruta);

        Assert.Equal(p.Nombre, r.Nombre);
        Assert.Equal(p.ArchivoPrincipal, r.ArchivoPrincipal);
        Assert.Equal(p.CarpetaSalida, r.CarpetaSalida);
        Assert.Equal(p.Archivos, r.Archivos);
        Assert.Equal("Propio", r.Targets[0].Name);
        Assert.Equal(TargetArch.Win32, r.Targets[0].Arch);
    }

    [Fact]
    public void Cargar_DejaLaRutaParaResolverLasRelativas()
    {
        var p = ProyectoConArchivos("ventana.asm");
        var ruta = Ruta("P.asmproj");
        ArchivoProyecto.Guardar(ruta, p);

        var r = ArchivoProyecto.Cargar(ruta);

        Assert.Equal(ruta, r.RutaArchivo);
        Assert.Equal(_dir.TrimEnd(Path.DirectorySeparatorChar),
                     r.Carpeta?.TrimEnd(Path.DirectorySeparatorChar));
        Assert.Equal(Ruta("ventana.asm"), r.RutaAbsoluta("ventana.asm"));
    }

    [Fact]
    public void Deserializar_ArchivoDanadoDaUnErrorEntendible()
    {
        var ex = Assert.Throws<InvalidDataException>(
            () => ArchivoProyecto.Deserializar("{ esto no es json"));

        Assert.Contains("dañado", ex.Message);
    }

    [Fact]
    public void Deserializar_VersionMasNuevaAvisaQueHayQueActualizar()
    {
        var json = """{ "Version": 99, "Proyecto": { "Nombre": "P" } }""";

        var ex = Assert.Throws<InvalidDataException>(() => ArchivoProyecto.Deserializar(json));

        Assert.Contains("más nueva", ex.Message);
    }

    /// <summary>
    /// Un .asmproj editado a mano con un principal que existe pero no está
    /// listado: se agrega solo, porque es lo que el usuario quiso decir.
    /// </summary>
    [Fact]
    public void Normalizar_ElPrincipalQueNoEstaListadoSeAgrega()
    {
        CrearArchivo("ventana.asm");

        var json = """
            {
              "Version": 1,
              "Proyecto": {
                "Nombre": "P",
                "ArchivoPrincipal": "ventana.asm",
                "Archivos": []
              }
            }
            """;

        var p = ArchivoProyecto.Deserializar(json, Ruta("P.asmproj"));

        Assert.Contains("ventana.asm", p.Archivos);
        Assert.Empty(p.Validar());
    }

    // ---------------- Buscar el proyecto de un archivo ----------------

    [Fact]
    public void BuscarProyectoDe_LoEncuentraEnLaMismaCarpeta()
    {
        var p = ProyectoConArchivos("ventana.asm");
        ArchivoProyecto.Guardar(Ruta("P.asmproj"), p);

        var hallado = ArchivoProyecto.BuscarProyectoDe(Ruta("ventana.asm"));

        Assert.Equal(Ruta("P.asmproj"), hallado);
    }

    [Fact]
    public void BuscarProyectoDe_LoEncuentraSubiendoDeCarpeta()
    {
        var p = ProyectoConArchivos("ventana.asm");
        ArchivoProyecto.Guardar(Ruta("P.asmproj"), p);

        var enSub = CrearArchivo(Path.Combine("inc", "macros.inc"));

        Assert.Equal(Ruta("P.asmproj"), ArchivoProyecto.BuscarProyectoDe(enSub));
    }

    /// <summary>Con dos proyectos en la misma carpeta no se adivina cuál.</summary>
    [Fact]
    public void BuscarProyectoDe_ConVariosProyectosNoElige()
    {
        var p = ProyectoConArchivos("ventana.asm");
        ArchivoProyecto.Guardar(Ruta("Uno.asmproj"), p);
        ArchivoProyecto.Guardar(Ruta("Dos.asmproj"), p);

        Assert.Null(ArchivoProyecto.BuscarProyectoDe(Ruta("ventana.asm")));
    }

    [Fact]
    public void BuscarProyectoDe_SinProyectoDevuelveNull()
    {
        var suelto = CrearArchivo("suelto.asm");

        Assert.Null(ArchivoProyecto.BuscarProyectoDe(suelto, nivelesArriba: 0));
    }

    // ---------------- Clonar ----------------

    [Fact]
    public void Clonar_NoCompartelistasConElOriginal()
    {
        var p = ProyectoConArchivos("a.asm");
        p.Targets.Add(new BuildTarget { Name = "T" });

        var c = p.Clonar();
        c.Archivos.Add("b.asm");
        c.Targets[0].Name = "Cambiado";

        Assert.Single(p.Archivos);
        Assert.Equal("T", p.Targets[0].Name);
    }
}

/// <summary>
/// El estado de interfaz, en lo que toca a proyectos: recientes y el proyecto
/// que estaba abierto.
/// </summary>
public class UiStateProyectoTests
{
    [Fact]
    public void AddRecentProject_PoneElUltimoPrimeroYNoDuplica()
    {
        var s = new UiState();

        s.AddRecentProject(@"C:\a\uno.asmproj");
        s.AddRecentProject(@"C:\a\dos.asmproj");
        s.AddRecentProject(@"C:\a\uno.asmproj");

        Assert.Equal(2, s.RecentProjects.Count);
        Assert.Equal(@"C:\a\uno.asmproj", s.RecentProjects[0]);
    }

    [Fact]
    public void AddRecentProject_RespetaElTope()
    {
        var s = new UiState();

        for (int i = 0; i < UiState.MaxRecientes + 5; i++)
        {
            s.AddRecentProject($@"C:\a\p{i}.asmproj");
        }

        Assert.Equal(UiState.MaxRecientes, s.RecentProjects.Count);
    }

    /// <summary>Los proyectos y los archivos van en listas separadas.</summary>
    [Fact]
    public void LosProyectosRecientesNoSeMezclanConLosArchivos()
    {
        var s = new UiState();

        s.AddRecent(@"C:\a\x.asm");
        s.AddRecentProject(@"C:\a\p.asmproj");

        Assert.Single(s.RecentFiles);
        Assert.Single(s.RecentProjects);
        Assert.DoesNotContain(@"C:\a\p.asmproj", s.RecentFiles);
    }

    [Fact]
    public void PurgeMissing_LimpiaProyectosQueYaNoEstan()
    {
        var s = new UiState
        {
            ProyectoAbierto = @"C:\a\borrado.asmproj"
        };

        s.AddRecentProject(@"C:\a\existe.asmproj");
        s.AddRecentProject(@"C:\a\borrado.asmproj");

        s.PurgeMissingRecents(p => p.Contains("existe"));

        Assert.Single(s.RecentProjects);
        Assert.Null(s.ProyectoAbierto);
    }

    [Fact]
    public void EnsureValid_SinProyectoEsUnEstadoValido()
    {
        var s = new UiState { ProyectoAbierto = "   " };

        s.EnsureValid();

        Assert.Null(s.ProyectoAbierto);
        Assert.NotNull(s.RecentProjects);
    }
}
