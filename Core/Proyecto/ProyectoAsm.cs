namespace AsmEditor.Core.Proyecto;

/// <summary>
/// Un proyecto del editor: la LISTA EXPLÍCITA de archivos que lo componen, cuál
/// se compila, adónde va la salida y con qué targets.
///
/// ⚠ ES UNA LISTA, NO UNA CARPETA. Un .asm que esté en la carpeta pero no en
/// <see cref="Archivos"/> NO pertenece al proyecto. Es a propósito: así se
/// pueden tener pruebas y descartes al lado de los fuentes de verdad sin que el
/// proyecto los adopte.
///
/// ⚠ EL PROYECTO ES OPCIONAL. El editor tiene que seguir abriendo y compilando
/// un .asm suelto sin proyecto, exactamente como antes de que esto existiera.
/// Nada acá puede volverse obligatorio para compilar.
///
/// No conoce WinForms: se compila también en el proyecto de pruebas.
/// </summary>
public sealed class ProyectoAsm
{
    /// <summary>
    /// Nombre para mostrar. Es libre: a diferencia del nombre de un formulario,
    /// de acá no sale ningún símbolo de ensamblador, así que puede tener
    /// espacios y acentos.
    /// </summary>
    public string Nombre { get; set; } = "Proyecto";

    /// <summary>
    /// El .asm que se ensambla, relativo a la carpeta del .asmproj.
    ///
    /// ⚠ ES EL QUE COMPILA F7, NO LA PESTAÑA ACTIVA. Sin esto, mirar un .inc y
    /// apretar F7 ensambla el .inc, que no es un programa, y NASM devuelve
    /// errores que no dicen que se erró de archivo.
    /// </summary>
    public string ArchivoPrincipal { get; set; } = "";

    /// <summary>
    /// Carpeta donde van el .obj y el .exe, relativa al .asmproj.
    ///
    /// ⚠ VACÍO SIGNIFICA «AL LADO DEL FUENTE», Y ESE ES EL VALOR POR DEFECTO.
    /// Es el comportamiento que el editor tuvo siempre y del que dependen los
    /// scripts de afuera (build.ps1 busca el .exe al lado del .asm). Los
    /// proyectos nuevos nacen con "bin" —ver <see cref="Nuevo"/>—, pero eso es
    /// una elección del que los crea, no del formato.
    /// </summary>
    public string CarpetaSalida { get; set; } = "";

    /// <summary>
    /// Los archivos del proyecto, RELATIVOS a la carpeta del .asmproj.
    ///
    /// ⚠ RELATIVOS, NUNCA ABSOLUTOS: con rutas absolutas, mover o copiar la
    /// carpeta del proyecto lo rompe, y es algo que se hace todo el tiempo
    /// (respaldos, pasarle el proyecto a otro, renombrar la carpeta).
    /// </summary>
    public List<string> Archivos { get; set; } = new();

    /// <summary>
    /// Targets propios del proyecto.
    ///
    /// ⚠ VACÍO = HEREDA LOS GLOBALES. No se copian los globales al crear el
    /// proyecto: si se copiaran, un cambio posterior en la configuración global
    /// no llegaría nunca a los proyectos ya creados, y el usuario no tendría
    /// forma de saber por qué.
    /// </summary>
    public List<BuildTarget> Targets { get; set; } = new();

    /// <summary>Índice del target activo dentro de <see cref="Targets"/>.</summary>
    public int TargetActivo { get; set; }

    // ------------------------------------------------------------------
    // Estado que no se guarda
    // ------------------------------------------------------------------

    /// <summary>
    /// Ruta del .asmproj en disco. La pone quien carga o guarda el archivo; no
    /// se serializa, porque el archivo no puede contener su propia ubicación
    /// sin quedar mal en cuanto alguien lo mueve.
    /// </summary>
    public string? RutaArchivo { get; set; }

    /// <summary>La carpeta del .asmproj: la base de todas las rutas relativas.</summary>
    public string? Carpeta =>
        RutaArchivo is null ? null : Path.GetDirectoryName(Path.GetFullPath(RutaArchivo));

    // ------------------------------------------------------------------
    // Rutas
    // ------------------------------------------------------------------

    /// <summary>
    /// Convierte una ruta del proyecto (relativa) en una ruta absoluta.
    /// Devuelve null si el proyecto todavía no tiene ubicación en disco.
    /// </summary>
    public string? RutaAbsoluta(string? relativa)
    {
        if (string.IsNullOrWhiteSpace(relativa)) return null;

        var carpeta = Carpeta;
        if (carpeta is null) return null;

        // Una ruta ya absoluta se respeta: puede venir de un archivo editado a
        // mano, y resolverla contra la carpeta produciría un disparate.
        if (Path.IsPathRooted(relativa)) return Path.GetFullPath(relativa);

        return Path.GetFullPath(Path.Combine(carpeta, relativa));
    }

    /// <summary>
    /// Convierte una ruta absoluta en una relativa al proyecto, para guardarla.
    ///
    /// Si el archivo está fuera de la carpeta del proyecto devuelve la absoluta:
    /// una relativa con "..\..\.." hasta otra unidad no es más portable, y en
    /// unidades distintas ni siquiera se puede expresar.
    /// </summary>
    public string RutaRelativa(string absoluta)
    {
        var carpeta = Carpeta;
        if (carpeta is null) return absoluta;

        try
        {
            var completa = Path.GetFullPath(absoluta);
            var rel = Path.GetRelativePath(carpeta, completa);

            // GetRelativePath devuelve la absoluta cuando están en unidades
            // distintas; y una que sube de carpeta no aporta portabilidad.
            if (Path.IsPathRooted(rel) || rel.StartsWith("..")) return completa;

            return rel;
        }
        catch (ArgumentException)
        {
            // Ruta inválida: se guarda tal cual y la validación la marcará.
            return absoluta;
        }
    }

    /// <summary>Ruta absoluta del archivo que se compila, o null si no hay.</summary>
    public string? RutaPrincipal => RutaAbsoluta(ArchivoPrincipal);

    /// <summary>
    /// Adónde va el .obj/.exe de un fuente dado.
    ///
    /// Con <see cref="CarpetaSalida"/> vacía, al lado del fuente: el
    /// comportamiento de siempre. Con carpeta, el archivo conserva su nombre
    /// pero cambia de carpeta.
    /// </summary>
    public string RutaDeSalida(string rutaFuente, string extension)
    {
        var nombre = Path.GetFileNameWithoutExtension(rutaFuente) + extension;

        if (string.IsNullOrWhiteSpace(CarpetaSalida))
        {
            var dir = Path.GetDirectoryName(Path.GetFullPath(rutaFuente));
            return dir is null ? nombre : Path.Combine(dir, nombre);
        }

        var salida = RutaAbsoluta(CarpetaSalida);
        return salida is null ? nombre : Path.Combine(salida, nombre);
    }

    /// <summary>
    /// Crea la carpeta de salida si hace falta.
    ///
    /// Se llama antes de compilar: NASM no crea la carpeta del -o y falla con
    /// un error de apertura que no dice que la carpeta no existe.
    /// </summary>
    public void AsegurarCarpetaDeSalida()
    {
        if (string.IsNullOrWhiteSpace(CarpetaSalida)) return;

        var salida = RutaAbsoluta(CarpetaSalida);
        if (salida is not null && !Directory.Exists(salida))
        {
            Directory.CreateDirectory(salida);
        }
    }

    // ------------------------------------------------------------------
    // La lista de archivos
    // ------------------------------------------------------------------

    /// <summary>
    /// True si esa ruta absoluta está en la lista del proyecto.
    /// Compara normalizando, porque la misma ruta puede escribirse de varias
    /// formas y en Windows no distingue mayúsculas.
    /// </summary>
    public bool Contiene(string rutaAbsoluta)
    {
        foreach (var a in Archivos)
        {
            var abs = RutaAbsoluta(a);
            if (abs is not null && PathComparer.SamePath(abs, rutaAbsoluta)) return true;
        }

        return false;
    }

    /// <summary>
    /// Agrega un archivo si no estaba. Devuelve true si lo agregó.
    /// Guarda la ruta relativa, que es la forma portable.
    /// </summary>
    public bool Agregar(string rutaAbsoluta)
    {
        if (string.IsNullOrWhiteSpace(rutaAbsoluta)) return false;
        if (Contiene(rutaAbsoluta)) return false;

        Archivos.Add(RutaRelativa(rutaAbsoluta));
        return true;
    }

    /// <summary>
    /// Saca un archivo de la lista. Devuelve true si lo sacó.
    ///
    /// ⚠ NO BORRA EL ARCHIVO DEL DISCO. Sacarlo del proyecto y borrarlo son dos
    /// cosas distintas, y confundirlas destruye trabajo.
    /// </summary>
    public bool Quitar(string rutaAbsoluta)
    {
        for (int i = 0; i < Archivos.Count; i++)
        {
            var abs = RutaAbsoluta(Archivos[i]);

            if (abs is not null && PathComparer.SamePath(abs, rutaAbsoluta))
            {
                Archivos.RemoveAt(i);

                // Si se quitó el principal, el proyecto queda sin qué compilar;
                // se deja explícito en vez de apuntar a algo que ya no está.
                if (PathComparer.SamePath(RutaPrincipal, rutaAbsoluta))
                {
                    ArchivoPrincipal = "";
                }

                return true;
            }
        }

        return false;
    }

    /// <summary>Marca un archivo de la lista como el que se compila.</summary>
    public bool MarcarComoPrincipal(string rutaAbsoluta)
    {
        if (!Contiene(rutaAbsoluta)) return false;

        ArchivoPrincipal = RutaRelativa(rutaAbsoluta);
        return true;
    }

    /// <summary>
    /// Los archivos con su ruta absoluta y si existen en disco.
    ///
    /// ⚠ LOS QUE FALTAN SE INFORMAN, NO SE BORRAN DE LA LISTA. Que un archivo
    /// no esté puede ser que alguien lo movió, que falta traerlo de un
    /// respaldo, o que la unidad de red no está montada: sacarlo solo de la
    /// lista convierte un problema visible en una pérdida silenciosa.
    /// </summary>
    public List<ArchivoDeProyecto> ListarArchivos()
    {
        var lista = new List<ArchivoDeProyecto>();

        foreach (var rel in Archivos)
        {
            var abs = RutaAbsoluta(rel);

            lista.Add(new ArchivoDeProyecto(
                Relativa: rel,
                Absoluta: abs,
                Existe: abs is not null && File.Exists(abs),
                EsPrincipal: !string.IsNullOrWhiteSpace(ArchivoPrincipal) &&
                             string.Equals(rel, ArchivoPrincipal, StringComparison.OrdinalIgnoreCase)));
        }

        return lista;
    }

    // ------------------------------------------------------------------
    // Targets
    // ------------------------------------------------------------------

    /// <summary>
    /// Los targets que valen para este proyecto: los propios, o los globales si
    /// no tiene ninguno.
    /// </summary>
    public List<BuildTarget> TargetsEfectivos(List<BuildTarget> globales) =>
        Targets.Count > 0 ? Targets : globales;

    /// <summary>
    /// El target con el que se compila, dados los globales por si hereda.
    /// Nunca devuelve null: si no hay ninguno en ningún lado, crea los de fábrica.
    /// </summary>
    public BuildTarget TargetEfectivo(List<BuildTarget> globales)
    {
        var lista = TargetsEfectivos(globales);

        if (lista.Count == 0)
        {
            // No debería pasar, pero compilar sin target no es una opción:
            // antes de fallar se vuelve a los de fábrica.
            lista = BuildTarget.CreateDefaults();
            Targets = lista;
        }

        var i = TargetActivo;
        if (i < 0 || i >= lista.Count) i = 0;

        return lista[i];
    }

    /// <summary>True si el proyecto usa los targets globales en vez de los suyos.</summary>
    public bool HeredaTargets => Targets.Count == 0;

    // ------------------------------------------------------------------
    // Validación
    // ------------------------------------------------------------------

    /// <summary>
    /// Problemas del proyecto, para mostrarlos. Lista vacía = está sano.
    ///
    /// A diferencia de la validación del diseñador, esto NO impide trabajar:
    /// un proyecto al que le falta un archivo se sigue pudiendo abrir y editar.
    /// Son avisos, no errores fatales.
    /// </summary>
    public List<string> Validar()
    {
        var problemas = new List<string>();

        if (string.IsNullOrWhiteSpace(ArchivoPrincipal))
        {
            problemas.Add("El proyecto no tiene archivo principal: no hay qué compilar. " +
                          "Marcá uno con el botón derecho sobre un .asm de la lista.");
        }
        else if (!Contiene(RutaPrincipal ?? ""))
        {
            problemas.Add($"El archivo principal '{ArchivoPrincipal}' no está en la lista del proyecto.");
        }
        else if (RutaPrincipal is not null && !File.Exists(RutaPrincipal))
        {
            problemas.Add($"El archivo principal '{ArchivoPrincipal}' no existe en el disco.");
        }

        var faltantes = ListarArchivos().Where(a => !a.Existe).ToList();

        foreach (var f in faltantes)
        {
            problemas.Add($"Falta el archivo '{f.Relativa}'.");
        }

        // Duplicados: dos entradas que apuntan al mismo archivo harían que
        // "quitar" parezca no funcionar, porque queda la otra.
        var vistos = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var a in Archivos)
        {
            var abs = RutaAbsoluta(a);
            var clave = abs is null ? a : PathComparer.Normalize(abs);

            if (!vistos.Add(clave))
            {
                problemas.Add($"El archivo '{a}' está repetido en la lista.");
            }
        }

        return problemas;
    }

    /// <summary>
    /// Arregla lo que se puede arreglar solo, al cargar: índice de target fuera
    /// de rango y un principal que no está en la lista pero existe en disco.
    /// </summary>
    public void Normalizar()
    {
        var lista = Targets.Count > 0 ? Targets : null;

        if (lista is not null && (TargetActivo < 0 || TargetActivo >= lista.Count))
        {
            TargetActivo = 0;
        }

        // Un principal que existe pero no está listado se agrega: es lo que el
        // usuario quiso decir, y sin esto el proyecto no compila sin motivo claro.
        if (!string.IsNullOrWhiteSpace(ArchivoPrincipal))
        {
            var abs = RutaPrincipal;

            if (abs is not null && File.Exists(abs) && !Contiene(abs))
            {
                Archivos.Insert(0, RutaRelativa(abs));
            }
        }
    }

    /// <summary>
    /// Un proyecto nuevo, vacío, en la carpeta dada.
    ///
    /// Nace con la salida en "bin": es lo ordenado para algo que empieza de
    /// cero, y no afecta a nada que ya exista.
    /// </summary>
    public static ProyectoAsm Nuevo(string nombre, string rutaArchivo) => new()
    {
        Nombre = string.IsNullOrWhiteSpace(nombre) ? "Proyecto" : nombre.Trim(),
        RutaArchivo = rutaArchivo,
        CarpetaSalida = "bin"
    };

    public ProyectoAsm Clonar() => new()
    {
        Nombre = Nombre,
        ArchivoPrincipal = ArchivoPrincipal,
        CarpetaSalida = CarpetaSalida,
        Archivos = new List<string>(Archivos),
        Targets = Targets.Select(t => t.Clone()).ToList(),
        TargetActivo = TargetActivo,
        RutaArchivo = RutaArchivo
    };
}

/// <summary>Un archivo del proyecto, con lo que hace falta para mostrarlo.</summary>
public sealed record ArchivoDeProyecto(
    string Relativa,
    string? Absoluta,
    bool Existe,
    bool EsPrincipal)
{
    public string NombreDeArchivo => Path.GetFileName(Relativa);

    /// <summary>La subcarpeta dentro del proyecto, o "" si está en la raíz.</summary>
    public string SubCarpeta => Path.GetDirectoryName(Relativa) ?? "";

    public string Extension => Path.GetExtension(Relativa).ToLowerInvariant();
}
