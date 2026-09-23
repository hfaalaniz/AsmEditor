using System.Diagnostics;
using AsmEditor.Core;

namespace AsmEditor;

/// <summary>
/// EL «ACERCA DE».
///
/// Además de la marca y el contacto, muestra QUÉ HERRAMIENTAS ENCONTRÓ el
/// editor y dónde, con su estado real comprobado en el momento de abrir.
///
/// ⚠ ESO ES LO QUE LO HACE ÚTIL Y NO DECORATIVO: cuando una compilación falla
/// por una ruta mal configurada, esta ventana dice exactamente cuál falta y
/// dónde la estaba buscando. Sin eso habría que adivinar entre el settings.json,
/// el PATH y la instalación de Visual Studio.
///
/// El texto se puede copiar entero al portapapeles, que es lo que sirve para
/// pasar un informe cuando algo no anda.
/// </summary>
public sealed class FormAcercaDe : Form
{
    // El ancho salió de mirar el informe: con 560 las rutas del SDK
    // (C:\Program Files (x86)\Windows Kits\10\Lib\10.0.26100.0\um\x64\...)
    // se partían en dos renglones y se leían mal.
    private const int Ancho = 660;
    private const int Alto = 480;
    private const int MargenX = 28;
    private const int LadoLogo = 72;

    private const int WM_NCLBUTTONDOWN = 0x00A1;
    private const int HTCAPTION = 0x0002;

    [System.Runtime.InteropServices.DllImport("user32.dll")]
    private static extern bool ReleaseCapture();

    [System.Runtime.InteropServices.DllImport("user32.dll")]
    private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

    private readonly BuildSettings _settings;
    private readonly PestanasAsm _pestanas = new();

    public FormAcercaDe(BuildSettings settings)
    {
        _settings = settings;

        FormBorderStyle = FormBorderStyle.None;
        StartPosition = FormStartPosition.CenterParent;
        ShowInTaskbar = false;
        ClientSize = new Size(Ancho, Alto);
        BackColor = Tema.Superficie;
        ForeColor = Tema.Texto;
        Font = Tema.Cuerpo;
        KeyPreview = true;
        DoubleBuffered = true;

        ConstruirCabecera();
        ConstruirPestanas();
        ConstruirBotones();

        KeyDown += (_, e) => { if (e.KeyCode == Keys.Escape) Close(); };
    }

    // ---------------------------------------------------------------
    // Cabecera
    // ---------------------------------------------------------------

    private const int AltoCabecera = 116;

    private void ConstruirCabecera()
    {
        var franja = new Panel
        {
            Dock = DockStyle.Top,
            Height = AltoCabecera,
            BackColor = Tema.Superficie2
        };

        franja.Paint += (_, e) =>
        {
            var g = e.Graphics;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            float logoY = (AltoCabecera - LadoLogo) / 2f;
            LogoEditor.Dibujar(g, MargenX, logoY, LadoLogo, Tema.Acento, Tema.Texto3);

            float x = MargenX + LadoLogo + 20;

            using var bTitulo = new SolidBrush(Tema.Texto);
            using var bAcento = new SolidBrush(Tema.Acento);
            using var bSub = new SolidBrush(Tema.Texto3);
            using var fTitulo = new Font("Segoe UI Light", 20f);
            using var fVersion = new Font("Segoe UI Semibold", 9f);
            using var fSub = new Font("Segoe UI", 8.5f);

            g.DrawString(InfoApp.NombreApp, fTitulo, bTitulo, x, logoY + 4);
            g.DrawString(InfoApp.Texto, fVersion, bAcento, x + 2, logoY + 40);
            g.DrawString(InfoApp.Descripcion, fSub, bSub, x + 2, logoY + 57);

            // Franja de acento arriba, igual que el splash y la pestaña activa.
            using var acento = new SolidBrush(Tema.Acento);
            g.FillRectangle(acento, 0, 0, franja.Width, 3);
        };

        // Sin marco de Windows hay que reponer el arrastre.
        franja.MouseDown += Arrastrar;

        Controls.Add(franja);
    }

    // ---------------------------------------------------------------
    // Pestañas
    // ---------------------------------------------------------------

    private void ConstruirPestanas()
    {
        _pestanas.Location = new Point(0, AltoCabecera);
        _pestanas.Size = new Size(Ancho, Alto - AltoCabecera - 54);
        _pestanas.PermiteCerrar = false;
        _pestanas.UsaFondoDeCodigo = false;
        _pestanas.ItemSize = new Size(Ancho / 3 - 4, PestanasAsm.AltoPestana);

        _pestanas.TabPages.Add(CrearPagina("Programa", ConstruirPaginaPrograma()));
        _pestanas.TabPages.Add(CrearPagina("Herramientas", ConstruirPaginaHerramientas()));
        _pestanas.TabPages.Add(CrearPagina("Sistema", ConstruirPaginaSistema()));

        Controls.Add(_pestanas);
    }

    private static TabPage CrearPagina(string titulo, Control contenido)
    {
        var page = new TabPage(titulo) { BackColor = Tema.Superficie };
        contenido.Dock = DockStyle.Fill;
        page.Controls.Add(contenido);
        return page;
    }

    /// <summary>Datos del autor y el contacto.</summary>
    private Control ConstruirPaginaPrograma()
    {
        var panel = new Panel { BackColor = Tema.Superficie, Padding = new Padding(MargenX, 18, MargenX, 10) };

        int y = 14;

        void Dato(string etiqueta, string valor, bool enlace = false)
        {
            panel.Controls.Add(new Label
            {
                Text = etiqueta,
                Location = new Point(0, y),
                Size = new Size(96, 20),
                ForeColor = Tema.Texto3,
                BackColor = Color.Transparent
            });

            if (enlace)
            {
                var link = new LinkLabel
                {
                    Text = valor,
                    Location = new Point(100, y),
                    AutoSize = true,
                    LinkColor = Tema.Acento,
                    ActiveLinkColor = Tema.Info,
                    LinkBehavior = LinkBehavior.HoverUnderline,
                    BackColor = Color.Transparent
                };
                link.Click += (_, _) => AbrirEnNavegador(valor);
                panel.Controls.Add(link);
            }
            else
            {
                panel.Controls.Add(new Label
                {
                    Text = valor,
                    Location = new Point(100, y),
                    Size = new Size(Ancho - MargenX * 2 - 100, 20),
                    ForeColor = Tema.Texto,
                    BackColor = Color.Transparent
                });
            }

            y += 26;
        }

        Dato("Autor", InfoApp.Autor);
        Dato("Sitio", InfoApp.Sitio, enlace: true);
        Dato("Móvil", InfoApp.Movil);
        Dato("Ubicación", InfoApp.Ubicacion);

        y += 10;

        panel.Controls.Add(new Label
        {
            Text = "Este editor orquesta herramientas de terceros; no las incluye:",
            Location = new Point(0, y),
            Size = new Size(Ancho - MargenX * 2, 20),
            ForeColor = Tema.Texto3,
            BackColor = Color.Transparent
        });
        y += 24;

        foreach (var (nombre, quien) in new[]
                 {
                     ("NASM", "Netwide Assembler — licencia BSD de 2 cláusulas"),
                     ("GoLink", "Jeremy Gordon — gratuito"),
                     ("link.exe", "Microsoft — parte de Visual Studio")
                 })
        {
            panel.Controls.Add(new Label
            {
                Text = "•  " + nombre,
                Location = new Point(8, y),
                Size = new Size(86, 18),
                ForeColor = Tema.Texto2,
                BackColor = Color.Transparent
            });
            panel.Controls.Add(new Label
            {
                Text = quien,
                Location = new Point(100, y),
                Size = new Size(Ancho - MargenX * 2 - 100, 18),
                ForeColor = Tema.Texto3,
                BackColor = Color.Transparent
            });
            y += 21;
        }

        return panel;
    }

    /// <summary>
    /// Las herramientas y su estado REAL, comprobado al abrir la ventana.
    /// Es lo que convierte esta pantalla en algo útil cuando algo no compila.
    /// </summary>
    private Control ConstruirPaginaHerramientas()
    {
        var lista = new ListView
        {
            View = View.Details,
            FullRowSelect = true,
            MultiSelect = false,
            GridLines = false,
            HideSelection = false,
            BorderStyle = BorderStyle.None,
            BackColor = Tema.Superficie,
            ForeColor = Tema.Texto,
            Font = Tema.Cuerpo
        };

        lista.Columns.Add("Herramienta", 120);
        lista.Columns.Add("Estado", 70);
        lista.Columns.Add("Ubicación", Ancho - 200);

        foreach (var (nombre, ruta) in DetectarHerramientas())
        {
            bool ok = ruta is not null && File.Exists(ruta);

            var item = new ListViewItem(nombre) { ForeColor = ok ? Tema.Ok : Tema.Critico };
            item.SubItems.Add(ok ? "OK" : "falta");
            item.SubItems.Add(ruta ?? "(no se encontró)");
            lista.Items.Add(item);
        }

        // Doble clic abre la carpeta: si algo falta, el paso siguiente es ir a
        // mirar dónde se lo estaba buscando.
        lista.DoubleClick += (_, _) =>
        {
            if (lista.SelectedItems.Count == 0) return;
            var ruta = lista.SelectedItems[0].SubItems[2].Text;
            if (File.Exists(ruta)) AbrirCarpetaDe(ruta);
        };

        return lista;
    }

    /// <summary>
    /// Arma la lista de herramientas con la ruta donde el editor las busca de
    /// verdad, no una ruta escrita a mano: sale de la configuración y de
    /// <see cref="LinkerLocator"/>, igual que al compilar.
    /// </summary>
    private List<(string Nombre, string? Ruta)> DetectarHerramientas()
    {
        var carpeta = Directory.Exists(_settings.ProjectFolder)
            ? _settings.ProjectFolder
            : AppContext.BaseDirectory;

        var lista = new List<(string, string?)>
        {
            ("NASM", _settings.NasmPath),
            ("MASM (64)", LinkerLocator.FindMasm(TargetArch.Win64, carpeta)),
            ("MASM (32)", LinkerLocator.FindMasm(TargetArch.Win32, carpeta)),
            ("GoLink", _settings.GoLinkPath),
            ("link.exe (64)", LinkerLocator.FindMsvcLinker(TargetArch.Win64, carpeta)),
            ("link.exe (32)", LinkerLocator.FindMsvcLinker(TargetArch.Win32, carpeta)),
            ("SDK libs (64)", RutaDeSdk(TargetArch.Win64)),
            ("SDK libs (32)", RutaDeSdk(TargetArch.Win32))
        };

        return lista;
    }

    /// <summary>
    /// Del SDK se comprueba kernel32.lib y no la carpeta: una carpeta vacía
    /// existiría igual y el enlazado fallaría después con LNK1181.
    /// </summary>
    private static string? RutaDeSdk(TargetArch arch)
    {
        var carpeta = LinkerLocator.FindSdkLibPath(arch);
        return carpeta is null ? null : Path.Combine(carpeta, "kernel32.lib");
    }

    /// <summary>Versiones del entorno, para un informe de problemas.</summary>
    private Control ConstruirPaginaSistema()
    {
        var caja = new TextBox
        {
            Multiline = true,
            ReadOnly = true,
            ScrollBars = ScrollBars.Vertical,
            BorderStyle = BorderStyle.None,
            BackColor = Tema.Superficie,
            ForeColor = Tema.Texto2,
            Font = Tema.CodigoChico,
            Text = ConstruirInforme()
        };

        // ⚠ UN TextBox MULTILÍNEA ARRANCA CON EL CARET AL FINAL y la vista
        // scrolleada hasta abajo: se veía el informe empezado por el medio.
        caja.HandleCreated += (_, _) =>
        {
            caja.SelectionStart = 0;
            caja.SelectionLength = 0;
            caja.ScrollToCaret();
        };

        return caja;
    }

    /// <summary>
    /// El informe completo en texto. Se usa para la pestaña Sistema y para el
    /// botón de copiar: es lo que sirve pegar cuando hay que reportar algo.
    /// </summary>
    private string ConstruirInforme()
    {
        var sb = new System.Text.StringBuilder();

        sb.AppendLine($"{InfoApp.NombreApp} {InfoApp.Texto}");
        sb.AppendLine(InfoApp.Descripcion);
        sb.AppendLine();
        sb.AppendLine($"Autor      {InfoApp.Autor}");
        sb.AppendLine($"Sitio      {InfoApp.Sitio}");
        sb.AppendLine($"Móvil      {InfoApp.Movil}");
        sb.AppendLine($"Ubicación  {InfoApp.Ubicacion}");
        sb.AppendLine();
        sb.AppendLine("── Entorno ──────────────────────────────────────────");
        sb.AppendLine($"Sistema    {Environment.OSVersion.VersionString}");
        sb.AppendLine($"Arquit.    {(Environment.Is64BitOperatingSystem ? "x64" : "x86")}" +
                      $"  ·  proceso {(Environment.Is64BitProcess ? "64" : "32")} bits");
        sb.AppendLine($".NET       {Environment.Version}");
        sb.AppendLine($"Equipo     {Environment.MachineName}");
        sb.AppendLine($"Monitores  {Screen.AllScreens.Length}");
        sb.AppendLine();
        sb.AppendLine("── Herramientas ─────────────────────────────────────");

        foreach (var (nombre, ruta) in DetectarHerramientas())
        {
            bool ok = ruta is not null && File.Exists(ruta);
            sb.AppendLine($"{(ok ? "OK  " : "MAL ")} {nombre,-16} {ruta ?? "(no se encontró)"}");
        }

        sb.AppendLine();
        sb.AppendLine("── Configuración ────────────────────────────────────");
        sb.AppendLine($"Proyecto   {_settings.ProjectFolder}");
        sb.AppendLine($"Target     {_settings.ActiveTarget.DisplayName}");
        sb.AppendLine($"Entrada    {_settings.ActiveTarget.EntryPoint}");
        sb.AppendLine($"Librerías  {_settings.ActiveTarget.Libraries}");
        sb.AppendLine($"Targets    {_settings.Targets.Count} configurados");

        return sb.ToString();
    }

    // ---------------------------------------------------------------
    // Botones
    // ---------------------------------------------------------------

    private void ConstruirBotones()
    {
        var panel = new Panel { Dock = DockStyle.Bottom, Height = 54, BackColor = Tema.Superficie };

        var copiar = Boton("Copiar informe", principal: false);
        copiar.Width = 128;
        copiar.Location = new Point(MargenX, 12);
        copiar.Click += (_, _) => CopiarInforme(copiar);

        var cerrar = Boton("Cerrar", principal: true);
        cerrar.Location = new Point(Ancho - MargenX - cerrar.Width, 12);
        cerrar.Click += (_, _) => Close();

        panel.Controls.Add(copiar);
        panel.Controls.Add(cerrar);

        AcceptButton = cerrar;
        Controls.Add(panel);
    }

    private static Button Boton(string texto, bool principal)
    {
        var b = new Button
        {
            Text = texto,
            Size = new Size(104, 30),
            FlatStyle = FlatStyle.Flat,
            BackColor = principal ? Tema.Acento : Tema.Superficie3,
            ForeColor = principal ? Color.Black : Tema.Texto,
            UseVisualStyleBackColor = false
        };

        b.FlatAppearance.BorderColor = principal ? Tema.Acento : Tema.LineaSuave;
        b.FlatAppearance.MouseOverBackColor = Tema.Realzar(b.BackColor, 16);
        b.FlatAppearance.MouseDownBackColor = Tema.Realzar(b.BackColor, 28);
        return b;
    }

    private void CopiarInforme(Button boton)
    {
        try
        {
            Clipboard.SetText(ConstruirInforme());

            // Confirmación en el propio botón: un diálogo para avisar de una
            // copia sería una ventana de más sobre otra ventana.
            var original = boton.Text;
            boton.Text = "Copiado";
            boton.Enabled = false;

            var t = new System.Windows.Forms.Timer { Interval = 1200 };
            t.Tick += (_, _) =>
            {
                t.Stop();
                t.Dispose();
                if (!boton.IsDisposed) { boton.Text = original; boton.Enabled = true; }
            };
            t.Start();
        }
        catch (Exception ex)
        {
            // El portapapeles lo puede tener tomado otro programa.
            Dialogo.Error(this, "No se pudo copiar", ex.Message);
        }
    }

    // ---------------------------------------------------------------
    // Acciones
    // ---------------------------------------------------------------

    private void AbrirEnNavegador(string url)
    {
        try
        {
            Process.Start(new ProcessStartInfo { FileName = url, UseShellExecute = true });
        }
        catch (Exception ex)
        {
            Dialogo.Error(this, "No se pudo abrir el enlace", ex.Message);
        }
    }

    private static void AbrirCarpetaDe(string archivo)
    {
        try
        {
            // /select deja el archivo marcado dentro de su carpeta.
            Process.Start(new ProcessStartInfo
            {
                FileName = "explorer.exe",
                Arguments = $"/select,\"{archivo}\"",
                UseShellExecute = true
            });
        }
        catch
        {
            // Si el shell falla no hay nada útil que hacer.
        }
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

    /// <summary>Abre la ventana centrada sobre su padre.</summary>
    public static void Mostrar(IWin32Window? padre, BuildSettings settings)
    {
        using var f = new FormAcercaDe(settings);

        if (padre is Form p)
        {
            f.StartPosition = FormStartPosition.Manual;
            f.Location = new Point(
                p.Left + (p.Width - f.Width) / 2,
                p.Top + (p.Height - f.Height) / 2);
        }

        f.ShowDialog(padre);
    }
}
