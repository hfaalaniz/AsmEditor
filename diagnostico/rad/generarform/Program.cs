using AsmEditor.Core;
using AsmEditor.Core.Disenador;

namespace GenerarForm;

/// <summary>
/// Arma un formulario de prueba con el modelo del diseñador y escribe los
/// archivos generados en la carpeta que se le pase.
///
/// Además imprime el detalle de los controles en un formato que el script de
/// PowerShell lee, para poder comparar lo diseñado contra lo que aparece en la
/// ventana real.
///
///   generarform &lt;carpeta&gt; [x86|x64] [dialogo]
/// </summary>
internal static class Program
{
    private static int Main(string[] args)
    {
        if (args.Length < 1)
        {
            Console.Error.WriteLine("Uso: generarform <carpeta> [x86|x64] [dialogo]");
            return 2;
        }

        var carpeta = args[0];
        var arch = args.Contains("x86") ? TargetArch.Win32 : TargetArch.Win64;
        var tipo = args.Contains("dialogo") ? TipoFormulario.Dialogo : TipoFormulario.VentanaPrincipal;

        Directory.CreateDirectory(carpeta);

        var f = new FormularioDisenado
        {
            Nombre = "Prueba",
            Titulo = "Generado por el disenador",
            Ancho = 400,
            Alto = 300,
            Arquitectura = arch,
            Tipo = tipo
        };

        // Un control de cada clase, con textos que hayan dado problemas antes.
        Agregar(f, TipoControl.Etiqueta, "Nombre:",        20,  20,  80,  20);
        Agregar(f, TipoControl.Campo,    "",               110, 18,  200, 24);
        Agregar(f, TipoControl.Boton,    "Aceptar",        110, 60,  100, 30);
        Agregar(f, TipoControl.Boton,    "Cancelar",       220, 60,  100, 30);
        Agregar(f, TipoControl.Casilla,  "Recordar datos", 110, 100, 160, 24);
        Agregar(f, TipoControl.Lista,    "",               20,  140, 300, 120);

        var errores = f.Validar();

        if (errores.Count > 0)
        {
            foreach (var e in errores) Console.Error.WriteLine("ERROR: " + e);
            return 1;
        }

        var ruta = Path.Combine(carpeta, f.Nombre + ArchivoFormulario.Extension);

        ArchivoFormulario.Guardar(ruta, f);
        var r = ArchivoFormulario.GenerarArchivos(ruta, f);

        Console.WriteLine($"ASMFORM={ruta}");
        Console.WriteLine($"INC={r.RutaInclude}");
        Console.WriteLine($"ASM={r.RutaAsm}");
        Console.WriteLine($"ARCH={(f.EsX64 ? "x64" : "x86")}");
        Console.WriteLine($"ENTRADA={(f.EsX64 ? "main" : "_main")}");

        // Una línea por control, para que el script compare contra la ventana.
        // El texto va último porque puede tener cualquier cosa adentro.
        foreach (var c in f.Controles)
        {
            Console.WriteLine($"CONTROL\t{c.Id}\t{c.Nombre}\t{c.ClaseEfectiva}\t{c.Texto}");
        }

        return 0;
    }

    private static void Agregar(
        FormularioDisenado f, TipoControl tipo, string texto,
        int x, int y, int ancho, int alto)
    {
        var c = f.AgregarControl(tipo, x, y);
        c.Texto = texto;
        c.Ancho = ancho;
        c.Alto = alto;
    }
}
