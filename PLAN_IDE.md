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

### Etapa 0 · Preparación y pruebas de concepto — *en curso*

El editor no cambia. Se prueba aparte lo que puede salir caro.
Los prototipos viven en `diagnostico\prototipos\` (ya excluido del `.csproj`).

- [x] **0.1 Respaldo:** `git init`, `.gitignore`, `.gitattributes` (sin
  conversión de fin de línea) y commit inicial. *(23/09/2026)*
- [ ] **0.2 Barra de título propia:** ventana sin borde de Windows con barra
  dibujada; interceptando `WM_NCHITTEST` para que arrastre, bordes, Aero Snap
  y maximizado los siga haciendo Windows. Verificar en los dos monitores
  (1366×768 y 1600×900): arrastre, redimensión, Snap, maximizar sin tapar la
  barra de tareas, doble clic, Alt+Espacio, paso entre monitores.
- [ ] **0.3 Terminal:** prueba mínima con **ConPTY** (la consola real de
  Windows) contra la alternativa por redirección. Se decide cuál se usa.
- [ ] **0.4 Acople propio:** prueba mínima de las dos partes difíciles:
  **sacar un panel a una ventana flotante** arrastrando su título, y
  **volver a acoplarlo con guías** y vista previa.
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

### Etapa 2 · Barra de título personalizada

- `BarraTitulo` + `.Designer.cs`: ícono, **menú integrado**, buscador de
  comandos del menú, etiqueta con el nombre del proyecto, minimizar /
  maximizar / cerrar.
- Mecanismo validado en 0.2. `Form.Text` se sigue actualizando (lo usan la
  barra de tareas y los diagnósticos).
- Diagnóstico: arrastre, maximizar/restaurar, Snap, cambio de monitor,
  capturas en los dos temas.

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
