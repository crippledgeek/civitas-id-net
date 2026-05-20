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
            var id = CoordinationId.Parse("191401682396");

            var encoded = (string?)sut.ConvertToProvider(id);

            await Assert.That(encoded).IsEqualTo("191401682396");
            await Assert.That(encoded!.Length).IsEqualTo(12);
        }

        [Test]
        public async Task ConvertToProvider_TwoInstancesSameValue_ProduceSameString()
        {
            var sut = new CoordinationIdConverter();
            var a = CoordinationId.Parse("191401682396");
            var b = CoordinationId.Parse("191401682396");

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
            var original = CoordinationId.Parse("191401682396");

            var encoded = (string?)sut.ConvertToProvider(original);
            var decoded = (CoordinationId?)sut.ConvertFromProvider(encoded);

            await Assert.That(decoded).IsEqualTo(original);
        }

        [Test]
        public async Task ConvertFromProvider_LuhnTamperedString_Throws()
        {
            var sut = new CoordinationIdConverter();
            // Original "191401682396" — flip the last digit (6 → 7):
            // Luhn check digit for body 140168239 is 6; 7 fails the checksum.
            const string tampered = "191401682397";

            await Assert.That(() => sut.ConvertFromProvider(tampered))
                .Throws<InvalidIdNumberException>();
        }

        [Test]
        public async Task ConvertFromProvider_PersonShapedDay_Throws()
        {
            var sut = new CoordinationIdConverter();
            // "189001019802" is a valid PersonalId (day=01, in 01-31 range),
            // which is NOT a valid CoordinationId (day must be in 61-91 range).
            const string personShaped = "189001019802";

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
