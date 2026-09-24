namespace AsmEditor.Core;

/// <summary>
/// El informe de «Configuración &gt; Verificar herramientas».
///
/// ⚠ EL INFORME HABLA DEL TARGET ACTIVO, NO DE NASM+GoLink FIJOS. Antes
/// reportaba siempre esas dos herramientas y mostraba la línea de NASM y la de
/// GoLink aunque el target ensamblara con MASM y enlazara con MSVC: decía que
/// faltaba GoLink en una cadena que no lo usa, y mostraba un «-f win64» que
/// MASM ni entiende. El informe tiene que nombrar las herramientas que la
/// compilación va a ejecutar de verdad.
///
/// Está separado de MainForm a propósito: acá es una función pura —entra un
/// target y las rutas resueltas, sale el texto— y por eso se puede probar.
/// La ventana solo lo muestra.
/// </summary>
public static class ToolReport
{
    /// <summary>
    /// Las rutas ya resueltas de las herramientas. Se pasan desde afuera para
    /// que las pruebas no dependan de lo que esté instalado en la máquina.
    /// null significa «no se encontró».
    /// </summary>
    public sealed class Rutas
    {
        /// <summary>Ruta del ensamblador que corresponde al target (NASM o MASM).</summary>
        public string? Ensamblador { get; init; }

        /// <summary>Ruta del enlazador que corresponde al target (GoLink o link.exe).</summary>
        public string? Enlazador { get; init; }

        /// <summary>Carpeta de los .lib del SDK. Solo la usa el enlazador de MSVC.</summary>
        public string? SdkLib { get; init; }

        // De dónde salió cada ruta. Sin esto el informe dice dónde está la
        // herramienta pero no si la elegiste vos o la eligió el editor, que es
        // justo lo que hace falta saber cuando hay 10 toolchains instalados.
        public ToolSource FuenteEnsamblador { get; init; } = ToolSource.Automatico;
        public ToolSource FuenteEnlazador { get; init; } = ToolSource.Automatico;
        public ToolSource FuenteSdk { get; init; } = ToolSource.Automatico;

        /// <summary>Cuántos juegos de MSVC hay instalados para esta arquitectura.</summary>
        public int ToolchainsDisponibles { get; init; }

        /// <summary>Cuántas versiones del SDK hay.</summary>
        public int SdksDisponibles { get; init; }
    }

    /// <summary>Cómo se lee cada procedencia en el informe.</summary>
    private static string TextoDeFuente(ToolSource f) => f switch
    {
        ToolSource.Target => "fijado en el target",
        ToolSource.Configuracion => "elegido en Configuración",
        _ => "detectado automáticamente"
    };

    /// <summary>Nombre del ejecutable del ensamblador que le toca al target.</summary>
    public static string NombreDelEnsamblador(BuildTarget t) =>
        !t.UsaMasm ? "NASM"
        : t.Arch == TargetArch.Win32 ? "MASM (ml.exe)"
        : "MASM (ml64.exe)";

    /// <summary>Nombre del ejecutable del enlazador que le toca al target.</summary>
    public static string NombreDelEnlazador(BuildTarget t) =>
        t.Linker == LinkerKind.MsvcLink ? "MSVC (link.exe)" : "GoLink";

    /// <summary>
    /// Arma el texto del informe. No toca el disco: todo lo que depende de la
    /// máquina entra por <paramref name="rutas"/>.
    /// </summary>
    public static string Construir(BuildTarget target, Rutas rutas)
    {
        var sb = new System.Text.StringBuilder();

        sb.AppendLine($"Target activo: {target.DisplayName}");
        sb.AppendLine();

        // ── El ensamblador ───────────────────────────────────────────────
        var nomAsm = NombreDelEnsamblador(target);
        sb.AppendLine(rutas.Ensamblador is not null
            ? $"OK  - {nomAsm} encontrado ({TextoDeFuente(rutas.FuenteEnsamblador)}):"
              + $"{Environment.NewLine}      {rutas.Ensamblador}"
            : $"MAL - {nomAsm} NO encontrado");

        // ⚠ BuildAssemblerArguments, NO BuildNasmArguments: con un target de
        // MASM la línea de NASM muestra un «-f win64» que ml64.exe rechaza.
        sb.AppendLine($"      {target.BuildAssemblerArguments("programa.asm", "programa.obj")}");

        // ── El enlazador ─────────────────────────────────────────────────
        // Solo si el target realmente enlaza: los de solo .obj no lo usan.
        if (target.ProducesExecutable)
        {
            sb.AppendLine();

            var nomLink = NombreDelEnlazador(target);
            sb.AppendLine(rutas.Enlazador is not null
                ? $"OK  - {nomLink} encontrado ({TextoDeFuente(rutas.FuenteEnlazador)}):"
                  + $"{Environment.NewLine}      {rutas.Enlazador}"
                : $"MAL - {nomLink} NO encontrado");

            sb.AppendLine($"      {ArgumentosDeEnlazado(target, rutas.SdkLib)}");

            // Las librerías del SDK son cosa de link.exe; GoLink enlaza contra
            // los .dll directamente y no las necesita.
            if (target.Linker == LinkerKind.MsvcLink)
            {
                sb.AppendLine();
                sb.AppendLine(rutas.SdkLib is not null
                    ? $"OK  - Librerías del SDK ({TextoDeFuente(rutas.FuenteSdk)}):"
                      + $"{Environment.NewLine}      {rutas.SdkLib}"
                    : "MAL - Librerías del SDK NO encontradas (el enlazado puede fallar con LNK1181)");
            }
        }
        else
        {
            sb.AppendLine();
            sb.AppendLine("El target solo genera .obj: no se enlaza.");
        }

        // Cuántas alternativas hay. Con una sola instalación no aporta nada,
        // pero en este equipo hay 10 y saber que existen es media explicación
        // cuando el enlazado se porta distinto de lo esperado.
        if (rutas.ToolchainsDisponibles > 1)
        {
            sb.AppendLine();
            sb.AppendLine($"Hay {rutas.ToolchainsDisponibles} juegos de herramientas de MSVC " +
                          $"instalados para {target.ArchText}.");

            if (rutas.SdksDisponibles > 1)
            {
                sb.AppendLine($"Hay {rutas.SdksDisponibles} versiones del SDK de Windows.");
            }

            sb.AppendLine("Se eligen en Configuración > Opciones > Herramientas > MSVC.");
        }

        return sb.ToString();
    }

    private static string ArgumentosDeEnlazado(BuildTarget t, string? sdkLib) =>
        t.Linker == LinkerKind.MsvcLink
            ? t.BuildMsvcArguments("programa.obj", "programa.exe", sdkLib)
            : t.BuildGoLinkArguments("programa.obj");
}
