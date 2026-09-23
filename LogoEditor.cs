using System.Drawing.Drawing2D;

namespace AsmEditor;

/// <summary>
/// EL LOGO DEL EDITOR: un chip con una «A» de Assembler y los bits que salen.
///
/// ── POR QUÉ NO ES «&lt;/&gt;» ─────────────────────────────────────────────
/// El chevron de código es el símbolo genérico de «esto es programación»: lo
/// usan editores, terminales, cursos y la mitad de los logos de software. No
/// identifica a nada. Este dibujo, en cambio, dice de qué se trata ESTE
/// programa: un ensamblador traduce texto a bytes para un procesador.
///
/// ── LAS TRES PIEZAS ──────────────────────────────────────────────────────
///   1. EL CHIP — cuadrado con patas a los cuatro lados. Es la silueta que se
///      reconoce a 16 px, cuando ya no se distingue nada de adentro.
///   2. LA «A» — trazada con ángulos rectos y diagonales duras, no con la
///      curva de una tipografía: se lee como algo grabado, no escrito.
///   3. LOS BITS — tres cuadraditos debajo, dos llenos y uno vacío: el 1-1-0
///      que sale del ensamblado. Es el detalle que aparece en los tamaños
///      grandes y desaparece sin hacer falta en los chicos.
///
/// ── ESCALA ───────────────────────────────────────────────────────────────
/// Todo se dibuja sobre una grilla de 32×32 y se escala con un factor, así que
/// el mismo código sirve para el ícono de 16 px de la barra de tareas y para
/// la imagen de 160 px del splash. No hay .png de ningún tamaño.
/// </summary>
public static class LogoEditor
{
    /// <summary>
    /// Dibuja el logo en un cuadrado de <paramref name="lado"/> píxeles.
    ///
    /// <paramref name="detalle"/> en falso omite las patas y los bits: es lo que
    /// conviene por debajo de ~20 px, donde esos trazos se convierten en una
    /// mancha y ensucian la silueta en vez de agregar información.
    /// </summary>
    public static void Dibujar(Graphics g, float x, float y, float lado,
                               Color acento, Color trazo, bool detalle = true)
    {
        float e = lado / 32f;
        var modoAnterior = g.SmoothingMode;
        g.SmoothingMode = SmoothingMode.AntiAlias;

        PointF P(float px, float py) => new(x + px * e, y + py * e);
        RectangleF R(float px, float py, float w, float h) =>
            new(x + px * e, y + py * e, w * e, h * e);

        // ── 1. Las patas del chip ────────────────────────────────────────
        // Van primero para que el cuerpo las tape donde se juntan.
        if (detalle)
        {
            using var penPata = new Pen(trazo, 1.6f * e) { StartCap = LineCap.Round, EndCap = LineCap.Round };

            foreach (float p in new[] { 11f, 16f, 21f })
            {
                g.DrawLine(penPata, P(p, 3f), P(p, 6.5f));    // arriba
                g.DrawLine(penPata, P(p, 25.5f), P(p, 29f));  // abajo
                g.DrawLine(penPata, P(3f, p), P(6.5f, p));    // izquierda
                g.DrawLine(penPata, P(25.5f, p), P(29f, p));  // derecha
            }
        }

        // ── 2. El cuerpo del chip ────────────────────────────────────────
        var cuerpo = R(6f, 6f, 20f, 20f);
        using (var path = Redondeado(cuerpo, 3f * e))
        {
            using var relleno = new SolidBrush(Color.FromArgb(28, acento));
            g.FillPath(relleno, path);

            using var borde = new Pen(acento, 2f * e) { LineJoin = LineJoin.Round };
            g.DrawPath(borde, path);
        }

        // ── 3. La «A» de Assembler ───────────────────────────────────────
        // Trazos rectos y ángulo duro en el vértice: grabada, no escrita.
        //
        // ⚠ NO OCUPA TODO EL CHIP. Con la A llegando a los bordes el dibujo se
        // veía como una letra dentro de un marco; dejándole aire alrededor se
        // lee como lo que hay ADENTRO del chip, que es la idea.
        using (var penA = new Pen(acento, detalle ? 2.2f * e : 2.8f * e)
        {
            StartCap = LineCap.Round,
            EndCap = LineCap.Round,
            LineJoin = LineJoin.Miter
        })
        {
            if (detalle)
            {
                // Con bits abajo, la A se apoya más arriba para dejarles lugar.
                g.DrawLines(penA, new[] { P(12.2f, 20f), P(16f, 11.2f), P(19.8f, 20f) });
                g.DrawLine(penA, P(13.7f, 16.6f), P(18.3f, 16.6f));
            }
            else
            {
                // Sin bits, la A se centra y engorda: a 16 px es lo único que
                // se va a distinguir, así que tiene que ser clara.
                g.DrawLines(penA, new[] { P(11.8f, 21.5f), P(16f, 10.5f), P(20.2f, 21.5f) });
                g.DrawLine(penA, P(13.6f, 17.4f), P(18.4f, 17.4f));
            }
        }

        // ── 4. Los bits que salen ────────────────────────────────────────
        if (detalle)
        {
            const float bitY = 22.3f;
            const float bitLado = 2.0f;
            const float paso = 3.4f;
            float x0 = 16f - (paso * 2 + bitLado) / 2f;   // centrados bajo la A

            using var lleno = new SolidBrush(trazo);
            using var vacio = new Pen(trazo, 1.0f * e);

            // 1 - 1 - 0: lo que sale del ensamblado.
            g.FillRectangle(lleno, R(x0, bitY, bitLado, bitLado));
            g.FillRectangle(lleno, R(x0 + paso, bitY, bitLado, bitLado));

            var hueco = R(x0 + paso * 2, bitY, bitLado, bitLado);
            g.DrawRectangle(vacio, hueco.X, hueco.Y, hueco.Width, hueco.Height);
        }

        g.SmoothingMode = modoAnterior;
    }

    /// <summary>Rectángulo de esquinas redondeadas.</summary>
    private static GraphicsPath Redondeado(RectangleF r, float radio)
    {
        var p = new GraphicsPath();
        float d = Math.Min(radio * 2, Math.Min(r.Width, r.Height));
        if (d <= 0) { p.AddRectangle(r); return p; }

        p.AddArc(r.X, r.Y, d, d, 180, 90);
        p.AddArc(r.Right - d, r.Y, d, d, 270, 90);
        p.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
        p.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
        p.CloseFigure();
        return p;
    }

    /// <summary>
    /// Ícono de la aplicación. Se arma con varios tamaños en un solo .ico para
    /// que Windows elija el que corresponda: la barra de tareas usa uno chico y
    /// Alt+Tab uno grande, y dejar que escale el de 32 se ve borroso.
    ///
    /// ⚠ Por debajo de 20 px se dibuja sin patas ni bits: ahí esos trazos son
    /// una mancha y la silueta se lee peor.
    /// </summary>
    public static Icon CrearIcono()
    {
        int[] tamanos = { 16, 20, 32, 48, 64, 128, 256 };
        var imagenes = new List<byte[]>();

        foreach (int lado in tamanos)
        {
            using var bmp = RenderizarPng(lado);
            using var ms = new MemoryStream();
            bmp.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
            imagenes.Add(ms.ToArray());
        }

        return EnsamblarIco(imagenes, tamanos);
    }

    /// <summary>Un mapa de bits del logo, para el splash y las vistas grandes.</summary>
    public static Bitmap RenderizarPng(int lado)
    {
        var bmp = new Bitmap(lado, lado);
        using var g = Graphics.FromImage(bmp);
        g.Clear(Color.Transparent);

        // El ícono del sistema no sigue el tema del editor: se dibuja siempre
        // con el dorado de la marca sobre un trazo claro, que es lo que se ve
        // bien tanto en la barra de tareas clara como en la oscura.
        // ⚠ EL UMBRAL DE DETALLE SALIÓ DE MIRARLO, no de estimarlo: a 24 px los
        // bits de 2 px quedan en una mancha de un píxel y medio, y las patas se
        // funden con el borde. Desde 32 se distinguen.
        Dibujar(g, 0, 0, lado,
                acento: Color.FromArgb(0xD4, 0xA0, 0x17),
                trazo: Color.FromArgb(0x9A, 0xA3, 0xB0),
                detalle: lado >= 32);

        return bmp;
    }

    /// <summary>
    /// Arma un .ico en memoria con varias imágenes PNG dentro.
    ///
    /// ⚠ SE ESCRIBE A MANO EL ENCABEZADO porque .NET no tiene forma de crear un
    /// ícono multi-tamaño: Icon.FromHandle devuelve uno solo del tamaño dado, y
    /// Windows lo escala para los demás usos, que es justo lo que se ve mal.
    /// El formato son 6 bytes de cabecera, 16 por cada entrada del directorio, y
    /// después las imágenes.
    /// </summary>
    private static Icon EnsamblarIco(List<byte[]> imagenes, int[] tamanos)
    {
        using var ms = new MemoryStream();
        using var w = new BinaryWriter(ms);

        w.Write((short)0);                  // reservado
        w.Write((short)1);                  // tipo: 1 = ícono
        w.Write((short)imagenes.Count);

        int offset = 6 + 16 * imagenes.Count;

        for (int i = 0; i < imagenes.Count; i++)
        {
            int lado = tamanos[i];
            // 256 se codifica como 0 en este campo de un byte.
            w.Write((byte)(lado >= 256 ? 0 : lado));
            w.Write((byte)(lado >= 256 ? 0 : lado));
            w.Write((byte)0);               // colores de la paleta
            w.Write((byte)0);               // reservado
            w.Write((short)1);              // planos
            w.Write((short)32);             // bits por píxel
            w.Write(imagenes[i].Length);
            w.Write(offset);

            offset += imagenes[i].Length;
        }

        foreach (var img in imagenes) w.Write(img);

        w.Flush();
        ms.Position = 0;
        return new Icon(ms);
    }
}
