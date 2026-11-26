using System.ComponentModel.DataAnnotations;

namespace Civitas.Id.Sweden.DataAnnotations.Tests;

/// <summary>
///     The rule the four attributes share, read through <see cref="ValidationAttribute.IsValid(object)" />, which
///     passes no context, and through <see cref="ValidationAttribute.GetValidationResult" />, which passes one. A number
///     is never printed.
/// </summary>
public class IdAttributeRuleTests
{
    private const string Malformed = "not-an-id";
    private const string MemberName = "Field";
    private const string Custom = "Bad {0}.";
    private const string CustomForField = "Bad Field.";
    private const string ResourceForField = "Resource Field.";

    private static ValidationContext Named => new(new object()) { MemberName = MemberName };

    private static class MessageResource
    {
        internal static string Message => "Resource {0}.";
    }

    private static async Task AssertWithoutContextAsync(AttributeKind kind)
    {
        var sut = kind.Create();
        await Assert.That(sut.IsValid(kind.Accepted)).IsTrue();
        await Assert.That(sut.IsValid(kind.AcceptedInstance)).IsTrue();
        await Assert.That(sut.IsValid(null)).IsTrue();
        await Assert.That(sut.IsValid(Malformed)).IsFalse();
        await Assert.That(sut.IsValid(42)).IsFalse();
    }

    private static async Task AssertWithContextAsync(AttributeKind kind)
    {
        var sut = kind.Create();
        var unnamed = new ValidationContext(new object());

        await Assert.That(sut.GetValidationResult(kind.Accepted, Named) is null).IsTrue();
        await Assert.That(sut.GetValidationResult(kind.AcceptedInstance, Named) is null).IsTrue();
        await Assert.That(sut.GetValidationResult(null, Named) is null).IsTrue();
        await Assert.That(sut.GetValidationResult(Malformed, Named)?.ErrorMessage).IsEqualTo(kind.DefaultText(MemberName));
        await Assert.That(sut.GetValidationResult(Malformed, unnamed)?.ErrorMessage).IsEqualTo(kind.DefaultText("value"));
        await Assert.That(sut.GetValidationResult(42, Named)?.ErrorMessage).IsEqualTo(kind.DefaultText(MemberName));
    }

    private static async Task AssertDefaultAsync(AttributeKind kind)
    {
        var sut = kind.Create();
        await Assert.That(sut.FormatErrorMessage(MemberName)).IsEqualTo(kind.DefaultText(MemberName));
        await Assert.That(sut.GetValidationResult(Malformed, Named)?.ErrorMessage).IsEqualTo(kind.DefaultText(MemberName));
    }

    private static async Task AssertCustomAsync(AttributeKind kind)
    {
        var sut = kind.Create();
        sut.ErrorMessage = Custom;
        await Assert.That(sut.FormatErrorMessage(MemberName)).IsEqualTo(CustomForField);
        await Assert.That(sut.GetValidationResult(Malformed, Named)?.ErrorMessage).IsEqualTo(CustomForField);
        await Assert.That(sut.GetValidationResult(42, Named)?.ErrorMessage).IsEqualTo(CustomForField);
        await Assert.That(sut.IsValid(Malformed)).IsFalse();
    }

    private static async Task AssertResourceAsync(AttributeKind kind)
    {
        var sut = kind.Create();
        sut.ErrorMessageResourceType = typeof(MessageResource);
        sut.ErrorMessageResourceName = nameof(MessageResource.Message);
        await Assert.That(sut.GetValidationResult(Malformed, Named)?.ErrorMessage).IsEqualTo(ResourceForField);
        await Assert.That(sut.GetValidationResult(42, Named)?.ErrorMessage).IsEqualTo(ResourceForField);
    }

    private static async Task AssertMisconfiguredAsync(AttributeKind kind)
    {
        var both = kind.Create();
        both.ErrorMessage = Custom;
        both.ErrorMessageResourceType = typeof(MessageResource);
        both.ErrorMessageResourceName = nameof(MessageResource.Message);
        await Assert.That(() => both.GetValidationResult(Malformed, Named)).Throws<InvalidOperationException>();

        var nameOnly = kind.Create();
        nameOnly.ErrorMessageResourceName = nameof(MessageResource.Message);
        await Assert.That(() => nameOnly.GetValidationResult(Malformed, Named)).Throws<InvalidOperationException>();
    }

    private static async Task AssertVerdictIgnoresMisconfigurationAsync(AttributeKind kind)
    {
        var sut = kind.Create();
        sut.ErrorMessage = Custom;
        sut.ErrorMessageResourceType = typeof(MessageResource);
        sut.ErrorMessageResourceName = nameof(MessageResource.Message);
        await Assert.That(sut.IsValid(kind.Accepted)).IsTrue();
        await Assert.That(sut.IsValid(Malformed)).IsFalse();
        await Assert.That(sut.IsValid(42)).IsFalse();
    }

    public class WithoutContext
    {
        [Test]
        public async Task PersonalId_ReturnsTheVerdict()
        {
            await AssertWithoutContextAsync(AttributeKind.Personal).ConfigureAwait(false);
        }

        [Test]
        public async Task CoordinationId_ReturnsTheVerdict()
        {
            await AssertWithoutContextAsync(AttributeKind.Coordination).ConfigureAwait(false);
        }

        [Test]
        public async Task OrganisationId_ReturnsTheVerdict()
        {
            await AssertWithoutContextAsync(AttributeKind.Organisation).ConfigureAwait(false);
        }

        [Test]
        public async Task SwedishOfficialId_ReturnsTheVerdict()
        {
            await AssertWithoutContextAsync(AttributeKind.Official).ConfigureAwait(false);
        }

        [Test]
        public async Task PersonalId_RejectsTheOtherPersonKind()
        {
            await Assert.That(AttributeKind.Personal.Create().IsValid(OfficialNumbers.CentenarianSamordningsnummer.Long)).IsFalse();
        }

        [Test]
        public async Task PersonalId_RejectsGeneratedDefects()
        {
            var sut = AttributeKind.Personal.Create();
            await Assert.That(sut.IsValid(GeneratedNumbers.ValidPersonnummer)).IsTrue();
            foreach (var text in GeneratedNumbers.InvalidPersonnummer())
                await Assert.That(sut.IsValid(text)).IsFalse();
        }
    }

    public class WithContext
    {
        [Test]
        public async Task PersonalId_KeepsItsMessages()
        {
            await AssertWithContextAsync(AttributeKind.Personal).ConfigureAwait(false);
        }

        [Test]
        public async Task CoordinationId_KeepsItsMessages()
        {
            await AssertWithContextAsync(AttributeKind.Coordination).ConfigureAwait(false);
        }

        [Test]
        public async Task OrganisationId_KeepsItsMessages()
        {
            await AssertWithContextAsync(AttributeKind.Organisation).ConfigureAwait(false);
        }

        [Test]
        public async Task SwedishOfficialId_KeepsItsMessages()
        {
            await AssertWithContextAsync(AttributeKind.Official).ConfigureAwait(false);
        }

        [Test]
        public async Task Validator_NamesTheFieldInTheMessage()
        {
            var dto = new Dto { Person = Malformed };
            var results = new List<ValidationResult>();
            var ok = Validator.TryValidateObject(dto, new ValidationContext(dto), results, true);

            await Assert.That(ok).IsFalse();
            await Assert.That(results.Single().ErrorMessage).IsEqualTo(AttributeKind.Personal.DefaultText(nameof(Dto.Person)));
        }
    }

    public class DefaultMessage
    {
        [Test]
        public async Task PersonalId_KeepsItsText()
        {
            await AssertDefaultAsync(AttributeKind.Personal).ConfigureAwait(false);
        }

        [Test]
        public async Task CoordinationId_KeepsItsText()
        {
            await AssertDefaultAsync(AttributeKind.Coordination).ConfigureAwait(false);
        }

        [Test]
        public async Task OrganisationId_KeepsItsText()
        {
            await AssertDefaultAsync(AttributeKind.Organisation).ConfigureAwait(false);
        }

        [Test]
        public async Task SwedishOfficialId_KeepsItsText()
        {
            await AssertDefaultAsync(AttributeKind.Official).ConfigureAwait(false);
        }
    }

    public class CustomMessage
    {
        [Test]
        public async Task PersonalId_IsUsed()
        {
            await AssertCustomAsync(AttributeKind.Personal).ConfigureAwait(false);
        }

        [Test]
        public async Task CoordinationId_IsUsed()
        {
            await AssertCustomAsync(AttributeKind.Coordination).ConfigureAwait(false);
        }

        [Test]
        public async Task OrganisationId_IsUsed()
        {
            await AssertCustomAsync(AttributeKind.Organisation).ConfigureAwait(false);
        }

        [Test]
        public async Task SwedishOfficialId_IsUsed()
        {
            await AssertCustomAsync(AttributeKind.Official).ConfigureAwait(false);
        }
    }

    public class ResourceMessage
    {
        [Test]
        public async Task PersonalId_IsUsed()
        {
            await AssertResourceAsync(AttributeKind.Personal).ConfigureAwait(false);
        }

        [Test]
        public async Task CoordinationId_IsUsed()
        {
            await AssertResourceAsync(AttributeKind.Coordination).ConfigureAwait(false);
        }

        [Test]
        public async Task OrganisationId_IsUsed()
        {
            await AssertResourceAsync(AttributeKind.Organisation).ConfigureAwait(false);
        }

        [Test]
        public async Task SwedishOfficialId_IsUsed()
        {
            await AssertResourceAsync(AttributeKind.Official).ConfigureAwait(false);
        }
    }

    public class Misconfigured
    {
        [Test]
        public async Task PersonalId_Throws()
        {
            await AssertMisconfiguredAsync(AttributeKind.Personal).ConfigureAwait(false);
        }

        [Test]
        public async Task CoordinationId_Throws()
        {
            await AssertMisconfiguredAsync(AttributeKind.Coordination).ConfigureAwait(false);
        }

        [Test]
        public async Task OrganisationId_Throws()
        {
            await AssertMisconfiguredAsync(AttributeKind.Organisation).ConfigureAwait(false);
        }

        [Test]
        public async Task SwedishOfficialId_Throws()
        {
            await AssertMisconfiguredAsync(AttributeKind.Official).ConfigureAwait(false);
        }

        [Test]
        public async Task PersonalId_LeavesTheVerdict()
        {
            await AssertVerdictIgnoresMisconfigurationAsync(AttributeKind.Personal).ConfigureAwait(false);
        }

        [Test]
        public async Task CoordinationId_LeavesTheVerdict()
        {
            await AssertVerdictIgnoresMisconfigurationAsync(AttributeKind.Coordination).ConfigureAwait(false);
        }

        [Test]
        public async Task OrganisationId_LeavesTheVerdict()
        {
            await AssertVerdictIgnoresMisconfigurationAsync(AttributeKind.Organisation).ConfigureAwait(false);
        }

        [Test]
        public async Task SwedishOfficialId_LeavesTheVerdict()
        {
            await AssertVerdictIgnoresMisconfigurationAsync(AttributeKind.Official).ConfigureAwait(false);
        }
    }

    private sealed class Dto
    {
        [ValidPersonalId] public string? Person { get; init; }
    }
}
