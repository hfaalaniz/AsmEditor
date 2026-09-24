using System.Diagnostics;
using AsmEditor.Core;

namespace AsmEditor;

internal static class Program
{
    [STAThread]
    private static void Main(string[] args)
    {
        ApplicationConfiguration.Initialize();

        // El splash se muestra ANTES de construir la ventana principal: es justo
        // el rato en que no se ve nada —leer la configuración, buscar los
        // enlazadores, poblar el árbol—. El proyecto y sus archivos ya no se
        // abren acá: los elige la ventana de inicio, después del splash.
        // Los pasos esperados salen de contar los Informar() de MainForm: son 5
        // fijos más uno por archivo de la línea de comandos. Si el número queda
        // corto, la barra topa en 92 % y la completa Completar(); si queda
        // largo, avanza de a menos. En los dos casos sigue sin mentir.
        var splash = new FormSplash(pasosEsperados: 5);
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

        // ── La ventana de inicio ─────────────────────────────────────────
        // splash → ventana de inicio → IDE (PLAN_IDE.md, Etapa 1). No aparece
        // si el editor se abrió con un archivo (doble clic en un .asm), igual
        // que en Visual Studio, ni si "Al iniciar" dice otra cosa.
        if (!ventana.AbrioArchivosDeLineaDeComandos)
        {
            switch (ventana.ModoAlIniciar)
            {
                case AlIniciar.VentanaDeInicio:
                    using (var inicio = ventana.CrearVentanaInicio())
                    {
                        // Cerrarla con la X cierra el editor, como en VS.
                        if (inicio.ShowDialog() != DialogResult.OK || inicio.Eleccion is null)
                        {
                            ventana.Dispose();
                            return;
                        }
                        ventana.PrepararInicio(inicio.Eleccion);
                    }
                    break;

                case AlIniciar.UltimoProyecto:
                    ventana.PrepararInicio(new EleccionInicio(AccionInicio.UltimoProyecto));
                    break;

                // EntornoVacio: el IDE vacío, sin nada que preparar.
            }
        }

        Application.Run(ventana);
    }
}
