namespace PruebaAcople;

/// <summary>Dónde puede ir un panel acoplado.</summary>
public enum Zona
{
    Izquierda,
    Derecha,
    Abajo
}

/// <summary>
/// La geometría del acople, sin ventanas: dónde se dibujan las guías, cuál
/// está bajo el ratón y qué rectángulo ocuparía cada zona (la vista previa).
///
/// Vive aparte para probarla sin interfaz ni ratón (PruebaSinVentana).
///
/// Las guías son como las de Visual Studio: un grupo en el centro del
/// anfitrión, una por zona, alrededor de un cuadro central.
/// </summary>
public static class GeometriaAcople
{
    /// <summary>Lado de cada guía, en píxeles.</summary>
    public const int LadoGuia = 36;

    /// <summary>Separación entre la guía y el cuadro central.</summary>
    public const int Separacion = 4;

    /// <summary>Qué parte del anfitrión ocupa una zona nueva (proporción).</summary>
    public const double ProporcionLateral = 0.25;
    public const double ProporcionAbajo = 0.30;

    /// <summary>El rectángulo de la guía de una zona, en las coordenadas de <paramref name="anfitrion"/>.</summary>
    public static Rectangle Guia(Zona zona, Rectangle anfitrion)
    {
        int cx = anfitrion.Left + anfitrion.Width / 2 - LadoGuia / 2;
        int cy = anfitrion.Top + anfitrion.Height / 2 - LadoGuia / 2;
        int paso = LadoGuia + Separacion;

        return zona switch
        {
            Zona.Izquierda => new Rectangle(cx - paso, cy, LadoGuia, LadoGuia),
            Zona.Derecha => new Rectangle(cx + paso, cy, LadoGuia, LadoGuia),
            Zona.Abajo => new Rectangle(cx, cy + paso, LadoGuia, LadoGuia),
            _ => Rectangle.Empty
        };
    }

    /// <summary>El cuadro central (no acopla: marca el centro del grupo).</summary>
    public static Rectangle Centro(Rectangle anfitrion) => new(
        anfitrion.Left + anfitrion.Width / 2 - LadoGuia / 2,
        anfitrion.Top + anfitrion.Height / 2 - LadoGuia / 2,
        LadoGuia, LadoGuia);

    /// <summary>La zona cuya guía está bajo el punto, o null si no hay ninguna.</summary>
    public static Zona? ZonaEn(Point punto, Rectangle anfitrion)
    {
        foreach (var z in Enum.GetValues<Zona>())
        {
            if (Guia(z, anfitrion).Contains(punto)) return z;
        }
        return null;
    }

    /// <summary>
    /// El rectángulo que ocuparía un panel acoplado en esa zona: es la vista
    /// previa que se muestra mientras el ratón está sobre la guía.
    /// </summary>
    public static Rectangle Destino(Zona zona, Rectangle anfitrion)
    {
        int ancho = (int)(anfitrion.Width * ProporcionLateral);
        int alto = (int)(anfitrion.Height * ProporcionAbajo);

        return zona switch
        {
            Zona.Izquierda => new Rectangle(anfitrion.Left, anfitrion.Top, ancho, anfitrion.Height),
            Zona.Derecha => new Rectangle(anfitrion.Right - ancho, anfitrion.Top, ancho, anfitrion.Height),
            Zona.Abajo => new Rectangle(anfitrion.Left, anfitrion.Bottom - alto, anfitrion.Width, alto),
            _ => Rectangle.Empty
        };
    }
}
