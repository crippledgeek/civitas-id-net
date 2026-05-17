using System.ComponentModel.DataAnnotations;
using Civitas.Id.Sweden.Core;

namespace Civitas.Id.Sweden.DataAnnotations.Tests;

public class ValidPersonalIdAttributeTests
{
    public class StringInput
    {
        [Test]
        public async Task ValidPersonalId_Passes()
        {
            var attr = new ValidPersonalIdAttribute();
            var result = attr.GetValidationResult("189001019802", new ValidationContext(this));
            await Assert.That(result).IsNull();
        }

        [Test]
        public async Task CoordinationIdString_Fails()
        {
            var attr = new ValidPersonalIdAttribute();
            var result = attr.GetValidationResult("198112752384", new ValidationContext(this));
            await Assert.That(result).IsNotNull();
        }

        [Test]
        public async Task OrganisationIdString_Fails()
        {
            var attr = new ValidPersonalIdAttribute();
            var result = attr.GetValidationResult("5560160680", new ValidationContext(this));
            await Assert.That(result).IsNotNull();
        }

        [Test]
        public async Task Malformed_Fails()
        {
            var attr = new ValidPersonalIdAttribute();
            var result = attr.GetValidationResult("not-an-id", new ValidationContext(this));
            await Assert.That(result).IsNotNull();
        }
    }

    public class TypedInput
    {
        [Test]
        public async Task PersonalId_Passes()
        {
            var attr = new ValidPersonalIdAttribute();
            var result = attr.GetValidationResult(PersonalId.Parse("189001019802"), new ValidationContext(this));
            await Assert.That(result).IsNull();
        }

        [Test]
        public async Task CoordinationIdInstance_Fails()
        {
            var attr = new ValidPersonalIdAttribute();
            var result = attr.GetValidationResult(CoordinationId.Parse("198112752384"), new ValidationContext(this));
            await Assert.That(result).IsNotNull();
        }

        [Test]
        public async Task OrganisationIdInstance_Fails()
        {
            var attr = new ValidPersonalIdAttribute();
            var result = attr.GetValidationResult(OrganisationId.Parse("5560160680"), new ValidationContext(this));
            await Assert.That(result).IsNotNull();
        }
    }

    public class NullInput
    {
        [Test]
        public async Task Passes()
        {
            var attr = new ValidPersonalIdAttribute();
            var result = attr.GetValidationResult(null, new ValidationContext(this));
            await Assert.That(result).IsNull();
        }
    }

    public class WrongType
    {
        [Test]
        public async Task Fails()
        {
            var attr = new ValidPersonalIdAttribute();
            var result = attr.GetValidationResult(42, new ValidationContext(this));
            await Assert.That(result).IsNotNull();
        }
    }

    public class ErrorMessage
    {
        [Test]
        public async Task FormatErrorMessage_IncludesFieldName()
        {
            var attr = new ValidPersonalIdAttribute();
            var msg = attr.FormatErrorMessage("PersonId");
            await Assert.That(msg).Contains("PersonId");
        }
    }
}
