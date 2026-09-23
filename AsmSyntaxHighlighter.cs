using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

namespace AsmEditor;

/// <summary>
/// Resaltado de sintaxis simple para NASM: instrucciones, registros, directivas,
/// números, strings, comentarios y etiquetas. No es un parser real, es basado
/// en expresiones regulares — suficiente para lectura cómoda, no para validación.
/// </summary>
public static class AsmSyntaxHighlighter
{
    private const int WM_SETREDRAW = 0x000B;

    [DllImport("user32.dll")]
    private static extern IntPtr SendMessage(IntPtr hWnd, int msg, bool wParam, int lParam);

    private static readonly string[] Keywords =
    {
        "mov","lea","call","ret","push","pop","jmp","je","jne","jz","jnz","jg","jl","jge","jle",
        "cmp","test","add","sub","mul","imul","div","idiv","and","or","xor","not","neg",
        "shl","shr","sal","sar","inc","dec","nop","loop","rep","repe","repne",
        "movsb","movsd","movsq","stosb","stosd","stosq","cld","std","syscall","int","proc","endp"
    };

    private static readonly string[] Directives =
    {
        "section", "global", "extern", "default", "rel", "bits", "org", "align",
        "db", "dw", "dd", "dq", "dt", "resb", "resw", "resd", "resq", "rest"
    };

    private static readonly string[] Registers =
    {
        "rax","rbx","rcx","rdx","rsi","rdi","rbp","rsp",
        "r8","r9","r10","r11","r12","r13","r14","r15",
        "r8d","r9d","r10d","r11d","r12d","r13d","r14d","r15d",
        "eax","ebx","ecx","edx","esi","edi","ebp","esp",
        "ax","bx","cx","dx","si","di","bp","sp",
        "al","bl","cl","dl","ah","bh","ch","dh"
    };

    public static void Highlight(RichTextBox rtb)
    {
        int selStart = rtb.SelectionStart;
        int selLength = rtb.SelectionLength;

        SendMessage(rtb.Handle, WM_SETREDRAW, false, 0);

        // Los colores salen del tema activo: así el resaltado sigue al tema
        // claro/oscuro sin tener una paleta propia que se desincronice.
        rtb.SelectAll();
        rtb.SelectionColor = Tema.CodigoTexto;

        ApplyPattern(rtb, @"\b(" + string.Join("|", Keywords) + @")\b", Tema.CodigoInstruccion, RegexOptions.IgnoreCase);
        ApplyPattern(rtb, @"\b(" + string.Join("|", Directives) + @")\b", Tema.CodigoDirectiva, RegexOptions.IgnoreCase);
        ApplyPattern(rtb, @"\b(" + string.Join("|", Registers) + @")\b", Tema.CodigoRegistro, RegexOptions.IgnoreCase);
        ApplyPattern(rtb, @"^\s*[A-Za-z_.$][A-Za-z0-9_.$]*:", Tema.CodigoEtiqueta, RegexOptions.Multiline);
        ApplyPattern(rtb, @"\b0[xX][0-9a-fA-F]+\b|\b\d+\b", Tema.CodigoNumero, RegexOptions.None);
        ApplyPattern(rtb, "\"[^\"]*\"|'[^']*'", Tema.CodigoCadena, RegexOptions.None);
        ApplyPattern(rtb, ";.*$", Tema.CodigoComentario, RegexOptions.Multiline);

        rtb.SelectionStart = selStart;
        rtb.SelectionLength = selLength;

        SendMessage(rtb.Handle, WM_SETREDRAW, true, 0);
        rtb.Invalidate();
    }

    private static void ApplyPattern(RichTextBox rtb, string pattern, Color color, RegexOptions options)
    {
        foreach (Match m in Regex.Matches(rtb.Text, pattern, options))
        {
            rtb.Select(m.Index, m.Length);
            rtb.SelectionColor = color;
        }
    }
}
