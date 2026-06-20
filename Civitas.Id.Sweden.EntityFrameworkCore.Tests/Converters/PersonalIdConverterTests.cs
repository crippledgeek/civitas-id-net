using Civitas.Id.Sweden.Core;
using Civitas.Id.Sweden.Errors;

namespace Civitas.Id.Sweden.EntityFrameworkCore.Tests.Converters;

public class PersonalIdConverterTests
{
    public class Construction
    {
        [Test]
        public async Task Constructor_NoArguments_BuildsValidConverter()
        {
            var sut = new PersonalIdConverter();

            await Assert.That(sut).IsNotNull();
            await Assert.That(sut.ProviderClrType).IsEqualTo(typeof(string));
            await Assert.That(sut.ModelClrType).IsEqualTo(typeof(PersonalId));
        }
    }

    public class Encode
    {
        [Test]
        public async Task ConvertToProvider_ValidId_ReturnsTwelveDigitString()
        {
            var sut = new PersonalIdConverter();
            var id = PersonalId.Parse("189001019802");

            var encoded = (string?)sut.ConvertToProvider(id);

            await Assert.That(encoded).IsEqualTo("189001019802");
            await Assert.That(encoded!.Length).IsEqualTo(12);
        }

        [Test]
        public async Task ConvertToProvider_TwoInstancesSameValue_ProduceSameString()
        {
            var sut = new PersonalIdConverter();
            var a = PersonalId.Parse("189001019802");
            var b = PersonalId.Parse("189001019802");

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
            var sut = new PersonalIdConverter();
            var original = PersonalId.Parse("189001019802");

            var encoded = (string?)sut.ConvertToProvider(original);
            var decoded = (PersonalId?)sut.ConvertFromProvider(encoded);

            await Assert.That(decoded).IsEqualTo(original);
        }

        [Test]
        public async Task ConvertFromProvider_LuhnTamperedString_Throws()
        {
            var sut = new PersonalIdConverter();
            // Original "189001019802" — flip the last digit:
            const string tampered = "189001019801";

            await Assert.That(() => sut.ConvertFromProvider(tampered))
                .Throws<InvalidIdNumberException>();
        }

        [Test]
        public async Task ConvertFromProvider_EmptyString_Throws()
        {
            var sut = new PersonalIdConverter();

            await Assert.That(() => sut.ConvertFromProvider(string.Empty))
                .Throws<InvalidIdNumberException>();
        }
    }

    public class MappingHints
    {
        [Test]
        public async Task MappingHints_DeclareSizeTwelveFixedLengthAscii()
        {
            var sut = new PersonalIdConverter();

            await Assert.That(sut.MappingHints).IsNotNull();
            await Assert.That(sut.MappingHints!.Size).IsEqualTo(12);
            await Assert.That(sut.MappingHints.IsUnicode).IsFalse();
        }
    }
}
