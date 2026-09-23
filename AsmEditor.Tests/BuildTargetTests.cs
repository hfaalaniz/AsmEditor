using AsmEditor.Core;

namespace AsmEditor.Tests;

public class BuildTargetTests
{
    // ---------------- Armado de argumentos ----------------

    [Fact]
    public void NasmArgs_Win64()
    {
        var t = new BuildTarget { Arch = TargetArch.Win64 };

        var args = t.BuildNasmArguments(@"C:\p\prog.asm", @"C:\p\prog.obj");

        Assert.Equal("-f win64 \"C:\\p\\prog.asm\" -o \"C:\\p\\prog.obj\"", args);
    }

    [Fact]
    public void NasmArgs_Win32CambiaElFormato()
    {
        var t = new BuildTarget { Arch = TargetArch.Win32 };

        Assert.Contains("-f win32", t.BuildNasmArguments("a.asm", "a.obj"));
    }

    [Fact]
    public void NasmArgs_IncluyeLasBanderasExtra()
    {
        var t = new BuildTarget { Arch = TargetArch.Win64, ExtraAsmFlags = "-g -Wall" };

        var args = t.BuildNasmArguments("a.asm", "a.obj");

        Assert.Contains("-g -Wall", args);
        Assert.StartsWith("-f win64 -g -Wall", args);
    }

    [Fact]
    public void NasmArgs_RutasConEspaciosVanEntreComillas()
    {
        var t = new BuildTarget();

        var args = t.BuildNasmArguments(@"C:\mis cosas\mi prog.asm", @"C:\mis cosas\mi prog.obj");

        Assert.Contains("\"C:\\mis cosas\\mi prog.asm\"", args);
        Assert.Contains("\"C:\\mis cosas\\mi prog.obj\"", args);
    }

    [Fact]
    public void GoLinkArgs_EntryYLibrerias()
    {
        var t = new BuildTarget { EntryPoint = "main", Libraries = "kernel32.dll user32.dll" };

        var args = t.BuildGoLinkArguments(@"C:\p\prog.obj");

        Assert.Equal("/entry main \"C:\\p\\prog.obj\" kernel32.dll user32.dll", args);
    }

    [Fact]
    public void GoLinkArgs_SinLibreriasNoDejaEspaciosDeMas()
    {
        var t = new BuildTarget { EntryPoint = "main", Libraries = "" };

        var args = t.BuildGoLinkArguments("a.obj");

        Assert.Equal("/entry main \"a.obj\"", args);
    }

    [Fact]
    public void GoLinkArgs_IncluyeBanderasExtra()
    {
        var t = new BuildTarget { EntryPoint = "main", Libraries = "kernel32.dll", ExtraLinkFlags = "/console" };

        Assert.Contains("/console", t.BuildGoLinkArguments("a.obj"));
    }

    // ---------------- Propiedades derivadas ----------------

    [Fact]
    public void ProducesExecutable_DistingueLosDosModos()
    {
        Assert.True(new BuildTarget { Output = TargetOutput.Executable }.ProducesExecutable);
        Assert.False(new BuildTarget { Output = TargetOutput.ObjectOnly }.ProducesExecutable);
    }

    [Fact]
    public void DisplayName_SoloObjNoNombraEnlazador()
    {
        // Un target que no enlaza no debe anunciar un enlazador que no va a usar,
        // pero sí el ensamblador, que siempre se usa.
        var t = new BuildTarget { Name = "Prueba", Arch = TargetArch.Win32, Output = TargetOutput.ObjectOnly };

        Assert.Equal("Prueba  (NASM, 32 bits, solo .obj)", t.DisplayName);
    }

    [Fact]
    public void DisplayName_CuandoEnlazaNombraElEnlazador()
    {
        var t = new BuildTarget
        {
            Name = "Prueba",
            Arch = TargetArch.Win64,
            Output = TargetOutput.Executable,
            Linker = LinkerKind.GoLink
        };

        Assert.Equal("Prueba  (NASM, 64 bits, GoLink)", t.DisplayName);
    }

    [Fact]
    public void Clone_EsIndependienteDelOriginal()
    {
        var t = new BuildTarget { Name = "Original", Libraries = "kernel32.dll" };
        var c = t.Clone();

        c.Name = "Copia";
        c.Libraries = "otra.dll";

        Assert.Equal("Original", t.Name);
        Assert.Equal("kernel32.dll", t.Libraries);
    }

    [Fact]
    public void Defaults_TraenAlMenosUnWin64YUnWin32()
    {
        var d = BuildTarget.CreateDefaults();

        Assert.Contains(d, t => t.Arch == TargetArch.Win64);
        Assert.Contains(d, t => t.Arch == TargetArch.Win32);
        Assert.Contains(d, t => t.Output == TargetOutput.ObjectOnly);
    }
}

public class BuildConfigMigrationTests
{
    /// <summary>
    /// El settings.json que genera la versión anterior del editor.
    /// Los valores de EntryPoint y Libraries son A PROPÓSITO distintos de los de
    /// cualquier target predefinido: si fueran iguales, una migración rota (que
    /// descartara la configuración del usuario) pasaría desapercibida.
    /// </summary>
    private const string JsonFormatoViejo = """
    {
      "NasmPath": "C:\\Users\\Fabian\\NASM\\nasm.exe",
      "GoLinkPath": "C:\\Users\\Fabian\\NASM\\GoLink.exe",
      "Libraries": "kernel32.dll user32.dll gdi32.dll comctl32.dll ole32.dll",
      "EntryPoint": "mi_punto_de_entrada",
      "ProjectFolder": "C:\\Users\\Fabian\\NASM"
    }
    """;

    [Fact]
    public void Migracion_ConservaLasRutasDeHerramientas()
    {
        var c = BuildConfig.FromJson(JsonFormatoViejo);

        Assert.Equal(@"C:\Users\Fabian\NASM\nasm.exe", c.NasmPath);
        Assert.Equal(@"C:\Users\Fabian\NASM\GoLink.exe", c.GoLinkPath);
        Assert.Equal(@"C:\Users\Fabian\NASM", c.ProjectFolder);
    }

    [Fact]
    public void Migracion_ConvierteLaConfiguracionViejaEnElPrimerTarget()
    {
        var c = BuildConfig.FromJson(JsonFormatoViejo);

        // Lo que el usuario tenía configurado no se pierde: es el primer target.
        Assert.NotEmpty(c.Targets);
        Assert.Equal("mi_punto_de_entrada", c.Targets[0].EntryPoint);
        Assert.Equal("kernel32.dll user32.dll gdi32.dll comctl32.dll ole32.dll", c.Targets[0].Libraries);
    }

    [Fact]
    public void Migracion_ElTargetActivoEsElMigrado()
    {
        var c = BuildConfig.FromJson(JsonFormatoViejo);

        Assert.Equal(0, c.ActiveTargetIndex);
        Assert.Equal("mi_punto_de_entrada", c.ActiveTarget.EntryPoint);
        Assert.Equal("kernel32.dll user32.dll gdi32.dll comctl32.dll ole32.dll", c.ActiveTarget.Libraries);
    }

    [Fact]
    public void Migracion_TambienOfreceLosPredefinidos()
    {
        var c = BuildConfig.FromJson(JsonFormatoViejo);

        // Además del migrado, quedan disponibles otros (32 bits, solo .obj, etc.)
        Assert.True(c.Targets.Count > 1);
        Assert.Contains(c.Targets, t => t.Arch == TargetArch.Win32);
    }

    [Fact]
    public void Migracion_NuncaDescartaLaConfiguracionDelUsuario()
    {
        // Guarda directa contra el peor caso: que la migración tire lo que el usuario
        // tenía y lo reemplace por los predefinidos.
        var c = BuildConfig.FromJson(JsonFormatoViejo);

        Assert.Contains(c.Targets, t =>
            t.EntryPoint == "mi_punto_de_entrada" &&
            t.Libraries.Contains("comctl32.dll"));
    }

    [Fact]
    public void ArchivoVacio_CreaLosTargetsPredefinidos()
    {
        var c = BuildConfig.FromJson("{}");

        Assert.NotEmpty(c.Targets);
        Assert.Equal(0, c.ActiveTargetIndex);
    }

    [Fact]
    public void IndiceActivoFueraDeRango_SeCorrigeSolo()
    {
        var c = BuildConfig.FromJson("""
        { "ActiveTargetIndex": 99, "Targets": [ { "Name": "uno" } ] }
        """);

        Assert.Equal(0, c.ActiveTargetIndex);
        Assert.Equal("uno", c.ActiveTarget.Name);
    }

    [Fact]
    public void IndiceActivoNegativo_SeCorrigeSolo()
    {
        var c = BuildConfig.FromJson("""
        { "ActiveTargetIndex": -3, "Targets": [ { "Name": "uno" } ] }
        """);

        Assert.Equal(0, c.ActiveTargetIndex);
    }

    [Fact]
    public void FormatoNuevo_SeLeeSinTocarLosTargets()
    {
        var json = """
        {
          "NasmPath": "n.exe",
          "GoLinkPath": "g.exe",
          "ProjectFolder": "C:\\p",
          "ActiveTargetIndex": 1,
          "Targets": [
            { "Name": "A", "Arch": 1, "Output": 1, "EntryPoint": "main", "Libraries": "k.dll" },
            { "Name": "B", "Arch": 0, "Output": 0, "EntryPoint": "_main", "Libraries": "" }
          ]
        }
        """;

        var c = BuildConfig.FromJson(json);

        Assert.Equal(2, c.Targets.Count);
        Assert.Equal(1, c.ActiveTargetIndex);
        Assert.Equal("B", c.ActiveTarget.Name);
        Assert.Equal(TargetArch.Win32, c.ActiveTarget.Arch);
        Assert.Equal(TargetOutput.ObjectOnly, c.ActiveTarget.Output);
    }

    [Fact]
    public void IdaYVuelta_ConservaLosTargets()
    {
        var original = BuildConfig.FromJson(JsonFormatoViejo);
        original.ActiveTargetIndex = 1;

        var recuperado = BuildConfig.FromJson(original.ToJson());

        Assert.Equal(original.Targets.Count, recuperado.Targets.Count);
        Assert.Equal(1, recuperado.ActiveTargetIndex);
        Assert.Equal(original.Targets[1].Name, recuperado.Targets[1].Name);
    }

    [Fact]
    public void Clone_EsIndependiente()
    {
        var c = BuildConfig.FromJson(JsonFormatoViejo);
        var copia = c.Clone();

        copia.Targets[0].Name = "Cambiado";
        copia.NasmPath = "otro.exe";

        Assert.NotEqual("Cambiado", c.Targets[0].Name);
        Assert.NotEqual("otro.exe", c.NasmPath);
    }

    [Fact]
    public void Clone_NoPierdeLosRecientes()
    {
        // Al abrir el diálogo de targets se clona la configuración. Si el clon
        // no llevara el estado de UI, guardar desde ahí borraría los recientes
        // y la geometría de la ventana.
        var c = BuildConfig.FromJson("{}");
        c.Ui.AddRecent(@"C:\a\uno.asm");
        c.Ui.Window.Width = 1234;

        var copia = c.Clone();

        Assert.Contains(@"C:\a\uno.asm", copia.Ui.RecentFiles);
        Assert.Equal(1234, copia.Ui.Window.Width);
    }

    [Fact]
    public void IdaYVuelta_ConservaElEstadoDeUi()
    {
        var c = BuildConfig.FromJson("{}");
        c.Ui.AddRecent(@"C:\a\uno.asm");
        c.Ui.Theme = ModoTemaGuardado.Claro;
        c.Ui.Window.X = 55;
        c.Ui.Window.Saved = true;

        var vuelta = BuildConfig.FromJson(c.ToJson());

        Assert.Contains(@"C:\a\uno.asm", vuelta.Ui.RecentFiles);
        Assert.Equal(ModoTemaGuardado.Claro, vuelta.Ui.Theme);
        Assert.Equal(55, vuelta.Ui.Window.X);
        Assert.True(vuelta.Ui.Window.Saved);
    }

    [Fact]
    public void ConfiguracionVieja_TieneEstadoDeUiVacioPeroValido()
    {
        // Un settings.json anterior no tiene la sección Ui: no debe fallar.
        var c = BuildConfig.FromJson(JsonFormatoViejo);

        Assert.NotNull(c.Ui);
        Assert.Empty(c.Ui.RecentFiles);
        Assert.False(c.Ui.Window.Saved);
    }

    [Fact]
    public void JsonCorruptoNoRompe_LoManejaElLlamador()
    {
        // FromJson deja propagar el error de formato; BuildSettings.Load lo atrapa.
        Assert.ThrowsAny<Exception>(() => BuildConfig.FromJson("{ esto no es json"));
    }
}
