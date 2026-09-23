using System.Text;

namespace AsmEditor.Core.Disenador;

/// <summary>
/// Un control colocado en el canvas.
///
/// ⚠ EL NOMBRE ES UN IDENTIFICADOR DE ENSAMBLADOR, NO UNA ETIQUETA. De él sale
/// el símbolo `IDC_<NOMBRE>` que se emite al .asm y que el usuario escribe en
/// su código. Si tiene un espacio, un acento o choca con una palabra reservada,
/// el fuente generado NO ENSAMBLA. Por eso se valida con
/// <see cref="NombreValido"/> antes de aceptarlo, y no al generar.
/// </summary>
public sealed class ControlDisenado
{
    public TipoControl Tipo { get; set; } = TipoControl.Boton;

    /// <summary>
    /// Nombre del control, en el estilo de una constante de ensamblador.
    /// De acá sale el símbolo IDC_ que ve el código del usuario.
    /// </summary>
    public string Nombre { get; set; } = "";

    /// <summary>Texto que muestra el control. Vacío es válido (un campo en blanco).</summary>
    public string Texto { get; set; } = "";

    public int X { get; set; }
    public int Y { get; set; }
    public int Ancho { get; set; } = 100;
    public int Alto { get; set; } = 30;

    /// <summary>
    /// Identificador numérico del control, único dentro del formulario. Es lo
    /// que llega en LOWORD(wParam) con WM_COMMAND y lo que se le pasa a
    /// GetDlgItem. Lo asigna <see cref="FormularioDisenado.AsignarIdsFaltantes"/>.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Para <see cref="TipoControl.Personalizado"/>: el nombre de la clase que
    /// el usuario registra por su cuenta. Se ignora en los demás tipos.
    /// </summary>
    public string ClasePersonalizada { get; set; } = "";

    /// <summary>
    /// Estilos extra que se suman a los del tipo, escritos como los escribiría
    /// una persona ("WS_GROUP|BS_LEFT"). Van tal cual al .asm: NO se validan
    /// acá porque las constantes las resuelve NASM, y si el usuario escribe una
    /// que no existe el error sale del ensamblador con su número de línea, que
    /// es más útil que un error nuestro.
    /// </summary>
    public string EstilosExtra { get; set; } = "";

    /// <summary>El control no se dibuja al abrir la ventana (sin WS_VISIBLE).</summary>
    public bool OcultoAlInicio { get; set; }

    /// <summary>El control arranca deshabilitado (WS_DISABLED, 0x08000000).</summary>
    public bool DeshabilitadoAlInicio { get; set; }

    /// <summary>La clase Win32 que le corresponde, ya resuelto el caso personalizado.</summary>
    public string ClaseEfectiva =>
        Tipo == TipoControl.Personalizado
            ? ClasePersonalizada.Trim()
            : InfoTipoControl.ClaseWin32(Tipo);

    /// <summary>
    /// El símbolo que se emite al .asm y que el usuario usa en su código.
    /// Va en mayúsculas porque es una constante.
    /// </summary>
    public string SimboloId => "IDC_" + Nombre.Trim().ToUpperInvariant();

    /// <summary>
    /// La expresión de estilo completa, con nombres de winuser.h.
    /// Siempre arranca con WS_CHILD, y lleva WS_VISIBLE salvo que se pida oculto.
    /// </summary>
    public string ExpresionEstilo()
    {
        var partes = new List<string> { "WS_CHILD" };

        if (!OcultoAlInicio) partes.Add("WS_VISIBLE");
        if (DeshabilitadoAlInicio) partes.Add("WS_DISABLED");

        var baseTexto = InfoTipoControl.EstiloBaseTexto(Tipo);
        if (!string.IsNullOrWhiteSpace(baseTexto)) partes.Add(baseTexto);

        var extra = EstilosExtra.Trim();
        if (!string.IsNullOrWhiteSpace(extra)) partes.Add(extra);

        return string.Join("|", partes);
    }

    public ControlDisenado Clonar() => new()
    {
        Tipo = Tipo,
        Nombre = Nombre,
        Texto = Texto,
        X = X,
        Y = Y,
        Ancho = Ancho,
        Alto = Alto,
        Id = Id,
        ClasePersonalizada = ClasePersonalizada,
        EstilosExtra = EstilosExtra,
        OcultoAlInicio = OcultoAlInicio,
        DeshabilitadoAlInicio = DeshabilitadoAlInicio
    };

    // ------------------------------------------------------------------
    // Validación de nombres
    // ------------------------------------------------------------------

    /// <summary>
    /// Palabras que NO pueden ser nombre de control porque el símbolo generado
    /// chocaría con algo que ya existe en el .asm.
    ///
    /// Se compara contra el nombre en mayúsculas, que es como queda el símbolo.
    /// </summary>
    private static readonly HashSet<string> Reservados = new(StringComparer.OrdinalIgnoreCase)
    {
        // Registros x86/x64: NASM los rechaza como etiqueta
        "AL","AH","BL","BH","CL","CH","DL","DH",
        "AX","BX","CX","DX","SI","DI","BP","SP",
        "EAX","EBX","ECX","EDX","ESI","EDI","EBP","ESP",
        "RAX","RBX","RCX","RDX","RSI","RDI","RBP","RSP",
        "R8","R9","R10","R11","R12","R13","R14","R15",
        "CS","DS","ES","FS","GS","SS",
        // Directivas y palabras de NASM
        "SECTION","SEGMENT","GLOBAL","EXTERN","DB","DW","DD","DQ",
        "RESB","RESW","RESD","RESQ","EQU","TIMES","ALIGN","DEFAULT",
        "BYTE","WORD","DWORD","QWORD","PTR","REL","ABS",
        // Símbolos que emite el propio generador
        "MAIN","WNDPROC","MSG","HINST","HWND","WC"
    };

    /// <summary>
    /// True si el nombre sirve como identificador de ensamblador.
    /// Regla: empieza con letra o guion bajo, sigue con letras, dígitos o
    /// guiones bajos, y no es una palabra reservada. Solo ASCII: un acento
    /// en un símbolo hace fallar a NASM.
    /// </summary>
    public static bool NombreValido(string? nombre)
    {
        if (string.IsNullOrWhiteSpace(nombre)) return false;

        var n = nombre.Trim();

        if (!(char.IsAsciiLetter(n[0]) || n[0] == '_')) return false;

        foreach (var c in n)
        {
            if (!(char.IsAsciiLetterOrDigit(c) || c == '_')) return false;
        }

        return !Reservados.Contains(n);
    }

    /// <summary>
    /// Por qué un nombre no sirve, para mostrarlo en el editor de propiedades.
    /// Devuelve null si el nombre es válido.
    /// </summary>
    public static string? ExplicarNombreInvalido(string? nombre)
    {
        if (string.IsNullOrWhiteSpace(nombre))
            return "El nombre no puede estar vacío.";

        var n = nombre.Trim();

        if (!(char.IsAsciiLetter(n[0]) || n[0] == '_'))
            return "Tiene que empezar con una letra o un guion bajo.";

        foreach (var c in n)
        {
            if (char.IsAsciiLetterOrDigit(c) || c == '_') continue;

            return c == ' '
                ? "No puede llevar espacios; usá guion bajo."
                : $"El carácter '{c}' no sirve en un símbolo de ensamblador (solo letras sin acento, dígitos y guion bajo).";
        }

        if (Reservados.Contains(n))
            return $"'{n}' es una palabra reservada del ensamblador; elegí otro nombre.";

        return null;
    }

    /// <summary>
    /// Convierte un texto cualquiera en un nombre válido, para proponer uno
    /// cuando el usuario suelta un control nuevo en el canvas.
    /// </summary>
    public static string SanearNombre(string? propuesto, string alternativa = "control")
    {
        if (string.IsNullOrWhiteSpace(propuesto)) propuesto = alternativa;

        var sb = new StringBuilder();

        foreach (var c in propuesto!.Trim())
        {
            if (char.IsAsciiLetterOrDigit(c)) sb.Append(c);
            else if (c == ' ' || c == '_' || c == '-') sb.Append('_');
            else
            {
                // Los acentos se pasan a su letra base en vez de perderse:
                // "Año" -> "Ano" se lee mejor que "Ao".
                var baseC = QuitarAcento(c);
                if (baseC is not null) sb.Append(baseC.Value);
            }
        }

        var n = sb.ToString().Trim('_');

        if (n.Length == 0) n = alternativa;
        if (!(char.IsAsciiLetter(n[0]) || n[0] == '_')) n = "_" + n;
        if (Reservados.Contains(n)) n += "_1";

        return n;
    }

    private static char? QuitarAcento(char c) => char.ToLowerInvariant(c) switch
    {
        'á' or 'à' or 'ä' or 'â' => char.IsUpper(c) ? 'A' : 'a',
        'é' or 'è' or 'ë' or 'ê' => char.IsUpper(c) ? 'E' : 'e',
        'í' or 'ì' or 'ï' or 'î' => char.IsUpper(c) ? 'I' : 'i',
        'ó' or 'ò' or 'ö' or 'ô' => char.IsUpper(c) ? 'O' : 'o',
        'ú' or 'ù' or 'ü' or 'û' => char.IsUpper(c) ? 'U' : 'u',
        'ñ' => char.IsUpper(c) ? 'N' : 'n',
        'ç' => char.IsUpper(c) ? 'C' : 'c',
        _ => null
    };
}
