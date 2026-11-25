using System.Globalization;
using System.Text;
using Civitas.Id.Sweden.Core;
using Civitas.Id.Sweden.Errors;
using Civitas.Id.Sweden.Fakers;

namespace Civitas.Id.Sweden.Tests.Core;

/// <summary>
///     Every parse and format contract accepts any <see cref="IFormatProvider" />, including
///     null (plain interpolation passes null), and gives the same result for each.
/// </summary>
public class FormatProviderContractTests
{
    private const int FakerSeed = 1;
    private const int BufferLength = 32; // longer than the longest form, 13 characters
    private const string LongFormat = "L";
    private const string NotAnId = "not-an-id";

    public static IEnumerable<Func<IFormatProvider?>> Providers()
    {
        yield return () => null;
        yield return () => CultureInfo.InvariantCulture;
    }

    [Test]
    [MethodDataSource(nameof(Providers))]
    public async Task PersonalId_AcceptsProvider(IFormatProvider? provider)
    {
        await AssertContracts(PersonalIdFaker.WithSeed(FakerSeed).Generate(), provider).ConfigureAwait(false);
    }

    [Test]
    [MethodDataSource(nameof(Providers))]
    public async Task CoordinationId_AcceptsProvider(IFormatProvider? provider)
    {
        await AssertContracts(CoordinationIdFaker.WithSeed(FakerSeed).Generate(), provider).ConfigureAwait(false);
    }

    [Test]
    [MethodDataSource(nameof(Providers))]
    public async Task OrganisationId_AcceptsProvider(IFormatProvider? provider)
    {
        await AssertContracts(OrganisationIdFaker.WithSeed(FakerSeed).GenerateLegalPerson(), provider)
            .ConfigureAwait(false);
    }

    private static async Task AssertContracts<T>(T id, IFormatProvider? provider)
        where T : SwedishOfficialId, ISpanParsable<T>, IUtf8SpanParsable<T>, ISpanFormattable, IUtf8SpanFormattable
    {
        var text = id.LongFormat();
        var utf8 = Encoding.UTF8.GetBytes(text);

        await Assert.That(T.Parse(text, provider)).IsEqualTo(id);
        await Assert.That(T.Parse(text.AsSpan(), provider)).IsEqualTo(id);
        await Assert.That(T.Parse(utf8, provider)).IsEqualTo(id);
        await Assert.That(T.TryParse(text, provider, out var fromString)).IsTrue();
        await Assert.That(fromString).IsEqualTo(id);
        await Assert.That(T.TryParse(text.AsSpan(), provider, out var fromSpan)).IsTrue();
        await Assert.That(fromSpan).IsEqualTo(id);
        await Assert.That(T.TryParse(utf8, provider, out var fromUtf8)).IsTrue();
        await Assert.That(fromUtf8).IsEqualTo(id);

        var notAnIdUtf8 = Encoding.UTF8.GetBytes(NotAnId);
        await Assert.That(T.TryParse(NotAnId, provider, out _)).IsFalse();
        await Assert.That(T.TryParse(NotAnId.AsSpan(), provider, out _)).IsFalse();
        await Assert.That(T.TryParse(notAnIdUtf8, provider, out _)).IsFalse();
        await Assert.That(() => T.Parse(NotAnId, provider)).ThrowsExactly<InvalidIdNumberException>();
        await Assert.That(() => T.Parse(NotAnId.AsSpan(), provider)).ThrowsExactly<InvalidIdNumberException>();
        await Assert.That(() => T.Parse(notAnIdUtf8, provider)).ThrowsExactly<InvalidIdNumberException>();

        await Assert.That(id.ToString(LongFormat, provider)).IsEqualTo(text);
        var chars = new char[BufferLength];
        await Assert.That(id.TryFormat(chars, out var charsWritten, LongFormat, provider)).IsTrue();
        await Assert.That(new string(chars, 0, charsWritten)).IsEqualTo(text);
        var bytes = new byte[BufferLength];
        await Assert.That(id.TryFormat(bytes, out var bytesWritten, LongFormat, provider)).IsTrue();
        await Assert.That(Encoding.UTF8.GetString(bytes, 0, bytesWritten)).IsEqualTo(text);
    }
}
