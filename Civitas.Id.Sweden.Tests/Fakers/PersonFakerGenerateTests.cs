using Civitas.Id.Sweden.Fakers.Internal;
using Civitas.Id.Sweden.Tests.Helpers;

namespace Civitas.Id.Sweden.Tests.Fakers;

/// <summary>What both person fakers must do. The derived classes name the faker.</summary>
public abstract class PersonFakerGenerateTests
{
    private const int Seed = 11;
    private const int Samples = 2000;
    private const int Defects = 8;
    private const int DateAndGenderCases = 8;

    private static readonly DateOnly LeapDay = new(2000, 2, 29);

    /// <summary>1900 is divisible by four and yet no leap year.</summary>
    private static readonly DateOnly CenturyWithoutLeapDay = new(1900, 2, 28);

    private static readonly IReadOnlyList<DateOnly> Dates =
        [LeapDay, CenturyWithoutLeapDay, new(1985, 1, 1), new(1985, 12, 31)];

    private static readonly IdGender[] Genders = Enum.GetValues<IdGender>();

    private protected abstract PersonGeneration Create(int seed);

    [Test]
    public async Task Generate_Valid_ParsesToOwnType()
    {
        var faker = Create(Seed);
        var samples = Enumerable.Range(0, Samples).Select(_ => faker.Generate(IdValidity.Valid, null, null)).ToList();

        await Assert.That(samples.Count(id => id is ValidGeneratedId)).IsEqualTo(Samples);
        await Assert.That(samples.IndexesWhere(id => faker.Parse(id.Text).LongFormat() != id.Text)).IsEmpty();
        await Assert.That(samples.IndexesWhere(id => !IndependentLuhn.IsValid(id.Text))).IsEmpty();
    }

    [Test]
    public async Task Generate_Valid_KeepsBirthDateAndGender()
    {
        var faker = Create(Seed);
        var cases = Dates.SelectMany(_ => Genders, (date, gender) => (date, gender)).ToList();

        await Assert.That(cases.Count).IsEqualTo(DateAndGenderCases);
        foreach (var (date, gender) in cases)
        {
            var id = faker.Parse(faker.Generate(IdValidity.Valid, date, gender).Text);
            await Assert.That(id.BirthDate).IsEqualTo(date);
            await Assert.That(id.IsMale).IsEqualTo(gender == IdGender.Male);
        }
    }

    [Test]
    public async Task Generate_Valid_KeepsBirthDateAlone()
    {
        var faker = Create(Seed);
        foreach (var date in Dates)
        {
            var samples = Enumerable.Range(0, Samples)
                .Select(_ => faker.Parse(faker.Generate(IdValidity.Valid, date, null).Text)).ToList();

            await Assert.That(samples.Count(id => id.BirthDate == date)).IsEqualTo(Samples);
            await Assert.That(samples.Select(id => id.IsMale).Distinct().Count()).IsEqualTo(Genders.Length);
        }
    }

    [Test]
    public async Task Generate_Valid_KeepsGenderAlone()
    {
        var faker = Create(Seed);
        foreach (var gender in Genders)
        {
            var samples = Enumerable.Range(0, Samples)
                .Select(_ => faker.Parse(faker.Generate(IdValidity.Valid, null, gender).Text)).ToList();

            await Assert.That(samples.Count(id => id.IsMale == (gender == IdGender.Male))).IsEqualTo(Samples);
            await Assert.That(samples.Select(id => id.BirthDate).Distinct().Count()).IsGreaterThan(1);
        }
    }

    [Test]
    public async Task Generate_SameSeed_SameText()
    {
        foreach (var validity in Enum.GetValues<IdValidity>())
            await Assert.That(Create(Seed).Generate(validity, null, null))
                .IsEqualTo(Create(Seed).Generate(validity, null, null));
    }

    [Test]
    public async Task Generate_Invalid_IsRejectedByEveryParserAndYieldsEverySupportedDefect()
    {
        var faker = Create(Seed);
        var samples = Enumerable.Range(0, Samples)
            .Select(_ => faker.Generate(IdValidity.Invalid, null, null)).OfType<InvalidGeneratedId>().ToList();

        await Assert.That(samples.Count).IsEqualTo(Samples);
        await Assert.That(samples.IndexesWhere(id => ParserProbe.AcceptedBy(id.Text).Count > 0)).IsEmpty();
        await Assert.That(samples.Select(id => id.Defect).Distinct()).IsEquivalentTo(faker.SupportedDefects);
        await Assert.That(faker.SupportedDefects.Count).IsEqualTo(Defects);
    }

    [Test]
    public async Task Generate_InvalidForLeapYear_NeverSelectsNonLeapFebruary29()
    {
        var faker = Create(Seed);
        var selected = Enumerable.Range(0, Samples)
            .Select(_ => faker.Generate(IdValidity.Invalid, LeapDay, null)).OfType<InvalidGeneratedId>()
            .Select(id => id.Defect).Distinct().ToList();

        await Assert.That(selected.Count).IsEqualTo(Defects - 1);
        await Assert.That(selected).DoesNotContain(IdDefect.NonLeapFebruary29);
    }

    /// <summary>The same seed gives the valid number that the defect was made from.</summary>
    [Test]
    public async Task Generate_Defect_ReturnsThatDefectAndBreaksOnlyThatRule()
    {
        IReadOnlyList<DateOnly?> dates = [null, CenturyWithoutLeapDay];
        IReadOnlyList<IdGender?> genders = [null, IdGender.Male, IdGender.Female];
        var samples = Create(Seed).SupportedDefects.SelectMany(defect => Enumerable.Range(0, Samples).Select(seed =>
        {
            var date = dates[seed % dates.Count];
            var gender = genders[seed % genders.Count];
            return (Defect: defect,
                Id: Create(seed).GenerateDefect(defect, date, gender),
                Valid: Create(seed).Generate(IdValidity.Valid, date, gender).Text);
        })).ToList();

        await Assert.That(samples.Count).IsEqualTo(Samples * Defects);
        await Assert.That(samples.IndexesWhere(s => s.Id.Defect != s.Defect)).IsEmpty();
        await Assert.That(samples.IndexesWhere(s => !DefectInspection.BreaksOnlyItsRule(s.Id, s.Valid))).IsEmpty();
        await Assert.That(samples.IndexesWhere(s => ParserProbe.AcceptedBy(s.Id.Text).Count > 0)).IsEmpty();
    }

    [Test]
    public async Task Generate_NonLeapFebruary29ForLeapYear_Throws()
    {
        await Assert.That(() => Create(Seed).GenerateDefect(IdDefect.NonLeapFebruary29, LeapDay, null))
            .ThrowsExactly<ArgumentException>();
    }

    [Test]
    public async Task Generate_UndefinedArgument_Throws()
    {
        var faker = Create(Seed);

        await Assert.That(() => faker.Generate((IdValidity)(-1), null, null)).Throws<ArgumentOutOfRangeException>();
        await Assert.That(() => faker.GenerateDefect((IdDefect)(-1), null, null)).Throws<ArgumentOutOfRangeException>();
        await Assert.That(() => faker.Generate(IdValidity.Valid, null, (IdGender)(-1)))
            .Throws<ArgumentOutOfRangeException>();
        await Assert.That(() => faker.GenerateDefect(IdDefect.TooLong, null, (IdGender)(-1)))
            .Throws<ArgumentOutOfRangeException>();
    }
}
