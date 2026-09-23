; window.asm - Ventana Win32 nativa en NASM x64
; Compilar:
;   nasm -f win64 window.asm -o window.obj
;   link window.obj /subsystem:windows /entry:main kernel32.lib user32.lib gdi32.lib
;
; (o con GoLink: golink /entry main window.obj kernel32.dll user32.dll gdi32.dll)

default rel
extern GetModuleHandleA
extern RegisterClassExA
extern CreateWindowExA
extern ShowWindow
extern UpdateWindow
extern GetMessageA
extern TranslateMessage
extern DispatchMessageA
extern DefWindowProcA
extern PostQuitMessage
extern LoadIconA
extern LoadCursorA
extern GetStockObject
extern ExitProcess

section .data
    className   db "MiVentanaClass", 0
    windowTitle db "Ventana en Assembler (NASM)", 0

; --- Offsets de WNDCLASSEXA (x64), tamaño total 80 bytes ---
; 0 cbSize(4) 4 style(4) 8 lpfnWndProc(8) 16 cbClsExtra(4) 20 cbWndExtra(4)
; 24 hInstance(8) 32 hIcon(8) 40 hCursor(8) 48 hbrBackground(8)
; 56 lpszMenuName(8) 64 lpszClassName(8) 72 hIconSm(8)

section .bss
    wc      resb 80
    msg     resb 48
    hInst   resq 1
    hWnd    resq 1

section .text
global main

; ---- Window Procedure ----
; RCX=hWnd, RDX=uMsg, R8=wParam, R9=lParam
WndProc:
    cmp edx, 0x0002              ; WM_DESTROY
    je .destroy
    sub rsp, 40                  ; primer ajuste desde la entrada (RSP%16==8 -> 40 la deja en 0)
    call DefWindowProcA
    add rsp, 40
    ret
.destroy:
    sub rsp, 40                  ; también primer ajuste (nada se ejecutó antes en esta rama)
    xor ecx, ecx
    call PostQuitMessage
    add rsp, 40
    xor eax, eax
    ret

main:
    sub rsp, 40                  ; primer ajuste desde la entrada (RSP%16==8 -> 40 la deja en 0)

    ; hInst = GetModuleHandleA(0)
    xor ecx, ecx
    call GetModuleHandleA
    mov [hInst], rax

    ; --- Rellenar WNDCLASSEXA ---
    mov dword [wc+0], 80
    mov dword [wc+4], 3                    ; CS_HREDRAW|CS_VREDRAW
    lea rax, [WndProc]
    mov [wc+8], rax
    mov dword [wc+16], 0
    mov dword [wc+20], 0
    mov rax, [hInst]
    mov [wc+24], rax

    xor ecx, ecx
    mov edx, 32512                         ; IDI_APPLICATION
    call LoadIconA
    mov [wc+32], rax

    xor ecx, ecx
    mov edx, 32512                         ; IDC_ARROW
    call LoadCursorA
    mov [wc+40], rax

    mov ecx, 0                             ; WHITE_BRUSH
    call GetStockObject
    mov [wc+48], rax

    mov qword [wc+56], 0
    lea rax, [className]
    mov [wc+64], rax
    mov qword [wc+72], 0

    lea rcx, [wc]
    call RegisterClassExA

    ; --- CreateWindowExA ---
    ; Firma: (dwExStyle, lpClassName, lpWindowName, dwStyle,
    ;         X, Y, nWidth, nHeight, hWndParent, hMenu, hInstance, lpParam)
    ; x64: primeros 4 -> RCX,RDX,R8,R9. Resto (8 args, 8 bytes c/u) -> stack tras 32 bytes de shadow space.
    sub rsp, 96                            ; 32 shadow + 8*8 stack-args (96 ya alineado a 16)

    mov dword [rsp+32], 0x80000000         ; X  = CW_USEDEFAULT
    mov dword [rsp+40], 0x80000000         ; Y  = CW_USEDEFAULT
    mov dword [rsp+48], 800                ; nWidth
    mov dword [rsp+56], 600                ; nHeight
    mov qword [rsp+64], 0                  ; hWndParent
    mov qword [rsp+72], 0                  ; hMenu
    mov rax, [hInst]
    mov [rsp+80], rax                      ; hInstance
    mov qword [rsp+88], 0                  ; lpParam

    mov r9d, 0x00CF0000                    ; dwStyle = WS_OVERLAPPEDWINDOW
    lea r8, [windowTitle]                  ; lpWindowName
    lea rdx, [className]                   ; lpClassName
    xor ecx, ecx                           ; dwExStyle = 0
    call CreateWindowExA
    add rsp, 96
    mov [hWnd], rax

    sub rsp, 32
    mov rcx, [hWnd]
    mov edx, 1                             ; SW_SHOWNORMAL
    call ShowWindow

    mov rcx, [hWnd]
    call UpdateWindow
    add rsp, 32

.loop:
    sub rsp, 32
    lea rcx, [msg]
    xor edx, edx
    xor r8d, r8d
    xor r9d, r9d
    call GetMessageA
    add rsp, 32
    test eax, eax
    jz .end

    sub rsp, 32
    lea rcx, [msg]
    call TranslateMessage
    lea rcx, [msg]
    call DispatchMessageA
    add rsp, 32
    jmp .loop

.end:
    xor ecx, ecx
    call ExitProcess