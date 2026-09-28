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

    // ---------------- Auto-ocultar (la chincheta, 3c) ----------------

    [Fact]
    public void AutoOcultar_LoSacaDeLaZona_YLoPoneEnElBorde()
    {
        var d = DeFabrica();

        d.AutoOcultar("explorador");

        Assert.Empty(d.VisiblesEn(ZonaAcople.Derecha));
        Assert.False(d.ZonaVisible(ZonaAcople.Derecha));
        Assert.Equal(new[] { "explorador" }, d.AutoOcultosEn(ZonaAcople.Derecha));
        Assert.True(d.EstaVisible("explorador"));      // sigue a mano: su pestaña del borde
        Assert.True(d.EstaAutoOculto("explorador"));
    }

    [Fact]
    public void AutoOcultar_ElActivo_PasaALaVecina()
    {
        // Tres paneles: con dos, «la vecina» y «la primera» son la misma y la
        // prueba pasaba aunque no se buscara la vecina (romper_diseno_acople).
        var d = DeFabrica();                       // abajo: errores, salida
        d.Registrar("pila", ZonaAcople.Abajo);     //        pila
        d.Activar("salida");

        d.AutoOcultar("salida");

        Assert.Equal("pila", d.ActivoEn(ZonaAcople.Abajo));
        Assert.Equal(new[] { "errores", "pila" }, d.VisiblesEn(ZonaAcople.Abajo));
        Assert.Equal(new[] { "salida" }, d.AutoOcultosEn(ZonaAcople.Abajo));
    }

    [Fact]
    public void Fijar_LoVuelveAAcoplar_EnSuLugarYActivo()
    {
        var d = DeFabrica();
        d.AutoOcultar("errores");
        d.Activar("salida");    // si no, «errores» sería el activo solo por ser el primero

        d.Fijar("errores");

        Assert.Equal(new[] { "errores", "salida" }, d.VisiblesEn(ZonaAcople.Abajo));
        Assert.Equal("errores", d.ActivoEn(ZonaAcople.Abajo));
        Assert.Empty(d.AutoOcultosEn(ZonaAcople.Abajo));
        Assert.False(d.EstaAutoOculto("errores"));
    }

    /// <summary>Cerrarlo con ✕ y volver a mostrarlo lo trae como estaba: auto-oculto.</summary>
    [Fact]
    public void OcultarYMostrar_UnAutoOculto_VuelveAutoOculto()
    {
        var d = DeFabrica();
        d.AutoOcultar("explorador");

        d.Ocultar("explorador");
        Assert.Empty(d.AutoOcultosEn(ZonaAcople.Derecha));
        Assert.False(d.EstaVisible("explorador"));

        d.Mostrar("explorador");
        Assert.Equal(new[] { "explorador" }, d.AutoOcultosEn(ZonaAcople.Derecha));
        Assert.Empty(d.VisiblesEn(ZonaAcople.Derecha));
    }

    /// <summary>Ocultar un auto-oculto no le cambia la pestaña activa a la zona.</summary>
    [Fact]
    public void Ocultar_UnAutoOculto_NoTocaElActivoDeLaZona()
    {
        var d = DeFabrica();
        d.Activar("salida");
        d.AutoOcultar("errores");

        d.Ocultar("errores");

        Assert.Equal("salida", d.ActivoEn(ZonaAcople.Abajo));
    }

    [Fact]
    public void Mover_UnAutoOculto_LoAcopla()
    {
        var d = DeFabrica();
        d.AutoOcultar("explorador");

        d.Mover("explorador", ZonaAcople.Izquierda);

        Assert.Equal(new[] { "explorador" }, d.VisiblesEn(ZonaAcople.Izquierda));
        Assert.False(d.EstaAutoOculto("explorador"));
    }

    /// <summary>Darle el foco a un auto-oculto (desplegado) no le cambia el activo a la zona.</summary>
    [Fact]
    public void Activar_UnAutoOculto_NoTocaElActivoDeLaZona()
    {
        var d = DeFabrica();                       // abajo: errores, salida
        d.Registrar("pila", ZonaAcople.Abajo);     //        pila
        d.Registrar("consola", ZonaAcople.Abajo);  //        consola
        d.AutoOcultar("consola");
        d.Activar("salida");

        d.Activar("consola");
        d.Ocultar("salida");

        // Sin la guarda, «consola» quedaba anotado como activo: al ocultar
        // «salida» no se buscaba la vecina (pila) y la zona volvía a la
        // primera (errores).
        Assert.Equal("pila", d.ActivoEn(ZonaAcople.Abajo));
        Assert.False(d.Activos.ContainsValue("consola"));
    }

    [Fact]
    public void AutoOcultar_DosVeces_NoCambiaNada()
    {
        var d = DeFabrica();
        d.AutoOcultar("errores");
        d.Activar("salida");

        d.AutoOcultar("errores");

        Assert.Equal("salida", d.ActivoEn(ZonaAcople.Abajo));
        Assert.Equal(new[] { "errores" }, d.AutoOcultosEn(ZonaAcople.Abajo));
    }

    /// <summary>
    /// Un auto-oculto cerrado (✕) sigue cerrado aunque le pidan auto-ocultarse:
    /// ya lo está, y volver a mostrarlo es cosa de Mostrar.
    /// </summary>
    [Fact]
    public void AutoOcultar_UnAutoOcultoCerrado_NoLoVuelveAMostrar()
    {
        var d = DeFabrica();
        d.AutoOcultar("errores");
        d.Ocultar("errores");

        d.AutoOcultar("errores");

        Assert.False(d.EstaVisible("errores"));
        Assert.Empty(d.AutoOcultosEn(ZonaAcople.Abajo));
    }

    /// <summary>Mostrar un auto-oculto (vuelve a su pestaña del borde) no le cambia el activo a la zona.</summary>
    [Fact]
    public void Mostrar_UnAutoOculto_NoTocaElActivoDeLaZona()
    {
        var d = DeFabrica();                       // abajo: errores, salida
        d.Registrar("pila", ZonaAcople.Abajo);     //        pila
        d.Registrar("consola", ZonaAcople.Abajo);  //        consola
        d.AutoOcultar("consola");
        d.Ocultar("consola");
        d.Activar("salida");

        d.Mostrar("consola");
        d.Ocultar("salida");

        // Si Mostrar anotara a «consola» como activo, al ocultar «salida» no
        // se buscaría la vecina (pila) y la zona volvería a la primera.
        Assert.Equal("pila", d.ActivoEn(ZonaAcople.Abajo));
        Assert.Equal(new[] { "consola" }, d.AutoOcultosEn(ZonaAcople.Abajo));
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
        d.AutoOcultar("errores");

        var leido = JsonSerializer.Deserialize<DisenoAcople>(JsonSerializer.Serialize(d))!;
        leido.EnsureValid();

        Assert.True(leido.EstaAutoOculto("errores"));
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
