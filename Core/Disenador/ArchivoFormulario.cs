using System.Text.Json;
using System.Text.Json.Serialization;

namespace AsmEditor.Core.Disenador;

/// <summary>
/// Lee y escribe el .asmform, y coordina la generación de los archivos.
///
/// ⚠ EL .asmform ES LA ÚNICA FUENTE DE VERDAD. El .inc se deriva de él y se
/// pisa sin preguntar; el .asm se deriva de él UNA VEZ y no se pisa nunca.
/// NUNCA se lee el .asm para reconstruir el diseño: parsear ensamblador para
/// adivinar dónde estaba un botón es frágil y silenciosamente equivocado.
/// </summary>
public static class ArchivoFormulario
{
    public const string Extension = ".asmform";

    /// <summary>
    /// Versión del formato. Se guarda en el archivo para poder migrarlo si el
    /// modelo cambia; un archivo sin versión es de la 1.
    /// </summary>
    public const int VersionFormato = 1;

    private static readonly JsonSerializerOptions Opciones = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() },
        // Se guarda con los acentos tal cual, no como á: el archivo se
        // lee a mano cuando hay que diagnosticar algo.
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    /// <summary>Lo que se serializa: el formulario más la versión del formato.</summary>
    private sealed class Envoltorio
    {
        public int Version { get; set; } = VersionFormato;
        public FormularioDisenado? Formulario { get; set; }
    }

    public static string Serializar(FormularioDisenado f) =>
        JsonSerializer.Serialize(new Envoltorio { Formulario = f }, Opciones);

    /// <summary>
    /// Reconstruye el formulario desde el texto del .asmform.
    /// Lanza <see cref="InvalidDataException"/> si el archivo no sirve, con un
    /// mensaje que se le pueda mostrar al usuario.
    /// </summary>
    public static FormularioDisenado Deserializar(string json)
    {
        Envoltorio? env;

        try
        {
            env = JsonSerializer.Deserialize<Envoltorio>(json, Opciones);
        }
        catch (JsonException ex)
        {
            throw new InvalidDataException($"El archivo de formulario está dañado: {ex.Message}", ex);
        }

        if (env?.Formulario is null)
        {
            throw new InvalidDataException("El archivo de formulario no tiene datos de formulario.");
        }

        if (env.Version > VersionFormato)
        {
            throw new InvalidDataException(
                $"El archivo es de una versión más nueva del diseñador (formato {env.Version}, " +
                $"este editor entiende hasta el {VersionFormato}). Actualizá el editor para abrirlo.");
        }

        // Un archivo editado a mano puede traer controles sin Id.
        env.Formulario.AsignarIdsFaltantes();

        return env.Formulario;
    }

    public static void Guardar(string ruta, FormularioDisenado f) =>
        File.WriteAllText(ruta, Serializar(f));

    public static FormularioDisenado Cargar(string ruta) =>
        Deserializar(File.ReadAllText(ruta));

    // ------------------------------------------------------------------
    // Rutas de los archivos derivados
    // ------------------------------------------------------------------

    /// <summary>El .inc que se regenera. Queda al lado del .asmform.</summary>
    public static string RutaInclude(string rutaAsmform) =>
        Path.ChangeExtension(rutaAsmform, ".inc");

    /// <summary>El .asm del usuario. Queda al lado del .asmform.</summary>
    public static string RutaAsm(string rutaAsmform) =>
        Path.ChangeExtension(rutaAsmform, ".asm");

    // ------------------------------------------------------------------
    // Generación
    // ------------------------------------------------------------------

    /// <summary>Qué hizo <see cref="GenerarArchivos"/>, para informarlo.</summary>
    public sealed record ResultadoGeneracion(
        string RutaInclude,
        string RutaAsm,
        bool SeCreoElAsm,
        string ManejadoresFaltantes)
    {
        /// <summary>
        /// True si hay controles nuevos cuyo manejador no está en el .asm del
        /// usuario. El texto para pegar está en <see cref="ManejadoresFaltantes"/>.
        /// </summary>
        public bool HayManejadoresPendientes => !string.IsNullOrWhiteSpace(ManejadoresFaltantes);
    }

    /// <summary>
    /// Escribe el .inc (siempre) y el .asm (solo si no existe).
    ///
    /// ⚠ VALIDA ANTES DE ESCRIBIR NADA: si el formulario tiene errores lanza y
    /// no toca el disco, para no dejar un .inc a medias que después no ensambla.
    /// </summary>
    public static ResultadoGeneracion GenerarArchivos(string rutaAsmform, FormularioDisenado f)
    {
        var errores = f.Validar();

        if (errores.Count > 0)
        {
            throw new InvalidOperationException(
                "El formulario tiene problemas que impiden generar el código:" +
                Environment.NewLine + string.Join(Environment.NewLine, errores.Select(e => "  • " + e)));
        }

        f.AsignarIdsFaltantes();

        var rutaInc = RutaInclude(rutaAsmform);
        var rutaAsm = RutaAsm(rutaAsmform);

        File.WriteAllText(rutaInc, GeneradorAsm.GenerarInclude(f));

        bool seCreo = false;
        string faltantes = "";

        // ⚠ EL %include LLEVA EL NOMBRE DEL ARCHIVO, NO EL DEL FORMULARIO. Los
        // archivos se llaman como el .asmform que eligió el usuario, y ese
        // nombre no tiene por qué coincidir con el del formulario: un
        // formulario "Formulario1" guardado como "Ciclo.asmform" produce
        // "Ciclo.inc", y un %include deducido del formulario apuntaría a un
        // archivo que no existe.
        var nombreInclude = Path.GetFileNameWithoutExtension(rutaInc);

        if (!File.Exists(rutaAsm))
        {
            File.WriteAllText(rutaAsm, GeneradorEsqueleto.Generar(f, nombreInclude));
            seCreo = true;
        }
        else
        {
            // El .asm ya existe y es del usuario: no se toca. Solo se calcula
            // qué manejadores le faltan para ofrecérselos.
            var actual = File.ReadAllText(rutaAsm);
            faltantes = GeneradorEsqueleto.GenerarManejadoresFaltantes(f, actual);
        }

        return new ResultadoGeneracion(rutaInc, rutaAsm, seCreo, faltantes);
    }
}
