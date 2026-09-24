namespace AsmEditor;

/// <summary>
/// Los colores del tema para la ventana Opciones y sus páginas. Van por código
/// y no en el diseñador porque dependen del tema activo (regla del diseñador:
/// los colores que cambian en vivo quedan en el .cs).
///
/// Recorre los controles por tipo, así una página nueva queda con el tema sin
/// escribir nada. Las etiquetas cuyo Name empieza con "lblNota" son notas y
/// van en el gris de texto secundario.
/// </summary>
internal static class EstiloOpciones
{
    public static void Aplicar(Control raiz)
    {
        Pintar(raiz);
        foreach (Control hijo in raiz.Controls) Aplicar(hijo);
    }

    private static void Pintar(Control c)
    {
        switch (c)
        {
            case Label l:
                l.ForeColor = l.Name.StartsWith("lblNota", StringComparison.Ordinal) ? Tema.Texto3 : Tema.Texto;
                break;

            case TextBox t:
                t.BackColor = Tema.Superficie2;
                t.ForeColor = Tema.Texto;
                break;

            case ComboBox cb:
                cb.BackColor = Tema.Superficie2;
                cb.ForeColor = Tema.Texto;
                break;

            case Button b:
                b.BackColor = Tema.Superficie2;
                b.ForeColor = Tema.Texto;
                b.FlatAppearance.BorderColor = Tema.LineaSuave;
                b.FlatAppearance.MouseOverBackColor = Tema.Realzar(Tema.Superficie2, 14);
                break;

            case TreeView tv:
                tv.BackColor = Tema.Superficie;
                tv.ForeColor = Tema.Texto;
                tv.LineColor = Tema.Linea;
                break;

            case Form or UserControl or Panel:
                c.BackColor = Tema.Fondo;
                c.ForeColor = Tema.Texto;
                break;
        }
    }
}
