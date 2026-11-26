using Civitas.Id.Sweden.Errors;

namespace Civitas.Id.Sweden.Tests.Helpers;

/// <summary>
///     What each line of <c>invalid_swedish_ids.csv</c> is rejected for today, and whether its digits also fail the
///     check digit. A line is a place in the file, so a plain <see cref="int" />. Each line carries its reason, which is the wrong
///     form for all of them today, because the parsers give no more specific reason yet.
/// </summary>
internal static class InvalidNumberExpectations
{
    private const InvalidIdNumberReason WrongForm = InvalidIdNumberReason.InvalidFormat;

    private static readonly IReadOnlyDictionary<int, Expectation> ByLine = new Dictionary<int, Expectation>
    {
        [1] = new(WrongForm, FailsChecksum: true), // letters only
        [2] = new(WrongForm, FailsChecksum: true), // three digits
        [3] = new(WrongForm, FailsChecksum: false), // ten zeros: the check digit holds, the month does not
        [4] = new(WrongForm, FailsChecksum: false), // fourteen nines: too long, and the last ten digits hold the check digit
        [5] = new(WrongForm, FailsChecksum: true), // month 13 in 2024
        [6] = new(WrongForm, FailsChecksum: true), // day 32 in July 2024
        [7] = new(WrongForm, FailsChecksum: true), // 30 February 2024
        [8] = new(WrongForm, FailsChecksum: true), // month 13 in 1999
        [9] = new(WrongForm, FailsChecksum: true) // month 00 and day 32
    };

    /// <summary>The lines of the file that have an expectation.</summary>
    public static IReadOnlyCollection<int> Lines => [.. ByLine.Keys];

    /// <exception cref="InvalidDataException">The line has no expectation.</exception>
    public static InvalidNumberRow Of(FixtureRow row)
    {
        if (!ByLine.TryGetValue(row.Line, out var expected))
            throw new InvalidDataException($"{row.Source}: no expected reason for the line.");

        return new InvalidNumberRow
        {
            Source = row.Source,
            Number = row.Cells[0],
            Reason = expected.Reason,
            FailsChecksum = expected.FailsChecksum
        };
    }

    private sealed record Expectation(InvalidIdNumberReason Reason, bool FailsChecksum);
}
