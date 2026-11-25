using Civitas.Id.Sweden.Core;
using Civitas.Id.Sweden.Errors;

namespace Civitas.Id.Sweden.EntityFrameworkCore.Tests.Converters;

public class CoordinationIdConverterTests
{
    public class Construction
    {
        [Test]
        public async Task Constructor_NoArguments_BuildsValidConverter()
        {
            var sut = new CoordinationIdConverter();

            await Assert.That(sut).IsNotNull();
            await Assert.That(sut.ProviderClrType).IsEqualTo(typeof(string));
            await Assert.That(sut.ModelClrType).IsEqualTo(typeof(CoordinationId));
        }
    }

    public class Encode
    {
        [Test]
        public async Task ConvertToProvider_ValidId_ReturnsTwelveDigitString()
        {
            var sut = new CoordinationIdConverter();
            var id = CoordinationId.Parse(OfficialNumbers.CentenarianSamordningsnummer.Long);

            var encoded = (string?)sut.ConvertToProvider(id);

            await Assert.That(encoded).IsEqualTo(OfficialNumbers.CentenarianSamordningsnummer.Long);
            await Assert.That(encoded!.Length).IsEqualTo(12);
        }

        [Test]
        public async Task ConvertToProvider_TwoInstancesSameValue_ProduceSameString()
        {
            var sut = new CoordinationIdConverter();
            var a = CoordinationId.Parse(OfficialNumbers.CentenarianSamordningsnummer.Long);
            var b = CoordinationId.Parse(OfficialNumbers.CentenarianSamordningsnummer.Long);

            var encA = (string?)sut.ConvertToProvider(a);
            var encB = (string?)sut.ConvertToProvider(b);

            await Assert.That(encA).IsEqualTo(encB);
        }
    }

    public class Decode
    {
        [Test]
        public async Task ConvertFromProvider_ValidString_ReturnsEqualId()
        {
            var sut = new CoordinationIdConverter();
            var original = CoordinationId.Parse(OfficialNumbers.CentenarianSamordningsnummer.Long);

            var encoded = (string?)sut.ConvertToProvider(original);
            var decoded = (CoordinationId?)sut.ConvertFromProvider(encoded);

            await Assert.That(decoded).IsEqualTo(original);
        }

        [Test]
        public async Task ConvertFromProvider_LuhnTamperedString_Throws()
        {
            var sut = new CoordinationIdConverter();
            // An official number with another last digit.
            var tampered = OfficialNumbers.CentenarianSamordningsnummer.WithWrongCheckDigit;

            await Assert.That(() => sut.ConvertFromProvider(tampered))
                .Throws<InvalidIdNumberException>();
        }

        [Test]
        public async Task ConvertFromProvider_PersonShapedDay_Throws()
        {
            var sut = new CoordinationIdConverter();
            // A valid PersonalId (day=01, in 01-31 range),
            // which is NOT a valid CoordinationId (day must be in 61-91 range).
            var personShaped = OfficialNumbers.Centenarian.Long;

            await Assert.That(() => sut.ConvertFromProvider(personShaped))
                .Throws<InvalidIdNumberException>();
        }

        [Test]
        public async Task ConvertFromProvider_EmptyString_Throws()
        {
            var sut = new CoordinationIdConverter();

            await Assert.That(() => sut.ConvertFromProvider(string.Empty))
                .Throws<InvalidIdNumberException>();
        }
    }

    public class MappingHints
    {
        [Test]
        public async Task MappingHints_DeclareSizeTwelveNonUnicode()
        {
            var sut = new CoordinationIdConverter();

            await Assert.That(sut.MappingHints).IsNotNull();
            await Assert.That(sut.MappingHints!.Size).IsEqualTo(12);
            await Assert.That(sut.MappingHints.IsUnicode).IsFalse();
        }
    }
}
