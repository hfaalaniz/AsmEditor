using System.Text.Json;
using AsmEditor.Core.Acople;

namespace AsmEditor.Tests;

/// <summary>
/// El modelo del acople (Etapa 3 de PLAN_IDE.md): zonas, pestañas por zona,
/// activo, ocultar y volver, tamaños, y leerlo del disco.
/// </summary>
public class DisenoAcopleTests
{
    /// <summary>El diseño de fábrica del editor: explorador a la derecha; errores y salida abajo.</summary>
    private static DisenoAcople DeFabrica()
    {
        var d = new DisenoAcople();
        d.Registrar("explorador", ZonaAcople.Derecha);
        d.Registrar("errores", ZonaAcople.Abajo);
        d.Registrar("salida", ZonaAcople.Abajo);
        return d;
    }

    // ---------------- Registrar ----------------

    [Fact]
    public void Registrar_OrdenaLasPestanasPorLlegada()
    {
        var d = DeFabrica();

        Assert.Equal(new[] { "errores", "salida" }, d.VisiblesEn(ZonaAcople.Abajo));
        Assert.Equal(new[] { "explorador" }, d.VisiblesEn(ZonaAcople.Derecha));
        Assert.Empty(d.VisiblesEn(ZonaAcople.Izquierda));
        Assert.False(d.ZonaVisible(ZonaAcople.Izquierda));
    }

    /// <summary>Un diseño guardado manda: registrar de nuevo no lo pisa.</summary>
    [Fact]
    public void Registrar_NoPisaLoGuardado()
    {
        var d = DeFabrica();
        d.Mover("explorador", ZonaAcople.Izquierda);
        d.Ocultar("salida");

        d.Registrar("explorador", ZonaAcople.Derecha);
        d.Registrar("salida", ZonaAcople.Abajo);

        Assert.Equal(ZonaAcople.Izquierda, d.ZonaDe("explorador"));
        Assert.False(d.EstaVisible("salida"));
    }

    // ---------------- Activo ----------------

    [Fact]
    public void ActivoEn_SinAnotar_EsElPrimero()
    {
        Assert.Equal("errores", DeFabrica().ActivoEn(ZonaAcople.Abajo));
    }

    [Fact]
    public void ActivoEn_ZonaVacia_EsNull()
    {
        Assert.Null(DeFabrica().ActivoEn(ZonaAcople.Izquierda));
    }

    /// <summary>
    /// Un diseño leído del disco puede anotar como activo un panel oculto: la
    /// zona muestra entonces el primero visible, no un hueco.
    /// </summary>
    [Fact]
    public void ActivoEn_ElAnotadoEstaOculto_EsElPrimeroVisible()
    {
        var d = DeFabrica();
        d.Paneles["salida"].Visible = false;
        d.Activos[ZonaAcople.Abajo] = "salida";

        Assert.Equal("errores", d.ActivoEn(ZonaAcople.Abajo));
    }

    [Fact]
    public void Activar_CambiaLaPestanaActiva()
    {
        var d = DeFabrica();
        d.Activar("salida");
        Assert.Equal("salida", d.ActivoEn(ZonaAcople.Abajo));
    }

    // ---------------- Ocultar y mostrar ----------------

    [Fact]
    public void Ocultar_ElActivo_PasaALaVecinaSiguiente()
    {
        var d = DeFabrica();
        d.Registrar("terminal", ZonaAcople.Abajo);
        d.Activar("salida");

        d.Ocultar("salida");

        Assert.Equal("terminal", d.ActivoEn(ZonaAcople.Abajo));
        Assert.Equal(new[] { "errores", "terminal" }, d.VisiblesEn(ZonaAcople.Abajo));
    }

    [Fact]
    public void Ocultar_ElUltimoActivo_PasaALaAnterior()
    {
        var d = DeFabrica();
        d.Registrar("terminal", ZonaAcople.Abajo);
        d.Activar("terminal");

        d.Ocultar("terminal");

        Assert.Equal("salida", d.ActivoEn(ZonaAcople.Abajo));
    }

    [Fact]
    public void Ocultar_ElUnicoDeLaZona_LaDejaSinOcupar()
    {
        var d = DeFabrica();
        d.Ocultar("explorador");

        Assert.False(d.ZonaVisible(ZonaAcople.Derecha));
        Assert.Null(d.ActivoEn(ZonaAcople.Derecha));
    }

    /// <summary>Mostrar lo devuelve a SU lugar (zona y posición de pestaña), activo.</summary>
    [Fact]
    public void Mostrar_VuelveASuLugarYQuedaActivo()
    {
        var d = DeFabrica();
        d.Ocultar("errores");
        Assert.Equal("salida", d.ActivoEn(ZonaAcople.Abajo));

        d.Mostrar("errores");

        Assert.Equal(new[] { "errores", "salida" }, d.VisiblesEn(ZonaAcople.Abajo));
        Assert.Equal("errores", d.ActivoEn(ZonaAcople.Abajo));
    }

    [Fact]
    public void MostrarYOcultar_UnIdDesconocido_NoHacenNada()
    {
        var d = DeFabrica();
        d.Mostrar("nada");
        d.Ocultar("nada");
        d.Activar("nada");
        Assert.Equal(3, d.Paneles.Count);
    }

    // ---------------- Mover ----------------

    [Fact]
    public void Mover_LoPoneUltimoEnLaOtraZona_VisibleYActivo()
    {
        var d = DeFabrica();
        d.Ocultar("salida");

        d.Mover("salida", ZonaAcople.Derecha);

        Assert.Equal(new[] { "explorador", "salida" }, d.VisiblesEn(ZonaAcople.Derecha));
        Assert.Equal("salida", d.ActivoEn(ZonaAcople.Derecha));
        Assert.Equal(new[] { "errores" }, d.VisiblesEn(ZonaAcople.Abajo));
    }

    /// <summary>
    /// El que era el PRIMERO de su zona también queda último en la nueva: su
    /// orden viejo (0) no lo pone delante de los que ya estaban.
    /// </summary>
    [Fact]
    public void Mover_ElPrimeroDeOtraZona_QuedaUltimo()
    {
        var d = DeFabrica();

        d.Mover("errores", ZonaAcople.Derecha);

        Assert.Equal(new[] { "explorador", "errores" }, d.VisiblesEn(ZonaAcople.Derecha));
    }

    // ---------------- Tamaños ----------------

    [Fact]
    public void TamanoDe_SinGuardar_EsElDeFabrica()
    {
        Assert.Equal(280, new DisenoAcople().TamanoDe(ZonaAcople.Derecha, 280));
    }

    [Fact]
    public void FijarTamano_RespetaElMinimo()
    {
        var d = new DisenoAcople();
        d.FijarTamano(ZonaAcople.Abajo, 30);
        Assert.Equal(DisenoAcople.TamanoMinimo, d.TamanoDe(ZonaAcople.Abajo, 220));

        d.FijarTamano(ZonaAcople.Abajo, 300);
        Assert.Equal(300, d.TamanoDe(ZonaAcople.Abajo, 220));
    }

    // ---------------- Leer del disco ----------------

    [Fact]
    public void Json_VaYVuelve()
    {
        var d = DeFabrica();
        d.Activar("salida");
        d.FijarTamano(ZonaAcople.Derecha, 333);
        d.Ocultar("explorador");

        var leido = JsonSerializer.Deserialize<DisenoAcople>(JsonSerializer.Serialize(d))!;
        leido.EnsureValid();

        Assert.Equal("salida", leido.ActivoEn(ZonaAcople.Abajo));
        Assert.Equal(333, leido.TamanoDe(ZonaAcople.Derecha, 280));
        Assert.False(leido.EstaVisible("explorador"));
        Assert.Equal(ZonaAcople.Derecha, leido.ZonaDe("explorador"));
    }

    [Fact]
    public void EnsureValid_ArreglaLoQueVieneRoto()
    {
        var d = new DisenoAcople
        {
            Paneles = new()
            {
                ["a"] = new UbicacionPanel { Zona = (ZonaAcople)99, Orden = 7 },
                ["b"] = new UbicacionPanel { Zona = ZonaAcople.Abajo, Orden = 40 },
                ["c"] = new UbicacionPanel { Zona = ZonaAcople.Abajo, Orden = 3 },
                ["x"] = null!
            },
            Tamanos = new() { [ZonaAcople.Abajo] = 5, [(ZonaAcople)42] = 300 },
            Activos = new() { [ZonaAcople.Izquierda] = "b", [ZonaAcople.Abajo] = "fantasma" }
        };

        d.EnsureValid();

        Assert.False(d.Paneles.ContainsKey("x"));
        Assert.Equal(ZonaAcople.Derecha, d.ZonaDe("a"));
        Assert.Equal(new[] { "c", "b" }, d.VisiblesEn(ZonaAcople.Abajo));
        Assert.Equal(new[] { 0, 1 }, new[] { d.Paneles["c"].Orden, d.Paneles["b"].Orden });
        Assert.Equal(DisenoAcople.TamanoMinimo, d.Tamanos[ZonaAcople.Abajo]);
        Assert.Single(d.Tamanos);
        Assert.Empty(d.Activos);   // "b" no está en la izquierda y "fantasma" no existe
    }

    [Fact]
    public void EnsureValid_ConNulos_NoRevienta()
    {
        var d = new DisenoAcople { Paneles = null!, Tamanos = null!, Activos = null! };
        d.EnsureValid();
        Assert.Empty(d.Paneles);
    }
}
