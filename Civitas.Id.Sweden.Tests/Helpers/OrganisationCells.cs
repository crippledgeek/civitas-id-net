using Civitas.Id.Sweden.Format;

namespace Civitas.Id.Sweden.Tests.Helpers;

/// <summary>The cells of an organisation row held against each other, with nothing fed to a parser.</summary>
internal static class OrganisationCells
{
    private const string VatPrefix = "SE";
    private const string VatSuffix = "01";

    /// <summary>
    ///     Fails unless the long format is the short format with one separator, the short format is the last ten digits
    ///     of the input, and the type is a known one.
    /// </summary>
    public static async Task AgreeAsync(OrganisationRow row)
    {
        await UnprintedAssert.AreEqual(WrittenNumber.Digits(row.LongFormat), row.ShortFormat).ConfigureAwait(false);
        await UnprintedAssert.AreEqual(WrittenNumber.LastTenDigits(row.Input), row.ShortFormat).ConfigureAwait(false);
        await Assert.That(row.LongFormat.Count(character => character is '-' or '+')).IsEqualTo(1);
        await Assert.That(Enum.IsDefined(NumberTypeOf(row.Type))).IsTrue();
    }

    /// <summary>
    ///     <see cref="AgreeAsync" />, and the VAT number is <c>SE</c>, the short format and <c>01</c>. Fails for a row
    ///     with no VAT number.
    /// </summary>
    public static async Task AgreeWithVatAsync(OrganisationRow row)
    {
        await AgreeAsync(row).ConfigureAwait(false);
        await Assert.That(row.VatNumber).IsNotNull();
        await UnprintedAssert.AreEqual(row.VatNumber, VatPrefix + row.ShortFormat + VatSuffix).ConfigureAwait(false);
    }

    /// <summary>The number type that the file's type column names.</summary>
    /// <exception cref="InvalidDataException">The column holds an unknown value.</exception>
    public static OrganisationNumberType NumberTypeOf(string type)
    {
        return type switch
        {
            "Aktiebolag" => OrganisationNumberType.LegalPerson,
            "Enskild firma" => OrganisationNumberType.PhysicalPerson,
            _ => throw new InvalidDataException($"Unknown type column value: {type}")
        };
    }
}
