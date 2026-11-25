namespace Civitas.Id.Sweden.Fakers.Internal;

/// <summary>The notations each kind of number can be written in.</summary>
internal static class NotationCatalog
{
    private const char Space = ' ';
    private const char NoBreakSpace = '\u00A0';
    private const string LegalPersonPrefix = "16";
    private const string CountryPrefix = "SE";

    /// <summary>A person is written with a plus from the year they turn this old.</summary>
    private const int PlusAge = 100;

    /// <summary>One over the length limit, which the string and span entry points apply after trimming.</summary>
    private const int OverLength = 101;

    private static readonly Dictionary<IdNotation, Func<IdDraft, string>> Common = new()
    {
        [IdNotation.Canonical] = draft => draft.Render(),
        [IdNotation.AsciiPadded] = PaddedWith(Space),
        [IdNotation.NoBreakSpacePadded] = PaddedWith(NoBreakSpace),
        [IdNotation.SePrefix] = PrefixedWith(CountryPrefix),
        [IdNotation.OverLengthPadded] = draft => draft.Render().PadLeft(OverLength)
    };

    private static readonly Dictionary<IdNotation, Func<IdDraft, string>> Person = new(Common)
    {
        [IdNotation.ContradictoryPlus] = WithPlus
    };

    private static readonly Dictionary<IdNotation, Func<IdDraft, string>> LegalPerson = new(Common)
    {
        [IdNotation.SixteenPrefix] = PrefixedWith(LegalPersonPrefix),
        [IdNotation.PlusOnLegalPerson] = WithPlus,
        [IdNotation.ZeroSerialLegalPerson] = draft => (draft with { Serial = 0 }).Render()
    };

    /// <summary>Writes <paramref name="draft" /> in <paramref name="notation" />.</summary>
    /// <param name="notation">The notation.</param>
    /// <param name="draft">The parts of a valid number.</param>
    /// <param name="today">The date that decides whether a plus is contradictory.</param>
    /// <returns>The text.</returns>
    /// <exception cref="ArgumentOutOfRangeException">This number cannot be written that way.</exception>
    public static string Render(IdNotation notation, IdDraft draft, DateOnly today)
    {
        // Only a legal person's number has no century.
        var notations = draft.Century is null ? LegalPerson : Person;
        return notations.TryGetValue(notation, out var render) &&
               (notation != IdNotation.ContradictoryPlus || IsUnderPlusAge(draft, today))
            ? render(draft)
            : throw new ArgumentOutOfRangeException(nameof(notation), notation, "Not possible for this number.");
    }

    /// <summary>For anyone older the plus would be the correct separator, so nothing would be contradicted.</summary>
    private static bool IsUnderPlusAge(IdDraft draft, DateOnly today)
    {
        return today.Year - draft.FullYear < PlusAge;
    }

    private static string WithPlus(IdDraft draft)
    {
        var text = draft.Render();
        return text.Insert(text.Length - IdDraft.DigitsAfterSeparator, "+");
    }

    private static Func<IdDraft, string> PaddedWith(char padding)
    {
        return draft => $"{padding}{draft.Render()}{padding}";
    }

    private static Func<IdDraft, string> PrefixedWith(string prefix)
    {
        return draft => prefix + draft.Render();
    }
}
