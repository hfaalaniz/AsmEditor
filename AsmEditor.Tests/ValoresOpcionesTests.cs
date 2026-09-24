using System.Reflection;
using AsmEditor.Core;

namespace AsmEditor.Tests;

/// <summary>
/// La copia de las opciones que edita Configuración → Opciones: que cada
/// valor vaya y vuelva, y que editarla no toque la configuración hasta
/// aceptar (Cancelar no cambia nada).
/// </summary>
public class ValoresOpcionesTests
{
    private static IEnumerable<PropertyInfo> Propiedades() =>
        typeof(ValoresOpciones).GetProperties(BindingFlags.Public | BindingFlags.Instance);

    /// <summary>Un valor distinto del de fábrica para cada tipo de propiedad.</summary>
    private static object ValorDistinto(PropertyInfo p)
    {
        if (p.PropertyType == typeof(string)) return "valor-" + p.Name;

        if (p.PropertyType.IsEnum)
        {
            var valores = Enum.GetValues(p.PropertyType);
            return valores.GetValue(valores.Length - 1)!;
        }

        throw new InvalidOperationException(
            $"ValoresOpciones.{p.Name} es de tipo {p.PropertyType.Name}: agregá ese tipo a ValorDistinto.");
    }

    /// <summary>
    /// Por reflexión, así cubre también las opciones que se agreguen: si a una
    /// le falta su línea en Leer o en Aplicar, su valor no vuelve.
    /// </summary>
    [Fact]
    public void TodaPropiedad_VaYVuelve()
    {
        var deFabrica = ValoresOpciones.Leer(new BuildConfig());
        var puestos = new ValoresOpciones();

        foreach (var p in Propiedades())
        {
            var v = ValorDistinto(p);
            Assert.NotEqual(p.GetValue(deFabrica), v);   // si no, la prueba no probaría nada
            p.SetValue(puestos, v);
        }

        var cfg = new BuildConfig();
        puestos.Aplicar(cfg);
        var leidos = ValoresOpciones.Leer(cfg);

        foreach (var p in Propiedades())
        {
            Assert.True(Equals(p.GetValue(puestos), p.GetValue(leidos)),
                $"ValoresOpciones.{p.Name} no va y vuelve: falta en Leer o en Aplicar.");
        }
    }

    [Fact]
    public void TodaPropiedad_SobreviveAlSettingsJson()
    {
        var puestos = new ValoresOpciones();
        foreach (var p in Propiedades()) p.SetValue(puestos, ValorDistinto(p));

        var cfg = new BuildConfig();
        puestos.Aplicar(cfg);
        var leidos = ValoresOpciones.Leer(BuildConfig.FromJson(cfg.ToJson()));

        foreach (var p in Propiedades())
        {
            Assert.True(Equals(p.GetValue(puestos), p.GetValue(leidos)),
                $"ValoresOpciones.{p.Name} se pierde al guardar y leer settings.json.");
        }
    }

    /// <summary>
    /// Cancelar: la ventana edita la copia y nunca llama a Aplicar. Incluye el
    /// tema y «Al iniciar», que viven en Ui, la parte que Clone comparte.
    /// </summary>
    [Fact]
    public void EditarLaCopia_NoTocaLaConfiguracion()
    {
        var cfg = new BuildConfig();
        var antes = cfg.ToJson();

        var copia = ValoresOpciones.Leer(cfg);
        foreach (var p in Propiedades()) p.SetValue(copia, ValorDistinto(p));

        Assert.Equal(antes, cfg.ToJson());
    }

    /// <summary>Aplicar escribe sobre la Ui existente: los recientes y la sesión no se pierden.</summary>
    [Fact]
    public void Aplicar_ConservaLoQueNoEsOpcion()
    {
        // Como llega en el editor: leída del disco, o sea validada (con targets).
        var cfg = BuildConfig.FromJson(new BuildConfig().ToJson());
        var ui = cfg.Ui;
        ui.AddRecentProject(@"C:\p\Uno.asmproj");
        ui.AddRecent(@"C:\p\uno.asm");
        var targets = cfg.Targets.Count;

        var copia = ValoresOpciones.Leer(cfg);
        copia.Tema = ModoTemaGuardado.Claro;
        copia.Aplicar(cfg);

        Assert.Same(ui, cfg.Ui);
        Assert.Equal(new[] { @"C:\p\Uno.asmproj" }, cfg.Ui.RecentProjects);
        Assert.Equal(new[] { @"C:\p\uno.asm" }, cfg.Ui.RecentFiles);
        Assert.Equal(targets, cfg.Targets.Count);
        Assert.Equal(ModoTemaGuardado.Claro, cfg.Ui.Theme);
    }

    [Fact]
    public void Aplicar_AlIniciarFueraDeRango_VuelveALaVentanaDeInicio()
    {
        var cfg = new BuildConfig();
        var copia = ValoresOpciones.Leer(cfg);
        copia.AlIniciar = (AlIniciar)99;

        copia.Aplicar(cfg);

        Assert.Equal(AlIniciar.VentanaDeInicio, cfg.Ui.AlIniciar);
    }
}
