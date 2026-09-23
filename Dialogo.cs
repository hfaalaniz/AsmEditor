namespace AsmEditor;

/// <summary>
/// LOS AVISOS Y PREGUNTAS DEL EDITOR, CON LA PALETA DEL PROGRAMA.
///
/// Reemplaza a <see cref="MessageBox"/>, que se dibuja con el tema de Windows:
/// sobre un editor oscuro aparecía una ventana clara y se notaba el parche.
///
/// La idea y la mecánica salen de `Dialogo.cs` del launcher de Trapezoide, donde
/// ya estaba resuelto el arrastre sin barra nativa y el foco del botón por
/// defecto.
///
/// ⚠ NO REEMPLAZA A LOS DIÁLOGOS DE ARCHIVO. `OpenFileDialog` los dibuja el
/// sistema y no se pueden pintar: para abrir está el explorador lateral, y
/// «Guardar como» usa <see cref="DialogoGuardar"/>, que sí es propio.
/// </summary>
public sealed class Dialogo : Form
{
    public enum Tono { Info, Aviso, Error, Pregunta }

    private const int WM_NCLBUTTONDOWN = 0x00A1;
    private const int HTCAPTION = 0x0002;

    [System.Runtime.InteropServices.DllImport("user32.dll")]
    private static extern bool ReleaseCapture();

    [System.Runtime.InteropServices.DllImport("user32.dll")]
    private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

    private Dialogo(string titulo, string texto, Tono tono, string[] botones, int porDefecto)
    {
        FormBorderStyle = FormBorderStyle.None;
        StartPosition = FormStartPosition.CenterParent;
        ShowInTaskbar = false;
        MinimizeBox = false;
        MaximizeBox = false;
        BackColor = Tema.Superficie;
        ForeColor = Tema.Texto;
        Font = Tema.Cuerpo;
        KeyPreview = true;

        // ── Medida del texto para que el diálogo crezca con el contenido ──
        int anchoTexto = 380;
        Size medida;
        using (var g = CreateGraphics())
        {
            medida = Size.Ceiling(g.MeasureString(texto, Tema.Cuerpo, anchoTexto));
        }

        int alto = Math.Max(150, 96 + medida.Height + 56);
        ClientSize = new Size(anchoTexto + 90, alto);

        // ── Franja de título ──
        var franja = new Panel
        {
            Dock = DockStyle.Top,
            Height = 34,
            BackColor = Tema.Superficie2
        };

        var lblTitulo = new Label
        {
            Text = titulo,
            AutoSize = true,
            Font = Tema.Etiqueta,
            ForeColor = ColorDe(tono),
            Location = new Point(14, 9),
            BackColor = Color.Transparent
        };
        franja.Controls.Add(lblTitulo);

        // Sin barra nativa hay que reponer el arrastre, o la ventana queda clavada.
        franja.MouseDown += Arrastrar;
        lblTitulo.MouseDown += Arrastrar;

        // ── Mensaje ──
        var lblTexto = new Label
        {
            Text = texto,
            Location = new Point(20, 52),
            Size = new Size(anchoTexto, medida.Height + 8),
            ForeColor = Tema.Texto,
            BackColor = Color.Transparent
        };

        // ── Botonera ──
        var panelBotones = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 52,
            BackColor = Tema.Superficie
        };

        int x = ClientSize.Width - 14;
        for (int i = botones.Length - 1; i >= 0; i--)
        {
            int indice = i;
            var b = new Button
            {
                Text = botones[i],
                Size = new Size(96, 30),
                FlatStyle = FlatStyle.Flat,
                BackColor = indice == porDefecto ? Tema.Acento : Tema.Superficie3,
                ForeColor = indice == porDefecto ? Color.Black : Tema.Texto,
                UseVisualStyleBackColor = false,
                TabStop = true
            };
            b.FlatAppearance.BorderColor = indice == porDefecto ? Tema.Acento : Tema.LineaSuave;
            b.FlatAppearance.BorderSize = 1;
            b.FlatAppearance.MouseOverBackColor = Tema.Realzar(b.BackColor, 16);
            b.FlatAppearance.MouseDownBackColor = Tema.Realzar(b.BackColor, 28);

            x -= b.Width + 8;
            b.Location = new Point(x, 11);

            b.Click += (_, _) => { Resultado = indice; DialogResult = DialogResult.OK; Close(); };

            panelBotones.Controls.Add(b);

            if (indice == porDefecto) AcceptButton = b;
        }

        Controls.Add(lblTexto);
        Controls.Add(panelBotones);
        Controls.Add(franja);

        // Escape equivale al último botón, que por convención es el de cancelar.
        KeyDown += (_, e) =>
        {
            if (e.KeyCode == Keys.Escape)
            {
                Resultado = botones.Length - 1;
                DialogResult = DialogResult.Cancel;
                Close();
            }
        };

        Shown += (_, _) => { if (AcceptButton is Button b) b.Focus(); };
    }

    /// <summary>Índice del botón elegido.</summary>
    public int Resultado { get; private set; }

    private void Arrastrar(object? sender, MouseEventArgs e)
    {
        if (e.Button != MouseButtons.Left) return;
        ReleaseCapture();
        SendMessage(Handle, WM_NCLBUTTONDOWN, HTCAPTION, IntPtr.Zero);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);

        // Un borde propio: sin marco de Windows el diálogo se confundiría con
        // el fondo de la ventana que tiene detrás.
        using var pen = new Pen(Tema.Linea, 1);
        e.Graphics.DrawRectangle(pen, 0, 0, ClientSize.Width - 1, ClientSize.Height - 1);
    }

    private static Color ColorDe(Tono t) => t switch
    {
        Tono.Aviso => Tema.Aviso,
        Tono.Error => Tema.Critico,
        Tono.Pregunta => Tema.Acento,
        _ => Tema.Info
    };

    // ---------------------------------------------------------------
    // API
    // ---------------------------------------------------------------

    /// <summary>Aviso de una sola salida.</summary>
    public static void Aviso(IWin32Window? padre, string titulo, string texto, Tono tono = Tono.Info)
    {
        using var d = new Dialogo(titulo, texto, tono, new[] { "Aceptar" }, 0);
        Mostrar(d, padre);
    }

    public static void Error(IWin32Window? padre, string titulo, string texto) =>
        Aviso(padre, titulo, texto, Tono.Error);

    /// <summary>Pregunta de sí o no. True si eligió la primera opción.</summary>
    public static bool Confirmar(IWin32Window? padre, string titulo, string texto,
                                 string si = "Sí", string no = "Cancelar")
    {
        using var d = new Dialogo(titulo, texto, Tono.Pregunta, new[] { si, no }, 0);
        Mostrar(d, padre);
        return d.Resultado == 0;
    }

    /// <summary>
    /// Pregunta de tres salidas, para «hay cambios sin guardar»: guardar,
    /// descartar o cancelar. Devuelve el índice del botón.
    /// </summary>
    public static int Preguntar(IWin32Window? padre, string titulo, string texto, string[] botones,
                                int porDefecto = 0, Tono tono = Tono.Pregunta)
    {
        using var d = new Dialogo(titulo, texto, tono, botones, porDefecto);
        Mostrar(d, padre);
        return d.Resultado;
    }

    private static void Mostrar(Dialogo d, IWin32Window? padre)
    {
        if (padre is Form f)
        {
            d.StartPosition = FormStartPosition.Manual;
            d.Location = new Point(
                f.Left + (f.Width - d.Width) / 2,
                f.Top + (f.Height - d.Height) / 2);
        }

        d.ShowDialog(padre);
    }
}
