namespace AsmEditor;

/// <summary>
/// LOS DATOS DEL PROGRAMA Y DE QUIÉN LO HACE, EN UN SOLO LUGAR.
///
/// Aparecen en el splash, en el «Acerca de» y en la barra de título. Estaban
/// para escribirse tres veces y desincronizarse: la versión que se muestra al
/// arrancar no puede decir una cosa y la del «Acerca de» otra.
///
/// ⚠ AL SUBIR LA VERSIÓN hay que tocar TAMBIÉN el .csproj: las propiedades
/// Version / FileVersion de ahí son las que ve el Explorador en las propiedades
/// del .exe, y no hay forma de que lean esta clase.
/// </summary>
public static class InfoApp
{
    public const string Numero = "1.0";

    /// <summary>Para mostrar: «Versión 1.0».</summary>
    public const string Texto = "Versión " + Numero;

    public const string NombreApp = "Editor ASM";

    /// <summary>Qué es, en una línea.</summary>
    public const string Descripcion = "Entorno de desarrollo para ensamblador x86/x64";

    /// <summary>Las herramientas que orquesta.</summary>
    public const string Herramientas = "NASM  ·  GoLink  ·  MSVC link";

    // ── Autor ────────────────────────────────────────────────────────────
    public const string Autor = "Fabián Alaniz";
    public const string Movil = "351-284-8802";
    public const string Sitio = "https://developers-soft.pages.dev";
    public const string SitioCorto = "developers-soft.pages.dev";
    public const string Localidad = "Villa Santa Cruz del Lago";
    public const string Provincia = "Córdoba";
    public const string Pais = "Argentina";

    /// <summary>La ubicación en una línea, como se muestra.</summary>
    public const string Ubicacion = Localidad + " · " + Provincia + " · " + Pais;

    public static string Copyright => $"© {DateTime.Now.Year} {Autor}";
}
