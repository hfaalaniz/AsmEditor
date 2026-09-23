namespace PruebaTerminal;

/// <summary>
/// La terminal en una ventana, para probarla a mano: PowerShell en la carpeta
/// de NASM, con NASM y GoLink en el PATH (el equivalente a "Developer
/// PowerShell" que va a tener el editor).
/// </summary>
public partial class FormTerminal : Form
{
    public FormTerminal()
    {
        InitializeComponent();

        BackColor = Color.FromArgb(12, 12, 12);
        lblEstado.BackColor = Color.FromArgb(31, 31, 31);
        lblEstado.ForeColor = Color.FromArgb(204, 204, 204);

        terminal.EstadoCambiado += Terminal_EstadoCambiado;
        terminal.ProgramaTerminado += Terminal_ProgramaTerminado;
    }

    private void FormTerminal_Load(object? sender, EventArgs e)
    {
        terminal.Arrancar("powershell.exe -NoLogo", Configuracion.CarpetaNasm, Configuracion.EntornoConNasm());
        terminal.Focus();
    }

    private void Terminal_EstadoCambiado()
    {
        var t = terminal.Pantalla.Titulo;
        lblEstado.Text = $"{terminal.Columnas}×{terminal.Filas}" + (t.Length > 0 ? $"   ·   {t}" : "");
    }

    private void Terminal_ProgramaTerminado() => lblEstado.Text = "El programa terminó.";
}
