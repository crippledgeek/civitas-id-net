using System.Text.RegularExpressions;
using Civitas.Id.Sweden.Tests.Helpers;

namespace Civitas.Id.Sweden.Tests.Provenance;

/// <summary>
///     Finds person-shaped numbers in text and in the path of the text, both as written and normalized. It reads 10 and 12 digits in pairs or whole,
///     with or without separators, after a <c>16</c> prefix, in the <c>SE…01</c> VAT form, next to letters, escaped,
///     grouped with <c>_</c>, and spread over adjacent literals joined with <c>+</c> or a comma.
/// </summary>
/// <remarks>
///     It does not see a number assembled at run time from variables or interpolation; one with other text, such as
///     an identifier or markup, between its digits; one grouped other than in pairs, unless by <c>_</c>; one inside a
///     longer run of digits; one that is encoded (Base64, octal) or written as numbers for its characters; one with
///     the check digit left out; or one in a binary file.
/// </remarks>
internal sealed partial class PersonNumberScanner
{
    private const string VatSuffix = "01";
    private const string LegalPersonPrefix = "16";

    private readonly HashSet<string> _official;

    public PersonNumberScanner(IEnumerable<OfficialNumber> official)
    {
        _official = [.. official.Select(number => number.Long)];
    }

    /// <summary>Scans every text.</summary>
    public ScanResult Scan(IEnumerable<SourceText> texts)
    {
        return new ScanResult([.. texts.Select(Scan)]);
    }

    private FileScan Scan(SourceText text)
    {
        var asWritten = Find(NormalizedText.Unchanged(text.Content));
        var normalized = NormalizedText.Of(text.Content);
        var lines = asWritten.Concat(Find(normalized)).Concat(Find(normalized.Joined()))
            .Where(finding => !finding.IsOfficial)
            .Select(finding => finding.Line)
            .Distinct()
            .Order()
            .Select(line => new SourceLine(text.Path, line));
        var path = Find(NormalizedText.Unchanged(text.Path.Value)).Concat(Find(NormalizedText.Of(text.Path.Value)))
            .Where(finding => !finding.IsOfficial)
            .Take(1)
            .Select(_ => new SourceLine(text.Path, null));

        return new FileScan(text.Path, asWritten.Count(finding => finding.IsOfficial), [.. path, .. lines]);
    }

    private List<Finding> Find(NormalizedText text)
    {
        var longSpans = Candidates(LongForm(), text.Text, LongReadings).ToList();
        var shortSpans = Candidates(ShortForm(), text.Text, ShortReadings)
            .Where(candidate => !longSpans.Exists(candidate.IsTailOf));

        // Candidates over the same digits are one finding, official only when every reading is.
        return
        [
            .. longSpans.Concat(shortSpans)
                .GroupBy(candidate => candidate.Start)
                .Select(same => new Finding(
                    text.LineOf(same.Key),
                    same.SelectMany(candidate => candidate.Readings).All(r => _official.Contains(r.Digits))))
        ];
    }

    // A match that is no number must not swallow the start of one, so the search resumes one character later.
    private static IEnumerable<Candidate> Candidates(
        Regex form, string text, Func<Match, IEnumerable<NumberReading?>> read)
    {
        var start = 0;
        while (form.Match(text, start) is { Success: true } match)
        {
            var readings = read(match).OfType<NumberReading>().Where(reading => reading.IsPersonShaped()).ToList();
            start = readings.Count > 0 ? match.Index + match.Length : match.Index + 1;
            if (readings.Count > 0)
                yield return new Candidate(match.Index, match.Length, readings);
        }
    }

    private static IEnumerable<NumberReading?> LongReadings(Match match)
    {
        var century = match.Groups["century"].Value;
        yield return century == LegalPersonPrefix ? ShortReading(match) : new NumberReading(century + Body(match));
    }

    private static IEnumerable<NumberReading?> ShortReadings(Match match)
    {
        yield return ShortReading(match);
    }

    private static NumberReading? ShortReading(Match match)
    {
        return NumberReading.OfShort(Body(match), match.Groups["sign"].Value.Contains('+', StringComparison.Ordinal));
    }

    private static string Body(Match match)
    {
        return string.Concat(
            ((string[])["year", "month", "day", "serial", "check"]).Select(name => match.Groups[name].Value));
    }

    // Inside the date, and inside the last four digits.
    private const string Inner = """
        (?:\\?[-_./ ])?
        """;

    // Between the date and the last four digits; a line break only after a mark.
    private const string Sign = """
        (?<sign>(?:[^\S\n]{0,2}\\?[-+_./,:;~|*\u2010-\u2015\u2212]\s{0,8}|[^\S\n]{1,2})?)
        """;

    private const string Digits = $"""
        (?<year>[0-9][0-9]){Inner}(?<month>[0-9][0-9]){Inner}(?<day>[0-9][0-9])
        {Sign}(?<serial>[0-9][0-9]){Inner}(?<check>[0-9][0-9])
        """;

    [GeneratedRegex($"""
        (?<![0-9])(?<century>[0-9][0-9]){Inner}{Digits}(?:{VatSuffix})?(?![0-9])
        """, RegexOptions.IgnorePatternWhitespace)]
    private static partial Regex LongForm();

    [GeneratedRegex($"""
        (?<![0-9]){Digits}(?:{VatSuffix})?(?![0-9])
        """, RegexOptions.IgnorePatternWhitespace)]
    private static partial Regex ShortForm();

    private sealed record Candidate(int Start, int Length, IReadOnlyList<NumberReading> Readings)
    {
        /// <summary>True when this is the end of a longer candidate, so a part of the same number.</summary>
        public bool IsTailOf(Candidate whole)
        {
            return Start > whole.Start && Start + Length <= whole.Start + whole.Length;
        }
    }

    private sealed record Finding(int Line, bool IsOfficial);
}
