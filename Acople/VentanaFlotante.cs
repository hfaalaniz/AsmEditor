using System.Runtime.InteropServices;

namespace AsmEditor;

/// <summary>
/// Un panel sacado de su zona (3d del plan): una ventana aparte que pertenece
/// al editor (queda encima de él, se minimiza con él, no va a la barra de
/// tareas). Técnica del prototipo 0.4 (diagnostico\prototipos\acople).
///
/// ⚠ CONSERVA EL MARCO NATIVO, SIN LA FRANJA DEL TÍTULO: la barra del panel
/// hace de título (decisión de Fabián, 28/09: como VS, un solo título). Es la
/// técnica de la ventana principal (WM_NCCALCSIZE): sombra, bordes para
/// redimensionar y Aero Snap siguen siendo de Windows. Arriba, como el título
/// del panel tapa el borde, una franja de 4 px de la ventana contesta HTTOP.
///
/// ⚠ EL ARRASTRE LO HACE WINDOWS (<see cref="EmpezarArrastre"/>): al sacar el
/// panel, esta ventana aparece bajo el ratón y se le entrega el arrastre al
/// mecanismo nativo de mover ventanas, con el botón todavía apretado.
///
/// ⚠ EL PANEL NO ES DE ESTA VENTANA: se cierra soltándolo antes
/// (<see cref="Cerrar"/>), o se iría con ella al Dispose. Alt+F4 no cierra:
/// avisa (<see cref="CierrePedido"/>) y el anfitrión lo oculta como la ✕.
///
/// Acoplar arrastrando (3e): mientras Windows la mueve avisa
/// <see cref="Moviendo"/> (el anfitrión muestra las guías) y al soltarla
/// <see cref="Soltada"/> (el anfitrión decide si se acopla).
/// </summary>
public partial class VentanaFlotante : Form
{
    private const int WM_NCCALCSIZE = 0x0083;
    private const int WM_NCHITTEST = 0x0084;
    private const int WM_NCLBUTTONDOWN = 0x00A1;
    private const int WM_SIZING = 0x0214;
    private const int WM_MOVING = 0x0216;
    private const int WM_ENTERSIZEMOVE = 0x0231;
    private const int WM_EXITSIZEMOVE = 0x0232;
    private const int HTCLIENT = 1;
    private const int HTCAPTION = 2;
    private const int HTTOP = 12;
    private const int HTTOPLEFT = 13;
    private const int HTTOPRIGHT = 14;

    private bool _cerrandoDesdeAnfitrion;
    private bool _moviendo;

    // El arrastre de ahora: si se movió (no solo cambió de tamaño) y dónde
    // estaba al empezar, para reconocer el Esc.
    private bool _arrastrada;
    private Rectangle _alEmpezar;

    /// <summary>Alt+F4 (o el menú de sistema): el anfitrión la oculta como la ✕.</summary>
    public event EventHandler? CierrePedido;

    /// <summary>Terminó de moverse o de cambiar de tamaño: el anfitrión guarda dónde quedó.</summary>
    public event EventHandler? Movida;

    /// <summary>Windows la está moviendo (el ratón manda: el anfitrión lo lee).</summary>
    public event EventHandler? Moviendo;

    /// <summary>
    /// Se soltó después de moverla. True = se canceló con Esc (Windows la
    /// devolvió a donde estaba): no hay que acoplarla aunque el ratón quede
    /// sobre una guía. Llega ANTES que <see cref="Movida"/>, y si el
    /// anfitrión la acopla, Movida ya no llega: el lugar donde se la soltó no
    /// pisa el último lugar donde flotó.
    /// </summary>
    public event EventHandler<bool>? Soltada;

    public VentanaFlotante()
    {
        InitializeComponent();
        AplicarTema();
        Tema.TemaCambiado += AplicarTema;
        Disposed += VentanaFlotante_Disposed;
    }

    /// <summary>El panel que contiene, o null.</summary>
    public VentanaHerramienta? Panel { get; private set; }

    /// <summary>Pone el panel adentro, llenando la ventana.</summary>
    public void Contener(VentanaHerramienta panel)
    {
        Panel = panel;
        Text = panel.Titulo;
        panel.Dock = DockStyle.Fill;
        panel.Visible = true;
        pnlContenido.Controls.Add(panel);
    }

    /// <summary>Lo saca (sin cerrarse): para acoplarlo, o para que no muera con la ventana.</summary>
    public VentanaHerramienta? Soltar()
    {
        var p = Panel;
        if (p is not null && p.Parent == pnlContenido) pnlContenido.Controls.Remove(p);
        Panel = null;
        return p;
    }

    /// <summary>La cierra el anfitrión: suelta el panel primero.</summary>
    public void Cerrar()
    {
        _cerrandoDesdeAnfitrion = true;
        Soltar();
        Close();
    }

    /// <summary>
    /// Le entrega el arrastre a Windows, como si se hubiera apretado el título.
    /// Se llama con el botón del ratón todavía apretado.
    /// </summary>
    public void EmpezarArrastre()
    {
        ReleaseCapture();
        SendMessage(Handle, WM_NCLBUTTONDOWN, (IntPtr)HTCAPTION, IntPtr.Zero);
    }

    // ------------------------------------------------------------------
    // Atajos
    // ------------------------------------------------------------------

    /// <summary>
    /// ⚠ SIN ESTO, LOS ATAJOS NO FUNCIONAN CON EL FOCO EN LA FLOTANTE: son del
    /// menú de la ventana principal, y esta es otra ventana. Lo que no usa se
    /// lo pasa a la principal, pero solo lo que parece un atajo (con Ctrl o
    /// Alt, o una tecla F): las teclas comunes son del contenido.
    /// Alt+F4 NO: es de esta ventana (en el menú principal cerraría el editor).
    /// </summary>
    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        if (base.ProcessCmdKey(ref msg, keyData)) return true;
        if (keyData == (Keys.Alt | Keys.F4)) return false;

        var tecla = keyData & Keys.KeyCode;
        bool pareceAtajo = (keyData & (Keys.Control | Keys.Alt)) != 0 || (tecla >= Keys.F1 && tecla <= Keys.F24);

        return pareceAtajo && Owner is IAtajosDelEditor editor && editor.EjecutarAtajo(keyData);
    }

    // ------------------------------------------------------------------
    // Marco sin título
    // ------------------------------------------------------------------

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        MarcoOscuro.Aplicar(Handle);

        // Obliga a recalcular el marco con nuestro WM_NCCALCSIZE: sin esto la
        // ventana aparece la primera vez con la franja de título de Windows.
        SetWindowPos(Handle, IntPtr.Zero, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_NOZORDER | SWP_FRAMECHANGED);
    }

    protected override void WndProc(ref Message m)
    {
        switch (m.Msg)
        {
            case WM_NCCALCSIZE when m.WParam != IntPtr.Zero:
                // Windows calcula el área de cliente como siempre y después se
                // le devuelve la franja de arriba (el título).
                var antes = Marshal.PtrToStructure<NCCALCSIZE_PARAMS>(m.LParam);
                int arriba = antes.rgrc0.top;
                base.WndProc(ref m);
                var p = Marshal.PtrToStructure<NCCALCSIZE_PARAMS>(m.LParam);
                p.rgrc0.top = arriba;
                Marshal.StructureToPtr(p, m.LParam, false);
                m.Result = IntPtr.Zero;
                return;

            case WM_NCHITTEST:
                base.WndProc(ref m);
                if ((int)m.Result == HTCLIENT) m.Result = (IntPtr)BordeSuperiorEn(m.LParam);
                return;

            case WM_ENTERSIZEMOVE:
                _alEmpezar = Bounds;
                _arrastrada = false;
                break;

            case WM_MOVING:
                _moviendo = true;
                _arrastrada = true;
                Moviendo?.Invoke(this, EventArgs.Empty);
                break;

            case WM_SIZING:
                _moviendo = true;
                break;

            case WM_EXITSIZEMOVE:
                base.WndProc(ref m);

                // ⚠ Esc: Windows la devuelve EXACTAMENTE a donde estaba al
                // empezar. Un arrastre que vuelve solo al mismo píxel cuenta
                // igual como cancelado; en la práctica no pasa.
                if (_arrastrada)
                {
                    _arrastrada = false;
                    Soltada?.Invoke(this, Bounds == _alEmpezar);
                }

                // Si el anfitrión la acopló al soltarla, ya soltó el panel y
                // se cerró: no hay lugar que guardar.
                if (_moviendo)
                {
                    _moviendo = false;
                    if (Panel is not null) Movida?.Invoke(this, EventArgs.Empty);
                }
                return;
        }

        base.WndProc(ref m);
    }

    /// <summary>La franja de arriba (fuera del panel) redimensiona; el resto es cliente.</summary>
    private int BordeSuperiorEn(IntPtr lParam)
    {
        var p = PointToClient(new Point((short)((long)lParam & 0xFFFF), (short)(((long)lParam >> 16) & 0xFFFF)));
        if (p.Y >= pnlContenido.Top) return HTCLIENT;
        if (p.X < pnlContenido.Top * 2) return HTTOPLEFT;
        if (p.X > ClientSize.Width - pnlContenido.Top * 2) return HTTOPRIGHT;
        return HTTOP;
    }

    // ------------------------------------------------------------------
    // Cierre
    // ------------------------------------------------------------------

    /// <summary>
    /// Alt+F4: no se cierra, avisa. Cuando cierra el EDITOR (FormOwnerClosing)
    /// no se toca nada: el editor todavía puede preguntar por los archivos sin
    /// guardar y el usuario cancelar la salida.
    /// </summary>
    private void VentanaFlotante_FormClosing(object? sender, FormClosingEventArgs e)
    {
        if (_cerrandoDesdeAnfitrion || e.CloseReason != CloseReason.UserClosing) return;

        e.Cancel = true;
        CierrePedido?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>Se cerró de verdad (también con el editor): el panel no se va con ella.</summary>
    private void VentanaFlotante_FormClosed(object? sender, FormClosedEventArgs e) => Soltar();

    // ------------------------------------------------------------------
    // Aspecto
    // ------------------------------------------------------------------

    /// <summary>El fondo solo se ve en la franja de arriba: hace de borde, como el marco de los lados.</summary>
    private void AplicarTema()
    {
        BackColor = Tema.LineaSuave;
        pnlContenido.BackColor = Tema.Superficie;
        if (IsHandleCreated) MarcoOscuro.Aplicar(Handle);
    }

    private void VentanaFlotante_Disposed(object? sender, EventArgs e) => Tema.TemaCambiado -= AplicarTema;

    [StructLayout(LayoutKind.Sequential)]
    private struct RECT { public int left, top, right, bottom; }

    [StructLayout(LayoutKind.Sequential)]
    private struct NCCALCSIZE_PARAMS
    {
        public RECT rgrc0, rgrc1, rgrc2;
        public IntPtr lppos;
    }

    private const uint SWP_NOSIZE = 0x0001;
    private const uint SWP_NOMOVE = 0x0002;
    private const uint SWP_NOZORDER = 0x0004;
    private const uint SWP_FRAMECHANGED = 0x0020;

    [DllImport("user32.dll")] private static extern bool ReleaseCapture();
    [DllImport("user32.dll")] private static extern IntPtr SendMessage(IntPtr h, int msg, IntPtr w, IntPtr l);
    [DllImport("user32.dll")] private static extern bool SetWindowPos(IntPtr h, IntPtr after, int x, int y, int cx, int cy, uint flags);
}
