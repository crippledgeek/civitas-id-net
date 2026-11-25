using System.ComponentModel.DataAnnotations;
using Civitas.Id.Sweden.Core;

namespace Civitas.Id.Sweden.DataAnnotations.Tests;

public class ValidSwedishOfficialIdAttributeTests
{
    public class StringInput
    {
        public static IEnumerable<string> ValidPassesInputs()
        {
            yield return OfficialNumbers.Centenarian.Long; // personnummer
            yield return OfficialNumbers.CentenarianSamordningsnummer.Long; // samordningsnummer
            yield return LegalPersonNumbers.Aktiebolag.Digits; // organisationsnummer
        }

        [Test]
        [MethodDataSource(nameof(ValidPassesInputs))]
        public async Task Valid_Passes(string input)
        {
            var attr = new ValidSwedishOfficialIdAttribute();
            var result = attr.GetValidationResult(input, new ValidationContext(this));
            await Assert.That(result).IsNull();
        }

        [Test]
        public async Task Malformed_Fails()
        {
            var attr = new ValidSwedishOfficialIdAttribute();
            var result = attr.GetValidationResult("not-an-id", new ValidationContext(this));
            await Assert.That(result).IsNotNull();
        }

        [Test]
        public async Task Empty_Fails()
        {
            var attr = new ValidSwedishOfficialIdAttribute();
            var result = attr.GetValidationResult(string.Empty, new ValidationContext(this));
            await Assert.That(result).IsNotNull();
        }
    }

    public class TypedInput
    {
        [Test]
        public async Task PersonalId_Passes()
        {
            var attr = new ValidSwedishOfficialIdAttribute();
            var result = attr.GetValidationResult(PersonalId.Parse(OfficialNumbers.Centenarian.Long), new ValidationContext(this));
            await Assert.That(result).IsNull();
        }

        [Test]
        public async Task CoordinationId_Passes()
        {
            var attr = new ValidSwedishOfficialIdAttribute();
            var result = attr.GetValidationResult(CoordinationId.Parse(OfficialNumbers.CentenarianSamordningsnummer.Long), new ValidationContext(this));
            await Assert.That(result).IsNull();
        }

        [Test]
        public async Task OrganisationId_Passes()
        {
            var attr = new ValidSwedishOfficialIdAttribute();
            var result = attr.GetValidationResult(OrganisationId.Parse(LegalPersonNumbers.Aktiebolag.Digits), new ValidationContext(this));
            await Assert.That(result).IsNull();
        }
    }

    public class NullInput
    {
        [Test]
        public async Task Passes()
        {
            var attr = new ValidSwedishOfficialIdAttribute();
            var result = attr.GetValidationResult(null, new ValidationContext(this));
            await Assert.That(result).IsNull();
        }
    }

    public class WrongType
    {
        [Test]
        public async Task Fails()
        {
            var attr = new ValidSwedishOfficialIdAttribute();
            var result = attr.GetValidationResult(42, new ValidationContext(this));
            await Assert.That(result).IsNotNull();
        }
    }

    public class ErrorMessage
    {
        [Test]
        public async Task FormatErrorMessage_IncludesFieldName()
        {
            var attr = new ValidSwedishOfficialIdAttribute();
            var msg = attr.FormatErrorMessage("MyId");
            await Assert.That(msg).Contains("MyId");
        }
    }
}
