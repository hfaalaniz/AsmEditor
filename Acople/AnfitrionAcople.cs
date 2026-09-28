using System.ComponentModel;
using AsmEditor.Core;
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
/// Auto-ocultar (la chincheta, 3c del plan): un panel auto-oculto no está en
/// su zona sino como pestaña en la franja del borde de ese lado
/// (<see cref="BordeAutoOcultos"/>, las más de afuera). Al pasar el ratón por
/// la pestaña se DESPLIEGA encima de todo (pnlDesplegado, sin Dock) y se
/// PLIEGA cuando el ratón se va y el foco no está adentro. Un clic lo
/// despliega al instante y con el foco. Solo hay uno desplegado a la vez.
///
/// Flotar (3d): arrastrar el título de un panel, hacerle doble clic, o
/// «Flotante» del ▾, lo pasa a una <see cref="VentanaFlotante"/> propia (una
/// por panel, del editor). Doble clic en el título flotando, o «Acoplar», lo
/// vuelve a su zona. Las flotantes se abren y se cierran en
/// <see cref="Actualizar"/>, según el modelo, como todo lo demás.
///
/// Acoplar arrastrando (3e): mientras una flotante se mueve (recién sacada o
/// no) se ven las <see cref="GuiasAcople"/> — una en cada borde y un rombo en
/// el centro de los documentos — y, con el ratón sobre una, la
/// <see cref="VistaPreviaAcople"/> de dónde quedaría. Soltada sobre una guía
/// se acopla en esa zona (como última pestaña, activa y con el foco); en
/// otro lado, sigue flotando. La geometría es <see cref="GeometriaAcople"/>.
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

    /// <summary>El auto-oculto que está desplegado (en pnlDesplegado), o null.</summary>
    private string? _desplegado;

    /// <summary>El que se va a desplegar cuando venza tmrDesplegar (el ratón sigue en su pestaña).</summary>
    private string? _porDesplegar;

    /// <summary>Las ventanas de los flotantes abiertos, por Id de panel.</summary>
    private readonly Dictionary<string, VentanaFlotante> _flotantes = new();

    /// <summary>Tamaño de una flotante que nunca se vio (ancho y alto de ventana).</summary>
    private static readonly Size TamanoFlotanteNuevo = new(300, 400);

    // Las guías y la vista previa de acoplar arrastrando (3e). Son ventanas
    // aparte (encima de todo, del editor), no controles del anfitrión: por
    // eso se crean acá y no en el diseñador. Se muestran solo mientras se
    // arrastra una flotante.
    private readonly GuiasAcople _guias = new();
    private readonly VistaPreviaAcople _vistaPrevia = new();

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
        v.ChinchetaPedida += Ventana_ChinchetaPedida;
        v.ArrastreIniciado += Ventana_ArrastreIniciado;
        v.FlotarPedido += Ventana_FlotarPedido;
        v.AcoplarPedido += Ventana_AcoplarPedido;
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

    /// <summary>
    /// Lo muestra en su zona, como pestaña activa. Con <paramref name="enfocar"/>, le da el foco.
    ///
    /// Si está auto-oculto, como en VS: lo DESPLIEGA desde el borde y le da el
    /// foco siempre (desplegado sin foco se plegaría apenas el ratón no esté
    /// encima, y pedir un panel para que no se vea no sirve). Si está
    /// flotante, trae su ventana al frente, también con el foco.
    /// </summary>
    public void Mostrar(string id, bool enfocar = false)
    {
        Diseno.Mostrar(id);
        Actualizar();

        if (Diseno.EstaAutoOculto(id)) Desplegar(id, enfocar: true);
        else if (Diseno.EstaFlotante(id)) ActivarFlotante(id);
        else if (enfocar && _ventanas.TryGetValue(id, out var v)) v.Enfocar();

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

    /// <summary>
    /// Ctrl+B y el menú Ver. Acoplado: lo oculta si se ve; si no, lo muestra.
    /// Auto-oculto (decisión de Fabián, 28/09: como VS): NO lo cierra; lo
    /// despliega con el foco, y si ya estaba desplegado lo pliega.
    /// Flotante (decisión de Fabián, 28/09): tampoco lo cierra; trae su
    /// ventana al frente con el foco, y si ya lo tenía el foco vuelve al
    /// editor (la ventana queda abierta).
    /// </summary>
    public void Alternar(string id)
    {
        if (!EstaVisible(id)) Mostrar(id);
        else if (Diseno.EstaFlotante(id))
        {
            if (_flotantes.TryGetValue(id, out var f) && f.ContainsFocus) FindForm()?.Activate();
            else ActivarFlotante(id);
        }
        else if (!Diseno.EstaAutoOculto(id)) Ocultar(id);
        else if (_desplegado == id) Plegar();
        else Desplegar(id, enfocar: true);
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
        // El desplegado que ya no es un auto-oculto a mano (se cerró con ✕, se
        // fijó con la chincheta, llegó otro diseño) se saca antes: si no, el
        // grupo de su zona no lo podría tomar.
        if (_desplegado is not null && !(Diseno.EstaVisible(_desplegado) && Diseno.EstaAutoOculto(_desplegado)))
            Plegar();

        // Lo mismo con las flotantes que ya no van (se acoplaron o se cerraron):
        // se cierran soltando el panel, que va a su zona o queda oculto.
        foreach (var id in _flotantes.Keys.Where(id => !(Diseno.EstaVisible(id) && Diseno.EstaFlotante(id))).ToList())
        {
            var f = _flotantes[id];
            _flotantes.Remove(id);
            if (!f.IsDisposed) f.Cerrar();
        }

        SuspendLayout();

        foreach (var (id, v) in _ventanas)
        {
            v.AutoOculta = Diseno.EstaAutoOculto(id);
            v.Flotante = Diseno.EstaFlotante(id);
        }

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

        // Los ocultos y los auto-ocultos no quedan colgados de ningún grupo
        // (GrupoHerramientas.Mostrar ya los sacó del suyo); siguen vivos para
        // volver o para desplegarse.
        MostrarBordes();
        ResumeLayout();

        if (_desplegado is not null) UbicarDesplegado();
        AbrirFlotantes();
    }

    /// <summary>
    /// Una ventana por flotante visible que todavía no la tenga, donde quedó
    /// la última vez. Sin el formulario del editor ya creado no se puede (sin
    /// dueño quedaría suelta): se abren en el próximo Actualizar.
    /// </summary>
    private void AbrirFlotantes()
    {
        var duenio = FindForm();
        if (duenio is null || !duenio.IsHandleCreated) return;

        foreach (var id in Diseno.FlotantesVisibles())
        {
            if (_flotantes.ContainsKey(id) || !_ventanas.TryGetValue(id, out var v)) continue;

            var f = new VentanaFlotante();
            f.CierrePedido += Flotante_CierrePedido;
            f.Movida += Flotante_Movida;
            f.Moviendo += Flotante_Moviendo;
            f.Soltada += Flotante_Soltada;
            f.Contener(v);
            f.Bounds = LimitesEnPantalla(id, duenio);
            _flotantes[id] = f;
            f.Show(duenio);
        }
    }

    /// <summary>
    /// Dónde va su ventana: donde quedó, o centrada en el editor si nunca se
    /// vio o si ya no la muestra ninguna pantalla (se desconectó un monitor).
    /// </summary>
    private Rectangle LimitesEnPantalla(string id, Form duenio)
    {
        var centrada = new Rectangle(
            duenio.Left + (duenio.Width - TamanoFlotanteNuevo.Width) / 2,
            duenio.Top + (duenio.Height - TamanoFlotanteNuevo.Height) / 2,
            TamanoFlotanteNuevo.Width, TamanoFlotanteNuevo.Height);

        if (Diseno.LimitesDe(id) is not { } l) return centrada;

        var r = new Rectangle(l.X, l.Y, l.Ancho, l.Alto);
        return Screen.AllScreens.Any(s => s.WorkingArea.IntersectsWith(r)) ? r : centrada;
    }

    /// <summary>
    /// Lo pasa a su ventana. Con <paramref name="bajoElRaton"/> (se arrastró el
    /// título), la ventana aparece con su título bajo el ratón, lista para
    /// que Windows siga el arrastre; si no, donde estaba el panel (o donde
    /// flotó la última vez).
    /// </summary>
    private void FlotarPanel(string id, Point? bajoElRaton)
    {
        if (!_ventanas.TryGetValue(id, out var v) || Diseno.EstaFlotante(id)) return;

        var aLaVista = v.Parent is not null && v.Visible && v.IsHandleCreated
            ? v.RectangleToScreen(v.ClientRectangle)
            : Rectangle.Empty;
        var previo = Diseno.LimitesDe(id);

        var tamano = previo is not null ? new Size(previo.Ancho, previo.Alto)
            : !aLaVista.IsEmpty ? aLaVista.Size
            : TamanoFlotanteNuevo;

        // El ratón a mitad del ancho y a mitad de la barra del panel (26 px,
        // debajo de la franja de 4 px que redimensiona). Sin ninguna
        // referencia queda null y la centra LimitesEnPantalla.
        Point? lugar = bajoElRaton is { } p ? new Point(p.X - tamano.Width / 2, p.Y - 4 - 13)
            : previo is not null ? new Point(previo.X, previo.Y)
            : !aLaVista.IsEmpty ? aLaVista.Location
            : null;

        Diseno.Flotar(id);
        if (lugar is { } l) Diseno.GuardarLimites(id, l.X, l.Y, tamano.Width, tamano.Height);

        Actualizar();
        Avisar();
    }

    /// <summary>Lo vuelve de su ventana a su zona, activo y con el foco.</summary>
    private void AcoplarPanel(string id)
    {
        if (!_ventanas.TryGetValue(id, out var v)) return;

        Diseno.Acoplar(id);
        Actualizar();

        // La flotante (la activa) se cerró: la ventana del editor vuelve a ser
        // la activa y el foco va al panel, ya en su zona.
        FindForm()?.Activate();
        v.Enfocar();
        Avisar();
    }

    /// <summary>Su ventana al frente, con el foco en el panel.</summary>
    private void ActivarFlotante(string id)
    {
        if (!_flotantes.TryGetValue(id, out var f)) return;
        f.Activate();
        f.Panel?.Enfocar();
    }

    /// <summary>Las franjas de los bordes: las pestañas de los auto-ocultos de cada lado.</summary>
    private void MostrarBordes()
    {
        foreach (var z in Enum.GetValues<ZonaAcople>())
        {
            var pestanas = Diseno.AutoOcultosEn(z)
                .Where(_ventanas.ContainsKey)
                .Select(id => (id, _ventanas[id].Titulo))
                .ToList();

            var borde = BordeDe(z);
            borde.Mostrar(pestanas, _desplegado);
            borde.Visible = pestanas.Count > 0;
        }
    }

    private void Avisar() => DisenoCambiado?.Invoke(this, EventArgs.Empty);

    // ------------------------------------------------------------------
    // Desplegar y plegar los auto-ocultos
    // ------------------------------------------------------------------

    /// <summary>
    /// Pone el auto-oculto encima de todo, pegado a la franja de su lado. Si
    /// había otro desplegado, lo reemplaza.
    /// </summary>
    private void Desplegar(string id, bool enfocar)
    {
        tmrDesplegar.Stop();
        _porDesplegar = null;

        if (!_ventanas.TryGetValue(id, out var v) || !Diseno.EstaVisible(id) || !Diseno.EstaAutoOculto(id)) return;

        if (_desplegado != id)
        {
            if (_desplegado is not null) SacarDesplegado();

            v.Dock = DockStyle.Fill;
            v.Visible = true;
            pnlDesplegado.Controls.Add(v);
            _desplegado = id;

            UbicarDesplegado();
            pnlDesplegado.Visible = true;
            MostrarBordes();
        }

        tmrPlegar.Start();
        if (enfocar) v.Enfocar();
    }

    /// <summary>
    /// Lo devuelve a su pestaña del borde. Si tenía el foco, vuelve a los
    /// documentos: la misma trampa de <see cref="DevolverFocoSiQuedoAfuera"/>
    /// (un control con el foco que sale de la ventana mata los atajos).
    /// </summary>
    private void Plegar()
    {
        tmrPlegar.Stop();
        if (_desplegado is null) return;

        bool teniaFoco = pnlDesplegado.ContainsFocus;
        SacarDesplegado();
        pnlDesplegado.Visible = false;
        MostrarBordes();

        if (teniaFoco && IsHandleCreated) BeginInvoke(DevolverFocoSiQuedoAfuera);
    }

    private void SacarDesplegado()
    {
        foreach (var v in pnlDesplegado.Controls.OfType<VentanaHerramienta>().ToList())
            pnlDesplegado.Controls.Remove(v);
        _desplegado = null;
    }

    /// <summary>
    /// Pegado a la franja de su lado y con el tamaño de su zona: los laterales
    /// a todo el alto (menos la franja de abajo), el de abajo entre las
    /// franjas laterales. Se calcula con el modelo y no con Visible, que da
    /// false mientras la ventana todavía no se mostró.
    /// </summary>
    private void UbicarDesplegado()
    {
        if (_desplegado is null || Diseno.ZonaDe(_desplegado) is not { } zona) return;

        var area = AreaDeZonas();

        pnlDesplegado.Bounds = zona switch
        {
            ZonaAcople.Izquierda => new Rectangle(area.Left, area.Top, Math.Min(Diseno.TamanoDe(zona, AnchoIzquierda), area.Width), area.Height),
            ZonaAcople.Derecha => AnchoDesde(area, Math.Min(Diseno.TamanoDe(zona, AnchoDerecha), area.Width)),
            _ => AltoDesde(area, Math.Min(Diseno.TamanoDe(zona, AltoAbajo), area.Height))
        };

        static Rectangle AnchoDesde(Rectangle a, int ancho) => new(a.Right - ancho, a.Top, ancho, a.Height);
        static Rectangle AltoDesde(Rectangle a, int alto) => new(a.Left, a.Bottom - alto, a.Width, alto);
    }

    /// <summary>
    /// Donde viven las zonas (en coordenadas del anfitrión): todo menos las
    /// franjas de los auto-ocultos. Se calcula con el modelo y no con Visible
    /// de las franjas, que da false mientras la ventana todavía no se mostró.
    /// </summary>
    private Rectangle AreaDeZonas()
    {
        int izq = Diseno.AutoOcultosEn(ZonaAcople.Izquierda).Count > 0 ? bordeIzquierda.Width : 0;
        int der = ClientSize.Width - (Diseno.AutoOcultosEn(ZonaAcople.Derecha).Count > 0 ? bordeDerecha.Width : 0);
        int abajo = ClientSize.Height - (Diseno.AutoOcultosEn(ZonaAcople.Abajo).Count > 0 ? bordeAbajo.Height : 0);
        return new Rectangle(izq, 0, Math.Max(0, der - izq), Math.Max(0, abajo));
    }

    // ------------------------------------------------------------------
    // Acoplar arrastrando (3e)
    // ------------------------------------------------------------------

    /// <summary>
    /// Una flotante se está moviendo: las guías sobre el editor, con la que
    /// está bajo el ratón resaltada y la vista previa de dónde quedaría. Con
    /// el editor minimizado no hay dónde acoplar: no se muestra nada.
    /// </summary>
    private void Flotante_Moviendo(object? sender, EventArgs e)
    {
        var duenio = FindForm();
        if (duenio is null || !IsHandleCreated || duenio.WindowState == FormWindowState.Minimized) return;

        var guias = GuiasEnPantalla();
        var raton = Control.MousePosition;
        var guia = GeometriaAcople.GuiaEn(raton.X, raton.Y, guias);

        _guias.Mostrar(RectangleToScreen(ClientRectangle), guias, GeometriaAcople.CentroDelRombo(CentroEnPantalla(), LadoGuia()), guia, duenio);

        if (guia is { } g) _vistaPrevia.Mostrar(ARectangulo(GeometriaAcople.VistaPrevia(g.Zona, Disposicion())), duenio);
        else _vistaPrevia.Hide();
    }

    /// <summary>
    /// Se soltó una flotante. Sobre una guía (y sin Esc): se acopla en esa
    /// zona, como última pestaña, activa y con el foco. En otro lado, sigue
    /// flotando donde quedó.
    ///
    /// La guía se vuelve a buscar con el ratón de AHORA, no con la última
    /// resaltada: es lo que el usuario ve al soltar.
    /// </summary>
    private void Flotante_Soltada(object? sender, bool cancelado)
    {
        OcultarGuias();
        if (cancelado || sender is not VentanaFlotante { Panel: { } v } || !IsHandleCreated) return;

        var raton = Control.MousePosition;
        if (GeometriaAcople.GuiaEn(raton.X, raton.Y, GuiasEnPantalla()) is not { } guia) return;

        Diseno.Mover(v.Id, guia.Zona);
        Actualizar();

        // La flotante (la activa) se cerró: la ventana del editor vuelve a ser
        // la activa y el foco va al panel, ya en su zona.
        FindForm()?.Activate();
        v.Enfocar();
        Avisar();
    }

    private void OcultarGuias()
    {
        _guias.Hide();
        _vistaPrevia.Hide();
    }

    /// <summary>Las seis guías, en pantalla, con el tamaño escalado a los ppp de la pantalla.</summary>
    private IReadOnlyList<GuiaAcople> GuiasEnPantalla() =>
        GeometriaAcople.Guias(APantalla(RectangleToScreen(ClientRectangle)), CentroEnPantalla(), LadoGuia());

    /// <summary>Los documentos (el centro), en pantalla: donde va el rombo.</summary>
    private ScreenRect CentroEnPantalla() => APantalla(pnlCentro.RectangleToScreen(pnlCentro.ClientRectangle));

    private int LadoGuia() => LogicalToDeviceUnits(GeometriaAcople.LadoGuia);

    /// <summary>
    /// Las zonas como están ahora, en pantalla. El panel que se arrastra ya
    /// es flotante: su zona, si quedó vacía, ya no ocupa lugar.
    /// </summary>
    private DisposicionZonas Disposicion() => new()
    {
        Area = APantalla(RectangleToScreen(AreaDeZonas())),
        Izquierda = ZonaEnPantalla(ZonaAcople.Izquierda),
        Derecha = ZonaEnPantalla(ZonaAcople.Derecha),
        Abajo = ZonaEnPantalla(ZonaAcople.Abajo),
        Divisor = divisorIzquierda.Width,
        AnchoIzquierda = Diseno.TamanoDe(ZonaAcople.Izquierda, AnchoIzquierda),
        AnchoDerecha = Diseno.TamanoDe(ZonaAcople.Derecha, AnchoDerecha),
        AltoAbajo = Diseno.TamanoDe(ZonaAcople.Abajo, AltoAbajo)
    };

    private ScreenRect? ZonaEnPantalla(ZonaAcople z) =>
        Diseno.ZonaVisible(z) ? APantalla(RectangleToScreen(ZonaDe(z).Bounds)) : null;

    private static ScreenRect APantalla(Rectangle r) => new(r.X, r.Y, r.Width, r.Height);

    private static Rectangle ARectangulo(ScreenRect r) => new(r.X, r.Y, r.Width, r.Height);

    // ------------------------------------------------------------------
    // Lo que hace el usuario
    // ------------------------------------------------------------------

    private void Ventana_OcultarPedido(object? sender, EventArgs e)
    {
        if (sender is VentanaHerramienta v) Ocultar(v.Id);
    }

    /// <summary>
    /// La chincheta. Acoplado: se repliega al borde (y el foco, que quedó en
    /// la chincheta fuera de la ventana, vuelve a los documentos). Auto-oculto:
    /// vuelve a su zona, activo y con el foco.
    /// </summary>
    private void Ventana_ChinchetaPedida(object? sender, EventArgs e)
    {
        if (sender is not VentanaHerramienta v) return;

        if (Diseno.EstaAutoOculto(v.Id))
        {
            Diseno.Fijar(v.Id);
            Actualizar();
            v.Enfocar();
        }
        else
        {
            Diseno.AutoOcultar(v.Id);
            Actualizar();
            if (IsHandleCreated) BeginInvoke(DevolverFocoSiQuedoAfuera);
        }

        Avisar();
    }

    /// <summary>El ratón entró a una pestaña del borde: se despliega si se queda (tmrDesplegar).</summary>
    private void Borde_PestanaSenalada(object? sender, string id)
    {
        if (id == _desplegado) return;
        _porDesplegar = id;
        tmrDesplegar.Stop();
        tmrDesplegar.Start();
    }

    /// <summary>El ratón se fue antes de tiempo: no se despliega.</summary>
    private void Borde_PestanaDejada(object? sender, string id)
    {
        if (_porDesplegar != id) return;
        tmrDesplegar.Stop();
        _porDesplegar = null;
    }

    /// <summary>Clic en la pestaña del borde: se despliega ya, con el foco.</summary>
    private void Borde_PestanaElegida(object? sender, string id) => Desplegar(id, enfocar: true);

    private void tmrDesplegar_Tick(object? sender, EventArgs e)
    {
        var id = _porDesplegar;
        if (id is null) tmrDesplegar.Stop();
        else Desplegar(id, enfocar: false);
    }

    /// <summary>
    /// Mientras hay uno desplegado: se pliega si el ratón no está ni sobre él
    /// ni sobre su pestaña, y el foco no está adentro. Así el desplegado con
    /// el ratón se va al sacarlo, y el desplegado con clic se queda hasta que
    /// el foco se va a otro lado, como en VS.
    /// </summary>
    private void tmrPlegar_Tick(object? sender, EventArgs e)
    {
        if (_desplegado is null || !_ventanas.TryGetValue(_desplegado, out var v) || Diseno.ZonaDe(_desplegado) is not { } zona)
        {
            tmrPlegar.Stop();
            return;
        }

        if (pnlDesplegado.ContainsFocus || v.MenuAbierto) return;

        var raton = Control.MousePosition;
        if (pnlDesplegado.RectangleToScreen(pnlDesplegado.ClientRectangle).Contains(raton)) return;
        if (BordeDe(zona).RectanguloEnPantalla(_desplegado).Contains(raton)) return;

        Plegar();
    }

    private void AnfitrionAcople_SizeChanged(object? sender, EventArgs e)
    {
        if (_desplegado is not null) UbicarDesplegado();
    }

    /// <summary>
    /// Se arrastró el título. Acoplado (o desplegado): pasa a su ventana bajo
    /// el ratón. Flotando: se mueve su ventana. En los dos casos el arrastre
    /// sigue con Windows, con el botón todavía apretado.
    /// </summary>
    private void Ventana_ArrastreIniciado(object? sender, Point enPantalla)
    {
        if (sender is not VentanaHerramienta v) return;

        if (!Diseno.EstaFlotante(v.Id)) FlotarPanel(v.Id, enPantalla);   // ya la ubica bajo el ratón
        else if (_flotantes.TryGetValue(v.Id, out var movida))
        {
            // Lo que el ratón avanzó hasta pasar la tolerancia: Windows sigue
            // desde acá, y sin esto la ventana queda atrasada ese tramo
            // (medido el 28/09 con acople.ps1: 110 de 120 px).
            movida.Location = new Point(
                movida.Left + enPantalla.X - v.PuntoApretado.X,
                movida.Top + enPantalla.Y - v.PuntoApretado.Y);
        }

        if (_flotantes.TryGetValue(v.Id, out var f)) f.EmpezarArrastre();
    }

    /// <summary>Doble clic en el título acoplado, o «Flotante»: a su ventana, con el foco.</summary>
    private void Ventana_FlotarPedido(object? sender, EventArgs e)
    {
        if (sender is not VentanaHerramienta v) return;
        FlotarPanel(v.Id, null);
        ActivarFlotante(v.Id);
    }

    /// <summary>Doble clic en el título flotando, o «Acoplar».</summary>
    private void Ventana_AcoplarPedido(object? sender, EventArgs e)
    {
        if (sender is VentanaHerramienta v) AcoplarPanel(v.Id);
    }

    /// <summary>Alt+F4 en una flotante: se oculta como con la ✕ (vuelve flotante desde Ver).</summary>
    private void Flotante_CierrePedido(object? sender, EventArgs e)
    {
        if (sender is VentanaFlotante { Panel: { } v }) Ocultar(v.Id);
    }

    /// <summary>Terminó de moverse o de cambiar de tamaño: se anota dónde quedó.</summary>
    private void Flotante_Movida(object? sender, EventArgs e)
    {
        if (sender is not VentanaFlotante { Panel: { } v } f) return;
        Diseno.GuardarLimites(v.Id, f.Left, f.Top, f.Width, f.Height);
        Avisar();
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

    private BordeAutoOcultos BordeDe(ZonaAcople z) => z switch
    {
        ZonaAcople.Izquierda => bordeIzquierda,
        ZonaAcople.Derecha => bordeDerecha,
        _ => bordeAbajo
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

        // El Padding de 1 de pnlDesplegado deja ver este color: el marco del
        // panel desplegado, que lo separa de los documentos que tapa.
        pnlDesplegado.BackColor = Tema.LineaSuave;
    }

    private void AnfitrionAcople_Disposed(object? sender, EventArgs e)
    {
        Tema.TemaCambiado -= AplicarTema;
        foreach (var f in _flotantes.Values.Where(f => !f.IsDisposed)) f.Cerrar();
        _flotantes.Clear();
        _guias.Dispose();
        _vistaPrevia.Dispose();
    }
}
