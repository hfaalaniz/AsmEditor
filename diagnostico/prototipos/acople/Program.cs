using System.Runtime.InteropServices;

namespace PruebaAcople;

internal static class Program
{
    /// <summary>
    ///   PruebaAcople.exe            -> la ventana, para probar a mano
    ///   PruebaAcople.exe --prueba   -> geometría y modelo sin ratón (salida en la consola)
    /// </summary>
    [STAThread]
    private static int Main(string[] args)
    {
        ApplicationConfiguration.Initialize();

        if (args.Contains("--prueba")) return PruebaSinVentana.Correr();

        FreeConsole();
        Application.Run(new FormPrueba());
        return 0;
    }

    [DllImport("kernel32.dll")]
    private static extern bool FreeConsole();
}
