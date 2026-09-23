using System.Text.Json;
using System.Text.Json.Serialization;

namespace AsmEditor.Core.Proyecto;

/// <summary>
/// Lee y escribe el .asmproj.
///
/// Mismo formato y mismas reglas que <see cref="Disenador.ArchivoFormulario"/>:
/// JSON legible, con versión adentro para poder migrarlo, y errores con un
/// mensaje que se le pueda mostrar al usuario.
/// </summary>
public static class ArchivoProyecto
{
    public const string Extension = ".asmproj";

    /// <summary>
    /// Versión del formato. Un archivo más nuevo que esto no se abre: es mejor
    /// decir «actualizá el editor» que cargarlo a medias y perder lo que el
    /// editor viejo no entendió al volver a guardar.
    /// </summary>
    public const int VersionFormato = 1;

    private static readonly JsonSerializerOptions Opciones = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() },
        // Los acentos van tal cual, no como á: el .asmproj se lee y se
        // edita a mano cuando hay que diagnosticar algo.
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    /// <summary>Lo que se serializa: el proyecto más la versión del formato.</summary>
    private sealed class Envoltorio
    {
        public int Version { get; set; } = VersionFormato;
        public ProyectoAsm? Proyecto { get; set; }
    }

    public static string Serializar(ProyectoAsm p) =>
        JsonSerializer.Serialize(new Envoltorio { Proyecto = p }, Opciones);

    /// <summary>
    /// Reconstruye el proyecto desde el texto del .asmproj.
    ///
    /// <paramref name="rutaArchivo"/> se necesita para resolver las rutas
    /// relativas: sin ella el proyecto no sabe dónde está y ningún archivo de
    /// su lista se puede encontrar.
    /// </summary>
    public static ProyectoAsm Deserializar(string json, string? rutaArchivo = null)
    {
        Envoltorio? env;

        try
        {
            env = JsonSerializer.Deserialize<Envoltorio>(json, Opciones);
        }
        catch (JsonException ex)
        {
            throw new InvalidDataException($"El archivo de proyecto está dañado: {ex.Message}", ex);
        }

        if (env?.Proyecto is null)
        {
            throw new InvalidDataException("El archivo de proyecto no tiene datos de proyecto.");
        }

        if (env.Version > VersionFormato)
        {
            throw new InvalidDataException(
                $"El proyecto es de una versión más nueva del editor (formato {env.Version}, " +
                $"este editor entiende hasta el {VersionFormato}). Actualizá el editor para abrirlo.");
        }

        var p = env.Proyecto;
        p.RutaArchivo = rutaArchivo;
        p.Normalizar();

        return p;
    }

    public static void Guardar(string ruta, ProyectoAsm p)
    {
        // La ruta se fija ANTES de serializar: las rutas relativas de la lista
        // se calculan contra ella, y con "Guardar como" a otra carpeta habría
        // que recalcularlas todas contra la vieja.
        p.RutaArchivo = ruta;

        File.WriteAllText(ruta, Serializar(p));
    }

    public static ProyectoAsm Cargar(string ruta) =>
        Deserializar(File.ReadAllText(ruta), ruta);

    /// <summary>
    /// Crea un proyecto nuevo en disco con los archivos dados.
    ///
    /// El primer .asm de la lista queda como principal: es lo que se quiere en
    /// el 99% de los casos y evita que el proyecto nazca sin nada que compilar.
    /// </summary>
    public static ProyectoAsm Crear(string rutaArchivo, string nombre, IEnumerable<string>? archivos = null)
    {
        var p = ProyectoAsm.Nuevo(nombre, rutaArchivo);

        if (archivos is not null)
        {
            foreach (var a in archivos) p.Agregar(a);
        }

        if (string.IsNullOrWhiteSpace(p.ArchivoPrincipal))
        {
            var primerAsm = p.ListarArchivos()
                .FirstOrDefault(a => a.Extension == ".asm");

            if (primerAsm?.Absoluta is not null)
            {
                p.MarcarComoPrincipal(primerAsm.Absoluta);
            }
        }

        Guardar(rutaArchivo, p);

        return p;
    }

    /// <summary>
    /// Busca un .asmproj en la carpeta de un archivo, y de ahí hacia arriba.
    ///
    /// Sirve para abrir el proyecto solo cuando se abre un .asm que pertenece a
    /// uno. Se limita a unos pocos niveles: subir hasta la raíz del disco
    /// terminaría adoptando un proyecto que no tiene nada que ver.
    /// </summary>
    public static string? BuscarProyectoDe(string rutaArchivo, int nivelesArriba = 3)
    {
        try
        {
            var dir = Path.GetDirectoryName(Path.GetFullPath(rutaArchivo));

            for (int i = 0; i <= nivelesArriba && dir is not null; i++)
            {
                var encontrados = Directory.GetFiles(dir, "*" + Extension);

                // Con más de uno no se adivina: se deja que el usuario elija.
                if (encontrados.Length == 1) return encontrados[0];
                if (encontrados.Length > 1) return null;

                dir = Path.GetDirectoryName(dir);
            }
        }
        catch (Exception)
        {
            // Rutas inválidas o sin permisos: no hay proyecto que ofrecer.
        }

        return null;
    }
}
