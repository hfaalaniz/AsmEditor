using AsmEditor.Core;

namespace AsmEditor;

/// <summary>
/// Una página de Configuración → Opciones.
///
/// ⚠ CÓMO SE AGREGA UNA PÁGINA: un UserControl nuevo, con su Designer.cs, que
/// implemente esta interfaz; se lo arrastra al panel pnlPaginas de
/// FormOpciones en el diseñador (Location 0,0 y el tamaño del panel). Nada
/// más: FormOpciones busca las páginas en ese panel y arma el árbol solo, con
/// <see cref="Categoria"/> y <see cref="Titulo"/>, en el orden de
/// <see cref="Orden"/>. Una opción nueva lleva además su propiedad en
/// Core\ValoresOpciones.cs (ver el aviso ahí).
///
/// La página no toca la configuración: carga de una copia y guarda en la
/// copia. La ventana la aplica solo si se acepta.
/// </summary>
public interface IPaginaOpciones
{
    /// <summary>El nodo padre en el árbol ("Entorno", "Herramientas").</summary>
    string Categoria { get; }

    /// <summary>El nodo de la página y el título que se muestra arriba.</summary>
    string Titulo { get; }

    /// <summary>Orden en el árbol; las categorías siguen a su primera página.</summary>
    int Orden { get; }

    void Cargar(ValoresOpciones valores);

    void Guardar(ValoresOpciones valores);
}
