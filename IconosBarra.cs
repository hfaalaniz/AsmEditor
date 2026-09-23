using System.Drawing.Drawing2D;

namespace AsmEditor;

/// <summary>Los íconos de la barra de herramientas.</summary>
public enum Glifo
{
    Nuevo,
    Abrir,
    Guardar,
    GuardarTodo,
    Deshacer,
    Rehacer,
    Buscar,
    Reemplazar,
    Compilar,
    Enlazar,
    Construir,
    Ejecutar,
    Detener,
    Configurar,
    Explorador,
    Tema
}

/// <summary>
/// LOS ÍCONOS DE LA BARRA, DIBUJADOS CON GDI+.
///
/// ⚠ NO SON ARCHIVOS .png, y es a propósito, por lo mismo que el logo del editor:
///   - no hay recursos que empaquetar ni rutas que se rompan;
///   - se dibujan en cualquier tamaño sin pixelarse;
///   - toman el color del tema activo, así que al pasar a claro no quedan
///     íconos claros sobre fondo claro.
///
/// El costo es que hay que dibujarlos a mano, así que son formas simples: lo que
/// se lee a 18 px es el contorno, no el detalle.
/// </summary>
public static class IconosBarra
{
    /// <summary>
    /// Dibuja un glifo en un mapa de bits del lado pedido. Quien lo recibe se
    /// ocupa de liberarlo (los ToolStripButton lo hacen al cambiar de imagen).
    /// </summary>
    public static Bitmap Dibujar(Glifo glifo, int lado)
    {
        var bmp = new Bitmap(lado, lado);

        using var g = Graphics.FromImage(bmp);
        g.Clear(Color.Transparent);
        g.SmoothingMode = SmoothingMode.AntiAlias;

        var color = ColorDe(glifo);
        float e = lado / 18f;   // los dibujos están pensados en 18×18

        PointF P(float x, float y) => new(x * e, y * e);

        using var pen = new Pen(color, 1.6f * e)
        {
            StartCap = LineCap.Round,
            EndCap = LineCap.Round,
            LineJoin = LineJoin.Round
        };
        using var brush = new SolidBrush(color);

        switch (glifo)
        {
            case Glifo.Nuevo:
                // Hoja con la esquina doblada
                g.DrawLines(pen, new[] { P(4, 2), P(10, 2), P(14, 6), P(14, 16), P(4, 16), P(4, 2) });
                g.DrawLines(pen, new[] { P(10, 2), P(10, 6), P(14, 6) });
                break;

            case Glifo.Abrir:
                // Carpeta abierta
                g.DrawLines(pen, new[] { P(2, 14), P(2, 4), P(7, 4), P(9, 6), P(15, 6) });
                g.DrawLines(pen, new[] { P(2, 14), P(5, 8), P(17, 8), P(14, 14), P(2, 14) });
                break;

            case Glifo.Guardar:
                // Disquete
                g.DrawLines(pen, new[] { P(3, 3), P(13, 3), P(15, 5), P(15, 15), P(3, 15), P(3, 3) });
                g.DrawRectangle(pen, 6 * e, 3 * e, 6 * e, 4 * e);
                g.DrawRectangle(pen, 5 * e, 10 * e, 8 * e, 5 * e);
                break;

            case Glifo.GuardarTodo:
                // Dos disquetes superpuestos
                g.DrawLines(pen, new[] { P(2, 2), P(10, 2), P(12, 4), P(12, 12), P(2, 12), P(2, 2) });
                g.DrawLines(pen, new[] { P(6, 6), P(14, 6), P(16, 8), P(16, 16), P(6, 16), P(6, 6) });
                g.DrawRectangle(pen, 8 * e, 11 * e, 6 * e, 5 * e);
                break;

            case Glifo.Deshacer:
                // Flecha curva a la izquierda
                g.DrawArc(pen, 3 * e, 5 * e, 12 * e, 10 * e, 180, 160);
                g.DrawLines(pen, new[] { P(3, 10), P(3, 5), P(8, 5) });
                break;

            case Glifo.Rehacer:
                // La misma, espejada
                g.DrawArc(pen, 3 * e, 5 * e, 12 * e, 10 * e, 200, 160);
                g.DrawLines(pen, new[] { P(15, 10), P(15, 5), P(10, 5) });
                break;

            case Glifo.Buscar:
                // Lupa
                g.DrawEllipse(pen, 3 * e, 3 * e, 9 * e, 9 * e);
                g.DrawLine(pen, P(11, 11), P(15, 15));
                break;

            case Glifo.Reemplazar:
                // Lupa con flecha de cambio
                g.DrawEllipse(pen, 2 * e, 2 * e, 8 * e, 8 * e);
                g.DrawLine(pen, P(9, 9), P(12, 12));
                g.DrawLines(pen, new[] { P(9, 15), P(16, 15) });
                g.DrawLines(pen, new[] { P(14, 13), P(16, 15), P(14, 17) });
                break;

            case Glifo.Compilar:
                // Engranaje simplificado: un círculo con dientes
                g.DrawEllipse(pen, 5 * e, 5 * e, 8 * e, 8 * e);
                for (int i = 0; i < 4; i++)
                {
                    double a = i * Math.PI / 2;
                    float cx = 9f + (float)Math.Cos(a) * 7f;
                    float cy = 9f + (float)Math.Sin(a) * 7f;
                    float dx = 9f + (float)Math.Cos(a) * 4.5f;
                    float dy = 9f + (float)Math.Sin(a) * 4.5f;
                    g.DrawLine(pen, P(dx, dy), P(cx, cy));
                }
                break;

            case Glifo.Enlazar:
                // Dos eslabones de cadena
                g.DrawArc(pen, 1 * e, 6 * e, 9 * e, 7 * e, 90, 180);
                g.DrawArc(pen, 8 * e, 6 * e, 9 * e, 7 * e, 270, 180);
                g.DrawLine(pen, P(6, 9), P(12, 9));
                break;

            case Glifo.Construir:
                // Martillo
                g.DrawLines(pen, new[] { P(3, 15), P(10, 8) });
                g.DrawLines(pen, new[] { P(8, 3), P(15, 3), P(15, 7), P(8, 7), P(8, 3) });
                g.DrawLine(pen, P(10, 7), P(10, 9));
                break;

            case Glifo.Ejecutar:
                // Triángulo de reproducción, relleno
                g.FillPolygon(brush, new[] { P(5, 3), P(15, 9), P(5, 15) });
                break;

            case Glifo.Detener:
                // Cuadrado relleno
                g.FillRectangle(brush, 5 * e, 5 * e, 8 * e, 8 * e);
                break;

            case Glifo.Configurar:
                // Llave inglesa / deslizadores
                g.DrawLine(pen, P(2, 5), P(16, 5));
                g.DrawLine(pen, P(2, 13), P(16, 13));
                g.FillEllipse(brush, 5 * e, 2.5f * e, 5 * e, 5 * e);
                g.FillEllipse(brush, 9 * e, 10.5f * e, 5 * e, 5 * e);
                break;

            case Glifo.Explorador:
                // Panel lateral: marco con una franja a la izquierda
                g.DrawRectangle(pen, 2 * e, 3 * e, 14 * e, 12 * e);
                g.FillRectangle(brush, 2 * e, 3 * e, 5 * e, 12 * e);
                break;

            case Glifo.Tema:
                // Medio sol, medio luna: el círculo partido
                g.DrawEllipse(pen, 3 * e, 3 * e, 12 * e, 12 * e);
                using (var path = new GraphicsPath())
                {
                    path.AddArc(3 * e, 3 * e, 12 * e, 12 * e, 90, 180);
                    path.CloseFigure();
                    g.FillPath(brush, path);
                }
                break;
        }

        return bmp;
    }

    /// <summary>
    /// El color de cada glifo. Los de compilación llevan su propio color —verde
    /// para ejecutar, rojo para detener— porque son los que se buscan con
    /// urgencia; el resto va en el color de texto para no convertir la barra en
    /// un semáforo.
    /// </summary>
    private static Color ColorDe(Glifo glifo) => glifo switch
    {
        Glifo.Ejecutar => Tema.Ok,
        Glifo.Detener => Tema.Critico,
        Glifo.Construir => Tema.Acento,
        Glifo.Tema => Tema.Acento,
        _ => Tema.Texto2
    };
}
