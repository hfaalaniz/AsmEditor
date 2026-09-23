using System.Text.RegularExpressions;

namespace AsmEditor.Core;

public enum CompletionKind
{
    Instruction,
    Register,
    Directive,
    Label
}

public sealed record CompletionItem(string Text, CompletionKind Kind)
{
    /// <summary>Etiqueta corta que se muestra a la derecha en el popup.</summary>
    public string KindLabel => Kind switch
    {
        CompletionKind.Instruction => "instr",
        CompletionKind.Register => "reg",
        CompletionKind.Directive => "dir",
        CompletionKind.Label => "etiq",
        _ => ""
    };
}

/// <summary>
/// Fuente de sugerencias para el autocompletado: instrucciones, registros y directivas
/// de NASM x86-64, más las etiquetas definidas en el propio archivo que se está editando.
/// Sin dependencias de UI para poder probarse.
/// </summary>
public static class AsmCompletion
{
    public static readonly string[] Instructions =
    {
        "adc","add","and","bsf","bsr","bswap","bt","btc","btr","bts",
        "call","cbw","cdq","cdqe","clc","cld","cli","cmc","cmp","cmpsb","cmpsd","cmpsq","cmpsw",
        "cmpxchg","cpuid","cqo","cwd","cwde","dec","div","enter","hlt","idiv","imul","in","inc",
        "int","int3","iret","iretq",
        "ja","jae","jb","jbe","jc","jcxz","je","jecxz","jg","jge","jl","jle","jmp","jna","jnae",
        "jnb","jnbe","jnc","jne","jng","jnge","jnl","jnle","jno","jnp","jns","jnz","jo","jp",
        "jpe","jpo","jrcxz","js","jz","lahf","lea","leave","lodsb","lodsd","lodsq","lodsw",
        "loop","loope","loopne","loopnz","loopz","mov","movsb","movsd","movsq","movsw",
        "movsx","movsxd","movzx","mul","neg","nop","not","or","out","pop","popf","popfq",
        "push","pushf","pushfq","rcl","rcr","rdtsc","rep","repe","repne","repnz","repz",
        "ret","retn","rol","ror","sahf","sal","sar","sbb","scasb","scasd","scasq","scasw",
        "seta","setae","setb","setbe","setc","sete","setg","setge","setl","setle","setna",
        "setnb","setnc","setne","setng","setnl","setno","setnp","setns","setnz","seto","setp",
        "sets","setz","shl","shld","shr","shrd","stc","std","sti","stosb","stosd","stosq",
        "stosw","sub","syscall","test","ud2","wait","xadd","xchg","xlatb","xor",
        "addsd","addss","comisd","comiss","cvtsi2sd","cvtsi2ss","cvtsd2si","cvtss2si",
        "divsd","divss","movaps","movd","movdqa","movdqu","movq","movss","mulsd","mulss",
        "pxor","subsd","subss","ucomisd","ucomiss","xorps","xorpd"
    };

    public static readonly string[] Registers =
    {
        "rax","rbx","rcx","rdx","rsi","rdi","rbp","rsp","rip",
        "r8","r9","r10","r11","r12","r13","r14","r15",
        "eax","ebx","ecx","edx","esi","edi","ebp","esp",
        "r8d","r9d","r10d","r11d","r12d","r13d","r14d","r15d",
        "ax","bx","cx","dx","si","di","bp","sp",
        "r8w","r9w","r10w","r11w","r12w","r13w","r14w","r15w",
        "al","bl","cl","dl","ah","bh","ch","dh","sil","dil","bpl","spl",
        "r8b","r9b","r10b","r11b","r12b","r13b","r14b","r15b",
        "cs","ds","es","fs","gs","ss",
        "xmm0","xmm1","xmm2","xmm3","xmm4","xmm5","xmm6","xmm7",
        "xmm8","xmm9","xmm10","xmm11","xmm12","xmm13","xmm14","xmm15"
    };

    public static readonly string[] Directives =
    {
        "section","segment","global","extern","default","rel","abs","bits","org","align","alignb",
        "db","dw","dd","dq","dt","resb","resw","resd","resq","rest",
        "equ","times","incbin","%include","%define","%undef","%assign","%macro","%endmacro",
        "%if","%ifdef","%ifndef","%else","%elif","%endif","%rep","%endrep","%error","%warning",
        "byte","word","dword","qword","tword","oword","yword","ptr","strict","nosplit",
        "proc","endp","struc","endstruc","istruc","iend","at","common","cpu","use32","use64"
    };

    // Etiquetas NASM: al inicio de línea, opcionalmente con punto (locales), con ':' al final.
    private static readonly Regex LabelWithColon = new(
        @"^[ \t]*(?<name>[A-Za-z_.?$][A-Za-z0-9_.?$@~#]*)[ \t]*:",
        RegexOptions.Multiline | RegexOptions.Compiled);

    // También las que definen datos sin ':' seguidas de db/dw/dd/dq/resb/equ/times.
    private static readonly Regex LabelBeforeData = new(
        @"^[ \t]*(?<name>[A-Za-z_.?$][A-Za-z0-9_.?$@~#]*)[ \t]+(?:d[bwdqt]|res[bwdqt]|equ|times)\b",
        RegexOptions.Multiline | RegexOptions.IgnoreCase | RegexOptions.Compiled);

    /// <summary>
    /// Extrae las etiquetas definidas en el texto, sin duplicados y en orden de aparición.
    /// Ignora las que están dentro de comentarios.
    /// </summary>
    public static List<string> ExtractLabels(string? source)
    {
        var result = new List<string>();
        if (string.IsNullOrEmpty(source)) return result;

        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var stripped = StripComments(source);

        foreach (var regex in new[] { LabelWithColon, LabelBeforeData })
        {
            foreach (Match m in regex.Matches(stripped))
            {
                var name = m.Groups["name"].Value;
                if (IsReservedWord(name)) continue;
                if (seen.Add(name)) result.Add(name);
            }
        }

        return result;
    }

    /// <summary>
    /// Reemplaza el contenido de los comentarios por espacios, preservando los saltos de línea
    /// para que las posiciones y el modo Multiline sigan siendo válidos. Respeta las cadenas.
    /// </summary>
    private static string StripComments(string source)
    {
        var chars = source.ToCharArray();
        bool inComment = false;
        char quote = '\0';

        for (int i = 0; i < chars.Length; i++)
        {
            char c = chars[i];

            if (c == '\n' || c == '\r')
            {
                inComment = false;
                quote = '\0';
                continue;
            }

            if (inComment) { chars[i] = ' '; continue; }

            if (quote != '\0')
            {
                if (c == quote) quote = '\0';
                continue;
            }

            if (c == '"' || c == '\'' || c == '`') { quote = c; continue; }
            if (c == ';') { inComment = true; chars[i] = ' '; }
        }

        return new string(chars);
    }

    private static bool IsReservedWord(string word) =>
        InstructionSet.Contains(word) || RegisterSet.Contains(word) || DirectiveSet.Contains(word);

    private static readonly HashSet<string> InstructionSet =
        new(Instructions, StringComparer.OrdinalIgnoreCase);
    private static readonly HashSet<string> RegisterSet =
        new(Registers, StringComparer.OrdinalIgnoreCase);
    private static readonly HashSet<string> DirectiveSet =
        new(Directives, StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Sugerencias para un prefijo dado, ordenadas por longitud y luego alfabéticamente.
    /// Las etiquetas del archivo se agregan primero para que ganen en caso de empate de nombre.
    /// </summary>
    public static List<CompletionItem> Suggest(string prefix, IEnumerable<string>? fileLabels = null, int maxResults = 40)
    {
        var items = new List<CompletionItem>();
        if (string.IsNullOrEmpty(prefix)) return items;

        void AddMatches(IEnumerable<string> source, CompletionKind kind)
        {
            foreach (var word in source)
            {
                if (word.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                {
                    items.Add(new CompletionItem(word, kind));
                }
            }
        }

        if (fileLabels is not null) AddMatches(fileLabels, CompletionKind.Label);
        AddMatches(Instructions, CompletionKind.Instruction);
        AddMatches(Registers, CompletionKind.Register);
        AddMatches(Directives, CompletionKind.Directive);

        return items
            .GroupBy(i => i.Text, StringComparer.OrdinalIgnoreCase)
            .Select(g => g.First())
            .OrderBy(i => i.Text.Length)
            .ThenBy(i => i.Text, StringComparer.OrdinalIgnoreCase)
            .Take(maxResults)
            .ToList();
    }

    /// <summary>
    /// Dado el texto y la posición del caret, devuelve el prefijo de palabra que se está
    /// tipeando (lo que hay que completar) y dónde empieza. Longitud 0 si no hay palabra.
    /// </summary>
    public static (int Start, string Prefix) GetWordBeforeCaret(string? text, int caretIndex)
    {
        if (string.IsNullOrEmpty(text)) return (Math.Max(0, caretIndex), "");
        if (caretIndex < 0) return (0, "");
        if (caretIndex > text.Length) caretIndex = text.Length;

        int start = caretIndex;
        while (start > 0 && IsWordChar(text[start - 1])) start--;

        return (start, text.Substring(start, caretIndex - start));
    }

    private static bool IsWordChar(char c) =>
        char.IsLetterOrDigit(c) || c == '_' || c == '.' || c == '%' || c == '?' || c == '$' || c == '@';
}
