using System.Globalization;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace Civitas.Id.Sweden.Tests.Provenance;

/// <summary>
///     Text with the many ways to write a character reduced to one: escapes and HTML entities decoded, compatibility
///     forms and decimal digits of any script folded, invisible format characters removed. Each character keeps the
///     line it came from.
/// </summary>
internal sealed partial class NormalizedText
{
    private readonly IReadOnlyList<int> _lines;

    private NormalizedText(string text, IReadOnlyList<int> lines)
    {
        Text = text;
        _lines = lines;
    }

    /// <summary>The reduced text, with line feeds where the source had them.</summary>
    public string Text { get; }

    /// <summary>The text as it is written.</summary>
    public static NormalizedText Unchanged(string content)
    {
        return Of(content, line => line);
    }

    /// <summary>Reduces <paramref name="content" />.</summary>
    public static NormalizedText Of(string content)
    {
        return Of(content, Normalize);
    }

    private static NormalizedText Of(string content, Func<string, string> normalize)
    {
        var text = new StringBuilder(content.Length);
        var lines = new List<int>(content.Length);
        var line = 0;
        foreach (var source in content.Split('\n'))
        {
            line++;
            var normalized = normalize(source);
            text.Append(normalized).Append('\n');
            lines.AddRange(Enumerable.Repeat(line, normalized.Length + 1));
        }

        return new NormalizedText(text.ToString(), lines);
    }

    /// <summary>The same text with string literals joined and digit grouping by <c>_</c> removed.</summary>
    public NormalizedText Joined()
    {
        var text = new StringBuilder(Text.Length);
        var lines = new List<int>(Text.Length);
        var kept = 0;
        foreach (var cut in Joins().EnumerateMatches(Text))
        {
            Keep(cut.Index);
            kept = cut.Index + cut.Length;
        }

        Keep(Text.Length);
        return new NormalizedText(text.ToString(), lines);

        void Keep(int end)
        {
            text.Append(Text, kept, end - kept);
            for (var i = kept; i < end; i++)
                lines.Add(_lines[i]);
        }
    }

    /// <summary>The source line of a character of <see cref="Text" />.</summary>
    public int LineOf(int index)
    {
        return _lines[index];
    }

    private static string Normalize(string line)
    {
        var decoded = WebUtility.HtmlDecode(Escape().Replace(line, Decode));
        var folded = HasLoneSurrogate(decoded)
            ? decoded
            : decoded.Normalize(NormalizationForm.FormKC);
        return string.Concat(folded
            .Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.Format)
            .Select(Fold));
    }

    private static string Decode(Match match)
    {
        var hex = match.Groups.Values.Skip(1).Single(group => group.Success).ValueSpan;
        return ((char)int.Parse(hex, NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture)).ToString();
    }

    // A decoded line break must not move the lines after it.
    private static char Fold(char c)
    {
        var digit = CharUnicodeInfo.GetDecimalDigitValue(c);
        return digit >= 0 ? (char)('0' + digit) : c == '\n' ? ' ' : c;
    }

    private static bool HasLoneSurrogate(string text)
    {
        for (var i = 0; i < text.Length; i++)
            if (char.IsSurrogatePair(text, i))
                i++;
            else if (char.IsSurrogate(text[i]))
                return true;
        return false;
    }

    [GeneratedRegex("""
        (?<!\\)\\+(?:u([0-9A-Fa-f]{4}) | u\{0*([0-9A-Fa-f]{1,4})\} | U0000([0-9A-Fa-f]{4}) | x([0-9A-Fa-f]{2}))
        | %([0-9A-Fa-f]{2})
        """, RegexOptions.IgnorePatternWhitespace)]
    private static partial Regex Escape();

    // The end of one string or character literal and the start of the next, or an underscore between digits.
    [GeneratedRegex("""
        ["'](?:u8)?\s*[+,]\s*[$@]*["'] | (?<=[0-9])_+(?=[0-9])
        """, RegexOptions.IgnorePatternWhitespace)]
    private static partial Regex Joins();
}
