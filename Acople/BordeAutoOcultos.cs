using System.ComponentModel;
using AsmEditor.Core.Acople;

namespace AsmEditor;

/// <summary>
/// La franja de un borde del acople con las pestañas de los paneles
/// auto-ocultos de ese lado (3c del plan), como en Visual Studio. Solo se ve
/// si hay alguno: eso lo decide el AnfitrionAcople.
///
/// No despliega nada: avisa (ratón encima, ratón afuera, clic) y el anfitrión
/// decide. Las pestañas (<see cref="PestanaBorde"/>) se crean acá y no en el
/// diseñador porque salen de los datos, como las filas de una lista.
/// </summary>
public partial class BordeAutoOcultos : UserControl
{
    private const int Separacion = 4;

    private readonly List<PestanaBorde> _pestanas = new();
    private Font _fuente = Tema.Cuerpo;

    /// <summary>El ratón entró a la pestaña del panel (su Id).</summary>
    public event EventHandler<string>? PestanaSenalada;

    /// <summary>El ratón salió de la pestaña del panel.</summary>
    public event EventHandler<string>? PestanaDejada;

    /// <summary>Clic en la pestaña del panel.</summary>
    public event EventHandler<string>? PestanaElegida;

    public BordeAutoOcultos()
    {
        InitializeComponent();
        AplicarTema();
        Tema.TemaCambiado += AplicarTema;
        Disposed += BordeAutoOcultos_Disposed;
    }

    [Category("Acople")]
    [Description("De qué lado del acople está la franja.")]
    [DefaultValue(ZonaAcople.Derecha)]
    public ZonaAcople Lado { get; set; } = ZonaAcople.Derecha;

    /// <summary>Muestra las pestañas de estos paneles; la del desplegado, marcada.</summary>
    public void Mostrar(IReadOnlyList<(string Id, string Titulo)> paneles, string? desplegado)
    {
        SuspendLayout();

        while (_pestanas.Count > paneles.Count)
        {
            var sobra = _pestanas[^1];
            _pestanas.RemoveAt(_pestanas.Count - 1);
            Controls.Remove(sobra);
            sobra.Dispose();
        }

        while (_pestanas.Count < paneles.Count)
        {
            var p = new PestanaBorde { AutoSize = false };
            p.MouseEnter += Pestana_MouseEnter;
            p.MouseLeave += Pestana_MouseLeave;
            p.MouseDown += Pestana_MouseDown;
            _pestanas.Add(p);
            Controls.Add(p);
        }

        int pos = Separacion;
        for (int i = 0; i < paneles.Count; i++)
        {
            var p = _pestanas[i];
            p.Tag = paneles[i].Id;
            p.Text = paneles[i].Titulo;
            p.Font = _fuente;
            p.Lado = Lado;
            p.BackColor = BackColor;
            p.Desplegada = paneles[i].Id == desplegado;

            int largo = p.LargoNecesario;
            if (Lado == ZonaAcople.Abajo) p.Bounds = new Rectangle(pos, 0, largo, Height);
            else p.Bounds = new Rectangle(0, pos, Width, largo);
            pos += largo + Separacion;
        }

        ResumeLayout();
    }

    /// <summary>Dónde está (en pantalla) la pestaña del panel, o vacío si no la tiene.</summary>
    public Rectangle RectanguloEnPantalla(string id)
    {
        var p = _pestanas.FirstOrDefault(x => (string?)x.Tag == id);
        return p is null ? Rectangle.Empty : p.RectangleToScreen(p.ClientRectangle);
    }

    private void Pestana_MouseEnter(object? sender, EventArgs e)
    {
        if (sender is Control { Tag: string id }) PestanaSenalada?.Invoke(this, id);
    }

    private void Pestana_MouseLeave(object? sender, EventArgs e)
    {
        if (sender is Control { Tag: string id }) PestanaDejada?.Invoke(this, id);
    }

    private void Pestana_MouseDown(object? sender, MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left && sender is Control { Tag: string id }) PestanaElegida?.Invoke(this, id);
    }

    private void AplicarTema()
    {
        BackColor = Tema.Superficie2;

        // Tema.Cuerpo crea una fuente nueva en cada lectura: se guarda una.
        var anterior = _fuente;
        _fuente = Tema.Cuerpo;
        foreach (var p in _pestanas) { p.Font = _fuente; p.BackColor = BackColor; }
        anterior.Dispose();
    }

    private void BordeAutoOcultos_Disposed(object? sender, EventArgs e)
    {
        Tema.TemaCambiado -= AplicarTema;
        _fuente.Dispose();
    }
}
