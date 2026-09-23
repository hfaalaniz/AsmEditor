using AsmEditor.Core;

namespace AsmEditor;

/// <summary>
/// Lo que MainForm necesita de CUALQUIER pestaña, sea de código o de diseño.
///
/// ⚠ EXISTE PORQUE LAS PESTAÑAS DEJARON DE SER TODAS DE TEXTO. Antes cada
/// consumidor hacía <c>Controls.OfType&lt;AsmDocumentControl&gt;()</c> y daba
/// por hecho que había un editor de texto adentro. Con el diseñador metido en
/// una pestaña eso deja de ser cierto, y los lugares que recorren pestañas
/// —cerrar, guardar, preguntar por los sucios al salir, restaurar la sesión—
/// tienen que seguir viéndolas TODAS, o una pestaña de diseño se volvería
/// imposible de cerrar y sus cambios se perderían sin aviso.
///
/// Las capacidades que no todas tienen (buscar, deshacer, ir a una línea) se
/// preguntan con las propiedades <c>Admite*</c> en vez de asumirse.
/// </summary>
public interface IPestanaEditor
{
    /// <summary>Estado lógico: ruta, si está sucio, nombre para la pestaña.</summary>
    DocumentState State { get; }

    string? FilePath { get; }

    bool IsDirty { get; }

    /// <summary>Guarda en su ruta actual. False si no se pudo.</summary>
    bool Guardar();

    /// <summary>Pregunta la ruta y guarda ahí. False si se canceló o falló.</summary>
    bool GuardarComo(IWin32Window duenio);

    /// <summary>Le da el foco a lo que corresponda dentro de la pestaña.</summary>
    void TomarFoco();

    /// <summary>Cambia al cambiar el estado sucio o la ruta, para refrescar el título.</summary>
    event EventHandler? DirtyChanged;

    // ---- Capacidades ----
    //
    // Se preguntan en vez de asumirse: un diseñador no tiene líneas ni texto
    // que buscar, y ofrecer Ctrl+F ahí sería ofrecer algo que no hace nada.

    /// <summary>True si tiene texto donde buscar y reemplazar.</summary>
    bool AdmiteBusqueda { get; }

    /// <summary>True si tiene su propio deshacer/rehacer.</summary>
    bool AdmiteDeshacer { get; }

    /// <summary>True si se puede saltar a una línea (panel de errores).</summary>
    bool AdmiteIrALinea { get; }

    void Deshacer();
    void Rehacer();

    /// <summary>Posición del cursor para la barra de estado, o null si no aplica.</summary>
    (int Linea, int Columna)? PosicionCursor { get; }
}
