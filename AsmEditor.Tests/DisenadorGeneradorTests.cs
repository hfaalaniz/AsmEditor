using System.Diagnostics;
using AsmEditor.Core;
using AsmEditor.Core.Disenador;

namespace AsmEditor.Tests;

/// <summary>
/// El código que genera el diseñador.
///
/// ⚠ LA PRUEBA QUE VALE ES LA QUE ENSAMBLA DE VERDAD. Comparar strings solo
/// dice que el generador escribe lo que el autor creía; pasarle el resultado a
/// nasm.exe dice si el código EXISTE como programa. Las de texto están para
/// señalar QUÉ se rompió cuando la de NASM falla.
/// </summary>
public class DisenadorGeneradorTests
{
    // ================= Literales de cadena =================
    //
    // NASM no tiene escapes dentro de las comillas: hay que sacar los
    // caracteres especiales afuera, como bytes sueltos.

    [Fact]
    public void CadenaAsm_TextoComunVaEntreComillas()
    {
        Assert.Equal("\"Aceptar\"", GeneradorAsm.CadenaAsm("Aceptar"));
    }

    [Fact]
    public void CadenaAsm_VacioNoRompe()
    {
        Assert.Equal("\"\"", GeneradorAsm.CadenaAsm(""));
        Assert.Equal("\"\"", GeneradorAsm.CadenaAsm(null));
    }

    [Fact]
    public void CadenaAsm_LasComillasSalenComoByte()
    {
        // Adentro de "..." no hay forma de escapar una comilla doble.
        var r = GeneradorAsm.CadenaAsm("di \"hola\"");

        Assert.Equal("\"di \", 34, \"hola\", 34", r);
    }

    [Fact]
    public void CadenaAsm_LosAcentosSalenComoByte()
    {
        // Un acento es > 126 y su byte depende de la codificación del archivo:
        // emitirlo numérico lo hace independiente de cómo se guarde el .inc.
        var r = GeneradorAsm.CadenaAsm("Botón");

        Assert.Contains("\"Bot\"", r);
        Assert.Contains("243", r);   // 'ó' en latin-1
        Assert.Contains("\"n\"", r);
    }

    [Fact]
    public void CadenaAsm_LosSaltosDeLineaSalenComoBytes()
    {
        var r = GeneradorAsm.CadenaAsm("uno\r\ndos");

        Assert.Equal("\"uno\", 13, 10, \"dos\"", r);
    }

    // ================= Contenido del .inc =================

    [Fact]
    public void Include_AvisaQueNoSeEdita()
    {
        var inc = GeneradorAsm.GenerarInclude(DisenadorModeloTests.FormularioDeEjemplo());

        Assert.Contains("NO EDITAR A MANO", inc);
    }

    [Fact]
    public void Include_DefineUnIdPorControl()
    {
        var f = DisenadorModeloTests.FormularioDeEjemplo();

        var inc = GeneradorAsm.GenerarInclude(f);

        foreach (var c in f.Controles)
        {
            Assert.Contains($"%define {c.SimboloId}", inc);
        }
    }

    [Fact]
    public void Include_LasClasesSeEmitenUnaSolaVez()
    {
        // Cinco controles, tres de ellos comparten clase: no puede haber cinco
        // literales "BUTTON".
        var f = new FormularioDisenado { Nombre = "F" };
        f.AgregarControl(TipoControl.Boton, 0, 0);
        f.AgregarControl(TipoControl.Boton, 0, 40);
        f.AgregarControl(TipoControl.Casilla, 0, 80);

        var inc = GeneradorAsm.GenerarInclude(f);

        var veces = inc.Split("db \"BUTTON\", 0").Length - 1;

        Assert.Equal(1, veces);
    }

    [Fact]
    public void Include_LaTablaTieneUnaFilaPorControl()
    {
        var f = DisenadorModeloTests.FormularioDeEjemplo();

        var inc = GeneradorAsm.GenerarInclude(f);

        foreach (var c in f.Controles)
        {
            Assert.Contains($"; {c.Nombre} —", inc);
        }
    }

    [Fact]
    public void Include_X64UsaPunterosDe8YX86De4()
    {
        var f = DisenadorModeloTests.FormularioDeEjemplo();

        f.Arquitectura = TargetArch.Win64;
        var x64 = GeneradorAsm.GenerarInclude(f);

        f.Arquitectura = TargetArch.Win32;
        var x86 = GeneradorAsm.GenerarInclude(f);

        Assert.Contains("dq claseWin32", x64);
        Assert.Contains("dd claseWin32", x86);
        Assert.Contains("TAM_ENTRADA_PRINCIPAL 48", x64);
        Assert.Contains("TAM_ENTRADA_PRINCIPAL 36", x86);
    }

    [Fact]
    public void Include_ElDialogoNoRegistraClaseNiTieneBucle()
    {
        var f = DisenadorModeloTests.FormularioDeEjemplo();
        f.Tipo = TipoFormulario.Dialogo;

        var inc = GeneradorAsm.GenerarInclude(f);

        Assert.DoesNotContain("call RegisterClassExA", inc);
        Assert.DoesNotContain("call GetMessageA", inc);
    }

    [Fact]
    public void Include_LaVentanaPrincipalSiRegistraClaseYTieneBucle()
    {
        var inc = GeneradorAsm.GenerarInclude(DisenadorModeloTests.FormularioDeEjemplo());

        Assert.Contains("call RegisterClassExA", inc);
        Assert.Contains("call GetMessageA", inc);
    }

    [Fact]
    public void Include_SinControlesNoRompe()
    {
        var f = new FormularioDisenado { Nombre = "Vacio" };

        var inc = GeneradorAsm.GenerarInclude(f);

        Assert.Contains("todavía no tiene controles", inc);
    }

    // ================= El esqueleto =================

    [Fact]
    public void Esqueleto_ArmaUnManejadorPorControlQueNotifica()
    {
        var f = DisenadorModeloTests.FormularioDeEjemplo();

        var asm = GeneradorEsqueleto.Generar(f);

        // Botón, casilla y lista avisan; etiqueta y campo no.
        Assert.Contains(".al_botonaceptar:", asm);
        Assert.Contains(".al_casillarecordar:", asm);
        Assert.Contains(".al_lista:", asm);
        Assert.DoesNotContain(".al_etiquetanombre:", asm);
        Assert.DoesNotContain(".al_camponombre:", asm);
    }

    [Fact]
    public void Esqueleto_DeclaraSoloLasApisQueUsa()
    {
        // Sin controles que notifiquen no hace falta GetDlgItem.
        var f = new FormularioDisenado { Nombre = "F" };
        f.AgregarControl(TipoControl.Etiqueta, 0, 0);

        var apis = GeneradorEsqueleto.ApisNecesarias(f);

        Assert.DoesNotContain("GetDlgItem", apis);
        Assert.Contains("CreateWindowExA", apis);

        f.AgregarControl(TipoControl.Boton, 0, 40);

        Assert.Contains("GetDlgItem", GeneradorEsqueleto.ApisNecesarias(f));
    }

    [Fact]
    public void Esqueleto_NoDeclaraApisRepetidas()
    {
        var apis = GeneradorEsqueleto.ApisNecesarias(DisenadorModeloTests.FormularioDeEjemplo());

        Assert.Equal(apis.Count, apis.Distinct().Count());
    }

    [Fact]
    public void Esqueleto_ElPuntoDeEntradaDependeDeLaArquitectura()
    {
        // NASM con -f win32 decora los símbolos con guion bajo.
        var f = DisenadorModeloTests.FormularioDeEjemplo();

        f.Arquitectura = TargetArch.Win64;
        Assert.Contains("global main", GeneradorEsqueleto.Generar(f));

        f.Arquitectura = TargetArch.Win32;
        Assert.Contains("global _main", GeneradorEsqueleto.Generar(f));
    }

    [Fact]
    public void Esqueleto_ElDialogoNoTienePuntoDeEntrada()
    {
        var f = DisenadorModeloTests.FormularioDeEjemplo();
        f.Tipo = TipoFormulario.Dialogo;

        var asm = GeneradorEsqueleto.Generar(f);

        Assert.DoesNotContain("global main", asm);
        Assert.DoesNotContain("call ExitProcess", asm);
    }

    // ================= Manejadores faltantes =================

    [Fact]
    public void ManejadoresFaltantes_SiEstanTodosNoProponeNada()
    {
        var f = DisenadorModeloTests.FormularioDeEjemplo();
        var asm = GeneradorEsqueleto.Generar(f);

        Assert.Equal("", GeneradorEsqueleto.GenerarManejadoresFaltantes(f, asm));
    }

    [Fact]
    public void ManejadoresFaltantes_ProponeSoloElControlNuevo()
    {
        var f = DisenadorModeloTests.FormularioDeEjemplo();
        var asmViejo = GeneradorEsqueleto.Generar(f);

        var nuevo = f.AgregarControl(TipoControl.Boton, 200, 200);
        nuevo.Nombre = "botonNuevo";

        var texto = GeneradorEsqueleto.GenerarManejadoresFaltantes(f, asmViejo);

        Assert.Contains(".al_botonnuevo", texto);
        Assert.DoesNotContain(".al_botonaceptar:", texto);
    }

    // ================= Generación de archivos =================

    [Fact]
    public void GenerarArchivos_NoPisaElAsmDelUsuario()
    {
        // ⚠ Es la garantía central del diseño: el código del usuario no se toca.
        var dir = CarpetaTemporal();

        try
        {
            var f = DisenadorModeloTests.FormularioDeEjemplo();
            var ruta = Path.Combine(dir, "Principal.asmform");

            ArchivoFormulario.GenerarArchivos(ruta, f);

            var rutaAsm = ArchivoFormulario.RutaAsm(ruta);
            File.WriteAllText(rutaAsm, "; código que escribió el usuario\nmain:\n    ret\n");

            f.AgregarControl(TipoControl.Boton, 10, 200);
            var r = ArchivoFormulario.GenerarArchivos(ruta, f);

            Assert.Equal("; código que escribió el usuario\nmain:\n    ret\n",
                         File.ReadAllText(rutaAsm));
            Assert.False(r.SeCreoElAsm);
            Assert.True(r.HayManejadoresPendientes);
        }
        finally { Directory.Delete(dir, true); }
    }

    /// <summary>
    /// ⚠ El %include tiene que nombrar al archivo .inc REAL, que se llama como
    /// el .asmform elegido por el usuario, no como el formulario. Un formulario
    /// "Formulario1" guardado como "Ciclo.asmform" produce "Ciclo.inc"; si el
    /// .asm dice «%include "Formulario1.inc"», NASM falla con «unable to open
    /// include file» y el formulario no compila nunca.
    /// </summary>
    [Fact]
    public void GenerarArchivos_ElIncludeNombraAlArchivoNoAlFormulario()
    {
        var dir = CarpetaTemporal();

        try
        {
            var f = DisenadorModeloTests.FormularioDeEjemplo();
            f.Nombre = "Formulario1";

            // El archivo se llama distinto del formulario, a propósito.
            var ruta = Path.Combine(dir, "OtroNombre.asmform");

            var r = ArchivoFormulario.GenerarArchivos(ruta, f);

            var asm = File.ReadAllText(r.RutaAsm);

            Assert.Contains("%include \"OtroNombre.inc\"", asm);
            Assert.DoesNotContain("%include \"Formulario1.inc\"", asm);

            // Y el .inc referido tiene que existir de verdad.
            var incluido = Path.Combine(dir, "OtroNombre.inc");
            Assert.True(File.Exists(incluido),
                "El .asm incluye un archivo que no se generó.");
        }
        finally { Directory.Delete(dir, true); }
    }

    [Fact]
    public void GenerarArchivos_SiElFormularioEstaMalNoEscribeNada()
    {
        // Un .inc a medias es peor que no generarlo: no ensambla y el error
        // aparece lejos de la causa.
        var dir = CarpetaTemporal();

        try
        {
            var f = new FormularioDisenado { Nombre = "F" };
            f.Controles.Add(new ControlDisenado { Nombre = "mi boton" });

            var ruta = Path.Combine(dir, "F.asmform");

            Assert.Throws<InvalidOperationException>(
                () => ArchivoFormulario.GenerarArchivos(ruta, f));

            Assert.False(File.Exists(ArchivoFormulario.RutaInclude(ruta)));
            Assert.False(File.Exists(ArchivoFormulario.RutaAsm(ruta)));
        }
        finally { Directory.Delete(dir, true); }
    }

    // ================= LA PRUEBA DE VERDAD: ensamblar =================

    /// <summary>
    /// Ensambla con nasm.exe el .asm y el .inc generados. Es la única prueba
    /// que demuestra que el código generado es un programa y no texto que se
    /// le parece.
    ///
    /// Si no hay nasm.exe la prueba se salta: no puede fallar por el entorno
    /// de otra máquina, pero acá corre siempre.
    /// </summary>
    [SkippableTheory]
    [InlineData(TargetArch.Win64, TipoFormulario.VentanaPrincipal)]
    [InlineData(TargetArch.Win32, TipoFormulario.VentanaPrincipal)]
    [InlineData(TargetArch.Win64, TipoFormulario.Dialogo)]
    [InlineData(TargetArch.Win32, TipoFormulario.Dialogo)]
    public void ElCodigoGenerado_EnsamblaConNasm(TargetArch arch, TipoFormulario tipo)
    {
        var nasm = BuscarNasm();
        Skip.If(nasm is null, "No se encontró nasm.exe.");

        var f = DisenadorModeloTests.FormularioDeEjemplo();
        f.Arquitectura = arch;
        f.Tipo = tipo;

        EnsamblarYExigirExito(nasm!, f);
    }

    /// <summary>
    /// Un formulario con TODOS los tipos de control y textos con acentos,
    /// comillas y saltos de línea: es donde se rompen los literales.
    /// </summary>
    [SkippableFact]
    public void ElCodigoGenerado_EnsamblaConTodosLosTiposYTextosDificiles()
    {
        var nasm = BuscarNasm();
        Skip.If(nasm is null, "No se encontró nasm.exe.");

        var f = new FormularioDisenado
        {
            Nombre = "Completo",
            Titulo = "Prueba \"difícil\" — año 2026",
            Arquitectura = TargetArch.Win64
        };

        int y = 10;

        foreach (var tipo in Enum.GetValues<TipoControl>())
        {
            if (tipo == TipoControl.Personalizado) continue;

            var c = f.AgregarControl(tipo, 10, y);
            c.Texto = "Ñandú \"con comillas\"\r\ny salto";
            y += 40;
        }

        var pers = f.AgregarControl(TipoControl.Personalizado, 10, y);
        pers.ClasePersonalizada = "MiClasePropia";
        pers.Texto = "";

        EnsamblarYExigirExito(nasm!, f);
    }

    private static void EnsamblarYExigirExito(string nasm, FormularioDisenado f)
    {
        var dir = CarpetaTemporal();

        try
        {
            // ⚠ EL ARCHIVO SE LLAMA DISTINTO DEL FORMULARIO, A PROPÓSITO. Con
            // los dos nombres iguales, un %include mal derivado apunta igual al
            // archivo correcto por casualidad y la prueba no ve el error.
            var ruta = Path.Combine(dir, "ArchivoDistinto" + ArchivoFormulario.Extension);

            ArchivoFormulario.GenerarArchivos(ruta, f);

            var rutaAsm = ArchivoFormulario.RutaAsm(ruta);
            var obj = Path.Combine(dir, f.Nombre + ".obj");
            var formato = f.EsX64 ? "win64" : "win32";

            var (codigo, salida) = Ejecutar(
                nasm, $"-f {formato} \"{rutaAsm}\" -o \"{obj}\"", dir);

            // La salida de NASM va entera al mensaje: filtrarla dejaría la
            // prueba diciendo "falló" sin decir por qué.
            Assert.True(codigo == 0,
                $"NASM rechazó el código generado ({formato}, {f.Tipo}):{Environment.NewLine}" +
                $"{salida}{Environment.NewLine}" +
                $"--- {f.Nombre}.asm ---{Environment.NewLine}{File.ReadAllText(rutaAsm)}" +
                $"--- {f.Nombre}.inc ---{Environment.NewLine}" +
                $"{File.ReadAllText(ArchivoFormulario.RutaInclude(ruta))}");

            Assert.True(File.Exists(obj), "NASM dijo que todo bien pero no dejó el .obj.");
        }
        finally { Directory.Delete(dir, true); }
    }

    // ---------------- Utilidades ----------------

    private static string CarpetaTemporal()
    {
        var dir = Path.Combine(Path.GetTempPath(), "asmrad_" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(dir);
        return dir;
    }

    /// <summary>
    /// nasm.exe, buscándolo donde vive en este proyecto y después en el PATH.
    /// </summary>
    private static string? BuscarNasm()
    {
        var candidatos = new List<string>();

        var dir = AppContext.BaseDirectory;

        // Desde bin/Debug/net8.0 hasta la raíz del proyecto NASM.
        for (int i = 0; i < 8 && dir is not null; i++)
        {
            candidatos.Add(Path.Combine(dir, "nasm.exe"));
            dir = Path.GetDirectoryName(dir.TrimEnd(Path.DirectorySeparatorChar));
        }

        candidatos.Add(@"C:\Users\Fabian\NASM\nasm.exe");

        foreach (var c in candidatos)
        {
            if (File.Exists(c)) return c;
        }

        var path = Environment.GetEnvironmentVariable("PATH") ?? "";

        foreach (var carpeta in path.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries))
        {
            try
            {
                var c = Path.Combine(carpeta.Trim(), "nasm.exe");
                if (File.Exists(c)) return c;
            }
            catch (ArgumentException) { /* entrada inválida en el PATH */ }
        }

        return null;
    }

    private static (int codigo, string salida) Ejecutar(string exe, string args, string dir)
    {
        var psi = new ProcessStartInfo
        {
            FileName = exe,
            Arguments = args,
            WorkingDirectory = dir,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var p = Process.Start(psi)!;

        var salida = p.StandardOutput.ReadToEnd() + p.StandardError.ReadToEnd();
        p.WaitForExit(30000);

        return (p.ExitCode, salida);
    }
}
