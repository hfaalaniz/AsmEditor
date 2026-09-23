namespace PruebaTerminal;

/// <summary>Dónde están las herramientas y cómo se arma el entorno de la terminal.</summary>
public static class Configuracion
{
    public const string CarpetaNasm = @"C:\Users\Fabian\NASM";

    /// <summary>
    /// El PATH con la carpeta de NASM y GoLink adelante: en la terminal se puede
    /// tipear "nasm" sin la ruta, como en el "Developer PowerShell" de VS.
    /// </summary>
    public static Dictionary<string, string> EntornoConNasm() => new()
    {
        ["PATH"] = CarpetaNasm + ";" + (Environment.GetEnvironmentVariable("PATH") ?? "")
    };
}
