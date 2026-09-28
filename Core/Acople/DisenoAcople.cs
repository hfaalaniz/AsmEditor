namespace AsmEditor.Core.Acople;

/// <summary>Dónde puede ir un panel acoplado. El centro es de los documentos.</summary>
public enum ZonaAcople
{
    Izquierda = 0,
    Derecha = 1,
    Abajo = 2
}

/// <summary>Dónde está un panel y si se ve.</summary>
public sealed class UbicacionPanel
{
    public ZonaAcople Zona { get; set; }

    /// <summary>False = oculto (se cerró con ✕ o desde Ver); conserva su zona para volver.</summary>
    public bool Visible { get; set; } = true;

    /// <summary>
    /// True = auto-oculto (la chincheta): no ocupa lugar en su zona; queda
    /// como una pestaña en el borde de ese lado y se despliega encima de los
    /// documentos. Se conserva al ocultarlo: vuelve auto-oculto.
    /// </summary>
    public bool AutoOculto { get; set; }

    /// <summary>Posición dentro de la zona: el orden de sus pestañas.</summary>
    public int Orden { get; set; }
}

/// <summary>
/// El diseño de los paneles acoplables (explorador, lista de errores,
/// salida...): en qué zona está cada uno, si se ve, cuál es el activo de cada
/// zona y cuánto mide cada zona. Es el MODELO: la interfaz
/// (AnfitrionAcople) solo lo dibuja y le avisa los cambios.
///
/// Vive en Core y sin WinForms para probarlo sin ventanas, y para guardarlo en
/// settings.json tal cual (Etapa 3f de PLAN_IDE.md).
///
/// Los paneles se identifican por un Id de texto fijo ("explorador",
/// "errores"): así un diseño guardado sobrevive a que cambie el título o el
/// orden en que el editor los registra.
/// </summary>
public sealed class DisenoAcople
{
    /// <summary>Lo mínimo que puede medir una zona (ancho o alto, en píxeles).</summary>
    public const int TamanoMinimo = 120;

    public Dictionary<string, UbicacionPanel> Paneles { get; set; } = new();

    /// <summary>Ancho de las zonas laterales y alto de la de abajo. Sin valor = el de fábrica.</summary>
    public Dictionary<ZonaAcople, int> Tamanos { get; set; } = new();

    /// <summary>La pestaña activa de cada zona.</summary>
    public Dictionary<ZonaAcople, string> Activos { get; set; } = new();

    /// <summary>
    /// Agrega un panel en su zona de fábrica. Si ya estaba (un diseño
    /// guardado), NO lo toca: manda lo que el usuario dejó.
    /// </summary>
    public void Registrar(string id, ZonaAcople zona, bool visible = true)
    {
        if (Paneles.ContainsKey(id)) return;

        Paneles[id] = new UbicacionPanel
        {
            Zona = zona,
            Visible = visible,
            Orden = SiguienteOrden(zona)
        };
    }

    /// <summary>
    /// Los paneles ACOPLADOS y visibles de la zona, en el orden de sus
    /// pestañas. Los auto-ocultos no están acá: ver <see cref="AutoOcultosEn"/>.
    /// </summary>
    public IReadOnlyList<string> VisiblesEn(ZonaAcople zona) =>
        EnZona(zona, autoOcultos: false);

    /// <summary>Los auto-ocultos (visibles) de ese lado: las pestañas de su borde.</summary>
    public IReadOnlyList<string> AutoOcultosEn(ZonaAcople zona) =>
        EnZona(zona, autoOcultos: true);

    private List<string> EnZona(ZonaAcople zona, bool autoOcultos) =>
        Paneles
            .Where(p => p.Value.Zona == zona && p.Value.Visible && p.Value.AutoOculto == autoOcultos)
            .OrderBy(p => p.Value.Orden)
            .ThenBy(p => p.Key, StringComparer.Ordinal)
            .Select(p => p.Key)
            .ToList();

    /// <summary>Una zona sin paneles visibles no ocupa lugar.</summary>
    public bool ZonaVisible(ZonaAcople zona) => VisiblesEn(zona).Count > 0;

    /// <summary>
    /// El panel activo de la zona: el anotado si sigue visible ahí; si no, el
    /// primero visible. Null si la zona está vacía.
    /// </summary>
    public string? ActivoEn(ZonaAcople zona)
    {
        var visibles = VisiblesEn(zona);
        if (visibles.Count == 0) return null;

        return Activos.TryGetValue(zona, out var id) && visibles.Contains(id) ? id : visibles[0];
    }

    /// <summary>Visible = acoplado o auto-oculto (con su pestaña en el borde); no cerrado.</summary>
    public bool EstaVisible(string id) => Paneles.TryGetValue(id, out var u) && u.Visible;

    public bool EstaAutoOculto(string id) => Paneles.TryGetValue(id, out var u) && u.AutoOculto;

    public ZonaAcople? ZonaDe(string id) => Paneles.TryGetValue(id, out var u) ? u.Zona : null;

    /// <summary>
    /// Lo deja como la pestaña activa de su zona (sin cambiar si se ve).
    ///
    /// ⚠ Un auto-oculto NO: no está en la zona. Si no, al darle el foco
    /// desplegado desde el borde se perdería cuál era el activo de verdad.
    /// </summary>
    public void Activar(string id)
    {
        if (Paneles.TryGetValue(id, out var u) && !u.AutoOculto) Activos[u.Zona] = id;
    }

    /// <summary>
    /// Lo muestra en su lugar: acoplado (y activo en su zona) o, si estaba
    /// auto-oculto, de nuevo con su pestaña en el borde.
    /// </summary>
    public void Mostrar(string id)
    {
        if (!Paneles.TryGetValue(id, out var u)) return;
        u.Visible = true;
        if (!u.AutoOculto) Activos[u.Zona] = id;
    }

    /// <summary>
    /// Lo oculta. Conserva la zona, el orden y si estaba auto-oculto: al
    /// mostrarlo vuelve a su lugar. Si era el activo, la zona pasa a la
    /// pestaña vecina (ver ActivoEn).
    /// </summary>
    public void Ocultar(string id)
    {
        if (!Paneles.TryGetValue(id, out var u)) return;

        if (u.Visible && !u.AutoOculto) SoltarActivo(u.Zona, id);
        u.Visible = false;
    }

    /// <summary>
    /// La chincheta, al acoplado: lo repliega a una pestaña en el borde de su
    /// lado. Deja de ocupar lugar en la zona (la vecina pasa a ser la activa).
    /// </summary>
    public void AutoOcultar(string id)
    {
        if (!Paneles.TryGetValue(id, out var u) || u.AutoOculto) return;

        if (u.Visible) SoltarActivo(u.Zona, id);
        u.AutoOculto = true;
        u.Visible = true;
    }

    /// <summary>La chincheta, al auto-oculto: lo vuelve a acoplar en su zona, activo.</summary>
    public void Fijar(string id)
    {
        if (!Paneles.TryGetValue(id, out var u)) return;

        u.AutoOculto = false;
        u.Visible = true;
        Activos[u.Zona] = id;
    }

    /// <summary>
    /// Lo pasa a otra zona, como última pestaña, ACOPLADO (aunque estuviera
    /// auto-oculto), visible y activo.
    /// </summary>
    public void Mover(string id, ZonaAcople zona)
    {
        if (!Paneles.TryGetValue(id, out var u)) return;

        if (u.Zona != zona)
        {
            Ocultar(id);
            u.Zona = zona;
            u.Orden = SiguienteOrden(zona);
        }

        u.AutoOculto = false;
        Mostrar(id);
    }

    /// <summary>
    /// Un panel acoplado deja la zona (se oculta o se repliega): si era el
    /// activo, pasa a serlo la vecina — la siguiente, o la anterior si era la
    /// última, como en VS.
    /// </summary>
    private void SoltarActivo(ZonaAcople zona, string id)
    {
        if (!Activos.TryGetValue(zona, out var activo) || activo != id) return;

        var visibles = VisiblesEn(zona).ToList();
        int i = visibles.IndexOf(id);
        if (i < 0) { Activos.Remove(zona); return; }

        visibles.RemoveAt(i);
        if (visibles.Count == 0) Activos.Remove(zona);
        else Activos[zona] = visibles[Math.Min(i, visibles.Count - 1)];
    }

    /// <summary>El tamaño de la zona: el guardado, o el de fábrica.</summary>
    public int TamanoDe(ZonaAcople zona, int porDefecto) =>
        Tamanos.TryGetValue(zona, out var t) ? t : Math.Max(TamanoMinimo, porDefecto);

    public void FijarTamano(ZonaAcople zona, int pixeles) =>
        Tamanos[zona] = Math.Max(TamanoMinimo, pixeles);

    /// <summary>
    /// Deja el diseño consistente después de leerlo del disco: sin nulos,
    /// zonas válidas, tamaños dentro del mínimo, órdenes 0..n-1 sin huecos y
    /// activos que existan.
    /// </summary>
    public void EnsureValid()
    {
        Paneles ??= new Dictionary<string, UbicacionPanel>();
        Tamanos ??= new Dictionary<ZonaAcople, int>();
        Activos ??= new Dictionary<ZonaAcople, string>();

        foreach (var k in Paneles.Where(p => p.Value is null || string.IsNullOrWhiteSpace(p.Key)).Select(p => p.Key).ToList())
            Paneles.Remove(k);

        foreach (var u in Paneles.Values)
        {
            if (!Enum.IsDefined(u.Zona)) u.Zona = ZonaAcople.Derecha;
        }

        foreach (var z in Enum.GetValues<ZonaAcople>())
        {
            int n = 0;
            foreach (var id in Paneles.Where(p => p.Value.Zona == z).OrderBy(p => p.Value.Orden).ThenBy(p => p.Key, StringComparer.Ordinal).Select(p => p.Key).ToList())
                Paneles[id].Orden = n++;
        }

        foreach (var z in Tamanos.Keys.ToList())
        {
            if (!Enum.IsDefined(z)) Tamanos.Remove(z);
            else if (Tamanos[z] < TamanoMinimo) Tamanos[z] = TamanoMinimo;
        }

        foreach (var z in Activos.Keys.ToList())
        {
            if (!Enum.IsDefined(z) || !Paneles.TryGetValue(Activos[z], out var u) || u.Zona != z)
                Activos.Remove(z);
        }
    }

    private int SiguienteOrden(ZonaAcople zona) =>
        Paneles.Values.Where(p => p.Zona == zona).Select(p => p.Orden + 1).DefaultIfEmpty(0).Max();
}
