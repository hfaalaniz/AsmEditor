using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Win32.SafeHandles;

namespace PruebaTerminal;

/// <summary>
/// Un programa de consola (PowerShell) corriendo dentro de una PSEUDOCONSOLA de
/// Windows (ConPTY, Windows 10 1809 en adelante).
///
/// ⚠ POR QUÉ ConPTY Y NO REDIRIGIR stdin/stdout. Con la salida redirigida el
/// programa sabe que no hay una consola del otro lado y se porta distinto:
/// PowerShell no muestra el prompt ni colores, Read-Host y los programas que
/// preguntan algo no funcionan bien, y no hay forma de mandar Ctrl+C. Con
/// ConPTY el programa cree que está en una consola de verdad; nosotros
/// recibimos lo que pintaría (texto con secuencias de escape VT) y le mandamos
/// lo que tipearía el usuario.
///
/// La sesión solo mueve bytes. Qué significan (colores, cursor) lo resuelve
/// <see cref="InterpreteVT"/>.
/// </summary>
public sealed class SesionConPty : IDisposable
{
    private IntPtr _pseudoconsola;
    private SafeFileHandle? _escrituraEntrada;   // lo que escribimos -> teclado del programa
    private SafeFileHandle? _lecturaSalida;      // lo que el programa pinta -> nosotros
    private FileStream? _entrada;
    private IntPtr _proceso;
    private IntPtr _hilo;
    private Thread? _lector;
    private Thread? _vigia;

    /// <summary>
    /// Llegaron datos del programa (UTF-8 con secuencias VT). Se dispara desde
    /// el hilo lector, NO desde el de la interfaz.
    /// </summary>
    public event Action<byte[], int>? DatosRecibidos;

    /// <summary>El programa terminó (desde el hilo lector).</summary>
    public event Action? Terminado;

    public bool Corriendo { get; private set; }

    /// <summary>
    /// Arranca <paramref name="lineaDeComandos"/> en una pseudoconsola de
    /// <paramref name="columnas"/>×<paramref name="filas"/>.
    /// </summary>
    /// <param name="variablesExtra">Variables de entorno que se agregan a las heredadas (null = ninguna).</param>
    public void Iniciar(string lineaDeComandos, short columnas, short filas,
                        string? carpeta = null, IDictionary<string, string>? variablesExtra = null)
    {
        if (Corriendo) throw new InvalidOperationException("La sesión ya está corriendo.");

        // Dos caños: uno para lo que tipeamos, otro para lo que el programa pinta.
        if (!CreatePipe(out var lecturaEntrada, out var escrituraEntrada, IntPtr.Zero, 0)) Fallar("CreatePipe (entrada)");
        if (!CreatePipe(out var lecturaSalida, out var escrituraSalida, IntPtr.Zero, 0)) Fallar("CreatePipe (salida)");

        int hr = CreatePseudoConsole(new COORD { X = columnas, Y = filas },
                                     lecturaEntrada, escrituraSalida, 0, out _pseudoconsola);
        if (hr != 0) throw new Win32Exception(hr, "CreatePseudoConsole");

        // ⚠ LOS EXTREMOS QUE USA LA PSEUDOCONSOLA SE CIERRAN ACÁ. Ella ya tiene
        // su copia; si nos quedáramos con la nuestra, el caño de salida nunca
        // daría fin de archivo y el hilo lector no terminaría jamás.
        lecturaEntrada.Dispose();
        escrituraSalida.Dispose();

        _escrituraEntrada = escrituraEntrada;
        _lecturaSalida = lecturaSalida;
        _entrada = new FileStream(_escrituraEntrada, FileAccess.Write, 1);

        CrearProceso(lineaDeComandos, carpeta, variablesExtra);
        Corriendo = true;

        _lector = new Thread(Leer) { IsBackground = true, Name = "Lector ConPTY" };
        _lector.Start();

        _vigia = new Thread(VigilarProceso) { IsBackground = true, Name = "Vigía ConPTY" };
        _vigia.Start();
    }

    /// <summary>
    /// ⚠ LA PSEUDOCONSOLA NO CIERRA SU SALIDA CUANDO EL PROGRAMA TERMINA: el
    /// caño sigue abierto hasta que alguien cierra la pseudoconsola, y el
    /// lector quedaría esperando para siempre. Medido el 23/09 con "exit 7": el
    /// proceso ya había terminado con código 7 y el lector no se enteraba.
    /// Así que se vigila el proceso y, cuando termina, se cierra la
    /// pseudoconsola; recién ahí el lector recibe el fin de archivo.
    /// </summary>
    private void VigilarProceso()
    {
        WaitForSingleObject(_proceso, INFINITE);
        CerrarPseudoconsola();
    }

    /// <summary>Cierra la pseudoconsola una sola vez (la llaman el vigía y Dispose).</summary>
    private void CerrarPseudoconsola()
    {
        var pc = Interlocked.Exchange(ref _pseudoconsola, IntPtr.Zero);
        if (pc != IntPtr.Zero) ClosePseudoConsole(pc);
    }

    /// <summary>Manda texto como si se tipeara (Enter es "\r", Ctrl+C es "\x03").</summary>
    public void Escribir(string texto)
    {
        if (_entrada is null || !Corriendo) return;

        var bytes = Encoding.UTF8.GetBytes(texto);
        _entrada.Write(bytes, 0, bytes.Length);
        _entrada.Flush();
    }

    /// <summary>La consola cambió de tamaño: el programa se entera y repinta.</summary>
    public void Redimensionar(short columnas, short filas)
    {
        if (_pseudoconsola == IntPtr.Zero) return;
        ResizePseudoConsole(_pseudoconsola, new COORD { X = columnas, Y = filas });
    }

    /// <summary>Código de salida del programa, o null si sigue corriendo.</summary>
    public int? CodigoDeSalida()
    {
        if (_proceso == IntPtr.Zero) return null;
        if (!GetExitCodeProcess(_proceso, out uint codigo)) return null;
        return codigo == STILL_ACTIVE ? null : (int)codigo;
    }

    private void Leer()
    {
        var buffer = new byte[16 * 1024];

        try
        {
            using var salida = new FileStream(_lecturaSalida!, FileAccess.Read, 1);

            while (true)
            {
                int n = salida.Read(buffer, 0, buffer.Length);
                if (n <= 0) break;

                var copia = new byte[n];
                Buffer.BlockCopy(buffer, 0, copia, 0, n);
                DatosRecibidos?.Invoke(copia, n);
            }
        }
        catch (IOException) { /* el caño se cerró: terminó */ }
        catch (ObjectDisposedException) { }

        Corriendo = false;
        Terminado?.Invoke();
    }

    private void CrearProceso(string lineaDeComandos, string? carpeta, IDictionary<string, string>? variablesExtra)
    {
        var si = new STARTUPINFOEX();
        si.StartupInfo.cb = Marshal.SizeOf<STARTUPINFOEX>();

        // ⚠ STARTF_USESTDHANDLES CON LOS TRES MANEJADORES EN NULO. Sin esto, si
        // NUESTRA salida está redirigida (un script que captura, un servicio),
        // el programa hereda esos manejadores y escribe ahí en vez de en la
        // pseudoconsola: la terminal queda en blanco. Medido el 23/09 con
        // PruebaTerminal --prueba capturado desde PowerShell: el prompt salía
        // por la consola del script.
        si.StartupInfo.dwFlags = STARTF_USESTDHANDLES;

        // Lista de atributos con UN atributo: "este proceso usa esta pseudoconsola".
        IntPtr tamano = IntPtr.Zero;
        InitializeProcThreadAttributeList(IntPtr.Zero, 1, 0, ref tamano);
        si.lpAttributeList = Marshal.AllocHGlobal(tamano);

        IntPtr bloqueEntorno = IntPtr.Zero;

        try
        {
            if (!InitializeProcThreadAttributeList(si.lpAttributeList, 1, 0, ref tamano))
                Fallar("InitializeProcThreadAttributeList");

            if (!UpdateProcThreadAttribute(si.lpAttributeList, 0, (IntPtr)PROC_THREAD_ATTRIBUTE_PSEUDOCONSOLE,
                                           _pseudoconsola, (IntPtr)IntPtr.Size, IntPtr.Zero, IntPtr.Zero))
                Fallar("UpdateProcThreadAttribute");

            uint banderas = EXTENDED_STARTUPINFO_PRESENT;

            if (variablesExtra is { Count: > 0 })
            {
                bloqueEntorno = CrearBloqueDeEntorno(variablesExtra);
                banderas |= CREATE_UNICODE_ENVIRONMENT;
            }

            if (!CreateProcess(null, new StringBuilder(lineaDeComandos), IntPtr.Zero, IntPtr.Zero, false,
                               banderas, bloqueEntorno, carpeta, ref si, out var pi))
                Fallar("CreateProcess");

            _proceso = pi.hProcess;
            _hilo = pi.hThread;
        }
        finally
        {
            DeleteProcThreadAttributeList(si.lpAttributeList);
            Marshal.FreeHGlobal(si.lpAttributeList);
            if (bloqueEntorno != IntPtr.Zero) Marshal.FreeHGlobal(bloqueEntorno);
        }
    }

    /// <summary>
    /// El entorno del proceso: el nuestro más las variables extra (pisan a las
    /// heredadas del mismo nombre). Formato Unicode "NOMBRE=valor\0...\0\0".
    /// </summary>
    private static IntPtr CrearBloqueDeEntorno(IDictionary<string, string> extra)
    {
        var todas = new SortedDictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (System.Collections.DictionaryEntry v in Environment.GetEnvironmentVariables())
            todas[(string)v.Key] = (string?)v.Value ?? "";

        foreach (var v in extra) todas[v.Key] = v.Value;

        var sb = new StringBuilder();
        foreach (var v in todas) sb.Append(v.Key).Append('=').Append(v.Value).Append('\0');
        sb.Append('\0');

        return Marshal.StringToHGlobalUni(sb.ToString());
    }

    private static void Fallar(string que) => throw new Win32Exception(Marshal.GetLastWin32Error(), que);

    public void Dispose()
    {
        // Cerrar la pseudoconsola termina el programa y hace que el caño de
        // salida dé fin de archivo: el hilo lector sale solo.
        CerrarPseudoconsola();

        _entrada?.Dispose();
        _escrituraEntrada?.Dispose();
        _lector?.Join(2000);

        if (_proceso != IntPtr.Zero) { CloseHandle(_proceso); _proceso = IntPtr.Zero; }
        if (_hilo != IntPtr.Zero) { CloseHandle(_hilo); _hilo = IntPtr.Zero; }
    }

    // ------------------------------------------------------------------
    // Win32
    // ------------------------------------------------------------------

    private const uint EXTENDED_STARTUPINFO_PRESENT = 0x00080000;
    private const uint CREATE_UNICODE_ENVIRONMENT = 0x00000400;
    private const int PROC_THREAD_ATTRIBUTE_PSEUDOCONSOLE = 0x00020016;
    private const uint STILL_ACTIVE = 259;
    private const int STARTF_USESTDHANDLES = 0x00000100;

    [StructLayout(LayoutKind.Sequential)]
    private struct COORD { public short X, Y; }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct STARTUPINFO
    {
        public int cb;
        public string? lpReserved, lpDesktop, lpTitle;
        public int dwX, dwY, dwXSize, dwYSize, dwXCountChars, dwYCountChars, dwFillAttribute, dwFlags;
        public short wShowWindow, cbReserved2;
        public IntPtr lpReserved2, hStdInput, hStdOutput, hStdError;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct STARTUPINFOEX
    {
        public STARTUPINFO StartupInfo;
        public IntPtr lpAttributeList;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct PROCESS_INFORMATION
    {
        public IntPtr hProcess, hThread;
        public int dwProcessId, dwThreadId;
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern int CreatePseudoConsole(COORD size, SafeFileHandle hInput, SafeFileHandle hOutput, uint flags, out IntPtr phPC);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern int ResizePseudoConsole(IntPtr hPC, COORD size);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern void ClosePseudoConsole(IntPtr hPC);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool CreatePipe(out SafeFileHandle hReadPipe, out SafeFileHandle hWritePipe, IntPtr lpPipeAttributes, int nSize);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool InitializeProcThreadAttributeList(IntPtr lpAttributeList, int dwAttributeCount, int dwFlags, ref IntPtr lpSize);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool UpdateProcThreadAttribute(IntPtr lpAttributeList, uint dwFlags, IntPtr attribute,
                                                         IntPtr lpValue, IntPtr cbSize, IntPtr lpPreviousValue, IntPtr lpReturnSize);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern void DeleteProcThreadAttributeList(IntPtr lpAttributeList);

    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern bool CreateProcess(string? lpApplicationName, StringBuilder lpCommandLine,
                                             IntPtr lpProcessAttributes, IntPtr lpThreadAttributes, bool bInheritHandles,
                                             uint dwCreationFlags, IntPtr lpEnvironment, string? lpCurrentDirectory,
                                             ref STARTUPINFOEX lpStartupInfo, out PROCESS_INFORMATION lpProcessInformation);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool GetExitCodeProcess(IntPtr hProcess, out uint lpExitCode);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool CloseHandle(IntPtr hObject);

    private const uint INFINITE = 0xFFFFFFFF;

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern uint WaitForSingleObject(IntPtr hHandle, uint dwMilliseconds);
}
