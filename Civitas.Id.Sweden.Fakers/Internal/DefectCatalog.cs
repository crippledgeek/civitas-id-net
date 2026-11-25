using System.Globalization;

namespace Civitas.Id.Sweden.Fakers.Internal;

/// <summary>
///     The defects one kind of number supports, in the order of <see cref="IdDefect" />. One ordered array feeds
///     both the list of supported defects and the lookup, so the two cannot disagree.
/// </summary>
internal sealed class DefectCatalog
{
    private const int DecimalBase = 10;
    /// <summary>ARABIC-INDIC DIGIT ZERO. Its digits follow in order and are decimal digits outside ASCII.</summary>
    private const char NonAsciiZero = '\u0660';

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

    /// <summary>The defects of a legal person's number, which has no date to break.</summary>
    public static DefectCatalog LegalPerson { get; } = new([WrongCheckDigit, .. LengthAndCharacterRules]);

    /// <summary>The supported defects, in a stable order.</summary>
    public IReadOnlyList<IdDefect> Supported { get; }

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

    private sealed record DefectRule(IdDefect Defect, Corruption Corrupt);
}
