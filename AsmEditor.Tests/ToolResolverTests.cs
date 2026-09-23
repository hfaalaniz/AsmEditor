using AsmEditor.Core;

namespace AsmEditor.Tests;

/// <summary>
/// La precedencia de las herramientas: target &gt; configuración global &gt; automático.
///
/// Es la regla que antes estaba repartida en tres lugares con tres versiones
/// distintas. Todo lo de acá usa rutas inventadas: no depende de lo instalado.
/// </summary>
public class ToolResolverTests
{
    private static BuildConfig Config() => new()
    {
        NasmPath = @"C:\cfg\nasm.exe",
        GoLinkPath = @"C:\cfg\GoLink.exe",
        ProjectFolder = @"C:\proyecto"
    };

    private static BuildTarget Masm64() => new()
    {
        Assembler = AssemblerKind.Masm,
        Arch = TargetArch.Win64,
        Linker = LinkerKind.MsvcLink
    };

    private static BuildTarget Nasm64() => new()
    {
        Assembler = AssemblerKind.Nasm,
        Arch = TargetArch.Win64,
        Linker = LinkerKind.GoLink
    };

    // ── El target manda sobre todo ──────────────────────────────────────

    [Fact]
    public void Ensamblador_LoDelTargetPisaLoGlobal()
    {
        var cfg = Config();
        cfg.MsvcToolchainDir64 = @"C:\global\bin";

        var t = Masm64();
        t.MasmPath = @"C:\delTarget\ml64.exe";

        Assert.Equal(@"C:\delTarget\ml64.exe", ToolResolver.Assembler(t, cfg));
        Assert.Equal(ToolSource.Target, ToolResolver.SourceOfAssembler(t, cfg));
    }

    [Fact]
    public void Enlazador_LoDelTargetPisaLoGlobal()
    {
        var cfg = Config();
        cfg.MsvcToolchainDir64 = @"C:\global\bin";

        var t = Masm64();
        t.MsvcLinkerPath = @"C:\delTarget\link.exe";

        Assert.Equal(@"C:\delTarget\link.exe", ToolResolver.Linker(t, cfg));
        Assert.Equal(ToolSource.Target, ToolResolver.SourceOfLinker(t, cfg));
    }

    [Fact]
    public void Sdk_LoDelTargetPisaLoGlobal()
    {
        var cfg = Config();
        cfg.SdkLibPath64 = @"C:\global\um\x64";

        var t = Masm64();
        t.SdkLibPath = @"C:\delTarget\um\x64";

        Assert.Equal(@"C:\delTarget\um\x64", ToolResolver.SdkLib(t, cfg));
        Assert.Equal(ToolSource.Target, ToolResolver.SourceOfSdk(t, cfg));
    }

    // ── Lo global manda sobre la autodetección ──────────────────────────

    [Fact]
    public void Ensamblador_LoGlobalSeUsaCuandoElTargetNoFijaNada()
    {
        var cfg = Config();
        cfg.MsvcToolchainDir64 = @"C:\global\bin";

        Assert.Equal(@"C:\global\bin\ml64.exe", ToolResolver.Assembler(Masm64(), cfg));
    }

    [Fact]
    public void Enlazador_LoGlobalSeUsaCuandoElTargetNoFijaNada()
    {
        var cfg = Config();
        cfg.MsvcToolchainDir64 = @"C:\global\bin";

        Assert.Equal(@"C:\global\bin\link.exe", ToolResolver.Linker(Masm64(), cfg));
    }

    [Fact]
    public void ElMasmYElLinkGlobalesSalenDeLaMismaCarpeta()
    {
        // ⚠ APAREADOS: no se mezclan versiones de MSVC.
        var cfg = Config();
        cfg.MsvcToolchainDir64 = @"C:\global\bin";

        var t = Masm64();

        Assert.Equal(
            Path.GetDirectoryName(ToolResolver.Assembler(t, cfg)),
            Path.GetDirectoryName(ToolResolver.Linker(t, cfg)));
    }

    [Fact]
    public void Sdk_LoGlobalSeUsaCuandoElTargetNoFijaNada()
    {
        var cfg = Config();
        cfg.SdkLibPath64 = @"C:\global\um\x64";

        Assert.Equal(@"C:\global\um\x64", ToolResolver.SdkLib(Masm64(), cfg));
        Assert.Equal(ToolSource.Configuracion, ToolResolver.SourceOfSdk(Masm64(), cfg));
    }

    // ── Las dos arquitecturas son juegos distintos ──────────────────────

    [Fact]
    public void LasArquitecturasNoSePisan()
    {
        var cfg = Config();
        cfg.MsvcToolchainDir64 = @"C:\x64\bin";
        cfg.MsvcToolchainDir32 = @"C:\x86\bin";

        var t64 = Masm64();
        var t32 = new BuildTarget
        {
            Assembler = AssemblerKind.Masm,
            Arch = TargetArch.Win32,
            Linker = LinkerKind.MsvcLink
        };

        Assert.Equal(@"C:\x64\bin\ml64.exe", ToolResolver.Assembler(t64, cfg));
        Assert.Equal(@"C:\x86\bin\ml.exe", ToolResolver.Assembler(t32, cfg));
    }

    [Fact]
    public void ElNombreDelEnsambladorDependeDeLaArquitectura()
    {
        Assert.Equal("ml64.exe", ToolResolver.MasmExe(TargetArch.Win64));
        Assert.Equal("ml.exe", ToolResolver.MasmExe(TargetArch.Win32));
    }

    // ── NASM y GoLink no participan de los toolchains ───────────────────

    [Fact]
    public void ConNasm_SeUsaLaRutaDeLaConfiguracion()
    {
        var cfg = Config();
        cfg.MsvcToolchainDir64 = @"C:\global\bin";   // no le incumbe

        Assert.Equal(@"C:\cfg\nasm.exe", ToolResolver.Assembler(Nasm64(), cfg));
    }

    [Fact]
    public void ConGoLink_SeUsaLaRutaDeLaConfiguracion()
    {
        var cfg = Config();
        cfg.MsvcToolchainDir64 = @"C:\global\bin";

        Assert.Equal(@"C:\cfg\GoLink.exe", ToolResolver.Linker(Nasm64(), cfg));
    }

    [Fact]
    public void ConGoLink_NoHaySdk()
    {
        // GoLink enlaza contra los .dll: las librerías del SDK no le hacen falta.
        var cfg = Config();
        cfg.SdkLibPath64 = @"C:\global\um\x64";

        Assert.Null(ToolResolver.SdkLib(Nasm64(), cfg));
    }

    // ── Fijar y limpiar la preferencia global ───────────────────────────

    [Fact]
    public void SetToolchainDir_GuardaPorArquitectura()
    {
        var cfg = Config();

        ToolResolver.SetToolchainDir(TargetArch.Win64, cfg, @"C:\a\bin");
        ToolResolver.SetToolchainDir(TargetArch.Win32, cfg, @"C:\b\bin");

        Assert.Equal(@"C:\a\bin", cfg.MsvcToolchainDir64);
        Assert.Equal(@"C:\b\bin", cfg.MsvcToolchainDir32);
    }

    [Fact]
    public void SetToolchainDir_ConNullVuelveAlAutomatico()
    {
        var cfg = Config();
        cfg.MsvcToolchainDir64 = @"C:\a\bin";

        ToolResolver.SetToolchainDir(TargetArch.Win64, cfg, null);

        Assert.Equal("", cfg.MsvcToolchainDir64);
        Assert.Equal(ToolSource.Automatico, ToolResolver.SourceOfAssembler(Masm64(), cfg));
    }

    [Fact]
    public void SetSdkLibPath_GuardaPorArquitectura()
    {
        var cfg = Config();

        ToolResolver.SetSdkLibPath(TargetArch.Win64, cfg, @"C:\a\um\x64");
        ToolResolver.SetSdkLibPath(TargetArch.Win32, cfg, @"C:\a\um\x86");

        Assert.Equal(@"C:\a\um\x64", cfg.SdkLibPath64);
        Assert.Equal(@"C:\a\um\x86", cfg.SdkLibPath32);
    }

    // ── Sin nada configurado, cae en la autodetección ───────────────────

    [Fact]
    public void SinNadaConfigurado_LaFuenteEsAutomatica()
    {
        var cfg = Config();

        Assert.Equal(ToolSource.Automatico, ToolResolver.SourceOfAssembler(Masm64(), cfg));
        Assert.Equal(ToolSource.Automatico, ToolResolver.SourceOfLinker(Masm64(), cfg));
        Assert.Equal(ToolSource.Automatico, ToolResolver.SourceOfSdk(Masm64(), cfg));
    }

    [Fact]
    public void SinNadaConfigurado_CoincideConLaAutodeteccion()
    {
        var cfg = Config();
        var t = Masm64();

        Assert.Equal(
            LinkerLocator.FindMasm(TargetArch.Win64, cfg.ProjectFolder),
            ToolResolver.Assembler(t, cfg));

        Assert.Equal(
            LinkerLocator.FindSdkLibPath(TargetArch.Win64),
            ToolResolver.SdkLib(t, cfg));
    }
}

/// <summary>
/// Que la configuración nueva sobreviva al guardado y al clonado.
/// </summary>
public class BuildConfigToolchainTests
{
    [Fact]
    public void Clone_CopiaElToolchainYElSdk()
    {
        // ⚠ Lo que falte en Clone se pierde en silencio al guardar desde un diálogo.
        var cfg = new BuildConfig
        {
            MsvcToolchainDir64 = @"C:\a\bin",
            MsvcToolchainDir32 = @"C:\b\bin",
            SdkLibPath64 = @"C:\sdk\x64",
            SdkLibPath32 = @"C:\sdk\x86"
        };

        var c = cfg.Clone();

        Assert.Equal(@"C:\a\bin", c.MsvcToolchainDir64);
        Assert.Equal(@"C:\b\bin", c.MsvcToolchainDir32);
        Assert.Equal(@"C:\sdk\x64", c.SdkLibPath64);
        Assert.Equal(@"C:\sdk\x86", c.SdkLibPath32);
    }

    [Fact]
    public void SobreviveAlJson()
    {
        var cfg = new BuildConfig
        {
            MsvcToolchainDir64 = @"C:\a\bin",
            SdkLibPath64 = @"C:\sdk\x64"
        };

        var vuelta = BuildConfig.FromJson(cfg.ToJson());

        Assert.Equal(@"C:\a\bin", vuelta.MsvcToolchainDir64);
        Assert.Equal(@"C:\sdk\x64", vuelta.SdkLibPath64);
    }

    [Fact]
    public void UnArchivoViejoSinEstosCamposSigueAbriendo()
    {
        // Compatibilidad: los settings.json que ya existen no los tienen.
        var json = """
        {
          "NasmPath": "C:\\x\\nasm.exe",
          "GoLinkPath": "C:\\x\\GoLink.exe",
          "ProjectFolder": "C:\\x",
          "Targets": [],
          "ActiveTargetIndex": 0
        }
        """;

        var cfg = BuildConfig.FromJson(json);

        Assert.Equal("", cfg.MsvcToolchainDir64);
        Assert.Equal("", cfg.SdkLibPath64);
        Assert.NotEmpty(cfg.Targets);   // EnsureValid puso los predefinidos
    }
}
