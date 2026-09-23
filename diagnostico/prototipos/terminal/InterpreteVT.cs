using System.Text;

namespace PruebaTerminal;

/// <summary>
/// Traduce lo que pinta el programa (texto con secuencias de escape VT, como
/// las manda ConPTY) a cambios en la <see cref="PantallaTerminal"/>.
///
/// Cubre lo que usan PowerShell, PSReadLine y los programas de consola
/// habituales: texto, CR/LF/BS/TAB, colores SGR (16, 256 y RGB), movimiento del
/// cursor (CUP, CUU/CUD/CUF/CUB, CHA, VPA), borrado (EL, ED, ECH), insertar y
/// borrar caracteres (ICH, DCH), visibilidad del cursor, guardar/restaurar
/// cursor y el título (OSC 0/2). Lo que no reconoce lo IGNORA sin romper el
/// estado: una secuencia desconocida no puede dejar la terminal "comiéndose"
/// el texto que sigue.
/// </summary>
public sealed class InterpreteVT
{
    private enum Estado { Normal, Esc, Csi, Osc, EscIntermedio }

    private readonly PantallaTerminal _p;

    /// <summary>
    /// ⚠ UTF-8 CON ESTADO: un carácter de varios bytes puede venir partido
    /// entre dos lecturas del caño. Un Decoder guarda la mitad pendiente; un
    /// GetString por lectura rompería los acentos al azar.
    /// </summary>
    private readonly Decoder _utf8 = Encoding.UTF8.GetDecoder();

    private Estado _estado = Estado.Normal;
    private readonly StringBuilder _parametros = new();
    private readonly StringBuilder _osc = new();
    private bool _privado;     // CSI ? ...
    private int _filaGuardada, _columnaGuardada;

    public InterpreteVT(PantallaTerminal pantalla) => _p = pantalla;

    public void Procesar(byte[] datos, int cantidad)
    {
        var chars = new char[_utf8.GetCharCount(datos, 0, cantidad)];
        _utf8.GetChars(datos, 0, cantidad, chars, 0);

        lock (_p.Candado)
        {
            foreach (var c in chars) Procesar(c);
        }
    }

    private void Procesar(char c)
    {
        switch (_estado)
        {
            case Estado.Normal: Normal(c); break;
            case Estado.Esc: Escape(c); break;
            case Estado.Csi: Csi(c); break;
            case Estado.Osc: Osc(c); break;
            case Estado.EscIntermedio: _estado = Estado.Normal; break;   // ESC ( B y similares: se ignora
        }
    }

    private void Normal(char c)
    {
        switch (c)
        {
            case '\x1b': _estado = Estado.Esc; break;
            case '\r': _p.CursorColumna = 0; _p.SaltoPendiente = false; break;
            case '\n': _p.SaltoPendiente = false; _p.SaltoDeLinea(); break;
            case '\b': if (_p.CursorColumna > 0) _p.CursorColumna--; _p.SaltoPendiente = false; break;
            case '\t': _p.MoverCursor(_p.CursorFila, Math.Min(_p.Columnas - 1, (_p.CursorColumna / 8 + 1) * 8)); break;
            case '\a': break;
            default:
                if (c >= ' ') _p.Escribir(c);
                break;
        }
    }

    private void Escape(char c)
    {
        switch (c)
        {
            case '[': _estado = Estado.Csi; _parametros.Clear(); _privado = false; return;
            case ']': _estado = Estado.Osc; _osc.Clear(); return;
            case '7': _filaGuardada = _p.CursorFila; _columnaGuardada = _p.CursorColumna; break;
            case '8': _p.MoverCursor(_filaGuardada, _columnaGuardada); break;
            case '(': case ')': case '#': _estado = Estado.EscIntermedio; return;
            case 'M':   // índice inverso: sube una línea
                if (_p.CursorFila > 0) _p.CursorFila--;
                break;
        }

        _estado = Estado.Normal;
    }

    private void Csi(char c)
    {
        if (c == '?') { _privado = true; return; }

        if ((c >= '0' && c <= '9') || c == ';' || c == ':')
        {
            _parametros.Append(c);
            return;
        }

        // Bytes intermedios (' ', '!', '>'...): se aceptan y se ignoran.
        if (c >= ' ' && c <= '/') return;
        if (c == '>' || c == '=' || c == '<') return;

        EjecutarCsi(c, LeerParametros());
        _estado = Estado.Normal;
    }

    private int[] LeerParametros()
    {
        if (_parametros.Length == 0) return Array.Empty<int>();

        var partes = _parametros.ToString().Split(';', ':');
        var r = new int[partes.Length];
        for (int i = 0; i < partes.Length; i++) r[i] = int.TryParse(partes[i], out var v) ? v : 0;
        return r;
    }

    private static int P(int[] ps, int i, int porDefecto) => i < ps.Length && ps[i] != 0 ? ps[i] : porDefecto;

    private void EjecutarCsi(char final, int[] ps)
    {
        if (_privado)
        {
            // ?25h / ?25l: mostrar / ocultar el cursor. El resto (?1049, ?9001,
            // ?1004...) se ignora: esta terminal no tiene pantalla alternativa.
            if (ps.Length > 0 && ps[0] == 25 && (final == 'h' || final == 'l')) _p.CursorVisible = final == 'h';
            return;
        }

        switch (final)
        {
            case 'm': Sgr(ps); break;

            case 'H':
            case 'f': _p.MoverCursor(P(ps, 0, 1) - 1, P(ps, 1, 1) - 1); break;

            case 'A': _p.MoverCursor(_p.CursorFila - P(ps, 0, 1), _p.CursorColumna); break;
            case 'B': _p.MoverCursor(_p.CursorFila + P(ps, 0, 1), _p.CursorColumna); break;
            case 'C': _p.MoverCursor(_p.CursorFila, _p.CursorColumna + P(ps, 0, 1)); break;
            case 'D': _p.MoverCursor(_p.CursorFila, _p.CursorColumna - P(ps, 0, 1)); break;
            case 'G': _p.MoverCursor(_p.CursorFila, P(ps, 0, 1) - 1); break;
            case 'd': _p.MoverCursor(P(ps, 0, 1) - 1, _p.CursorColumna); break;

            case 'K':   // borrar en la línea
                switch (ps.Length > 0 ? ps[0] : 0)
                {
                    case 0: _p.Limpiar(_p.CursorFila, _p.CursorColumna, _p.CursorFila, _p.Columnas - 1); break;
                    case 1: _p.Limpiar(_p.CursorFila, 0, _p.CursorFila, _p.CursorColumna); break;
                    case 2: _p.Limpiar(_p.CursorFila, 0, _p.CursorFila, _p.Columnas - 1); break;
                }
                break;

            case 'J':   // borrar en la pantalla
                switch (ps.Length > 0 ? ps[0] : 0)
                {
                    case 0:
                        _p.Limpiar(_p.CursorFila, _p.CursorColumna, _p.CursorFila, _p.Columnas - 1);
                        _p.Limpiar(_p.CursorFila + 1, 0, _p.Filas - 1, _p.Columnas - 1);
                        break;
                    case 1:
                        _p.Limpiar(0, 0, _p.CursorFila - 1, _p.Columnas - 1);
                        _p.Limpiar(_p.CursorFila, 0, _p.CursorFila, _p.CursorColumna);
                        break;
                    case 2:
                    case 3:
                        _p.Limpiar(0, 0, _p.Filas - 1, _p.Columnas - 1);
                        break;
                }
                break;

            case 'X': _p.Limpiar(_p.CursorFila, _p.CursorColumna, _p.CursorFila, _p.CursorColumna + P(ps, 0, 1) - 1); break;
            case 'P': _p.BorrarCaracteres(P(ps, 0, 1)); break;
            case '@': _p.InsertarCaracteres(P(ps, 0, 1)); break;
            case 'S': _p.Desplazar(P(ps, 0, 1)); break;
        }
    }

    /// <summary>Colores y negrita (Select Graphic Rendition).</summary>
    private void Sgr(int[] ps)
    {
        if (ps.Length == 0) ps = new[] { 0 };

        for (int i = 0; i < ps.Length; i++)
        {
            int v = ps[i];

            switch (v)
            {
                case 0:
                    _p.FrenteActual = PantallaTerminal.FrentePorDefecto;
                    _p.FondoActual = PantallaTerminal.FondoPorDefecto;
                    _p.NegritaActual = false;
                    break;
                case 1: _p.NegritaActual = true; break;
                case 22: _p.NegritaActual = false; break;
                case >= 30 and <= 37: _p.FrenteActual = v - 30; break;
                case 39: _p.FrenteActual = PantallaTerminal.FrentePorDefecto; break;
                case >= 40 and <= 47: _p.FondoActual = v - 40; break;
                case 49: _p.FondoActual = PantallaTerminal.FondoPorDefecto; break;
                case >= 90 and <= 97: _p.FrenteActual = v - 90 + 8; break;
                case >= 100 and <= 107: _p.FondoActual = v - 100 + 8; break;

                case 38:
                case 48:
                    // 38;5;n (paleta de 256) o 38;2;r;g;b (color directo).
                    int color;
                    if (i + 2 < ps.Length && ps[i + 1] == 5) { color = ps[i + 2]; i += 2; }
                    else if (i + 4 < ps.Length && ps[i + 1] == 2)
                    {
                        color = PantallaTerminal.MarcaRgb | (ps[i + 2] << 16) | (ps[i + 3] << 8) | ps[i + 4];
                        i += 4;
                    }
                    else continue;

                    if (v == 38) _p.FrenteActual = color; else _p.FondoActual = color;
                    break;
            }
        }
    }

    private void Osc(char c)
    {
        // Termina con BEL o con ESC \ (ST).
        if (c == '\a' || c == '\x1b')
        {
            var s = _osc.ToString();
            int pc = s.IndexOf(';');

            if (pc > 0 && (s.StartsWith("0;") || s.StartsWith("2;"))) _p.Titulo = s[(pc + 1)..];

            _estado = c == '\x1b' ? Estado.EscIntermedio : Estado.Normal;
            return;
        }

        if (_osc.Length < 4096) _osc.Append(c);
    }
}
