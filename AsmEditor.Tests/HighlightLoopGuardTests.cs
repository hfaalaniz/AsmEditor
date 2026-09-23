namespace AsmEditor.Tests;

/// <summary>
/// El resaltado de sintaxis usa SelectionColor, que dispara TextChanged aunque el
/// texto no cambie. Si el manejador de TextChanged no se protege, rearma el timer de
/// resaltado y se entra en un bucle infinito que marca el documento como sucio solo.
///
/// Esa regresión ocurrió de verdad: el documento recién abierto aparecía con '*' y el
/// proceso quemaba CPU de forma permanente sin que nadie tocara el teclado.
///
/// El control es WinForms y no se puede instanciar acá, así que lo que se prueba es la
/// mecánica de la guarda: reentrar mientras se está pintando no debe volver a contar
/// como una edición del usuario.
/// </summary>
public class HighlightLoopGuardTests
{
    /// <summary>
    /// Reproduce el patrón de AsmDocumentControl. El rearme del timer se modela como
    /// una cola de trabajo y no como recursión: en WinForms el timer no reentra en la
    /// pila, y así una regresión falla la prueba en vez de desbordar la pila y tumbar
    /// el runner completo.
    /// </summary>
    private sealed class EditorSimulado
    {
        private readonly bool _conGuarda;
        private bool _estaPintando;

        /// <summary>Pintados pendientes, como los ticks que el timer todavía debe entregar.</summary>
        private int _pintadosPendientes;

        public int VecesMarcadoSucio { get; private set; }
        public int VecesPintado { get; private set; }

        /// <summary>Tope: sin él una regresión no terminaría nunca.</summary>
        private const int Tope = 5_000;

        public EditorSimulado(bool conGuarda) => _conGuarda = conGuarda;

        /// <summary>Pide un pintado y procesa la cola hasta que se agote (o se alcance el tope).</summary>
        public void Pintar()
        {
            _pintadosPendientes++;

            while (_pintadosPendientes > 0 && VecesPintado < Tope)
            {
                _pintadosPendientes--;
                PintarUnaVez();
            }
        }

        /// <summary>Equivale a ApplyHighlight: pinta y, al pintar, dispara TextChanged.</summary>
        private void PintarUnaVez()
        {
            if (_conGuarda && _estaPintando) return;

            _estaPintando = true;
            try
            {
                VecesPintado++;
                OnTextChanged();
            }
            finally
            {
                _estaPintando = false;
            }
        }

        /// <summary>Equivale a OnEditorTextChanged: marca sucio y rearma el timer de resaltado.</summary>
        private void OnTextChanged()
        {
            // Esta es la guarda que se agregó en AsmDocumentControl.
            if (_conGuarda && _estaPintando) return;

            VecesMarcadoSucio++;
            _pintadosPendientes++;   // el timer se rearma
        }
    }

    [Fact]
    public void ConLaGuarda_PintarNoMarcaElDocumentoComoSucio()
    {
        var editor = new EditorSimulado(conGuarda: true);

        editor.Pintar();

        Assert.Equal(1, editor.VecesPintado);
        Assert.Equal(0, editor.VecesMarcadoSucio);
    }

    [Fact]
    public void SinLaGuarda_ElPintadoSeRealimentaSinFin()
    {
        // Documenta la regresión: sin guarda, un solo pintado no se detiene más.
        var editor = new EditorSimulado(conGuarda: false);

        editor.Pintar();

        Assert.True(editor.VecesMarcadoSucio > 100,
            $"Se esperaba una realimentación descontrolada, hubo {editor.VecesMarcadoSucio}.");
    }
}
