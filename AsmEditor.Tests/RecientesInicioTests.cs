using AsmEditor.Core;

namespace AsmEditor.Tests;

/// <summary>
/// La ventana de inicio: armado de los recientes, agrupado por fecha, filtro,
/// y lo que se agregó a UiState (fechas, sesiones por proyecto, "Al iniciar").
/// </summary>
public class RecientesInicioTests
{
    // Miércoles 23/09/2026, 15:00. La semana empieza el lunes 21.
    private static readonly DateTime Ahora = new(2026, 9, 23, 15, 0, 0);

    private static ProyectoReciente P(string nombre, DateTime cuando) =>
        new($@"C:\proy\{nombre}\{nombre}.asmproj", nombre, $@"C:\proy\{nombre}", cuando);

    // ---------------- Agrupar ----------------

    [Fact]
    public void Agrupar_UsaLosGruposDeVisualStudio_EnOrden()
    {
        var lista = new[]
        {
            P("viejo", new DateTime(2026, 8, 30)),        // mes anterior
            P("mes", new DateTime(2026, 9, 5)),           // este mes, semana anterior
            P("lunes", new DateTime(2026, 9, 21, 10, 0, 0)),
            P("ayer", new DateTime(2026, 9, 22, 23, 59, 0)),
            P("hoy", new DateTime(2026, 9, 23, 0, 1, 0)),
        };

        var grupos = RecientesInicio.Agrupar(lista, Ahora);

        Assert.Equal(new[] { "Hoy", "Ayer", "Esta semana", "Este mes", "Anterior" }, grupos.Select(g => g.Titulo));
        Assert.Equal("hoy", grupos[0].Proyectos.Single().Nombre);
        Assert.Equal("ayer", grupos[1].Proyectos.Single().Nombre);
        Assert.Equal("lunes", grupos[2].Proyectos.Single().Nombre);
        Assert.Equal("mes", grupos[3].Proyectos.Single().Nombre);
        Assert.Equal("viejo", grupos[4].Proyectos.Single().Nombre);
    }

    [Fact]
    public void Agrupar_LaSemanaEmpiezaElLunes()
    {
        // El domingo 20 ya es la semana anterior (mismo mes: "Este mes").
        var grupos = RecientesInicio.Agrupar(new[] { P("domingo", new DateTime(2026, 9, 20, 12, 0, 0)) }, Ahora);

        Assert.Equal("Este mes", grupos.Single().Titulo);
    }

    [Fact]
    public void Agrupar_NoMuestraGruposVacios_YOrdenaPorFechaDentroDelGrupo()
    {
        var lista = new[]
        {
            P("temprano", new DateTime(2026, 9, 23, 8, 0, 0)),
            P("tarde", new DateTime(2026, 9, 23, 14, 0, 0)),
        };

        var grupos = RecientesInicio.Agrupar(lista, Ahora);

        Assert.Single(grupos);
        Assert.Equal(new[] { "tarde", "temprano" }, grupos[0].Proyectos.Select(p => p.Nombre));
    }

    // ---------------- Armar ----------------

    [Fact]
    public void Armar_UsaLaFechaGuardada_YSinFechaLaDelArchivo()
    {
        var rutas = new[] { @"C:\a\uno.asmproj", @"C:\b\dos.asmproj" };
        var fechas = new Dictionary<string, DateTime> { [@"c:/A/UNO.asmproj"] = new DateTime(2026, 9, 1) };

        var lista = RecientesInicio.Armar(rutas, fechas, _ => new DateTime(2026, 9, 10));

        var uno = lista.Single(p => p.Nombre == "uno");
        var dos = lista.Single(p => p.Nombre == "dos");

        Assert.Equal(new DateTime(2026, 9, 1), uno.UltimaApertura);   // guardada (ruta escrita distinto)
        Assert.Equal(new DateTime(2026, 9, 10), dos.UltimaApertura);  // del archivo
        Assert.Equal(@"C:\b", dos.Carpeta);
        Assert.Equal("dos", lista[0].Nombre);                          // más nuevo primero
    }

    // ---------------- Filtrar ----------------

    [Theory]
    [InlineData("", 3)]
    [InlineData("   ", 3)]
    [InlineData("TELE", 1)]
    [InlineData("disenador", 1)]      // sin acento encuentra "Diseñador"
    [InlineData("proy", 3)]            // la carpeta también cuenta
    [InlineData("tele remo", 1)]       // las dos palabras están en "TeleRemo"
    [InlineData("tele otro", 0)]       // cada una está en OTRO proyecto: no alcanza
    public void Filtrar_PorNombreOCarpeta_SinMayusculasNiAcentos(string consulta, int esperados)
    {
        var lista = new[]
        {
            P("TeleRemo", Ahora),
            P("Diseñador", Ahora),
            P("Otro", Ahora),
        };

        Assert.Equal(esperados, RecientesInicio.Filtrar(lista, consulta).Count);
    }

    // ---------------- UiState ----------------

    [Fact]
    public void UnSettingsViejo_CargaConLosValoresNuevosPorDefecto()
    {
        // El formato de antes de la ventana de inicio: sin AlIniciar, sin fechas ni sesiones.
        var json = """
        { "Ui": { "RecentProjects": [ "C:\\p\\a.asmproj" ], "ProyectoAbierto": "C:\\p\\a.asmproj" } }
        """;

        var cfg = BuildConfig.FromJson(json);

        Assert.Equal(AlIniciar.VentanaDeInicio, cfg.Ui.AlIniciar);
        Assert.Empty(cfg.Ui.FechasProyectos);
        Assert.Empty(cfg.Ui.SesionesProyectos);
        Assert.Single(cfg.Ui.RecentProjects);
    }

    [Fact]
    public void LoNuevoSobreviveAGuardarYLeer()
    {
        var cfg = new BuildConfig();
        cfg.Ui.AlIniciar = AlIniciar.UltimoProyecto;
        cfg.Ui.AddRecentProject(@"C:\p\a.asmproj", new DateTime(2026, 9, 20, 10, 30, 0));
        cfg.Ui.GuardarSesionDeProyecto(@"C:\p\a.asmproj", new[] { @"C:\p\x.asm", @"C:\p\y.inc" }, 1);

        var leida = BuildConfig.FromJson(cfg.ToJson());

        Assert.Equal(AlIniciar.UltimoProyecto, leida.Ui.AlIniciar);
        Assert.Equal(new DateTime(2026, 9, 20, 10, 30, 0), leida.Ui.FechasProyectos[@"C:\p\a.asmproj"]);
        var s = leida.Ui.SesionDe(@"c:/P/A.ASMPROJ");
        Assert.NotNull(s);
        Assert.Equal(new[] { @"C:\p\x.asm", @"C:\p\y.inc" }, s!.Archivos);
        Assert.Equal(1, s.Activo);
    }

    [Fact]
    public void AgregarUnReciente_ActualizaSuFecha_SinDuplicarla()
    {
        var ui = new UiState();
        ui.AddRecentProject(@"C:\p\a.asmproj", new DateTime(2026, 9, 1));
        ui.AddRecentProject(@"c:/p/A.asmproj", new DateTime(2026, 9, 20));

        Assert.Single(ui.RecentProjects);
        Assert.Single(ui.FechasProyectos);
        Assert.Equal(new DateTime(2026, 9, 20), ui.FechasProyectos.Values.Single());
    }

    [Fact]
    public void QuitarDeLaLista_SacaTambienFechaYSesion()
    {
        var ui = new UiState();
        ui.AddRecentProject(@"C:\p\a.asmproj", Ahora);
        ui.GuardarSesionDeProyecto(@"C:\p\a.asmproj", new[] { @"C:\p\x.asm" }, 0);

        ui.RemoveRecentProject(@"C:\P\A.asmproj");

        Assert.Empty(ui.RecentProjects);
        Assert.Empty(ui.FechasProyectos);
        Assert.Null(ui.SesionDe(@"C:\p\a.asmproj"));
    }

    [Fact]
    public void EnsureValid_PodaFechasYSesionesDeProyectosQueYaNoEstan()
    {
        var ui = new UiState();
        ui.AddRecentProject(@"C:\p\vigente.asmproj", Ahora);
        ui.FechasProyectos[@"C:\p\olvidado.asmproj"] = Ahora;
        ui.SesionesProyectos[@"C:\p\olvidado.asmproj"] = new SesionDeProyecto { Archivos = new() { "x" } };

        ui.EnsureValid();

        Assert.Single(ui.FechasProyectos);
        Assert.Empty(ui.SesionesProyectos);
    }

    [Fact]
    public void LaSesion_AcotaElActivoYDescartaVacios()
    {
        var ui = new UiState();
        ui.AddRecentProject(@"C:\p\a.asmproj", Ahora);
        ui.GuardarSesionDeProyecto(@"C:\p\a.asmproj", new[] { @"C:\p\x.asm", "", "  " }, 7);

        var s = ui.SesionDe(@"C:\p\a.asmproj")!;

        Assert.Single(s.Archivos);
        Assert.Equal(0, s.Activo);
    }
}
