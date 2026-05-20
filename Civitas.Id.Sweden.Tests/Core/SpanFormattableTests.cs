using Civitas.Id.Sweden.Core;
using Civitas.Id.Sweden.Format;

namespace Civitas.Id.Sweden.Tests.Core;

/// <summary>
///     Pins <see cref="ISpanFormattable"/> + <see cref="IFormattable"/> contracts
///     on PersonalId, CoordinationId, OrganisationId. Format string mnemonics:
///     null/""/"L" = LongFormat; "S" = ShortFormat; "LD"/"SD" = with '-' separator;
///     "LI"/"SI" = with inferred '+'/'-' separator.
/// </summary>
public class SpanFormattableTests
{
    private const string ValidPersonal = "200001019801";   // 12-digit personnummer
    private const string ValidOrg = "5560160680";          // 10-digit Aktiebolag

    /// <summary>Default specifier (null) returns the LongFormat output for a person ID.</summary>
    [Test]
    public async Task PersonalId_ToStringNull_ReturnsLongFormat()
    {
        var pid = PersonalId.Parse(ValidPersonal);
#pragma warning disable CA1859 // Test exercises the IFormattable contract explicitly.
        IFormattable f = pid;
#pragma warning restore CA1859
        var s = f.ToString(null, null);
        await Assert.That(s).IsEqualTo(pid.Format(PnrFormat.LongFormat));
    }

    /// <summary>"L" specifier returns long-format equivalent.</summary>
    [Test]
    public async Task PersonalId_ToStringL_ReturnsLongFormat()
    {
        var pid = PersonalId.Parse(ValidPersonal);
#pragma warning disable CA1859 // Test exercises the IFormattable contract explicitly.
        IFormattable f = pid;
#pragma warning restore CA1859
        await Assert.That(f.ToString("L", null)).IsEqualTo(pid.Format(PnrFormat.LongFormat));
    }

    /// <summary>"S" specifier returns short-format equivalent.</summary>
    [Test]
    public async Task PersonalId_ToStringS_ReturnsShortFormat()
    {
        var pid = PersonalId.Parse(ValidPersonal);
#pragma warning disable CA1859 // Test exercises the IFormattable contract explicitly.
        IFormattable f = pid;
#pragma warning restore CA1859
        await Assert.That(f.ToString("S", null)).IsEqualTo(pid.Format(PnrFormat.ShortFormat));
    }

    /// <summary>"LD" specifier returns long-with-standard-separator equivalent.</summary>
    [Test]
    public async Task PersonalId_ToStringLD_ReturnsLongWithStandardSeparator()
    {
        var pid = PersonalId.Parse(ValidPersonal);
#pragma warning disable CA1859 // Test exercises the IFormattable contract explicitly.
        IFormattable f = pid;
#pragma warning restore CA1859
        await Assert.That(f.ToString("LD", null))
            .IsEqualTo(pid.Format(PnrFormat.LongFormatWithStandardSeparator));
    }

    /// <summary>"SD" specifier returns short-with-standard-separator equivalent.</summary>
    [Test]
    public async Task PersonalId_ToStringSD_ReturnsShortWithStandardSeparator()
    {
        var pid = PersonalId.Parse(ValidPersonal);
#pragma warning disable CA1859 // Test exercises the IFormattable contract explicitly.
        IFormattable f = pid;
#pragma warning restore CA1859
        await Assert.That(f.ToString("SD", null))
            .IsEqualTo(pid.Format(PnrFormat.ShortFormatWithStandardSeparator));
    }

    /// <summary>Unrecognised specifier raises FormatException.</summary>
    [Test]
    public async Task PersonalId_ToString_UnknownSpec_Throws()
    {
        var pid = PersonalId.Parse(ValidPersonal);
#pragma warning disable CA1859 // Test exercises the IFormattable contract explicitly.
        IFormattable f = pid;
#pragma warning restore CA1859
        var ex = await Assert.That(() => f.ToString("XYZ", null)).Throws<FormatException>();
        await Assert.That(ex).IsNotNull();
    }

    /// <summary>TryFormat into a sufficient buffer returns true with expected output.</summary>
    [Test]
    public async Task PersonalId_TryFormat_SufficientDestination_WritesCorrectly()
    {
        var pid = PersonalId.Parse(ValidPersonal);
        bool ok;
        int written;
        string formatted;
        {
            Span<char> buffer = stackalloc char[16];
#pragma warning disable CA1859 // Test exercises the ISpanFormattable contract explicitly.
            ISpanFormattable f = pid;
#pragma warning restore CA1859
            ok = f.TryFormat(buffer, out written, "LD", null);
            formatted = new string(buffer[..written]);
        }
        await Assert.That(ok).IsTrue();
        await Assert.That(written).IsEqualTo(13);
        await Assert.That(formatted)
            .IsEqualTo(pid.Format(PnrFormat.LongFormatWithStandardSeparator));
    }

    /// <summary>TryFormat into a too-small buffer returns false and zero charsWritten.</summary>
    [Test]
    public async Task PersonalId_TryFormat_TooSmallDestination_ReturnsFalse()
    {
        var pid = PersonalId.Parse(ValidPersonal);
        Span<char> buffer = stackalloc char[5];
#pragma warning disable CA1859 // Test exercises the ISpanFormattable contract explicitly.
        ISpanFormattable f = pid;
#pragma warning restore CA1859
        var ok = f.TryFormat(buffer, out var written, "L", null);
        await Assert.That(ok).IsFalse();
        await Assert.That(written).IsEqualTo(0);
    }

    /// <summary>TryFormat with deterministic today overload matches Format(PnrFormat, today).</summary>
    [Test]
    public async Task PersonalId_TryFormat_DeterministicToday_MatchesFormatString()
    {
        var pid = PersonalId.Parse(ValidPersonal);
        var today = new DateOnly(2030, 6, 1);
        bool ok;
        string formatted;
        {
            Span<char> buffer = stackalloc char[16];
            ok = pid.TryFormat(buffer, PnrFormat.LongFormatWithSeparator, today, out var written);
            formatted = new string(buffer[..written]);
        }
        await Assert.That(ok).IsTrue();
        await Assert.That(formatted)
            .IsEqualTo(pid.Format(PnrFormat.LongFormatWithSeparator, today));
    }

    /// <summary>OrganisationId implements ISpanFormattable; default returns 10-digit.</summary>
    [Test]
    public async Task OrganisationId_ToString_DefaultReturns10Digits()
    {
        var oid = OrganisationId.Parse(ValidOrg);
#pragma warning disable CA1859 // Test exercises the IFormattable contract explicitly.
        IFormattable f = oid;
#pragma warning restore CA1859
        await Assert.That(f.ToString(null, null)).IsEqualTo(ValidOrg);
    }

    /// <summary>OrganisationId "LD" produces canonical with-separator form.</summary>
    [Test]
    public async Task OrganisationId_ToStringLD_MatchesFormatString()
    {
        var oid = OrganisationId.Parse(ValidOrg);
#pragma warning disable CA1859 // Test exercises the IFormattable contract explicitly.
        IFormattable f = oid;
#pragma warning restore CA1859
        await Assert.That(f.ToString("LD", null))
            .IsEqualTo(oid.Format(PnrFormat.LongFormatWithStandardSeparator));
    }

    /// <summary>OrganisationId TryFormat with too-small buffer returns false.</summary>
    [Test]
    public async Task OrganisationId_TryFormat_TooSmall_ReturnsFalse()
    {
        var oid = OrganisationId.Parse(ValidOrg);
        Span<char> buffer = stackalloc char[3];
#pragma warning disable CA1859 // Test exercises the ISpanFormattable contract explicitly.
        ISpanFormattable f = oid;
#pragma warning restore CA1859
        var ok = f.TryFormat(buffer, out var written, "L", null);
        await Assert.That(ok).IsFalse();
        await Assert.That(written).IsEqualTo(0);
    }

    /// <summary>OrganisationId TryFormat deterministic overload matches Format(format, today).</summary>
    [Test]
    public async Task OrganisationId_TryFormat_DeterministicToday_MatchesFormatString()
    {
        var oid = OrganisationId.Parse(ValidOrg);
        var today = new DateOnly(2030, 6, 1);
        bool ok;
        string formatted;
        {
            Span<char> buffer = stackalloc char[16];
            ok = oid.TryFormat(buffer, PnrFormat.LongFormatWithStandardSeparator, today, out var written);
            formatted = new string(buffer[..written]);
        }
        await Assert.That(ok).IsTrue();
        await Assert.That(formatted)
            .IsEqualTo(oid.Format(PnrFormat.LongFormatWithStandardSeparator, today));
    }
}
