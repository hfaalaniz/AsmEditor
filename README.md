# Editor ASM (NASM + GoLink)

Editor WinForms mínimo para escribir, compilar y enlazar archivos `.asm` con
NASM y GoLink, sin salir de la app.

## Requisitos para compilar el editor

- Visual Studio 2022 (workload ".NET desktop development") **o**
- .NET 8 SDK + `dotnet` CLI

## Compilar y ejecutar

### Opción A: Visual Studio
1. Abrí `AsmEditor.csproj` con Visual Studio.
2. F5 para compilar y ejecutar.

### Opción B: línea de comandos
```powershell
cd C:\Users\Fabian\NASM\AsmEditor
dotnet run
```

O para generar un `.exe` standalone:
```powershell
dotnet publish -c Release -r win-x64 --self-contained false
```
El ejecutable queda en `bin\Release\net8.0-windows\win-x64\publish\AsmEditor.exe`.

## Configuración inicial

Al abrir por primera vez, el editor ya viene apuntando a:
- `nasm.exe` → `C:\Users\Fabian\NASM\nasm.exe`
- `GoLink.exe` → `C:\Users\Fabian\NASM\GoLink.exe`
- Librerías por defecto: `kernel32.dll user32.dll gdi32.dll`
- Punto de entrada: `main`

Si tus rutas son distintas, andá a **Configuración → Opciones...**, categoría
**Herramientas** (NASM y GoLink, MSVC).
Los cambios se guardan en `settings.json`, junto al `.exe` del editor.

Usá **Configuración → Verificar herramientas** para confirmar que NASM y
GoLink se encuentran en las rutas configuradas.

## Atajos

| Acción | Atajo |
|---|---|
| Nuevo | Ctrl+N |
| Abrir | Ctrl+O |
| Guardar | Ctrl+S |
| Guardar como | Ctrl+Shift+S |
| Deshacer | Ctrl+Z |
| Rehacer | Ctrl+Y |
| Buscar | Ctrl+F |
| Reemplazar | Ctrl+H |
| Compilar (NASM) | F7 |
| Enlazar (GoLink) | Ctrl+F7 |
| Compilar y enlazar | Ctrl+Shift+B |
| Compilar, enlazar y ejecutar | F5 |
| Ejecutar solamente | Ctrl+F5 |

## Proyectos

El menú **Proyecto** permite agrupar archivos en un `.asmproj`, con su propio
archivo principal, carpeta de salida y targets.

**El proyecto es opcional.** Sin proyecto abierto el editor funciona igual que
siempre: abre un `.asm` suelto, compila la pestaña activa y deja el `.obj` y el
`.exe` al lado del fuente.

### Qué cambia con un proyecto abierto

| | Sin proyecto | Con proyecto |
|---|---|---|
| Qué compila F7 | La pestaña activa | El **archivo principal** declarado |
| Dónde va la salida | Al lado del fuente | Donde diga el proyecto (`bin` en los nuevos) |
| Targets | Los de la configuración general | Los del proyecto (o los generales, si los hereda) |
| El panel izquierdo | La carpeta del disco | La **lista** de archivos del proyecto |

Que F7 compile el principal evita el caso molesto de mirar un `.inc`, apretar
F7 y que NASM devuelva errores que no explican que se erró de archivo.

El nombre del proyecto aparece entre corchetes en el título de la ventana,
porque con proyecto abierto F7 ya no compila lo que estás mirando.

### Es una lista, no una carpeta

Un `.asm` que esté en la carpeta pero **no** en la lista no pertenece al
proyecto. Es a propósito: así se pueden tener pruebas y descartes al lado de
los fuentes sin que el proyecto los adopte.

Con el botón derecho sobre el panel izquierdo se agrega, se quita y se marca
cuál es el archivo que compila. **Quitar del proyecto no borra el archivo del
disco.**

Un archivo de la lista que ya no esté se muestra en rojo como `(falta)` y no
se saca solo: puede ser que lo movieron o que falta traerlo de un respaldo.

### El archivo .asmproj

JSON, con rutas **relativas** a su propia carpeta, para que mover o copiar el
proyecto no lo rompa:

```json
{
  "Version": 1,
  "Proyecto": {
    "Nombre": "Mi programa",
    "ArchivoPrincipal": "ventana.asm",
    "CarpetaSalida": "bin",
    "Archivos": [ "ventana.asm", "ventana.inc" ],
    "Targets": [],
    "TargetActivo": 0
  }
}
```

`CarpetaSalida` vacía significa «al lado del fuente», que es lo que el editor
hizo siempre. `Targets` vacío significa que usa los de la configuración general
y sigue recibiendo los cambios que se hagan ahí.

## Diseñador de formularios

**Herramientas → Diseñador de formularios** (Ctrl+D) abre un diseñador visual
para armar ventanas y diálogos Win32 sin escribir a mano las llamadas a
`CreateWindowExA`.

Se dibuja el formulario, se sueltan controles desde la paleta y el diseñador
escribe el ensamblador.

### Es una pestaña más

El diseñador vive en una pestaña, al lado de las de código. Eso significa que:

- Se guarda con **Ctrl+S** y se cierra con **Ctrl+W**, como cualquier documento.
- Se puede mirar el `.asm` y el formulario al mismo tiempo, saltando de pestaña.
- Un `.asmform` se abre con doble clic desde el panel de la izquierda, y se
  reabre solo al arrancar el editor si quedó abierto.
- Guardar el formulario **regenera su `.inc`**, y si ese `.inc` está abierto en
  otra pestaña, esa pestaña se recarga sola (salvo que tenga cambios sin
  guardar, en cuyo caso avisa y no la pisa).

Buscar (Ctrl+F) queda deshabilitado en una pestaña de diseño: no hay texto que
buscar.

### Grilla

Arrastrar un control lo ajusta a una grilla de 4 px. Un clic no mueve nada:
el control empieza a moverse recién cuando el ratón se aleja más que la
tolerancia de clic de Windows. Al redimensionar, solo se ajusta el borde que
se arrastra; el opuesto queda donde estaba. Las flechas mueven de a 1 px y
Shift+flechas de a 4 px, sin ajustar.

### Deshacer y rehacer

**Ctrl+Z** y **Ctrl+Y** funcionan en el diseñador, con hasta 100 pasos por
pestaña. Cada paso es una acción entera, no cada movimiento del ratón:

- Arrastrar o redimensionar un control es un paso, al soltar el ratón.
- Mantener apretada una flecha es un paso, al soltar la tecla.
- Soltar un control, borrar, alinear y cambiar una propiedad son un paso cada uno.
- Una acción que no cambia nada (un clic, un arrastre que vuelve a su lugar)
  no deja paso.

Deshacer hasta el estado guardado le quita el `*` a la pestaña. Mientras se
escribe un valor en el panel de propiedades, Ctrl+Z deshace lo tipeado en esa
casilla, no el formulario.

### Los tres archivos

| Archivo | Quién lo escribe | Se pisa |
|---|---|---|
| `Form.asmform` | El diseñador | Sí, al guardar |
| `Form.inc` | El diseñador | **Sí, entero, cada vez** |
| `Form.asm` | El diseñador la primera vez; después **vos** | **Nunca** |

El `.asmform` (JSON) es la fuente de verdad. El `.inc` se regenera completo y
no hay que editarlo. El `.asm` es tuyo: se genera una sola vez con el esqueleto
y el diseñador no lo vuelve a tocar nunca, aunque agregues controles. Cuando
hay controles nuevos, el diseñador copia al portapapeles el texto de los
manejadores que faltan para que lo pegues donde quieras.

### Cómo se accede a los controles desde el código

Cada control genera un `%define IDC_<NOMBRE>` en el `.inc`. Ese número es el
que llega en `LOWORD(wParam)` con `WM_COMMAND` y el que se le pasa a
`GetDlgItem`:

```nasm
.comando:
    movzx eax, r8w                 ; el ID del control que avisa
    cmp eax, IDC_BOTONACEPTAR
    je .al_botonaceptar
```

Los controles no se crean con una llamada escrita por control: el `.inc` lleva
una **tabla de datos** con una fila por control y una rutina que la recorre
llamando a `CreateWindowExA`. Agregar un control es agregar una fila.

### Ventanas y diálogos

- **Ventana principal**: registra su clase, tiene punto de entrada y bucle de
  mensajes propio. Es la que abre el programa.
- **Diálogo**: ventana hija que corre dentro del bucle de la principal. Su
  `WM_DESTROY` **no** llama a `PostQuitMessage` — eso terminaría el programa
  entero.

Genera código distinto para 32 y 64 bits: no es el mismo con otro `-f`, cambia
la convención de llamada completa.

## Funciones del editor

- **Números de línea**: panel sincronizado a la izquierda (vía mensajes nativos
  de RichEdit, no repintado manual — se mantiene alineado incluso en archivos
  largos sin parpadeo).
- **Salto a línea de error**: doble clic sobre cualquier línea del panel de
  salida que tenga el formato `archivo.asm:N: error...` (el que usa NASM) y el
  editor salta y selecciona esa línea directamente. Solo salta si el error
  pertenece al archivo actualmente abierto (no a un `.inc` incluido).
- **Undo/Redo propio**: el resaltado de sintaxis por regex "ensucia" el undo
  nativo de `RichTextBox` (queda registrado como cambio de formato). Por eso
  el editor lleva su propia pila de undo/redo, con snapshots por pausas de
  tipeo en vez de por cada tecla, y bloquea el Ctrl+Z/Ctrl+Y nativo.
- **Auto-indentación**: al presionar Enter, la nueva línea hereda la sangría
  (espacios/tabs) de la línea anterior.
- **Buscar y reemplazar**: ventana flotante no modal (Ctrl+F / Ctrl+H),
  con opción de coincidir mayúsculas/minúsculas y reemplazar todo.
- El resaltado de sintaxis sigue siendo liviano (basado en expresiones
  regulares): colorea instrucciones, registros, directivas, números, strings,
  comentarios y etiquetas. No valida sintaxis — eso lo sigue haciendo NASM.
- El panel inferior muestra la salida real de `nasm.exe` y `GoLink.exe`,
  línea por línea, con errores resaltados en naranja.
- "Compilar y enlazar" guarda el archivo automáticamente antes de compilar
  si hay cambios sin guardar.
