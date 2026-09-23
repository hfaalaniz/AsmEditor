using System.ComponentModel;
using AsmEditor.Core;

namespace AsmEditor;

/// <summary>
/// La barra de título propia de la ventana principal, como la de Visual
/// Studio: logo, menú, buscador de comandos, insignia del proyecto y los tres
/// botones de la ventana.
///
/// ⚠ LA VENTANA SIGUE TENIENDO MARCO NATIVO. Esta barra solo ocupa la franja
/// que antes era el título de Windows; el arrastre, el doble clic, Aero Snap y
/// el maximizado los sigue haciendo Windows, porque <see cref="MainForm"/> le
/// contesta que esta franja ES el título (WM_NCHITTEST). Técnica probada en
/// la Etapa 0.2 (diagnostico\prototipos\barra_titulo): ver PLAN_IDE.md.
///
/// ⚠ POR ESO SU SUPERFICIE VACÍA ES "TRANSPARENTE" PARA WINDOWS (HTTRANSPARENT):
/// si contestara "área de cliente", la pregunta no llegaría al formulario y la
/// ventana no se podría arrastrar. El menú, el buscador, la insignia y los
/// botones no lo son: esos sí tienen que recibir el clic.
/// </summary>
public partial class BarraTitulo : UserControl
{
    private const int WM_NCHITTEST = 0x0084;
    private const int HTTRANSPARENT = -1;

    // Segoe MDL2 Assets: los glifos de los botones de ventana de Windows 10.
    private const string GlifoMaximizar = "\uE922";
    private const string GlifoRestaurar = "\uE923";

    /// <summary>La lista de resultados del buscador (ventana aparte, ver PopupBusqueda).</summary>
    private readonly PopupBusqueda _resultados = new();

    /// <summary>Lo que tenía el foco antes de ir al buscador, para devolvérselo.</summary>
    private Control? _focoPrevio;

    private bool _activa = true;

    public BarraTitulo()
    {
        InitializeComponent();

        _resultados.Elegido += Resultados_Elegido;

        AplicarTema();
        Tema.TemaCambiado += AplicarTema;
        Disposed += BarraTitulo_Disposed;

        AcomodarDespuesDelMenu();
    }

    /// <summary>
    /// El menú principal. Lo llena <see cref="MainForm"/>, que es quien sabe
    /// qué comandos hay; la barra solo lo aloja.
    /// </summary>
    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public MenuStrip Menu => menu;

    /// <summary>
    /// La zona del logo, en coordenadas de la barra. El formulario le dice a
    /// Windows que ahí está el menú de sistema (HTSYSMENU), y así el clic lo
    /// abre y el doble clic cierra la ventana, como en cualquier ventana.
    /// </summary>
    [Browsable(false)]
    public Rectangle AreaLogo => lblLogo.Bounds;

    /// <summary>
    /// Ventana activa o no. Inactiva, los textos se atenúan, como hace Windows
    /// con el título: así se ve cuál de las ventanas tiene el teclado.
    /// </summary>
    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool Activa
    {
        get => _activa;
        set
        {
            if (_activa == value) return;
            _activa = value;
            PintarTextos();

            // Otra ventana pasó al frente: la lista de resultados no puede
            // quedar flotando encima de ella.
            if (!value) _resultados.Ocultar();
        }
    }

    /// <summary>
    /// Muestra la insignia (nombre del proyecto o del archivo) con la ruta
    /// completa en el tooltip. Sin texto, la insignia se oculta.
    /// </summary>
    public void MostrarInsignia(string? texto, string? detalle)
    {
        lblInsignia.Visible = !string.IsNullOrWhiteSpace(texto);
        lblInsignia.Text = texto ?? "";

        // Los tooltips van acá y no en el Designer.cs: VS los mandaría al .resx.
        toolTip.SetToolTip(lblInsignia, detalle ?? "");
    }

    public void ActualizarBotonMaximizar(bool maximizada)
    {
        btnMaximizar.Text = maximizada ? GlifoRestaurar : GlifoMaximizar;
        toolTip.SetToolTip(btnMaximizar, maximizada ? "Restaurar" : "Maximizar");
    }

    /// <summary>Ctrl+Q: lleva el foco al buscador, como en Visual Studio.</summary>
    public void EnfocarBuscador()
    {
        var f = FindForm();
        _focoPrevio = f is null ? null : FocoMasProfundo(f);

        txtBuscar.Focus();
        txtBuscar.SelectAll();
    }

    // ------------------------------------------------------------------
    // Transparencia para Windows
    // ------------------------------------------------------------------

    protected override void WndProc(ref Message m)
    {
        if (m.Msg == WM_NCHITTEST)
        {
            m.Result = (IntPtr)HTTRANSPARENT;
            return;
        }

        base.WndProc(ref m);
    }

    // ------------------------------------------------------------------
    // Buscador de comandos
    // ------------------------------------------------------------------

    private void txtBuscar_TextChanged(object? sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(txtBuscar.Text))
        {
            _resultados.Ocultar();
            return;
        }

        if (FindForm() is not Form f) return;

        var encontrados = FiltroComandos.Filtrar(ComandosDelMenu(), txtBuscar.Text);
        _resultados.Mostrar(encontrados, RectangleToScreen(txtBuscar.Bounds), f);
    }

    private void txtBuscar_KeyDown(object? sender, KeyEventArgs e)
    {
        switch (e.KeyCode)
        {
            case Keys.Down:
                _resultados.Mover(1);
                break;

            case Keys.Up:
                _resultados.Mover(-1);
                break;

            case Keys.Enter:
                Ejecutar(_resultados.Seleccionado);
                break;

            case Keys.Escape:
                Cancelar();
                break;

            default:
                return;
        }

        // Sin esto, Enter y Esc hacen el "ding" del sistema en un TextBox.
        e.Handled = true;
        e.SuppressKeyPress = true;
    }

    private void txtBuscar_Leave(object? sender, EventArgs e)
    {
        _resultados.Ocultar();
        txtBuscar.Text = "";
    }

    private void Resultados_Elegido(ComandoMenu c) => Ejecutar(c);

    /// <summary>
    /// Ejecuta el comando elegido como si se lo hubiera elegido en el menú.
    ///
    /// Primero se devuelve el foco adonde estaba: el comando tiene que actuar
    /// sobre el documento en el que se estaba trabajando (Guardar, Buscar...),
    /// no sobre el campo de búsqueda.
    /// </summary>
    private void Ejecutar(ComandoMenu? c)
    {
        if (c?.Dato is not ToolStripMenuItem item) return;

        Cancelar();

        if (item.Enabled) item.PerformClick();
    }

    private void Cancelar()
    {
        _resultados.Ocultar();
        txtBuscar.Text = "";

        if (_focoPrevio is { IsDisposed: false, CanFocus: true }) _focoPrevio.Focus();
        else FindForm()?.SelectNextControl(this, true, true, true, true);

        _focoPrevio = null;
    }

    /// <summary>
    /// Todos los comandos del menú, con su ruta ("Archivo › Guardar"). Se
    /// arma en cada búsqueda porque qué está habilitado cambia todo el tiempo
    /// (hay documento o no, se está compilando o no).
    /// </summary>
    private List<ComandoMenu> ComandosDelMenu()
    {
        var lista = new List<ComandoMenu>();

        foreach (ToolStripItem item in menu.Items) Recorrer(item, "", lista);

        return lista;
    }

    private static void Recorrer(ToolStripItem item, string prefijo, List<ComandoMenu> lista)
    {
        if (item is not ToolStripMenuItem mi || !mi.Available) return;

        var nombre = FiltroComandos.QuitarMnemonico(mi.Text ?? "");
        var ruta = prefijo.Length == 0 ? nombre : prefijo + " › " + nombre;

        bool tieneHijos = mi.DropDownItems.OfType<ToolStripMenuItem>().Any();

        if (tieneHijos)
        {
            foreach (ToolStripItem hijo in mi.DropDownItems) Recorrer(hijo, ruta, lista);
            return;
        }

        var atajo = !string.IsNullOrEmpty(mi.ShortcutKeyDisplayString)
            ? mi.ShortcutKeyDisplayString
            : mi.ShortcutKeys != Keys.None
                ? new KeysConverter().ConvertToString(mi.ShortcutKeys) ?? ""
                : "";

        lista.Add(new ComandoMenu(ruta, nombre, atajo, mi.Enabled, mi));
    }

    /// <summary>El control que tiene el foco, bajando por los contenedores.</summary>
    private static Control? FocoMasProfundo(ContainerControl c)
    {
        Control? activo = c.ActiveControl;

        while (activo is ContainerControl cc && cc.ActiveControl is not null) activo = cc.ActiveControl;

        return activo;
    }

    // ------------------------------------------------------------------
    // Botones de la ventana
    // ------------------------------------------------------------------

    private void btnMinimizar_Click(object? sender, EventArgs e)
    {
        if (FindForm() is Form f) f.WindowState = FormWindowState.Minimized;
    }

    private void btnMaximizar_Click(object? sender, EventArgs e)
    {
        if (FindForm() is not Form f) return;

        f.WindowState = f.WindowState == FormWindowState.Maximized
            ? FormWindowState.Normal
            : FormWindowState.Maximized;
    }

    private void btnCerrar_Click(object? sender, EventArgs e) => FindForm()?.Close();

    // ------------------------------------------------------------------
    // Ubicación y aspecto
    // ------------------------------------------------------------------

    /// <summary>
    /// El menú crece al llenarlo (lo llena MainForm después de construir la
    /// barra): el buscador y la insignia van a continuación.
    /// </summary>
    private void menu_SizeChanged(object? sender, EventArgs e) => AcomodarDespuesDelMenu();

    private void AcomodarDespuesDelMenu()
    {
        txtBuscar.Left = menu.Right + 12;
        lblInsignia.Left = txtBuscar.Right + 12;
    }

    /// <summary>
    /// Colores del tema activo. Van acá y no en el Designer.cs porque cambian
    /// en vivo al pasar de oscuro a claro.
    ///
    /// La barra usa el mismo tono que la barra de herramientas de abajo, como
    /// en Visual Studio: juntas forman la cabecera, distinta del contenido.
    /// </summary>
    private void AplicarTema()
    {
        BackColor = Tema.Superficie2;

        lblLogo.Image?.Dispose();
        lblLogo.Image = DibujarLogo();

        txtBuscar.BackColor = Tema.Superficie3;
        txtBuscar.ForeColor = Tema.Texto;
        txtBuscar.Font = Tema.Cuerpo;

        lblInsignia.ColorFondo = Tema.Superficie3;
        lblInsignia.Font = Tema.Cuerpo;

        foreach (var b in new[] { btnMinimizar, btnMaximizar, btnCerrar })
        {
            b.BackColor = Tema.Superficie2;
            b.FlatAppearance.MouseOverBackColor = Tema.Realzar(Tema.Superficie2, 14);
            b.FlatAppearance.MouseDownBackColor = Tema.Realzar(Tema.Superficie2, 24);
        }

        // Como en Windows: cerrar se pone rojo, en los dos temas.
        btnCerrar.FlatAppearance.MouseOverBackColor = Color.FromArgb(232, 17, 35);
        btnCerrar.FlatAppearance.MouseDownBackColor = Color.FromArgb(241, 112, 122);

        toolTip.SetToolTip(btnMinimizar, "Minimizar");
        toolTip.SetToolTip(btnCerrar, "Cerrar");

        PintarTextos();
        Invalidate(true);
    }

    private void PintarTextos()
    {
        var texto = _activa ? Tema.Texto : Tema.Texto3;

        lblInsignia.ForeColor = texto;
        foreach (var b in new[] { btnMinimizar, btnMaximizar, btnCerrar }) b.ForeColor = texto;
    }

    private Bitmap DibujarLogo()
    {
        var bmp = new Bitmap(lblLogo.Width, lblLogo.Height);

        using var g = Graphics.FromImage(bmp);
        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        Tema.DibujarLogo(g, 1, 1, lblLogo.Width - 2, Tema.Acento);

        return bmp;
    }

    private void BarraTitulo_Disposed(object? sender, EventArgs e)
    {
        Tema.TemaCambiado -= AplicarTema;
        _resultados.Dispose();
    }
}

/// <summary>
/// Una etiqueta de la barra de título.
///
/// Por defecto deja pasar la pregunta de Windows al formulario
/// (<see cref="DejaPasarRaton"/>): sobre el logo o un texto también se tiene
/// que poder arrastrar la ventana. La insignia la desactiva, porque necesita
/// el ratón para mostrar su tooltip.
/// </summary>
public class EtiquetaTitulo : Label
{
    private const int WM_NCHITTEST = 0x0084;
    private const int HTTRANSPARENT = -1;

    /// <summary>Si la pregunta de Windows pasa al formulario (arrastrable).</summary>
    [DefaultValue(true)]
    public bool DejaPasarRaton { get; set; } = true;

    /// <summary>Dibuja un fondo redondeado detrás del texto (la insignia).</summary>
    [DefaultValue(false)]
    public bool ConFondo { get; set; }

    /// <summary>El color de ese fondo. Lo pone el tema, por eso no se serializa.</summary>
    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Color ColorFondo { get; set; } = Color.Gray;

    protected override void WndProc(ref Message m)
    {
        if (DejaPasarRaton && m.Msg == WM_NCHITTEST)
        {
            m.Result = (IntPtr)HTTRANSPARENT;
            return;
        }

        base.WndProc(ref m);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        if (ConFondo)
        {
            e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

            using var fondo = new SolidBrush(ColorFondo);
            using var forma = Tema.Redondeado(new RectangleF(0, 0, Width - 1, Height - 1), 4);
            e.Graphics.FillPath(fondo, forma);
        }

        base.OnPaint(e);
    }
}
