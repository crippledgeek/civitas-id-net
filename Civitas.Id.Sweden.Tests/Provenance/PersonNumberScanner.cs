using System.Text.RegularExpressions;
using Civitas.Id.Sweden.Tests.Helpers;

namespace Civitas.Id.Sweden.Tests.Provenance;

/// <summary>
///     Finds person-shaped numbers in text. It reads 10 and 12 digits with or without a separator, the <c>16</c>
///     prefix, the <c>SE…01</c> VAT form, digits next to letters, and literals joined with <c>+</c>. It does not see
///     a number assembled at run time from variables, or one whose digits are spread over separate values.
/// </summary>
internal sealed partial class PersonNumberScanner
{
    private const string VatSuffix = "01";

    private readonly HashSet<string> _officialLong;
    private readonly HashSet<string> _officialShort;

    public PersonNumberScanner(IEnumerable<OfficialNumber> official)
    {
        var numbers = official.ToList();
        _officialLong = [.. numbers.Select(number => number.Long)];
        _officialShort = [.. numbers.Select(number => number.Short)];
    }

    /// <summary>Scans every text.</summary>
    public ScanResult Scan(IEnumerable<SourceText> texts)
    {
        var scanned = 0;
        var officialHits = 0;
        var unofficial = new List<SourceLine>();
        foreach (var text in texts)
        {
            scanned++;
            foreach (Match match in Candidate().Matches(text.Content))
            {
                var readings = Readings(match).Where(reading => reading.IsPersonShaped()).ToList();
                if (readings.Any(IsOfficial))
                    officialHits++;
                else if (readings.Count > 0)
                    unofficial.Add(new SourceLine(text.RelativePath, LineOf(text.Content, match.Index)));
            }
        }

        return new ScanResult(scanned, officialHits, unofficial);
    }

    private bool IsOfficial(NumberReading reading)
    {
        return _officialLong.Contains(reading.Digits) || _officialShort.Contains(reading.Digits);
    }

    private static IEnumerable<NumberReading> Readings(Match match)
    {
        var century = DigitsOf(match.Groups["century"]);
        var body = DigitsOf(match.Groups["date"]) + DigitsOf(match.Groups["serial"]);

        yield return new NumberReading(NumberReading.IsPersonCentury(century) ? century + body : body);

        // Twelve digits ending in 01 may also be ten digits with the VAT suffix.
        if (century.Length > 0 && body.EndsWith(VatSuffix, StringComparison.Ordinal))
            yield return new NumberReading(century + body[..^VatSuffix.Length]);
    }

    private static string DigitsOf(Group group)
    {
        return LiteralJoin().Replace(group.Value, string.Empty);
    }

    private static int LineOf(string content, int index)
    {
        return content.AsSpan(0, index).Count('\n') + 1;
    }

    // The end of one string literal and the start of the next, as in "1234" + "56".
    private const string Join = """
        (?:"(?:u8)?\s*\+\s*[$@]*")?
        """;

    private const string Separator = """
        (?:[ ]?(?:[-+_./ \u00A0\u2009\u2010-\u2015\u2212]|\\u002[BbDd]|%2[BbDd])?[ ]?)
        """;

    [GeneratedRegex($$"""
        (?<![0-9])
        (?<century>[0-9]{{Join}}[0-9]{{Join}})?
        (?<date>[0-9](?:{{Join}}[0-9]){5})
        {{Join}}{{Separator}}{{Join}}
        (?<serial>[0-9](?:{{Join}}[0-9]){3})
        (?:{{Join}}01)?
        (?![0-9])
        """, RegexOptions.IgnorePatternWhitespace)]
    private static partial Regex Candidate();

    [GeneratedRegex(Join, RegexOptions.IgnorePatternWhitespace)]
    private static partial Regex LiteralJoin();
}
