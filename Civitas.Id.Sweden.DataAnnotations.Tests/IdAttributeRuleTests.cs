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

    private static ValidationContext Named => new(new object()) { MemberName = MemberName };

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
        await Assert.That(sut.GetValidationResult(42, Named)?.ErrorMessage).IsEqualTo($"Type Int32 is not {kind.Article}.");
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

    private sealed class Dto
    {
        [ValidPersonalId] public string? Person { get; init; }
    }
}
