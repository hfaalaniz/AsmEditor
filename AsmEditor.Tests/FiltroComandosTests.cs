using AsmEditor.Core;

namespace AsmEditor.Tests;

/// <summary>
/// El buscador de comandos de la barra de título: qué encuentra, qué deja
/// afuera y en qué orden.
/// </summary>
public class FiltroComandosTests
{
    private static ComandoMenu C(string ruta, bool habilitado = true, string atajo = "") =>
        new(ruta, ruta.Split(" › ").Last(), atajo, habilitado);

    private static readonly List<ComandoMenu> Menu = new()
    {
        C("Archivo › Nuevo"),
        C("Archivo › Abrir..."),
        C("Archivo › Guardar"),
        C("Archivo › Guardar como..."),
        C("Archivo › Guardar todo"),
        C("Herramientas › Diseñador de formularios..."),
        C("Compilar › Compilar y enlazar"),
        C("Compilar › Detener compilación", habilitado: false),
        C("Ver › Tema › Oscuro"),
    };

    private static List<string> Rutas(IEnumerable<ComandoMenu> r) => r.Select(c => c.Ruta).ToList();

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void SinConsulta_NoDevuelveNada(string? consulta)
    {
        Assert.Empty(FiltroComandos.Filtrar(Menu, consulta));
    }

    [Fact]
    public void EncuentraPorParteDelNombre()
    {
        var r = Rutas(FiltroComandos.Filtrar(Menu, "guar"));

        Assert.Equal(new[] { "Archivo › Guardar", "Archivo › Guardar como...", "Archivo › Guardar todo" }, r);
    }

    [Fact]
    public void NoDistingueMayusculasNiAcentos()
    {
        var r = Rutas(FiltroComandos.Filtrar(Menu, "DISENADOR"));

        Assert.Equal(new[] { "Herramientas › Diseñador de formularios..." }, r);
    }

    [Fact]
    public void CadaPalabraTieneQueAparecerEnLaRuta()
    {
        // "arch" está en el menú, "todo" en el nombre: solo uno cumple las dos.
        var r = Rutas(FiltroComandos.Filtrar(Menu, "arch todo"));

        Assert.Equal(new[] { "Archivo › Guardar todo" }, r);
    }

    [Fact]
    public void BuscaTambienEnElNombreDelMenu()
    {
        var r = Rutas(FiltroComandos.Filtrar(Menu, "tema"));

        Assert.Equal(new[] { "Ver › Tema › Oscuro" }, r);
    }

    [Fact]
    public void LosDeshabilitadosNoAparecen()
    {
        var r = Rutas(FiltroComandos.Filtrar(Menu, "detener"));

        Assert.Empty(r);
    }

    [Fact]
    public void PrimeroLosQueEmpiezanConLoTipeado()
    {
        // "compilar" está en la ruta de los dos del menú Compilar, pero solo
        // "Compilar y enlazar" EMPIEZA así; "Detener compilación" está
        // deshabilitado. Se agrega uno que lo contiene en el medio.
        var menu = new List<ComandoMenu>
        {
            C("Ayuda › Cómo compilar"),
            C("Compilar › Compilar y enlazar"),
        };

        var r = Rutas(FiltroComandos.Filtrar(menu, "compilar"));

        Assert.Equal(new[] { "Compilar › Compilar y enlazar", "Ayuda › Cómo compilar" }, r);
    }

    [Fact]
    public void RespetaElMaximo()
    {
        var muchos = Enumerable.Range(1, 30).Select(i => C($"Menú › Comando {i}")).ToList();

        Assert.Equal(5, FiltroComandos.Filtrar(muchos, "comando", maximo: 5).Count);
        Assert.Equal(FiltroComandos.MaximoResultados, FiltroComandos.Filtrar(muchos, "comando").Count);
    }

    [Theory]
    [InlineData("&Guardar", "Guardar")]
    [InlineData("Guardar &todo", "Guardar todo")]
    [InlineData("Copiar && pegar", "Copiar & pegar")]
    [InlineData("Sin marca", "Sin marca")]
    public void QuitarMnemonico_SacaLaMarcaDeTeclaDeAcceso(string entrada, string esperado)
    {
        Assert.Equal(esperado, FiltroComandos.QuitarMnemonico(entrada));
    }

    [Fact]
    public void LaMarcaDeTeclaDeAccesoNoMolestaAlBuscar()
    {
        var menu = new List<ComandoMenu> { new("&Archivo › &Guardar", "&Guardar", "Ctrl+S", true) };

        Assert.Single(FiltroComandos.Filtrar(menu, "guardar"));
    }
}
