using System.Drawing.Drawing2D;

namespace AsmEditor;

/// <summary>
/// EL ARRANQUE, A LA VISTA.
///
/// Entre el doble clic y la ventana del editor pasan varias cosas a puertas
/// cerradas: leer la configuración, buscar los enlazadores, poblar el árbol del
/// proyecto y reabrir los archivos de la sesión anterior. Con una sesión de
/// varios archivos eso se vive como «hice doble clic y no pasó nada».
///
/// Esta ventana muestra el logo, la marca, en qué paso va y cuánto falta. Es
/// SÓLO INFORMATIVA: no pregunta, no se puede cancelar y se cierra sola.
///
/// ── LA BARRA DE PROGRESO ES SUAVIZADA, NO FALSA ──────────────────────────
/// Avanza a ritmo parejo HACIA el paso que el editor informó, y nunca lo
/// adelanta: si el arranque se traba en un paso, la barra llega hasta ahí y se
/// queda. Una barra que avanza sola sin relación con el trabajo es mentira; una
/// que salta de 20 a 90 de golpe se ve rota. Esto es lo que resuelve las dos.
///
/// ── SE QUEDA UN RATO DESPUÉS DE CARGAR ───────────────────────────────────
/// <see cref="PermanenciaTrasCargaMs"/>: el editor ya está listo detrás, pero
/// el splash sigue unos segundos para que se alcance a leer. Es una decisión de
/// presentación, no un retardo del programa.
///
/// ⚠ NO ROBA EL FOCO (ShowWithoutActivation) NI APARECE EN LA BARRA DE TAREAS.
/// Si lo hiciera, lo que el usuario esté escribiendo en otro programa se
/// perdería en el medio.
/// </summary>
public sealed class FormSplash : Form
{
    // ── Medidas ──────────────────────────────────────────────────────────
    private const int Ancho = 560;
    private const int Alto = 380;
    private const int LadoLogo = 104;

    /// <summary>Cuánto se queda a la vista después de que el editor está listo.</summary>
    public const int PermanenciaTrasCargaMs = 3000;

    /// <summary>Alto de la barra de progreso.</summary>
    private const int AltoBarra = 4;

    private const int MargenX = 36;

    // ── Estado del progreso ──────────────────────────────────────────────
    /// <summary>A dónde tiene que llegar la barra (0 a 1), según el paso informado.</summary>
    private float _objetivo;

    /// <summary>Dónde está dibujada ahora. Persigue a _objetivo.</summary>
    private float _actual;

    private const string TextoListo = "Listo";
    private string _paso = "Iniciando...";

    private readonly System.Windows.Forms.Timer _animacion = new() { Interval = 16 };
    private readonly System.Windows.Forms.Timer _cierreAutomatico = new() { Interval = 20000 };

    /// <summary>Cuántos pasos se esperan; de ahí sale cuánto avanza cada uno.</summary>
    private readonly int _pasosEsperados;
    private int _pasosInformados;

    public FormSplash(int pasosEsperados = 6)
    {
        _pasosEsperados = Math.Max(1, pasosEsperados);

        FormBorderStyle = FormBorderStyle.None;
        StartPosition = FormStartPosition.CenterScreen;
        ShowInTaskbar = false;
        ClientSize = new Size(Ancho, Alto);
        BackColor = Tema.Superficie;
        DoubleBuffered = true;

        _animacion.Tick += (_, _) => AnimarBarra();
        _animacion.Start();

        // Red de seguridad: si el arranque queda colgado, el splash no se queda
        // para siempre tapando la pantalla.
        _cierreAutomatico.Tick += (_, _) => Cerrar();
        _cierreAutomatico.Start();
    }

    /// <summary>
    /// Informa en qué paso del arranque va. Cada llamada mueve el objetivo de la
    /// barra un escalón; la animación se encarga de llegar sin saltos.
    /// </summary>
    public void Informar(string texto)
    {
        if (IsDisposed || !IsHandleCreated) return;

        if (InvokeRequired) { BeginInvoke(() => Informar(texto)); return; }

        _paso = texto;
        _pasosInformados++;

        // Tope en 0.92: el último tramo lo completa Completar(), así la barra no
        // llega al final antes de que el editor esté realmente listo.
        _objetivo = Math.Min(0.92f, (float)_pasosInformados / _pasosEsperados);

        Invalidate();
        // Refresh y no solo Invalidate: durante el arranque no se vuelve al
        // bucle de mensajes, así que sin esto no se vería ningún paso.
        Refresh();
    }

    /// <summary>
    /// El editor ya está listo: la barra se completa y arranca la permanencia.
    /// </summary>
    public void Completar()
    {
        if (IsDisposed || !IsHandleCreated) return;

        if (InvokeRequired) { BeginInvoke(Completar); return; }

        // ⚠ EL TEXTO NO CAMBIA ACÁ. La barra todavía está viajando hacia el
        // 100 %, y poner «Listo» junto al 75 % se lee como un error. El cambio
        // lo hace la animación cuando la barra llega de verdad.
        _objetivo = 1f;
        _completando = true;
        Invalidate();
    }

    /// <summary>True desde que el editor avisó que terminó de cargar.</summary>
    private bool _completando;

    /// <summary>
    /// Avance suave hacia el objetivo. La velocidad es proporcional a lo que
    /// falta, así frena al acercarse en vez de golpear el tope.
    /// </summary>
    private void AnimarBarra()
    {
        if (IsDisposed) return;

        float delta = _objetivo - _actual;

        if (Math.Abs(delta) < 0.004f)
        {
            if (_actual != _objetivo)
            {
                _actual = _objetivo;

                // Recién ahora que la barra llegó al final se dice «Listo».
                if (_completando && _paso != TextoListo) _paso = TextoListo;

                Invalidate();
            }
            return;
        }

        _actual += delta * 0.12f;
        Invalidate();
    }

    public void Cerrar()
    {
        _animacion.Stop();
        _cierreAutomatico.Stop();
        if (!IsDisposed) Close();
    }

    /// <summary>
    /// ⚠ QUE NO SE ACTIVE AL MOSTRARSE: sin esto el splash roba el foco del
    /// programa que el usuario esté usando mientras el editor levanta.
    /// </summary>
    protected override bool ShowWithoutActivation => true;

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);

        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

        DibujarCabecera(g);
        DibujarMarca(g);
        DibujarProgreso(g);
        DibujarPie(g);
        DibujarMarco(g);
    }

    /// <summary>Franja superior con el logo y el nombre, sobre fondo elevado.</summary>
    private void DibujarCabecera(Graphics g)
    {
        const int altoCabecera = 176;

        using (var fondo = new SolidBrush(Tema.Superficie2))
            g.FillRectangle(fondo, 0, 0, Ancho, altoCabecera);

        // Logo a la izquierda, texto a la derecha: es la disposición que se lee
        // en una pasada, más que el logo centrado con el nombre debajo.
        float logoY = (altoCabecera - LadoLogo) / 2f;
        LogoEditor.Dibujar(g, MargenX, logoY, LadoLogo, Tema.Acento, Tema.Texto3);

        float x = MargenX + LadoLogo + 26;

        using var brushTitulo = new SolidBrush(Tema.Texto);
        using var brushAcento = new SolidBrush(Tema.Acento);
        using var brushSub = new SolidBrush(Tema.Texto3);

        using var fTitulo = new Font("Segoe UI Light", 26f);
        using var fVersion = new Font("Segoe UI Semibold", 9.5f);
        using var fSub = new Font("Segoe UI", 8.5f);

        g.DrawString(InfoApp.NombreApp, fTitulo, brushTitulo, x, logoY + 6);

        // La versión en el acento, debajo del nombre.
        g.DrawString(InfoApp.Texto, fVersion, brushAcento, x + 3, logoY + 52);

        g.DrawString(InfoApp.Descripcion, fSub, brushSub, x + 3, logoY + 72);
        g.DrawString(InfoApp.Herramientas, fSub, brushSub, x + 3, logoY + 88);

        // Línea que separa la cabecera del cuerpo.
        using var linea = new Pen(Tema.LineaSuave);
        g.DrawLine(linea, 0, altoCabecera, Ancho, altoCabecera);
    }

    /// <summary>Los datos del autor, en el cuerpo.</summary>
    private void DibujarMarca(Graphics g)
    {
        float y = 200;

        using var brushDato = new SolidBrush(Tema.Texto2);
        using var brushEtiq = new SolidBrush(Tema.Texto3);
        using var brushSitio = new SolidBrush(Tema.Acento);

        using var fDato = new Font("Segoe UI", 9f);
        using var fAutor = new Font("Segoe UI Semibold", 10f);
        using var fSitio = new Font("Segoe UI", 9f);

        g.DrawString(InfoApp.Autor, fAutor, brushDato, MargenX, y);
        g.DrawString(InfoApp.Sitio, fSitio, brushSitio, MargenX, y + 20);
        g.DrawString(InfoApp.Ubicacion, fDato, brushEtiq, MargenX, y + 40);

        // El móvil a la derecha, alineado con el nombre.
        using var fmtDer = new StringFormat { Alignment = StringAlignment.Far };
        g.DrawString("Móvil  " + InfoApp.Movil, fDato, brushEtiq,
                     new RectangleF(0, y + 2, Ancho - MargenX, 20), fmtDer);
    }

    /// <summary>La barra de progreso y el paso actual.</summary>
    private void DibujarProgreso(Graphics g)
    {
        float y = Alto - 62;
        float ancho = Ancho - MargenX * 2;

        // Canal de fondo
        using (var fondo = new SolidBrush(Tema.Superficie3))
        using (var path = Redondeado(new RectangleF(MargenX, y, ancho, AltoBarra), AltoBarra / 2f))
            g.FillPath(fondo, path);

        // Relleno, con un degradado del acento para que no sea un bloque plano
        float llenado = Math.Max(0f, Math.Min(1f, _actual)) * ancho;
        if (llenado > 2)
        {
            var rect = new RectangleF(MargenX, y, llenado, AltoBarra);
            using var degradado = new LinearGradientBrush(
                new RectangleF(MargenX, y, ancho, AltoBarra),
                Tema.AcentoTenue, Tema.Acento, LinearGradientMode.Horizontal);
            using var path = Redondeado(rect, AltoBarra / 2f);
            g.FillPath(degradado, path);
        }

        // El paso, debajo a la izquierda; el porcentaje a la derecha.
        using var brushPaso = new SolidBrush(Tema.Texto3);
        using var fPaso = new Font("Segoe UI", 8.5f);

        g.DrawString(_paso, fPaso, brushPaso, MargenX - 2, y + 12);

        using var fmtDer = new StringFormat { Alignment = StringAlignment.Far };
        g.DrawString($"{(int)Math.Round(_actual * 100)}%", fPaso, brushPaso,
                     new RectangleF(0, y + 12, Ancho - MargenX + 2, 18), fmtDer);
    }

    private void DibujarPie(Graphics g)
    {
        using var brush = new SolidBrush(Tema.Texto3);
        using var f = new Font("Segoe UI", 7.5f);
        using var fmt = new StringFormat { Alignment = StringAlignment.Center };

        g.DrawString(InfoApp.Copyright, f, brush,
                     new RectangleF(0, Alto - 20, Ancho, 16), fmt);
    }

    private void DibujarMarco(Graphics g)
    {
        using var pen = new Pen(Tema.Linea, 1);
        g.DrawRectangle(pen, 0, 0, ClientSize.Width - 1, ClientSize.Height - 1);

        // Franja de acento arriba: la misma marca que la pestaña activa.
        using var acento = new SolidBrush(Tema.Acento);
        g.FillRectangle(acento, 1, 1, ClientSize.Width - 2, 3);
    }

    private static GraphicsPath Redondeado(RectangleF r, float radio)
    {
        var p = new GraphicsPath();
        float d = Math.Min(radio * 2, Math.Min(r.Width, r.Height));
        if (d <= 0) { p.AddRectangle(r); return p; }

        p.AddArc(r.X, r.Y, d, d, 180, 90);
        p.AddArc(r.Right - d, r.Y, d, d, 270, 90);
        p.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
        p.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
        p.CloseFigure();
        return p;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _animacion.Dispose();
            _cierreAutomatico.Dispose();
        }
        base.Dispose(disposing);
    }
}
