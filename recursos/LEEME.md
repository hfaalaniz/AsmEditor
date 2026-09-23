# Recursos de marca del Editor ASM

## Qué hay acá

| Archivo | Qué es |
|---|---|
| `logo.svg` | **La fuente de diseño.** El dibujo completo, en grilla 32×32. |
| `logo-chico.svg` | La variante sin patas ni bits, para menos de 32 px. |
| `AsmEditor.ico` | Ícono multi-tamaño que Windows usa para el `.exe`. **Generado.** |
| `logo-32/128/256.png` | Para documentación y web. **Generados.** |

## Cómo se usa cada uno

**El programa NO carga ninguno de estos archivos para dibujar en pantalla.**
Lo que ejecuta es el código GDI+ de `LogoEditor.cs`, que dibuja el mismo logo
con las mismas coordenadas.

⚠ **WinForms no dibuja SVG**: no tiene soporte nativo, y agregar una biblioteca
que lo rasterice (Svg.NET, SkiaSharp) serían varios MB de dependencia para un
dibujo de cuarenta líneas. Por eso el SVG es fuente de diseño y no recurso de
ejecución.

El reparto queda así:

- **SVG** → editar el diseño, documentar, exportar.
- **`.ico`** → Windows, embebido en el `.exe` vía `<ApplicationIcon>` del
  `.csproj`. Es lo que hace que el Explorador muestre el ícono **sin que el
  programa corra**; un ícono generado en memoria no da eso.
- **GDI+** (`LogoEditor.cs`) → el splash, la pantalla de bienvenida y la ventana,
  donde el logo tiene que seguir el tema activo y escalar a cualquier tamaño.

## ⚠ Los tres tienen que coincidir a mano

Son dos archivos SVG y un archivo C#, en tres formatos distintos. **No hay forma
de que uno lea al otro.** Si se toca el dibujo hay que tocar los tres.

Para facilitarlo, cada pieza del SVG lleva anotadas al lado las coordenadas que
usa el código, y `LogoEditor.cs` está ordenado en las mismas cuatro secciones:

1. patas del chip
2. cuerpo del chip
3. la «A»
4. los bits

## Regenerar el `.ico` y los PNG

Los archivos generados salen del propio código, así que no hace falta Inkscape
ni ImageMagick. Desde la carpeta del proyecto:

```powershell
dotnet run --project <carpeta temporal del generador>
```

El generador está en `diagnostico/` como referencia. También se puede llamar
directamente a `LogoEditor.CrearIcono()`, que arma el `.ico` con los tamaños
16, 20, 32, 48, 64, 128 y 256.

⚠ **`Icon.FromHandle` NO sirve para esto**: devuelve un ícono de un solo tamaño
y Windows lo escala para los demás usos, que se ve borroso en Alt+Tab y en la
vista de iconos grandes. Por eso `CrearIcono` escribe el encabezado `.ico` a
mano con las siete imágenes adentro.

## El diseño

Un ensamblador traduce texto a bytes para un procesador. De ahí las tres piezas:

- **el chip** — cuadrado con patas; es la silueta que se reconoce a 16 px;
- **la «A»** de Assembler, con ángulos duros: grabada, no escrita;
- **los bits** 1-1-0 debajo: lo que sale del ensamblado.

No es `</>` porque ese chevron es el símbolo genérico de «programación» —lo usan
editores, terminales, cursos y medio mundo— y no identifica a nada.

## Colores

| Token | Valor | Para qué |
|---|---|---|
| acento | `#D4A017` | El dorado de Trapezoide, que identifica estos sistemas. |
| trazo | `#9AA3B0` | Gris neutro, legible sobre fondo claro y oscuro. |

El ícono del sistema usa siempre estos dos colores fijos, no los del tema: la
barra de tareas puede estar clara u oscura y el ícono es el mismo archivo.
