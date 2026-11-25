using Civitas.Id.Sweden.Core;
using Civitas.Id.Sweden.Fakers;
using Civitas.Id.Sweden.Fakers.Internal;
using Civitas.Id.Sweden.Format;
using Civitas.Id.Sweden.Tests.Helpers;

namespace Civitas.Id.Sweden.Tests.Fakers;

public class OrganisationIdFakerGenerateTests
{
    private const int Seed = 11;
    private const int Samples = 2000;
    private const int Defects = 5;
    private const int DateDefects = 4;

    [Test]
    public async Task Generate_Valid_ParsesToALegalPerson()
    {
        var faker = new OrganisationIdFaker(Seed);
        var samples = Enumerable.Range(0, Samples).Select(_ => faker.Generate(IdValidity.Valid)).ToList();

        await Assert.That(samples.Count(id => id is ValidGeneratedId)).IsEqualTo(Samples);
        await Assert.That(samples.IndexesWhere(id => !IndependentLuhn.IsValid(id.Text))).IsEmpty();
        await Assert.That(samples.IndexesWhere(id =>
            OrganisationId.Parse(id.Text).NumberType != OrganisationNumberType.LegalPerson)).IsEmpty();
    }

    /// <summary>The generated number is the one <c>GenerateLegalPerson</c> gives for the same seed.</summary>
    [Test]
    public async Task Generate_Valid_UsesTheDrawsOfGenerateLegalPerson()
    {
        var different = Enumerable.Range(0, Samples).Where(seed =>
            new OrganisationIdFaker(seed).Generate(IdValidity.Valid).Text !=
            new OrganisationIdFaker(seed).GenerateLegalPerson().LongFormat());

        await Assert.That(different).IsEmpty();
    }

    [Test]
    public async Task Generate_SameSeed_SameText()
    {
        foreach (var validity in Enum.GetValues<IdValidity>())
            await Assert.That(new OrganisationIdFaker(Seed).Generate(validity))
                .IsEqualTo(new OrganisationIdFaker(Seed).Generate(validity));
    }

    [Test]
    public async Task Generate_Invalid_IsRejectedByEveryParserAndYieldsEverySupportedDefect()
    {
        var faker = new OrganisationIdFaker(Seed);
        var samples = Enumerable.Range(0, Samples)
            .Select(_ => faker.Generate(IdValidity.Invalid)).OfType<InvalidGeneratedId>().ToList();

        await Assert.That(samples.Count).IsEqualTo(Samples);
        await Assert.That(samples.IndexesWhere(id => ParserProbe.AcceptedBy(id.Text).Count > 0)).IsEmpty();
        await Assert.That(samples.Select(id => id.Defect).Distinct()).IsEquivalentTo(faker.SupportedDefects);
        await Assert.That(faker.SupportedDefects.Count).IsEqualTo(Defects);
    }

    /// <summary>The same seed gives the valid number that the defect was made from.</summary>
    [Test]
    public async Task Generate_Defect_ReturnsThatDefectAndBreaksOnlyThatRule()
    {
        var samples = new OrganisationIdFaker(Seed).SupportedDefects
            .SelectMany(_ => Enumerable.Range(0, Samples), (defect, seed) => (
                Defect: defect,
                Id: new OrganisationIdFaker(seed).Generate(defect),
                Valid: new OrganisationIdFaker(seed).Generate(IdValidity.Valid).Text))
            .ToList();

        await Assert.That(samples.Count).IsEqualTo(Samples * Defects);
        await Assert.That(samples.IndexesWhere(s => s.Id.Defect != s.Defect)).IsEmpty();
        await Assert.That(samples.IndexesWhere(s => !DefectInspection.BreaksOnlyItsRule(s.Id, s.Valid))).IsEmpty();
        await Assert.That(samples.IndexesWhere(s => ParserProbe.AcceptedBy(s.Id.Text).Count > 0)).IsEmpty();
    }

    [Test]
    public async Task Generate_DateDefect_Throws()
    {
        var faker = new OrganisationIdFaker(Seed);
        var unsupported = Enum.GetValues<IdDefect>().Except(faker.SupportedDefects).ToList();

        await Assert.That(unsupported.Count).IsEqualTo(DateDefects);
        foreach (var defect in unsupported)
            await Assert.That(() => faker.Generate(defect)).Throws<ArgumentOutOfRangeException>();
    }

    /// <summary>As in 1.0.0, a form that does not fit its two digits is an invalid argument.</summary>
    [Test]
    public async Task Generate_FormWithThreeDigits_Throws()
    {
        await Assert.That(() => new OrganisationIdFaker(Seed).Generate((OrganisationForm)100))
            .ThrowsExactly<ArgumentException>();
    }

    [Test]
    public async Task Generate_UndefinedArgument_Throws()
    {
        var faker = new OrganisationIdFaker(Seed);

        await Assert.That(() => faker.Generate((IdValidity)(-1))).Throws<ArgumentOutOfRangeException>();
        await Assert.That(() => faker.Generate((IdDefect)(-1))).Throws<ArgumentOutOfRangeException>();
    }
}
