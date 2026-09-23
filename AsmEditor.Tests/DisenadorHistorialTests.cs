using AsmEditor.Core.Disenador;

namespace AsmEditor.Tests;

/// <summary>
/// Deshacer y rehacer del diseñador: que cada acción sea un paso, que una
/// acción sin cambios no deje paso, y que el estado sucio se mida contra lo
/// que está en el disco.
/// </summary>
public class DisenadorHistorialTests
{
    private static FormularioDisenado FormularioConUnBoton()
    {
        var f = new FormularioDisenado();
        f.AgregarControl(TipoControl.Boton, 8, 8);
        return f;
    }

    [Fact]
    public void RecienAbierto_NoHayNadaQueDeshacerNiRehacer()
    {
        var h = new HistorialDisenador(FormularioConUnBoton());

        Assert.False(h.PuedeDeshacer);
        Assert.False(h.PuedeRehacer);
        Assert.Null(h.Deshacer());
        Assert.Null(h.Rehacer());
        Assert.False(h.EstaSucio);
    }

    [Fact]
    public void Deshacer_DevuelveElEstadoAnteriorALaAccion()
    {
        var f = FormularioConUnBoton();
        var h = new HistorialDisenador(f);

        f.Controles[0].X = 100;
        Assert.True(h.Confirmar(f));

        var anterior = h.Deshacer();

        Assert.NotNull(anterior);
        Assert.Equal(8, anterior!.Controles[0].X);
    }

    [Fact]
    public void Rehacer_VuelveAlEstadoDeshecho()
    {
        var f = FormularioConUnBoton();
        var h = new HistorialDisenador(f);

        f.AgregarControl(TipoControl.Etiqueta, 40, 40);
        h.Confirmar(f);

        var sinEtiqueta = h.Deshacer()!;
        Assert.Single(sinEtiqueta.Controles);

        var conEtiqueta = h.Rehacer()!;
        Assert.Equal(2, conEtiqueta.Controles.Count);
        Assert.Equal(TipoControl.Etiqueta, conEtiqueta.Controles[1].Tipo);
    }

    [Fact]
    public void VariosPasos_SeDeshacenEnOrdenInverso()
    {
        var f = FormularioConUnBoton();
        var h = new HistorialDisenador(f);

        foreach (var x in new[] { 20, 30, 40 })
        {
            f.Controles[0].X = x;
            h.Confirmar(f);
        }

        Assert.Equal(30, h.Deshacer()!.Controles[0].X);
        Assert.Equal(20, h.Deshacer()!.Controles[0].X);
        Assert.Equal(8, h.Deshacer()!.Controles[0].X);
        Assert.Null(h.Deshacer());
    }

    [Fact]
    public void UnaAccionSinCambios_NoDejaPaso()
    {
        var f = FormularioConUnBoton();
        var h = new HistorialDisenador(f);

        // Un arrastre que termina donde empezó.
        f.Controles[0].X = 50;
        f.Controles[0].X = 8;

        Assert.False(h.Confirmar(f));
        Assert.False(h.PuedeDeshacer);
    }

    [Fact]
    public void UnCambioNuevo_BorraElRehacer()
    {
        var f = FormularioConUnBoton();
        var h = new HistorialDisenador(f);

        f.Controles[0].X = 20;
        h.Confirmar(f);

        f = h.Deshacer()!;
        Assert.True(h.PuedeRehacer);

        f.Controles[0].Y = 60;
        h.Confirmar(f);

        Assert.False(h.PuedeRehacer);
        Assert.Null(h.Rehacer());
    }

    [Fact]
    public void AlPasarElLimite_SeOlvidaElPasoMasViejo()
    {
        var f = FormularioConUnBoton();
        var h = new HistorialDisenador(f, limite: 3);

        foreach (var x in new[] { 20, 30, 40, 50 })
        {
            f.Controles[0].X = x;
            h.Confirmar(f);
        }

        Assert.Equal(3, h.PasosParaDeshacer);

        // Se perdió el 8 original; lo más viejo que queda es el 20.
        Assert.Equal(40, h.Deshacer()!.Controles[0].X);
        Assert.Equal(30, h.Deshacer()!.Controles[0].X);
        Assert.Equal(20, h.Deshacer()!.Controles[0].X);
        Assert.Null(h.Deshacer());
    }

    [Fact]
    public void LoQueDevuelveDeshacer_EsUnaCopiaQueNoTocaElHistorial()
    {
        var f = FormularioConUnBoton();
        var h = new HistorialDisenador(f);

        f.Controles[0].X = 20;
        h.Confirmar(f);

        var repuesto = h.Deshacer()!;
        repuesto.Controles[0].X = 999;   // el canvas la edita

        var rehecho = h.Rehacer()!;
        Assert.Equal(20, rehecho.Controles[0].X);

        // Y el paso deshecho sigue intacto.
        Assert.Equal(8, h.Deshacer()!.Controles[0].X);
    }

    [Fact]
    public void RecuperaTodasLasPropiedadesDelControl()
    {
        var f = FormularioConUnBoton();
        var h = new HistorialDisenador(f);

        var c = f.Controles[0];
        c.Nombre = "aceptar";
        c.Texto = "Aceptar";
        c.EstilosExtra = "BS_DEFPUSHBUTTON";
        c.OcultoAlInicio = true;
        c.DeshabilitadoAlInicio = true;
        h.Confirmar(f);

        f.Controles.Clear();
        h.Confirmar(f);

        var r = h.Deshacer()!.Controles[0];

        Assert.Equal("aceptar", r.Nombre);
        Assert.Equal("Aceptar", r.Texto);
        Assert.Equal("BS_DEFPUSHBUTTON", r.EstilosExtra);
        Assert.True(r.OcultoAlInicio);
        Assert.True(r.DeshabilitadoAlInicio);
        Assert.Equal(c.Id, r.Id);
    }

    // ---------------- Estado sucio ----------------

    [Fact]
    public void UnCambio_LoDejaSucio_YDeshacerHastaLoGuardado_LoLimpia()
    {
        var f = FormularioConUnBoton();
        var h = new HistorialDisenador(f);

        f.Controles[0].X = 20;
        h.Confirmar(f);
        Assert.True(h.EstaSucio);

        h.Deshacer();
        Assert.False(h.EstaSucio);

        h.Rehacer();
        Assert.True(h.EstaSucio);
    }

    [Fact]
    public void DespuesDeGuardar_DeshacerLoVuelveAEnsuciar()
    {
        var f = FormularioConUnBoton();
        var h = new HistorialDisenador(f);

        f.Controles[0].X = 20;
        h.Confirmar(f);
        h.MarcarGuardado(f);
        Assert.False(h.EstaSucio);

        h.Deshacer();
        Assert.True(h.EstaSucio);
    }

    [Fact]
    public void MarcarGuardado_ConfirmaLoQueQuedoSinConfirmar()
    {
        var f = FormularioConUnBoton();
        var h = new HistorialDisenador(f);

        f.Controles[0].X = 20;   // cambio que nadie confirmó
        h.MarcarGuardado(f);

        Assert.False(h.EstaSucio);
        Assert.True(h.PuedeDeshacer);
        Assert.Equal(8, h.Deshacer()!.Controles[0].X);
    }
}
