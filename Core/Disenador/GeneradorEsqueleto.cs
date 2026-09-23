using System.Text;

namespace AsmEditor.Core.Disenador;

/// <summary>
/// Genera el .asm del usuario: el que tiene el procedimiento de ventana y los
/// manejadores de cada control.
///
/// ⚠ ESTE ARCHIVO SE GENERA UNA SOLA VEZ Y NUNCA SE PISA. Es donde el usuario
/// escribe su código. Si existe, el diseñador NO lo vuelve a tocar aunque se
/// agreguen controles: lo único que se regenera es el .inc.
///
/// Cuando aparecen controles nuevos, en vez de reescribir el archivo se le
/// ofrece al usuario el texto de los manejadores que faltan, para que los
/// pegue donde quiera (ver <see cref="GenerarManejadoresFaltantes"/>).
/// </summary>
public static class GeneradorEsqueleto
{
    /// <summary>
    /// El .asm inicial completo: extern, include, punto de entrada,
    /// procedimiento de ventana y un manejador por cada control que notifica.
    /// </summary>
    /// <param name="nombreInclude">
    /// Nombre del archivo .inc a incluir, SIN extensión.
    ///
    /// ⚠ NO SE DEDUCE DEL NOMBRE DEL FORMULARIO: los archivos se llaman como el
    /// .asmform que el usuario eligió al guardar, que no tiene por qué
    /// coincidir con el nombre del formulario. Deducirlo generaba un
    /// «%include "Formulario1.inc"» junto a un archivo llamado «Ciclo.inc», y
    /// NASM fallaba con «unable to open include file».
    ///
    /// Vacío o null = usar el nombre del formulario (solo para pruebas que no
    /// pasan por un archivo).
    /// </param>
    public static string Generar(FormularioDisenado f, string? nombreInclude = null)
    {
        var sb = new StringBuilder();

        var inc = string.IsNullOrWhiteSpace(nombreInclude)
            ? f.Nombre.Trim()
            : nombreInclude!.Trim();

        EscribirEncabezado(sb, f, inc);
        EscribirExternos(sb, f);
        sb.AppendLine($"%include \"{inc}.inc\"");
        sb.AppendLine();

        if (f.EsVentanaPrincipal) EscribirPuntoDeEntrada(sb, f);

        EscribirProcedimiento(sb, f);

        return sb.ToString();
    }

    private static void EscribirEncabezado(StringBuilder sb, FormularioDisenado f, string nombreInclude)
    {
        sb.AppendLine("; ==========================================================================");
        sb.AppendLine($"; {nombreInclude}.asm — {f.Titulo}");
        sb.AppendLine(";");
        sb.AppendLine("; Este archivo es TUYO: el diseñador lo genera una sola vez y después no");
        sb.AppendLine("; lo toca más. Escribí acá lo que hace tu programa.");
        sb.AppendLine(";");
        sb.AppendLine($"; Lo que se regenera solo es {nombreInclude}.inc, cada vez que guardás el");
        sb.AppendLine("; formulario en el diseñador. Ese no lo edites.");
        sb.AppendLine("; ==========================================================================");
        sb.AppendLine();

        if (f.EsX64) sb.AppendLine("default rel");
        else sb.AppendLine("; x86: sin 'default rel', que es de x64");

        sb.AppendLine();
    }

    private static void EscribirExternos(StringBuilder sb, FormularioDisenado f)
    {
        sb.AppendLine("; ---- Funciones de la API que usa el código generado ----");

        foreach (var api in ApisNecesarias(f))
        {
            sb.AppendLine($"extern {api}");
        }

        sb.AppendLine();
    }

    /// <summary>
    /// Las funciones de la API que hay que declarar extern.
    ///
    /// ⚠ SE CALCULA, NO SE LISTA FIJO: declarar un extern que no se usa hace
    /// que GoLink traiga un .dll de más, y olvidar uno es un error de enlazado.
    /// La lista sale de lo que el .inc y el esqueleto llaman de verdad.
    /// </summary>
    public static List<string> ApisNecesarias(FormularioDisenado f)
    {
        var apis = new List<string>
        {
            // Las llama CrearControles, en cualquier formulario.
            "CreateWindowExA",
            "DefWindowProcA"
        };

        if (f.EsVentanaPrincipal)
        {
            apis.AddRange(new[]
            {
                "GetModuleHandleA",
                "RegisterClassExA",
                "LoadIconA",
                "LoadCursorA",
                "GetStockObject",
                "ShowWindow",
                "UpdateWindow",
                "GetMessageA",
                "TranslateMessage",
                "DispatchMessageA",
                "PostQuitMessage",
                "ExitProcess"
            });
        }

        // GetDlgItem solo hace falta si hay algo que responda.
        if (f.Controles.Any(c => InfoTipoControl.NotificaPorComando(c.Tipo)))
        {
            apis.Add("GetDlgItem");
            apis.Add("SendMessageA");
        }

        return apis.Distinct().ToList();
    }

    private static void EscribirPuntoDeEntrada(StringBuilder sb, FormularioDisenado f)
    {
        var entrada = f.EsX64 ? "main" : "_main";

        sb.AppendLine("section .text");
        sb.AppendLine($"global {entrada}");
        sb.AppendLine();
        sb.AppendLine("; ==========================================================================");
        sb.AppendLine("; Punto de entrada del programa");
        sb.AppendLine("; ==========================================================================");
        sb.AppendLine($"{entrada}:");

        if (f.EsX64)
        {
            sb.AppendLine("    sub rsp, 40                    ; alineación de pila de la ABI x64");
            sb.AppendLine();
            sb.AppendLine($"    call {GeneradorAsm.SimboloCrearVentana(f)}");
            sb.AppendLine("    test rax, rax");
            sb.AppendLine("    jz .salir                      ; no se pudo crear la ventana");
            sb.AppendLine();
            sb.AppendLine($"    call {GeneradorAsm.SimboloMostrar(f)}   ; no vuelve hasta que se cierra");
            sb.AppendLine();
            sb.AppendLine(".salir:");
            sb.AppendLine("    xor ecx, ecx");
            sb.AppendLine("    call ExitProcess");
        }
        else
        {
            sb.AppendLine($"    call {GeneradorAsm.SimboloCrearVentana(f)}");
            sb.AppendLine("    test eax, eax");
            sb.AppendLine("    jz .salir");
            sb.AppendLine();
            sb.AppendLine($"    call {GeneradorAsm.SimboloMostrar(f)}");
            sb.AppendLine();
            sb.AppendLine(".salir:");
            sb.AppendLine("    push dword 0");
            sb.AppendLine("    call ExitProcess");
        }

        sb.AppendLine();
    }

    private static void EscribirProcedimiento(StringBuilder sb, FormularioDisenado f)
    {
        if (!f.EsVentanaPrincipal)
        {
            sb.AppendLine("section .text");
            sb.AppendLine();
        }

        if (f.EsX64) EscribirProcedimientoX64(sb, f);
        else EscribirProcedimientoX86(sb, f);
    }

    private static void EscribirProcedimientoX64(StringBuilder sb, FormularioDisenado f)
    {
        var notifican = f.Controles.Where(c => InfoTipoControl.NotificaPorComando(c.Tipo)).ToList();

        sb.AppendLine("; ==========================================================================");
        sb.AppendLine($"; {f.SimboloProc} — procedimiento de ventana");
        sb.AppendLine(";   RCX = hWnd   RDX = uMsg   R8 = wParam   R9 = lParam");
        sb.AppendLine(";");
        sb.AppendLine("; Acá llega todo lo que le pasa a la ventana. Windows lo llama a vos,");
        sb.AppendLine("; no al revés.");
        sb.AppendLine("; ==========================================================================");
        sb.AppendLine($"{f.SimboloProc}:");
        sb.AppendLine("    push rbx");
        sb.AppendLine("    push rsi");
        sb.AppendLine("    sub rsp, 40                    ; 2 push (16) + 40 + 8 del call = 64");
        sb.AppendLine();
        sb.AppendLine("    mov rbx, rcx                   ; rbx = hWnd (sobrevive a los call)");
        sb.AppendLine("    mov esi, edx                   ; esi = uMsg");
        sb.AppendLine();
        sb.AppendLine("    cmp esi, WM_CREATE");
        sb.AppendLine("    je .crear");
        sb.AppendLine("    cmp esi, WM_COMMAND");
        sb.AppendLine("    je .comando");
        sb.AppendLine("    cmp esi, WM_DESTROY");
        sb.AppendLine("    je .destruir");
        sb.AppendLine();
        sb.AppendLine(".pordefecto:");
        sb.AppendLine("    ; Todo lo que no atendemos lo maneja Windows. Sin esto la ventana");
        sb.AppendLine("    ; no se mueve, no se cierra y no se dibuja.");
        sb.AppendLine("    mov rcx, rbx");
        sb.AppendLine("    mov edx, esi");
        sb.AppendLine("    call DefWindowProcA");
        sb.AppendLine("    jmp .salir");
        sb.AppendLine();
        sb.AppendLine(".crear:");
        sb.AppendLine("    ; La ventana ya existe pero todavía no se dibujó: es el momento");
        sb.AppendLine("    ; de crear los controles.");
        sb.AppendLine("    mov rcx, rbx");
        sb.AppendLine($"    call {f.SimboloCrearControles}");
        sb.AppendLine("    xor eax, eax");
        sb.AppendLine("    jmp .salir");
        sb.AppendLine();
        sb.AppendLine(".comando:");

        if (notifican.Count == 0)
        {
            sb.AppendLine("    ; Todavía no hay controles que avisen por WM_COMMAND.");
            sb.AppendLine("    xor eax, eax");
            sb.AppendLine("    jmp .salir");
        }
        else
        {
            sb.AppendLine("    ; LOWORD(wParam) trae el ID del control que avisa.");
            sb.AppendLine("    movzx eax, r8w");
            sb.AppendLine();

            foreach (var c in notifican)
            {
                sb.AppendLine($"    cmp eax, {c.SimboloId}");
                sb.AppendLine($"    je {EtiquetaManejador(c)}");
            }

            sb.AppendLine();
            sb.AppendLine("    xor eax, eax");
            sb.AppendLine("    jmp .salir");
        }

        sb.AppendLine();
        sb.AppendLine(".destruir:");

        if (f.EsVentanaPrincipal)
        {
            sb.AppendLine("    ; Se cerró la ventana principal: se le avisa al bucle que corte.");
            sb.AppendLine("    xor ecx, ecx");
            sb.AppendLine("    call PostQuitMessage");
        }
        else
        {
            // ⚠ UN DIÁLOGO NO LLAMA PostQuitMessage: eso manda WM_QUIT al bucle
            // de mensajes, que es el de la ventana PRINCIPAL, y cerrar el
            // diálogo terminaría el programa entero.
            sb.AppendLine("    ; Se cerró el diálogo. NO se llama a PostQuitMessage: eso cortaría");
            sb.AppendLine("    ; el bucle de mensajes de la ventana principal y terminaría el");
            sb.AppendLine("    ; programa entero. Un diálogo solo desaparece.");
        }

        sb.AppendLine("    xor eax, eax");
        sb.AppendLine("    jmp .salir");

        foreach (var c in notifican)
        {
            sb.AppendLine();
            EscribirManejadorX64(sb, c);
        }

        sb.AppendLine();
        sb.AppendLine(".salir:");
        sb.AppendLine("    add rsp, 40");
        sb.AppendLine("    pop rsi");
        sb.AppendLine("    pop rbx");
        sb.AppendLine("    ret");
        sb.AppendLine();
    }

    private static void EscribirManejadorX64(StringBuilder sb, ControlDisenado c)
    {
        sb.AppendLine($"{EtiquetaManejador(c)}:");
        sb.AppendLine($"    ; ---- {InfoTipoControl.Nombre(c.Tipo)} '{TextoParaComentario(c.Texto)}' ({c.Nombre}) ----");
        sb.AppendLine("    ; Tu código va acá. El hWnd de la ventana está en rbx.");

        switch (c.Tipo)
        {
            case TipoControl.Boton:
                sb.AppendLine("    ;");
                sb.AppendLine("    ; Para leer un campo de texto:");
                sb.AppendLine("    ;   sub rsp, 32");
                sb.AppendLine("    ;   mov rcx, rbx");
                sb.AppendLine("    ;   mov edx, IDC_TUCAMPO");
                sb.AppendLine("    ;   lea r8, [buffer]");
                sb.AppendLine("    ;   mov r9d, 64");
                sb.AppendLine("    ;   call GetDlgItemTextA");
                sb.AppendLine("    ;   add rsp, 32");
                break;

            case TipoControl.Casilla:
                sb.AppendLine("    ;");
                sb.AppendLine("    ; Para saber si quedó marcada (devuelve 1 si sí):");
                sb.AppendLine("    ;   sub rsp, 32");
                sb.AppendLine("    ;   mov rcx, rbx");
                sb.AppendLine($"    ;   mov edx, {c.SimboloId}");
                sb.AppendLine("    ;   call GetDlgItem");
                sb.AppendLine("    ;   mov rcx, rax");
                sb.AppendLine("    ;   mov edx, BM_GETCHECK");
                sb.AppendLine("    ;   xor r8d, r8d");
                sb.AppendLine("    ;   xor r9d, r9d");
                sb.AppendLine("    ;   call SendMessageA");
                sb.AppendLine("    ;   add rsp, 32");
                break;

            case TipoControl.Lista:
                sb.AppendLine("    ;");
                sb.AppendLine("    ; Para agregarle una línea:");
                sb.AppendLine("    ;   sub rsp, 32");
                sb.AppendLine("    ;   mov rcx, rbx");
                sb.AppendLine($"    ;   mov edx, {c.SimboloId}");
                sb.AppendLine("    ;   call GetDlgItem");
                sb.AppendLine("    ;   mov rcx, rax");
                sb.AppendLine("    ;   mov edx, LB_ADDSTRING");
                sb.AppendLine("    ;   xor r8d, r8d");
                sb.AppendLine("    ;   lea r9, [miTexto]");
                sb.AppendLine("    ;   call SendMessageA");
                sb.AppendLine("    ;   add rsp, 32");
                break;

            case TipoControl.Desplegable:
                sb.AppendLine("    ;");
                sb.AppendLine("    ; Para saber qué opción eligió (devuelve el índice, -1 si ninguna):");
                sb.AppendLine("    ;   mov edx, CB_GETCURSEL  con SendMessageA, como arriba");
                break;
        }

        sb.AppendLine();
        sb.AppendLine("    xor eax, eax");
        sb.AppendLine("    jmp .salir");
    }

    private static void EscribirProcedimientoX86(StringBuilder sb, FormularioDisenado f)
    {
        var notifican = f.Controles.Where(c => InfoTipoControl.NotificaPorComando(c.Tipo)).ToList();

        sb.AppendLine("; ==========================================================================");
        sb.AppendLine($"; {f.SimboloProc} — procedimiento de ventana (x86, stdcall)");
        sb.AppendLine(";   [ebp+8]=hWnd  [ebp+12]=uMsg  [ebp+16]=wParam  [ebp+20]=lParam");
        sb.AppendLine("; ==========================================================================");
        sb.AppendLine($"{f.SimboloProc}:");
        sb.AppendLine("    push ebp");
        sb.AppendLine("    mov ebp, esp");
        sb.AppendLine("    push ebx");
        sb.AppendLine();
        sb.AppendLine("    mov ebx, [ebp+8]               ; hWnd");
        sb.AppendLine("    mov eax, [ebp+12]              ; uMsg");
        sb.AppendLine();
        sb.AppendLine("    cmp eax, WM_CREATE");
        sb.AppendLine("    je .crear");
        sb.AppendLine("    cmp eax, WM_COMMAND");
        sb.AppendLine("    je .comando");
        sb.AppendLine("    cmp eax, WM_DESTROY");
        sb.AppendLine("    je .destruir");
        sb.AppendLine();
        sb.AppendLine(".pordefecto:");
        sb.AppendLine("    push dword [ebp+20]");
        sb.AppendLine("    push dword [ebp+16]");
        sb.AppendLine("    push dword [ebp+12]");
        sb.AppendLine("    push ebx");
        sb.AppendLine("    call DefWindowProcA");
        sb.AppendLine("    jmp .salir");
        sb.AppendLine();
        sb.AppendLine(".crear:");
        sb.AppendLine("    push ebx");
        sb.AppendLine($"    call {f.SimboloCrearControles}");
        sb.AppendLine("    xor eax, eax");
        sb.AppendLine("    jmp .salir");
        sb.AppendLine();
        sb.AppendLine(".comando:");

        if (notifican.Count == 0)
        {
            sb.AppendLine("    ; Todavía no hay controles que avisen por WM_COMMAND.");
            sb.AppendLine("    xor eax, eax");
            sb.AppendLine("    jmp .salir");
        }
        else
        {
            sb.AppendLine("    movzx eax, word [ebp+16]       ; LOWORD(wParam) = ID del control");
            sb.AppendLine();

            foreach (var c in notifican)
            {
                sb.AppendLine($"    cmp eax, {c.SimboloId}");
                sb.AppendLine($"    je {EtiquetaManejador(c)}");
            }

            sb.AppendLine();
            sb.AppendLine("    xor eax, eax");
            sb.AppendLine("    jmp .salir");
        }

        sb.AppendLine();
        sb.AppendLine(".destruir:");

        if (f.EsVentanaPrincipal)
        {
            sb.AppendLine("    push dword 0");
            sb.AppendLine("    call PostQuitMessage");
        }
        else
        {
            // Ver el aviso del equivalente x64: un diálogo no corta el bucle.
            sb.AppendLine("    ; Se cerró el diálogo. NO se llama a PostQuitMessage: cortaría el");
            sb.AppendLine("    ; bucle de la ventana principal y terminaría el programa.");
        }

        sb.AppendLine("    xor eax, eax");
        sb.AppendLine("    jmp .salir");

        foreach (var c in notifican)
        {
            sb.AppendLine();
            sb.AppendLine($"{EtiquetaManejador(c)}:");
            sb.AppendLine($"    ; ---- {InfoTipoControl.Nombre(c.Tipo)} '{TextoParaComentario(c.Texto)}' ({c.Nombre}) ----");
            sb.AppendLine("    ; Tu código va acá. El hWnd está en ebx.");
            sb.AppendLine("    xor eax, eax");
            sb.AppendLine("    jmp .salir");
        }

        sb.AppendLine();
        sb.AppendLine(".salir:");
        sb.AppendLine("    pop ebx");
        sb.AppendLine("    mov esp, ebp");
        sb.AppendLine("    pop ebp");
        sb.AppendLine("    ret 16                         ; stdcall: saca los 4 argumentos");
        sb.AppendLine();
    }

    /// <summary>
    /// La etiqueta local del manejador de un control. Es local (arranca con
    /// punto) para que quede dentro del procedimiento y no choque con nada.
    /// </summary>
    public static string EtiquetaManejador(ControlDisenado c) =>
        ".al_" + c.Nombre.Trim().ToLowerInvariant();

    /// <summary>
    /// Deja un texto del usuario en condiciones de ir DENTRO DE UN COMENTARIO
    /// del .asm.
    ///
    /// ⚠ UN COMENTARIO DE NASM TERMINA EN EL FIN DE LÍNEA: si el texto del
    /// control trae un salto, la segunda mitad queda fuera del comentario y el
    /// ensamblador la lee como instrucción. Un botón con el texto "Aceptar\ny
    /// seguir" hacía fallar la compilación con «instruction expected».
    ///
    /// Los saltos se muestran como «↵» para que el comentario siga diciendo
    /// cuál es el control, y el texto se recorta si es muy largo.
    /// </summary>
    public static string TextoParaComentario(string? texto, int maximo = 40)
    {
        if (string.IsNullOrEmpty(texto)) return "";

        var limpio = texto
            .Replace("\r\n", "↵")
            .Replace("\n", "↵")
            .Replace("\r", "↵")
            .Replace("\t", " ");

        return limpio.Length > maximo ? limpio[..maximo] + "…" : limpio;
    }

    /// <summary>
    /// El texto de los manejadores de los controles que todavía no aparecen en
    /// el .asm del usuario, para ofrecérselo a que lo pegue.
    ///
    /// ⚠ NO REESCRIBE EL ARCHIVO: se le pasa el texto actual del .asm y
    /// devuelve solo lo que falta. Si devuelve vacío, no hay nada que agregar.
    /// La detección es por la etiqueta del manejador, que es única por control.
    /// </summary>
    public static string GenerarManejadoresFaltantes(FormularioDisenado f, string asmActual)
    {
        var faltantes = f.Controles
            .Where(c => InfoTipoControl.NotificaPorComando(c.Tipo))
            .Where(c => !asmActual.Contains(EtiquetaManejador(c) + ":", StringComparison.Ordinal))
            .ToList();

        if (faltantes.Count == 0) return "";

        var sb = new StringBuilder();

        sb.AppendLine("; ---- Controles nuevos del diseñador ----");
        sb.AppendLine($"; Pegá estas comparaciones dentro de .comando de {f.SimboloProc}:");
        sb.AppendLine(";");

        foreach (var c in faltantes)
        {
            sb.AppendLine($";     cmp eax, {c.SimboloId}");
            sb.AppendLine($";     je {EtiquetaManejador(c)}");
        }

        sb.AppendLine(";");
        sb.AppendLine("; Y estos manejadores antes de la etiqueta .salir:");
        sb.AppendLine();

        foreach (var c in faltantes)
        {
            if (f.EsX64)
            {
                EscribirManejadorX64(sb, c);
            }
            else
            {
                sb.AppendLine($"{EtiquetaManejador(c)}:");
                sb.AppendLine($"    ; ---- {InfoTipoControl.Nombre(c.Tipo)} '{TextoParaComentario(c.Texto)}' ({c.Nombre}) ----");
                sb.AppendLine("    xor eax, eax");
                sb.AppendLine("    jmp .salir");
            }

            sb.AppendLine();
        }

        return sb.ToString();
    }
}
