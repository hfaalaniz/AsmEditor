namespace AsmEditor.Core.Disenador;

/// <summary>
/// Deshacer y rehacer del diseñador.
///
/// ⚠ GUARDA COPIAS COMPLETAS DEL FORMULARIO, SERIALIZADAS CON EL MISMO
/// <see cref="ArchivoFormulario.Serializar"/> QUE ESCRIBE EL .asmform. No se usa
/// <see cref="FormularioDisenado.Clonar"/> porque esa copia se mantiene a mano,
/// campo por campo: una propiedad nueva que alguien olvide agregarle se
/// perdería en silencio al deshacer. Lo que se serializa es, por definición,
/// todo lo que el formulario guarda.
///
/// ⚠ REGISTRA COMPARANDO ESTADOS, NO CONTANDO AVISOS. Al terminar cada acción
/// se llama a <see cref="Confirmar"/>, que compara contra el último estado
/// confirmado: si nada cambió no deja paso (un arrastre que vuelve a su lugar
/// no ensucia la historia), y si una acción futura se olvida de confirmar, su
/// cambio no se pierde: viaja en el paso siguiente.
///
/// No conoce WinForms: se prueba sin abrir una ventana.
/// </summary>
public sealed class HistorialDisenador
{
    /// <summary>Pasos que se recuerdan. Un formulario serializado pesa pocos KB.</summary>
    public const int LimitePorDefecto = 100;

    private readonly int _limite;

    // Se usan listas y no Stack<T> porque al pasar el límite hay que tirar el
    // paso MÁS VIEJO, que en una pila queda en el fondo.
    private readonly List<string> _deshacer = new();
    private readonly List<string> _rehacer = new();

    /// <summary>El estado que el historial da por vigente.</summary>
    private string _confirmado;

    /// <summary>El estado que está escrito en el disco, para saber si está sucio.</summary>
    private string _guardado;

    /// <param name="inicial">
    /// El formulario tal como se abrió. Cuenta como LIMPIO aunque sea uno nuevo
    /// sin guardar: es lo que ya hacía la pestaña, y un Ctrl+D que se cierra
    /// sin tocar no tiene que preguntar si se guarda.
    /// </param>
    public HistorialDisenador(FormularioDisenado inicial, int limite = LimitePorDefecto)
    {
        if (limite < 1) throw new ArgumentOutOfRangeException(nameof(limite));

        _limite = limite;
        _confirmado = ArchivoFormulario.Serializar(inicial);
        _guardado = _confirmado;
    }

    public bool PuedeDeshacer => _deshacer.Count > 0;

    public bool PuedeRehacer => _rehacer.Count > 0;

    public int PasosParaDeshacer => _deshacer.Count;

    public int PasosParaRehacer => _rehacer.Count;

    /// <summary>
    /// True si el estado confirmado no es el que está en el disco. Deshacer
    /// hasta lo guardado vuelve a dejarlo limpio.
    /// </summary>
    public bool EstaSucio => _confirmado != _guardado;

    /// <summary>
    /// Registra el estado actual como resultado de una acción terminada.
    /// Devuelve true si se agregó un paso (el estado había cambiado).
    ///
    /// Un paso nuevo borra el rehacer: después de un cambio, lo que se había
    /// deshecho ya no se sigue del estado actual.
    /// </summary>
    public bool Confirmar(FormularioDisenado actual)
    {
        var nuevo = ArchivoFormulario.Serializar(actual);

        if (nuevo == _confirmado) return false;

        _deshacer.Add(_confirmado);
        if (_deshacer.Count > _limite) _deshacer.RemoveAt(0);

        _rehacer.Clear();
        _confirmado = nuevo;

        return true;
    }

    /// <summary>
    /// El estado anterior al último paso, o null si no hay nada que deshacer.
    ///
    /// ⚠ DEVUELVE UNA COPIA NUEVA: quien la recibe la pone en el canvas y la
    /// edita, y eso no puede tocar lo que el historial tiene guardado.
    /// </summary>
    public FormularioDisenado? Deshacer()
    {
        if (_deshacer.Count == 0) return null;

        _rehacer.Add(_confirmado);
        _confirmado = Sacar(_deshacer);

        return ArchivoFormulario.Deserializar(_confirmado);
    }

    /// <summary>El estado del último paso deshecho, o null si no hay nada que rehacer.</summary>
    public FormularioDisenado? Rehacer()
    {
        if (_rehacer.Count == 0) return null;

        _deshacer.Add(_confirmado);
        _confirmado = Sacar(_rehacer);

        return ArchivoFormulario.Deserializar(_confirmado);
    }

    /// <summary>
    /// Deja constancia de que el formulario se escribió al disco en este
    /// estado. Confirma antes, por si quedó algún cambio sin confirmar: lo que
    /// se guarda es lo que está en pantalla.
    /// </summary>
    public void MarcarGuardado(FormularioDisenado actual)
    {
        Confirmar(actual);
        _guardado = _confirmado;
    }

    private static string Sacar(List<string> pila)
    {
        var ultimo = pila[^1];
        pila.RemoveAt(pila.Count - 1);
        return ultimo;
    }
}
