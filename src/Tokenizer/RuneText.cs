using System.Text;

namespace Lokad.Tokenizers.Tokenizer;

internal static class RuneText
{
    // StringBuilder.Append(object) would box a Rune and allocate its ToString().
    internal static void AppendRune(this StringBuilder builder, Rune rune)
    {
        Span<char> buffer = stackalloc char[2];
        builder.Append(buffer[..rune.EncodeToUtf16(buffer)]);
    }
}
