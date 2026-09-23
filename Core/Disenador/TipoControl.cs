namespace AsmEditor.Core.Disenador;

/// <summary>
/// Los tipos de control que el diseñador sabe crear.
///
/// ⚠ NO SON "WIDGETS": cada uno es una CLASE DE VENTANA REGISTRADA POR WINDOWS,
/// y el nombre de esa clase es lo único que Windows entiende. Un botón, una
/// casilla y un radio son los TRES la clase "BUTTON": lo que los diferencia es
/// el estilo (BS_*), no la clase. Por eso <see cref="ClaseWin32"/> y
/// <see cref="EstiloBase"/> van juntos y siempre se emiten juntos.
/// </summary>
public enum TipoControl
{
    /// <summary>Texto fijo. Clase STATIC.</summary>
    Etiqueta,

    /// <summary>Campo de texto de una línea. Clase EDIT.</summary>
    Campo,

    /// <summary>Campo de texto multilínea. Clase EDIT con ES_MULTILINE.</summary>
    CampoMultilinea,

    /// <summary>Botón de comando. Clase BUTTON.</summary>
    Boton,

    /// <summary>Casilla de verificación. Clase BUTTON con BS_AUTOCHECKBOX.</summary>
    Casilla,

    /// <summary>Botón de opción. Clase BUTTON con BS_AUTORADIOBUTTON.</summary>
    Opcion,

    /// <summary>Marco agrupador. Clase BUTTON con BS_GROUPBOX.</summary>
    Grupo,

    /// <summary>Lista. Clase LISTBOX.</summary>
    Lista,

    /// <summary>Lista desplegable. Clase COMBOBOX.</summary>
    Desplegable,

    /// <summary>
    /// Control de clase propia: el nombre de la clase lo pone el usuario y
    /// ES ÉL quien debe registrarla con RegisterClassExA antes de que se cree
    /// la ventana. Ver <see cref="ControlDisenado.ClasePersonalizada"/>.
    /// </summary>
    Personalizado
}

/// <summary>
/// Lo que cada tipo de control necesita para existir en Win32: su clase
/// registrada y los estilos que lo definen.
///
/// Está separado del enum porque es una TABLA DE DATOS DE LA API DE WINDOWS,
/// no una preferencia del editor: estos valores salen de winuser.h y no se
/// eligen, se respetan.
/// </summary>
public static class InfoTipoControl
{
    /// <summary>
    /// El nombre de clase Win32 de cada tipo. Es el segundo argumento de
    /// CreateWindowExA y Windows lo compara literalmente.
    /// </summary>
    public static string ClaseWin32(TipoControl tipo) => tipo switch
    {
        TipoControl.Etiqueta        => "STATIC",
        TipoControl.Campo           => "EDIT",
        TipoControl.CampoMultilinea => "EDIT",
        TipoControl.Boton           => "BUTTON",
        TipoControl.Casilla         => "BUTTON",
        TipoControl.Opcion          => "BUTTON",
        TipoControl.Grupo           => "BUTTON",
        TipoControl.Lista           => "LISTBOX",
        TipoControl.Desplegable     => "COMBOBOX",
        TipoControl.Personalizado   => "",
        _ => "STATIC"
    };

    /// <summary>
    /// Los estilos que hacen que el control SEA ese control, además de
    /// WS_CHILD|WS_VISIBLE que llevan todos.
    ///
    /// Valores de winuser.h:
    ///   BS_PUSHBUTTON 0x0000   BS_AUTOCHECKBOX 0x0003
    ///   BS_AUTORADIOBUTTON 0x0009   BS_GROUPBOX 0x0007
    ///   ES_AUTOHSCROLL 0x0080   ES_MULTILINE 0x0004
    ///   WS_BORDER 0x00800000   WS_VSCROLL 0x00200000   WS_TABSTOP 0x00010000
    ///   CBS_DROPDOWNLIST 0x0003
    /// </summary>
    public static uint EstiloBase(TipoControl tipo) => tipo switch
    {
        TipoControl.Etiqueta        => 0,
        TipoControl.Campo           => EstilosWin32.WS_BORDER | EstilosWin32.WS_TABSTOP | 0x0080,
        TipoControl.CampoMultilinea => EstilosWin32.WS_BORDER | EstilosWin32.WS_TABSTOP |
                                       EstilosWin32.WS_VSCROLL | 0x0004,
        TipoControl.Boton           => EstilosWin32.WS_TABSTOP,
        TipoControl.Casilla         => EstilosWin32.WS_TABSTOP | 0x0003,
        TipoControl.Opcion          => EstilosWin32.WS_TABSTOP | 0x0009,
        TipoControl.Grupo           => 0x0007,
        TipoControl.Lista           => EstilosWin32.WS_BORDER | EstilosWin32.WS_VSCROLL,
        TipoControl.Desplegable     => EstilosWin32.WS_TABSTOP | EstilosWin32.WS_VSCROLL | 0x0003,
        TipoControl.Personalizado   => 0,
        _ => 0
    };

    /// <summary>
    /// Los estilos base escritos con los NOMBRES de winuser.h, para que el .asm
    /// generado se lea como lo escribiría una persona y no como números mágicos.
    /// El valor numérico y este texto DEBEN corresponderse: los fija el mismo
    /// switch de arriba y hay una prueba que los compara.
    /// </summary>
    public static string EstiloBaseTexto(TipoControl tipo) => tipo switch
    {
        TipoControl.Etiqueta        => "",
        TipoControl.Campo           => "WS_BORDER|WS_TABSTOP|ES_AUTOHSCROLL",
        TipoControl.CampoMultilinea => "WS_BORDER|WS_TABSTOP|WS_VSCROLL|ES_MULTILINE",
        TipoControl.Boton           => "WS_TABSTOP",
        TipoControl.Casilla         => "WS_TABSTOP|BS_AUTOCHECKBOX",
        TipoControl.Opcion          => "WS_TABSTOP|BS_AUTORADIOBUTTON",
        TipoControl.Grupo           => "BS_GROUPBOX",
        TipoControl.Lista           => "WS_BORDER|WS_VSCROLL",
        TipoControl.Desplegable     => "WS_TABSTOP|WS_VSCROLL|CBS_DROPDOWNLIST",
        TipoControl.Personalizado   => "",
        _ => ""
    };

    /// <summary>Nombre para la paleta y el árbol de controles.</summary>
    public static string Nombre(TipoControl tipo) => tipo switch
    {
        TipoControl.Etiqueta        => "Etiqueta",
        TipoControl.Campo           => "Campo de texto",
        TipoControl.CampoMultilinea => "Campo multilínea",
        TipoControl.Boton           => "Botón",
        TipoControl.Casilla         => "Casilla",
        TipoControl.Opcion          => "Opción",
        TipoControl.Grupo           => "Marco",
        TipoControl.Lista           => "Lista",
        TipoControl.Desplegable     => "Desplegable",
        TipoControl.Personalizado   => "Personalizado",
        _ => "Control"
    };

    /// <summary>
    /// Tamaño inicial razonable al soltar el control en el canvas, en píxeles.
    /// </summary>
    public static (int ancho, int alto) TamanoPorDefecto(TipoControl tipo) => tipo switch
    {
        TipoControl.Etiqueta        => (80, 20),
        TipoControl.Campo           => (160, 24),
        TipoControl.CampoMultilinea => (200, 80),
        TipoControl.Boton           => (100, 30),
        TipoControl.Casilla         => (140, 24),
        TipoControl.Opcion          => (140, 24),
        TipoControl.Grupo           => (200, 120),
        TipoControl.Lista           => (180, 100),
        TipoControl.Desplegable     => (160, 200),
        TipoControl.Personalizado   => (120, 40),
        _ => (100, 24)
    };

    /// <summary>
    /// True si el control avisa por WM_COMMAND al interactuar. Define si el
    /// esqueleto generado le arma un manejador.
    ///
    /// Una etiqueta y un marco no avisan nunca; un campo avisa solo con
    /// estilos que el diseñador no pone por defecto, así que tampoco.
    /// </summary>
    public static bool NotificaPorComando(TipoControl tipo) => tipo switch
    {
        TipoControl.Boton       => true,
        TipoControl.Casilla     => true,
        TipoControl.Opcion      => true,
        TipoControl.Lista       => true,
        TipoControl.Desplegable => true,
        _ => false
    };
}

/// <summary>
/// Las constantes de winuser.h que usa el diseñador. Se declaran acá una sola
/// vez y se emiten al .asm generado, para que el código no tenga números
/// mágicos y el usuario pueda leerlo.
/// </summary>
public static class EstilosWin32
{
    public const uint WS_OVERLAPPED     = 0x00000000;
    public const uint WS_CHILD          = 0x40000000;
    public const uint WS_VISIBLE        = 0x10000000;
    public const uint WS_BORDER         = 0x00800000;
    public const uint WS_TABSTOP        = 0x00010000;
    public const uint WS_VSCROLL        = 0x00200000;
    public const uint WS_HSCROLL        = 0x00100000;
    public const uint WS_SYSMENU        = 0x00080000;
    public const uint WS_CAPTION        = 0x00C00000;
    public const uint WS_THICKFRAME     = 0x00040000;
    public const uint WS_MINIMIZEBOX    = 0x00020000;
    public const uint WS_MAXIMIZEBOX    = 0x00010000;

    /// <summary>WS_CAPTION|WS_SYSMENU|WS_THICKFRAME|WS_MINIMIZEBOX|WS_MAXIMIZEBOX</summary>
    public const uint WS_OVERLAPPEDWINDOW = 0x00CF0000;

    /// <summary>Ventana fija: sin borde redimensionable ni botón de maximizar.</summary>
    public const uint WS_VENTANA_FIJA = WS_CAPTION | WS_SYSMENU | WS_MINIMIZEBOX;
}
