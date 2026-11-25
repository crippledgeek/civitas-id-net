namespace Civitas.Id.Sweden.Tests.Helpers;

/// <summary>Organisation numbers of legal persons, from the pinned organisation file.</summary>
internal static class LegalPersonNumbers
{
    /// <summary>An aktiebolag in the 556 range.</summary>
    public static LegalPersonNumber Aktiebolag => At(0);

    /// <summary>Another aktiebolag in the 556 range.</summary>
    public static LegalPersonNumber OtherAktiebolag => At(2);

    /// <summary>An aktiebolag in the 559 range.</summary>
    public static LegalPersonNumber Aktiebolag559 => At(4);

    private static LegalPersonNumber At(int row)
    {
        return new LegalPersonNumber(CsvLoader.Column(Fixtures.OrganisationsnummerExtended)[row]);
    }
}
