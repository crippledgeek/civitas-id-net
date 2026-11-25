using System.Globalization;
using Civitas.Id.Sweden.Internal;

namespace Civitas.Id.Sweden.Fakers.Internal;

/// <summary>
///     The defects one kind of number supports, in the order of <see cref="IdDefect" />. One ordered array feeds
///     both the list of supported defects and the lookup, so the two cannot disagree.
/// </summary>
internal sealed class DefectCatalog
{
    private const int DecimalBase = 10;
    private const int February = 2;
    private const int LeapDay = 29;

    /// <summary>Leap years are at least this far apart, so adding less to one gives an ordinary year.</summary>
    private const int LeapCycle = 4;

    /// <summary>ARABIC-INDIC DIGIT ZERO. Its digits follow in order and are decimal digits outside ASCII.</summary>
    private const char NonAsciiZero = '\u0660';

    private static readonly InclusiveRange ImpossibleMonths = new(13, 19);

    private static readonly int[] YearsBelowMinimum = [.. Enumerable.Range(0, SwedishIdParsing.MinimumBirthYear)];

    /// <summary>The years that keep a leap day real. Not year zero, which <see cref="DateTime" /> does not hold.</summary>
    private static readonly int[] LeapYearsBelowMinimum =
        [.. YearsBelowMinimum.Where(year => year > 0 && DateTime.IsLeapYear(year))];

    private static readonly DefectRule WrongCheckDigit = new(IdDefect.WrongCheckDigit, InText((text, next) =>
        text[..^1] + Digit((text[^1] - '0' + next(1, DecimalBase)) % DecimalBase)));

    private static readonly DefectRule[] LengthAndCharacterRules =
    [
        new(IdDefect.TooShort, InText((text, next) => text.Remove(next(0, text.Length), 1))),
        new(IdDefect.TooLong, InText((text, next) =>
            text.Insert(next(0, text.Length + 1), Digit(next(0, DecimalBase))))),
        new(IdDefect.NonAsciiDigit, InText((text, next) =>
        {
            var index = next(0, text.Length);
            return text[..index] + (char)(NonAsciiZero + (text[index] - '0')) + text[(index + 1)..];
        })),
        // The legal position is excluded: a number with its separator there is valid.
        new(IdDefect.MisplacedSeparator, InText((text, next) =>
            text.Insert(next(0, text.Length - IdDraft.DigitsAfterSeparator), "-")))
    ];

    private readonly DefectRule[] _rules;

    private DefectCatalog(DefectRule[] rules)
    {
        _rules = rules;
        Supported = Array.AsReadOnly([.. rules.Select(rule => rule.Defect)]);
    }

    /// <summary>The defects of a personnummer.</summary>
    public static DefectCatalog Personnummer { get; } = ForPersons(new InclusiveRange(32, 59), 0);

    /// <summary>The defects of a samordningsnummer.</summary>
    public static DefectCatalog Samordningsnummer { get; } =
        ForPersons(new InclusiveRange(92, 99), IdDraft.CoordinationDayOffset);

    /// <summary>The defects of a legal person's number, which has no date to break.</summary>
    public static DefectCatalog LegalPerson { get; } = new([WrongCheckDigit, .. LengthAndCharacterRules]);

    /// <summary>The supported defects, in a stable order.</summary>
    public IReadOnlyList<IdDefect> Supported { get; }

    /// <summary>Whether a number for <paramref name="birthDate" /> can have <paramref name="defect" />.</summary>
    /// <param name="defect">The defect.</param>
    /// <param name="birthDate">The date to keep, or <see langword="null" /> when any date will do.</param>
    /// <returns>Whether the two go together.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="defect" /> is not supported.</exception>
    public bool Allows(IdDefect defect, DateOnly? birthDate)
    {
        return Find(defect).Allows(birthDate);
    }

    /// <summary>Picks a supported defect that goes together with <paramref name="birthDate" />.</summary>
    /// <param name="birthDate">The date to keep, or <see langword="null" /> when any date will do.</param>
    /// <param name="next">The random source.</param>
    /// <returns>The defect.</returns>
    public IdDefect Pick(DateOnly? birthDate, Func<int, int, int> next)
    {
        IdDefect[] candidates = [.. _rules.Where(rule => rule.Allows(birthDate)).Select(rule => rule.Defect)];
        return candidates[next(0, candidates.Length)];
    }

    /// <summary>Makes <paramref name="draft" /> invalid.</summary>
    /// <param name="defect">The rule to break.</param>
    /// <param name="draft">The parts of a valid number.</param>
    /// <param name="next">The random source.</param>
    /// <returns>The invalid number.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="defect" /> is not supported.</exception>
    public InvalidGeneratedId Corrupt(IdDefect defect, IdDraft draft, Func<int, int, int> next)
    {
        return new InvalidGeneratedId(Find(defect).Corrupt(draft, next), defect);
    }

    private static DefectCatalog ForPersons(InclusiveRange impossibleDays, int dayOffset)
    {
        return new DefectCatalog(
        [
            WrongCheckDigit,
            new DefectRule(IdDefect.ImpossibleMonth, InFields((draft, next) =>
                draft with { Month = ImpossibleMonths.Draw(next) })),
            new DefectRule(IdDefect.ImpossibleDay, InFields((draft, next) =>
                draft with { Day = impossibleDays.Draw(next) })),
            new DefectRule(IdDefect.NonLeapFebruary29, InFields((draft, next) =>
                draft with { Year = NonLeapYear(draft, next), Month = February, Day = LeapDay + dayOffset }))
            {
                Allows = birthDate => birthDate is not { } date || !DateTime.IsLeapYear(date.Year)
            },
            new DefectRule(IdDefect.BirthYearBelowMinimum, InFields((draft, next) =>
            {
                var years = draft.Month == February && draft.Day == LeapDay + dayOffset
                    ? LeapYearsBelowMinimum
                    : YearsBelowMinimum;
                return draft.BornIn(years[next(0, years.Length)]);
            })),
            .. LengthAndCharacterRules
        ]);
    }

    /// <summary>The draft's own year when it has no leap day, else one of the years up to the next that has.</summary>
    private static int NonLeapYear(IdDraft draft, Func<int, int, int> next)
    {
        return DateTime.IsLeapYear(draft.FullYear) ? draft.Year + next(1, LeapCycle) : draft.Year;
    }

    private static Corruption InFields(Func<IdDraft, Func<int, int, int>, IdDraft> edit)
    {
        return (draft, next) => edit(draft, next).Render();
    }

    private static Corruption InText(Func<string, Func<int, int, int>, string> edit)
    {
        return (draft, next) => edit(draft.Render(), next);
    }

    private static string Digit(int value)
    {
        return value.ToString(CultureInfo.InvariantCulture);
    }

    private DefectRule Find(IdDefect defect)
    {
        return Array.Find(_rules, rule => rule.Defect == defect)
               ?? throw new ArgumentOutOfRangeException(nameof(defect), defect, "Not supported for this kind of number.");
    }

    private sealed record DefectRule(IdDefect Defect, Corruption Corrupt)
    {
        public Predicate<DateOnly?> Allows { get; init; } = static _ => true;
    }

    private readonly record struct InclusiveRange(int First, int Last)
    {
        public int Draw(Func<int, int, int> next)
        {
            return next(First, Last + 1);
        }
    }
}
