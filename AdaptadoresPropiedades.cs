using System.ComponentModel;
using AsmEditor.Core;
using AsmEditor.Core.Disenador;

namespace AsmEditor;

/// <summary>
/// Lo que el PropertyGrid muestra de un control seleccionado.
///
/// ⚠ ES UN ADAPTADOR, NO EL MODELO. El PropertyGrid necesita atributos de
/// presentación ([Category], [Description], [DisplayName]) y validación al
/// escribir, y nada de eso tiene que vivir en <see cref="ControlDisenado"/>:
/// ese modelo se compila también en el proyecto de pruebas, que no referencia
/// WinForms. Este adaptador queda del lado de la UI y escribe sobre el modelo.
/// </summary>
public sealed class AdaptadorControl
{
    private readonly ControlDisenado _c;
    private readonly FormularioDisenado _f;

    public AdaptadorControl(ControlDisenado c, FormularioDisenado f)
    {
        _c = c;
        _f = f;
    }

    [Category("1 · Identidad")]
    [DisplayName("Nombre")]
    [Description("El nombre del control. De acá sale el símbolo IDC_ que usás " +
                 "en tu código. Tiene que servir como identificador de " +
                 "ensamblador: letras sin acento, dígitos y guion bajo.")]
    public string Nombre
    {
        get => _c.Nombre;
        set
        {
            var nuevo = (value ?? "").Trim();

            // ⚠ SE VALIDA AL ESCRIBIR, NO AL GENERAR: un nombre inválido
            // aceptado acá reaparece como un error de NASM mucho después, lejos
            // de donde se causó.
            var motivo = ControlDisenado.ExplicarNombreInvalido(nuevo);

            if (motivo is not null)
            {
                throw new ArgumentException(motivo);
            }

            bool repetido = _f.Controles.Any(
                o => !ReferenceEquals(o, _c) &&
                     string.Equals(o.Nombre.Trim(), nuevo, StringComparison.OrdinalIgnoreCase));

            if (repetido)
            {
                throw new ArgumentException(
                    $"Ya hay otro control que se llama '{nuevo}'. " +
                    $"Los dos generarían el símbolo IDC_{nuevo.ToUpperInvariant()}.");
            }

            _c.Nombre = nuevo;
        }
    }

    [Category("1 · Identidad")]
    [DisplayName("Tipo")]
    [Description("Qué clase de control es. Cambiarlo cambia la clase de ventana " +
                 "de Windows con la que se crea.")]
    public TipoControl Tipo
    {
        get => _c.Tipo;
        set => _c.Tipo = value;
    }

    [Category("1 · Identidad")]
    [DisplayName("Identificador")]
    [Description("El número que llega en WM_COMMAND y que se le pasa a GetDlgItem. " +
                 "Solo cambialo si tenés una razón: si ya escribiste código que " +
                 "lo compara, cambiarlo lo rompe.")]
    public int Id
    {
        get => _c.Id;
        set
        {
            if (value <= 0)
            {
                throw new ArgumentException("El identificador tiene que ser mayor que cero.");
            }

            // Windows usa los valores bajos para sus propios botones (IDOK=1,
            // IDCANCEL=2): pisarlos trae comportamientos raros y difíciles de
            // atribuir.
            if (value < 100)
            {
                throw new ArgumentException(
                    "Los identificadores por debajo de 100 los usa Windows " +
                    "(IDOK, IDCANCEL y compañía). Usá 1001 o más.");
            }

            var otro = _f.Controles.FirstOrDefault(
                o => !ReferenceEquals(o, _c) && o.Id == value);

            if (otro is not null)
            {
                throw new ArgumentException($"El identificador {value} ya lo usa '{otro.Nombre}'.");
            }

            _c.Id = value;
        }
    }

    [Category("1 · Identidad")]
    [DisplayName("Clase personalizada")]
    [Description("Solo para el tipo Personalizado: el nombre de la clase que " +
                 "registrás con RegisterClassExA antes de crear la ventana.")]
    public string ClasePersonalizada
    {
        get => _c.ClasePersonalizada;
        set => _c.ClasePersonalizada = (value ?? "").Trim();
    }

    [Category("2 · Apariencia")]
    [DisplayName("Texto")]
    [Description("Lo que muestra el control.")]
    public string Texto
    {
        get => _c.Texto;
        set => _c.Texto = value ?? "";
    }

    [Category("3 · Posición")]
    [Description("Distancia desde el borde izquierdo del área de cliente, en píxeles.")]
    public int X
    {
        get => _c.X;
        set => _c.X = value;
    }

    [Category("3 · Posición")]
    [Description("Distancia desde el borde superior del área de cliente, en píxeles.")]
    public int Y
    {
        get => _c.Y;
        set => _c.Y = value;
    }

    [Category("3 · Posición")]
    [Description("Ancho en píxeles.")]
    public int Ancho
    {
        get => _c.Ancho;
        set
        {
            if (value <= 0) throw new ArgumentException("El ancho tiene que ser mayor que cero.");
            _c.Ancho = value;
        }
    }

    [Category("3 · Posición")]
    [Description("Alto en píxeles.")]
    public int Alto
    {
        get => _c.Alto;
        set
        {
            if (value <= 0) throw new ArgumentException("El alto tiene que ser mayor que cero.");
            _c.Alto = value;
        }
    }

    [Category("4 · Estilo")]
    [DisplayName("Estilos extra")]
    [Description("Estilos de winuser.h que se suman a los del tipo, separados " +
                 "por |. Por ejemplo BS_DEFPUSHBUTTON o ES_PASSWORD. Van tal " +
                 "cual al .asm: si no existen, el error lo da NASM.")]
    public string EstilosExtra
    {
        get => _c.EstilosExtra;
        set => _c.EstilosExtra = (value ?? "").Trim();
    }

    [Category("4 · Estilo")]
    [DisplayName("Oculto al inicio")]
    [Description("El control se crea sin WS_VISIBLE: existe pero no se ve hasta " +
                 "que tu código lo muestre con ShowWindow.")]
    public bool OcultoAlInicio
    {
        get => _c.OcultoAlInicio;
        set => _c.OcultoAlInicio = value;
    }

    [Category("4 · Estilo")]
    [DisplayName("Deshabilitado al inicio")]
    [Description("El control se crea con WS_DISABLED: se ve gris y no responde.")]
    public bool DeshabilitadoAlInicio
    {
        get => _c.DeshabilitadoAlInicio;
        set => _c.DeshabilitadoAlInicio = value;
    }

    [Category("5 · Generado")]
    [DisplayName("Símbolo")]
    [Description("El símbolo que se emite al .inc y que usás en tu código. " +
                 "Sale del nombre; no se edita directo.")]
    [ReadOnly(true)]
    public string Simbolo => _c.SimboloId;

    [Category("5 · Generado")]
    [DisplayName("Clase de Windows")]
    [Description("La clase de ventana con la que se llama a CreateWindowExA.")]
    [ReadOnly(true)]
    public string ClaseWin32 => _c.ClaseEfectiva;

    [Category("5 · Generado")]
    [DisplayName("Estilo completo")]
    [Description("La expresión de estilo tal como va a quedar en el .asm.")]
    [ReadOnly(true)]
    public string EstiloCompleto => _c.ExpresionEstilo();

    public override string ToString() => $"{_c.Nombre} ({InfoTipoControl.Nombre(_c.Tipo)})";
}

/// <summary>
/// Lo que el PropertyGrid muestra del formulario cuando no hay ningún control
/// seleccionado.
/// </summary>
public sealed class AdaptadorFormulario
{
    private readonly FormularioDisenado _f;

    public AdaptadorFormulario(FormularioDisenado f) => _f = f;

    [Category("1 · Identidad")]
    [DisplayName("Nombre")]
    [Description("El nombre del formulario. De él salen los símbolos del código " +
                 "generado y los nombres de los archivos .inc y .asm.")]
    public string Nombre
    {
        get => _f.Nombre;
        set
        {
            var nuevo = (value ?? "").Trim();
            var motivo = ControlDisenado.ExplicarNombreInvalido(nuevo);

            if (motivo is not null) throw new ArgumentException(motivo);

            _f.Nombre = nuevo;
        }
    }

    [Category("1 · Identidad")]
    [DisplayName("Tipo")]
    [Description("Ventana principal: registra su clase, tiene punto de entrada y " +
                 "bucle de mensajes propio. Diálogo: es una ventana hija que corre " +
                 "dentro del bucle de la principal.")]
    public TipoFormulario Tipo
    {
        get => _f.Tipo;
        set => _f.Tipo = value;
    }

    [Category("1 · Identidad")]
    [DisplayName("Arquitectura")]
    [Description("Para qué arquitectura se genera el código. NO es solo el -f de " +
                 "NASM: cambia la convención de llamada, así que el código " +
                 "generado es distinto.")]
    public TargetArch Arquitectura
    {
        get => _f.Arquitectura;
        set => _f.Arquitectura = value;
    }

    [Category("2 · Apariencia")]
    [DisplayName("Título")]
    [Description("El texto de la barra de título.")]
    public string Titulo
    {
        get => _f.Titulo;
        set => _f.Titulo = value ?? "";
    }

    [Category("3 · Tamaño")]
    [Description("Ancho del área de cliente en píxeles. La ventana real va a ser " +
                 "un poco más ancha: los bordes van por fuera.")]
    public int Ancho
    {
        get => _f.Ancho;
        set
        {
            if (value < 40) throw new ArgumentException("El ancho mínimo es 40 píxeles.");
            _f.Ancho = value;
        }
    }

    [Category("3 · Tamaño")]
    [Description("Alto del área de cliente en píxeles, sin contar la barra de título.")]
    public int Alto
    {
        get => _f.Alto;
        set
        {
            if (value < 40) throw new ArgumentException("El alto mínimo es 40 píxeles.");
            _f.Alto = value;
        }
    }

    [Category("3 · Tamaño")]
    [DisplayName("Tamaño fijo")]
    [Description("La ventana no se puede redimensionar ni maximizar.")]
    public bool TamanoFijo
    {
        get => _f.TamanoFijo;
        set => _f.TamanoFijo = value;
    }

    [Category("4 · Generado")]
    [DisplayName("Procedimiento")]
    [Description("La etiqueta del procedimiento de ventana que se genera en el .asm.")]
    [ReadOnly(true)]
    public string Procedimiento => _f.SimboloProc;

    [Category("4 · Generado")]
    [DisplayName("Estilo de la ventana")]
    [ReadOnly(true)]
    public string EstiloVentana => _f.ExpresionEstiloVentana;

    [Category("4 · Generado")]
    [DisplayName("Controles")]
    [ReadOnly(true)]
    public int CantidadDeControles => _f.Controles.Count;

    public override string ToString() => $"{_f.Nombre} (formulario)";
}
