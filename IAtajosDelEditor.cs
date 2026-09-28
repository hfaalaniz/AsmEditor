namespace AsmEditor;

/// <summary>
/// La ventana principal ejecuta un atajo de su menú que llegó a OTRA ventana
/// del editor (una flotante del acople, 3d): así Ctrl+S, F7, Ctrl+B... andan
/// también con el foco ahí.
/// </summary>
public interface IAtajosDelEditor
{
    /// <summary>True si el atajo era de un comando y se ejecutó.</summary>
    bool EjecutarAtajo(Keys teclas);
}
