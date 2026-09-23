using System.Diagnostics;

namespace AsmEditor;

internal static class Program
{
    [STAThread]
    private static void Main(string[] args)
    {
        ApplicationConfiguration.Initialize();

        // El splash se muestra ANTES de construir la ventana principal: es justo
        // el rato en que no se ve nada —leer la configuración, buscar los
        // enlazadores, poblar el árbol y reabrir la sesión anterior—.
        // Los pasos esperados salen de contar los Informar() de MainForm: son 5
        // fijos más al menos uno por archivo que se reabra. Si el número queda
        // corto, la barra topa en 92 % y la completa Completar(); si queda
        // largo, avanza de a menos. En los dos casos sigue sin mentir.
        var splash = new FormSplash(pasosEsperados: 6);
        splash.Show();
        Application.DoEvents();   // que se pinte antes de seguir

        var reloj = Stopwatch.StartNew();

        MainForm ventana;
        try
        {
            ventana = new MainForm(args, splash);
        }
        catch
        {
            splash.Cerrar();
            splash.Dispose();
            throw;
        }

        splash.Completar();

        // ── La permanencia ───────────────────────────────────────────────
        // El editor ya está listo detrás; el splash se queda unos segundos para
        // que se alcance a leer.
        //
        // ⚠ NO SE USA Thread.Sleep: bloquearía el hilo de la interfaz y la barra
        // de progreso quedaría congelada justo mientras se la mira. Se cuenta
        // con DoEvents, que deja correr la animación.
        int restante = FormSplash.PermanenciaTrasCargaMs - (int)reloj.ElapsedMilliseconds;
        restante = Math.Max(FormSplash.PermanenciaTrasCargaMs / 2, restante);

        var espera = Stopwatch.StartNew();
        while (espera.ElapsedMilliseconds < restante && !splash.IsDisposed)
        {
            Application.DoEvents();
            Thread.Sleep(16);   // ~60 cuadros por segundo, sin quemar la CPU
        }

        splash.Cerrar();
        splash.Dispose();

        Application.Run(ventana);
    }
}
