using System.Globalization;
using Civitas.Id.Sweden.Internal;

namespace Civitas.Id.Sweden.Tests.Helpers;

internal static class TestIdBuilder
{
    /// <summary>
    ///     Constructs a valid 12-digit personnummer for the given <paramref name="birthDate" />
    ///     and 3-digit <paramref name="serial" />. The Luhn check digit is computed via the
    ///     project's <c>SwedishLuhnAlgorithm</c>.
    /// </summary>
    public static string PersonalIdForBirthDate(DateOnly birthDate, string serial)
    {
        ArgumentException.ThrowIfNullOrEmpty(serial);
        if (serial.Length != 3) throw new ArgumentException("serial must be 3 digits", nameof(serial));

        var yyyymmdd = birthDate.ToString("yyyyMMdd", CultureInfo.InvariantCulture);
        Span<char> body9 = stackalloc char[9];
        yyyymmdd.AsSpan(2).CopyTo(body9[..6]);
        serial.AsSpan().CopyTo(body9[6..9]);
        var check = SwedishLuhnAlgorithm.ComputeCheckDigit(body9);
        return string.Concat(yyyymmdd, serial, check.ToString(CultureInfo.InvariantCulture));
    }

    /// <summary>
    ///     Constructs a valid 12-digit samordningsnummer for the given <paramref name="birthDate" />
    ///     and 3-digit <paramref name="serial" />. The day is encoded as <c>day + 60</c>; Luhn is
    ///     computed over the encoded 9-digit body.
    /// </summary>
    public static string CoordinationIdForBirthDate(DateOnly birthDate, string serial)
    {
        ArgumentException.ThrowIfNullOrEmpty(serial);
        if (serial.Length != 3) throw new ArgumentException("serial must be 3 digits", nameof(serial));

        var yyyy = birthDate.Year.ToString("0000", CultureInfo.InvariantCulture);
        var yy = yyyy.AsSpan(2, 2);
        var mm = birthDate.Month.ToString("00", CultureInfo.InvariantCulture);
        var ddOffset = (birthDate.Day + 60).ToString("00", CultureInfo.InvariantCulture);

        Span<char> body9 = stackalloc char[9];
        yy.CopyTo(body9[..2]);
        mm.AsSpan().CopyTo(body9[2..4]);
        ddOffset.AsSpan().CopyTo(body9[4..6]);
        serial.AsSpan().CopyTo(body9[6..9]);

        var check = SwedishLuhnAlgorithm.ComputeCheckDigit(body9);
        return string.Concat(yyyy, mm, ddOffset, serial, check.ToString(CultureInfo.InvariantCulture));
    }
}
