namespace AsmEditor.Core;

/// <summary>Qué hace el editor al arrancar sin un archivo por línea de comandos.</summary>
public enum AlIniciar
{
    /// <summary>Muestra la ventana de inicio (el selector de proyectos). Por defecto.</summary>
    VentanaDeInicio = 0,

    /// <summary>Reabre el último proyecto, como hacía el editor antes de la ventana de inicio.</summary>
    UltimoProyecto = 1,

    /// <summary>Arranca el IDE vacío.</summary>
    EntornoVacio = 2
}

/// <summary>Un proyecto reciente, como lo muestra la ventana de inicio.</summary>
/// <param name="Ruta">Ruta del .asmproj.</param>
/// <param name="Nombre">El nombre que se muestra (el del archivo, sin extensión).</param>
/// <param name="Carpeta">La carpeta, que va debajo del nombre.</param>
/// <param name="UltimaApertura">Cuándo se abrió por última vez.</param>
public sealed record ProyectoReciente(string Ruta, string Nombre, string Carpeta, DateTime UltimaApertura);

/// <summary>Un grupo de la lista: "Hoy", "Esta semana"... con sus proyectos.</summary>
public sealed record GrupoRecientes(string Titulo, IReadOnlyList<ProyectoReciente> Proyectos);

/// <summary>
/// La lista de proyectos recientes de la ventana de inicio: armado, agrupado
/// por fecha y filtro del buscador. Sin interfaz, para probarlo.
/// </summary>
public static class RecientesInicio
{
    /// <summary>
    /// Arma la lista a partir de las rutas (en orden, del más nuevo al más
    /// viejo) y las fechas guardadas.
    ///
    /// ⚠ UN PROYECTO SIN FECHA GUARDADA usa la fecha del archivo (la que da
    /// <paramref name="fechaDelArchivo"/>). Pasa con los settings.json de antes
    /// de la ventana de inicio, que solo guardaban la ruta: sin esto, todos los
    /// proyectos viejos caerían juntos en "Anterior" con una fecha inventada.
    /// </summary>
    public static List<ProyectoReciente> Armar(IEnumerable<string> rutas,
                                               IReadOnlyDictionary<string, DateTime> fechas,
                                               Func<string, DateTime> fechaDelArchivo)
    {
        var lista = new List<ProyectoReciente>();

        foreach (var ruta in rutas)
        {
            var fecha = BuscarFecha(fechas, ruta) ?? fechaDelArchivo(ruta);

            lista.Add(new ProyectoReciente(
                ruta,
                Path.GetFileNameWithoutExtension(ruta),
                Path.GetDirectoryName(ruta) ?? "",
                fecha));
        }

        // Del más nuevo al más viejo por FECHA: el orden de la lista guardada y
        // las fechas pueden no coincidir si una fecha salió del archivo.
        return lista.OrderByDescending(p => p.UltimaApertura).ToList();
    }

    private static DateTime? BuscarFecha(IReadOnlyDictionary<string, DateTime> fechas, string ruta)
    {
        foreach (var f in fechas)
        {
            if (PathComparer.SamePath(f.Key, ruta)) return f.Value;
        }
        return null;
    }

    /// <summary>
    /// Agrupa por fecha como Visual Studio: Hoy, Ayer, Esta semana, Este mes,
    /// Anterior. Los grupos vacíos no aparecen.
    ///
    /// "Esta semana" es desde el lunes (semana de Argentina, no la de EE. UU.
    /// que arranca el domingo) y sin Hoy ni Ayer; "Este mes", del mismo mes
    /// calendario sin lo anterior.
    /// </summary>
    public static List<GrupoRecientes> Agrupar(IEnumerable<ProyectoReciente> proyectos, DateTime ahora)
    {
        var hoy = ahora.Date;
        var ayer = hoy.AddDays(-1);
        int desdeLunes = ((int)hoy.DayOfWeek + 6) % 7;
        var lunes = hoy.AddDays(-desdeLunes);
        var primeroDelMes = new DateTime(hoy.Year, hoy.Month, 1);

        string Grupo(DateTime f)
        {
            var d = f.Date;
            if (d >= hoy) return "Hoy";
            if (d == ayer) return "Ayer";
            if (d >= lunes) return "Esta semana";
            if (d >= primeroDelMes) return "Este mes";
            return "Anterior";
        }

        var orden = new[] { "Hoy", "Ayer", "Esta semana", "Este mes", "Anterior" };

        return proyectos
            .OrderByDescending(p => p.UltimaApertura)
            .GroupBy(p => Grupo(p.UltimaApertura))
            .OrderBy(g => Array.IndexOf(orden, g.Key))
            .Select(g => new GrupoRecientes(g.Key, g.ToList()))
            .ToList();
    }

    /// <summary>
    /// Filtro del buscador: cada palabra tiene que aparecer en el nombre o en
    /// la carpeta, sin distinguir mayúsculas ni acentos (el mismo criterio que
    /// el buscador de comandos). Vacío = todos.
    /// </summary>
    public static List<ProyectoReciente> Filtrar(IEnumerable<ProyectoReciente> proyectos, string? consulta)
    {
        var q = FiltroComandos.Normalizar(consulta);
        if (q.Length == 0) return proyectos.ToList();

        var palabras = q.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        return proyectos
            .Where(p =>
            {
                var texto = FiltroComandos.Normalizar(p.Nombre + " " + p.Carpeta);
                return palabras.All(texto.Contains);
            })
            .ToList();
    }
}
