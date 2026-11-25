using System.Globalization;
using Civitas.Id.Sweden.Fakers.Internal;

namespace Civitas.Id.Sweden.Tests.Helpers;

/// <summary>Numbers from the pinned files, split into the parts a draft holds.</summary>
internal static class OfficialDrafts
{
    private const int TwelveDigits = 12;

    public static IReadOnlyList<IdDraft> Personnummer { get; } = Of<OfficialPersonnummer>();

    public static IReadOnlyList<IdDraft> LegalPersons { get; } =
    [
        Of(LegalPersonNumbers.Aktiebolag.Digits), Of(LegalPersonNumbers.OtherAktiebolag.Digits),
        Of(LegalPersonNumbers.Aktiebolag559.Digits)
    ];

    public static IdDraft Of(string digits)
    {
        ArgumentNullException.ThrowIfNull(digits);
        var hasCentury = digits.Length == TwelveDigits;
        var body = hasCentury ? digits[2..] : digits;
        return new IdDraft
        {
            Century = hasCentury ? Number(digits[..2]) : null,
            Year = Number(body[..2]),
            Month = Number(body[2..4]),
            Day = Number(body[4..6]),
            Serial = Number(body[6..9])
        };
    }

    /// <summary>The numbers of one kind that encode a calendar date.</summary>
    private static IReadOnlyList<IdDraft> Of<TNumber>()
        where TNumber : OfficialNumber
    {
        return
        [
            .. OfficialNumbers.All.OfType<TNumber>()
                .Where(number => number.CalendarDate is not null)
                .Select(number => Of(number.Long))
        ];
    }

    private static int Number(string digits)
    {
        return int.Parse(digits, NumberStyles.None, CultureInfo.InvariantCulture);
    }
}
