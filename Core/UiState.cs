namespace AsmEditor.Core;

/// <summary>
/// Un rectángulo de pantalla, sin depender de System.Drawing.
///
/// Core se compila también en el proyecto de pruebas, que es net8.0 puro y no
/// tiene WinForms: por eso no se usa <c>System.Drawing.Rectangle</c> acá.
/// La UI convierte de uno a otro al llamar.
/// </summary>
public readonly record struct ScreenRect(int X, int Y, int Width, int Height)
{
    public int Right => X + Width;
    public int Bottom => Y + Height;

    /// <summary>Ancho y alto del solapamiento con otro rectángulo (0 si no se tocan).</summary>
    public (int Width, int Height) OverlapWith(ScreenRect other)
    {
        int w = Math.Min(Right, other.Right) - Math.Max(X, other.X);
        int h = Math.Min(Bottom, other.Bottom) - Math.Max(Y, other.Y);
        return (Math.Max(0, w), Math.Max(0, h));
    }
}

/// <summary>
/// Posición y tamaño de la ventana entre sesiones.
///
/// ⚠ NO SE RESTAURA A CIEGAS. Un monitor que se desconectó, una resolución que
/// cambió o una laptop que se desacopló dejan coordenadas que ya no existen, y
/// la ventana aparecería fuera de la pantalla sin forma de alcanzarla. Antes de
/// usarla hay que validarla contra los monitores actuales — ver
/// <see cref="EsVisibleEn"/>.
/// </summary>
public sealed class WindowGeometry
{
    public int X { get; set; }
    public int Y { get; set; }
    public int Width { get; set; } = 1300;
    public int Height { get; set; } = 820;

    /// <summary>True si la ventana estaba maximizada al cerrarse.</summary>
    public bool Maximized { get; set; }

    /// <summary>True si los valores fueron guardados alguna vez.</summary>
    public bool Saved { get; set; }

    /// <summary>
    /// Comprueba que una porción suficiente de la ventana caiga dentro de alguna
    /// de las áreas de trabajo dadas (una por monitor).
    ///
    /// Alcanza con que se vea una franja: lo que importa es que quede algo
    /// visible para poder agarrarla con el mouse y traerla de vuelta.
    /// </summary>
    public bool EsVisibleEn(IEnumerable<ScreenRect> areasDeTrabajo, int minimoVisible = 80)
    {
        if (Width <= 0 || Height <= 0) return false;

        var ventana = new ScreenRect(X, Y, Width, Height);

        foreach (var area in areasDeTrabajo)
        {
            var (w, h) = ventana.OverlapWith(area);
            if (w >= minimoVisible && h >= minimoVisible) return true;
        }

        return false;
    }
}

/// <summary>
/// Lo que el editor recuerda entre sesiones: tema, archivos recientes, qué había
/// abierto y dónde estaba la ventana.
///
/// Vive en Core y sin dependencias de UI para poder probarse: las reglas de los
/// recientes (sin duplicados, el último primero, con tope) y la validación de la
/// geometría son justo las que se rompen en silencio.
/// </summary>
public sealed class UiState
{
    /// <summary>Cuántos archivos recientes se recuerdan.</summary>
    public const int MaxRecientes = 10;

    public ModoTemaGuardado Theme { get; set; } = ModoTemaGuardado.Oscuro;

    /// <summary>Archivos abiertos al cerrar, para restaurarlos al abrir.</summary>
    public List<string> OpenFiles { get; set; } = new();

    /// <summary>Índice de la pestaña que estaba activa.</summary>
    public int ActiveFileIndex { get; set; }

    /// <summary>Recientes, del más nuevo al más viejo.</summary>
    public List<string> RecentFiles { get; set; } = new();

    /// <summary>
    /// El .asmproj que estaba abierto al cerrar, para reabrirlo.
    /// Null o vacío = no había proyecto abierto, que es un estado válido: el
    /// editor funciona con archivos sueltos igual que siempre.
    /// </summary>
    public string? ProyectoAbierto { get; set; }

    /// <summary>Proyectos recientes, del más nuevo al más viejo.</summary>
    public List<string> RecentProjects { get; set; } = new();

    public WindowGeometry Window { get; set; } = new();

    /// <summary>True si hay que reabrir lo que estaba abierto la vez anterior.</summary>
    public bool RestoreSession { get; set; } = true;

    /// <summary>
    /// Qué hacer al arrancar sin archivo por línea de comandos. Por defecto la
    /// ventana de inicio. Un settings.json anterior no lo trae y queda en ese
    /// valor: a partir de la ventana de inicio el editor YA NO reabre solo el
    /// último proyecto, salvo que se elija <see cref="Core.AlIniciar.UltimoProyecto"/>.
    /// </summary>
    public AlIniciar AlIniciar { get; set; } = AlIniciar.VentanaDeInicio;

    /// <summary>
    /// Cuándo se abrió por última vez cada proyecto (ruta del .asmproj → fecha),
    /// para agrupar los recientes en la ventana de inicio. Los settings.json de
    /// antes no lo traen: ahí la ventana usa la fecha del archivo.
    /// </summary>
    public Dictionary<string, DateTime> FechasProyectos { get; set; } = new();

    /// <summary>
    /// Qué archivos tenía abiertos cada proyecto al cerrarlo, para reabrirlos
    /// al volver a abrirlo desde la ventana de inicio. Reemplaza, para los
    /// proyectos, a la lista única <see cref="OpenFiles"/> de antes.
    /// </summary>
    public Dictionary<string, SesionDeProyecto> SesionesProyectos { get; set; } = new();

    /// <summary>
    /// Registra un archivo como recién usado: queda primero, sin duplicados y
    /// respetando el tope.
    ///
    /// La comparación ignora mayúsculas y normaliza la ruta, así "C:\a\x.asm" y
    /// "c:/a/x.asm" no entran dos veces.
    /// </summary>
    public void AddRecent(string? path)
    {
        if (string.IsNullOrWhiteSpace(path)) return;

        RecentFiles.RemoveAll(p => PathComparer.SamePath(p, path));
        RecentFiles.Insert(0, path);

        if (RecentFiles.Count > MaxRecientes)
        {
            RecentFiles.RemoveRange(MaxRecientes, RecentFiles.Count - MaxRecientes);
        }
    }

    public void RemoveRecent(string? path)
    {
        if (string.IsNullOrWhiteSpace(path)) return;
        RecentFiles.RemoveAll(p => PathComparer.SamePath(p, path));
    }

    public void ClearRecent() => RecentFiles.Clear();

    /// <summary>
    /// Registra un proyecto como recién usado. Misma mecánica que
    /// <see cref="AddRecent"/>, en una lista aparte: mezclar proyectos y
    /// archivos en un solo menú de recientes hace que abrir uno u otro sea una
    /// lotería según la extensión.
    /// </summary>
    public void AddRecentProject(string? path) => AddRecentProject(path, DateTime.Now);

    /// <summary>Igual, con la fecha explícita (las pruebas no dependen del reloj).</summary>
    public void AddRecentProject(string? path, DateTime cuando)
    {
        if (string.IsNullOrWhiteSpace(path)) return;

        RecentProjects.RemoveAll(p => PathComparer.SamePath(p, path));
        RecentProjects.Insert(0, path);

        if (RecentProjects.Count > MaxRecientes)
        {
            RecentProjects.RemoveRange(MaxRecientes, RecentProjects.Count - MaxRecientes);
        }

        QuitarClave(FechasProyectos, path);
        FechasProyectos[path] = cuando;
    }

    /// <summary>
    /// Saca un proyecto de los recientes ("Quitar de la lista" en la ventana de
    /// inicio), con su fecha y su sesión: no borra nada del disco.
    /// </summary>
    public void RemoveRecentProject(string? path)
    {
        if (string.IsNullOrWhiteSpace(path)) return;
        RecentProjects.RemoveAll(p => PathComparer.SamePath(p, path));
        QuitarClave(FechasProyectos, path);
        QuitarClave(SesionesProyectos, path);
    }

    /// <summary>Anota qué archivos tenía abiertos el proyecto al cerrarlo.</summary>
    public void GuardarSesionDeProyecto(string? proyecto, IEnumerable<string> archivos, int activo)
    {
        if (string.IsNullOrWhiteSpace(proyecto)) return;

        var lista = archivos.Where(a => !string.IsNullOrWhiteSpace(a)).ToList();

        QuitarClave(SesionesProyectos, proyecto);
        SesionesProyectos[proyecto] = new SesionDeProyecto
        {
            Archivos = lista,
            Activo = lista.Count == 0 ? 0 : Math.Clamp(activo, 0, lista.Count - 1)
        };
    }

    /// <summary>La sesión guardada de un proyecto, o null si no tiene.</summary>
    public SesionDeProyecto? SesionDe(string? proyecto)
    {
        if (string.IsNullOrWhiteSpace(proyecto)) return null;

        foreach (var s in SesionesProyectos)
        {
            if (PathComparer.SamePath(s.Key, proyecto)) return s.Value;
        }
        return null;
    }

    /// <summary>Quita la entrada de esa ruta aunque esté escrita distinto (mayúsculas, barras).</summary>
    private static void QuitarClave<T>(Dictionary<string, T> dic, string ruta)
    {
        foreach (var k in dic.Keys.Where(k => PathComparer.SamePath(k, ruta)).ToList()) dic.Remove(k);
    }

    public void ClearRecentProjects() => RecentProjects.Clear();

    /// <summary>
    /// Quita de los recientes los archivos que ya no existen. Se llama al
    /// cargar: una lista que ofrece archivos borrados es peor que una corta.
    /// </summary>
    public void PurgeMissingRecents(Func<string, bool> existe)
    {
        RecentFiles.RemoveAll(p => !existe(p));
        RecentProjects.RemoveAll(p => !existe(p));

        // Un proyecto que ya no está deja de reabrirse solo, pero eso no es un
        // error: el editor arranca sin proyecto, que es un estado válido.
        if (!string.IsNullOrWhiteSpace(ProyectoAbierto) && !existe(ProyectoAbierto))
        {
            ProyectoAbierto = null;
        }
    }

    /// <summary>
    /// Deja el estado consistente después de leerlo del disco: sin nulos, sin
    /// duplicados en recientes, con el índice activo dentro de rango.
    /// </summary>
    public void EnsureValid()
    {
        OpenFiles ??= new List<string>();
        RecentFiles ??= new List<string>();
        RecentProjects ??= new List<string>();
        Window ??= new WindowGeometry();
        FechasProyectos ??= new Dictionary<string, DateTime>();
        SesionesProyectos ??= new Dictionary<string, SesionDeProyecto>();

        if (!Enum.IsDefined(AlIniciar)) AlIniciar = AlIniciar.VentanaDeInicio;

        OpenFiles.RemoveAll(string.IsNullOrWhiteSpace);
        RecentFiles.RemoveAll(string.IsNullOrWhiteSpace);
        RecentProjects.RemoveAll(string.IsNullOrWhiteSpace);

        if (string.IsNullOrWhiteSpace(ProyectoAbierto)) ProyectoAbierto = null;

        // Duplicados en recientes: se conserva la primera aparición (la más nueva).
        var vistos = new List<string>();
        RecentFiles.RemoveAll(p =>
        {
            if (vistos.Any(v => PathComparer.SamePath(v, p))) return true;
            vistos.Add(p);
            return false;
        });

        if (RecentFiles.Count > MaxRecientes)
        {
            RecentFiles.RemoveRange(MaxRecientes, RecentFiles.Count - MaxRecientes);
        }

        // Lo mismo con los proyectos recientes, en su propia lista.
        var vistosProy = new List<string>();
        RecentProjects.RemoveAll(p =>
        {
            if (vistosProy.Any(v => PathComparer.SamePath(v, p))) return true;
            vistosProy.Add(p);
            return false;
        });

        if (RecentProjects.Count > MaxRecientes)
        {
            RecentProjects.RemoveRange(MaxRecientes, RecentProjects.Count - MaxRecientes);
        }

        if (OpenFiles.Count == 0) ActiveFileIndex = 0;
        else ActiveFileIndex = Math.Clamp(ActiveFileIndex, 0, OpenFiles.Count - 1);

        // Fechas y sesiones solo de proyectos que siguen en los recientes (o
        // del abierto): sin esto crecerían para siempre con proyectos viejos.
        bool Vigente(string ruta) =>
            RecentProjects.Any(p => PathComparer.SamePath(p, ruta)) ||
            (ProyectoAbierto is not null && PathComparer.SamePath(ProyectoAbierto, ruta));

        foreach (var k in FechasProyectos.Keys.Where(k => !Vigente(k)).ToList()) FechasProyectos.Remove(k);
        foreach (var k in SesionesProyectos.Keys.Where(k => !Vigente(k)).ToList()) SesionesProyectos.Remove(k);

        foreach (var s in SesionesProyectos.Values)
        {
            s.Archivos ??= new List<string>();
            s.Archivos.RemoveAll(string.IsNullOrWhiteSpace);
            s.Activo = s.Archivos.Count == 0 ? 0 : Math.Clamp(s.Activo, 0, s.Archivos.Count - 1);
        }
    }
}

/// <summary>Los archivos que un proyecto tenía abiertos al cerrarlo, y cuál estaba activo.</summary>
public sealed class SesionDeProyecto
{
    public List<string> Archivos { get; set; } = new();
    public int Activo { get; set; }
}

/// <summary>
/// El tema, serializado aparte del enum de la UI para que Core no dependa de
/// WinForms. Los valores coinciden con los de ModoTema.
/// </summary>
public enum ModoTemaGuardado
{
    Oscuro = 0,
    Claro = 1
}
