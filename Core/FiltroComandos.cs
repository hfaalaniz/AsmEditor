using System.Globalization;
using System.Text;

namespace AsmEditor.Core;

/// <summary>
/// Un comando del menú, tal como lo muestra el buscador de la barra de título.
/// </summary>
/// <param name="Ruta">El camino en el menú, para mostrar: "Archivo › Guardar".</param>
/// <param name="Nombre">El último tramo: "Guardar".</param>
/// <param name="Atajo">El atajo de teclado para mostrar, o vacío.</param>
/// <param name="Habilitado">Si ahora se puede ejecutar.</param>
/// <param name="Dato">Lo que la interfaz necesita para ejecutarlo (el ítem del menú).</param>
public sealed record ComandoMenu(string Ruta, string Nombre, string Atajo, bool Habilitado, object? Dato = null);

/// <summary>
/// El filtro del buscador de comandos («Buscar (Ctrl+Q)»), como el de Visual
/// Studio: escribiendo parte del nombre aparecen los comandos del menú que
/// coinciden.
///
/// Vive en Core y no en la barra para poder probarlo sin abrir una ventana.
/// </summary>
public static class FiltroComandos
{
    /// <summary>Resultados que se muestran como máximo: más no se leen.</summary>
    public const int MaximoResultados = 12;

    /// <summary>
    /// Los comandos que coinciden con la consulta, mejores primero.
    ///
    /// Reglas:
    ///   - Cada palabra de la consulta tiene que aparecer en la ruta del
    ///     comando ("arch guar" encuentra "Archivo › Guardar").
    ///   - Sin distinguir mayúsculas NI ACENTOS: "disenador" encuentra
    ///     "Diseñador". Nadie tipea acentos en un buscador rápido.
    ///   - ⚠ LOS DESHABILITADOS NO APARECEN: ofrecer "Guardar" sin nada
    ///     abierto sería ofrecer algo que al elegirlo no hace nada.
    ///   - Primero los que EMPIEZAN con lo tipeado, después los que tienen una
    ///     palabra que empieza así, al final el resto; a igualdad, el orden del
    ///     menú.
    /// </summary>
    public static List<ComandoMenu> Filtrar(IEnumerable<ComandoMenu> comandos, string? consulta,
                                            int maximo = MaximoResultados)
    {
        var q = Normalizar(consulta);
        if (q.Length == 0) return new List<ComandoMenu>();

        var palabras = q.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        return comandos
            .Select((c, orden) => (c, orden, ruta: Normalizar(c.Ruta), nombre: Normalizar(c.Nombre)))
            .Where(x => x.c.Habilitado && palabras.All(p => x.ruta.Contains(p)))
            .OrderBy(x => Puntaje(x.nombre, q, palabras[0]))
            .ThenBy(x => x.orden)
            .Take(maximo)
            .Select(x => x.c)
            .ToList();
    }

    private static int Puntaje(string nombre, string consulta, string primeraPalabra)
    {
        if (nombre.StartsWith(consulta)) return 0;
        if (nombre.Split(' ').Any(w => w.StartsWith(primeraPalabra))) return 1;
        return 2;
    }

    /// <summary>
    /// Minúsculas, sin acentos, sin la marca de tecla de acceso (&amp;) y con
    /// los espacios colapsados.
    /// </summary>
    public static string Normalizar(string? texto)
    {
        if (string.IsNullOrWhiteSpace(texto)) return "";

        var sinMarca = QuitarMnemonico(texto).ToLowerInvariant().Normalize(NormalizationForm.FormD);

        var sb = new StringBuilder(sinMarca.Length);
        foreach (var ch in sinMarca)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(ch) == UnicodeCategory.NonSpacingMark) continue;
            sb.Append(char.IsWhiteSpace(ch) ? ' ' : ch);
        }

        return string.Join(' ', sb.ToString().Split(' ', StringSplitOptions.RemoveEmptyEntries));
    }

    /// <summary>
    /// El texto de un ítem de menú sin la marca de tecla de acceso:
    /// "&amp;Guardar" → "Guardar". Un "&amp;&amp;" es un &amp; literal.
    /// </summary>
    public static string QuitarMnemonico(string texto)
    {
        var sb = new StringBuilder(texto.Length);

        for (int i = 0; i < texto.Length; i++)
        {
            if (texto[i] == '&')
            {
                if (i + 1 < texto.Length && texto[i + 1] == '&') { sb.Append('&'); i++; }
                continue;
            }
            sb.Append(texto[i]);
        }

        return sb.ToString();
    }
}
