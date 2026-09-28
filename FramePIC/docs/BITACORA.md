# Bitácora Frame PIC

Actualizada: 28/09/2026 · Responsable: Fabián

## Resumen

Frame PIC genera, desde una definición única de proyecto, el firmware en C para PIC C Compiler (CCS 5.0 o superior) y para XC8 y la aplicación host de PC (C# WinForms, Web, Rust). Los PICs objetivos iniciales son 16F84, 16F628A, 18F2550, 18F4550 y 18F45K20. El editor ASM en desarrollo se extiende para trabajar con C embebido y funciona como IDE del framework.

Estado actual: plan de desarrollo definido, sin código. El código se escribe fase por fase, una vez cerrado el diseño de cada una.

Convenciones: cada fase lleva un estado (Pendiente, En curso, Bloqueada, Cerrada). Los pendientes se marcan como tareas y se tachan al resolverse. Cada avance o decisión se anota en el Registro con su fecha.

## Fases

F1 (arquitectura) es la fase activa. F0 sigue abierta solo por el prototipo del 16F84, que se construye al inicio de F5. Cada fase se cierra solo cuando cumple su criterio de salida.

| Fase | Nombre | Estado | Objetivo | Criterio de salida |
| --- | --- | --- | --- | --- |
| F0 | Requisitos y alcance | En curso | Definir casos de uso, matriz de capacidades por PIC, lenguajes host prioritarios y versión de CCS | Requisitos cerrados y priorizados |
| F1 | Arquitectura | En curso | Definir módulos, contratos, flujo proyecto→validación→generación→compilación→grabación y modelo de plugins | Documento de arquitectura aprobado |
| F2 | Base de dispositivos | Pendiente | Esquema de descriptor de PIC y alta de los 5 chips iniciales | Descriptores validados contra datasheets |
| F3 | Modelo de proyecto y validación | Pendiente | Especificar el archivo de proyecto y las reglas de validación | Proyectos inválidos rechazados con diagnóstico claro |
| F4 | Protocolo de comunicación | Pendiente | Trama, comandos, eventos, perfiles mínimo y completo, timeouts | Especificación cerrada y verificable |
| F5 | Generador de firmware CCS | Pendiente | Plantillas por familia y zonas de código de usuario protegidas | Todos los PICs compilan sin intervención manual |
| F6 | Editor y toolchain | Pendiente | Modo C en el editor, compilación, errores navegables, vista .lst, grabación | Ciclo editar→compilar→grabar dentro del editor |
| F7 | Generadores host | Pendiente | C# WinForms, luego Web (Web Serial) y luego Rust | App generada controla el PIC real |
| F8 | Módulos funcionales | Pendiente | Temporizadores, máquina de estados, ADC, PWM, EEPROM, eventos | Proceso típico definido sin firmware manual |
| F9 | Pruebas y validación | Pendiente | Emulador de dispositivo, pruebas de generadores y en hardware | Todos los PICs pasan la batería de pruebas |
| F10 | Documentación y distribución | Pendiente | Manual, guía de extensión, plantillas de ejemplo, instalador | Paquete publicable |

## Decisiones tomadas

Las decisiones de alcance quedaron cerradas el 28/09/2026. Todas amplían el alcance inicial: no hay perfiles reducidos ni soporte diferido.

| Tema | Decisión | Implicancias de diseño | Fases afectadas |
| --- | --- | --- | --- |
| 16F84 | Perfil completo, igual que el resto de los PICs | Todo lo que el framework ofrece debe funcionar en el 16F84. Lo que falta en hardware se implementa por software: UART, PWM y temporización. El protocolo y los módulos se dimensionan tomando los 68 bytes de RAM como límite inferior | F0, F2, F4, F5, F8 |
| Compilador CCS | Cualquier versión desde la 5.0 en adelante | Detección de la versión instalada. Tabla de diferencias por versión (directivas, opciones de línea de comandos, formato de errores). Plantillas limitadas al subconjunto común o condicionadas por versión. Matriz de pruebas con varias versiones | F0, F5, F6, F9 |
| USB en 18F2550/4550 | Soporte completo desde el inicio | USB forma parte del protocolo y de los generadores desde la primera versión. Incluye configuración de reloj a 48 MHz, descriptores, VID/PID, clases de dispositivo y soporte en los tres hosts | F0, F2, F4, F5, F7 |
| Código de usuario | Preservación completa al regenerar | Ninguna regeneración puede perder código escrito a mano. Se requieren zonas protegidas, detección de cambios fuera de ellas, respaldo previo, reporte de conflictos e historial | F1, F5, F6, F7 |
| XC8 vía MPLAB X IDE | Segundo backend de firmware, diseñado completo desde ahora | CCS y XC8 son destinos de primer nivel sobre un modelo intermedio común. Para XC8 el framework genera un proyecto MPLAB X (.X) listo para abrir, y también puede compilar por línea de comandos. La matriz de pruebas cubre ambos compiladores | F0, F1, F5, F6, F9 |
| ADC en 16F628A | Por software sobre los comparadores, con dos variantes elegibles por proyecto | Variante 1: comparador + VREF interno, unos 4 bits, sin componentes externos. Variante 2: comparador + red RC medida con TMR1, más resolución, requiere R y C por canal. Entradas en RA0/AN0 (pin 17) y RA1/AN1 (pin 18) | F0, F2, F5, F8 |
| ADC en 16F84 | ADC externo o método RC sobre una entrada digital, elegible por proyecto | Sin comparadores internos: el RC mide el tiempo de carga con TMR0; el ADC externo requiere definir chip e interfaz | F0, F2, F5, F8 |

Decisiones validadas el 28/09/2026 para cerrar F0. Donde no había un valor concreto, se adoptó la propuesta indicada.

| Tema | Valor adoptado | Motivo |
| --- | --- | --- |
| Casos de uso | Catálogo F0.1 aprobado tal como está, con sus exclusiones | Validado |
| Orden de los hosts | C# WinForms, luego Web, luego Rust | Validado |
| Clases USB | CDC, HID y bulk (WinUSB), las tres | Soporte completo |
| VID/PID | VID de Microchip (0x04D8) con PID del programa de sublicencia de Microchip; PIDs de demostración solo en desarrollo | Evita comprar un VID propio |
| Bootloader | Bootloader USB HID en 18F2550/4550 | HID no requiere driver en Windows ni Linux |
| ADC externo del 16F84 | MCP3202: 12 bits, 2 canales, SPI, 5 V, DIP-8; SPI por software | Bajo consumo de pines y RAM; trabaja a 5 V como el 16F84 |
| XC8 | Versión mínima 4.00 | Desde la 4.00 todas las optimizaciones están disponibles sin licencia |
| MPLAB X IDE | 6.20 como instalación de referencia (actual: 6.35) | Última con soporte de PICkit 3, necesario para el 16F84A; funciona con XC8 4.00 |
| Sistemas operativos | Windows 10/11 64 bits para todo; Linux para hosts Web y Rust y para el toolchain XC8 | CCS solo funciona en Windows |
| .NET | .NET 10 LTS para el host WinForms | Versión LTS vigente |
| Navegadores | Chrome y Edge actuales (Chromium) | Únicos con Web Serial, WebHID y WebUSB |
| Rust | Toolchain estable; x86_64 Windows (MSVC) y x86_64 Linux (GNU) | Cubre los dos sistemas soportados |
| Hardware de pruebas | Conjunto mínimo de F0.9 | Validado |

Decisiones de arquitectura (F1):

| Tema | Decisión | Implicancias |
| --- | --- | --- |
| Núcleo del framework | Rust | Modelo de proyecto, validación, modelo intermedio, generadores, preservación R1–R6 y base de dispositivos se escriben una sola vez en Rust |
| Interfaz (IDE/editor) | C# WinForms, sobre el AsmEditor existente | El editor llama al núcleo Rust. El host generado para el usuario sigue siendo C# WinForms |
| Orden con la reforma del IDE | Frame PIC se integra en AsmEditor después de terminar la etapa 7 del PLAN_IDE | F1 a F5 avanzan sin tocar AsmEditor (núcleo Rust, generadores, protocolo). La integración en el editor (F6) espera el cierre de la etapa 7 |
| Integración núcleo ↔ interfaz | Propuesta: DLL con interfaz C (P/Invoke) para el IDE y CLI para automatización; WASM a futuro | Pendiente de confirmar |

### Base existente: AsmEditor

La interfaz de Frame PIC se construye sobre [AsmEditor](https://github.com/hfaalaniz/AsmEditor). Hoy es un editor WinForms en .NET 8 orientado a NASM, GoLink y MSVC, con una reforma estilo Visual Studio en curso según su [PLAN_IDE.md](https://github.com/hfaalaniz/AsmEditor/blob/main/PLAN_IDE.md).

| Pieza de AsmEditor | Qué aporta a Frame PIC | Qué falta agregar |
| --- | --- | --- |
| Editor sobre RichTextBox con resaltado por regex y deshacer propio | Base del modo C y del modo ASM de PIC | Resaltado de C/CCS/XC8 y ASM de PIC (MPASM/PIC-AS) |
| Pestañas con `IPestanaEditor` (código y diseñador) | Punto de extensión para nuevos tipos de documento | Pestaña de diseño del proyecto Frame PIC (pines, módulos, parámetros) |
| `BuildRunner` con targets (NASM/GoLink, MSVC) | Mecanismo de compilación desde el editor | Targets CCS (`ccsc`), XC8 (prjMakefilesGenerator + make) y programación (ipecmd, PICkit) |
| Salto a línea de error y Lista de errores | Navegación de errores | Analizadores del formato de errores de CCS (`.err`) y de XC8 |
| Proyectos `.asmproj` (JSON, rutas relativas) | Modelo de proyecto del editor | Relación con el archivo de proyecto de Frame PIC (a definir en F1) |
| Diseñador de formularios con tres archivos: fuente JSON, `.inc` regenerado y `.asm` del usuario que nunca se pisa | Mismo principio que la preservación por archivos separados de R1 | Extenderlo a zonas protegidas, fusión y reubicación (R2–R6) |
| Proyecto `Core` con 472 pruebas unitarias | Lógica separada de la interfaz y probada | Puente C# hacia el núcleo Rust |
| Sistema de acople propio (etapas 3a–3d hechas; 3e–7 pendientes) | Paneles para explorador, errores, salida y terminal | Paneles de Frame PIC: dispositivo, monitor serie/USB |

Reglas de trabajo de AsmEditor que Frame PIC adopta en su parte C#:

- Todo formulario o control nuevo lleva su `.Designer.cs`; los eventos se conectan con métodos con nombre, nunca con lambdas.
- Las conversiones se reescriben enteras; no conviven diseños viejos y nuevos.
- Cada etapa termina con compilación, pruebas unitarias, diagnóstico por interfaz y el OK de Fabián.
- Las pruebas se verifican rompéndolas antes de darlas por buenas.
- Un commit por etapa.

## Fase 0 en detalle

F0 se cierra cuando estén aprobados nueve entregables: casos de uso, capacidades por PIC, brechas cubiertas por software, presupuesto de recursos, matriz de compiladores, requisitos USB, requisitos de preservación, requisitos de host y entorno de pruebas. El 16F84 marca el límite inferior de todo el diseño.

### F0.1 · Catálogo de casos de uso

Validado el 28/09/2026. Tipos de proceso cubiertos:

- secuencias por tiempo, como etapas con duración fija;
- secuencias por eventos, disparadas por sensores, finales de carrera o pulsadores;
- control on/off con histéresis, como temperatura o nivel;
- monitoreo y registro desde la PC;
- mando manual remoto desde el host;
- recetas y parámetros persistentes en EEPROM;
- alarmas y paradas de seguridad.

Exclusiones propuestas: lazos de control cerrados por la PC con tiempo real estricto, y control de movimiento de alta velocidad.

### F0.2 · Matriz de capacidades por PIC

Matriz revisada el 28/09/2026, sin correcciones. Pila de hardware: 8 niveles en 16F84A y 16F628A, 31 niveles en los 18F. El 16F84A-04 llega a 4 MHz y el 16F84A-20 a 20 MHz. XC8 soporta los cinco chips; CCS usa PCM para la familia 14 bits y PCH para los 18F.

| PIC | CCS | Flash | RAM | EEPROM | E/S | Timers | ADC | CCP/PWM | UART | USB | Oscilador | Vdd |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 16F84A | PCM | 1K palabras | 68 B | 64 B | 13 | TMR0 | No | No | No | No | Externo, hasta 20 MHz | 2,0–5,5 V |
| 16F628A | PCM | 2K palabras | 224 B | 128 B | 16 | TMR0, TMR1, TMR2 | No; 2 comparadores (RA0–RA3) + VREF interno | 1 | USART | No | Interno 4 MHz o externo 20 MHz | 3,0–5,5 V |
| 18F2550 | PCH | 32 KB | 2048 B | 256 B | 24 | TMR0–TMR3 | 10 canales, 10 bits | 2 | EUSART | Full Speed | Externo + PLL a 48 MHz | 4,2–5,5 V |
| 18F4550 | PCH | 32 KB | 2048 B | 256 B | 35 | TMR0–TMR3 | 13 canales, 10 bits | 1 CCP + 1 ECCP | EUSART | Full Speed | Externo + PLL a 48 MHz | 4,2–5,5 V |
| 18F45K20 | PCH | 32 KB | 1536 B | 256 B | 36 | TMR0–TMR3 | 14 canales, 10 bits | 1 CCP + 1 ECCP | EUSART | No | Interno 16 MHz, PLL hasta 64 MHz | 1,8–3,6 V |

### F0.3 · Brechas de hardware cubiertas por software

El perfil completo exige ofrecer las mismas funciones en todos los chips. Donde falta el periférico, el framework genera su versión por software.

| Función | Chips sin hardware | Solución a diseñar |
| --- | --- | --- |
| UART | 16F84A | UART por software temporizada con TMR0; recepción por flanco en RB0/INT |
| PWM | 16F84A | PWM por software sobre TMR0, con resolución y frecuencia limitadas |
| Temporizadores múltiples | 16F84A | Timers virtuales multiplexados sobre un tick de TMR0 |
| ADC | 16F628A | Elegible por proyecto: comparador + VREF interno (unos 4 bits) o comparador + RC medido con TMR1. Entradas RA0/AN0 (pin 17) y RA1/AN1 (pin 18) |
| ADC | 16F84A | Elegible por proyecto: ADC externo o RC sobre una entrada digital medido con TMR0 |
| USB | 16F84A, 16F628A, 18F45K20 | Puente USB-serie externo; el host lo ve como puerto serie |

### F0.4 · Presupuesto de recursos del 16F84

Con 68 bytes de RAM y 1K palabras de flash, hay que fijar cuánto consume el framework y cuánto queda para el usuario. Los límites se definen en F0 y se miden en F9.

Borrador con todos los módulos activos: el framework ocupa unos 41 B de RAM y 650 palabras de flash, y deja 27 B y 374 palabras al usuario. Son estimaciones; se miden con el prototipo.

| Bloque | RAM (B) | Flash (palabras) |
| --- | --- | --- |
| Contexto de interrupción y temporales del compilador | 8 | incluido abajo |
| UART por software (registro, contador de bits, estado) | 4 | 90 |
| Protocolo: buffer de recepción de 4 B + estado | 8 | 150 |
| Timers virtuales (4 × 2 B + tick) | 9 | 60 |
| PWM por software (1 canal) | 2 | 40 |
| E/S con antirrebote | 3 | 50 |
| Máquina de estados | 2 | 50 |
| Parámetros en EEPROM | 2 | 30 |
| ADC (RC o MCP3202 por SPI software) | 3 | 60 |
| Inicialización y bucle principal | — | 40 |
| Biblioteca del compilador (delays, aritmética) | — | 80 |
| **Total framework** | **41 de 68** | **650 de 1024** |
| **Disponible para el usuario** | **27** | **374** |

Reglas derivadas:

- Solo se generan los módulos que el proyecto usa, así que un proyecto típico queda por debajo del total.
- Límites del framework: 60 % de la RAM y 65 % de la flash en el 16F84.
- Payload máximo de 4 B en el 16F84. El dispositivo informa su payload máximo al identificarse; el protocolo sigue siendo único y el tamaño se negocia.
- Pila de 8 niveles: la interrupción usa 1 más hasta 2 internos, y el código principal no pasa de 5.
- UART por software a 9600 baudios con cristal de 4 MHz: 104 ciclos por bit, a validar en el prototipo.

### F0.5 · Matriz de compiladores

Las plantillas CCS usan por defecto las funciones clásicas, válidas en toda la serie 5.x. XC8 se integra mediante proyectos MPLAB X que el framework genera y compila sin abrir el IDE.

**Diferencias entre versiones de CCS desde la 5.0**

| Tema | Detalle | Impacto en el generador |
| --- | --- | --- |
| Novedades de la v5 | Directivas `#use pwm()` y `#use capture()`; el archivo de proyecto pasa de `.pjt` a `.ccspjt`; historial automático de cambios ([CCS v5](https://www.ccsinfo.com/version5)) | Se genera `.ccspjt`; `#use pwm()` es opcional |
| Correcciones en `#use pwm()` | Arreglos en 5.058, 5.072, 5.085 y 5.110, algunos por familia de chip ([historial CCS](http://www.ccsinfo.com/cgi-bin/update.cgi?software=VERSIONS)) | Por defecto `setup_ccp1()`, `set_pwm1_duty()` y `setup_timer_2()`; `#use pwm()` solo en versiones posteriores a su última corrección para ese chip |
| Corrección en `#use timer` | Fallo con TMR0 corregido en 5.057 | Timers virtuales propios, sin `#use timer` |
| Corrección en `getenv()` | La información de puertos RS232 volvió a funcionar en 5.095 | No depender de `getenv()` para la UART |
| Línea de comandos | Selección de compilador con `+FM` (14 bits) y `+FH` (PIC18) y optimización con `+Yx`, estables desde la v3 ([CCS v2→v3](http://www.ccsinfo.com/pdfs/v2.to.v3.pdf?q=ccsc)) | Mismo juego de opciones para todas las versiones |
| Versiones instaladas | CCS permite tener varias versiones y elegir la activa ([manual CCS](https://www.ccsinfo.com/downloads/ccs_c_manual.pdf)) | El framework detecta y selecciona la versión; la detección exacta se define en F1 |

**Estructura de un proyecto MPLAB X (.X) y compilación sin IDE**

- Carpeta `Proyecto.X/` con `Makefile` y `nbproject/configurations.xml`, donde se guardan dispositivo, compilador y opciones. Los `nbproject/Makefile-*.mk` son derivados y se regeneran.
- `prjMakefilesGenerator` recrea los makefiles fuera del IDE a partir de `configurations.xml`. Está en `mplab_platform\bin` en Windows y acepta `-create` y `-setoptions` ([Microchip Developer Help](https://developerhelp.microchip.com/xwiki/bin/view/software-tools/ides/x/outside/make/)).
- La compilación usa el GNU Make incluido con MPLAB X: `make` produce el HEX y `make clean` limpia ([guía MPLAB X](https://onlinedocs.microchip.com/oxy/GUID-D79ACEBE-41BD-43EF-8E1B-9462847AE13E-en-US-10/GUID-04FC8E17-4BCF-40C3-B395-32488AE7926C.html)).
- Flujo del framework: generar fuentes, `Makefile`, `project.xml` y `configurations.xml`; ejecutar `prjMakefilesGenerator`; ejecutar `make`. El proyecto también se puede abrir y depurar en el IDE.

**XC8 y MPLAB X**

- XC8 4.00 (julio de 2026) eliminó las restricciones de licencia: todas las optimizaciones están disponibles sin costo ([Microchip XC8](https://www.microchip.com/en-us/tools-resources/develop/mplab-xc-compilers/xc8)). Con 4.00 como mínimo desaparece el riesgo de la edición gratuita en el 16F84.
- MPLAB X 6.20 es la última versión con soporte de PICkit 3, ICD 3 y REAL ICE; la actual es la 6.35 ([Microchip MPLAB X](https://www.microchip.com/en-us/tools-resources/develop/mplab-x-ide)).
- Verificado: XC8 4.00 funciona con MPLAB X 6.20, así que una sola instalación 6.20 cubre compilación y programación.
- Equivalencias CCS ↔ XC8 a documentar en F1: fuses, delays, UART, interrupciones, EEPROM, comparadores, VREF, PWM y USB.

### F0.6 · Requisitos USB (18F2550/4550)

- Clases soportadas: CDC (puerto serie virtual), HID y bulk genérico (WinUSB).
- Pila USB por compilador: bibliotecas de CCS y la pila de Microchip para XC8. Verificado: la pila USB de Microchip funciona con XC8 4.00.
- VID/PID: VID de Microchip (0x04D8) con PID del programa de sublicencia; PIDs de demostración solo en desarrollo.
- Reloj: cristal de 20 MHz con PLL a 48 MHz.
- Bootloader: USB HID, sin driver.
- Acceso desde cada host:
  - C#: SerialPort para CDC, biblioteca HID y WinUSB.
  - Web: Web Serial, WebHID y WebUSB en Chrome y Edge.
  - Rust: serialport, hidapi y rusb.

### F0.7 · Requisitos de preservación de código

Ninguna regeneración escribe un archivo si hay riesgo de perder código del usuario. Estos seis requisitos aplican al firmware (CCS y XC8), a los proyectos .X y a los tres hosts.

| Requisito | Mecanismo | Criterio de aceptación |
| --- | --- | --- |
| R1 · Zonas de usuario | Marcadores de comentario con un ID estable (`fpic:user id=…`) en C, C#, JS y Rust. Separación por archivos donde el lenguaje lo permite: clases parciales en C# (`.g.cs` + archivo del usuario), módulos y traits en Rust, módulos ES en JS | 100 regeneraciones seguidas sin cambios en ninguna zona |
| R2 · Detección de ediciones fuera de zona | Manifiesto con el hash de cada archivo y una copia base de la última salida generada | Toda edición fuera de zona se detecta antes de escribir |
| R3 · Respaldo previo | Instantánea comprimida del proyecto antes de cada regeneración, con retención configurable | Cualquier respaldo se restaura completo |
| R4 · Fusión y conflictos | Fusión a tres vías entre la base (última generada), el archivo actual y la nueva generación. Sin conflicto se fusiona sola; con conflicto el archivo no se escribe y el editor muestra el reporte y las diferencias | Cero sobrescrituras silenciosas |
| R5 · Historial | Registro de cada regeneración: fecha, versión del framework, archivos y hashes. Vuelta atrás a cualquier punto. Commit automático opcional si el proyecto usa git | Cualquier regeneración se revierte |
| R6 · Reubicación | Los IDs de zona no dependen de nombres. Si se renombra un elemento, la zona lo sigue; si se elimina, la zona pasa a un archivo de huérfanos y nunca se borra. En `configurations.xml`, los ajustes del usuario se fusionan a nivel XML | Renombrar o eliminar elementos no pierde código |

### F0.8 · Requisitos de host

- Sistemas operativos: Windows 10/11 de 64 bits para todo el framework. Linux para los hosts Web y Rust y para el toolchain XC8/MPLAB X. CCS solo funciona en Windows.
- .NET 10 LTS para el host WinForms.
- Chrome y Edge actuales para el host Web (Web Serial, WebHID, WebUSB).
- Rust estable, con destinos x86_64 Windows (MSVC) y x86_64 Linux (GNU).
- Orden de desarrollo: C# WinForms, luego Web, luego Rust.

### F0.9 · Entorno de pruebas

- Una placa por PIC:
  - 16F84A-20 con cristal de 4 MHz,
  - 16F628A con oscilador interno y opción de cristal,
  - 18F2550 y 18F4550 con cristal de 20 MHz (PLL a 48 MHz) y conector USB,
  - 18F45K20 alimentado a 3,3 V.
- Programador: PICkit 4 y PICkit 5 no soportan el 16F84A; sí programan el 16F628A y los 18F. Para el 16F84A se usa PICkit 3 con MPLAB X/IPE 6.20 (línea de comandos con ipecmd) o, como alternativa, PICkit 2 con pk2cmd. PICkit 4/5 solo para los chips que soportan. Programador de pruebas del 16F84A: PICkit 3.
- Adaptadores USB-serie de 5 V y 3,3 V.
- MCP3202 y componentes RC para las variantes de ADC.
- CCS: la versión 5.x más antigua disponible y la más reciente. XC8 4.00 o superior.

### Entregables de F0

- [x] Catálogo de casos de uso aprobado
- [x] Matriz de capacidades verificada
- [x] Tabla de brechas y soluciones por software
- [x] Presupuesto de recursos del 16F84 (borrador; se mide con el prototipo)
- [x] Matriz de compiladores y versiones (las equivalencias CCS ↔ XC8 pasan a F1)
- [x] Especificación de requisitos USB
- [x] Requisitos de preservación R1–R6 aprobados
- [x] Requisitos de host
- [x] Inventario del entorno de pruebas
- [ ] Prototipo mínimo en el 16F84 para medir RAM y flash con CCS y XC8

## Pendientes y decisiones abiertas

Los pendientes de F0 bloquean el inicio de F1; el resto se resuelve al entrar en su fase.

### F0 · Requisitos y alcance

- [x] Definir el catálogo de casos de uso — resuelto: validado
- [x] Armar la matriz de capacidades por PIC — resuelto: revisada
- [x] Priorizar lenguajes host — resuelto: C# WinForms, Web, Rust
- [x] Fijar la versión de CCS soportada — resuelto: 5.0 en adelante
- [x] Decidir el nivel de soporte del 16F84 — resuelto: perfil completo
- [x] Solución de ADC: 16F628A con comparador + VREF o + RC; 16F84A con ADC externo o RC (F0.3)
- [x] Chip del ADC externo del 16F84A: MCP3202 por SPI software (F0.3)
- [x] Presupuesto de RAM y flash del 16F84: borrador aprobado (F0.4)
- [x] Diferencias entre versiones de CCS desde la 5.0 (F0.5)
- [x] Versiones mínimas: MPLAB X 6.20 y XC8 4.00 (F0.5)
- [x] Estructura de proyectos .X y compilación con prjMakefilesGenerator + make (F0.5)
- [x] Riesgo de XC8 gratuito: eliminado con XC8 4.00 (F0.5)
- [x] Clases USB: CDC, HID y bulk (F0.6)
- [x] VID/PID por sublicencia de Microchip y bootloader USB HID (F0.6)
- [x] Requisitos de preservación R1–R6 (F0.7)
- [x] Sistemas operativos, .NET 10, navegadores y plataformas Rust (F0.8)
- [x] Inventario del entorno de pruebas (F0.9)
- [x] XC8 4.00 funciona con MPLAB X 6.20 (F0.5)
- [x] PICkit 4 y 5 no soportan el 16F84A (sí el 16F628A): se usa PICkit 3 o PICkit 2 (F0.9)
- [x] La pila USB de Microchip funciona con XC8 4.00 (F0.6)
- [x] Programador del 16F84A en las pruebas: PICkit 3 (F0.9)
- [x] 16F628A con PICkit 4/5: sí lo programan (F0.9)
- [ ] Construir el prototipo mínimo en el 16F84 y medir RAM y flash con CCS y XC8 (F0.4)
- [ ] Documentar las equivalencias CCS ↔ XC8 (pasa a F1)
- [ ] Diseñar la capa de programación con varios programadores (pasa a F1 y F6)

### F1 · Arquitectura

- [x] Decidir si el core se escribe en C# o en Rust — resuelto: núcleo Rust, interfaz C# WinForms
- [x] Definir cómo se incorpora el editor ASM — resuelto: la interfaz es el propio AsmEditor en WinForms
- [x] Coordinar con la reforma del IDE — resuelto: se integra después de la etapa 7
- [ ] Confirmar la vía de integración núcleo Rust ↔ WinForms: DLL con interfaz C, CLI o ambas
- [ ] Elegir el formato del archivo de proyecto (JSON, XML, otro)
- [ ] Decidir la relación entre `.asmproj` y el archivo de proyecto de Frame PIC
- [ ] Elegir el motor de plantillas para los generadores
- [ ] Definir el modelo de plugins para dispositivos y generadores
- [ ] Definir la estrategia de versionado del proyecto y del protocolo
- [ ] Diseñar el modelo intermedio independiente del compilador (CCS y XC8)
- [ ] Diseñar el mecanismo de preservación: zonas, detección, respaldo, fusión, historial
- [ ] Diseñar la detección de versión y la capa de compatibilidad de CCS
- [ ] Migrar AsmEditor de .NET 8 a .NET 10 LTS (.NET 8 deja de tener soporte el 10/11/2026)

### F2 · Base de dispositivos

- [ ] Diseñar el esquema del descriptor de PIC
- [ ] Cargar y validar los 5 descriptores contra sus datasheets
- [ ] Escribir la guía para agregar nuevos chips

### F3 · Modelo de proyecto

- [ ] Definir qué puede declarar el usuario (E/S, periféricos, parámetros, comunicación)
- [ ] Redactar el catálogo de reglas de validación y sus mensajes

### F4 · Protocolo

- [x] USB en 18F2550/4550: soporte completo desde el inicio
- [ ] Definir el formato de trama y la detección de errores
- [ ] Definir comandos base y eventos
- [ ] Dimensionar el protocolo para los 68 bytes de RAM del 16F84, con un único perfil para todos
- [ ] Definir el transporte sobre UART por hardware, UART por software, CDC, HID y bulk
- [ ] Definir el comportamiento ante timeouts y pérdida de conexión

### F5 · Generador de firmware

- [x] Soporte XC8: se diseña completo desde ahora, junto con CCS
- [ ] Definir la estructura de archivos generados, común a CCS y XC8
- [ ] Diseñar los backends CCS y XC8 sobre el modelo intermedio
- [ ] Implementar la preservación completa (R1–R6) en los generadores

### F6 · Editor y toolchain

- [ ] Revisar el estado actual del editor ASM y qué reutilizar
- [ ] Definir el modo C y el resaltado de directivas CCS
- [ ] Confirmar las opciones de línea de comandos de ccsc.exe y el formato de errores
- [ ] Elegir grabadores soportados (PICkit, IPE u otros)
- [ ] Agregar a `BuildRunner` los targets CCS, XC8 (prjMakefilesGenerator + make) y programación (ipecmd para PICkit 3/4/5)
- [ ] Agregar analizadores del formato de errores de CCS y XC8 para el salto a línea y la Lista de errores
- [ ] Agregar resaltado de C para PIC y ASM de PIC (MPASM/PIC-AS)

### F7 a F10

- [ ] Definir la UI base generada para el host
- [ ] Listar los módulos funcionales de F8 por prioridad
- [ ] Definir el alcance del emulador de dispositivo
- [ ] Definir el formato de distribución (instalador, paquete)

## Riesgos

El mayor riesgo es sostener un perfil completo en el 16F84 compilando con CCS y con XC8.

| Riesgo | Impacto | Mitigación prevista | Fase |
| --- | --- | --- | --- |
| Perfil completo en el 16F84: 68 B de RAM, 1K palabras, solo TMR0, sin UART | El framework puede no caber o dejar poco espacio al usuario | Presupuesto de F0.4 (41 B y 650 palabras con todos los módulos), generación solo de módulos usados y medición con prototipo | F0, F4, F5, F9 |
| ~~XC8 gratuito optimiza menos~~ | Eliminado | XC8 4.00 libera todas las optimizaciones; es la versión mínima | Cerrado |
| Diferencias entre versiones de CCS desde la 5.0 | Código generado que falla en alguna versión | Funciones clásicas por defecto, directivas nuevas condicionadas por versión y matriz de pruebas con la 5.x más antigua y la más reciente | F0, F5, F6, F9 |
| PICkit 4 y 5 no programan el 16F84A | Hace falta un programador antiguo para ese chip | MPLAB X/IPE 6.20 como referencia; PICkit 3 vía ipecmd para el 16F84A y PICkit 4/5 para el resto | F0, F6 |
| USB completo desde el inicio (CDC, HID, bulk) en dos compiladores | Mayor carga en F4, F5 y F7 | Probar cada clase por compilador y por host; bootloader HID sin driver | F0, F4, F5, F7 |
| Preservación completa del código de usuario | Complejidad en la fusión y la reubicación de zonas | Requisitos R1–R6 con criterios de aceptación; diseño dedicado en F1; se parte del patrón de tres archivos del diseñador de AsmEditor | F0, F1, F5 |
| Dos compiladores de primer nivel (CCS y XC8) | Duplica plantillas y pruebas | Modelo intermedio común y backends delgados | F1, F5, F9 |
| 18F45K20 trabaja a 1,8–3,6 V | Incompatibilidad con periféricos y adaptadores de 5 V | Documentar niveles lógicos y usar un adaptador USB-serie de 3,3 V | F0, F9 |
| AsmEditor en .NET 8, que deja de tener soporte el 10/11/2026 | Sin parches de seguridad para la interfaz | Migrar a .NET 10 LTS antes de integrar Frame PIC | F1, F6 |
| Reforma del IDE de AsmEditor en curso (etapas 3e–7) | Cambios simultáneos en `MainForm` y el acople | Integrar Frame PIC recién al cerrar la etapa 7; hasta entonces F1–F5 avanzan fuera de AsmEditor. Luego, integrar por las interfaces existentes (`IPestanaEditor`, targets, paneles) | F1, F6 |
| Editor sobre RichTextBox con resaltado por regex | Rendimiento limitado en archivos grandes, como los `.lst` | Medir con archivos reales en F6; evaluar alternativas solo si hace falta | F6 |

## Registro

Las entradas van de la más reciente a la más antigua.

| Fecha | Fase | Entrada |
| --- | --- | --- |
| 2026-09-28 | F1 | Se agregan `FramePIC/CLAUDE.md` (reglas del proyecto) y `docs/ARQUITECTURA.md` con el paso F1.1: mapa de componentes y flujo, pendiente de OK |
| 2026-09-28 | F1 | Frame PIC se desarrolla en la carpeta `FramePIC/` del repo AsmEditor, desde VS Code; el editor no se toca hasta cerrar la etapa 7. La bitácora pasa a `FramePIC/docs/BITACORA.md` |
| 2026-09-28 | F1 | Decisión: Frame PIC se integra en AsmEditor después de la etapa 7 del PLAN_IDE. F1–F5 avanzan en paralelo sin tocar el editor |
| 2026-09-28 | F1 | La interfaz pasa de WPF a C# WinForms, sobre el AsmEditor existente. Se releva el repositorio y su PLAN_IDE; se adoptan sus reglas de trabajo para la parte C# |
| 2026-09-28 | F1 | Arranca F1. Decisión: núcleo en Rust. Integración propuesta: DLL con interfaz C + CLI |
| 2026-09-28 | F0 | Programador de pruebas del 16F84A: PICkit 3 con MPLAB X/IPE 6.20. F0 queda abierta solo por el prototipo; F1 lista para arrancar |
| 2026-09-28 | F0 | PICkit 4/5 sí programan el 16F628A. Solo el 16F84A requiere programador antiguo |
| 2026-09-28 | F0 | Verificaciones cerradas: XC8 4.00 funciona con MPLAB X 6.20; la pila USB de Microchip funciona con XC8 4.00; PICkit 4/5 no soportan el 16F84A |
| 2026-09-28 | F0 | Validados casos de uso, orden de hosts, USB (CDC, HID, bulk), VID/PID, bootloader HID, MCP3202, MPLAB X 6.20+, XC8 4.00+, SO, .NET 10, navegadores e inventario. Desarrollados F0.4, F0.5 y F0.7 |
| 2026-09-28 | F0 | ADC resuelto: 16F628A por comparadores (VREF o RC, elegible); 16F84A con ADC externo o RC. XC8 se integra vía MPLAB X IDE con generación de proyectos .X |
| 2026-09-28 | F0 | Se detalla F0 en nueve actividades (F0.1 a F0.9) con matriz de capacidades, brechas por software y requisitos de USB, preservación y host |
| 2026-09-28 | F0 | Decisiones cerradas: 16F84 con perfil completo, CCS 5.0+, USB completo desde el inicio, preservación completa, XC8 diseñado desde ahora |
| 2026-09-28 | — | Se crea la bitácora con fases, pendientes y riesgos |
| 2026-09-28 | — | Se define el plan de desarrollo en 11 fases (F0 a F10); el código queda para cada fase |
| 2026-09-28 | — | Inicio del proyecto Frame PIC: firmware CCS + host C#/Web/Rust desde una definición única |

## Fuentes

- [MPLAB XC8 Compiler — Microchip](https://www.microchip.com/en-us/tools-resources/develop/mplab-xc-compilers/xc8)
- [MPLAB X IDE — Microchip](https://www.microchip.com/en-us/tools-resources/develop/mplab-x-ide)
- [MPLAB X IDE v6.25 y herramientas legacy — Microchip Blog](https://www.microchip.com/en-us/about/media-center/blog/2024/discontinued-ide-support-for-gen3-tools)
- [Creating Makefiles Outside of MPLAB X IDE — Microchip Developer Help](https://developerhelp.microchip.com/xwiki/bin/view/software-tools/ides/x/outside/make/)
- [Building a Project Outside of MPLAB X IDE — Microchip Online Docs](https://onlinedocs.microchip.com/oxy/GUID-D79ACEBE-41BD-43EF-8E1B-9462847AE13E-en-US-10/GUID-04FC8E17-4BCF-40C3-B395-32488AE7926C.html)
- [New Features in Version 5 — CCS](https://www.ccsinfo.com/version5)
- [Recent Changes — CCS](http://www.ccsinfo.com/cgi-bin/update.cgi?software=VERSIONS)
- [CCS C Compiler Manual — CCS](https://www.ccsinfo.com/downloads/ccs_c_manual.pdf)
- [AsmEditor — repositorio](https://github.com/hfaalaniz/AsmEditor)
- [AsmEditor — PLAN_IDE.md](https://github.com/hfaalaniz/AsmEditor/blob/main/PLAN_IDE.md)
