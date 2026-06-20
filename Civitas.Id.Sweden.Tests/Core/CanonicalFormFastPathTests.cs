using Civitas.Id.Sweden.Core;

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
        const string input = "200001019801";
        var ok = PersonalId.TryParse(input, out var pid);
        await Assert.That(ok).IsTrue();
        await Assert.That(ReferenceEquals(pid!.LongFormat(), input)).IsTrue();
    }

    /// <summary>10-digit personnummer (no century) requires reconstruction; new string allocated.</summary>
    [Test]
    public async Task PersonalId_NonCanonicalInput_AllocatesNewString()
    {
        const string input = "0001019801"; // 10-digit, no century — needs reconstruction
        var ok = PersonalId.TryParse(input, out var pid);
        await Assert.That(ok).IsTrue();
        await Assert.That(ReferenceEquals(pid!.LongFormat(), input)).IsFalse();
    }

    /// <summary>Canonical 10-digit orgnummer reuses input string instance.</summary>
    [Test]
    public async Task OrganisationId_CanonicalInput_ReusesInputString()
    {
        const string input = "5560160680";
        var ok = OrganisationId.TryParse(input, out var oid);
        await Assert.That(ok).IsTrue();
        await Assert.That(ReferenceEquals(oid!.LongFormat(), input)).IsTrue();
    }

    /// <summary>Input with '-' delimiter forces reconstruction.</summary>
    [Test]
    public async Task PersonalId_InputWithDelimiter_AllocatesNewString()
    {
        const string input = "20000101-9801";
        var ok = PersonalId.TryParse(input, out var pid);
        await Assert.That(ok).IsTrue();
        await Assert.That(ReferenceEquals(pid!.LongFormat(), input)).IsFalse();
    }

    /// <summary>Input with "SE" prefix forces reconstruction.</summary>
    [Test]
    public async Task PersonalId_InputWithSePrefix_AllocatesNewString()
    {
        const string input = "SE200001019801";
        var ok = PersonalId.TryParse(input, out var pid);
        await Assert.That(ok).IsTrue();
        await Assert.That(ReferenceEquals(pid!.LongFormat(), input)).IsFalse();
    }

    /// <summary>Input with surrounding whitespace forces reconstruction.</summary>
    [Test]
    public async Task PersonalId_InputWithWhitespace_AllocatesNewString()
    {
        const string input = " 200001019801 ";
        var ok = PersonalId.TryParse(input, out var pid);
        await Assert.That(ok).IsTrue();
        await Assert.That(ReferenceEquals(pid!.LongFormat(), input)).IsFalse();
    }

    /// <summary>Org input with '-' delimiter forces reconstruction.</summary>
    [Test]
    public async Task OrganisationId_InputWithDelimiter_AllocatesNewString()
    {
        const string input = "556016-0680";
        var ok = OrganisationId.TryParse(input, out var oid);
        await Assert.That(ok).IsTrue();
        await Assert.That(ReferenceEquals(oid!.LongFormat(), input)).IsFalse();
    }
}
