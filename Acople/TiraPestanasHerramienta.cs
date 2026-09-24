namespace AsmEditor;

/// <summary>
/// Las pestañas de una zona con varios paneles (Lista de errores · Salida),
/// abajo, como las de Visual Studio. Solo se ve cuando la zona tiene más de
/// uno: eso lo decide GrupoHerramientas.
///
/// ⚠ CADA PESTAÑA ES UNA ETIQUETA DE VERDAD, no un dibujo: así las pruebas por
/// interfaz la encuentran por su texto (WM_GETTEXT) y le hacen clic. Las
/// etiquetas se crean acá y no en el diseñador porque salen de los datos (qué
/// paneles hay en la zona), como las filas de una lista.
/// </summary>
public partial class TiraPestanasHerramienta : UserControl
{
    private const int MargenTexto = 10;

    private readonly List<Label> _pestanas = new();
    private int _seleccionada = -1;
    private Font _fuente = Tema.Cuerpo;

    /// <summary>Se hizo clic en una pestaña (su índice).</summary>
    public event EventHandler<int>? PestanaElegida;

    public TiraPestanasHerramienta()
    {
        InitializeComponent();
        AplicarTema();
        Tema.TemaCambiado += AplicarTema;
        Disposed += TiraPestanasHerramienta_Disposed;
    }

    /// <summary>Muestra estas pestañas, con la indicada como elegida.</summary>
    public void Mostrar(IReadOnlyList<string> titulos, int seleccionada)
    {
        SuspendLayout();

        while (_pestanas.Count > titulos.Count)
        {
            var sobra = _pestanas[^1];
            _pestanas.RemoveAt(_pestanas.Count - 1);
            Controls.Remove(sobra);
            sobra.Dispose();
        }

        while (_pestanas.Count < titulos.Count)
        {
            var l = new Label
            {
                AutoSize = false,
                TextAlign = ContentAlignment.MiddleCenter,
                Height = Height,
                Top = 0,
                Tag = _pestanas.Count
            };
            l.MouseDown += Pestana_MouseDown;
            l.MouseEnter += Pestana_MouseEnter;
            l.MouseLeave += Pestana_MouseLeave;
            _pestanas.Add(l);
            Controls.Add(l);
        }

        int x = 0;
        for (int i = 0; i < titulos.Count; i++)
        {
            var l = _pestanas[i];
            l.Text = titulos[i];
            l.Font = _fuente;
            l.Width = TextRenderer.MeasureText(titulos[i], _fuente).Width + 2 * MargenTexto;
            l.Left = x;
            x += l.Width;
        }

        _seleccionada = seleccionada;
        Pintar();
        ResumeLayout();
    }

    private void Pestana_MouseDown(object? sender, MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left && sender is Label { Tag: int i }) PestanaElegida?.Invoke(this, i);
    }

    private void Pestana_MouseEnter(object? sender, EventArgs e)
    {
        if (sender is Label { Tag: int i } l && i != _seleccionada) l.ForeColor = Tema.Texto;
    }

    private void Pestana_MouseLeave(object? sender, EventArgs e) => Pintar();

    // ------------------------------------------------------------------
    // Aspecto
    // ------------------------------------------------------------------

    /// <summary>La elegida con el fondo del contenido y texto en acento; las demás, apagadas.</summary>
    private void Pintar()
    {
        for (int i = 0; i < _pestanas.Count; i++)
        {
            bool elegida = i == _seleccionada;
            _pestanas[i].BackColor = elegida ? Tema.Superficie : Tema.Superficie2;
            _pestanas[i].ForeColor = elegida ? Tema.Acento : Tema.Texto2;
        }
    }

    private void AplicarTema()
    {
        BackColor = Tema.Superficie2;

        // Tema.Cuerpo crea una fuente nueva en cada lectura: se guarda una y
        // se libera la anterior, o cada cambio de tema perdería un objeto GDI.
        var anterior = _fuente;
        _fuente = Tema.Cuerpo;
        foreach (var l in _pestanas) l.Font = _fuente;
        anterior.Dispose();

        Pintar();
    }

    private void TiraPestanasHerramienta_Disposed(object? sender, EventArgs e)
    {
        Tema.TemaCambiado -= AplicarTema;
        _fuente.Dispose();
    }
}
