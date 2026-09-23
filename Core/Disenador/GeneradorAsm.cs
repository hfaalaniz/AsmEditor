using System.Text;

namespace AsmEditor.Core.Disenador;

/// <summary>
/// Convierte un <see cref="FormularioDisenado"/> en el .inc que el usuario
/// incluye desde su código.
///
/// ⚠ ESTE ARCHIVO SE REGENERA ENTERO Y EL USUARIO NO LO EDITA. Lleva un aviso
/// arriba que lo dice. El código propio va en el .asm, que se genera una sola
/// vez (ver <see cref="GeneradorEsqueleto"/>) y nunca se pisa.
///
/// La estructura emitida es la que se verificó contra nasm.exe y GoLink.exe
/// reales el 2026-09-14: tabla de datos de 48 bytes por control y un runtime
/// que la recorre. El diagnóstico está en diagnostico/rad/objetivo_win64.asm.
///
/// Es una función pura: entra un formulario, sale texto. No toca el disco.
/// </summary>
public static class GeneradorAsm
{
    /// <summary>Bytes que ocupa una fila de la tabla de controles en x64.</summary>
    public const int TamanoEntradaX64 = 48;

    /// <summary>
    /// Bytes por fila en x86. Los punteros son de 4 bytes en vez de 8, así que
    /// la fila es más corta: no es el mismo layout con otro -f.
    /// </summary>
    public const int TamanoEntradaX86 = 36;

    public static int TamanoEntrada(FormularioDisenado f) =>
        f.EsX64 ? TamanoEntradaX64 : TamanoEntradaX86;

    /// <summary>
    /// El .inc completo. Incluye las constantes, la tabla, el runtime que la
    /// recorre y —si es ventana principal— el registro de clase y el bucle
    /// de mensajes.
    /// </summary>
    public static string GenerarInclude(FormularioDisenado f)
    {
        var sb = new StringBuilder();

        EscribirEncabezado(sb, f);
        EscribirConstantes(sb, f);
        EscribirIdsDeControles(sb, f);
        EscribirDatos(sb, f);
        EscribirTabla(sb, f);
        EscribirBss(sb, f);
        EscribirRuntime(sb, f);

        return sb.ToString();
    }

    // ------------------------------------------------------------------

    private static void EscribirEncabezado(StringBuilder sb, FormularioDisenado f)
    {
        sb.AppendLine("; ==========================================================================");
        sb.AppendLine($"; {f.Nombre}.inc — GENERADO POR EL DISEÑADOR DE FORMULARIOS");
        sb.AppendLine(";");
        sb.AppendLine("; ⚠ NO EDITAR A MANO: este archivo se reescribe entero cada vez que se");
        sb.AppendLine(";   guarda el formulario en el diseñador, y cualquier cambio se pierde.");
        sb.AppendLine($";   Tu código va en {f.Nombre}.asm, que el diseñador no toca.");
        sb.AppendLine(";");
        sb.AppendLine($"; Formulario : {f.Titulo}");
        sb.AppendLine($"; Tipo       : {(f.EsVentanaPrincipal ? "ventana principal" : "diálogo")}");
        sb.AppendLine($"; Arquitectura: {(f.EsX64 ? "x64" : "x86")}");
        sb.AppendLine($"; Controles  : {f.Controles.Count}");
        sb.AppendLine("; ==========================================================================");
        sb.AppendLine();
    }

    private static void EscribirConstantes(StringBuilder sb, FormularioDisenado f)
    {
        sb.AppendLine("; ---- Constantes de winuser.h que usa el código generado ----");
        sb.AppendLine("; Van con %ifndef porque si se incluyen dos formularios en el mismo");
        sb.AppendLine("; programa, NASM avisaría de macro redefinida en el segundo.");
        sb.AppendLine("%ifndef RAD_CONSTANTES_DEFINIDAS");
        sb.AppendLine("%define RAD_CONSTANTES_DEFINIDAS");
        sb.AppendLine();
        sb.AppendLine("%define WM_CREATE            0x0001");
        sb.AppendLine("%define WM_DESTROY           0x0002");
        sb.AppendLine("%define WM_CLOSE             0x0010");
        sb.AppendLine("%define WM_COMMAND           0x0111");
        sb.AppendLine("%define WM_SETFONT           0x0030");
        sb.AppendLine();
        sb.AppendLine("%define WS_OVERLAPPED        0x00000000");
        sb.AppendLine("%define WS_CHILD             0x40000000");
        sb.AppendLine("%define WS_VISIBLE           0x10000000");
        sb.AppendLine("%define WS_DISABLED          0x08000000");
        sb.AppendLine("%define WS_BORDER            0x00800000");
        sb.AppendLine("%define WS_TABSTOP           0x00010000");
        sb.AppendLine("%define WS_GROUP             0x00020000");
        sb.AppendLine("%define WS_VSCROLL           0x00200000");
        sb.AppendLine("%define WS_HSCROLL           0x00100000");
        sb.AppendLine("%define WS_SYSMENU           0x00080000");
        sb.AppendLine("%define WS_CAPTION           0x00C00000");
        sb.AppendLine("%define WS_THICKFRAME        0x00040000");
        sb.AppendLine("%define WS_MINIMIZEBOX       0x00020000");
        sb.AppendLine("%define WS_MAXIMIZEBOX       0x00010000");
        sb.AppendLine("%define WS_OVERLAPPEDWINDOW  0x00CF0000");
        sb.AppendLine();
        sb.AppendLine("%define BS_PUSHBUTTON        0x0000");
        sb.AppendLine("%define BS_DEFPUSHBUTTON     0x0001");
        sb.AppendLine("%define BS_AUTOCHECKBOX      0x0003");
        sb.AppendLine("%define BS_GROUPBOX          0x0007");
        sb.AppendLine("%define BS_AUTORADIOBUTTON   0x0009");
        sb.AppendLine("%define BS_LEFT              0x0100");
        sb.AppendLine("%define BS_RIGHT             0x0200");
        sb.AppendLine("%define BS_CENTER            0x0300");
        sb.AppendLine();
        sb.AppendLine("%define ES_LEFT              0x0000");
        sb.AppendLine("%define ES_CENTER            0x0001");
        sb.AppendLine("%define ES_RIGHT             0x0002");
        sb.AppendLine("%define ES_MULTILINE         0x0004");
        sb.AppendLine("%define ES_PASSWORD          0x0020");
        sb.AppendLine("%define ES_AUTOVSCROLL       0x0040");
        sb.AppendLine("%define ES_AUTOHSCROLL       0x0080");
        sb.AppendLine("%define ES_READONLY          0x0800");
        sb.AppendLine();
        sb.AppendLine("%define SS_LEFT              0x0000");
        sb.AppendLine("%define SS_CENTER            0x0001");
        sb.AppendLine("%define SS_RIGHT             0x0002");
        sb.AppendLine();
        sb.AppendLine("%define CBS_SIMPLE           0x0001");
        sb.AppendLine("%define CBS_DROPDOWN         0x0002");
        sb.AppendLine("%define CBS_DROPDOWNLIST     0x0003");
        sb.AppendLine();
        sb.AppendLine("%define LBS_NOTIFY           0x0001");
        sb.AppendLine("%define LBS_SORT             0x0002");
        sb.AppendLine();
        sb.AppendLine("; Mensajes útiles para hablarle a los controles");
        sb.AppendLine("%define BM_GETCHECK          0x00F0");
        sb.AppendLine("%define BM_SETCHECK          0x00F1");
        sb.AppendLine("%define LB_ADDSTRING         0x0180");
        sb.AppendLine("%define LB_GETCURSEL         0x0188");
        sb.AppendLine("%define LB_RESETCONTENT      0x0184");
        sb.AppendLine("%define CB_ADDSTRING         0x0143");
        sb.AppendLine("%define CB_GETCURSEL         0x0147");
        sb.AppendLine("%define CB_SETCURSEL         0x014E");
        sb.AppendLine();
        sb.AppendLine("%define CW_USEDEFAULT        0x80000000");
        sb.AppendLine("%define SW_HIDE              0");
        sb.AppendLine("%define SW_SHOWNORMAL        1");
        sb.AppendLine("%define SW_SHOW              5");
        sb.AppendLine("%define IDI_APPLICATION      32512");
        sb.AppendLine("%define IDC_ARROW            32512");
        sb.AppendLine("%define WHITE_BRUSH          0");
        sb.AppendLine("%define COLOR_BTNFACE        15");
        sb.AppendLine();
        sb.AppendLine("%endif ; RAD_CONSTANTES_DEFINIDAS");
        sb.AppendLine();
    }

    private static void EscribirIdsDeControles(StringBuilder sb, FormularioDisenado f)
    {
        sb.AppendLine("; ---- Identificadores de los controles ----");
        sb.AppendLine("; Estos son los símbolos que usás en tu código para hablarle a cada");
        sb.AppendLine("; control, por ejemplo con GetDlgItem o SendDlgItemMessageA.");
        sb.AppendLine();

        if (f.Controles.Count == 0)
        {
            sb.AppendLine("; (el formulario todavía no tiene controles)");
            sb.AppendLine();
            return;
        }

        var ancho = f.Controles.Max(c => c.SimboloId.Length);

        foreach (var c in f.Controles)
        {
            var etiqueta = InfoTipoControl.Nombre(c.Tipo);
            sb.AppendLine($"%define {c.SimboloId.PadRight(ancho)}  {c.Id}    ; {etiqueta}");
        }

        sb.AppendLine();
    }

    private static void EscribirDatos(StringBuilder sb, FormularioDisenado f)
    {
        sb.AppendLine("section .data");
        sb.AppendLine();
        sb.AppendLine($"    {SimboloTitulo(f)} db {CadenaAsm(f.Titulo)}, 0");

        // ⚠ EL NOMBRE DE CLASE SE EMITE SIEMPRE, TAMBIÉN EN EL DIÁLOGO. El
        // diálogo no REGISTRA la clase, pero igual se la nombra a
        // CreateWindowExA, así que el literal tiene que existir. Emitirlo solo
        // en la ventana principal dejaba el .inc del diálogo sin el símbolo y
        // NASM lo rechazaba con "symbol not defined".
        sb.AppendLine($"    {SimboloClase(f)} db {CadenaAsm(f.NombreClaseVentana)}, 0");

        sb.AppendLine();

        // Las clases Win32 se emiten UNA VEZ cada una, no una por control:
        // seis botones comparten el mismo literal "BUTTON".
        var clases = f.Controles
            .Select(c => c.ClaseEfectiva)
            .Where(c => !string.IsNullOrWhiteSpace(c))
            .Distinct(StringComparer.Ordinal)
            .OrderBy(c => c, StringComparer.Ordinal)
            .ToList();

        if (clases.Count > 0)
        {
            sb.AppendLine("    ; Nombres de clase de los controles");
            foreach (var clase in clases)
            {
                sb.AppendLine($"    {SimboloClaseControl(clase)} db {CadenaAsm(clase)}, 0");
            }
            sb.AppendLine();
        }

        var conTexto = f.Controles.Where(c => !string.IsNullOrEmpty(c.Texto)).ToList();

        if (conTexto.Count > 0)
        {
            sb.AppendLine("    ; Textos de los controles");
            foreach (var c in conTexto)
            {
                sb.AppendLine($"    {SimboloTexto(c)} db {CadenaAsm(c.Texto)}, 0");
            }
            sb.AppendLine();
        }
    }

    private static void EscribirTabla(StringBuilder sb, FormularioDisenado f)
    {
        var tam = TamanoEntrada(f);
        var puntero = f.EsX64 ? "dq" : "dd";

        sb.AppendLine("; ---- Tabla de controles ----");
        sb.AppendLine($"; Una fila por control, {tam} bytes cada una. La recorre {f.SimboloCrearControles}.");
        sb.AppendLine("; Layout de cada fila:");

        if (f.EsX64)
        {
            sb.AppendLine(";   +0  clase(8)  +8  texto(8)  +16 estilo(4)  +20 id(4)");
            sb.AppendLine(";   +24 x(4)  +28 y(4)  +32 ancho(4)  +36 alto(4)  +40 exestilo(4)  +44 relleno(4)");
        }
        else
        {
            sb.AppendLine(";   +0  clase(4)  +4  texto(4)  +8  estilo(4)  +12 id(4)");
            sb.AppendLine(";   +16 x(4)  +20 y(4)  +24 ancho(4)  +28 alto(4)  +32 exestilo(4)");
        }

        sb.AppendLine();
        sb.AppendLine("    align 8");
        sb.AppendLine($"{f.SimboloTabla}:");

        foreach (var c in f.Controles)
        {
            var textoPtr = string.IsNullOrEmpty(c.Texto) ? "0" : SimboloTexto(c);
            var clasePtr = SimboloClaseControl(c.ClaseEfectiva);

            sb.AppendLine();
            sb.AppendLine($"    ; {c.Nombre} — {InfoTipoControl.Nombre(c.Tipo)}");
            sb.AppendLine($"    {puntero} {clasePtr}, {textoPtr}");
            sb.AppendLine($"    dd {c.ExpresionEstilo()}, {c.SimboloId}");
            sb.AppendLine($"    dd {c.X}, {c.Y}, {c.Ancho}, {c.Alto}");

            // El relleno de x64 lleva la fila a 48 bytes, múltiplo de 8, para
            // que los punteros de la fila siguiente queden alineados.
            sb.AppendLine(f.EsX64 ? "    dd 0, 0" : "    dd 0");
        }

        sb.AppendLine();
        sb.AppendLine($"{f.SimboloTabla}Fin:");
        sb.AppendLine();
        sb.AppendLine($"%define {TamEntradaSimbolo(f)} {tam}");
        sb.AppendLine($"%define {CantSimbolo(f)} (({f.SimboloTabla}Fin - {f.SimboloTabla}) / {TamEntradaSimbolo(f)})");
        sb.AppendLine();
    }

    private static void EscribirBss(StringBuilder sb, FormularioDisenado f)
    {
        sb.AppendLine("section .bss");
        sb.AppendLine();

        var palabra = f.EsX64 ? "resq" : "resd";

        sb.AppendLine($"    {f.SimboloHwnd} {palabra} 1        ; hWnd de esta ventana");

        // ⚠ hInstance SE DECLARA SIEMPRE: CrearControles lo lee para cada
        // CreateWindowExA, y esa rutina existe en los dos tipos de formulario.
        // Declararlo solo en la ventana principal dejaba al diálogo sin él.
        sb.AppendLine($"    {SimboloHinst(f)} {palabra} 1        ; hInstance del proceso");

        if (f.EsVentanaPrincipal)
        {
            sb.AppendLine($"    {SimboloWc(f)} resb {(f.EsX64 ? 80 : 48)}      ; WNDCLASSEXA");
            sb.AppendLine($"    {SimboloMsg(f)} resb {(f.EsX64 ? 48 : 28)}      ; MSG");
        }

        sb.AppendLine();
    }

    private static void EscribirRuntime(StringBuilder sb, FormularioDisenado f)
    {
        sb.AppendLine("section .text");
        sb.AppendLine();

        if (f.EsX64) EscribirCrearControlesX64(sb, f);
        else EscribirCrearControlesX86(sb, f);

        if (f.EsVentanaPrincipal)
        {
            if (f.EsX64) EscribirCrearVentanaX64(sb, f);
            else EscribirCrearVentanaX86(sb, f);
        }
        else
        {
            if (f.EsX64) EscribirCrearDialogoX64(sb, f);
            else EscribirCrearDialogoX86(sb, f);
        }
    }

    // ------------------------------------------------------------------
    // Runtime x64
    //
    // ⚠ LA ALINEACIÓN DE LA PILA ES OBLIGATORIA, NO UNA OPTIMIZACIÓN. La ABI
    // de Windows x64 exige RSP múltiplo de 16 al entrar a una API; si no, las
    // que usan SSE alineado (muchas de user32) fallan con violación de acceso.
    // Al entrar a una rutina RSP%16 == 8 por la dirección de retorno que dejó
    // el call: cada sub rsp de abajo está calculado para dejarlo en 0.
    // ------------------------------------------------------------------

    private static void EscribirCrearControlesX64(StringBuilder sb, FormularioDisenado f)
    {
        sb.AppendLine("; ==========================================================================");
        sb.AppendLine($"; {f.SimboloCrearControles} — crea todos los controles del formulario");
        sb.AppendLine(";   RCX = hWnd de la ventana padre");
        sb.AppendLine(";");
        sb.AppendLine("; Recorre la tabla y llama CreateWindowExA una vez por fila. Agregar un");
        sb.AppendLine("; control es agregar una fila a la tabla: este código no cambia.");
        sb.AppendLine("; ==========================================================================");
        sb.AppendLine($"{f.SimboloCrearControles}:");
        sb.AppendLine("    push rbx");
        sb.AppendLine("    push rsi");
        sb.AppendLine("    push r12");
        sb.AppendLine("    sub rsp, 32                    ; 3 push (24) + 32 + 8 del call = 64, alineado");
        sb.AppendLine();
        sb.AppendLine("    mov rbx, rcx                   ; rbx = hWnd padre");
        sb.AppendLine($"    lea rsi, [{f.SimboloTabla}]    ; rsi = fila actual");
        sb.AppendLine($"    mov r12d, {CantSimbolo(f)}     ; r12 = filas que faltan");
        sb.AppendLine();
        sb.AppendLine(".siguiente:");
        sb.AppendLine("    test r12d, r12d");
        sb.AppendLine("    jz .fin");
        sb.AppendLine();
        sb.AppendLine("    ; CreateWindowExA(exStyle, clase, texto, estilo, x, y, ancho, alto,");
        sb.AppendLine("    ;                 padre, id, hInstance, 0)");
        sb.AppendLine("    ; 12 argumentos: 4 en registros y 8 en la pila tras el shadow space.");
        sb.AppendLine("    sub rsp, 96                    ; 32 de shadow + 8*8 de argumentos");
        sb.AppendLine();
        sb.AppendLine("    mov eax, [rsi+24]              ; x");
        sb.AppendLine("    mov [rsp+32], eax");
        sb.AppendLine("    mov eax, [rsi+28]              ; y");
        sb.AppendLine("    mov [rsp+40], eax");
        sb.AppendLine("    mov eax, [rsi+32]              ; ancho");
        sb.AppendLine("    mov [rsp+48], eax");
        sb.AppendLine("    mov eax, [rsi+36]              ; alto");
        sb.AppendLine("    mov [rsp+56], eax");
        sb.AppendLine("    mov [rsp+64], rbx              ; hWndParent");
        sb.AppendLine("    mov eax, [rsi+20]              ; el id del control va como hMenu");
        sb.AppendLine("    mov [rsp+72], rax");
        sb.AppendLine($"    mov rax, [{SimboloHinst(f)}]");
        sb.AppendLine("    mov [rsp+80], rax              ; hInstance");
        sb.AppendLine("    mov qword [rsp+88], 0          ; lpParam");
        sb.AppendLine();
        sb.AppendLine("    mov r9d, [rsi+16]              ; dwStyle");
        sb.AppendLine("    mov r8,  [rsi+8]               ; texto");
        sb.AppendLine("    mov rdx, [rsi+0]               ; clase");
        sb.AppendLine("    mov ecx, [rsi+40]              ; dwExStyle");
        sb.AppendLine("    call CreateWindowExA");
        sb.AppendLine();
        sb.AppendLine("    add rsp, 96");
        sb.AppendLine();
        sb.AppendLine($"    add rsi, {TamEntradaSimbolo(f)}");
        sb.AppendLine("    dec r12d");
        sb.AppendLine("    jmp .siguiente");
        sb.AppendLine();
        sb.AppendLine(".fin:");
        sb.AppendLine("    add rsp, 32");
        sb.AppendLine("    pop r12");
        sb.AppendLine("    pop rsi");
        sb.AppendLine("    pop rbx");
        sb.AppendLine("    ret");
        sb.AppendLine();
    }

    private static void EscribirCrearVentanaX64(StringBuilder sb, FormularioDisenado f)
    {
        sb.AppendLine("; ==========================================================================");
        sb.AppendLine($"; {SimboloCrearVentana(f)} — registra la clase y crea la ventana principal");
        sb.AppendLine(";   Devuelve el hWnd en RAX, o 0 si falló.");
        sb.AppendLine(";");
        sb.AppendLine($"; El procedimiento de ventana ({f.SimboloProc}) lo escribís vos en el .asm.");
        sb.AppendLine("; ==========================================================================");
        sb.AppendLine($"{SimboloCrearVentana(f)}:");
        sb.AppendLine("    sub rsp, 40                    ; 40 + 8 del call = 48, alineado");
        sb.AppendLine();
        sb.AppendLine("    xor ecx, ecx");
        sb.AppendLine("    call GetModuleHandleA");
        sb.AppendLine($"    mov [{SimboloHinst(f)}], rax");
        sb.AppendLine();
        sb.AppendLine("    ; ---- WNDCLASSEXA (80 bytes en x64) ----");
        sb.AppendLine($"    mov dword [{SimboloWc(f)}+0], 80        ; cbSize");
        sb.AppendLine($"    mov dword [{SimboloWc(f)}+4], 3         ; CS_HREDRAW|CS_VREDRAW");
        sb.AppendLine($"    lea rax, [{f.SimboloProc}]");
        sb.AppendLine($"    mov [{SimboloWc(f)}+8], rax             ; lpfnWndProc");
        sb.AppendLine($"    mov dword [{SimboloWc(f)}+16], 0        ; cbClsExtra");
        sb.AppendLine($"    mov dword [{SimboloWc(f)}+20], 0        ; cbWndExtra");
        sb.AppendLine($"    mov rax, [{SimboloHinst(f)}]");
        sb.AppendLine($"    mov [{SimboloWc(f)}+24], rax            ; hInstance");
        sb.AppendLine();
        sb.AppendLine("    xor ecx, ecx");
        sb.AppendLine("    mov edx, IDI_APPLICATION");
        sb.AppendLine("    call LoadIconA");
        sb.AppendLine($"    mov [{SimboloWc(f)}+32], rax            ; hIcon");
        sb.AppendLine();
        sb.AppendLine("    xor ecx, ecx");
        sb.AppendLine("    mov edx, IDC_ARROW");
        sb.AppendLine("    call LoadCursorA");
        sb.AppendLine($"    mov [{SimboloWc(f)}+40], rax            ; hCursor");
        sb.AppendLine();
        sb.AppendLine("    mov ecx, WHITE_BRUSH");
        sb.AppendLine("    call GetStockObject");
        sb.AppendLine($"    mov [{SimboloWc(f)}+48], rax            ; hbrBackground");
        sb.AppendLine();
        sb.AppendLine($"    mov qword [{SimboloWc(f)}+56], 0        ; lpszMenuName");
        sb.AppendLine($"    lea rax, [{SimboloClase(f)}]");
        sb.AppendLine($"    mov [{SimboloWc(f)}+64], rax            ; lpszClassName");
        sb.AppendLine($"    mov qword [{SimboloWc(f)}+72], 0        ; hIconSm");
        sb.AppendLine();
        sb.AppendLine($"    lea rcx, [{SimboloWc(f)}]");
        sb.AppendLine("    call RegisterClassExA");
        sb.AppendLine();
        sb.AppendLine("    ; ---- CreateWindowExA de la ventana ----");
        sb.AppendLine("    sub rsp, 96");
        sb.AppendLine("    mov dword [rsp+32], CW_USEDEFAULT       ; x");
        sb.AppendLine("    mov dword [rsp+40], CW_USEDEFAULT       ; y");
        sb.AppendLine($"    mov dword [rsp+48], {AnchoConBordes(f)}            ; ancho con bordes");
        sb.AppendLine($"    mov dword [rsp+56], {AltoConBordes(f)}            ; alto con bordes y título");
        sb.AppendLine("    mov qword [rsp+64], 0                   ; hWndParent");
        sb.AppendLine("    mov qword [rsp+72], 0                   ; hMenu");
        sb.AppendLine($"    mov rax, [{SimboloHinst(f)}]");
        sb.AppendLine("    mov [rsp+80], rax                       ; hInstance");
        sb.AppendLine("    mov qword [rsp+88], 0                   ; lpParam");
        sb.AppendLine();
        sb.AppendLine($"    mov r9d, {f.ExpresionEstiloVentana}");
        sb.AppendLine($"    lea r8, [{SimboloTitulo(f)}]");
        sb.AppendLine($"    lea rdx, [{SimboloClase(f)}]");
        sb.AppendLine("    xor ecx, ecx                            ; dwExStyle");
        sb.AppendLine("    call CreateWindowExA");
        sb.AppendLine("    add rsp, 96");
        sb.AppendLine();
        sb.AppendLine($"    mov [{f.SimboloHwnd}], rax");
        sb.AppendLine();
        sb.AppendLine("    add rsp, 40");
        sb.AppendLine("    ret");
        sb.AppendLine();

        EscribirBucleMensajesX64(sb, f);
    }

    private static void EscribirBucleMensajesX64(StringBuilder sb, FormularioDisenado f)
    {
        sb.AppendLine("; ==========================================================================");
        sb.AppendLine($"; {SimboloMostrar(f)} — muestra la ventana y corre el bucle de mensajes");
        sb.AppendLine(";   No vuelve hasta que se cierra la ventana.");
        sb.AppendLine("; ==========================================================================");
        sb.AppendLine($"{SimboloMostrar(f)}:");
        sb.AppendLine("    sub rsp, 40");
        sb.AppendLine();
        sb.AppendLine($"    mov rcx, [{f.SimboloHwnd}]");
        sb.AppendLine("    mov edx, SW_SHOWNORMAL");
        sb.AppendLine("    call ShowWindow");
        sb.AppendLine();
        sb.AppendLine($"    mov rcx, [{f.SimboloHwnd}]");
        sb.AppendLine("    call UpdateWindow");
        sb.AppendLine();
        sb.AppendLine(".bucle:");
        sb.AppendLine($"    lea rcx, [{SimboloMsg(f)}]");
        sb.AppendLine("    xor edx, edx");
        sb.AppendLine("    xor r8d, r8d");
        sb.AppendLine("    xor r9d, r9d");
        sb.AppendLine("    call GetMessageA");
        sb.AppendLine("    test eax, eax");
        sb.AppendLine("    jz .fin                        ; WM_QUIT devuelve 0");
        sb.AppendLine();
        sb.AppendLine($"    lea rcx, [{SimboloMsg(f)}]");
        sb.AppendLine("    call TranslateMessage");
        sb.AppendLine($"    lea rcx, [{SimboloMsg(f)}]");
        sb.AppendLine("    call DispatchMessageA");
        sb.AppendLine("    jmp .bucle");
        sb.AppendLine();
        sb.AppendLine(".fin:");
        sb.AppendLine("    add rsp, 40");
        sb.AppendLine("    ret");
        sb.AppendLine();
    }

    private static void EscribirCrearDialogoX64(StringBuilder sb, FormularioDisenado f)
    {
        sb.AppendLine("; ==========================================================================");
        sb.AppendLine($"; {SimboloCrearVentana(f)} — crea el diálogo como ventana hija");
        sb.AppendLine(";   RCX = hWnd del padre, RDX = hInstance");
        sb.AppendLine(";   Devuelve el hWnd del diálogo en RAX.");
        sb.AppendLine(";");
        sb.AppendLine("; ⚠ USA UNA CLASE YA REGISTRADA: el diálogo no registra la suya. Por");
        sb.AppendLine($";   defecto reusa la del formulario padre, así que su {f.SimboloProc}");
        sb.AppendLine(";   recibe los mensajes. Si querés un procedimiento propio, registrá una");
        sb.AppendLine(";   clase aparte y cambiá el lea rdx de abajo.");
        sb.AppendLine("; ==========================================================================");
        sb.AppendLine($"{SimboloCrearVentana(f)}:");
        sb.AppendLine("    push rbx");
        sb.AppendLine("    push rsi");
        sb.AppendLine("    sub rsp, 40                    ; 2 push (16) + 40 + 8 = 64, alineado");
        sb.AppendLine();
        sb.AppendLine("    mov rbx, rcx                   ; padre");
        sb.AppendLine("    mov rsi, rdx                   ; hInstance");
        sb.AppendLine();
        sb.AppendLine("    ; CrearControles lee el hInstance de la variable, no de un registro:");
        sb.AppendLine("    ; hay que dejárselo ahí antes de llamarla.");
        sb.AppendLine($"    mov [{SimboloHinst(f)}], rsi");
        sb.AppendLine();
        sb.AppendLine("    sub rsp, 96");
        sb.AppendLine("    mov dword [rsp+32], CW_USEDEFAULT");
        sb.AppendLine("    mov dword [rsp+40], CW_USEDEFAULT");
        sb.AppendLine($"    mov dword [rsp+48], {AnchoConBordes(f)}");
        sb.AppendLine($"    mov dword [rsp+56], {AltoConBordes(f)}");
        sb.AppendLine("    mov [rsp+64], rbx                       ; hWndParent");
        sb.AppendLine("    mov qword [rsp+72], 0                   ; hMenu");
        sb.AppendLine("    mov [rsp+80], rsi                       ; hInstance");
        sb.AppendLine("    mov qword [rsp+88], 0                   ; lpParam");
        sb.AppendLine();
        sb.AppendLine($"    mov r9d, {f.ExpresionEstiloVentana}");
        sb.AppendLine($"    lea r8, [{SimboloTitulo(f)}]");
        sb.AppendLine($"    lea rdx, [{SimboloClase(f)}]            ; clase del padre — ver el aviso");
        sb.AppendLine("    xor ecx, ecx");
        sb.AppendLine("    call CreateWindowExA");
        sb.AppendLine("    add rsp, 96");
        sb.AppendLine();
        sb.AppendLine($"    mov [{f.SimboloHwnd}], rax");
        sb.AppendLine();
        sb.AppendLine("    ; Los controles se crean acá y no en WM_CREATE: el diálogo comparte");
        sb.AppendLine("    ; el procedimiento del padre, que ya atiende su propio WM_CREATE.");
        sb.AppendLine("    mov rcx, rax");
        sb.AppendLine($"    call {f.SimboloCrearControles}");
        sb.AppendLine();
        sb.AppendLine($"    mov rax, [{f.SimboloHwnd}]");
        sb.AppendLine();
        sb.AppendLine("    add rsp, 40");
        sb.AppendLine("    pop rsi");
        sb.AppendLine("    pop rbx");
        sb.AppendLine("    ret");
        sb.AppendLine();
    }

    // ------------------------------------------------------------------
    // Runtime x86
    //
    // ⚠ ES OTRA CONVENCIÓN, NO EL MISMO CÓDIGO MÁS CORTO. stdcall: los
    // argumentos se apilan de DERECHA A IZQUIERDA y los saca la función
    // llamada, así que después del call NO hay que ajustar la pila.
    // ------------------------------------------------------------------

    private static void EscribirCrearControlesX86(StringBuilder sb, FormularioDisenado f)
    {
        sb.AppendLine("; ==========================================================================");
        sb.AppendLine($"; {f.SimboloCrearControles} — crea todos los controles (x86, stdcall)");
        sb.AppendLine(";   Argumento en la pila: hWnd del padre");
        sb.AppendLine("; ==========================================================================");
        sb.AppendLine($"{f.SimboloCrearControles}:");
        sb.AppendLine("    push ebp");
        sb.AppendLine("    mov ebp, esp");
        sb.AppendLine("    push ebx");
        sb.AppendLine("    push esi");
        sb.AppendLine("    push edi");
        sb.AppendLine();
        sb.AppendLine("    mov ebx, [ebp+8]               ; hWnd padre");
        sb.AppendLine($"    mov esi, {f.SimboloTabla}      ; fila actual");
        sb.AppendLine($"    mov edi, {CantSimbolo(f)}      ; filas que faltan");
        sb.AppendLine();
        sb.AppendLine(".siguiente:");
        sb.AppendLine("    test edi, edi");
        sb.AppendLine("    jz .fin");
        sb.AppendLine();
        sb.AppendLine("    ; stdcall: se apila al revés, del último argumento al primero.");
        sb.AppendLine("    push dword 0                   ; lpParam");
        sb.AppendLine($"    push dword [{SimboloHinst(f)}] ; hInstance");
        sb.AppendLine("    push dword [esi+12]            ; id -> hMenu");
        sb.AppendLine("    push ebx                       ; hWndParent");
        sb.AppendLine("    push dword [esi+28]            ; alto");
        sb.AppendLine("    push dword [esi+24]            ; ancho");
        sb.AppendLine("    push dword [esi+20]            ; y");
        sb.AppendLine("    push dword [esi+16]            ; x");
        sb.AppendLine("    push dword [esi+8]             ; dwStyle");
        sb.AppendLine("    push dword [esi+4]             ; texto");
        sb.AppendLine("    push dword [esi+0]             ; clase");
        sb.AppendLine("    push dword [esi+32]            ; dwExStyle");
        sb.AppendLine("    call CreateWindowExA");
        sb.AppendLine("    ; sin add esp: stdcall ya limpió la pila");
        sb.AppendLine();
        sb.AppendLine($"    add esi, {TamEntradaSimbolo(f)}");
        sb.AppendLine("    dec edi");
        sb.AppendLine("    jmp .siguiente");
        sb.AppendLine();
        sb.AppendLine(".fin:");
        sb.AppendLine("    pop edi");
        sb.AppendLine("    pop esi");
        sb.AppendLine("    pop ebx");
        sb.AppendLine("    mov esp, ebp");
        sb.AppendLine("    pop ebp");
        sb.AppendLine("    ret 4                          ; stdcall: saca su argumento");
        sb.AppendLine();
    }

    private static void EscribirCrearVentanaX86(StringBuilder sb, FormularioDisenado f)
    {
        sb.AppendLine("; ==========================================================================");
        sb.AppendLine($"; {SimboloCrearVentana(f)} — registra la clase y crea la ventana (x86)");
        sb.AppendLine(";   Devuelve el hWnd en EAX.");
        sb.AppendLine("; ==========================================================================");
        sb.AppendLine($"{SimboloCrearVentana(f)}:");
        sb.AppendLine("    push ebp");
        sb.AppendLine("    mov ebp, esp");
        sb.AppendLine();
        sb.AppendLine("    push dword 0");
        sb.AppendLine("    call GetModuleHandleA");
        sb.AppendLine($"    mov [{SimboloHinst(f)}], eax");
        sb.AppendLine();
        sb.AppendLine("    ; ---- WNDCLASSEXA (48 bytes en x86) ----");
        sb.AppendLine($"    mov dword [{SimboloWc(f)}+0], 48        ; cbSize");
        sb.AppendLine($"    mov dword [{SimboloWc(f)}+4], 3         ; CS_HREDRAW|CS_VREDRAW");
        sb.AppendLine($"    mov dword [{SimboloWc(f)}+8], {f.SimboloProc}");
        sb.AppendLine($"    mov dword [{SimboloWc(f)}+12], 0        ; cbClsExtra");
        sb.AppendLine($"    mov dword [{SimboloWc(f)}+16], 0        ; cbWndExtra");
        sb.AppendLine($"    mov eax, [{SimboloHinst(f)}]");
        sb.AppendLine($"    mov [{SimboloWc(f)}+20], eax            ; hInstance");
        sb.AppendLine();
        sb.AppendLine("    push dword IDI_APPLICATION");
        sb.AppendLine("    push dword 0");
        sb.AppendLine("    call LoadIconA");
        sb.AppendLine($"    mov [{SimboloWc(f)}+24], eax            ; hIcon");
        sb.AppendLine();
        sb.AppendLine("    push dword IDC_ARROW");
        sb.AppendLine("    push dword 0");
        sb.AppendLine("    call LoadCursorA");
        sb.AppendLine($"    mov [{SimboloWc(f)}+28], eax            ; hCursor");
        sb.AppendLine();
        sb.AppendLine("    push dword WHITE_BRUSH");
        sb.AppendLine("    call GetStockObject");
        sb.AppendLine($"    mov [{SimboloWc(f)}+32], eax            ; hbrBackground");
        sb.AppendLine();
        sb.AppendLine($"    mov dword [{SimboloWc(f)}+36], 0        ; lpszMenuName");
        sb.AppendLine($"    mov dword [{SimboloWc(f)}+40], {SimboloClase(f)}");
        sb.AppendLine($"    mov dword [{SimboloWc(f)}+44], 0        ; hIconSm");
        sb.AppendLine();
        sb.AppendLine($"    push dword {SimboloWc(f)}");
        sb.AppendLine("    call RegisterClassExA");
        sb.AppendLine();
        sb.AppendLine("    ; ---- CreateWindowExA ----");
        sb.AppendLine("    push dword 0                            ; lpParam");
        sb.AppendLine($"    push dword [{SimboloHinst(f)}]          ; hInstance");
        sb.AppendLine("    push dword 0                            ; hMenu");
        sb.AppendLine("    push dword 0                            ; hWndParent");
        sb.AppendLine($"    push dword {AltoConBordes(f)}                        ; alto");
        sb.AppendLine($"    push dword {AnchoConBordes(f)}                        ; ancho");
        sb.AppendLine("    push dword CW_USEDEFAULT                ; y");
        sb.AppendLine("    push dword CW_USEDEFAULT                ; x");
        sb.AppendLine($"    push dword {f.ExpresionEstiloVentana}");
        sb.AppendLine($"    push dword {SimboloTitulo(f)}");
        sb.AppendLine($"    push dword {SimboloClase(f)}");
        sb.AppendLine("    push dword 0                            ; dwExStyle");
        sb.AppendLine("    call CreateWindowExA");
        sb.AppendLine();
        sb.AppendLine($"    mov [{f.SimboloHwnd}], eax");
        sb.AppendLine();
        sb.AppendLine("    mov esp, ebp");
        sb.AppendLine("    pop ebp");
        sb.AppendLine("    ret");
        sb.AppendLine();

        EscribirBucleMensajesX86(sb, f);
    }

    private static void EscribirBucleMensajesX86(StringBuilder sb, FormularioDisenado f)
    {
        sb.AppendLine("; ==========================================================================");
        sb.AppendLine($"; {SimboloMostrar(f)} — muestra la ventana y corre el bucle (x86)");
        sb.AppendLine("; ==========================================================================");
        sb.AppendLine($"{SimboloMostrar(f)}:");
        sb.AppendLine("    push ebp");
        sb.AppendLine("    mov ebp, esp");
        sb.AppendLine();
        sb.AppendLine("    push dword SW_SHOWNORMAL");
        sb.AppendLine($"    push dword [{f.SimboloHwnd}]");
        sb.AppendLine("    call ShowWindow");
        sb.AppendLine();
        sb.AppendLine($"    push dword [{f.SimboloHwnd}]");
        sb.AppendLine("    call UpdateWindow");
        sb.AppendLine();
        sb.AppendLine(".bucle:");
        sb.AppendLine("    push dword 0");
        sb.AppendLine("    push dword 0");
        sb.AppendLine("    push dword 0");
        sb.AppendLine($"    push dword {SimboloMsg(f)}");
        sb.AppendLine("    call GetMessageA");
        sb.AppendLine("    test eax, eax");
        sb.AppendLine("    jz .fin");
        sb.AppendLine();
        sb.AppendLine($"    push dword {SimboloMsg(f)}");
        sb.AppendLine("    call TranslateMessage");
        sb.AppendLine($"    push dword {SimboloMsg(f)}");
        sb.AppendLine("    call DispatchMessageA");
        sb.AppendLine("    jmp .bucle");
        sb.AppendLine();
        sb.AppendLine(".fin:");
        sb.AppendLine("    mov esp, ebp");
        sb.AppendLine("    pop ebp");
        sb.AppendLine("    ret");
        sb.AppendLine();
    }

    private static void EscribirCrearDialogoX86(StringBuilder sb, FormularioDisenado f)
    {
        sb.AppendLine("; ==========================================================================");
        sb.AppendLine($"; {SimboloCrearVentana(f)} — crea el diálogo como ventana hija (x86)");
        sb.AppendLine(";   Argumentos en la pila: hWnd del padre, hInstance");
        sb.AppendLine("; ==========================================================================");
        sb.AppendLine($"{SimboloCrearVentana(f)}:");
        sb.AppendLine("    push ebp");
        sb.AppendLine("    mov ebp, esp");
        sb.AppendLine("    push ebx");
        sb.AppendLine();
        sb.AppendLine("    mov ebx, [ebp+8]               ; padre");
        sb.AppendLine();
        sb.AppendLine("    ; CrearControles lee el hInstance de la variable: se lo dejamos ahí.");
        sb.AppendLine("    mov eax, [ebp+12]");
        sb.AppendLine($"    mov [{SimboloHinst(f)}], eax");
        sb.AppendLine();
        sb.AppendLine("    push dword 0");
        sb.AppendLine("    push dword [ebp+12]                     ; hInstance");
        sb.AppendLine("    push dword 0");
        sb.AppendLine("    push ebx");
        sb.AppendLine($"    push dword {AltoConBordes(f)}");
        sb.AppendLine($"    push dword {AnchoConBordes(f)}");
        sb.AppendLine("    push dword CW_USEDEFAULT");
        sb.AppendLine("    push dword CW_USEDEFAULT");
        sb.AppendLine($"    push dword {f.ExpresionEstiloVentana}");
        sb.AppendLine($"    push dword {SimboloTitulo(f)}");
        sb.AppendLine($"    push dword {SimboloClase(f)}");
        sb.AppendLine("    push dword 0");
        sb.AppendLine("    call CreateWindowExA");
        sb.AppendLine($"    mov [{f.SimboloHwnd}], eax");
        sb.AppendLine();
        sb.AppendLine("    push eax");
        sb.AppendLine($"    call {f.SimboloCrearControles}");
        sb.AppendLine();
        sb.AppendLine($"    mov eax, [{f.SimboloHwnd}]");
        sb.AppendLine();
        sb.AppendLine("    pop ebx");
        sb.AppendLine("    mov esp, ebp");
        sb.AppendLine("    pop ebp");
        sb.AppendLine("    ret 8");
        sb.AppendLine();
    }

    // ------------------------------------------------------------------
    // Símbolos derivados y utilidades
    // ------------------------------------------------------------------

    private static string SimboloTitulo(FormularioDisenado f) => "titulo" + f.Nombre.Trim();
    private static string SimboloClase(FormularioDisenado f) => "clase" + f.Nombre.Trim();
    private static string SimboloHinst(FormularioDisenado f) => "hInst" + f.Nombre.Trim();
    private static string SimboloWc(FormularioDisenado f) => "wc" + f.Nombre.Trim();
    private static string SimboloMsg(FormularioDisenado f) => "msg" + f.Nombre.Trim();

    /// <summary>Rutina que registra la clase y crea la ventana.</summary>
    public static string SimboloCrearVentana(FormularioDisenado f) => "Crear" + f.Nombre.Trim();

    /// <summary>Rutina que muestra la ventana y corre el bucle de mensajes.</summary>
    public static string SimboloMostrar(FormularioDisenado f) => "Mostrar" + f.Nombre.Trim();

    private static string TamEntradaSimbolo(FormularioDisenado f) =>
        "TAM_ENTRADA_" + f.Nombre.Trim().ToUpperInvariant();

    private static string CantSimbolo(FormularioDisenado f) =>
        "CANT_" + f.Nombre.Trim().ToUpperInvariant();

    /// <summary>
    /// El símbolo del literal de una clase Win32: "BUTTON" -> claseWin32BUTTON.
    /// Los caracteres raros de una clase personalizada se sanean, porque el
    /// símbolo tiene que ser un identificador válido.
    /// </summary>
    private static string SimboloClaseControl(string clase) =>
        "claseWin32" + ControlDisenado.SanearNombre(clase, "Custom");

    private static string SimboloTexto(ControlDisenado c) => "txt" + c.Nombre.Trim();

    /// <summary>
    /// El ancho que se le pide a CreateWindowExA para que el ÁREA DE CLIENTE
    /// quede del tamaño diseñado.
    ///
    /// ⚠ CreateWindowExA MIDE LA VENTANA ENTERA, NO EL ÁREA ÚTIL: los bordes
    /// van por fuera. Sin este ajuste el formulario sale más chico que lo
    /// dibujado y los controles de la derecha quedan cortados.
    ///
    /// Lo exacto sería AdjustWindowRect, que consulta las métricas reales del
    /// sistema. Acá se suma el borde típico porque el .inc son datos estáticos
    /// y no puede llamar a una API; el esqueleto generado explica cómo pasar a
    /// AdjustWindowRect si el usuario lo necesita exacto.
    /// </summary>
    private static int AnchoConBordes(FormularioDisenado f) => f.Ancho + 16;

    /// <summary>El alto, más el borde y la barra de título.</summary>
    private static int AltoConBordes(FormularioDisenado f) => f.Alto + 39;

    /// <summary>
    /// Un literal de cadena para NASM.
    ///
    /// ⚠ NASM NO TIENE ESCAPES EN LAS COMILLAS SIMPLES NI DOBLES: un `\n`
    /// dentro de "..." son dos caracteres, barra y ene. Los caracteres de
    /// control se emiten como BYTES SUELTOS fuera de las comillas
    /// (`db "Hola", 13, 10, "mundo", 0`), que es la forma que NASM entiende.
    ///
    /// Las comillas dobles del texto obligan a cortar y emitir el byte 34,
    /// porque adentro de "..." no hay forma de escaparlas.
    /// </summary>
    public static string CadenaAsm(string? texto)
    {
        if (string.IsNullOrEmpty(texto)) return "\"\"";

        var partes = new List<string>();
        var actual = new StringBuilder();

        void CerrarTramo()
        {
            if (actual.Length > 0)
            {
                partes.Add("\"" + actual + "\"");
                actual.Clear();
            }
        }

        foreach (var c in texto)
        {
            // Las comillas dobles y todo lo que no sea imprimible ASCII van
            // como número: es la única forma segura en NASM.
            if (c == '"' || c < 32 || c > 126)
            {
                CerrarTramo();
                partes.Add(((int)c).ToString());
            }
            else
            {
                actual.Append(c);
            }
        }

        CerrarTramo();

        return partes.Count == 0 ? "\"\"" : string.Join(", ", partes);
    }
}
