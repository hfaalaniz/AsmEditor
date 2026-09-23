namespace AsmEditor.Core;

/// <summary>
/// Un documento abierto en el editor, visto desde la lógica (sin UI).
/// FilePath es null mientras el documento nunca fue guardado.
/// </summary>
public sealed class DocumentState
{
    private static int _untitledCounter;

    public string? FilePath { get; set; }
    public bool IsDirty { get; set; }

    /// <summary>Número de "Sin título N", fijo durante toda la vida del documento.</summary>
    public int UntitledNumber { get; }

    public DocumentState(string? filePath = null)
    {
        FilePath = filePath;
        UntitledNumber = filePath is null ? ++_untitledCounter : 0;
    }

    /// <summary>Nombre para la pestaña: el del archivo, o "Sin título N", con * si está sucio.</summary>
    public string DisplayName
    {
        get
        {
            var baseName = FilePath is null
                ? $"Sin título {UntitledNumber}"
                : Path.GetFileName(FilePath);
            return IsDirty ? baseName + " *" : baseName;
        }
    }

    /// <summary>True si este documento corresponde a la ruta dada (comparación de Windows).</summary>
    public bool Matches(string? path)
    {
        if (FilePath is null || path is null) return false;
        return PathComparer.SamePath(FilePath, path);
    }
}

/// <summary>
/// Comparación de rutas de archivo en Windows: sin distinguir mayúsculas y
/// normalizando separadores y rutas relativas.
/// </summary>
public static class PathComparer
{
    public static bool SamePath(string? a, string? b)
    {
        if (a is null || b is null) return false;
        if (a.Length == 0 || b.Length == 0) return false;

        return string.Equals(Normalize(a), Normalize(b), StringComparison.OrdinalIgnoreCase);
    }

    public static string Normalize(string path)
    {
        try
        {
            return Path.GetFullPath(path).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        }
        catch
        {
            // Rutas inválidas: al menos unificamos los separadores para poder compararlas.
            return path.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar)
                       .TrimEnd(Path.DirectorySeparatorChar);
        }
    }
}

/// <summary>
/// Colección de documentos abiertos con el índice del activo.
/// Es la lógica que respalda el TabControl: qué está abierto, cuál está activo,
/// y la regla de no duplicar un archivo que ya está abierto.
/// </summary>
public sealed class OpenDocuments
{
    private readonly List<DocumentState> _documents = new();

    public IReadOnlyList<DocumentState> Documents => _documents;
    public int Count => _documents.Count;

    /// <summary>Índice del documento activo, o -1 si no hay ninguno abierto.</summary>
    public int ActiveIndex { get; private set; } = -1;

    public DocumentState? Active =>
        ActiveIndex >= 0 && ActiveIndex < _documents.Count ? _documents[ActiveIndex] : null;

    public bool AnyDirty => _documents.Any(d => d.IsDirty);

    /// <summary>
    /// Agrega un documento y lo deja activo. Devuelve su índice.
    /// </summary>
    public int Add(DocumentState document)
    {
        _documents.Add(document);
        ActiveIndex = _documents.Count - 1;
        return ActiveIndex;
    }

    /// <summary>
    /// Índice del documento que ya tiene abierta esa ruta, o -1 si ninguno.
    /// </summary>
    public int IndexOfPath(string? path)
    {
        if (path is null) return -1;
        for (int i = 0; i < _documents.Count; i++)
        {
            if (_documents[i].Matches(path)) return i;
        }
        return -1;
    }

    /// <summary>
    /// Regla central de las pestañas: si el archivo ya está abierto, lo activa y
    /// devuelve false (no hay que crear pestaña). Si no, devuelve true: el llamador
    /// debe crear la pestaña y luego llamar a Add.
    /// </summary>
    public bool TryActivateExisting(string path)
    {
        int index = IndexOfPath(path);
        if (index < 0) return false;

        ActiveIndex = index;
        return true;
    }

    public void SetActive(int index)
    {
        if (index < 0 || index >= _documents.Count) return;
        ActiveIndex = index;
    }

    /// <summary>
    /// Quita el documento en ese índice y reajusta el activo de forma razonable:
    /// se queda en la misma posición (la pestaña de la derecha ocupa el lugar) o
    /// retrocede si se cerró la última.
    /// </summary>
    public void RemoveAt(int index)
    {
        if (index < 0 || index >= _documents.Count) return;

        _documents.RemoveAt(index);

        if (_documents.Count == 0)
        {
            ActiveIndex = -1;
            return;
        }

        if (index < ActiveIndex)
        {
            ActiveIndex--;
        }
        else if (index == ActiveIndex)
        {
            ActiveIndex = Math.Min(index, _documents.Count - 1);
        }
    }

    public IEnumerable<DocumentState> Dirty => _documents.Where(d => d.IsDirty);
}
