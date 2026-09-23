namespace PruebaAcople;

/// <summary>
/// Etapa 0.4 (PLAN_IDE.md): el acople medido SIN RATÓN. La geometría de las
/// guías, y sacar y volver a acoplar paneles por código sobre un formulario
/// real (se muestra unos segundos pero no se toca: no manda teclas ni clics).
///
/// Lo que depende del arrastre de verdad (la ventana flotante siguiendo al
/// ratón, las guías apareciendo encima) lo mide probar_acople.ps1, con el
/// escritorio libre.
/// </summary>
public static class PruebaSinVentana
{
    private static int _fallas;

    private static void Bien(string t) { Console.ForegroundColor = ConsoleColor.Green; Console.WriteLine("  OK   " + t); Console.ResetColor(); }
    private static void Mal(string t) { Console.ForegroundColor = ConsoleColor.Red; Console.WriteLine("  MAL  " + t); Console.ResetColor(); _fallas++; }
    private static void Titulo(string t) { Console.WriteLine(); Console.ForegroundColor = ConsoleColor.Cyan; Console.WriteLine("=== " + t + " ==="); Console.ResetColor(); }

    public static int Correr()
    {
        Geometria();
        Modelo();

        Console.WriteLine();
        if (_fallas == 0) { Console.ForegroundColor = ConsoleColor.Green; Console.WriteLine("=== TODO BIEN: geometría y modelo del acople ==="); Console.ResetColor(); return 0; }
        Console.ForegroundColor = ConsoleColor.Red; Console.WriteLine($"=== {_fallas} FALLAS ==="); Console.ResetColor();
        return 1;
    }

    private static void Geometria()
    {
        Titulo("1. Geometría de las guías");
        var h = new Rectangle(100, 50, 1000, 700);

        foreach (var z in Enum.GetValues<Zona>())
        {
            var g = GeometriaAcople.Guia(z, h);
            var centro = new Point(g.X + g.Width / 2, g.Y + g.Height / 2);
            if (GeometriaAcople.ZonaEn(centro, h) == z) Bien($"el centro de la guía {z} da {z}");
            else Mal($"el centro de la guía {z} da {GeometriaAcople.ZonaEn(centro, h)}");
        }

        var c = GeometriaAcople.Centro(h);
        if (GeometriaAcople.ZonaEn(new Point(c.X + c.Width / 2, c.Y + c.Height / 2), h) is null) Bien("el cuadro central no acopla");
        else Mal("el cuadro central acopla");

        if (GeometriaAcople.ZonaEn(new Point(h.X + 5, h.Y + 5), h) is null) Bien("una esquina lejos de las guías no acopla");
        else Mal("una esquina acopla");

        var zonas = Enum.GetValues<Zona>().Select(z => GeometriaAcople.Guia(z, h)).ToList();
        bool solapan = zonas.Any(a => zonas.Any(b => a != b && a.IntersectsWith(b))) || zonas.Any(a => a.IntersectsWith(c));
        if (!solapan) Bien("las guías no se pisan entre sí ni con el centro"); else Mal("hay guías que se pisan");

        var izq = GeometriaAcople.Destino(Zona.Izquierda, h);
        var der = GeometriaAcople.Destino(Zona.Derecha, h);
        var aba = GeometriaAcople.Destino(Zona.Abajo, h);
        if (izq.Left == h.Left && izq.Height == h.Height && izq.Width == 250) Bien("destino izquierda: pegado a la izquierda, alto completo, 25%");
        else Mal($"destino izquierda: {izq}");
        if (der.Right == h.Right && der.Width == 250) Bien("destino derecha: pegado a la derecha, 25%"); else Mal($"destino derecha: {der}");
        if (aba.Bottom == h.Bottom && aba.Width == h.Width && aba.Height == 210) Bien("destino abajo: pegado abajo, ancho completo, 30%");
        else Mal($"destino abajo: {aba}");
    }

    private static void Modelo()
    {
        Titulo("2. Sacar y volver a acoplar, por código");

        using var f = new FormPrueba { StartPosition = FormStartPosition.Manual, Location = new Point(-2000, -2000), ShowInTaskbar = false };
        f.Show();          // fuera de la pantalla: nadie la ve ni la toca
        Application.DoEvents();

        var a = f.Anfitrion;

        if (a.ZonaDe(f.Explorador) == Zona.Derecha && a.ZonaDe(f.Salida) == Zona.Abajo && a.ZonaDe(f.Errores) == Zona.Izquierda)
            Bien("al cargar, cada panel quedó en su zona");
        else Mal($"al cargar: explorador={a.ZonaDe(f.Explorador)} salida={a.ZonaDe(f.Salida)} errores={a.ZonaDe(f.Errores)}");

        if (!a.Acoplar(f.Salida, Zona.Derecha)) Bien("no deja acoplar en una zona ocupada");
        else Mal("acopló en una zona ocupada");

        var flot = a.Desacoplar(f.Errores, new Point(-1500, -1500));
        Application.DoEvents();

        if (a.ZonaDe(f.Errores) is null && flot.Panel == f.Errores && a.Flotantes.Count == 1) Bien("sacado: el panel está en una ventana flotante");
        else Mal("no quedó flotando");

        if (!a.ZonaOcupada(Zona.Izquierda)) Bien("la zona izquierda quedó libre (y oculta)"); else Mal("la zona izquierda sigue ocupada");

        // Soltar sobre la guía izquierda: se simula lo que hace el arrastre.
        var guia = GeometriaAcople.Guia(Zona.Izquierda, a.RectangleToScreen(a.ClientRectangle));
        var punto = new Point(guia.X + guia.Width / 2, guia.Y + guia.Height / 2);

        if (a.ZonaBajoGuia(punto) == Zona.Izquierda) Bien("el punto sobre la guía izquierda la detecta (zona libre)");
        else Mal("la guía izquierda no se detecta");

        var guiaDer = GeometriaAcople.Guia(Zona.Derecha, a.RectangleToScreen(a.ClientRectangle));
        if (a.ZonaBajoGuia(new Point(guiaDer.X + 5, guiaDer.Y + 5)) is null) Bien("la guía derecha no se ofrece (zona ocupada)");
        else Mal("se ofrece la guía de una zona ocupada");

        var v = flot.Soltar();
        a.Acoplar(v!, Zona.Izquierda);
        flot.Close();
        Application.DoEvents();

        if (a.ZonaDe(f.Errores) == Zona.Izquierda && a.Flotantes.Count == 0) Bien("vuelto a acoplar a la izquierda; la flotante se cerró");
        else Mal("no volvió a la zona izquierda");

        // Doble clic en la flotante: vuelve a su última zona.
        var flot2 = a.Desacoplar(f.Salida, new Point(-1500, -1500));
        Application.DoEvents();
        var metodo = typeof(AnfitrionAcople).GetMethod("Flotante_VolverPedido",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        metodo!.Invoke(a, new object[] { flot2 });
        Application.DoEvents();

        if (a.ZonaDe(f.Salida) == Zona.Abajo) Bien("«volver» la devolvió a su última zona (abajo)"); else Mal("«volver» no la devolvió");
    }
}
