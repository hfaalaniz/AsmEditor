using System.Runtime.InteropServices;

namespace PruebaTerminal;

internal static class Program
{
    /// <summary>
    ///   PruebaTerminal.exe                  -> la terminal en una ventana, para probarla a mano
    ///   PruebaTerminal.exe --prueba         -> las mediciones sin ventana (salida en la consola)
    /// </summary>
    [STAThread]
    private static int Main(string[] args)
    {
        if (args.Contains("--prueba")) return PruebaSinVentana.Correr();

        // En modo ventana no hace falta la consola del .exe: se suelta.
        FreeConsole();

        ApplicationConfiguration.Initialize();
        Application.Run(new FormTerminal());
        return 0;
    }

    [DllImport("kernel32.dll")]
    private static extern bool FreeConsole();
}
