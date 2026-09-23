using System.Runtime.InteropServices;

namespace PruebaBarraTitulo;

/// <summary>
/// Ventana con barra de título propia, conservando el marco NATIVO de Windows.
///
/// ⚠ NO ES UNA VENTANA SIN BORDE (FormBorderStyle.None). Una ventana sin borde
/// pierde todo lo que Windows hace solo: la sombra, los bordes invisibles para
/// redimensionar, el ajuste a los costados (Aero Snap), maximizar respetando la
/// barra de tareas, la animación de minimizar y el menú de Alt+Espacio. Habría
/// que imitar cada una a mano, y casi siempre quedan mal.
///
/// Acá la ventana SIGUE teniendo marco y título (WS_CAPTION, WS_THICKFRAME);
/// solo se le dice a Windows que el área de cliente empieza arriba de todo
/// (WM_NCCALCSIZE), así la franja del título desaparece y la ocupa nuestra
/// barra. Después se le contesta qué es cada zona (WM_NCHITTEST): "título"
/// para arrastrar y doble clic, "borde de arriba" para redimensionar. Los
/// bordes de los costados y de abajo siguen siendo los nativos.
/// Es la técnica que usa Windows Terminal.
/// </summary>
public partial class FormPrueba : Form
{
    private const int WM_NCCALCSIZE = 0x0083;
    private const int WM_NCHITTEST = 0x0084;

    private const int HTCLIENT = 1;
    private const int HTCAPTION = 2;
    private const int HTTOP = 12;
    private const int HTTOPLEFT = 13;
    private const int HTTOPRIGHT = 14;

    private const uint SWP_NOSIZE = 0x0001;
    private const uint SWP_NOMOVE = 0x0002;
    private const uint SWP_NOZORDER = 0x0004;
    private const uint SWP_FRAMECHANGED = 0x0020;

    /// <summary>
    /// Grosor del marco que calcula Windows, medido en el primer WM_NCCALCSIZE.
    /// Se usa como alto de la franja superior para redimensionar y para correr
    /// el cliente cuando la ventana está maximizada.
    /// </summary>
    private int _grosorMarco = 8;

    public FormPrueba()
    {
        InitializeComponent();

        BackColor = Color.FromArgb(30, 30, 30);
        panelContenido.BackColor = Color.FromArgb(30, 30, 30);
        lblInfo.ForeColor = Color.FromArgb(204, 204, 204);
    }

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);

        // Obliga a Windows a recalcular el marco con nuestro WM_NCCALCSIZE: sin
        // esto la ventana se muestra la primera vez con la franja de título.
        SetWindowPos(Handle, IntPtr.Zero, 0, 0, 0, 0,
            SWP_NOMOVE | SWP_NOSIZE | SWP_NOZORDER | SWP_FRAMECHANGED);
    }

    protected override void WndProc(ref Message m)
    {
        if (m.Msg == WM_NCCALCSIZE && m.WParam != IntPtr.Zero)
        {
            CalcularAreaDeCliente(ref m);
            return;
        }

        if (m.Msg == WM_NCHITTEST)
        {
            base.WndProc(ref m);

            // Los bordes nativos (izquierda, derecha, abajo) ya los resolvió
            // Windows. Solo se decide lo que cae adentro del área de cliente.
            if ((int)m.Result == HTCLIENT) m.Result = (IntPtr)QueHayEn(m.LParam);
            return;
        }

        base.WndProc(ref m);
    }

    /// <summary>
    /// Deja que Windows calcule el área de cliente como siempre (con los
    /// bordes) y después le devuelve la franja de arriba: el cliente arranca
    /// en el borde superior de la ventana.
    /// </summary>
    private void CalcularAreaDeCliente(ref Message m)
    {
        var antes = Marshal.PtrToStructure<NCCALCSIZE_PARAMS>(m.LParam);
        int arribaOriginal = antes.rgrc0.top;
        int izquierdaOriginal = antes.rgrc0.left;

        base.WndProc(ref m);

        var p = Marshal.PtrToStructure<NCCALCSIZE_PARAMS>(m.LParam);

        // Lo que Windows descontó a la izquierda es el grosor del marco.
        _grosorMarco = Math.Max(1, p.rgrc0.left - izquierdaOriginal);

        p.rgrc0.top = arribaOriginal;

        // ⚠ MAXIMIZADA, LA VENTANA SE SALE DE LA PANTALLA por el grosor del
        // marco en los cuatro lados (así lo hace Windows con cualquier
        // ventana). Los otros tres lados ya vienen descontados; arriba hay que
        // descontarlo a mano, o la barra queda cortada fuera de la pantalla.
        if (EstaMaximizada()) p.rgrc0.top += _grosorMarco;

        Marshal.StructureToPtr(p, m.LParam, false);
        m.Result = IntPtr.Zero;
    }

    /// <summary>Qué hay en ese punto de la pantalla, dentro del área de cliente.</summary>
    private int QueHayEn(IntPtr lParam)
    {
        int x = (short)((long)lParam & 0xFFFF);
        int y = (short)(((long)lParam >> 16) & 0xFFFF);
        var p = PointToClient(new Point(x, y));

        // La franja superior redimensiona, salvo maximizada (no hay qué estirar).
        if (!EstaMaximizada() && p.Y < _grosorMarco)
        {
            if (p.X < _grosorMarco * 2) return HTTOPLEFT;
            if (p.X > ClientSize.Width - _grosorMarco * 2) return HTTOPRIGHT;
            return HTTOP;
        }

        // Sobre la barra (lo que no son el menú ni los botones, que contestan
        // por su cuenta) es título: arrastrar y doble clic.
        if (barra.Bounds.Contains(p)) return HTCAPTION;

        return HTCLIENT;
    }

    private bool EstaMaximizada() => IsZoomed(Handle);

    private void FormPrueba_Resize(object? sender, EventArgs e)
    {
        barra.ActualizarBotonMaximizar(WindowState == FormWindowState.Maximized);
    }

    // ------------------------------------------------------------------
    // Win32
    // ------------------------------------------------------------------

    [StructLayout(LayoutKind.Sequential)]
    private struct RECT { public int left, top, right, bottom; }

    [StructLayout(LayoutKind.Sequential)]
    private struct NCCALCSIZE_PARAMS
    {
        public RECT rgrc0, rgrc1, rgrc2;
        public IntPtr lppos;
    }

    [DllImport("user32.dll")]
    private static extern bool SetWindowPos(IntPtr hWnd, IntPtr after, int x, int y, int cx, int cy, uint flags);

    [DllImport("user32.dll")]
    private static extern bool IsZoomed(IntPtr hWnd);
}
