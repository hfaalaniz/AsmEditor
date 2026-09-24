using System.Diagnostics;
using AsmEditor.Core;

namespace AsmEditor;

/// <summary>Qué eligió el usuario en la ventana de inicio.</summary>
public enum AccionInicio
{
    AbrirProyecto,
    CrearProyecto,
    AbrirCarpeta,
    AbrirArchivo,
    ContinuarSinCodigo,

    /// <summary>No viene de la ventana: es la opción "Al iniciar: último proyecto".</summary>
    UltimoProyecto
}

/// <summary>La elección, con la ruta cuando corresponde.</summary>
public sealed record EleccionInicio(AccionInicio Accion, string? Ruta = null);

/// <summary>
/// La ventana de inicio, como la de Visual Studio: el editor arranca acá y el
/// IDE abre con lo que se elija.
///
///   - Izquierda: buscador y proyectos recientes agrupados por fecha (Hoy,
///     Ayer, Esta semana, Este mes, Anterior). Doble clic o Enter abre;
///     clic derecho: quitar de la lista, abrir la carpeta.
///   - Derecha: crear un proyecto, abrir un proyecto, abrir una carpeta, abrir
///     un archivo o proyecto.
///   - Abajo: continuar sin código (el IDE vacío).
///
/// No abre nada por su cuenta: devuelve <see cref="Eleccion"/> y MainForm la
/// aplica. Cerrarla con la X cierra el editor, como en Visual Studio.
///
/// La lógica (agrupado, filtro, fechas) está en Core\RecientesInicio.cs, con
/// pruebas; acá queda solo lo visual.
/// </summary>
public partial class VentanaInicio : Form
{
    private readonly UiState _ui;
    private readonly Action _guardar;
    private readonly string _carpetaInicial;

    private List<ProyectoReciente> _todos = new();

    /// <summary>Un renglón de la lista: título de grupo o proyecto.</summary>
    private sealed record ItemGrupo(string Titulo) { public override string ToString() => Titulo; }
    private sealed record ItemProyecto(ProyectoReciente Proyecto) { public override string ToString() => Proyecto.Nombre; }

    public EleccionInicio? Eleccion { get; private set; }

    /// <param name="ui">El estado del editor: de ahí salen los recientes, y "Quitar de la lista" lo modifica.</param>
    /// <param name="guardar">Guarda la configuración (después de quitar un reciente).</param>
    /// <param name="carpetaInicial">Dónde arrancan los diálogos de abrir.</param>
    public VentanaInicio(UiState ui, Action guardar, string carpetaInicial)
    {
        InitializeComponent();

        _ui = ui;
        _guardar = guardar;
        _carpetaInicial = carpetaInicial;

        Icon = LogoEditor.CrearIcono();
        AplicarTema();
        CargarRecientes();
    }

    // ------------------------------------------------------------------
    // Recientes
    // ------------------------------------------------------------------

    /// <summary>Lee los recientes (los que siguen existiendo) y los muestra.</summary>
    private void CargarRecientes()
    {
        _todos = RecientesInicio.Armar(
            _ui.RecentProjects.Where(File.Exists),
            _ui.FechasProyectos,
            File.GetLastWriteTime);

        Mostrar();
    }

    private void Mostrar()
    {
        var filtrados = RecientesInicio.Filtrar(_todos, txtBuscar.Text);

        lstRecientes.BeginUpdate();
        lstRecientes.Items.Clear();

        foreach (var g in RecientesInicio.Agrupar(filtrados, DateTime.Now))
        {
            lstRecientes.Items.Add(new ItemGrupo(g.Titulo));
            foreach (var p in g.Proyectos) lstRecientes.Items.Add(new ItemProyecto(p));
        }

        lstRecientes.EndUpdate();

        lblSinRecientes.Text = _todos.Count == 0
            ? "Todavía no hay proyectos recientes."
            : "Ningún proyecto coincide con la búsqueda.";
        lblSinRecientes.Visible = lstRecientes.Items.Count == 0;
        if (lblSinRecientes.Visible) lblSinRecientes.BringToFront();
    }

    private ProyectoReciente? Seleccionado =>
        lstRecientes.SelectedItem is ItemProyecto ip ? ip.Proyecto : null;

    private void AbrirSeleccionado()
    {
        if (Seleccionado is { } p) Elegir(AccionInicio.AbrirProyecto, p.Ruta);
    }

    private void Elegir(AccionInicio accion, string? ruta = null)
    {
        Eleccion = new EleccionInicio(accion, ruta);
        DialogResult = DialogResult.OK;
        Close();
    }

    // ------------------------------------------------------------------
    // Lista
    // ------------------------------------------------------------------

    private void lstRecientes_MeasureItem(object? sender, MeasureItemEventArgs e)
    {
        if (e.Index < 0) return;
        e.ItemHeight = lstRecientes.Items[e.Index] is ItemGrupo ? 30 : 46;
    }

    private void lstRecientes_DrawItem(object? sender, DrawItemEventArgs e)
    {
        if (e.Index < 0) return;

        var item = lstRecientes.Items[e.Index];
        var g = e.Graphics;
        var r = e.Bounds;

        if (item is ItemGrupo grupo)
        {
            using (var b = new SolidBrush(Tema.Fondo)) g.FillRectangle(b, r);
            using var f = new Font("Segoe UI Semibold", 9.5f);
            TextRenderer.DrawText(g, grupo.Titulo, f, new Rectangle(r.X + 4, r.Y + 8, r.Width, 20), Tema.Texto2,
                                  TextFormatFlags.Left | TextFormatFlags.Top);
            return;
        }

        if (item is not ItemProyecto { Proyecto: var p }) return;

        bool marcado = (e.State & DrawItemState.Selected) != 0;
        using (var b = new SolidBrush(marcado ? Tema.Seleccion : Tema.Fondo)) g.FillRectangle(b, r);

        using (var icono = IconosBarra.Dibujar(Glifo.Abrir, 18)) g.DrawImage(icono, r.X + 12, r.Y + 14);

        var fecha = p.UltimaApertura.ToString("d/M/yyyy HH:mm");
        var anchoFecha = TextRenderer.MeasureText(fecha, Tema.Chico).Width;
        var zonaTexto = new Rectangle(r.X + 40, r.Y + 4, r.Width - 40 - anchoFecha - 16, 20);

        TextRenderer.DrawText(g, p.Nombre, Tema.Cuerpo, zonaTexto, Tema.Texto,
                              TextFormatFlags.Left | TextFormatFlags.EndEllipsis);
        TextRenderer.DrawText(g, p.Carpeta, Tema.Chico, new Rectangle(zonaTexto.X, r.Y + 24, zonaTexto.Width, 18), Tema.Texto3,
                              TextFormatFlags.Left | TextFormatFlags.PathEllipsis);
        TextRenderer.DrawText(g, fecha, Tema.Chico, new Rectangle(r.Right - anchoFecha - 12, r.Y + 4, anchoFecha + 4, 20), Tema.Texto3,
                              TextFormatFlags.Right);
    }

    private void lstRecientes_DoubleClick(object? sender, EventArgs e) => AbrirSeleccionado();

    private void lstRecientes_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Enter) { AbrirSeleccionado(); e.Handled = e.SuppressKeyPress = true; }
        if (e.KeyCode == Keys.Delete && Seleccionado is not null) { miQuitar_Click(sender, e); e.Handled = true; }
    }

    /// <summary>El clic derecho también selecciona: el menú actúa sobre el renglón señalado.</summary>
    private void lstRecientes_MouseDown(object? sender, MouseEventArgs e)
    {
        if (e.Button != MouseButtons.Right) return;
        int i = lstRecientes.IndexFromPoint(e.Location);
        if (i >= 0) lstRecientes.SelectedIndex = i;
    }

    private void cmsProyecto_Opening(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        // Sobre un título de grupo (o en el vacío) no hay nada que ofrecer.
        e.Cancel = Seleccionado is null;
    }

    private void miQuitar_Click(object? sender, EventArgs e)
    {
        if (Seleccionado is not { } p) return;

        // Solo de la lista: el proyecto sigue en el disco.
        _ui.RemoveRecentProject(p.Ruta);
        _guardar();
        CargarRecientes();
    }

    private void miAbrirCarpeta_Click(object? sender, EventArgs e)
    {
        if (Seleccionado is not { } p) return;

        // El Explorador de Windows con el .asmproj marcado.
        Process.Start(new ProcessStartInfo("explorer.exe", $"/select,\"{p.Ruta}\"") { UseShellExecute = true });
    }

    // ------------------------------------------------------------------
    // Buscador
    // ------------------------------------------------------------------

    private void txtBuscar_TextChanged(object? sender, EventArgs e) => Mostrar();

    private void txtBuscar_KeyDown(object? sender, KeyEventArgs e)
    {
        // Flecha abajo pasa a la lista; Enter abre el primer resultado.
        int primero = -1;
        for (int i = 0; i < lstRecientes.Items.Count; i++)
        {
            if (lstRecientes.Items[i] is ItemProyecto) { primero = i; break; }
        }

        if (e.KeyCode == Keys.Down && primero >= 0)
        {
            lstRecientes.Focus();
            lstRecientes.SelectedIndex = primero;
            e.Handled = e.SuppressKeyPress = true;
        }
        else if (e.KeyCode == Keys.Enter && primero >= 0)
        {
            lstRecientes.SelectedIndex = primero;
            AbrirSeleccionado();
            e.Handled = e.SuppressKeyPress = true;
        }
    }

    private void VentanaInicio_KeyDown(object? sender, KeyEventArgs e)
    {
        // Alt+U: al buscador, como en Visual Studio.
        if (e.Alt && e.KeyCode == Keys.U)
        {
            txtBuscar.Focus();
            e.Handled = e.SuppressKeyPress = true;
        }
        else if (e.KeyCode == Keys.Escape)
        {
            Close();
        }
    }

    // ------------------------------------------------------------------
    // Acciones
    // ------------------------------------------------------------------

    private void btnCrear_Click(object? sender, EventArgs e) => Elegir(AccionInicio.CrearProyecto);

    private void btnAbrirProyecto_Click(object? sender, EventArgs e)
    {
        using var dlg = new OpenFileDialog
        {
            Title = "Abrir proyecto",
            Filter = "Proyecto del editor (*.asmproj)|*.asmproj|Todos los archivos (*.*)|*.*",
            InitialDirectory = _carpetaInicial
        };

        if (dlg.ShowDialog(this) == DialogResult.OK) Elegir(AccionInicio.AbrirProyecto, dlg.FileName);
    }

    private void btnAbrirCarpeta_Click(object? sender, EventArgs e)
    {
        using var dlg = new FolderBrowserDialog
        {
            Description = "Abrir una carpeta",
            UseDescriptionForTitle = true,
            InitialDirectory = _carpetaInicial
        };

        if (dlg.ShowDialog(this) == DialogResult.OK) Elegir(AccionInicio.AbrirCarpeta, dlg.SelectedPath);
    }

    private void btnAbrirArchivo_Click(object? sender, EventArgs e)
    {
        using var dlg = new OpenFileDialog
        {
            Title = "Abrir un archivo o proyecto",
            Filter = "Fuentes y proyectos (*.asm;*.inc;*.s;*.asmform;*.asmproj)|*.asm;*.inc;*.s;*.asmform;*.asmproj|Todos los archivos (*.*)|*.*",
            InitialDirectory = _carpetaInicial
        };

        if (dlg.ShowDialog(this) == DialogResult.OK) Elegir(AccionInicio.AbrirArchivo, dlg.FileName);
    }

    private void btnContinuar_Click(object? sender, EventArgs e) => Elegir(AccionInicio.ContinuarSinCodigo);

    // ------------------------------------------------------------------
    // Aspecto
    // ------------------------------------------------------------------

    private void AplicarTema()
    {
        BackColor = Tema.Fondo;
        ForeColor = Tema.Texto;

        lblTitulo.ForeColor = Tema.Texto;
        lblIntroduccion.ForeColor = Tema.Texto;
        lblSinRecientes.ForeColor = Tema.Texto3;
        lblSinRecientes.BackColor = Tema.Fondo;

        txtBuscar.BackColor = Tema.Superficie2;
        txtBuscar.ForeColor = Tema.Texto;
        txtBuscar.Font = Tema.Cuerpo;

        lstRecientes.BackColor = Tema.Fondo;

        btnContinuar.BackColor = Tema.Superficie2;
        btnContinuar.ForeColor = Tema.Texto;
        btnContinuar.FlatAppearance.BorderColor = Tema.LineaSuave;
        btnContinuar.FlatAppearance.MouseOverBackColor = Tema.Realzar(Tema.Superficie2, 14);
    }

    /// <summary>Barra de título oscura en el tema oscuro (ver MarcoOscuro).</summary>
    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        MarcoOscuro.Aplicar(Handle);
    }
}
