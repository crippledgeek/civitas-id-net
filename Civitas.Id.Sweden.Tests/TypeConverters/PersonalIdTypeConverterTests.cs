using System.Globalization;
using Civitas.Id.Sweden.Core;
using Civitas.Id.Sweden.Errors;
using Civitas.Id.Sweden.TypeConverters;

namespace Civitas.Id.Sweden.Tests.TypeConverters;

/// <summary>
///     Tests for <see cref="PersonalIdTypeConverter" />.
/// </summary>
public class PersonalIdTypeConverterTests
{
    private const string ValidPin12 = "189001019802";
    private const string ValidPin10 = "9001019802";
    private const string ValidPinWithHyphen = "900101-9802";

    public class CanConvertFrom
    {
        [Test]
        public async Task ReturnsTrue_ForString()
        {
            var sut = new PersonalIdTypeConverter();
            await Assert.That(sut.CanConvertFrom(typeof(string))).IsTrue();
        }

        [Test]
        public async Task ReturnsFalse_ForInt()
        {
            var sut = new PersonalIdTypeConverter();
            await Assert.That(sut.CanConvertFrom(typeof(int))).IsFalse();
        }
    }

    public class CanConvertTo
    {
        [Test]
        public async Task ReturnsTrue_ForString()
        {
            var sut = new PersonalIdTypeConverter();
            await Assert.That(sut.CanConvertTo(typeof(string))).IsTrue();
        }

        [Test]
        public async Task ReturnsFalse_ForInt()
        {
            var sut = new PersonalIdTypeConverter();
            await Assert.That(sut.CanConvertTo(typeof(int))).IsFalse();
        }
    }

    public class ConvertFrom
    {
        [Test]
        [Arguments(ValidPin12)]
        [Arguments(ValidPin10)]
        [Arguments(ValidPinWithHyphen)]
        public async Task Parses_ValidString(string input)
        {
            var sut = new PersonalIdTypeConverter();
            var result = sut.ConvertFrom(null, CultureInfo.InvariantCulture, input);
            await Assert.That(result).IsTypeOf<PersonalId>();
        }

        [Test]
        public async Task RoundTrips_ToCanonicalLongFormat()
        {
            var sut = new PersonalIdTypeConverter();
            var result = (PersonalId?)sut.ConvertFrom(null, CultureInfo.InvariantCulture, ValidPin12);
            await Assert.That(result).IsNotNull();
            await Assert.That(result!.LongFormat()).IsEqualTo(ValidPin12);
        }

        [Test]
        public async Task Throws_InvalidIdNumberException_OnMalformedString()
        {
            var sut = new PersonalIdTypeConverter();
            var ex = await Assert.That(() => sut.ConvertFrom(null, CultureInfo.InvariantCulture, "not-a-pnr"))
                .Throws<InvalidIdNumberException>();
            await Assert.That(ex!.Reason).IsEqualTo(InvalidIdNumberReason.InvalidFormat);
        }

        [Test]
        public async Task Throws_InvalidIdNumberException_OnInvalidChecksum()
        {
            var sut = new PersonalIdTypeConverter();
            const string input = "189001019800";
            var ex = await Assert.That(() => sut.ConvertFrom(null, CultureInfo.InvariantCulture, input))
                .Throws<InvalidIdNumberException>();
            // The current parser surfaces all failure modes as InvalidFormat;
            // when the parser is refined to distinguish checksum failures
            // (out of scope for v1.0), the assertion should tighten to
            // InvalidIdNumberReason.InvalidChecksum. Until then, assert
            // that the exception's redacted input preserves the original
            // prefix/suffix — a regression guard against the converter
            // swallowing the value before constructing the exception.
            await Assert.That(ex!.RedactedInput).IsEqualTo("18******9800");
        }

        [Test]
        public async Task Throws_OnEmptyString()
        {
            var sut = new PersonalIdTypeConverter();
            await Assert.That(() => sut.ConvertFrom(null, CultureInfo.InvariantCulture, ""))
                .Throws<InvalidIdNumberException>();
        }

        [Test]
        public async Task Delegates_ToBase_OnNonStringValue()
        {
            // The `_ => base.ConvertFrom(...)` arm — base.ConvertFrom throws NotSupportedException
            // when the source type is not supported. This exercises the fallthrough branch.
            var sut = new PersonalIdTypeConverter();
            await Assert.That(() => sut.ConvertFrom(null, CultureInfo.InvariantCulture, 42))
                .Throws<NotSupportedException>();
        }
    }

    public class ConvertTo
    {
        [Test]
        public async Task ReturnsLongFormat()
        {
            var sut = new PersonalIdTypeConverter();
            var id = PersonalId.Parse(ValidPin12);
            var result = sut.ConvertTo(null, CultureInfo.InvariantCulture, id, typeof(string));
            await Assert.That(result).IsEqualTo(ValidPin12);
        }

        [Test]
        public async Task RoundTrips_String_To_PersonalId_To_String()
        {
            var sut = new PersonalIdTypeConverter();
            var id = (PersonalId?)sut.ConvertFrom(null, CultureInfo.InvariantCulture, ValidPin12);
            var roundTripped = sut.ConvertTo(null, CultureInfo.InvariantCulture, id, typeof(string));
            await Assert.That(roundTripped).IsEqualTo(ValidPin12);
        }

        [Test]
        public async Task Delegates_ToBase_WhenDestinationTypeIsNotString()
        {
            // `base.ConvertTo` arm — throws NotSupportedException for unsupported dest types.
            var sut = new PersonalIdTypeConverter();
            var id = PersonalId.Parse(ValidPin12);
            await Assert.That(() => sut.ConvertTo(null, CultureInfo.InvariantCulture, id, typeof(int)))
                .Throws<NotSupportedException>();
        }

    }
}
