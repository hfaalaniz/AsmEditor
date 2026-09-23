using System.Diagnostics;
using System.Text.RegularExpressions;

namespace PruebaTerminal;

/// <summary>
/// Etapa 0.3 (PLAN_IDE.md): la terminal medida SIN VENTANA. Maneja la sesión
/// ConPTY por código y lee la grilla del intérprete, así que no toca el ratón
/// ni el teclado: se puede correr mientras se usa la PC.
///
/// Mide lo que decide si ConPTY sirve: el prompt, el eco, los colores, NASM en
/// el PATH, la entrada interactiva (Read-Host), Ctrl+C, el cambio de tamaño y
/// la salida. Después compara con la alternativa (redirigir stdin/stdout).
/// </summary>
public static class PruebaSinVentana
{
    private static int _fallas;

    private static void Bien(string t) { Console.ForegroundColor = ConsoleColor.Green; Console.WriteLine("  OK   " + t); Console.ResetColor(); }
    private static void Mal(string t) { Console.ForegroundColor = ConsoleColor.Red; Console.WriteLine("  MAL  " + t); Console.ResetColor(); _fallas++; }
    private static void Titulo(string t) { Console.WriteLine(); Console.ForegroundColor = ConsoleColor.Cyan; Console.WriteLine("=== " + t + " ==="); Console.ResetColor(); }

    public static int Correr()
    {
        var pantalla = new PantallaTerminal(120, 30);
        var vt = new InterpreteVT(pantalla);

        using var sesion = new SesionConPty();
        sesion.DatosRecibidos += (d, n) => vt.Procesar(d, n);

        var terminado = new ManualResetEventSlim(false);
        sesion.Terminado += () => terminado.Set();

        string Texto() { lock (pantalla.Candado) return pantalla.TodoElTexto(); }

        // Un prompt de PowerShell es una línea que empieza con "PS " y termina con ">".
        int Prompts() => Regex.Matches(Texto(), @"(?m)^PS [^\n]*>").Count;

        bool Esperar(Func<bool> condicion, int ms = 10000)
        {
            var reloj = Stopwatch.StartNew();
            while (reloj.ElapsedMilliseconds < ms)
            {
                if (condicion()) return true;
                Thread.Sleep(50);
            }
            return condicion();
        }

        // Manda un comando y espera a que vuelva el prompt (uno más que antes).
        bool Comando(string cmd, int ms = 15000)
        {
            int antes = Prompts();
            sesion.Escribir(cmd + "\r");
            return Esperar(() => Prompts() > antes, ms);
        }

        // ------------------------------------------------------------------
        Titulo("1. PowerShell arranca en la pseudoconsola y muestra el prompt");
        var reloj = Stopwatch.StartNew();
        sesion.Iniciar("powershell.exe -NoLogo -NoProfile", 120, 30, Configuracion.CarpetaNasm, Configuracion.EntornoConNasm());

        if (Esperar(() => Prompts() >= 1, 20000)) Bien($"prompt a los {reloj.ElapsedMilliseconds} ms");
        else { Mal("no apareció el prompt en 20 s"); Volcar(Texto()); return 1; }

        // ------------------------------------------------------------------
        Titulo("2. Eco de un comando");
        if (Comando("echo hola-desde-conpty") && Texto().Contains("\nhola-desde-conpty")) Bien("la salida del comando está en la pantalla");
        else Mal("no se vio la salida de echo");

        // ------------------------------------------------------------------
        Titulo("3. Colores");
        Comando("Write-Host 'ROJO' -ForegroundColor Red; Write-Host 'VERDE' -ForegroundColor Green");
        var colorRojo = ColorDe(pantalla, "ROJO");
        var colorVerde = ColorDe(pantalla, "VERDE");
        Console.WriteLine($"  color de ROJO: {Nombre(colorRojo)}   color de VERDE: {Nombre(colorVerde)}");
        if (colorRojo is 1 or 9) Bien("ROJO se pinta en rojo"); else Mal("ROJO no quedó en rojo");
        if (colorVerde is 2 or 10) Bien("VERDE se pinta en verde"); else Mal("VERDE no quedó en verde");

        // ------------------------------------------------------------------
        Titulo("4. NASM en el PATH (como Developer PowerShell)");
        if (Comando("nasm -v") && Texto().Contains("NASM version")) Bien("'nasm -v' respondió sin ruta");
        else Mal("'nasm -v' no respondió");

        // ------------------------------------------------------------------
        Titulo("5. Entrada interactiva (Read-Host)");
        int antesRead = Prompts();
        sesion.Escribir("$n = Read-Host 'Nombre'\r");
        if (Esperar(() => Texto().Contains("Nombre:"))) Bien("apareció la pregunta"); else Mal("no apareció 'Nombre:'");
        sesion.Escribir("Fabian\r");
        Esperar(() => Prompts() > antesRead);
        if (Comando("echo \"respuesta=$n\"") && Texto().Contains("respuesta=Fabian")) Bien("Read-Host recibió lo tipeado");
        else Mal("Read-Host no recibió lo tipeado");

        // ------------------------------------------------------------------
        Titulo("6. Ctrl+C corta un programa que no termina");
        int antesPing = Prompts();
        sesion.Escribir("ping -t 127.0.0.1\r");
        if (Esperar(() => Regex.Matches(Texto(), "127\\.0\\.0\\.1").Count >= 3)) Bien("ping está corriendo");
        else Mal("ping no arrancó");
        sesion.Escribir("\x03");
        if (Esperar(() => Prompts() > antesPing, 8000)) Bien("Ctrl+C lo cortó y volvió el prompt");
        else Mal("Ctrl+C no cortó el ping");

        // ------------------------------------------------------------------
        Titulo("7. Cambio de tamaño");
        lock (pantalla.Candado) pantalla.Redimensionar(80, 25);
        sesion.Redimensionar(80, 25);
        Thread.Sleep(300);
        Comando("Clear-Host; $Host.UI.RawUI.WindowSize.Width");
        if (Regex.IsMatch(Texto(), @"(?m)^80$")) Bien("el programa ve 80 columnas");
        else Mal("el programa no se enteró del cambio de tamaño");

        // ------------------------------------------------------------------
        Titulo("8. Salida");
        sesion.Escribir("exit 7\r");
        if (terminado.Wait(8000)) Bien("el programa terminó y el lector se cerró solo");
        else Mal("el programa no terminó");
        Thread.Sleep(300);
        var codigo = sesion.CodigoDeSalida();
        if (codigo == 7) Bien("código de salida 7"); else Mal($"código de salida {codigo?.ToString() ?? "(sigue corriendo)"}");

        // ------------------------------------------------------------------
        Titulo("9. Comparación: la misma prueba con stdin/stdout redirigidos");
        CompararConRedireccion();

        Console.WriteLine();
        if (_fallas == 0) { Console.ForegroundColor = ConsoleColor.Green; Console.WriteLine("=== TODO BIEN: ConPTY sirve para la terminal ==="); Console.ResetColor(); return 0; }

        Console.ForegroundColor = ConsoleColor.Red; Console.WriteLine($"=== {_fallas} FALLAS ==="); Console.ResetColor();
        Volcar(Texto());
        return 1;
    }

    /// <summary>
    /// Lo mismo, pero con la salida redirigida (lo que haría un Process común).
    /// No suma fallas: es para anotar qué se pierde.
    /// </summary>
    private static void CompararConRedireccion()
    {
        var psi = new ProcessStartInfo("powershell.exe", "-NoLogo -NoProfile -Command -")
        {
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            WorkingDirectory = Configuracion.CarpetaNasm
        };
        psi.Environment["PATH"] = Configuracion.EntornoConNasm()["PATH"];

        using var p = Process.Start(psi)!;
        p.StandardInput.WriteLine("echo hola-redirigido");
        p.StandardInput.WriteLine("Write-Host 'ROJO' -ForegroundColor Red");
        p.StandardInput.WriteLine("nasm -v");
        p.StandardInput.WriteLine("exit");
        p.StandardInput.Close();

        var salida = p.StandardOutput.ReadToEnd();
        p.WaitForExit(10000);

        bool prompt = Regex.IsMatch(salida, @"(?m)^PS [^\n]*>");
        bool colores = salida.Contains('\x1b');

        Console.WriteLine($"  eco: {(salida.Contains("hola-redirigido") ? "sí" : "no")}   " +
                          $"nasm: {(salida.Contains("NASM version") ? "sí" : "no")}   " +
                          $"prompt: {(prompt ? "sí" : "NO")}   colores: {(colores ? "sí" : "NO")}");
        Console.WriteLine("  sin consola del otro lado: no hay forma de mandar Ctrl+C ni de avisar un cambio de tamaño.");
    }

    /// <summary>El color de frente de la primera celda de la palabra, o -1 si no está.</summary>
    private static int ColorDe(PantallaTerminal p, string palabra)
    {
        lock (p.Candado)
        {
            for (int f = p.Filas - 1; f >= 0; f--)
            {
                var fila = p.TextoDeFila(f);
                // Solo una línea que sea EXACTAMENTE la palabra: la del comando
                // tipeado también la contiene, pero en otro color.
                if (fila == palabra) return p[f, 0].Frente;
            }
        }
        return -1;
    }

    private static string Nombre(int c) => c switch
    {
        -1 => "(no está)", 1 => "rojo (31)", 9 => "rojo claro (91)", 2 => "verde (32)", 10 => "verde claro (92)", _ => $"índice {c}"
    };

    private static void Volcar(string texto)
    {
        Console.WriteLine("  --- pantalla ---");
        foreach (var l in texto.Split('\n').Where(l => l.Length > 0)) Console.WriteLine("  | " + l);
    }
}
