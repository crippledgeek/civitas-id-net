using System.Globalization;
using Civitas.Id.Sweden.Core;
using Civitas.Id.Sweden.Errors;
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
        [Test]
        [Arguments("189001019802", typeof(PersonalId))]
        [Arguments("191401682396", typeof(CoordinationId))]
        [Arguments("5560160680", typeof(OrganisationId))]
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
    }

    public class ConvertTo
    {
        [Test]
        public async Task ReturnsLongFormat_ForPersonalId()
        {
            var sut = new SwedishOfficialIdTypeConverter();
            SwedishOfficialId id = PersonalId.Parse("189001019802");
            var result = sut.ConvertTo(null, CultureInfo.InvariantCulture, id, typeof(string));
            await Assert.That(result).IsEqualTo("189001019802");
        }

        [Test]
        public async Task ReturnsLongFormat_ForCoordinationId()
        {
            var sut = new SwedishOfficialIdTypeConverter();
            SwedishOfficialId id = CoordinationId.Parse("191401682396");
            var result = sut.ConvertTo(null, CultureInfo.InvariantCulture, id, typeof(string));
            await Assert.That(result).IsEqualTo("191401682396");
        }

        [Test]
        public async Task ReturnsLongFormat_ForOrganisationId()
        {
            var sut = new SwedishOfficialIdTypeConverter();
            SwedishOfficialId id = OrganisationId.Parse("5560160680");
            var result = sut.ConvertTo(null, CultureInfo.InvariantCulture, id, typeof(string));
            await Assert.That(result).IsEqualTo("5560160680");
        }

        [Test]
        public async Task RoundTrips_String_To_SwedishOfficialId_To_String()
        {
            var sut = new SwedishOfficialIdTypeConverter();
            var id = (SwedishOfficialId?)sut.ConvertFrom(null, CultureInfo.InvariantCulture, "189001019802");
            var roundTripped = sut.ConvertTo(null, CultureInfo.InvariantCulture, id, typeof(string));
            await Assert.That(roundTripped).IsEqualTo("189001019802");
        }
    }
}
