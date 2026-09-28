namespace AsmEditor;

/// <summary>
/// EL DIÁLOGO DE SALIR DEL EDITOR.
///
/// Tiene dos formas según lo que haya sin guardar, y esa es toda su razón de ser:
///
///   - SIN cambios pendientes: una confirmación simple. No tiene sentido ofrecer
///     «guardar» cuando no hay nada que guardar.
///   - CON cambios pendientes: lista los archivos modificados y ofrece las tres
///     salidas reales — guardar todo y salir, salir descartando, o volver.
///
/// Muestra los nombres porque «hay cambios sin guardar» no alcanza para decidir:
/// con ocho pestañas abiertas, saber CUÁLES cambiaron es la diferencia entre
/// descartar tranquilo y perder trabajo.
/// </summary>
public sealed class DialogoSalida : Form
{
    /// <summary>Qué eligió el usuario.</summary>
    public enum Resultado
    {
        /// <summary>Guardar los cambios y salir.</summary>
        GuardarYSalir,

        /// <summary>Salir perdiendo los cambios.</summary>
        SalirSinGuardar,

        /// <summary>No salir.</summary>
        Cancelar
    }

    public Resultado Eleccion { get; private set; } = Resultado.Cancelar;

    private const int WM_NCLBUTTONDOWN = 0x00A1;
    private const int HTCAPTION = 0x0002;

    [System.Runtime.InteropServices.DllImport("user32.dll")]
    private static extern bool ReleaseCapture();

    [System.Runtime.InteropServices.DllImport("user32.dll")]
    private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

    private DialogoSalida(IReadOnlyList<string> modificados)
    {
        bool haySinGuardar = modificados.Count > 0;

        FormBorderStyle = FormBorderStyle.None;
        StartPosition = FormStartPosition.CenterParent;
        ShowInTaskbar = false;
        BackColor = Tema.Superficie;
        ForeColor = Tema.Texto;
        Font = Tema.Cuerpo;
        KeyPreview = true;

        int alto = haySinGuardar
            ? 150 + Math.Min(modificados.Count, 6) * 20 + 40
            : 165;

        ClientSize = new Size(470, alto);

        ConstruirFranja(haySinGuardar);
        ConstruirCuerpo(modificados, haySinGuardar);
        ConstruirBotones(haySinGuardar);

        KeyDown += (_, e) =>
        {
            if (e.KeyCode == Keys.Escape) Cerrar(Resultado.Cancelar);
        };
    }

    private void ConstruirFranja(bool haySinGuardar)
    {
        var franja = new Panel { Dock = DockStyle.Top, Height = 34, BackColor = Tema.Superficie2 };

        var titulo = new Label
        {
            Text = haySinGuardar ? "Hay cambios sin guardar" : "Salir del editor",
            AutoSize = true,
            Font = Tema.Etiqueta,
            ForeColor = haySinGuardar ? Tema.Aviso : Tema.Acento,
            Location = new Point(14, 9),
            BackColor = Color.Transparent
        };

        franja.Controls.Add(titulo);
        franja.MouseDown += Arrastrar;
        titulo.MouseDown += Arrastrar;

        Controls.Add(franja);
    }

    private void ConstruirCuerpo(IReadOnlyList<string> modificados, bool haySinGuardar)
    {
        if (!haySinGuardar)
        {
            var texto = new Label
            {
                Text = "¿Cerrar el editor?",
                Location = new Point(20, 56),
                Size = new Size(430, 24),
                ForeColor = Tema.Texto,
                BackColor = Color.Transparent
            };
            Controls.Add(texto);
            return;
        }

        var encabezado = new Label
        {
            Text = modificados.Count == 1
                ? "Este archivo tiene cambios sin guardar:"
                : $"Estos {modificados.Count} archivos tienen cambios sin guardar:",
            Location = new Point(20, 48),
            Size = new Size(430, 20),
            ForeColor = Tema.Texto,
            BackColor = Color.Transparent
        };
        Controls.Add(encabezado);

        // La lista de nombres: con varias pestañas abiertas, saber cuáles
        // cambiaron es lo que permite decidir sin miedo.
        int y = 72;
        foreach (var nombre in modificados.Take(6))
        {
            Controls.Add(new Label
            {
                Text = "•  " + nombre,
                Location = new Point(32, y),
                Size = new Size(420, 18),
                ForeColor = Tema.Acento,
                BackColor = Color.Transparent,
                AutoEllipsis = true
            });
            y += 20;
        }

        if (modificados.Count > 6)
        {
            Controls.Add(new Label
            {
                Text = $"   y {modificados.Count - 6} más...",
                Location = new Point(32, y),
                Size = new Size(420, 18),
                ForeColor = Tema.Texto3,
                BackColor = Color.Transparent
            });
        }
    }

    private void ConstruirBotones(bool haySinGuardar)
    {
        var panel = new Panel { Dock = DockStyle.Bottom, Height = 52, BackColor = Tema.Superficie };

        if (haySinGuardar)
        {
            // Tres salidas, de derecha a izquierda: la de volver atrás al borde,
            // la destructiva separada de la principal para no confundirlas.
            var cancelar = Boton("Cancelar", Tipo.Normal);
            var descartar = Boton("Salir sin guardar", Tipo.Peligro);
            var guardar = Boton("Guardar y salir", Tipo.Principal);

            int x = ClientSize.Width - 14;
            foreach (var (b, r) in new[]
                     {
                         (cancelar, Resultado.Cancelar),
                         (descartar, Resultado.SalirSinGuardar),
                         (guardar, Resultado.GuardarYSalir)
                     })
            {
                x -= b.Width + 8;
                b.Location = new Point(x, 11);
                var eleccion = r;
                b.Click += (_, _) => Cerrar(eleccion);
                panel.Controls.Add(b);
            }

            AcceptButton = guardar;
        }
        else
        {
            var cancelar = Boton("Cancelar", Tipo.Normal);
            var salir = Boton("Salir", Tipo.Principal);

            int x = ClientSize.Width - 14;
            x -= cancelar.Width + 8;
            cancelar.Location = new Point(x, 11);
            cancelar.Click += (_, _) => Cerrar(Resultado.Cancelar);

            x -= salir.Width + 8;
            salir.Location = new Point(x, 11);
            // Sin nada que guardar, «salir» y «salir sin guardar» son lo mismo.
            salir.Click += (_, _) => Cerrar(Resultado.SalirSinGuardar);

            panel.Controls.Add(cancelar);
            panel.Controls.Add(salir);
            AcceptButton = salir;
        }

        Controls.Add(panel);

        // ⚠ EL FOCO ARRANCA EN EL BOTÓN POR DEFECTO. Si no, lo toma el primero
        // que se agregó («Cancelar»), y un botón con el foco se queda con el
        // Enter antes que el AcceptButton: Enter cancelaba la salida (medido
        // el 28/09 con diagnostico\cierre_con_flotante.ps1 -SinFlotante).
        ActiveControl = (Control)AcceptButton;
    }

    private enum Tipo { Normal, Principal, Peligro }

    private static Button Boton(string texto, Tipo tipo)
    {
        var fondo = tipo switch
        {
            Tipo.Principal => Tema.Acento,
            Tipo.Peligro => Tema.Superficie3,
            _ => Tema.Superficie3
        };

        var frente = tipo switch
        {
            Tipo.Principal => Color.Black,
            Tipo.Peligro => Tema.Critico,
            _ => Tema.Texto
        };

        var b = new Button
        {
            Text = texto,
            Size = new Size(tipo == Tipo.Peligro ? 128 : 108, 30),
            FlatStyle = FlatStyle.Flat,
            BackColor = fondo,
            ForeColor = frente,
            UseVisualStyleBackColor = false
        };

        b.FlatAppearance.BorderColor = tipo == Tipo.Principal ? Tema.Acento : Tema.LineaSuave;
        b.FlatAppearance.BorderSize = 1;
        b.FlatAppearance.MouseOverBackColor = Tema.Realzar(fondo, 16);
        b.FlatAppearance.MouseDownBackColor = Tema.Realzar(fondo, 28);

        return b;
    }

    private void Cerrar(Resultado r)
    {
        Eleccion = r;
        DialogResult = r == Resultado.Cancelar ? DialogResult.Cancel : DialogResult.OK;
        Close();
    }

    private void Arrastrar(object? sender, MouseEventArgs e)
    {
        if (e.Button != MouseButtons.Left) return;
        ReleaseCapture();
        SendMessage(Handle, WM_NCLBUTTONDOWN, HTCAPTION, IntPtr.Zero);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        using var pen = new Pen(Tema.Linea, 1);
        e.Graphics.DrawRectangle(pen, 0, 0, ClientSize.Width - 1, ClientSize.Height - 1);
    }

    /// <summary>
    /// Pregunta si se puede salir. <paramref name="modificados"/> son los nombres
    /// de los documentos con cambios: si viene vacío, el diálogo es la
    /// confirmación simple.
    /// </summary>
    public static Resultado Preguntar(IWin32Window? padre, IReadOnlyList<string> modificados)
    {
        using var d = new DialogoSalida(modificados);

        if (padre is Form f)
        {
            d.StartPosition = FormStartPosition.Manual;
            d.Location = new Point(
                f.Left + (f.Width - d.Width) / 2,
                f.Top + (f.Height - d.Height) / 2);
        }

        d.ShowDialog(padre);
        return d.Eleccion;
    }
}
