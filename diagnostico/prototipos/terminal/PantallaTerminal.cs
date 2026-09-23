namespace PruebaTerminal;

/// <summary>Un casillero de la grilla: el carácter y cómo se pinta.</summary>
public struct Celda
{
    public char Caracter;
    public int Frente;     // índice de paleta (0-255) o color RGB con el bit 24 prendido
    public int Fondo;
    public bool Negrita;

    public static Celda Vacia(int frente, int fondo) => new() { Caracter = ' ', Frente = frente, Fondo = fondo };
}

/// <summary>
/// La pantalla de la terminal: una grilla de columnas × filas con el cursor.
/// Es lo que <see cref="InterpreteVT"/> va modificando y lo que el control
/// dibuja. No sabe nada de secuencias de escape ni de ventanas.
///
/// ⚠ SE USA DESDE DOS HILOS (el lector de ConPTY escribe, la interfaz lee):
/// todo acceso va con <see cref="Candado"/>.
/// </summary>
public sealed class PantallaTerminal
{
    public const int FrentePorDefecto = 7;    // gris claro de la paleta ANSI
    public const int FondoPorDefecto = 0;     // negro

    /// <summary>Marca de color RGB directo (sin la marca, es índice de paleta).</summary>
    public const int MarcaRgb = 1 << 24;

    public object Candado { get; } = new();

    private Celda[,] _celdas;

    public int Columnas { get; private set; }
    public int Filas { get; private set; }

    public int CursorFila { get; set; }
    public int CursorColumna { get; set; }
    public bool CursorVisible { get; set; } = true;

    // Atributos con los que se escriben los caracteres nuevos (SGR).
    public int FrenteActual { get; set; } = FrentePorDefecto;
    public int FondoActual { get; set; } = FondoPorDefecto;
    public bool NegritaActual { get; set; }

    /// <summary>El título que pidió el programa (OSC 0/2).</summary>
    public string Titulo { get; set; } = "";

    /// <summary>
    /// Pendiente de salto: se escribió en la última columna y el siguiente
    /// carácter va a la línea de abajo (así lo hacen las terminales VT; si se
    /// saltara enseguida, un texto que ocupa justo el ancho dejaría una línea
    /// vacía de más).
    /// </summary>
    public bool SaltoPendiente { get; set; }

    public PantallaTerminal(int columnas, int filas)
    {
        Columnas = columnas;
        Filas = filas;
        _celdas = new Celda[filas, columnas];
        Limpiar(0, 0, filas - 1, columnas - 1);
    }

    public Celda this[int fila, int columna] => _celdas[fila, columna];

    /// <summary>Escribe un carácter en el cursor y avanza.</summary>
    public void Escribir(char c)
    {
        if (SaltoPendiente)
        {
            SaltoPendiente = false;
            CursorColumna = 0;
            SaltoDeLinea();
        }

        _celdas[CursorFila, CursorColumna] = new Celda
        {
            Caracter = c, Frente = FrenteActual, Fondo = FondoActual, Negrita = NegritaActual
        };

        if (CursorColumna == Columnas - 1) SaltoPendiente = true;
        else CursorColumna++;
    }

    /// <summary>Baja una línea; en la última, sube todo el contenido.</summary>
    public void SaltoDeLinea()
    {
        if (CursorFila < Filas - 1) { CursorFila++; return; }
        Desplazar(1);
    }

    /// <summary>Sube el contenido <paramref name="n"/> líneas (lo de arriba se pierde).</summary>
    public void Desplazar(int n)
    {
        for (int f = 0; f < Filas; f++)
            for (int c = 0; c < Columnas; c++)
                _celdas[f, c] = f + n < Filas ? _celdas[f + n, c] : Celda.Vacia(FrenteActual, FondoActual);
    }

    public void MoverCursor(int fila, int columna)
    {
        CursorFila = Math.Clamp(fila, 0, Filas - 1);
        CursorColumna = Math.Clamp(columna, 0, Columnas - 1);
        SaltoPendiente = false;
    }

    /// <summary>Limpia el rectángulo (inclusive) con el fondo actual.</summary>
    public void Limpiar(int fila1, int col1, int fila2, int col2)
    {
        for (int f = Math.Max(0, fila1); f <= Math.Min(Filas - 1, fila2); f++)
            for (int c = Math.Max(0, col1); c <= Math.Min(Columnas - 1, col2); c++)
                _celdas[f, c] = Celda.Vacia(FrenteActual, FondoActual);
    }

    /// <summary>Borra <paramref name="n"/> caracteres en el cursor, corriendo el resto a la izquierda.</summary>
    public void BorrarCaracteres(int n)
    {
        int f = CursorFila;
        for (int c = CursorColumna; c < Columnas; c++)
            _celdas[f, c] = c + n < Columnas ? _celdas[f, c + n] : Celda.Vacia(FrenteActual, FondoActual);
    }

    /// <summary>Inserta <paramref name="n"/> espacios en el cursor, corriendo el resto a la derecha.</summary>
    public void InsertarCaracteres(int n)
    {
        int f = CursorFila;
        for (int c = Columnas - 1; c >= CursorColumna; c--)
            _celdas[f, c] = c - n >= CursorColumna ? _celdas[f, c - n] : Celda.Vacia(FrenteActual, FondoActual);
    }

    /// <summary>Cambia el tamaño conservando lo que entra (esquina superior izquierda).</summary>
    public void Redimensionar(int columnas, int filas)
    {
        var nuevas = new Celda[filas, columnas];

        for (int f = 0; f < filas; f++)
            for (int c = 0; c < columnas; c++)
                nuevas[f, c] = f < Filas && c < Columnas ? _celdas[f, c] : Celda.Vacia(FrentePorDefecto, FondoPorDefecto);

        _celdas = nuevas;
        Columnas = columnas;
        Filas = filas;
        MoverCursor(CursorFila, CursorColumna);
    }

    /// <summary>El texto de una fila, sin los espacios del final (para las pruebas).</summary>
    public string TextoDeFila(int fila)
    {
        var chars = new char[Columnas];
        for (int c = 0; c < Columnas; c++) chars[c] = _celdas[fila, c].Caracter;
        return new string(chars).TrimEnd();
    }

    /// <summary>Toda la pantalla como texto, una fila por línea.</summary>
    public string TodoElTexto()
    {
        var filas = new string[Filas];
        for (int f = 0; f < Filas; f++) filas[f] = TextoDeFila(f);
        return string.Join("\n", filas);
    }
}
