using AsmEditor.Core;
using AsmEditor.Core.Disenador;

namespace AsmEditor.Tests;

/// <summary>
/// El modelo del diseñador: validación de nombres, asignación de Ids y
/// detección de lo que haría fallar al ensamblador.
/// </summary>
public class DisenadorModeloTests
{
    // ---------------- Nombres ----------------

    [Theory]
    [InlineData("boton1")]
    [InlineData("_interno")]
    [InlineData("CampoNombre")]
    [InlineData("x")]
    [InlineData("control_2")]
    public void NombreValido_AceptaIdentificadoresDeEnsamblador(string nombre)
    {
        Assert.True(ControlDisenado.NombreValido(nombre));
        Assert.Null(ControlDisenado.ExplicarNombreInvalido(nombre));
    }

    [Theory]
    [InlineData("", "vacío")]
    [InlineData("   ", "vacío")]
    [InlineData("1boton", "empezar")]
    [InlineData("mi boton", "espacios")]
    [InlineData("año", "no sirve")]
    [InlineData("boton-1", "no sirve")]
    [InlineData("botón", "no sirve")]
    public void NombreValido_RechazaLoQueNoEnsambla(string nombre, string fragmentoDelMotivo)
    {
        Assert.False(ControlDisenado.NombreValido(nombre));

        var motivo = ControlDisenado.ExplicarNombreInvalido(nombre);

        Assert.NotNull(motivo);
        Assert.Contains(fragmentoDelMotivo, motivo, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("rax")]
    [InlineData("RAX")]
    [InlineData("eax")]
    [InlineData("section")]
    [InlineData("global")]
    [InlineData("main")]
    public void NombreValido_RechazaPalabrasReservadas(string nombre)
    {
        Assert.False(ControlDisenado.NombreValido(nombre));
        Assert.Contains("reservada", ControlDisenado.ExplicarNombreInvalido(nombre)!);
    }

    [Theory]
    [InlineData("Mi Botón", "Mi_Boton")]
    [InlineData("año 2026", "ano_2026")]
    [InlineData("¿qué?", "que")]
    [InlineData("123", "_123")]
    [InlineData("rax", "rax_1")]
    public void SanearNombre_ProduceAlgoValido(string entrada, string esperado)
    {
        var saneado = ControlDisenado.SanearNombre(entrada);

        Assert.Equal(esperado, saneado);
        Assert.True(ControlDisenado.NombreValido(saneado),
            $"'{saneado}' debería ser un nombre válido");
    }

    [Fact]
    public void SanearNombre_TextoSinNadaUtilizableCaeEnLaAlternativa()
    {
        Assert.Equal("control", ControlDisenado.SanearNombre("!!!"));
        Assert.Equal("control", ControlDisenado.SanearNombre(""));
    }

    // ---------------- Ids ----------------

    [Fact]
    public void AsignarIds_ArrancaEn1001()
    {
        var f = new FormularioDisenado();
        f.Controles.Add(new ControlDisenado { Nombre = "a" });
        f.Controles.Add(new ControlDisenado { Nombre = "b" });

        f.AsignarIdsFaltantes();

        Assert.Equal(1001, f.Controles[0].Id);
        Assert.Equal(1002, f.Controles[1].Id);
    }

    [Fact]
    public void AsignarIds_NoTocaLosQueYaTienen()
    {
        // ⚠ El usuario pudo escribir código que compara contra ese número:
        // renumerar se lo rompe en silencio.
        var f = new FormularioDisenado();
        f.Controles.Add(new ControlDisenado { Nombre = "a", Id = 5000 });
        f.Controles.Add(new ControlDisenado { Nombre = "b" });

        f.AsignarIdsFaltantes();

        Assert.Equal(5000, f.Controles[0].Id);
        Assert.Equal(1001, f.Controles[1].Id);
    }

    [Fact]
    public void AsignarIds_NoRepiteUnoYaUsado()
    {
        var f = new FormularioDisenado();
        f.Controles.Add(new ControlDisenado { Nombre = "a", Id = 1001 });
        f.Controles.Add(new ControlDisenado { Nombre = "b" });
        f.Controles.Add(new ControlDisenado { Nombre = "c" });

        f.AsignarIdsFaltantes();

        Assert.Equal(1002, f.Controles[1].Id);
        Assert.Equal(1003, f.Controles[2].Id);

        var ids = f.Controles.Select(c => c.Id).ToList();
        Assert.Equal(ids.Count, ids.Distinct().Count());
    }

    // ---------------- Validación del formulario ----------------

    [Fact]
    public void Validar_FormularioSanoNoTieneErrores()
    {
        var f = FormularioDeEjemplo();

        Assert.Empty(f.Validar());
    }

    [Fact]
    public void Validar_DetectaNombresRepetidos()
    {
        var f = new FormularioDisenado();
        f.Controles.Add(new ControlDisenado { Nombre = "boton", Id = 1001 });
        f.Controles.Add(new ControlDisenado { Nombre = "boton", Id = 1002 });

        var errores = f.Validar();

        Assert.Contains(errores, e => e.Contains("repetido"));
    }

    [Fact]
    public void Validar_DetectaNombresQueSoloDifierenEnMayusculas()
    {
        // Los dos generarían el mismo símbolo IDC_BOTON.
        var f = new FormularioDisenado();
        f.Controles.Add(new ControlDisenado { Nombre = "boton", Id = 1001 });
        f.Controles.Add(new ControlDisenado { Nombre = "Boton", Id = 1002 });

        var errores = f.Validar();

        Assert.Contains(errores, e => e.Contains("IDC_BOTON"));
    }

    [Fact]
    public void Validar_DetectaIdsRepetidos()
    {
        var f = new FormularioDisenado();
        f.Controles.Add(new ControlDisenado { Nombre = "a", Id = 1001 });
        f.Controles.Add(new ControlDisenado { Nombre = "b", Id = 1001 });

        Assert.Contains(f.Validar(), e => e.Contains("identificador 1001"));
    }

    [Fact]
    public void Validar_ControlPersonalizadoSinClaseEsError()
    {
        var f = new FormularioDisenado();
        f.Controles.Add(new ControlDisenado
        {
            Nombre = "miControl",
            Tipo = TipoControl.Personalizado,
            ClasePersonalizada = ""
        });

        Assert.Contains(f.Validar(), e => e.Contains("clase personalizada"));
    }

    [Fact]
    public void Validar_TamanoCeroEsError()
    {
        var f = new FormularioDisenado();
        f.Controles.Add(new ControlDisenado { Nombre = "a", Ancho = 0, Alto = 10 });

        Assert.Contains(f.Validar(), e => e.Contains("mayores que cero"));
    }

    // ---------------- Estilos ----------------

    [Fact]
    public void ExpresionEstilo_TodoControlEsHijoYVisible()
    {
        var c = new ControlDisenado { Nombre = "b", Tipo = TipoControl.Boton };

        var estilo = c.ExpresionEstilo();

        Assert.StartsWith("WS_CHILD|WS_VISIBLE", estilo);
    }

    [Fact]
    public void ExpresionEstilo_OcultoNoLlevaVisible()
    {
        var c = new ControlDisenado { Nombre = "b", Tipo = TipoControl.Boton, OcultoAlInicio = true };

        Assert.DoesNotContain("WS_VISIBLE", c.ExpresionEstilo());
    }

    [Fact]
    public void ExpresionEstilo_IncluyeLosExtrasDelUsuario()
    {
        var c = new ControlDisenado
        {
            Nombre = "b",
            Tipo = TipoControl.Boton,
            EstilosExtra = "BS_DEFPUSHBUTTON"
        };

        Assert.Contains("BS_DEFPUSHBUTTON", c.ExpresionEstilo());
    }

    [Fact]
    public void ClaseEfectiva_LosTresTiposDeBotonSonClaseBUTTON()
    {
        // ⚠ Lo que los diferencia es el estilo, no la clase.
        foreach (var tipo in new[] { TipoControl.Boton, TipoControl.Casilla, TipoControl.Opcion })
        {
            var c = new ControlDisenado { Nombre = "x", Tipo = tipo };
            Assert.Equal("BUTTON", c.ClaseEfectiva);
        }
    }

    [Fact]
    public void ClaseEfectiva_PersonalizadoUsaLaClaseDelUsuario()
    {
        var c = new ControlDisenado
        {
            Nombre = "x",
            Tipo = TipoControl.Personalizado,
            ClasePersonalizada = "MiControlRaro"
        };

        Assert.Equal("MiControlRaro", c.ClaseEfectiva);
    }

    /// <summary>
    /// El valor numérico de EstiloBase y el texto de EstiloBaseTexto tienen que
    /// describir lo mismo: si se cambia uno y no el otro, el canvas dibuja una
    /// cosa y el .asm genera otra.
    /// </summary>
    [Theory]
    [InlineData(TipoControl.Campo)]
    [InlineData(TipoControl.CampoMultilinea)]
    [InlineData(TipoControl.Boton)]
    [InlineData(TipoControl.Casilla)]
    [InlineData(TipoControl.Opcion)]
    [InlineData(TipoControl.Grupo)]
    [InlineData(TipoControl.Lista)]
    [InlineData(TipoControl.Desplegable)]
    public void EstiloBase_ElNumeroYElTextoCoinciden(TipoControl tipo)
    {
        var numero = InfoTipoControl.EstiloBase(tipo);
        var texto = InfoTipoControl.EstiloBaseTexto(tipo);

        uint delTexto = texto
            .Split('|', StringSplitOptions.RemoveEmptyEntries)
            .Aggregate(0u, (acc, nombre) => acc | ValorDeConstante(nombre.Trim()));

        Assert.Equal(numero, delTexto);
    }

    /// <summary>Los valores de winuser.h, para cotejar el texto con el número.</summary>
    private static uint ValorDeConstante(string nombre) => nombre switch
    {
        "WS_BORDER"          => 0x00800000,
        "WS_TABSTOP"         => 0x00010000,
        "WS_VSCROLL"         => 0x00200000,
        "ES_AUTOHSCROLL"     => 0x0080,
        "ES_MULTILINE"       => 0x0004,
        "BS_AUTOCHECKBOX"    => 0x0003,
        "BS_AUTORADIOBUTTON" => 0x0009,
        "BS_GROUPBOX"        => 0x0007,
        "CBS_DROPDOWNLIST"   => 0x0003,
        _ => throw new Xunit.Sdk.XunitException(
            $"La prueba no conoce la constante '{nombre}'. Si se agregó a " +
            $"EstiloBaseTexto, agregá su valor de winuser.h acá.")
    };

    // ---------------- Agregar controles ----------------

    [Fact]
    public void AgregarControl_ProponeNombresQueNoSeRepiten()
    {
        var f = new FormularioDisenado();

        var a = f.AgregarControl(TipoControl.Boton, 10, 10);
        var b = f.AgregarControl(TipoControl.Boton, 10, 50);

        Assert.NotEqual(a.Nombre, b.Nombre);
        Assert.Empty(f.Validar());
    }

    [Fact]
    public void AgregarControl_DejaElFormularioValido()
    {
        var f = new FormularioDisenado();

        foreach (var tipo in Enum.GetValues<TipoControl>())
        {
            if (tipo == TipoControl.Personalizado) continue;
            f.AgregarControl(tipo, 10, 10);
        }

        Assert.Empty(f.Validar());
        Assert.All(f.Controles, c => Assert.NotEqual(0, c.Id));
    }

    // ---------------- Guardado ----------------

    [Fact]
    public void Serializar_IdaYVueltaConservaTodo()
    {
        var original = FormularioDeEjemplo();

        var recuperado = ArchivoFormulario.Deserializar(ArchivoFormulario.Serializar(original));

        Assert.Equal(original.Nombre, recuperado.Nombre);
        Assert.Equal(original.Titulo, recuperado.Titulo);
        Assert.Equal(original.Tipo, recuperado.Tipo);
        Assert.Equal(original.Arquitectura, recuperado.Arquitectura);
        Assert.Equal(original.Controles.Count, recuperado.Controles.Count);

        for (int i = 0; i < original.Controles.Count; i++)
        {
            Assert.Equal(original.Controles[i].Nombre, recuperado.Controles[i].Nombre);
            Assert.Equal(original.Controles[i].Tipo, recuperado.Controles[i].Tipo);
            Assert.Equal(original.Controles[i].Id, recuperado.Controles[i].Id);
            Assert.Equal(original.Controles[i].X, recuperado.Controles[i].X);
            Assert.Equal(original.Controles[i].Texto, recuperado.Controles[i].Texto);
        }
    }

    [Fact]
    public void Deserializar_ArchivoDanadoDaUnErrorEntendible()
    {
        var ex = Assert.Throws<InvalidDataException>(
            () => ArchivoFormulario.Deserializar("{ esto no es json"));

        Assert.Contains("dañado", ex.Message);
    }

    [Fact]
    public void Deserializar_VersionMasNuevaAvisaQueHayQueActualizar()
    {
        var json = """{ "Version": 99, "Formulario": { "Nombre": "F" } }""";

        var ex = Assert.Throws<InvalidDataException>(() => ArchivoFormulario.Deserializar(json));

        Assert.Contains("más nueva", ex.Message);
    }

    [Fact]
    public void Deserializar_ControlesSinIdRecibenUno()
    {
        var json = """
            {
              "Version": 1,
              "Formulario": {
                "Nombre": "F",
                "Controles": [ { "Nombre": "boton1", "Tipo": "Boton" } ]
              }
            }
            """;

        var f = ArchivoFormulario.Deserializar(json);

        Assert.Equal(1001, f.Controles[0].Id);
    }

    // ---------------- Ayuda ----------------

    internal static FormularioDisenado FormularioDeEjemplo() => new()
    {
        Nombre = "Principal",
        Titulo = "Ventana de prueba",
        Tipo = TipoFormulario.VentanaPrincipal,
        Arquitectura = TargetArch.Win64,
        Ancho = 400,
        Alto = 300,
        Controles =
        {
            new ControlDisenado
            {
                Nombre = "etiquetaNombre", Tipo = TipoControl.Etiqueta,
                Texto = "Nombre:", X = 20, Y = 20, Ancho = 80, Alto = 20, Id = 1001
            },
            new ControlDisenado
            {
                Nombre = "campoNombre", Tipo = TipoControl.Campo,
                Texto = "", X = 110, Y = 18, Ancho = 200, Alto = 24, Id = 1002
            },
            new ControlDisenado
            {
                Nombre = "botonAceptar", Tipo = TipoControl.Boton,
                Texto = "Aceptar", X = 110, Y = 60, Ancho = 100, Alto = 30, Id = 1003
            },
            new ControlDisenado
            {
                Nombre = "casillaRecordar", Tipo = TipoControl.Casilla,
                Texto = "Recordar", X = 110, Y = 100, Ancho = 140, Alto = 24, Id = 1004
            },
            new ControlDisenado
            {
                Nombre = "lista", Tipo = TipoControl.Lista,
                Texto = "", X = 20, Y = 140, Ancho = 290, Alto = 120, Id = 1005
            }
        }
    };
}
