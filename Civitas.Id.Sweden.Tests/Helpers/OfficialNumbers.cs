namespace Civitas.Id.Sweden.Tests.Helpers;

/// <summary>The numbers of Skatteverket's pinned test sets. A lookup throws when the sets hold no such number.</summary>
internal static class OfficialNumbers
{
    private static readonly Lazy<IReadOnlyList<OfficialPersonnummer>> PersonnummerRows =
        RowsOf(digits => new OfficialPersonnummer(digits), Fixtures.Personnummer1890S, Fixtures.Personnummer2000S);

    private static readonly Lazy<IReadOnlyList<OfficialSamordningsnummer>> SamordningsnummerRows =
        RowsOf(digits => new OfficialSamordningsnummer(digits),
            Fixtures.SamordningsnummerOfficial, Fixtures.Samordningsnummer2020);

    private static readonly Lazy<ILookup<DateOnly, OfficialPersonnummer>> PersonnummerByBirthDate =
        new(() => ByBirthDate(PersonnummerRows.Value));

    private static readonly Lazy<ILookup<DateOnly, OfficialSamordningsnummer>> SamordningsnummerByBirthDate =
        new(() => ByBirthDate(SamordningsnummerRows.Value));

    /// <summary>Every pinned official number.</summary>
    public static IEnumerable<OfficialNumber> All => [.. PersonnummerRows.Value, .. SamordningsnummerRows.Value];

    /// <summary>A woman's number born in 1890, so more than 100 years old on any day the tests run.</summary>
    public static OfficialPersonnummer Centenarian => Personnummer(new DateOnly(1890, 1, 1));

    /// <summary>A man's number born in 1890.</summary>
    public static OfficialPersonnummer CentenarianMale => Personnummer(new DateOnly(1890, 1, 2));

    /// <summary>A third number born in 1890.</summary>
    public static OfficialPersonnummer OtherCentenarian => Personnummer(new DateOnly(1890, 1, 3));

    /// <summary>A number born in 2000, whose short form resolves to the same century today.</summary>
    public static OfficialPersonnummer Modern => Personnummer(new DateOnly(2000, 1, 1));

    /// <summary>A number born on 8 May 2008.</summary>
    public static OfficialPersonnummer EighteenIn2026 => Personnummer(new DateOnly(2008, 5, 8));

    /// <summary>A number born on 1 June 1895.</summary>
    public static OfficialPersonnummer HundredIn1995 => Personnummer(new DateOnly(1895, 6, 1));

    /// <summary>A man's samordningsnummer born in 1914.</summary>
    public static OfficialSamordningsnummer CentenarianSamordningsnummer =>
        Samordningsnummer(new DateOnly(1914, 1, 8));

    /// <summary>A woman's samordningsnummer born in 1914.</summary>
    public static OfficialSamordningsnummer CentenarianFemaleSamordningsnummer =>
        Samordningsnummer(new DateOnly(1914, 6, 5));

    /// <summary>A samordningsnummer born in 2000, whose short form resolves to the same century today.</summary>
    public static OfficialSamordningsnummer ModernSamordningsnummer => Samordningsnummer(new DateOnly(2000, 6, 6));

    /// <summary>A samordningsnummer born in 1981, whose short form resolves to the same century today.</summary>
    public static OfficialSamordningsnummer TwentiethCenturySamordningsnummer =>
        Samordningsnummer(new DateOnly(1981, 12, 15));

    /// <summary>The first published personnummer born on <paramref name="born" />.</summary>
    public static OfficialPersonnummer Personnummer(DateOnly born)
    {
        return PersonnummerByBirthDate.Value[born].First();
    }

    /// <summary>The first published samordningsnummer born on <paramref name="born" />.</summary>
    public static OfficialSamordningsnummer Samordningsnummer(DateOnly born)
    {
        return SamordningsnummerByBirthDate.Value[born].First();
    }

    private static Lazy<IReadOnlyList<TNumber>> RowsOf<TNumber>(
        Func<string, TNumber> create, params FixtureSpec[] specs)
    {
        return new Lazy<IReadOnlyList<TNumber>>(
            () => [.. specs.SelectMany(spec => CsvLoader.Column(spec)).Select(create)]);
    }

    private static ILookup<DateOnly, TNumber> ByBirthDate<TNumber>(IEnumerable<TNumber> numbers)
        where TNumber : OfficialNumber
    {
        return numbers.Where(number => number.CalendarDate is not null).ToLookup(number => number.BirthDate);
    }
}
