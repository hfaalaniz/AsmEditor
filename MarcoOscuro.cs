using System.Runtime.InteropServices;

namespace AsmEditor;

/// <summary>
/// Barra de título nativa oscura en el tema oscuro (Windows 10 1809+): sin
/// esto, el marco del sistema sale blanco sobre una ventana oscura. Lo usan
/// las ventanas que conservan el marco nativo (la de inicio, Opciones); la
/// principal dibuja su propia barra.
/// </summary>
internal static class MarcoOscuro
{
    /// <summary>Se llama desde OnHandleCreated de la ventana.</summary>
    public static void Aplicar(IntPtr ventana)
    {
        int oscuro = Tema.EsOscuro ? 1 : 0;
        // 20 = DWMWA_USE_IMMERSIVE_DARK_MODE (Windows 10 20H1+); 19 en versiones anteriores.
        if (DwmSetWindowAttribute(ventana, 20, ref oscuro, sizeof(int)) != 0)
            DwmSetWindowAttribute(ventana, 19, ref oscuro, sizeof(int));
    }

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int value, int size);
}
