# Editor ASM

Entorno de desarrollo WinForms (.NET 8) para ensamblador x86/x64 en Windows.
Escribe, ensambla, enlaza y ejecuta sin salir de la app, con **NASM o MASM**
como ensamblador y **GoLink o el link.exe de MSVC** como enlazador.

Además del editor de código tiene proyectos (`.asmproj`), un diseñador visual
de ventanas y diálogos Win32 que genera el ensamblador, y una interfaz al
estilo de Visual Studio: ventana de inicio, barras de título y de estado
propias, y paneles acoplables. Esa reforma está en curso; ver
[Estado del proyecto](#estado-del-proyecto).

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

O para generar un `.exe` standalone (es lo que hace `compilar.ps1`):
```powershell
dotnet publish -c Release -r win-x64 --self-contained false
```
El ejecutable queda en `bin\Release\net8.0-windows\win-x64\publish\AsmEditor.exe`.

## Configuración inicial

Al abrir por primera vez, el editor ya viene apuntando a:
- `nasm.exe` → `C:\Users\Fabian\NASM\nasm.exe`
- `GoLink.exe` → `C:\Users\Fabian\NASM\GoLink.exe`

Si tus rutas son distintas, andá a **Configuración → Opciones...**, categoría
**Herramientas** (NASM y GoLink, MSVC).
Los cambios se guardan en `settings.json`, junto al `.exe` del editor.

Usá **Configuración → Verificar herramientas** para confirmar que las
herramientas se encuentran en las rutas configuradas.

### Opciones

**Configuración → Opciones...** tiene un árbol de categorías a la izquierda y
la página elegida a la derecha, como en Visual Studio:

| Página | Qué se configura |
|---|---|
| Entorno → General | Tema (oscuro / claro) y qué se abre **al iniciar** |
| Entorno → Proyectos | Carpeta del proyecto: la que muestra el explorador sin proyecto abierto y donde arrancan los diálogos de abrir y guardar |
| Herramientas → NASM y GoLink | Rutas de `nasm.exe` y `GoLink.exe` |
| Herramientas → MSVC | Toolchain (ml64/ml + link.exe) y SDK, para 64 y 32 bits |

El tema también se cambia desde **Ver → Tema**.

## Targets

El target activo decide con qué se ensambla, con qué se enlaza y para qué
arquitectura. Se elige en el combo de la barra de herramientas o con un clic
sobre el target en la barra de estado. Se editan en
**Compilar → Administrar targets...**, donde también se configuran las
librerías y el punto de entrada de cada uno.

Vienen estos de fábrica:

| Target | Ensamblador | Enlazador | Arquitectura |
|---|---|---|---|
| Win64 ventana | NASM | GoLink | x64 |
| Win64 consola | NASM | GoLink (`/console`) | x64 |
| Win32 ventana | NASM | GoLink | x86 |
| MSVC 64 ventana | NASM | MSVC link | x64 |
| MSVC 32 ventana | NASM | MSVC link | x86 |
| MSVC 64 consola | NASM | MSVC link | x64 |
| MASM 64 ventana | MASM | MSVC link | x64 |
| MASM 32 ventana | MASM | MSVC link | x86 |
| MASM 64 consola | MASM | MSVC link | x64 |
| Solo ensamblar (64) | NASM | — (solo `.obj`) | x64 |

## Al arrancar

El orden es **splash → ventana de inicio → IDE**.

La **ventana de inicio** muestra los proyectos recientes agrupados por fecha
(Hoy / Esta semana / Anterior), con un buscador (Alt+U), y las acciones
**Crear un proyecto · Abrir un proyecto · Abrir una carpeta · Abrir un archivo
o proyecto · Continuar sin código**. Con el botón derecho sobre un reciente se
lo quita de la lista o se abre su carpeta. Cerrarla con la X o con Esc cierra
el editor, como en Visual Studio.

Qué pasa al arrancar se elige en **Opciones → Entorno → General → Al iniciar**:

- **Ventana de inicio** (la de arriba).
- **Último proyecto**: el que estaba abierto al salir. Si se salió sin
  proyecto, arranca vacío.
- **Entorno vacío**: el IDE sin nada abierto.

Si el editor se abre con un archivo (doble clic en un `.asm`, o por línea de
comandos), la ventana de inicio no aparece.

Los archivos abiertos se recuerdan **por proyecto**: al volver a abrir un
proyecto se reabren sus pestañas y la que estaba activa.

## La ventana del IDE

### Barra de título

La ventana no usa la franja de título de Windows: la reemplaza una barra
propia con el logo (clic: menú de sistema), **el menú del editor**, el
**buscador de comandos (Ctrl+Q)**, el nombre del proyecto o del archivo, y los
botones de minimizar, maximizar y cerrar. Se arrastra, se maximiza con doble
clic y admite Aero Snap como una ventana normal.

### Barra de estado

Muestra lo que está pasando («Listo», «Ensamblando...», «Enlazando...», el
resultado), se pinta con el color de acento mientras compila y tiene:

- contadores de errores y advertencias (clic: abre la lista de errores);
- línea y columna del cursor;
- el target activo (clic: menú para cambiarlo).

### Paneles

El **Explorador** está a la derecha; la **Lista de errores** y la **Salida**
comparten la zona de abajo, con pestañas. Cada panel tiene una barra con su
nombre, un menú ▾ y una ✕ que lo oculta. Se vuelven a mostrar desde el menú
**Ver** (el Explorador también con **Ctrl+B**).

- **Auto-ocultar**: la chincheta (o «Ocultar automáticamente» del ▾) pliega el
  panel a una pestaña en el borde de su lado. Con el ratón encima se despliega
  sin tomar el foco; con un clic se despliega con foco. Se vuelve a plegar
  cuando el ratón y el foco salen de él. Ctrl+B o **Ver** sobre un panel
  auto-oculto **no lo cierra**: lo despliega, o lo pliega si ya estaba
  desplegado.
- **Flotar**: arrastrar la barra del panel, hacerle doble clic o elegir
  «Flotante» en el ▾ lo pasa a una ventana propia. Doble clic en su título o
  «Acoplar» lo devuelve a su zona. La ✕ y Alt+F4 la ocultan, y **Ver** la
  vuelve a mostrar donde quedó. Los atajos del editor funcionan también con el
  foco en una flotante.

## Atajos

| Acción | Atajo |
|---|---|
| Nuevo | Ctrl+N |
| Abrir | Ctrl+O |
| Guardar | Ctrl+S |
| Guardar como | Ctrl+Shift+S |
| Guardar todo | Ctrl+Alt+S |
| Cerrar pestaña | Ctrl+W |
| Cerrar todas | Ctrl+Shift+W |
| Pestaña siguiente / anterior | Ctrl+Tab / Ctrl+Shift+Tab |
| Deshacer | Ctrl+Z |
| Rehacer | Ctrl+Y |
| Buscar | Ctrl+F |
| Reemplazar | Ctrl+H |
| Autocompletar | Ctrl+Espacio |
| Buscar comandos | Ctrl+Q |
| Mostrar / ocultar Explorador | Ctrl+B |
| Actualizar explorador | Ctrl+R |
| Compilar | F7 |
| Enlazar | Ctrl+F7 |
| Compilar y enlazar | Ctrl+Shift+B |
| Compilar, enlazar y ejecutar | F5 |
| Ejecutar solamente | Ctrl+F5 |
| Detener compilación | Shift+F5 |
| Diseñador de formularios | Ctrl+D |
| Documentación de NASM | F1 |

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
| El Explorador | La carpeta del disco | La **lista** de archivos del proyecto |

Que F7 compile el principal evita el caso molesto de mirar un `.inc`, apretar
F7 y que NASM devuelva errores que no explican que se erró de archivo.

El nombre del proyecto aparece en la barra de título, porque con proyecto
abierto F7 ya no compila lo que estás mirando.

### Es una lista, no una carpeta

Un `.asm` que esté en la carpeta pero **no** en la lista no pertenece al
proyecto. Es a propósito: así se pueden tener pruebas y descartes al lado de
los fuentes sin que el proyecto los adopte.

Con el botón derecho sobre el Explorador se agrega, se quita y se marca cuál
es el archivo que compila. **Quitar del proyecto no borra el archivo del
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
- Un `.asmform` se abre con doble clic desde el Explorador, y vuelve a abrirse
  con la sesión del proyecto como cualquier otro archivo.
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
- **Lista de errores**: los errores y advertencias de la última compilación,
  con archivo y línea. Doble clic salta al lugar.
- **Salto a línea de error desde la Salida**: doble clic sobre cualquier línea
  que tenga el formato `archivo.asm:N: error...` (el que usa NASM) y el editor
  salta y selecciona esa línea. Si el error es de otro archivo (por ejemplo un
  `.inc` incluido), lo abre o activa su pestaña.
- **Undo/Redo propio**: el resaltado de sintaxis por regex "ensucia" el undo
  nativo de `RichTextBox` (queda registrado como cambio de formato). Por eso
  el editor lleva su propia pila de undo/redo, con snapshots por pausas de
  tipeo en vez de por cada tecla, y bloquea el Ctrl+Z/Ctrl+Y nativo.
- **Auto-indentación**: al presionar Enter, la nueva línea hereda la sangría
  (espacios/tabs) de la línea anterior.
- **Autocompletar** (Ctrl+Espacio) de instrucciones, registros y directivas.
- **Buscar y reemplazar**: ventana flotante no modal (Ctrl+F / Ctrl+H),
  con opción de coincidir mayúsculas/minúsculas y reemplazar todo.
- El resaltado de sintaxis sigue siendo liviano (basado en expresiones
  regulares): colorea instrucciones, registros, directivas, números, strings,
  comentarios y etiquetas. No valida sintaxis — eso lo sigue haciendo el
  ensamblador.
- La Salida muestra la salida real del ensamblador y del enlazador, línea por
  línea, con errores resaltados en naranja.
- "Compilar y enlazar" guarda el archivo automáticamente antes de compilar
  si hay cambios sin guardar.

## Pruebas y diagnósticos

Las pruebas unitarias están en `AsmEditor.Tests\` y cubren la lógica de
`Core\` (targets, parseo de errores, proyectos, diseñador, modelo del acople,
opciones, recientes):

```powershell
cd C:\Users\Fabian\NASM\AsmEditor\AsmEditor.Tests
dotnet test
```

En `diagnostico\` están los scripts de PowerShell que prueban el editor
**por la interfaz** (abren la ventana, mandan teclas y clics, toman
capturas), más los `romper_*.ps1`, que rompen el código a propósito para
comprobar que cada prueba detecta lo que dice detectar. Dos condiciones:

- Corren sobre un **editor aislado** (`editor_aislado.ps1`: copia de `bin\`
  con su propio `settings.json`), para no tocar la configuración ni los
  archivos de uso diario.
- Necesitan el **escritorio libre**: con otra ventana en uso, las teclas
  pueden ir a otro programa y aparecen fallas falsas.

## Estado del proyecto

La reforma de la interfaz al estilo de Visual Studio se lleva por etapas en
[PLAN_IDE.md](PLAN_IDE.md), con el registro de cada una y su commit.

- **Hecho:** etapa 0 (prototipos de barra de título, terminal y acople),
  etapa 1 (ventana de inicio y ventana Opciones), etapa 2 (barras de título y
  de estado) y etapa 3 hasta **3d** (zonas, pestañas, auto-ocultar y flotar).
- **Falta:** 3e (acoplar arrastrando, con guías), 3f (guardar y restaurar el
  diseño de paneles), 3g (`MainForm` reescrito, menú y barra como VS),
  4 (explorador estilo VS), 5 (barra de navegación), 6 (terminal integrada) y
  7 (cierre).

Defectos conocidos, previos a la reforma y anotados en `PLAN_IDE.md`:

- Cerrar la pestaña activa salta a la primera, no a la vecina.
- El título de la ventana dice siempre «(NASM + GoLink)», aunque el target
  sea MASM o MSVC.
- La barra de estado dice «Sin errores» aunque la compilación se cancele o
  falle sin diagnósticos.
- El editor se cuelga con archivos grandes (en pausa).
