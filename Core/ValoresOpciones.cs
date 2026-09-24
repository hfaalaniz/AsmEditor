namespace AsmEditor.Core;

/// <summary>
/// Todas las opciones del editor como valores sueltos: lo que muestra y edita
/// la ventana Configuración → Opciones.
///
/// ⚠ POR QUÉ UNA COPIA PROPIA Y NO <see cref="BuildConfig.Clone"/>. El Clone
/// comparte <see cref="BuildConfig.Ui"/> por referencia (a propósito: ver el
/// comentario ahí). Si la ventana editara el tema o «Al iniciar» sobre ese
/// clon, Cancelar los cambiaría igual. Acá cada valor se copia al abrir
/// (<see cref="Leer"/>) y se escribe en la configuración solo al aceptar
/// (<see cref="Aplicar"/>).
///
/// ⚠ AGREGAR UNA OPCIÓN = una propiedad acá, su línea en <see cref="Leer"/> y
/// en <see cref="Aplicar"/>, y el control en su página. Si falta una de las
/// dos líneas, la prueba ValoresOpcionesTests.TodaPropiedad_VaYVuelve falla:
/// la recorre por reflexión, así que cubre también las que se agreguen.
/// </summary>
public sealed class ValoresOpciones
{
    // ---- Entorno ----

    public ModoTemaGuardado Tema { get; set; }

    public AlIniciar AlIniciar { get; set; }

    /// <summary>La carpeta de trabajo (BuildConfig.ProjectFolder).</summary>
    public string CarpetaProyecto { get; set; } = "";

    // ---- Herramientas ----

    public string RutaNasm { get; set; } = "";

    public string RutaGoLink { get; set; } = "";

    /// <summary>Vacío = automático.</summary>
    public string ToolchainMsvc64 { get; set; } = "";

    /// <summary>Vacío = automático.</summary>
    public string ToolchainMsvc32 { get; set; } = "";

    /// <summary>Vacío = automático.</summary>
    public string SdkLib64 { get; set; } = "";

    /// <summary>Vacío = automático.</summary>
    public string SdkLib32 { get; set; } = "";

    /// <summary>Los valores actuales de la configuración, copiados.</summary>
    public static ValoresOpciones Leer(BuildConfig cfg) => new()
    {
        Tema = cfg.Ui.Theme,
        AlIniciar = cfg.Ui.AlIniciar,
        CarpetaProyecto = cfg.ProjectFolder,

        RutaNasm = cfg.NasmPath,
        RutaGoLink = cfg.GoLinkPath,
        ToolchainMsvc64 = cfg.MsvcToolchainDir64,
        ToolchainMsvc32 = cfg.MsvcToolchainDir32,
        SdkLib64 = cfg.SdkLibPath64,
        SdkLib32 = cfg.SdkLibPath32
    };

    /// <summary>Escribe los valores en la configuración (al aceptar).</summary>
    public void Aplicar(BuildConfig cfg)
    {
        cfg.Ui.Theme = Tema;
        cfg.Ui.AlIniciar = AlIniciar;
        cfg.ProjectFolder = CarpetaProyecto;

        cfg.NasmPath = RutaNasm;
        cfg.GoLinkPath = RutaGoLink;
        cfg.MsvcToolchainDir64 = ToolchainMsvc64;
        cfg.MsvcToolchainDir32 = ToolchainMsvc32;
        cfg.SdkLibPath64 = SdkLib64;
        cfg.SdkLibPath32 = SdkLib32;

        // Un valor fuera del enum (un settings.json editado a mano) vuelve al
        // de fábrica, igual que al leer el archivo.
        cfg.EnsureValid();
    }
}
