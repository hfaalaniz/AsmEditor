# Frame PIC — reglas del proyecto

Frame PIC genera, desde una definición única de proyecto, el firmware en C para PIC (CCS y XC8) y la aplicación host de PC (C# WinForms, Web, Rust). Vive en la carpeta `FramePIC/` del repo AsmEditor.

**Fuente de verdad del plan:** `FramePIC/docs/BITACORA.md`. Leerla al empezar cada sesión. Toda decisión o avance se anota en su Registro (fecha, fase, una línea), la más reciente arriba.

## Reglas de avance

1. **No se escribe código de una fase sin su diseño cerrado y el OK de Fabián.** Si cambia el alcance o el mecanismo, se vuelve a presentar antes de escribir código.
2. **Cada etapa termina con:** compilación, pruebas, verificación y el OK de Fabián. No se arranca la siguiente sin ese OK.
3. **Las pruebas se verifican rompiéndolas** antes de darlas por buenas.
4. **Un commit por etapa o decisión cerrada**, con prefijo `FramePIC:` (ej. `FramePIC: F2 esquema de descriptor de PIC`).
5. **Datos de hardware:** nunca se inventan. Pines, memoria, periféricos y fuses se toman del datasheet y se citan en el descriptor.

## Límites del repo

- **No tocar nada fuera de `FramePIC/`** hasta que cierre la etapa 7 del `PLAN_IDE.md` de AsmEditor. Únicas excepciones ya aplicadas: la exclusión de `FramePIC\**` en `AsmEditor.csproj` y `FramePIC/target/` en `.gitignore`.
- La integración en AsmEditor (fase F6) se hace después de la etapa 7, por las interfaces existentes: `IPestanaEditor`, targets de `BuildRunner`, Lista de errores y paneles de acople.

## Estructura

```
FramePIC/
├─ Cargo.toml          workspace Rust
├─ crates/
│  ├─ framepic-core/   modelo, validación, modelo intermedio, generadores, preservación
│  ├─ framepic-cli/    ejecutable para consola y CI
│  └─ framepic-ffi/    DLL con interfaz C para AsmEditor (F6)
├─ devices/            descriptores de PIC (JSON)
├─ templates/          plantillas CCS, XC8 y hosts
├─ tests/
└─ docs/               BITACORA.md, ARQUITECTURA.md
```

## Reglas por lenguaje

**Rust (núcleo)**
- Toolchain estable. `cargo fmt` y `cargo clippy -- -D warnings` sin avisos antes de cada commit.
- Sin `unwrap()` ni `expect()` fuera de pruebas; errores con tipos propios.
- La interfaz C de `framepic-ffi` es el único punto de contacto con C#: funciones `extern "C"`, sin pánicos que crucen el límite.

**C# (integración y host generado)** — mismas reglas que AsmEditor:
- Todo formulario o control nuevo lleva su `.Designer.cs`; controles, propiedades y `Controls.Add` en `InitializeComponent()`.
- Eventos con métodos con nombre, nunca lambdas.
- Las conversiones se reescriben enteras; no conviven diseños viejos y nuevos.

**C para PIC (plantillas)**
- CCS: funciones clásicas por defecto (`setup_ccp1()`, `set_pwm1_duty()`, `setup_timer_2()`), válidas en toda la serie 5.x. Directivas nuevas solo condicionadas por versión.
- XC8: proyectos MPLAB X (.X) compilables con `prjMakefilesGenerator` + `make`.
- El 16F84 es el límite inferior: 68 B de RAM, 1K palabras, pila de 8 niveles.

## Decisiones vigentes (resumen; detalle en la bitácora)

| Tema | Decisión |
| --- | --- |
| PICs | 16F84A, 16F628A, 18F2550, 18F4550, 18F45K20 |
| 16F84 | Perfil completo: UART, PWM y timers por software |
| Compiladores | CCS 5.0 o superior; XC8 4.00 o superior vía MPLAB X 6.20 |
| Programadores | PICkit 3 (ipecmd 6.20) para el 16F84A; PICkit 4/5 para el resto |
| USB (18F2550/4550) | CDC, HID y bulk desde el inicio; bootloader HID |
| ADC | 16F628A: comparador + VREF o RC; 16F84A: MCP3202 o RC |
| Código de usuario | Preservación completa (R1–R6) |
| Núcleo / interfaz | Rust / C# WinForms (AsmEditor) |
| Hosts generados | C# WinForms, luego Web, luego Rust |

## Estilo de trabajo

- Idioma: español.
- Respuestas cortas; los recursos van al código.
- Ante una duda de alcance, preguntar antes de escribir código.
