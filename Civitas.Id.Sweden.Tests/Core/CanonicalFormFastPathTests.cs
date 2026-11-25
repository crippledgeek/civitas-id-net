using Civitas.Id.Sweden.Core;
using Civitas.Id.Sweden.Tests.Helpers;

namespace Civitas.Id.Sweden.Tests.Core;

/// <summary>
///     Pins the canonical-form fast-path in TryParse: when input is already
///     the canonical 12-digit (person) or 10-digit (organisation) form with
///     no SE prefix, no delimiter, and no whitespace, the input string is
///     reused as the backing field — saving the new string(canonical)
///     allocation. Verified via <see cref="object.ReferenceEquals" />.
/// </summary>
public class CanonicalFormFastPathTests
{
    /// <summary>Canonical 12-digit personnummer reuses input string instance.</summary>
    [Test]
    public async Task PersonalId_CanonicalInput_ReusesInputString()
    {
        var input = OfficialNumbers.Modern.Long;
        var ok = PersonalId.TryParse(input, out var pid);
        await Assert.That(ok).IsTrue();
        await Assert.That(ReferenceEquals(pid!.LongFormat(), input)).IsTrue();
    }

    /// <summary>10-digit personnummer (no century) requires reconstruction; new string allocated.</summary>
    [Test]
    public async Task PersonalId_NonCanonicalInput_AllocatesNewString()
    {
        var input = OfficialNumbers.Modern.Short; // 10-digit, no century — needs reconstruction
        var ok = PersonalId.TryParse(input, out var pid);
        await Assert.That(ok).IsTrue();
        await Assert.That(ReferenceEquals(pid!.LongFormat(), input)).IsFalse();
    }

    /// <summary>Canonical 10-digit orgnummer reuses input string instance.</summary>
    [Test]
    public async Task OrganisationId_CanonicalInput_ReusesInputString()
    {
        var input = LegalPersonNumbers.Aktiebolag.Digits;
        var ok = OrganisationId.TryParse(input, out var oid);
        await Assert.That(ok).IsTrue();
        await Assert.That(ReferenceEquals(oid!.LongFormat(), input)).IsTrue();
    }

    /// <summary>Input with '-' delimiter forces reconstruction.</summary>
    [Test]
    public async Task PersonalId_InputWithDelimiter_AllocatesNewString()
    {
        var input = OfficialNumbers.Modern.LongWithHyphen;
        var ok = PersonalId.TryParse(input, out var pid);
        await Assert.That(ok).IsTrue();
        await Assert.That(ReferenceEquals(pid!.LongFormat(), input)).IsFalse();
    }

    /// <summary>Input with "SE" prefix forces reconstruction.</summary>
    [Test]
    public async Task PersonalId_InputWithSePrefix_AllocatesNewString()
    {
        var input = $"SE{OfficialNumbers.Modern.Long}";
        var ok = PersonalId.TryParse(input, out var pid);
        await Assert.That(ok).IsTrue();
        await Assert.That(ReferenceEquals(pid!.LongFormat(), input)).IsFalse();
    }

    /// <summary>Input with surrounding whitespace forces reconstruction.</summary>
    [Test]
    public async Task PersonalId_InputWithWhitespace_AllocatesNewString()
    {
        var input = $" {OfficialNumbers.Modern.Long} ";
        var ok = PersonalId.TryParse(input, out var pid);
        await Assert.That(ok).IsTrue();
        await Assert.That(ReferenceEquals(pid!.LongFormat(), input)).IsFalse();
    }

    /// <summary>Org input with '-' delimiter forces reconstruction.</summary>
    [Test]
    public async Task OrganisationId_InputWithDelimiter_AllocatesNewString()
    {
        var input = LegalPersonNumbers.Aktiebolag.WithHyphen;
        var ok = OrganisationId.TryParse(input, out var oid);
        await Assert.That(ok).IsTrue();
        await Assert.That(ReferenceEquals(oid!.LongFormat(), input)).IsFalse();
    }
}
