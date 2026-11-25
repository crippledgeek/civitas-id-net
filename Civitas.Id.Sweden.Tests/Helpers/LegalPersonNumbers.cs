namespace Civitas.Id.Sweden.Tests.Helpers;

/// <summary>Organisation numbers of legal persons, from the pinned organisation file.</summary>
internal static class LegalPersonNumbers
{
    /// <summary>An aktiebolag, the file's first row. Raw parser input, so a <see cref="string" />.</summary>
    public static string Aktiebolag => CsvLoader.Column(Fixtures.OrganisationsnummerExtended)[0];
}
