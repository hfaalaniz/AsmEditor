using System.Runtime.InteropServices;

namespace PruebaAcople;

/// <summary>
/// Un panel sacado de su zona: una ventana chica aparte, con marco nativo.
///
/// ⚠ EL ARRASTRE LO HACE WINDOWS. Al sacar el panel, esta ventana aparece bajo
/// el ratón y se le entrega el arrastre al mecanismo nativo de mover ventanas
/// (<see cref="EmpezarArrastre"/>). Así se mueve suave, entre monitores y con
/// las mismas reglas que cualquier ventana, sin imitarlo con MouseMove.
/// Mientras se mueve, Windows manda WM_MOVING (→ <see cref="Moviendo"/>: el
/// anfitrión muestra las guías) y al soltar WM_EXITSIZEMOVE (→
/// <see cref="Soltada"/>: el anfitrión decide si se acopla).
/// </summary>
public partial class VentanaFlotante : Form
{
    private const int WM_MOVING = 0x0216;
    private const int WM_EXITSIZEMOVE = 0x0232;
    private const int WM_NCLBUTTONDOWN = 0x00A1;
    private const int WM_NCLBUTTONDBLCLK = 0x00A3;
    private const int HTCAPTION = 2;

    private bool _moviendo;

    /// <summary>La ventana se está moviendo (posición del ratón en pantalla).</summary>
    public event Action<VentanaFlotante, Point>? Moviendo;

    /// <summary>Terminó el movimiento (posición del ratón en pantalla).</summary>
    public event Action<VentanaFlotante, Point>? Soltada;

    /// <summary>Doble clic en el título: volver a la última zona, como en Visual Studio.</summary>
    public event Action<VentanaFlotante>? VolverPedido;

    public VentanaFlotante()
    {
        InitializeComponent();
        BackColor = Color.FromArgb(37, 37, 38);
    }

    /// <summary>El panel que contiene.</summary>
    public VentanaHerramienta? Panel { get; private set; }

    /// <summary>Pone el panel adentro, llenando la ventana.</summary>
    public void Contener(VentanaHerramienta panel)
    {
        Panel = panel;
        Text = panel.Titulo;
        panel.Parent = pnlContenido;
        panel.Dock = DockStyle.Fill;
    }

    /// <summary>Suelta el panel (para acoplarlo en otro lado).</summary>
    public VentanaHerramienta? Soltar()
    {
        var p = Panel;
        if (p is not null) p.Parent = null;
        Panel = null;
        return p;
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

    protected override void WndProc(ref Message m)
    {
        switch (m.Msg)
        {
            case WM_MOVING:
                _moviendo = true;
                Moviendo?.Invoke(this, Control.MousePosition);
                break;

            case WM_EXITSIZEMOVE:
                // También llega al terminar de REDIMENSIONAR: solo cuenta si hubo movimiento.
                if (_moviendo)
                {
                    _moviendo = false;
                    base.WndProc(ref m);
                    Soltada?.Invoke(this, Control.MousePosition);
                    return;
                }
                break;

            case WM_NCLBUTTONDBLCLK when (int)m.WParam == HTCAPTION:
                VolverPedido?.Invoke(this);
                return;
        }

        base.WndProc(ref m);
    }

    [DllImport("user32.dll")] private static extern bool ReleaseCapture();
    [DllImport("user32.dll")] private static extern IntPtr SendMessage(IntPtr h, int msg, IntPtr w, IntPtr l);
}
