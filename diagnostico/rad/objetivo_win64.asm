; ============================================================================
; objetivo_win64.asm — EL CÓDIGO QUE EL DISEÑADOR TIENE QUE GENERAR
;
; Escrito A MANO para verificar contra NASM y GoLink reales que el enfoque
; A+C ensambla, enlaza y corre. Recién cuando esto funciona se escribe el
; generador que lo produce.
;
; Enfoque C: los controles NO se crean con una llamada escrita por control,
; sino con una TABLA de datos que un runtime recorre. Agregar un control es
; agregar una fila, no editar código.
;
;   nasm -f win64 objetivo_win64.asm -o objetivo_win64.obj
;   GoLink /entry main objetivo_win64.obj kernel32.dll user32.dll gdi32.dll
; ============================================================================

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
extern SendMessageA
extern GetDlgItem

; ---------------------------------------------------------------------------
; Constantes de Windows que usa el código generado
; ---------------------------------------------------------------------------
%define WM_DESTROY      0x0002
%define WM_CREATE       0x0001
%define WM_COMMAND      0x0111
%define WM_SETFONT      0x0030

%define WS_CHILD        0x40000000
%define WS_VISIBLE      0x10000000
%define WS_BORDER       0x00800000
%define WS_TABSTOP      0x00010000
%define WS_VSCROLL      0x00200000
%define WS_OVERLAPPEDWINDOW 0x00CF0000

%define BS_DEFPUSHBUTTON 0x0001
%define BS_AUTOCHECKBOX  0x0003
%define ES_AUTOHSCROLL   0x0080

%define CW_USEDEFAULT   0x80000000
%define SW_SHOWNORMAL   1
%define IDI_APPLICATION 32512
%define IDC_ARROW       32512
%define WHITE_BRUSH     0

; ---------------------------------------------------------------------------
; IDs de los controles — esto es lo que el diseñador asigna y el usuario usa
; ---------------------------------------------------------------------------
%define IDC_ETIQUETA_NOMBRE  1001
%define IDC_CAMPO_NOMBRE     1002
%define IDC_BOTON_ACEPTAR    1003
%define IDC_BOTON_CANCELAR   1004
%define IDC_CHECK_RECORDAR   1005
%define IDC_LISTA            1006

; ---------------------------------------------------------------------------
; LA TABLA DE CONTROLES (el corazón del enfoque C)
;
; Cada fila describe un control. El runtime CrearControles la recorre y llama
; CreateWindowExA una vez por fila. 48 bytes por entrada:
;
;   +0   clase     (qword)  puntero al nombre de clase ("BUTTON", "EDIT"...)
;   +8   texto     (qword)  puntero al texto inicial, o 0
;   +16  estilo    (dword)  dwStyle
;   +20  id        (dword)  identificador del control
;   +24  x         (dword)
;   +28  y         (dword)
;   +32  ancho     (dword)
;   +36  alto      (dword)
;   +40  exestilo  (dword)
;   +44  relleno   (dword)  alineación a 8
; ---------------------------------------------------------------------------
%define CTL_SIZE 48

section .data

    claseVentana    db "RadVentanaClase", 0
    tituloVentana   db "Ventana generada por el disenador", 0

    claseBoton      db "BUTTON", 0
    claseEdit       db "EDIT", 0
    claseStatic     db "STATIC", 0
    claseListBox    db "LISTBOX", 0

    txtEtiqueta     db "Nombre:", 0
    txtCampo        db "", 0
    txtAceptar      db "Aceptar", 0
    txtCancelar     db "Cancelar", 0
    txtRecordar     db "Recordar datos", 0

    txtClicAceptar  db "Clic en Aceptar", 0
    txtClicCancelar db "Clic en Cancelar", 0

    align 8
; --- La tabla ---
tablaControles:
    ; STATIC "Nombre:"
    dq claseStatic, txtEtiqueta
    dd WS_CHILD|WS_VISIBLE, IDC_ETIQUETA_NOMBRE
    dd 20, 24, 80, 20
    dd 0, 0

    ; EDIT
    dq claseEdit, txtCampo
    dd WS_CHILD|WS_VISIBLE|WS_BORDER|WS_TABSTOP|ES_AUTOHSCROLL, IDC_CAMPO_NOMBRE
    dd 110, 20, 220, 24
    dd 0, 0

    ; BUTTON "Aceptar"
    dq claseBoton, txtAceptar
    dd WS_CHILD|WS_VISIBLE|WS_TABSTOP|BS_DEFPUSHBUTTON, IDC_BOTON_ACEPTAR
    dd 110, 60, 100, 30
    dd 0, 0

    ; BUTTON "Cancelar"
    dq claseBoton, txtCancelar
    dd WS_CHILD|WS_VISIBLE|WS_TABSTOP, IDC_BOTON_CANCELAR
    dd 230, 60, 100, 30
    dd 0, 0

    ; CHECKBOX
    dq claseBoton, txtRecordar
    dd WS_CHILD|WS_VISIBLE|WS_TABSTOP|BS_AUTOCHECKBOX, IDC_CHECK_RECORDAR
    dd 110, 100, 160, 24
    dd 0, 0

    ; LISTBOX
    dq claseListBox, 0
    dd WS_CHILD|WS_VISIBLE|WS_BORDER|WS_VSCROLL, IDC_LISTA
    dd 20, 140, 310, 120
    dd 0, 0

tablaControlesFin:

%define CANT_CONTROLES ((tablaControlesFin - tablaControles) / CTL_SIZE)

section .bss
    wc      resb 80
    msg     resb 48
    hInst   resq 1
    hWnd    resq 1

section .text
global main

; ===========================================================================
; CrearControles — el runtime del enfoque C
;   RCX = hWnd padre
; Recorre la tabla y crea un control por fila.
; ===========================================================================
CrearControles:
    push rbx
    push rsi
    push rdi
    push r12
    sub rsp, 40                    ; 4 push (32) + 40 = 72; +8 del call = 80, alineado

    mov rbx, rcx                   ; rbx = hWnd padre
    lea rsi, [tablaControles]      ; rsi = fila actual
    mov r12d, CANT_CONTROLES       ; r12 = cuántas quedan

.siguiente:
    test r12d, r12d
    jz .fin

    ; CreateWindowExA(exStyle, clase, texto, estilo, x, y, ancho, alto,
    ;                 padre, id, hInst, 0)
    ; 12 argumentos: 4 en registros, 8 en la pila tras el shadow space.
    sub rsp, 96                    ; 32 shadow + 8*8

    mov eax, [rsi+24]              ; x
    mov [rsp+32], eax
    mov eax, [rsi+28]              ; y
    mov [rsp+40], eax
    mov eax, [rsi+32]              ; ancho
    mov [rsp+48], eax
    mov eax, [rsi+36]              ; alto
    mov [rsp+56], eax
    mov [rsp+64], rbx              ; hWndParent
    mov eax, [rsi+20]              ; id -> hMenu
    mov [rsp+72], rax
    mov rax, [hInst]
    mov [rsp+80], rax              ; hInstance
    mov qword [rsp+88], 0          ; lpParam

    mov r9d, [rsi+16]              ; dwStyle
    mov r8,  [rsi+8]               ; texto
    mov rdx, [rsi+0]               ; clase
    mov ecx, [rsi+40]              ; exStyle
    call CreateWindowExA

    add rsp, 96

    add rsi, CTL_SIZE
    dec r12d
    jmp .siguiente

.fin:
    add rsp, 40
    pop r12
    pop rdi
    pop rsi
    pop rbx
    ret

; ===========================================================================
; VentanaProc — RCX=hWnd RDX=uMsg R8=wParam R9=lParam
; ===========================================================================
VentanaProc:
    push rbx
    push rsi
    sub rsp, 40                    ; 2 push (16) + 40 = 56; +8 del call = 64, alineado

    mov rbx, rcx                   ; hWnd
    mov esi, edx                   ; uMsg

    cmp esi, WM_CREATE
    je .crear
    cmp esi, WM_COMMAND
    je .comando
    cmp esi, WM_DESTROY
    je .destruir

.pordefecto:
    mov rcx, rbx
    mov edx, esi
    call DefWindowProcA
    jmp .salir

.crear:
    mov rcx, rbx
    call CrearControles
    xor eax, eax
    jmp .salir

.comando:
    ; LOWORD(wParam) = ID del control que avisa
    movzx eax, r8w
    cmp eax, IDC_BOTON_ACEPTAR
    je .aceptar
    cmp eax, IDC_BOTON_CANCELAR
    je .cancelar
    xor eax, eax
    jmp .salir

.aceptar:
    ; Agrega una línea a la lista: SendMessageA(lista, LB_ADDSTRING=0x180, 0, texto)
    mov rcx, rbx
    mov edx, IDC_LISTA
    call GetDlgItem
    mov rcx, rax
    mov edx, 0x180
    xor r8d, r8d
    lea r9, [txtClicAceptar]
    call SendMessageA
    xor eax, eax
    jmp .salir

.cancelar:
    mov rcx, rbx
    mov edx, IDC_LISTA
    call GetDlgItem
    mov rcx, rax
    mov edx, 0x180
    xor r8d, r8d
    lea r9, [txtClicCancelar]
    call SendMessageA
    xor eax, eax
    jmp .salir

.destruir:
    xor ecx, ecx
    call PostQuitMessage
    xor eax, eax

.salir:
    add rsp, 40
    pop rsi
    pop rbx
    ret

; ===========================================================================
main:
    sub rsp, 40

    xor ecx, ecx
    call GetModuleHandleA
    mov [hInst], rax

    ; --- WNDCLASSEXA ---
    mov dword [wc+0], 80
    mov dword [wc+4], 3                    ; CS_HREDRAW|CS_VREDRAW
    lea rax, [VentanaProc]
    mov [wc+8], rax
    mov dword [wc+16], 0
    mov dword [wc+20], 0
    mov rax, [hInst]
    mov [wc+24], rax

    xor ecx, ecx
    mov edx, IDI_APPLICATION
    call LoadIconA
    mov [wc+32], rax

    xor ecx, ecx
    mov edx, IDC_ARROW
    call LoadCursorA
    mov [wc+40], rax

    mov ecx, WHITE_BRUSH
    call GetStockObject
    mov [wc+48], rax

    mov qword [wc+56], 0
    lea rax, [claseVentana]
    mov [wc+64], rax
    mov qword [wc+72], 0

    lea rcx, [wc]
    call RegisterClassExA

    ; --- CreateWindowExA de la ventana principal ---
    sub rsp, 96
    mov dword [rsp+32], CW_USEDEFAULT
    mov dword [rsp+40], CW_USEDEFAULT
    mov dword [rsp+48], 380                ; ancho
    mov dword [rsp+56], 330                ; alto
    mov qword [rsp+64], 0
    mov qword [rsp+72], 0
    mov rax, [hInst]
    mov [rsp+80], rax
    mov qword [rsp+88], 0

    mov r9d, WS_OVERLAPPEDWINDOW
    lea r8, [tituloVentana]
    lea rdx, [claseVentana]
    xor ecx, ecx
    call CreateWindowExA
    add rsp, 96
    mov [hWnd], rax

    mov rcx, [hWnd]
    mov edx, SW_SHOWNORMAL
    call ShowWindow

    mov rcx, [hWnd]
    call UpdateWindow

.bucle:
    lea rcx, [msg]
    xor edx, edx
    xor r8d, r8d
    xor r9d, r9d
    call GetMessageA
    test eax, eax
    jz .terminar

    lea rcx, [msg]
    call TranslateMessage
    lea rcx, [msg]
    call DispatchMessageA
    jmp .bucle

.terminar:
    xor ecx, ecx
    call ExitProcess
