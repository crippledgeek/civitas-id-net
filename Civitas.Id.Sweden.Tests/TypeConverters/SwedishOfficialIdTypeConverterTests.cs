using System.Globalization;
using Civitas.Id.Sweden.Core;
using Civitas.Id.Sweden.Errors;
using Civitas.Id.Sweden.Tests.Helpers;
using Civitas.Id.Sweden.TypeConverters;

namespace Civitas.Id.Sweden.Tests.TypeConverters;

/// <summary>
///     Tests for <see cref="SwedishOfficialIdTypeConverter" /> (composite — dispatches
///     to any of the three subtypes via <see cref="SwedishOfficialId.TryParseAny(string?, out SwedishOfficialId?)" />).
/// </summary>
public class SwedishOfficialIdTypeConverterTests
{
    public class CanConvertFrom
    {
        [Test]
        public async Task ReturnsTrue_ForString()
        {
            var sut = new SwedishOfficialIdTypeConverter();
            await Assert.That(sut.CanConvertFrom(typeof(string))).IsTrue();
        }

        [Test]
        public async Task ReturnsFalse_ForInt()
        {
            var sut = new SwedishOfficialIdTypeConverter();
            await Assert.That(sut.CanConvertFrom(typeof(int))).IsFalse();
        }
    }

    public class CanConvertTo
    {
        [Test]
        public async Task ReturnsTrue_ForString()
        {
            var sut = new SwedishOfficialIdTypeConverter();
            await Assert.That(sut.CanConvertTo(typeof(string))).IsTrue();
        }

        [Test]
        public async Task ReturnsFalse_ForInt()
        {
            var sut = new SwedishOfficialIdTypeConverter();
            await Assert.That(sut.CanConvertTo(typeof(int))).IsFalse();
        }
    }

    public class ConvertFrom
    {
        public static IEnumerable<(string Input, Type ExpectedType)> DispatchesToCorrectSubtypeInputs()
        {
            yield return (OfficialNumbers.Centenarian.Long, typeof(PersonalId));
            yield return (OfficialNumbers.CentenarianSamordningsnummer.Long, typeof(CoordinationId));
            yield return (LegalPersonNumbers.Aktiebolag.Digits, typeof(OrganisationId));
        }

        [Test]
        [MethodDataSource(nameof(DispatchesToCorrectSubtypeInputs))]
        public async Task DispatchesToCorrectSubtype(string input, Type expectedType)
        {
            var sut = new SwedishOfficialIdTypeConverter();
            var result = sut.ConvertFrom(null, CultureInfo.InvariantCulture, input);
            await Assert.That(result).IsNotNull();
            await Assert.That(result!.GetType()).IsEqualTo(expectedType);
        }

        [Test]
        public async Task Throws_OnUnknownInput()
        {
            var sut = new SwedishOfficialIdTypeConverter();
            var ex = await Assert.That(() => sut.ConvertFrom(null, CultureInfo.InvariantCulture, "garbage"))
                .Throws<InvalidIdNumberException>();
            await Assert.That(ex!.Reason).IsEqualTo(InvalidIdNumberReason.UnsupportedIdType);
        }

        [Test]
        public async Task Throws_OnEmptyString()
        {
            var sut = new SwedishOfficialIdTypeConverter();
            await Assert.That(() => sut.ConvertFrom(null, CultureInfo.InvariantCulture, ""))
                .Throws<InvalidIdNumberException>();
        }

        [Test]
        public async Task Delegates_ToBase_OnNonStringValue()
        {
            // `_ => base.ConvertFrom(...)` arm — base throws NotSupportedException for int.
            var sut = new SwedishOfficialIdTypeConverter();
            await Assert.That(() => sut.ConvertFrom(null, CultureInfo.InvariantCulture, 3.14))
                .Throws<NotSupportedException>();
        }
    }

    public class ConvertTo
    {
        [Test]
        public async Task ReturnsLongFormat_ForPersonalId()
        {
            var sut = new SwedishOfficialIdTypeConverter();
            SwedishOfficialId id = PersonalId.Parse(OfficialNumbers.Centenarian.Long);
            var result = sut.ConvertTo(null, CultureInfo.InvariantCulture, id, typeof(string));
            await Assert.That(result).IsEqualTo(OfficialNumbers.Centenarian.Long);
        }

        [Test]
        public async Task ReturnsLongFormat_ForCoordinationId()
        {
            var sut = new SwedishOfficialIdTypeConverter();
            SwedishOfficialId id = CoordinationId.Parse(OfficialNumbers.CentenarianSamordningsnummer.Long);
            var result = sut.ConvertTo(null, CultureInfo.InvariantCulture, id, typeof(string));
            await Assert.That(result).IsEqualTo(OfficialNumbers.CentenarianSamordningsnummer.Long);
        }

        [Test]
        public async Task ReturnsLongFormat_ForOrganisationId()
        {
            var sut = new SwedishOfficialIdTypeConverter();
            SwedishOfficialId id = OrganisationId.Parse(LegalPersonNumbers.Aktiebolag.Digits);
            var result = sut.ConvertTo(null, CultureInfo.InvariantCulture, id, typeof(string));
            await Assert.That(result).IsEqualTo(LegalPersonNumbers.Aktiebolag.Digits);
        }

        [Test]
        public async Task RoundTrips_String_To_SwedishOfficialId_To_String()
        {
            var sut = new SwedishOfficialIdTypeConverter();
            var id = (SwedishOfficialId?)sut.ConvertFrom(null, CultureInfo.InvariantCulture, OfficialNumbers.Centenarian.Long);
            var roundTripped = sut.ConvertTo(null, CultureInfo.InvariantCulture, id, typeof(string));
            await Assert.That(roundTripped).IsEqualTo(OfficialNumbers.Centenarian.Long);
        }

        [Test]
        public async Task Delegates_ToBase_WhenDestinationTypeIsNotString()
        {
            var sut = new SwedishOfficialIdTypeConverter();
            SwedishOfficialId id = PersonalId.Parse(OfficialNumbers.Centenarian.Long);
            await Assert.That(() => sut.ConvertTo(null, CultureInfo.InvariantCulture, id, typeof(int)))
                .Throws<NotSupportedException>();
        }

    }
}
