using Civitas.Id.Sweden.Fakers.Internal;

namespace Civitas.Id.Sweden.Tests.Helpers;

/// <summary>
///     Numbers of one kind that are alike but for the year and check digit. The years are <see cref="int" /> because
///     year zero is no calendar year, and the texts are <see cref="string" /> because they are raw parser input.
/// </summary>
internal sealed class BirthYearCases
{
    /// <summary>Written here and not taken from the library, so that a wrong one there is seen.</summary>
    public const int FirstAcceptedYear = 1800;

    private const int Serial = 120;
    private const int GenderDigit = 4;

    /// <summary>Every year has this day.</summary>
    private static readonly DateOnly MonthAndDay = new(2000, 12, 18);

    private BirthYearCases(IdDraft draft, DefectCatalog defects)
    {
        Draft = draft;
        Defects = defects;
    }

    public static BirthYearCases Personnummer { get; } =
        new(Generation.DraftPersonnummer(MonthAndDay, Serial, GenderDigit), DefectCatalog.Personnummer);

    public static BirthYearCases Samordningsnummer { get; } =
        new(Generation.DraftSamordningsnummer(MonthAndDay, Serial, GenderDigit), DefectCatalog.Samordningsnummer);

    /// <summary>The kinds that encode a birth date, each also the number of a sole trader.</summary>
    public static IReadOnlyList<BirthYearCases> All { get; } = [Personnummer, Samordningsnummer];

    /// <summary>The parts that the numbers share.</summary>
    public IdDraft Draft { get; }

    /// <summary>The defects of this kind.</summary>
    public DefectCatalog Defects { get; }

    /// <summary>The number with its birth year replaced.</summary>
    public string BornIn(int year)
    {
        return Draft.BornIn(year).Render();
    }
}
