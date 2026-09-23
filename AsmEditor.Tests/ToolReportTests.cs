using AsmEditor.Core;

namespace AsmEditor.Tests;

/// <summary>
/// El informe de «Verificar herramientas».
///
/// Las tres reglas de acá nacieron de un bug real: el informe hablaba siempre
/// de NASM y de GoLink, así que con un target de MASM+MSVC decía que faltaba
/// GoLink —que esa cadena no usa— y mostraba la línea de NASM, con un «-f win64»
/// que ml64.exe rechaza.
/// </summary>
public class ToolReportTests
{
    private static BuildTarget MasmMsvc64() => new()
    {
        Name = "MASM 64",
        Assembler = AssemblerKind.Masm,
        Arch = TargetArch.Win64,
        Linker = LinkerKind.MsvcLink,
        EntryPoint = "main",
        Libraries = "kernel32.dll user32.dll"
    };

    private static BuildTarget NasmGoLink64() => new()
    {
        Name = "NASM 64",
        Assembler = AssemblerKind.Nasm,
        Arch = TargetArch.Win64,
        Linker = LinkerKind.GoLink,
        EntryPoint = "main",
        Libraries = "kernel32.dll user32.dll"
    };

    private static ToolReport.Rutas TodoHallado() => new()
    {
        Ensamblador = @"C:\x\ml64.exe",
        Enlazador = @"C:\x\link.exe",
        SdkLib = @"C:\sdk\um\x64"
    };

    private static ToolReport.Rutas NadaHallado() => new();

    // ── Regla 1: nombra el ensamblador del target, no «NASM» siempre ──

    [Fact]
    public void ConMasm_NoNombraNasm()
    {
        var texto = ToolReport.Construir(MasmMsvc64(), TodoHallado());

        Assert.Contains("MASM (ml64.exe)", texto);
        Assert.DoesNotContain("NASM", texto);
    }

    [Fact]
    public void ConMasm32_NombraMlNoMl64()
    {
        var t = MasmMsvc64();
        t.Arch = TargetArch.Win32;

        // La ruta del fixture es la del hallazgo y puede decir cualquier cosa:
        // lo que se afirma es el NOMBRE con que el informe llama a la herramienta.
        var texto = ToolReport.Construir(t, new ToolReport.Rutas
        {
            Ensamblador = @"C:\x\ml.exe",
            Enlazador = @"C:\x\link.exe",
            SdkLib = @"C:\sdk\um\x86"
        });

        Assert.Contains("MASM (ml.exe)", texto);
        Assert.DoesNotContain("MASM (ml64.exe)", texto);
    }

    [Fact]
    public void ConNasm_NombraNasm()
    {
        var texto = ToolReport.Construir(NasmGoLink64(), TodoHallado());

        Assert.Contains("NASM", texto);
        Assert.DoesNotContain("MASM", texto);
    }

    // ── Regla 2: la línea de argumentos es la del ensamblador correcto ──

    [Fact]
    public void ConMasm_LaLineaNoLlevaElFormatoDeNasm()
    {
        // ⚠ Este era el bug: BuildNasmArguments en vez de BuildAssemblerArguments.
        var texto = ToolReport.Construir(MasmMsvc64(), TodoHallado());

        Assert.DoesNotContain("-f win64", texto);
        Assert.Contains("/c", texto);
    }

    [Fact]
    public void ConNasm_LaLineaSiLlevaElFormato()
    {
        var texto = ToolReport.Construir(NasmGoLink64(), TodoHallado());

        Assert.Contains("-f win64", texto);
    }

    // ── Regla 3: nombra el enlazador del target, no «GoLink» siempre ──

    [Fact]
    public void ConMsvc_NoNombraGoLink()
    {
        var texto = ToolReport.Construir(MasmMsvc64(), TodoHallado());

        Assert.Contains("MSVC (link.exe)", texto);
        Assert.DoesNotContain("GoLink", texto);
    }

    [Fact]
    public void ConGoLink_NoNombraMsvc()
    {
        // Con GoLink la ruta hallada es la de GoLink.exe, no la de link.exe.
        var texto = ToolReport.Construir(NasmGoLink64(), new ToolReport.Rutas
        {
            Ensamblador = @"C:\x\nasm.exe",
            Enlazador = @"C:\x\GoLink.exe"
        });

        Assert.Contains("GoLink", texto);
        Assert.DoesNotContain("MSVC (link.exe)", texto);
    }

    [Fact]
    public void ConGoLink_NoHablaDelSdk()
    {
        // GoLink enlaza contra los .dll: las librerías del SDK no le hacen falta.
        var texto = ToolReport.Construir(NasmGoLink64(), TodoHallado());

        Assert.DoesNotContain("SDK", texto);
    }

    [Fact]
    public void ConMsvc_SiHablaDelSdk()
    {
        var texto = ToolReport.Construir(MasmMsvc64(), TodoHallado());

        Assert.Contains("SDK", texto);
    }

    // ── Hallado / faltante ──────────────────────────────────────────────

    [Fact]
    public void LoHalladoVaConOkYSuRuta()
    {
        var texto = ToolReport.Construir(MasmMsvc64(), TodoHallado());

        // La ruta va en su propia línea, después de decir de dónde salió.
        Assert.Contains("OK  - MASM (ml64.exe) encontrado", texto);
        Assert.Contains(@"C:\x\ml64.exe", texto);
        Assert.Contains("OK  - MSVC (link.exe) encontrado", texto);
        Assert.Contains(@"C:\x\link.exe", texto);
    }

    // ── De dónde salió cada herramienta ─────────────────────────────────

    [Fact]
    public void DiceQueLaEligioElEditorCuandoEsAutomatica()
    {
        var texto = ToolReport.Construir(MasmMsvc64(), TodoHallado());

        Assert.Contains("detectado automáticamente", texto);
    }

    [Fact]
    public void DiceQueSaleDeLaConfiguracionCuandoSeEligio()
    {
        var texto = ToolReport.Construir(MasmMsvc64(), new ToolReport.Rutas
        {
            Ensamblador = @"C:\x\ml64.exe",
            Enlazador = @"C:\x\link.exe",
            SdkLib = @"C:\sdk\um\x64",
            FuenteEnsamblador = ToolSource.Configuracion,
            FuenteEnlazador = ToolSource.Configuracion,
            FuenteSdk = ToolSource.Configuracion
        });

        Assert.Contains("elegido en Configuración", texto);
        Assert.DoesNotContain("detectado automáticamente", texto);
    }

    [Fact]
    public void DiceQueLoFijaElTargetCuandoElTargetLoFija()
    {
        var texto = ToolReport.Construir(MasmMsvc64(), new ToolReport.Rutas
        {
            Ensamblador = @"C:\x\ml64.exe",
            Enlazador = @"C:\x\link.exe",
            SdkLib = @"C:\sdk\um\x64",
            FuenteEnsamblador = ToolSource.Target,
            FuenteEnlazador = ToolSource.Target,
            FuenteSdk = ToolSource.Target
        });

        Assert.Contains("fijado en el target", texto);
    }

    // ── Cuántas alternativas hay ────────────────────────────────────────

    [Fact]
    public void ConVariosToolchainsLoInforma()
    {
        var texto = ToolReport.Construir(MasmMsvc64(), new ToolReport.Rutas
        {
            Ensamblador = @"C:\x\ml64.exe",
            Enlazador = @"C:\x\link.exe",
            SdkLib = @"C:\sdk\um\x64",
            ToolchainsDisponibles = 10,
            SdksDisponibles = 4
        });

        Assert.Contains("10 juegos de herramientas", texto);
        Assert.Contains("4 versiones del SDK", texto);
    }

    [Fact]
    public void ConUnSoloToolchainNoMolestaConElDato()
    {
        // Con una sola instalación el dato no aporta nada.
        var texto = ToolReport.Construir(MasmMsvc64(), new ToolReport.Rutas
        {
            Ensamblador = @"C:\x\ml64.exe",
            Enlazador = @"C:\x\link.exe",
            SdkLib = @"C:\sdk\um\x64",
            ToolchainsDisponibles = 1,
            SdksDisponibles = 1
        });

        Assert.DoesNotContain("juegos de herramientas", texto);
    }

    [Fact]
    public void LoFaltanteVaConMal()
    {
        var texto = ToolReport.Construir(MasmMsvc64(), NadaHallado());

        Assert.Contains("MAL - MASM (ml64.exe) NO encontrado", texto);
        Assert.Contains("MAL - MSVC (link.exe) NO encontrado", texto);
        Assert.Contains("MAL - Librerías del SDK NO encontradas", texto);
        Assert.DoesNotContain("OK  -", texto);
    }

    [Fact]
    public void FaltarElSdkAvisaDelLnk1181()
    {
        var texto = ToolReport.Construir(MasmMsvc64(), new ToolReport.Rutas
        {
            Ensamblador = @"C:\x\ml64.exe",
            Enlazador = @"C:\x\link.exe"
        });

        Assert.Contains("LNK1181", texto);
    }

    // ── Solo .obj ───────────────────────────────────────────────────────

    [Fact]
    public void SoloObj_NoHablaDeNingunEnlazador()
    {
        var t = MasmMsvc64();
        t.Output = TargetOutput.ObjectOnly;

        // Solo el ensamblador hallado: si el informe nombrara un enlazador
        // igual, la afirmación de abajo no lo distinguiría de la ruta del fixture.
        var texto = ToolReport.Construir(t, new ToolReport.Rutas
        {
            Ensamblador = @"C:\x\ml64.exe"
        });

        Assert.Contains("solo genera .obj", texto);
        Assert.DoesNotContain("GoLink", texto);
        Assert.DoesNotContain("link.exe", texto);
        Assert.DoesNotContain("SDK", texto);
    }

    [Fact]
    public void SoloObj_IgualNombraElEnsamblador()
    {
        var t = MasmMsvc64();
        t.Output = TargetOutput.ObjectOnly;

        var texto = ToolReport.Construir(t, TodoHallado());

        Assert.Contains("MASM (ml64.exe)", texto);
    }

    // ── El target siempre encabeza ──────────────────────────────────────

    [Fact]
    public void EncabezaConElTargetActivo()
    {
        var texto = ToolReport.Construir(MasmMsvc64(), TodoHallado());

        Assert.StartsWith("Target activo: MASM 64", texto);
    }

    // ── La regla del /ENTRY también se ve en el informe ──────────────────

    [Fact]
    public void ConMasm64_LaLineaDeEnlazadoMuestraEntry()
    {
        var texto = ToolReport.Construir(MasmMsvc64(), TodoHallado());

        Assert.Contains("/ENTRY:main", texto);
    }

    [Fact]
    public void ConMasm32_LaLineaDeEnlazadoNoMuestraEntry()
    {
        // El .obj de 32 bits ya lo declara decorado; pasarlo da LNK4258.
        var t = MasmMsvc64();
        t.Arch = TargetArch.Win32;

        var texto = ToolReport.Construir(t, TodoHallado());

        Assert.DoesNotContain("/ENTRY", texto);
    }
}
