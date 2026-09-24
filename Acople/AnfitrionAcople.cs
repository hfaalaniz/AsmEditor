using System.ComponentModel;
using AsmEditor.Core.Acople;

namespace AsmEditor;

/// <summary>
/// El acople de paneles, como el de Visual Studio: tres zonas (izquierda,
/// derecha, abajo) alrededor del centro de documentos, con divisores para
/// redimensionarlas y pestañas cuando una zona tiene varios paneles.
///
/// El diseño (qué panel está en qué zona, cuál es el activo, tamaños) es el
/// modelo <see cref="DisenoAcople"/> de Core, probado sin ventanas. Esta clase
/// solo lo DIBUJA (<see cref="Actualizar"/>) y le pasa lo que hace el usuario
/// (✕, pestañas, divisores). Avisa cada cambio con <see cref="DisenoCambiado"/>
/// (el menú Ver pone sus marcas; en 3f, se guarda).
///
/// La zona de abajo queda ENTRE las laterales, no a todo el ancho: es como lo
/// hace VS (lección del prototipo 0.4). Lo da el orden de los Controls.Add.
///
/// ⚠ EXCEPCIÓN A LA REGLA DEL DISEÑADOR ("Dock solo para cabecera y pie"): las
/// zonas, los divisores, el centro y lo que va dentro de cada zona usan Dock.
/// Un Splitter no funciona sin Dock, y las zonas tienen que redimensionarse con
/// él. Es el único lugar del editor (decidido en el plan, Etapa 0.4).
/// </summary>
public partial class AnfitrionAcople : UserControl
{
    /// <summary>Tamaños de fábrica: ancho de las laterales, alto de la de abajo.</summary>
    public const int AnchoIzquierda = 240;
    public const int AnchoDerecha = 280;
    public const int AltoAbajo = 220;

    private readonly Dictionary<string, VentanaHerramienta> _ventanas = new();
    private readonly Dictionary<string, (ZonaAcople Zona, bool Visible)> _deFabrica = new();
    private ToolStripRenderer? _rendererMenus;

    /// <summary>El diseño cambió (se mostró, ocultó o activó un panel, o se movió un divisor).</summary>
    public event EventHandler? DisenoCambiado;

    public AnfitrionAcople()
    {
        InitializeComponent();
        AplicarTema();
        Tema.TemaCambiado += AplicarTema;
        Disposed += AnfitrionAcople_Disposed;
    }

    /// <summary>El diseño actual. Para guardarlo; para cambiarlo, <see cref="Cargar"/>.</summary>
    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public DisenoAcople Diseno { get; private set; } = new();

    /// <summary>Donde van los documentos (las pestañas y la bienvenida).</summary>
    [Browsable(false)]
    public Control.ControlCollection Centro => pnlCentro.Controls;

    /// <summary>El renderer de los menús de los paneles, para que se vean como los del editor.</summary>
    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public ToolStripRenderer? RendererMenus
    {
        get => _rendererMenus;
        set
        {
            _rendererMenus = value;
            foreach (var v in _ventanas.Values) v.RendererMenus = value;
        }
    }

    /// <summary>
    /// Agrega un panel. La zona y la visibilidad son las de fábrica: si el
    /// diseño ya lo tenía (uno cargado), manda el diseño.
    /// </summary>
    public void Registrar(VentanaHerramienta v, ZonaAcople zona, bool visible = true)
    {
        if (string.IsNullOrWhiteSpace(v.Id)) throw new ArgumentException("La ventana necesita un Id.", nameof(v));

        _ventanas[v.Id] = v;
        _deFabrica[v.Id] = (zona, visible);
        Diseno.Registrar(v.Id, zona, visible);

        v.RendererMenus = _rendererMenus;
        v.OcultarPedido += Ventana_OcultarPedido;
        v.Activada += Ventana_Activada;

        Actualizar();
    }

    /// <summary>
    /// Reemplaza el diseño (uno guardado). Los paneles registrados que el
    /// diseño no conoce entran en su zona de fábrica.
    /// </summary>
    public void Cargar(DisenoAcople diseno)
    {
        diseno.EnsureValid();
        foreach (var (id, f) in _deFabrica) diseno.Registrar(id, f.Zona, f.Visible);

        Diseno = diseno;
        Actualizar();
    }

    public bool EstaVisible(string id) => Diseno.EstaVisible(id);

    /// <summary>Lo muestra en su zona, como pestaña activa. Con <paramref name="enfocar"/>, le da el foco.</summary>
    public void Mostrar(string id, bool enfocar = false)
    {
        Diseno.Mostrar(id);
        Actualizar();
        if (enfocar && _ventanas.TryGetValue(id, out var v)) v.Enfocar();
        Avisar();
    }

    public void Ocultar(string id)
    {
        Diseno.Ocultar(id);
        Actualizar();

        // Después de que termine lo que se está procesando (el clic en la ✕).
        if (IsHandleCreated) BeginInvoke(DevolverFocoSiQuedoAfuera);
        Avisar();
    }

    /// <summary>
    /// Si el foco quedó fuera de la ventana, vuelve a los documentos, como en
    /// VS al cerrar un panel.
    ///
    /// ⚠ SIN ESTO SE MUEREN LOS ATAJOS. Ocultar un panel lo saca de la ventana
    /// (WinForms lo estaciona en una ventana oculta) pero Windows le deja el
    /// foco a lo que lo tenía, la ✕ que se acaba de apretar: las teclas le
    /// llegan a un control que ya no cuelga del formulario, y Ctrl+B, Ctrl+S,
    /// F7... nunca llegan al menú. Medido el 23/09 con acople.ps1.
    ///
    /// ⚠ SelectNextControl NO ALCANZA: elige el control (lo anota como activo)
    /// pero, como el formulario ya no tiene el foco, no se lo pasa a Windows.
    /// Hay que darle Focus() al elegido. Medido el 23/09 con
    /// diagnostico\foco_al_ocultar.ps1, dentro del proceso y sin ratón.
    /// </summary>
    private void DevolverFocoSiQuedoAfuera()
    {
        var form = FindForm();
        if (form is null || form.ContainsFocus || form != Form.ActiveForm) return;

        if (pnlCentro.SelectNextControl(null, true, true, true, true))
        {
            // El elegido puede estar dentro de otro contenedor (la bienvenida):
            // se baja hasta el control de verdad.
            Control? elegido = ActiveControl;
            while (elegido is ContainerControl { ActiveControl: { } interno }) elegido = interno;
            if (elegido is not null && elegido.Focus()) return;
        }

        // Nada que enfocar en el centro: la ventana misma, soltando antes el
        // control activo viejo (si no, al recibir el foco se lo devuelve).
        form.ActiveControl = null;
        form.Focus();
    }

    /// <summary>Lo oculta si se ve; si no, lo muestra (Ctrl+B y el menú Ver).</summary>
    public void Alternar(string id)
    {
        if (EstaVisible(id)) Ocultar(id);
        else Mostrar(id);
    }

    // ------------------------------------------------------------------
    // Del modelo a la pantalla
    // ------------------------------------------------------------------

    /// <summary>
    /// Acomoda todo según el diseño: qué paneles tiene cada zona, cuál se ve,
    /// qué zonas ocupan lugar y cuánto miden.
    /// </summary>
    private void Actualizar()
    {
        SuspendLayout();

        foreach (var z in Enum.GetValues<ZonaAcople>())
        {
            var ventanas = Diseno.VisiblesEn(z)
                .Where(_ventanas.ContainsKey)
                .Select(id => _ventanas[id])
                .ToList();

            var activo = Diseno.ActivoEn(z);
            GrupoDe(z).Mostrar(ventanas, activo is not null && _ventanas.TryGetValue(activo, out var a) ? a : null);

            bool ocupada = ventanas.Count > 0;
            ZonaDe(z).Visible = ocupada;
            DivisorDe(z).Visible = ocupada;

            if (z == ZonaAcople.Abajo) ZonaDe(z).Height = Diseno.TamanoDe(z, AltoAbajo);
            else ZonaDe(z).Width = Diseno.TamanoDe(z, z == ZonaAcople.Izquierda ? AnchoIzquierda : AnchoDerecha);
        }

        // Los ocultos no quedan colgados de ningún grupo (GrupoHerramientas.Mostrar
        // ya los sacó del suyo); siguen vivos para volver.
        ResumeLayout();
    }

    private void Avisar() => DisenoCambiado?.Invoke(this, EventArgs.Empty);

    // ------------------------------------------------------------------
    // Lo que hace el usuario
    // ------------------------------------------------------------------

    private void Ventana_OcultarPedido(object? sender, EventArgs e)
    {
        if (sender is VentanaHerramienta v) Ocultar(v.Id);
    }

    /// <summary>El foco entró a un panel: queda como el activo de su zona (no cambia nada visible).</summary>
    private void Ventana_Activada(object? sender, EventArgs e)
    {
        if (sender is VentanaHerramienta v) Diseno.Activar(v.Id);
    }

    private void Grupo_PestanaElegida(object? sender, VentanaHerramienta v)
    {
        Diseno.Activar(v.Id);
        Actualizar();
        v.Enfocar();
        Avisar();
    }

    private void Divisor_SplitterMoved(object? sender, SplitterEventArgs e)
    {
        foreach (var z in Enum.GetValues<ZonaAcople>())
        {
            if (!ReferenceEquals(sender, DivisorDe(z))) continue;
            Diseno.FijarTamano(z, z == ZonaAcople.Abajo ? ZonaDe(z).Height : ZonaDe(z).Width);
        }
        Avisar();
    }

    // ------------------------------------------------------------------
    // Zonas
    // ------------------------------------------------------------------

    private Panel ZonaDe(ZonaAcople z) => z switch
    {
        ZonaAcople.Izquierda => zonaIzquierda,
        ZonaAcople.Derecha => zonaDerecha,
        _ => zonaAbajo
    };

    private GrupoHerramientas GrupoDe(ZonaAcople z) => z switch
    {
        ZonaAcople.Izquierda => grupoIzquierda,
        ZonaAcople.Derecha => grupoDerecha,
        _ => grupoAbajo
    };

    private Splitter DivisorDe(ZonaAcople z) => z switch
    {
        ZonaAcople.Izquierda => divisorIzquierda,
        ZonaAcople.Derecha => divisorDerecha,
        _ => divisorAbajo
    };

    // ------------------------------------------------------------------
    // Aspecto
    // ------------------------------------------------------------------

    /// <summary>Los divisores tienen el color del fondo: se ven como la separación entre paneles, como en VS.</summary>
    private void AplicarTema()
    {
        BackColor = Tema.Fondo;
        pnlCentro.BackColor = Tema.Fondo;
        foreach (var d in new[] { divisorIzquierda, divisorDerecha, divisorAbajo }) d.BackColor = Tema.Fondo;
    }

    private void AnfitrionAcople_Disposed(object? sender, EventArgs e) => Tema.TemaCambiado -= AplicarTema;
}
