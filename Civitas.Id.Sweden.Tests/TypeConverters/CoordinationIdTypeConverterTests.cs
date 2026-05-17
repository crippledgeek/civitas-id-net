using System.Globalization;
using Civitas.Id.Sweden.Core;
using Civitas.Id.Sweden.Errors;
using Civitas.Id.Sweden.TypeConverters;

namespace Civitas.Id.Sweden.Tests.TypeConverters;

/// <summary>
///     Tests for <see cref="CoordinationIdTypeConverter" />.
/// </summary>
public class CoordinationIdTypeConverterTests
{
    private const string ValidPin12 = "191401682396";
    private const string ValidPinWithHyphen = "140168-2396";

    public class CanConvertFrom
    {
        [Test]
        public async Task ReturnsTrue_ForString()
        {
            var sut = new CoordinationIdTypeConverter();
            await Assert.That(sut.CanConvertFrom(typeof(string))).IsTrue();
        }

        [Test]
        public async Task ReturnsFalse_ForInt()
        {
            var sut = new CoordinationIdTypeConverter();
            await Assert.That(sut.CanConvertFrom(typeof(int))).IsFalse();
        }
    }

    public class CanConvertTo
    {
        [Test]
        public async Task ReturnsTrue_ForString()
        {
            var sut = new CoordinationIdTypeConverter();
            await Assert.That(sut.CanConvertTo(typeof(string))).IsTrue();
        }

        [Test]
        public async Task ReturnsFalse_ForInt()
        {
            var sut = new CoordinationIdTypeConverter();
            await Assert.That(sut.CanConvertTo(typeof(int))).IsFalse();
        }
    }

    public class ConvertFrom
    {
        [Test]
        [Arguments(ValidPin12)]
        [Arguments(ValidPinWithHyphen)]
        public async Task Parses_ValidString(string input)
        {
            var sut = new CoordinationIdTypeConverter();
            var result = sut.ConvertFrom(null, CultureInfo.InvariantCulture, input);
            await Assert.That(result).IsTypeOf<CoordinationId>();
        }

        [Test]
        public async Task RoundTrips_ToCanonicalLongFormat()
        {
            var sut = new CoordinationIdTypeConverter();
            var result = (CoordinationId?)sut.ConvertFrom(null, CultureInfo.InvariantCulture, ValidPin12);
            await Assert.That(result).IsNotNull();
            await Assert.That(result!.LongFormat()).IsEqualTo(ValidPin12);
        }

        [Test]
        public async Task Throws_InvalidIdNumberException_OnMalformedString()
        {
            var sut = new CoordinationIdTypeConverter();
            var ex = await Assert.That(() => sut.ConvertFrom(null, CultureInfo.InvariantCulture, "not-a-samordningsnummer"))
                .Throws<InvalidIdNumberException>();
            await Assert.That(ex!.Reason).IsEqualTo(InvalidIdNumberReason.InvalidFormat);
        }

        [Test]
        public async Task Throws_OnPersonalIdInput()
        {
            // A valid personnummer (day 01) is NOT a valid samordningsnummer (day must be +60).
            var sut = new CoordinationIdTypeConverter();
            await Assert.That(() => sut.ConvertFrom(null, CultureInfo.InvariantCulture, "189001019802"))
                .Throws<InvalidIdNumberException>();
        }

        [Test]
        public async Task Throws_OnEmptyString()
        {
            var sut = new CoordinationIdTypeConverter();
            await Assert.That(() => sut.ConvertFrom(null, CultureInfo.InvariantCulture, ""))
                .Throws<InvalidIdNumberException>();
        }
    }

    public class ConvertTo
    {
        [Test]
        public async Task ReturnsLongFormat()
        {
            var sut = new CoordinationIdTypeConverter();
            var id = CoordinationId.Parse(ValidPin12);
            var result = sut.ConvertTo(null, CultureInfo.InvariantCulture, id, typeof(string));
            await Assert.That(result).IsEqualTo(ValidPin12);
        }

        [Test]
        public async Task RoundTrips_String_To_CoordinationId_To_String()
        {
            var sut = new CoordinationIdTypeConverter();
            var id = (CoordinationId?)sut.ConvertFrom(null, CultureInfo.InvariantCulture, ValidPin12);
            var roundTripped = sut.ConvertTo(null, CultureInfo.InvariantCulture, id, typeof(string));
            await Assert.That(roundTripped).IsEqualTo(ValidPin12);
        }
    }
}
