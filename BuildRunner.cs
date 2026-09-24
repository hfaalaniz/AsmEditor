using System.Diagnostics;

namespace AsmEditor;

/// <summary>Cómo terminó la ejecución de una herramienta externa.</summary>
public enum RunOutcome
{
    Success,
    Failed,
    Canceled
}

public sealed record RunResult(RunOutcome Outcome, int ExitCode)
{
    public bool Succeeded => Outcome == RunOutcome.Success;
    public bool WasCanceled => Outcome == RunOutcome.Canceled;
}

/// <summary>
/// Ejecuta procesos externos (nasm.exe, GoLink.exe) y reporta su salida línea por línea.
/// Admite cancelación: al cancelar se mata el árbol de procesos, porque cancelar el
/// token por sí solo no detiene un proceso que ya está corriendo.
/// </summary>
public class BuildRunner
{
    public event Action<string>? OutputReceived;

    /// <summary>Salida cruda línea por línea, para que el parser de diagnósticos la interprete.</summary>
    public event Action<string>? LineReceived;

    public async Task<RunResult> RunAsync(
        string exePath,
        string arguments,
        string workingDirectory,
        CancellationToken cancellationToken = default)
    {
        OutputReceived?.Invoke($"> {Path.GetFileName(exePath)} {arguments}{Environment.NewLine}");

        if (!File.Exists(exePath))
        {
            var msg = $"ERROR: no se encontró el ejecutable en '{exePath}'. " +
                      $"Revisá Configuración > Opciones > Herramientas.";
            OutputReceived?.Invoke(msg + Environment.NewLine);
            LineReceived?.Invoke(msg);
            return new RunResult(RunOutcome.Failed, -1);
        }

        var psi = new ProcessStartInfo
        {
            FileName = exePath,
            Arguments = arguments,
            WorkingDirectory = workingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = new Process { StartInfo = psi, EnableRaisingEvents = true };

        void Handle(string? data)
        {
            if (data is null) return;
            OutputReceived?.Invoke(data + Environment.NewLine);
            LineReceived?.Invoke(data);
        }

        process.OutputDataReceived += (_, e) => Handle(e.Data);
        process.ErrorDataReceived += (_, e) => Handle(e.Data);

        try
        {
            process.Start();
        }
        catch (Exception ex)
        {
            var msg = $"ERROR al iniciar el proceso: {ex.Message}";
            OutputReceived?.Invoke(msg + Environment.NewLine);
            LineReceived?.Invoke(msg);
            return new RunResult(RunOutcome.Failed, -1);
        }

        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        bool canceled = false;

        try
        {
            await process.WaitForExitAsync(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            canceled = true;
            KillProcessTree(process);

            // Esperamos la salida real del proceso ya muerto, sin token: si no,
            // el ExitCode puede consultarse antes de que el proceso termine de morir.
            try { await process.WaitForExitAsync(CancellationToken.None); }
            catch { /* el proceso ya no existe */ }
        }

        if (canceled)
        {
            OutputReceived?.Invoke($"** Compilación cancelada por el usuario **{Environment.NewLine}{Environment.NewLine}");
            return new RunResult(RunOutcome.Canceled, -1);
        }

        int exitCode = SafeExitCode(process);
        OutputReceived?.Invoke($"(código de salida: {exitCode}){Environment.NewLine}{Environment.NewLine}");

        return new RunResult(exitCode == 0 ? RunOutcome.Success : RunOutcome.Failed, exitCode);
    }

    /// <summary>
    /// Mata el proceso y sus hijos. Va envuelto porque entre la comprobación y el Kill
    /// el proceso puede haber terminado solo, y eso lanza excepción.
    /// </summary>
    private static void KillProcessTree(Process process)
    {
        try
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
            }
        }
        catch (InvalidOperationException)
        {
            // El proceso ya había terminado.
        }
        catch (NotSupportedException)
        {
            // Plataforma sin soporte para el árbol: al menos intentamos el proceso solo.
            try { process.Kill(); } catch { }
        }
        catch (Exception)
        {
            // Cancelar nunca debe tumbar al editor.
        }
    }

    private static int SafeExitCode(Process process)
    {
        try { return process.ExitCode; }
        catch { return -1; }
    }
}
