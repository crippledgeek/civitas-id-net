using System.Text;
using Civitas.Id.Sweden.Core;
using Civitas.Id.Sweden.Format;

namespace Civitas.Id.Sweden.Tests.Core;

/// <summary>
///     Pins <see cref="IUtf8SpanFormattable"/> + <see cref="IUtf8SpanParsable{T}"/>
///     contracts: UTF-8 bytes round-trip through Parse/TryFormat and produce
///     identical results to the UTF-16 path. Swedish IDs are pure ASCII so
///     UTF-8 == ASCII encoding (1 byte per char).
/// </summary>
public class Utf8RoundTripTests
{
    private const string ValidPersonal = "200001019801";
    private const string ValidCoordination = "200001612399";
    private const string ValidOrg = "5560160680";

    /// <summary>PersonalId UTF-8 parse + format round-trips bit-for-bit.</summary>
    [Test]
    public async Task PersonalId_Utf8RoundTrip_BitForBit()
    {
        var utf8 = Encoding.UTF8.GetBytes(ValidPersonal);
        var ok = PersonalId.TryParse(utf8, null, out var pid);
        await Assert.That(ok).IsTrue();

        var buffer = new byte[16];
        var fmtOk = pid!.TryFormat(buffer, out var written, "L", null);
        await Assert.That(fmtOk).IsTrue();
        await Assert.That(Encoding.UTF8.GetString(buffer, 0, written)).IsEqualTo(ValidPersonal);
    }

    /// <summary>CoordinationId UTF-8 parse + format round-trips bit-for-bit.</summary>
    [Test]
    public async Task CoordinationId_Utf8RoundTrip_BitForBit()
    {
        var utf8 = Encoding.UTF8.GetBytes(ValidCoordination);
        var ok = CoordinationId.TryParse(utf8, null, out var cid);
        await Assert.That(ok).IsTrue();

        var buffer = new byte[16];
        var fmtOk = cid!.TryFormat(buffer, out var written, "L", null);
        await Assert.That(fmtOk).IsTrue();
        await Assert.That(Encoding.UTF8.GetString(buffer, 0, written)).IsEqualTo(ValidCoordination);
    }

    /// <summary>OrganisationId UTF-8 parse + format round-trips bit-for-bit.</summary>
    [Test]
    public async Task OrganisationId_Utf8RoundTrip_BitForBit()
    {
        var utf8 = Encoding.UTF8.GetBytes(ValidOrg);
        var ok = OrganisationId.TryParse(utf8, null, out var oid);
        await Assert.That(ok).IsTrue();

        var buffer = new byte[16];
        var fmtOk = oid!.TryFormat(buffer, out var written, "L", null);
        await Assert.That(fmtOk).IsTrue();
        await Assert.That(Encoding.UTF8.GetString(buffer, 0, written)).IsEqualTo(ValidOrg);
    }

    /// <summary>UTF-8 parse produces semantically identical result to UTF-16 path.</summary>
    [Test]
    public async Task PersonalId_Utf8AndUtf16_PathsAgree()
    {
        var utf8 = Encoding.UTF8.GetBytes(ValidPersonal);
        var utf16Ok = PersonalId.TryParse(ValidPersonal, out var utf16);
        var utf8Ok = PersonalId.TryParse(utf8, null, out var utf8Parsed);
        await Assert.That(utf16Ok).IsTrue();
        await Assert.That(utf8Ok).IsTrue();
        await Assert.That(utf16).IsEqualTo(utf8Parsed);
    }

    /// <summary>Non-ASCII byte in the input is rejected cleanly.</summary>
    [Test]
    public async Task PersonalId_NonAsciiUtf8_ReturnsFalse()
    {
        // Replace one ASCII byte with a UTF-8 lead byte > 127 → invalid for Swedish ID.
        var utf8 = Encoding.UTF8.GetBytes(ValidPersonal);
        utf8[0] = 0xC3; // start of a 2-byte UTF-8 sequence
        var ok = PersonalId.TryParse(utf8, null, out var pid);
        await Assert.That(ok).IsFalse();
        await Assert.That(pid).IsNull();
    }

    /// <summary>OrganisationId rejects non-ASCII UTF-8 input cleanly.</summary>
    [Test]
    public async Task OrganisationId_NonAsciiUtf8_ReturnsFalse()
    {
        var utf8 = Encoding.UTF8.GetBytes(ValidOrg);
        utf8[1] = 0xE2; // start of a 3-byte UTF-8 sequence
        var ok = OrganisationId.TryParse(utf8, null, out var oid);
        await Assert.That(ok).IsFalse();
        await Assert.That(oid).IsNull();
    }

    /// <summary>UTF-8 format into too-small destination returns false + zero bytesWritten.</summary>
    [Test]
    public async Task PersonalId_Utf8TryFormat_TooSmall_ReturnsFalse()
    {
        var pid = PersonalId.Parse(ValidPersonal);
        var buffer = new byte[4];
        var ok = pid.TryFormat(buffer, out var written, "L", null);
        await Assert.That(ok).IsFalse();
        await Assert.That(written).IsEqualTo(0);
    }

    /// <summary>UTF-8 format with PnrFormat and deterministic today matches the char path.</summary>
    [Test]
    public async Task PersonalId_Utf8TryFormat_DeterministicToday_MatchesCharPath()
    {
        var pid = PersonalId.Parse(ValidPersonal);
        var today = new DateOnly(2030, 6, 1);
        var buffer = new byte[16];
        var ok = pid.TryFormat(buffer, PnrFormat.LongFormatWithStandardSeparator, today, out var written);
        await Assert.That(ok).IsTrue();
        var utf8String = Encoding.UTF8.GetString(buffer, 0, written);
        await Assert.That(utf8String).IsEqualTo(pid.Format(PnrFormat.LongFormatWithStandardSeparator, today));
    }

    /// <summary>UTF-8 Parse (throwing) on invalid input throws InvalidIdNumberException.</summary>
    [Test]
    public async Task PersonalId_Utf8Parse_InvalidInput_Throws()
    {
        var utf8 = "0000000000"u8.ToArray();
        await Assert.That(() => PersonalId.Parse(utf8, null))
            .Throws<Civitas.Id.Sweden.Errors.InvalidIdNumberException>();
    }

    // ── CoordinationId empty UTF-8 branches ──
    // These cover the `if (utf8Source.Length is 0 or > 100) return false;` guard
    // in PhysicalPersonId.TryParseUtf8Core via the CoordinationId path.

    /// <summary>CoordinationId.TryParse returns false for an empty UTF-8 span.</summary>
    [Test]
    public async Task CoordinationId_Utf8TryParse_EmptySpan_ReturnsFalse()
    {
        var ok = CoordinationId.TryParse(ReadOnlySpan<byte>.Empty, null, out var result);
        await Assert.That(ok).IsFalse();
        await Assert.That(result).IsNull();
    }

    /// <summary>CoordinationId.Parse throws InvalidIdNumberException for an empty UTF-8 span.</summary>
    [Test]
    public async Task CoordinationId_Utf8Parse_EmptySpan_ThrowsInvalidIdNumberException()
    {
        var emptyBytes = Array.Empty<byte>();
        await Assert.That(() => CoordinationId.Parse(emptyBytes.AsSpan(), null))
            .Throws<Civitas.Id.Sweden.Errors.InvalidIdNumberException>();
    }
}
