using System.Globalization;
using Civitas.Id.Sweden.Core;
using Civitas.Id.Sweden.Fakers;
using Microsoft.Extensions.Time.Testing;
using TUnit.FsCheck;

namespace Civitas.Id.Sweden.Tests.PropertyBased;

#pragma warning disable CA1822 // Property tests are instance methods by TUnit/FsCheck convention.

/// <summary>
///     Property-based invariants over the public API. Each test runs with FsCheck's default 100 iterations.
/// </summary>
public sealed class InvariantTests
{
    /// <summary>
    ///     a) Faker output round-trips through Parse: the canonical string is parseable and equal to the source.
    /// </summary>
    [Test]
    [FsCheckProperty(MaxTest = 100)]
    public bool PersonalId_FakerOutput_RoundTripsThroughParse(int seed)
    {
        var faker = new PersonalIdFaker(seed);
        var id = faker.Generate();
        var parsed = PersonalId.Parse(id.LongFormat());
        return parsed.Equals(id);
    }

    /// <summary>
    ///     b) Luhn validation invariant: any faker-generated PersonalId is recognised as valid.
    /// </summary>
    [Test]
    [FsCheckProperty(MaxTest = 100)]
    public bool PersonalId_FakerOutput_IsValid(int seed)
    {
        var faker = new PersonalIdFaker(seed);
        var id = faker.Generate();
        return PersonalId.IsValid(id.LongFormat());
    }

    /// <summary>
    ///     c) Luhn one-digit-flip detection: tampering with any single digit inside the 10-digit
    ///     Luhn body (positions 2..11 of the 12-digit canonical form, i.e. YYMMDDXXXX) breaks
    ///     validation. The first two digits encode the century and are not part of the Luhn body,
    ///     so they are excluded from this property — flipping them yields a different but still
    ///     algorithmically valid identifier (different person).
    ///     We flip by adding 1 mod 10 — guarantees the digit changes. Luhn detects every single-
    ///     digit substitution within its body.
    /// </summary>
    [Test]
    [FsCheckProperty(MaxTest = 100)]
    public bool PersonalId_SingleDigitFlipInLuhnBody_FailsValidation(int seed, int positionSeed)
    {
        var faker = new PersonalIdFaker(seed);
        var id = faker.Generate();
        var canonical = id.LongFormat(); // 12 digits, YYYYMMDDXXXX
        var position = 2 + (positionSeed % 10 + 10) % 10; // 2..11

        var chars = canonical.ToCharArray();
        var original = chars[position] - '0';
        chars[position] = (char)('0' + (original + 1) % 10);
        var tampered = new string(chars);

        // Some flips inside positions 2..7 still yield a valid date but with a now-broken checksum;
        // some flips inside positions 8..11 directly invalidate the Luhn check. Either way the
        // overall string MUST NOT validate.
        return !PersonalId.IsValid(tampered);
    }

    /// <summary>
    ///     d) BirthDate components round-trip: Generate(y, m, d).BirthDate == DateOnly(y, m, d).
    ///     Constrained to 1970..2019 and day 1..28 to be safe across all months and the century-inference
    ///     window.
    /// </summary>
    [Test]
    [FsCheckProperty(MaxTest = 100)]
    public bool PersonalId_GenerateWithDateComponents_PreservesBirthDate(int seed, int yearOffset, int monthSeed,
        int daySeed)
    {
        var year = 1970 + (yearOffset % 50 + 50) % 50; // 1970..2019
        var month = (monthSeed % 12 + 12) % 12 + 1; // 1..12
        var day = (daySeed % 28 + 28) % 28 + 1; // 1..28
        var faker = new PersonalIdFaker(seed);
        var id = faker.Generate(year, month, day);
        return id.BirthDate == new DateOnly(year, month, day);
    }

    /// <summary>
    ///     e) CoordinationId day-encoding invariant: the day digits in the 12-digit form are in [61, 91].
    ///     Per Skatteverket: samordningsnummer encode the calendar day with a +60 offset.
    /// </summary>
    [Test]
    [FsCheckProperty(MaxTest = 100)]
    public bool CoordinationId_DayDigits_AreInExpectedRange(int seed)
    {
        var faker = new CoordinationIdFaker(seed);
        var id = faker.Generate();
        var longFormat = id.LongFormat(); // YYYYMMDDXXXX with DD in [61, 91]
        var dayDigits = int.Parse(longFormat.AsSpan(6, 2), CultureInfo.InvariantCulture);
        return dayDigits is >= 61 and <= 91;
    }

    /// <summary>
    ///     f) For any seed → faker-generated PersonalId, parsing its LongFormat()
    ///     via the DateOnly overload with a fixed today MUST equal parsing it
    ///     via the TimeProvider overload pinned to a FakeTimeProvider at the
    ///     same Stockholm civil date. Locks the equivalence between the two
    ///     deterministic-parse paths and prevents drift.
    /// </summary>
    [Test]
    [FsCheckProperty(MaxTest = 100)]
    public bool DateOnlyParse_AndTimeProviderParse_AreEquivalent(int seed)
    {
        var today = new DateOnly(2026, 5, 10);
        // FakeTimeProvider pinned to UTC noon on 2026-05-10 → Stockholm 2026-05-10 (CEST 14:00).
        var fake = new FakeTimeProvider(new DateTimeOffset(2026, 5, 10, 12, 0, 0, TimeSpan.Zero));

        var generated = new PersonalIdFaker(seed: seed).Generate();
        var fromDateOnly = PersonalId.Parse(generated.LongFormat(), today);
        var fromTimeProvider = PersonalId.Parse(generated.LongFormat(), fake);

        return fromDateOnly.Equals(fromTimeProvider);
    }
}
