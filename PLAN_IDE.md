# Plan: IDE estilo Visual Studio

Aprobado por Fabián el 23/09/2026. Este archivo es la referencia para ejecutar
la reforma **por etapas** y para retomarla entre sesiones: cada etapa se marca
al terminarla, con la fecha y el commit.

> **Regla de avance:** cada etapa termina con compilación, pruebas unitarias,
> su diagnóstico por interfaz (con capturas) y **el OK de Fabián**. No se
> arranca la siguiente sin ese OK. Si en el camino cambia el alcance o el
> mecanismo, se vuelve a presentar antes de escribir código.

---

## ▶ PRÓXIMOS PASOS, EN ORDEN (al cierre del 23/09/2026)

Hecho: 0.1, 0.2, 0.3, 0.4 y la Etapa 2 (barras). Lo que sigue:

1. ✅ *(hecho el 23/09, al retomar: se tomaron las recomendadas; la
   protección está en `diagnostico\proteccion_interfaz.ps1`, verificada con
   `verificar_proteccion.ps1`, y los avisos muestran solo el NOMBRE del
   programa, nunca el título de una ventana ajena)*.
   **Confirmar las decisiones con un «sí» (Fabián)**. Si no dice otra cosa,
   se toman las recomendadas:
   - Terminal con **ConPTY** → recomendado: sí.
   - **`Dock` en el anfitrión de acople** (única excepción a la regla del
     diseñador) → recomendado: sí.
   - **Proteger los diagnósticos viejos** (`humo_funciones`,
     `rad\disenador_en_pestanas`, `proyectos\probar_por_interfaz`,
     `rad\deshacer_en_disenador`): no mandar teclas sin el editor al frente
     ni hacer clic en ventanas ajenas → recomendado: sí, antes de la Etapa 1.
2. ✅ **Etapa 1 · Ventana de inicio** *(23/09; ver su sección y el registro)*.
   «Al iniciar» se elige en la nueva ventana **Configuración → Opciones**.
3. **Etapa 3 · Acople real y reescritura de `MainForm`** (3a → 3g).
4. **Etapa 4 · Explorador estilo VS.**
5. **Etapa 5 · Barra de navegación.**
6. **Etapa 6 · Terminal integrada** (sobre el prototipo 0.3).
7. **Etapa 7 · Cierre.**

Aparte, para cuando Fabián quiera: probar a mano el teclado de la terminal
(`diagnostico\prototipos\terminal\...\PruebaTerminal.exe`) y el acople
(`diagnostico\prototipos\acople\...\PruebaAcople.exe`), y decidir si se
arreglan los defectos previos anotados al final de este archivo.

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

**Cómo quedó (23/09/2026):**

- `Program.cs`: splash → (si no vino un archivo por línea de comandos)
  según `Ui.AlIniciar`: ventana de inicio / último proyecto / nada →
  `Application.Run(MainForm)`. Cerrar la ventana de inicio (X o Esc) cierra
  el editor, como VS.
- `MainForm` ya **no reabre solo** el proyecto anterior ni la lista global
  `Ui.OpenFiles` (queda en el JSON, sin uso). La elección se guarda con
  `PrepararInicio` y se aplica en `Shown` (`AplicarInicio`): así el diálogo de
  «Crear un proyecto» sale con el IDE visible como dueño.
- **Sesión por proyecto** (`Ui.SesionesProyectos`): archivos abiertos y
  pestaña activa. Se guarda al salir, al cambiar de proyecto y al cerrarlo;
  se restaura al abrir el proyecto desde la ventana de inicio o con «último
  proyecto».
- «Último proyecto» = el que estaba **abierto al salir** (`Ui.ProyectoAbierto`),
  como hacía el editor antes. Si se salió sin proyecto, arranca vacío. (VS
  usa, en cambio, el primero de los recientes.)
- «Abrir una carpeta»: el explorador muestra esa carpeta solo en esta sesión.
- «Al iniciar» se elige en **Configuración → Opciones → Entorno → General**
  (ver «Ventana Opciones» abajo).

### Ventana Opciones (23/09/2026, pedido de Fabián)

**Configuración → Opciones...** reemplaza a «Rutas de herramientas...»
(`SettingsForm`, hecho en código, se reescribió con diseñador y se borró).
Estilo VS: árbol de categorías a la izquierda, página a la derecha.

| Página | Opciones |
|---|---|
| Entorno → General | Tema, Al iniciar |
| Entorno → Proyectos | Carpeta del proyecto (de trabajo) |
| Herramientas → NASM y GoLink | Rutas de nasm.exe y GoLink.exe |
| Herramientas → MSVC | Toolchain 64/32 y SDK 64/32 (lógica de antes, sin cambios) |

- **Agregar una página:** un `UserControl` con `Designer.cs` que implemente
  `IPaginaOpciones`, arrastrado a `pnlPaginas` en el diseñador. El árbol se
  arma solo (`Categoria`, `Titulo`, `Orden`); los colores los pone
  `EstiloOpciones` por tipo de control (etiquetas `lblNota*` en gris).
- **Agregar una opción:** su propiedad en `Core\ValoresOpciones.cs` + su
  línea en `Leer` y en `Aplicar` + el control en su página. Si falta una de
  las dos líneas, `ValoresOpcionesTests.TodaPropiedad_VaYVuelve` falla
  (reflexión; verificado agregando una opción sin conectar).
- ⚠ Se edita una **copia** (`ValoresOpciones`), no `BuildConfig.Clone`: el
  Clone comparte `Ui`, y Cancelar habría cambiado igual el tema y «Al iniciar».
- **Ver → Tema** sigue como atajo del mismo valor.
- Defecto previo corregido al reescribir: guardar «Rutas de herramientas» con
  un proyecto abierto le cambiaba el explorador a la vista de carpeta.
- Pruebas: `ValoresOpcionesTests` (5), `diagnostico\opciones.ps1` (por
  interfaz), `romper_valores_opciones.ps1`, `romper_opciones.ps1`.

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

⚠ **Cambio de orden (23/09, con OK de Fabián):** el acople se monta **en el
editor desde 3a**, no en un banco aparte hasta 3g. Cada subetapa suma su
función sobre el editor real; 3g queda para menú, barra, pie y pasar
`MainForm` a `Designer.cs`. 3a y 3b van juntas: «Errores» y «Salida» ya
compartían la zona de abajo, y sin pestañas una quedaba inaccesible.

- [x] **3a · Zonas fijas** *(23/09)*: `Acople\AnfitrionAcople` (zonas
  izquierda / derecha / abajo con `Splitter`, la de abajo ENTRE las laterales)
  reemplaza a los dos `SplitContainer` de `MainForm`. `VentanaHerramienta`:
  barra con nombre, ▾ (menú: Ocultar), ✕ (la chincheta está, oculta, hasta
  3c); se resalta con el foco. Ver → Explorador (Ctrl+B) / Lista de errores /
  Salida, con marca. Modelo: `Core\Acople\DisenoAcople` (18 pruebas,
  verificadas con `romper_diseno_acople.ps1`, 8 defectos).
  ⚠ Ocultar un panel con el foco adentro dejaba el foco en la ✕ estacionada
  fuera de la ventana y **se morían todos los atajos**; el anfitrión lo
  devuelve al centro, con `BeginInvoke` y `Focus()` explícito (medido con
  `foco_al_ocultar.ps1`, dentro del proceso).
  El explorador pasó a la **derecha**: se ajustaron `disenador_en_pestanas`
  y `deshacer_en_disenador` (buscaban la paleta con «x > 240») y
  `probar_por_interfaz` (recorría 6 niveles de ventanas; ahora 12).
- [x] **3b · Pestañas por zona** *(23/09)*: `GrupoHerramientas` +
  `TiraPestanasHerramienta` (pestañas abajo, solo con más de un panel; son
  etiquetas reales para que las pruebas las lean). Lista de errores · Salida.
  Prueba por interfaz: `acople.ps1`, verificada con `romper_acople.ps1`.
- [x] **3c · Auto-ocultar (chincheta)** *(28/09)*: la chincheta (o «Ocultar
  automáticamente» del ▾) repliega el panel a una pestaña en la franja del
  borde de su lado (`BordeAutoOcultos` + `PestanaBorde`, vertical en los
  costados). Ratón encima 400 ms → se despliega encima de todo
  (`pnlDesplegado`), sin foco; clic → al instante y con foco. Se pliega
  (`tmrPlegar`, 300 ms) solo si el ratón no está ni sobre el panel ni sobre
  su pestaña, el foco no está adentro y el menú ▾ está cerrado. La chincheta
  del desplegado lo vuelve a acoplar.
  ⚠ Decisión de Fabián: **Ctrl+B / Ver sobre un auto-oculto NO lo cierra**
  (como VS): lo despliega con foco, y si ya está desplegado lo pliega.
  `Mostrar` de un auto-oculto también lo despliega con foco.
  ⚠ `DisenoAcople.Activar` ignora los auto-ocultos: darle el foco al
  desplegado pisaba el activo de su zona.
  Los dos `romper_*` quedaron desactualizados por la reforma del modelo
  del 23/09 (un texto a romper ya no existía y otro rompía OTRO método):
  ahora exigen que cada texto aparezca **exactamente una vez**.
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
| 23/09/2026 | — | ver log | Diagnósticos viejos protegidos (`proteccion_interfaz.ps1`). |
| 23/09/2026 | 1 | ver log | Ventana de inicio. 426 pruebas unitarias (17 nuevas de `RecientesInicio`, verificadas rompiéndolas). `ventana_inicio.ps1` 8 casos por interfaz, verificada con `romper_ventana_inicio.ps1` (6 defectos). |
| 23/09/2026 | 1 | ver log | Ventana Configuración → Opciones (reemplaza a «Rutas de herramientas»). 431 pruebas unitarias (5 nuevas, verificadas con `romper_valores_opciones.ps1`, incluida una opción nueva sin conectar). `opciones.ps1` por interfaz, verificada con `romper_opciones.ps1` (6 defectos). `ventana_inicio` y `probar_barras` en verde. |
| 23/09/2026 | 3a+3b | ver log | Acople en el editor: explorador a la derecha, Lista de errores · Salida abajo con pestañas, ✕ / Ver / Ctrl+B, divisores. 449 pruebas unitarias (18 del modelo, verificadas con `romper_diseno_acople.ps1`, 8 defectos). `acople.ps1` por interfaz, verificada con `romper_acople.ps1` (7 defectos). Corregido: el foco quedaba en la ✕ oculta y morían los atajos. Los 8 diagnósticos por interfaz en verde. |
| 28/09/2026 | 3c | ver log | Auto-ocultar (chincheta). 459 pruebas unitarias; `romper_diseno_acople.ps1` 17/17 (5 no se detectaban: 4 pruebas corregidas o nuevas y 1 defecto equivalente, documentado). `acople.ps1` secciones F (explorador) y G (abajo), con capturas `acople_desplegado.png` y `acople_abajo_desplegado.png`; `romper_acople.ps1` 17/17 (7 viejos + 10 de la 3c; nuevo `-Solo` para repetir uno). El resguardo de foco de `Plegar` no es medible (WinForms ya devuelve el foco en el único camino que llega con foco): queda, documentado en `romper_acople.ps1`. |

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
