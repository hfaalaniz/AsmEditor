# Plan: IDE estilo Visual Studio

Aprobado por Fabián el 23/09/2026. Este archivo es la referencia para ejecutar
la reforma **por etapas** y para retomarla entre sesiones: cada etapa se marca
al terminarla, con la fecha y el commit.

> **Regla de avance:** cada etapa termina con compilación, pruebas unitarias,
> su diagnóstico por interfaz (con capturas) y **el OK de Fabián**. No se
> arranca la siguiente sin ese OK. Si en el camino cambia el alcance o el
> mecanismo, se vuelve a presentar antes de escribir código.

---

## Qué se quiere

Llevar el Editor ASM a la estructura de Visual Studio 2026, a partir de dos
capturas de Fabián:

- **Ventana de inicio** antes del IDE: buscador, proyectos recientes
  agrupados por fecha y acciones a la derecha.
- **IDE**: barra de título propia con el menú integrado, barra de
  herramientas, documentos al centro con **barra de navegación**, **Explorador
  a la derecha**, panel inferior (**Lista de errores · Salida · Terminal**),
  pie del editor y barra de estado. Paneles **acoplables, flotantes y con
  auto-ocultar**.

## Decisiones (23/09/2026)

| Tema | Decisión |
|---|---|
| Arranque | **splash → ventana de inicio → IDE**. Si no se elige proyecto, el IDE arranca **vacío**. Con un archivo por línea de comandos, la ventana de inicio se saltea. |
| Pantalla de bienvenida | **Sigue existiendo** tal como está: se ve dentro del IDE cuando no hay pestañas. |
| Formularios nuevos | **Todo archivo nuevo lleva su `.Designer.cs`** (ver «Reglas»). |
| «Clonar un repositorio» | Se reemplaza por **«Abrir un archivo o proyecto»**. |
| Barra de título | **Personalizada**, con el menú integrado. |
| Barra de navegación, terminal, paneles flotantes | **Se hacen ahora**, dentro de este plan. |
| Sistema de acople | **Propio**, no DockPanelSuite (se autorizó probarlo, pero la decisión es construir el nuestro). |
| Respaldo | **`git init`** en `AsmEditor\`, un commit por etapa. Commit inicial: estado previo a la reforma. |

## Reglas que se aplican en todas las etapas

1. **Designer.cs en todo archivo nuevo.** Controles, propiedades y **todos los
   `Controls.Add`** en `InitializeComponent()`; eventos con **métodos con
   nombre** (nunca lambdas); `Location`/`Size`/`Anchor` para que se arrastren en
   el diseñador; `Dock` solo para cabecera y pie. En el `.cs`, solo lógica.
   Lo que no puede ir al diseñador (tooltips, glifos dibujados, colores del
   tema en vivo) va al `.cs`, y el «por qué» se documenta ahí, no en el
   `Designer.cs`. Verificar abriendo el diseñador, no solo compilando.
2. **Las conversiones se reescriben, no se parchan.** El armado visual de
   `MainForm` se reemplaza entero; nunca conviven el layout viejo y el nuevo.
3. **La lógica no se toca**: compilar, proyectos, pestañas (`IPestanaEditor`),
   diseñador de formularios, deshacer. La reforma es de estructura visual.
4. **Diagnósticos aislados**: se corren con el editor aislado
   (`diagnostico\editor_aislado.ps1`) y con el escritorio libre. Los que
   ubican controles por posición se ajustan en la etapa que los mueve.
5. **Pruebas verificadas rompiéndolas** antes de darlas por buenas.

---

## Etapas

### Etapa 0 · Preparación y pruebas de concepto — *0.1 y 0.2 hechas; 0.3 y 0.4 después de la Etapa 2*

El editor no cambia. Se prueba aparte lo que puede salir caro.
Los prototipos viven en `diagnostico\prototipos\` (ya excluido del `.csproj`).

- [x] **0.1 Respaldo:** `git init`, `.gitignore`, `.gitattributes` (sin
  conversión de fin de línea) y commit inicial. *(23/09/2026)*
- [x] **0.2 Barra de título propia** *(23/09/2026, verde)*. Técnica de Windows
  Terminal: se CONSERVA el marco nativo y solo se quita la franja del título
  (`WM_NCCALCSIZE`); la barra contesta `HTTRANSPARENT` y el formulario decide
  título / borde superior (`WM_NCHITTEST`). Prototipo en
  `diagnostico\prototipos\barra_titulo\`, con `Designer.cs`. Medido con
  `probar_barra_titulo.ps1`, 15/15: sin franja nativa, arrastre, borde
  derecho (nativo) y superior (nuestro), doble clic, maximizar exacto al área
  de trabajo en los dos monitores, botones, Aero Snap, paso entre monitores,
  Alt+Espacio, cerrar. Capturas sin franja blanca.
  ⚠ Lecciones: las pruebas por interfaz tienen que verificar que el punto es
  de la ventana antes de hacer clic (la primera corrida le hizo clic a Chrome);
  en PowerShell, dentro de `@( )` la coma pesa más que la suma.
  Pendiente de estilo para la Etapa 2: la barra necesita un tono distinto al
  del contenido.
- [x] **0.3 Terminal** *(23/09/2026, verde)*: **ConPTY**. Prototipo en
  `diagnostico\prototipos\terminal\` (`SesionConPty`, `InterpreteVT`,
  `PantallaTerminal`, `ControlTerminal` y `FormTerminal` con `Designer.cs`).
  `PruebaTerminal.exe --prueba` mide SIN VENTANA (no toca ratón ni teclado):
  prompt en ~300 ms, eco, colores (91/92), `nasm -v` por el PATH, Read-Host,
  Ctrl+C corta `ping -t`, cambio de tamaño (80 columnas), salida con código 7
  — 12/12, y la de colores verificada rompiéndola. La redirección, en cambio:
  sin prompt, sin colores, sin Ctrl+C ni cambio de tamaño.
  ⚠ Dos hallazgos que el editor tiene que respetar: (1) `STARTF_USESTDHANDLES`
  con manejadores nulos, o si nuestra salida está redirigida el programa
  escribe ahí y la terminal queda en blanco; (2) **la pseudoconsola no cierra
  su salida cuando el programa termina**: hay que vigilar el proceso y
  cerrarla, o el lector espera para siempre.
  Pendiente: probar el teclado en la ventana a mano (Fabián).
- [x] **0.4 Acople propio** *(23/09/2026, verde)*. Prototipo en
  `diagnostico\prototipos\acople\` (`AnfitrionAcople`, `VentanaHerramienta`,
  `VentanaFlotante`, `GuiasAcople`, `VistaPreviaAcople`, `FormPrueba`, todos
  con `Designer.cs`; geometría en `GeometriaAcople`). Técnica: al sacar el
  panel, la flotante aparece bajo el ratón y **el arrastre se le entrega a
  Windows** (`WM_NCLBUTTONDOWN` + `HTCAPTION`); `WM_MOVING` muestra guías y
  vista previa, `WM_EXITSIZEMOVE` decide si acopla. Guías y vista previa son
  ventanas que no se activan ni reciben el ratón.
  Medido: `PruebaAcople.exe --prueba` (geometría y modelo, sin ratón) 17/17;
  `probar_acople.ps1` (arrastre real, con clics protegidos) 8/8: sacar a
  flotante, guías, vista previa, volver a acoplar, y afuera/adentro en un
  solo arrastre.
  ⚠ Excepción a la regla del diseñador: el anfitrión usa `Dock` en zonas y
  `Splitter` (sin Dock un Splitter no funciona). Es el único lugar.
  Para la versión real (3a–3f): la vista previa tiene que salir de la
  disposición real (abajo queda ENTRE izquierda y derecha, no a todo el
  ancho); la flotante necesita barra propia con el tema; zonas con pestañas
  (3b) y auto-ocultar (3c) no se probaron acá.
- [ ] **Entrega:** informe de cada prueba y decisiones de Fabián.

### Etapa 1 · Ventana de inicio

- `VentanaInicio` + `.Designer.cs`: título, «Introducción», buscador,
  recientes agrupados (**Hoy / Esta semana / Anterior**) con nombre, ruta y
  fecha; clic derecho: «Quitar de la lista», «Abrir carpeta contenedora».
- Acciones: **Crear un proyecto · Abrir un proyecto · Abrir una carpeta ·
  Abrir un archivo o proyecto · Continuar sin código**.
- `Core` (con pruebas): fecha de última apertura en los recientes (con
  migración de los `settings.json` que solo guardan la ruta), agrupado por
  fecha, filtro del buscador, opción **«Al iniciar: ventana de inicio / último
  proyecto / entorno vacío»**, y **archivos abiertos por proyecto** (para no
  perder la reapertura de la sesión que existe hoy).
- `Program.cs`: splash → ventana de inicio → `MainForm` con la elección.
- Diagnósticos: el editor aislado arranca en «entorno vacío» (o «último
  proyecto» para `probar_por_interfaz.ps1`); nuevo `ventana_inicio.ps1`.

### Etapa 2 · Barras de título y de estado propias (solo `MainForm`) — *adelantada, 23/09/2026*

Fabián pidió hacerla antes que 0.3, 0.4 y la Etapa 1, con las dos barras
«con todo lo que deben tener» y solo en `MainForm`.

- **`BarraTitulo` + `.Designer.cs`**: logo (zona HTSYSMENU: clic abre el menú
  de sistema, doble clic cierra — lo hace Windows), **el menú de siempre
  mudado a la barra** (uno solo; sigue siendo `MainMenuStrip`), buscador de
  comandos **Ctrl+Q** (`PopupBusqueda` + `.Designer.cs`, ventana que no se
  activa; filtro en `Core\FiltroComandos.cs` con pruebas), insignia con el
  proyecto o el archivo, minimizar / maximizar / cerrar, textos atenuados con
  la ventana inactiva.
- **`BarraEstado` + `.Designer.cs`**: estado con ícono («Listo»,
  «Ensamblando...», «Enlazando...», resultado, aviso de herramientas),
  **color de acento mientras compila**, contadores de errores y advertencias
  (clic: lista de errores), Ln/Col, **target activo (clic: menú para
  cambiarlo, pasa por el combo)**.
- `MainForm`: manejo de `WM_NCCALCSIZE` / `WM_NCHITTEST` del prototipo 0.2;
  la barra vieja (`StatusStrip`) y el `MenuStrip` suelto se borraron enteros.
- Verificación: `diagnostico\probar_barras.ps1` sobre el editor aislado,
  25/25 (título, maximizado exacto, arrastre, logo, botones, compilar con y
  sin error, contadores, target, Ctrl+N, buscador, cerrar con confirmación).

### Etapa 3 · Sistema de acople propio y reescritura de `MainForm`

El modelo del diseño de ventanas (qué panel está en qué zona, tamaños,
flotantes, ocultos) vive en `Core` y se prueba sin interfaz. Cada pieza
visual nueva lleva su `.Designer.cs`.

- [ ] **3a · Zonas fijas:** anfitrión con zonas izquierda / derecha / abajo /
  centro, divisores redimensionables; ventana de herramienta con barra de
  título (nombre, ▾, chincheta, ✕); mostrar y ocultar desde «Ver».
- [ ] **3b · Pestañas por zona:** varios paneles en la misma zona (Lista de
  errores · Salida · Terminal).
- [ ] **3c · Auto-ocultar (chincheta):** el panel se repliega a una pestaña en
  el borde y se despliega al pasar el ratón.
- [ ] **3d · Flotar:** arrastrar la barra de título fuera de la zona lo
  convierte en ventana flotante; doble clic lo vuelve a acoplar.
- [ ] **3e · Acoplar arrastrando:** guías de acople (rombo central y guías de
  borde) con vista previa translúcida.
- [ ] **3f · Diseño persistente:** se guarda y se restaura; «Ventana →
  Restablecer diseño».
- [ ] **3g · `MainForm` reescrito** sobre el anfitrión: el área central aloja
  las **`PestanasAsm` actuales** (sin tocar la lógica de pestañas); paneles
  **Explorador** (derecha), **Lista de errores** y **Salida** (abajo).
  Menú como el de VS (Archivo, Editar, Ver, Proyecto, Compilar,
  Herramientas → **Opciones**, Ventana, Ayuda). Barra: guardar ·
  **cortar/copiar/pegar** · deshacer/rehacer · target · **▶ Ejecutar
  «Proyecto»**. Pie del editor: «N errores · Línea, Carácter · CRLF/LF ·
  codificación». Barra de estado: «Listo» / «Compilando...».

### Etapa 4 · Explorador de proyectos estilo VS

- Barra: actualizar, contraer todo, **mostrar todos los archivos**,
  propiedades. **Buscador** que filtra el árbol.
- Árbol **Proyecto «X» → carpetas → archivos**, con el principal marcado.
- Se conservan el modo carpeta y el menú contextual actual.

### Etapa 5 · Barra de navegación

- `Core` (con pruebas): analizador de ASM (NASM y MASM) que encuentra
  secciones, procedimientos y etiquetas con su línea.
- `BarraNavegacion` + `.Designer.cs`: tres combos **archivo · sección ·
  etiqueta**; siguen al cursor y saltan al elegir.

### Etapa 6 · Terminal integrada

- Panel «Terminal» + `.Designer.cs`: PowerShell en la carpeta del proyecto,
  con NASM / GoLink / MSVC en el `PATH` (el equivalente a «Developer
  PowerShell»), con la tecnología elegida en 0.3.

### Etapa 7 · Cierre

- Los nueve diagnósticos adaptados a la nueva disposición;
  `verificar_aislamiento.ps1` completo en verde.
- README y memoria del proyecto actualizados.

---

## Registro

| Fecha | Etapa | Commit | Nota |
|---|---|---|---|
| 23/09/2026 | 0.1 | inicial | Respaldo con git; estado previo a la reforma. |
| 23/09/2026 | 0.2 | ver log | Prototipo de barra de título propia: 15/15 en los dos monitores. |
| 23/09/2026 | 2 | ver log | Barras de título y de estado en `MainForm`. `probar_barras.ps1` 25/25, verificada rompiéndola; 409 pruebas unitarias (15 nuevas, verificadas rompiéndolas). Pendiente: proteger el foco en los diagnósticos viejos (ver abajo). |

### Pendientes detectados en la Etapa 2 (no tocados)

- **Diagnósticos viejos sin protección de primer plano**: `humo_funciones`,
  `rad\disenador_en_pestanas`, `proyectos\probar_por_interfaz` y
  `rad\deshacer_en_disenador` mandan teclas sin verificar que el editor esté
  al frente. Con otras ventanas en uso fallan (y las teclas pueden ir a otra
  ventana). Solos, con el escritorio libre, pasan.
- **Cerrar la pestaña activa salta a la primera, no a la vecina** (defecto
  previo): `CloseTabAt` quita la pestaña del control antes que de
  `_documents`; el control selecciona la 0 y `OnTabChanged` fija el activo
  en 0 antes de `_documents.RemoveAt`.
- **El título siempre dice «(NASM + GoLink)»** aunque el target sea MSVC o
  MASM (texto fijo en `UpdateTitle`, previo).
- **«Sin errores» con ✓ aunque la compilación se cancele o falle sin
  diagnósticos** (el resumen ya decía «Sin errores» antes; ahora se ve más).
