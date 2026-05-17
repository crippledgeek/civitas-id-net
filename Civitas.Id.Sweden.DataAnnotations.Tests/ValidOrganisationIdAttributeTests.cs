using System.ComponentModel.DataAnnotations;
using Civitas.Id.Sweden.Core;

namespace Civitas.Id.Sweden.DataAnnotations.Tests;

public class ValidOrganisationIdAttributeTests
{
    public class StringInput
    {
        [Test]
        public async Task ValidOrganisationId_Passes()
        {
            var attr = new ValidOrganisationIdAttribute();
            var result = attr.GetValidationResult("5560160680", new ValidationContext(this));
            await Assert.That(result).IsNull();
        }

        // Note: A 12-digit personnummer like "189001019802" intentionally parses as
        // OrganisationId (Enskild firma — sole proprietor) per Lag (1974:174). So
        // cross-type string rejection is only meaningful at the instance level here.

        [Test]
        public async Task Malformed_Fails()
        {
            var attr = new ValidOrganisationIdAttribute();
            var result = attr.GetValidationResult("not-an-id", new ValidationContext(this));
            await Assert.That(result).IsNotNull();
        }

        [Test]
        public async Task Empty_Fails()
        {
            var attr = new ValidOrganisationIdAttribute();
            var result = attr.GetValidationResult(string.Empty, new ValidationContext(this));
            await Assert.That(result).IsNotNull();
        }
    }

    public class TypedInput
    {
        [Test]
        public async Task OrganisationId_Passes()
        {
            var attr = new ValidOrganisationIdAttribute();
            var result = attr.GetValidationResult(OrganisationId.Parse("5560160680"), new ValidationContext(this));
            await Assert.That(result).IsNull();
        }

        [Test]
        public async Task PersonalIdInstance_Fails()
        {
            var attr = new ValidOrganisationIdAttribute();
            var result = attr.GetValidationResult(PersonalId.Parse("189001019802"), new ValidationContext(this));
            await Assert.That(result).IsNotNull();
        }

        [Test]
        public async Task CoordinationIdInstance_Fails()
        {
            var attr = new ValidOrganisationIdAttribute();
            var result = attr.GetValidationResult(CoordinationId.Parse("198112752384"), new ValidationContext(this));
            await Assert.That(result).IsNotNull();
        }
    }

    public class NullInput
    {
        [Test]
        public async Task Passes()
        {
            var attr = new ValidOrganisationIdAttribute();
            var result = attr.GetValidationResult(null, new ValidationContext(this));
            await Assert.That(result).IsNull();
        }
    }

    public class WrongType
    {
        [Test]
        public async Task Fails()
        {
            var attr = new ValidOrganisationIdAttribute();
            var result = attr.GetValidationResult(42, new ValidationContext(this));
            await Assert.That(result).IsNotNull();
        }
    }

    public class ErrorMessage
    {
        [Test]
        public async Task FormatErrorMessage_IncludesFieldName()
        {
            var attr = new ValidOrganisationIdAttribute();
            var msg = attr.FormatErrorMessage("OrgId");
            await Assert.That(msg).Contains("OrgId");
        }
    }
}
