namespace AsmEditor.Core.Disenador;

/// <summary>Qué clase de ventana es el formulario. Cambia el código generado entero.</summary>
public enum TipoFormulario
{
    /// <summary>
    /// Ventana principal: se registra una clase con RegisterClassExA, se crea
    /// con CreateWindowExA y lleva su propio bucle de mensajes con GetMessageA.
    /// Es la ventana que ABRE el programa.
    /// </summary>
    VentanaPrincipal,

    /// <summary>
    /// Diálogo: NO registra clase ni tiene bucle propio. Se crea con
    /// CreateWindowExA sobre una clase ya existente y corre dentro del bucle
    /// de la ventana principal.
    ///
    /// ⚠ NO ES UN DialogBoxParamA CON RECURSO .rc: este diseñador genera
    /// código, no recursos, así que un "diálogo" acá es una ventana hija
    /// creada por código. La diferencia con la principal es que no arranca el
    /// programa ni corre su propio bucle.
    /// </summary>
    Dialogo
}

/// <summary>
/// Un formulario completo: la ventana y los controles que tiene encima.
/// Es la fuente de verdad que se guarda en el .asmform y de la que salen
/// TODOS los archivos generados.
///
/// No conoce WinForms ni el canvas: es solo datos, para que los generadores
/// se puedan probar sin abrir una ventana.
/// </summary>
public sealed class FormularioDisenado
{
    /// <summary>
    /// Nombre del formulario. De él salen los símbolos del código generado
    /// (la clase de ventana, el procedimiento, la tabla de controles), así que
    /// se valida igual que el de un control.
    /// </summary>
    public string Nombre { get; set; } = "Formulario1";

    public TipoFormulario Tipo { get; set; } = TipoFormulario.VentanaPrincipal;

    /// <summary>Texto de la barra de título.</summary>
    public string Titulo { get; set; } = "Ventana";

    /// <summary>Medidas del área de cliente, en píxeles.</summary>
    public int Ancho { get; set; } = 480;
    public int Alto { get; set; } = 320;

    /// <summary>Ventana de tamaño fijo: sin borde elástico ni maximizar.</summary>
    public bool TamanoFijo { get; set; }

    /// <summary>
    /// Arquitectura para la que se genera el código.
    ///
    /// ⚠ NO ES UN DETALLE DE SALIDA: cambia la convención de llamada entera.
    /// En x64 los argumentos van en RCX/RDX/R8/R9 con 32 bytes de shadow space;
    /// en x86 van todos apilados al revés y los limpia la función llamada.
    /// El generador emite código DISTINTO, no el mismo con otro -f.
    /// </summary>
    public TargetArch Arquitectura { get; set; } = TargetArch.Win64;

    /// <summary>Los controles, en orden de creación (que es el orden de tabulación).</summary>
    public List<ControlDisenado> Controles { get; set; } = new();

    /// <summary>
    /// Primer ID que se le asigna a un control. Arranca en 1001 por costumbre
    /// de Win32: los valores bajos los usa el propio Windows (IDOK=1,
    /// IDCANCEL=2...) y pisarlos trae comportamientos raros.
    /// </summary>
    public const int PrimerId = 1001;

    // ------------------------------------------------------------------
    // Símbolos que el generador emite a partir del nombre
    // ------------------------------------------------------------------

    private string NombreLimpio => string.IsNullOrWhiteSpace(Nombre) ? "Formulario1" : Nombre.Trim();

    /// <summary>Etiqueta del procedimiento de ventana: "Formulario1Proc".</summary>
    public string SimboloProc => NombreLimpio + "Proc";

    /// <summary>Etiqueta de la rutina que crea los controles.</summary>
    public string SimboloCrearControles => "Crear" + NombreLimpio + "Controles";

    /// <summary>Etiqueta de la tabla de controles.</summary>
    public string SimboloTabla => "tabla" + NombreLimpio;

    /// <summary>Nombre de la clase de ventana que se registra.</summary>
    public string NombreClaseVentana => NombreLimpio + "Clase";

    /// <summary>Variable que guarda el hWnd de esta ventana.</summary>
    public string SimboloHwnd => "hWnd" + NombreLimpio;

    /// <summary>El estilo de la ventana, con nombres de winuser.h.</summary>
    public string ExpresionEstiloVentana =>
        TamanoFijo
            ? "WS_CAPTION|WS_SYSMENU|WS_MINIMIZEBOX"
            : "WS_OVERLAPPEDWINDOW";

    public bool EsVentanaPrincipal => Tipo == TipoFormulario.VentanaPrincipal;

    public bool EsX64 => Arquitectura == TargetArch.Win64;

    // ------------------------------------------------------------------
    // Operaciones sobre los controles
    // ------------------------------------------------------------------

    /// <summary>
    /// Le da un Id a todo control que no tenga, sin tocar los que ya tienen
    /// uno. Los Ids ya asignados NO SE REORDENAN NUNCA: el usuario pudo haber
    /// escrito código que compara contra ese número, y renumerar se lo rompe
    /// en silencio.
    /// </summary>
    public void AsignarIdsFaltantes()
    {
        var usados = new HashSet<int>(Controles.Where(c => c.Id != 0).Select(c => c.Id));

        var siguiente = PrimerId;

        foreach (var c in Controles.Where(c => c.Id == 0))
        {
            while (usados.Contains(siguiente)) siguiente++;
            c.Id = siguiente;
            usados.Add(siguiente);
        }
    }

    /// <summary>
    /// Un nombre libre para un control nuevo del tipo dado ("boton1", "boton2"...).
    /// </summary>
    public string ProponerNombre(TipoControl tipo)
    {
        var raiz = ControlDisenado.SanearNombre(InfoTipoControl.Nombre(tipo).Split(' ')[0])
                                  .ToLowerInvariant();

        var usados = new HashSet<string>(
            Controles.Select(c => c.Nombre.Trim()),
            StringComparer.OrdinalIgnoreCase);

        for (int i = 1; ; i++)
        {
            var candidato = raiz + i;
            if (!usados.Contains(candidato)) return candidato;
        }
    }

    /// <summary>
    /// Agrega un control con nombre e Id ya resueltos y el tamaño por defecto
    /// de su tipo. Es la vía por la que el canvas crea controles: así no hay
    /// forma de meter uno sin nombre o con Id repetido.
    /// </summary>
    public ControlDisenado AgregarControl(TipoControl tipo, int x, int y)
    {
        var (ancho, alto) = InfoTipoControl.TamanoPorDefecto(tipo);

        var c = new ControlDisenado
        {
            Tipo = tipo,
            Nombre = ProponerNombre(tipo),
            Texto = TextoInicial(tipo),
            X = x,
            Y = y,
            Ancho = ancho,
            Alto = alto
        };

        Controles.Add(c);
        AsignarIdsFaltantes();

        return c;
    }

    private static string TextoInicial(TipoControl tipo) => tipo switch
    {
        TipoControl.Etiqueta        => "Etiqueta",
        TipoControl.Boton           => "Botón",
        TipoControl.Casilla         => "Casilla",
        TipoControl.Opcion          => "Opción",
        TipoControl.Grupo           => "Marco",
        _ => ""
    };

    // ------------------------------------------------------------------
    // Validación
    // ------------------------------------------------------------------

    /// <summary>
    /// Todo lo que impediría generar código que ensamble. Lista vacía = se
    /// puede generar.
    ///
    /// ⚠ SE VALIDA ACÁ, NO AL GENERAR: un generador que valida es un generador
    /// que puede fallar a mitad y dejar un .asm truncado. Acá se decide si se
    /// genera; el generador solo escribe.
    /// </summary>
    public List<string> Validar()
    {
        var errores = new List<string>();

        if (!ControlDisenado.NombreValido(Nombre))
        {
            var razon = ControlDisenado.ExplicarNombreInvalido(Nombre);
            errores.Add($"El nombre del formulario no sirve: {razon}");
        }

        var vistos = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var ids = new Dictionary<int, string>();

        foreach (var c in Controles)
        {
            if (!ControlDisenado.NombreValido(c.Nombre))
            {
                var razon = ControlDisenado.ExplicarNombreInvalido(c.Nombre);
                var cual = string.IsNullOrWhiteSpace(c.Nombre) ? "(sin nombre)" : c.Nombre;
                errores.Add($"Control {cual}: {razon}");
                continue;
            }

            var nom = c.Nombre.Trim();

            // Se comparan sin distinguir mayúsculas porque el símbolo IDC_ sale
            // en mayúsculas: "boton" y "Boton" darían el mismo IDC_BOTON.
            if (vistos.TryGetValue(nom, out _))
            {
                errores.Add($"El nombre '{nom}' está repetido: cada control necesita uno propio " +
                            $"(los dos generarían {c.SimboloId}).");
            }
            else
            {
                vistos[nom] = c.Id;
            }

            if (c.Id != 0)
            {
                if (ids.TryGetValue(c.Id, out var otro))
                {
                    errores.Add($"El identificador {c.Id} está repetido entre '{otro}' y '{nom}'.");
                }
                else
                {
                    ids[c.Id] = nom;
                }
            }

            if (c.Tipo == TipoControl.Personalizado && string.IsNullOrWhiteSpace(c.ClasePersonalizada))
            {
                errores.Add($"Control '{nom}': es de clase personalizada pero no tiene " +
                            $"nombre de clase. Escribí la clase que registrás con RegisterClassExA.");
            }

            if (c.Ancho <= 0 || c.Alto <= 0)
            {
                errores.Add($"Control '{nom}': ancho y alto tienen que ser mayores que cero.");
            }
        }

        return errores;
    }

    public FormularioDisenado Clonar() => new()
    {
        Nombre = Nombre,
        Tipo = Tipo,
        Titulo = Titulo,
        Ancho = Ancho,
        Alto = Alto,
        TamanoFijo = TamanoFijo,
        Arquitectura = Arquitectura,
        Controles = Controles.Select(c => c.Clonar()).ToList()
    };
}
