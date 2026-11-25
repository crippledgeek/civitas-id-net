using Civitas.Id.Sweden.Core;
using Civitas.Id.Sweden.Fakers;
using Civitas.Id.Sweden.Fakers.Internal;
using Civitas.Id.Sweden.Tests.Helpers;

namespace Civitas.Id.Sweden.Tests.Fakers;

/// <summary>
///     What each notation looks like and which entry points accept it, as in 1.0.0. UTF-8 rejects the notations in
///     <see cref="CharactersOnly" />, which string and span accept.
/// </summary>
public class IdNotationTests
{
    private const int Seed = 3;
    private const int Notations = 9;
    private const int PersonNotations = 6;
    private const int LegalPersonNotations = 8;
    private const int LongestAccepted = 100;
    private const int DigitsAfterPlus = 4;

    private const int PlusAge = 100;
    private const int Serial = 120;
    private const int GenderDigit = 4;

    private static readonly DateOnly Today = new(2026, 6, 15);

    /// <summary>The plus is the correct separator for this person, whatever the year of the test run.</summary>
    private static readonly DateOnly Centenarian = new(1900, 1, 1);

    private static readonly IReadOnlyList<(Type Type, ValidGeneratedId Id)> Persons =
    [
        (typeof(PersonalId), (ValidGeneratedId)new PersonalIdFaker(Seed).Generate(IdValidity.Valid)),
        (typeof(CoordinationId), (ValidGeneratedId)new CoordinationIdFaker(Seed).Generate(IdValidity.Valid))
    ];

    private static readonly ValidGeneratedId LegalPerson =
        (ValidGeneratedId)new OrganisationIdFaker(Seed).Generate(IdValidity.Valid);

    private static readonly IReadOnlyList<(Type Type, ValidGeneratedId Id)> All =
        [.. Persons, (typeof(OrganisationId), LegalPerson)];

    private static readonly IReadOnlyList<IdNotation> CharactersOnly =
        [IdNotation.NoBreakSpacePadded, IdNotation.OverLengthPadded];

    private static async Task AssertAcceptedAsync(Type type, ValidGeneratedId id, IdNotation notation)
    {
        var acceptedBy = ParserProbe.AcceptedBy(id.Render(notation));
        await Assert.That(acceptedBy.Contains($"{type.Name}/utf8")).IsEqualTo(!CharactersOnly.Contains(notation));
        await Assert.That(acceptedBy).Contains($"{type.Name}/string");
        await Assert.That(acceptedBy).Contains($"{type.Name}/span");
    }

    private static ValidGeneratedId BornIn(int year)
    {
        return (ValidGeneratedId)new PersonalIdFaker(Seed).Generate(IdValidity.Valid, new DateOnly(year, 12, 31));
    }

    private static async Task AssertPrefixedAsync(string text, ValidGeneratedId id, string prefix)
    {
        await UnprintedAssert.AreEqual(text[..prefix.Length], prefix).ConfigureAwait(false);
        await UnprintedAssert.AreEqual(text[prefix.Length..], id.Text).ConfigureAwait(false);
    }

    private static async Task AssertWrappedAsync(string text, ValidGeneratedId id, char padding)
    {
        await UnprintedAssert.AreEqual(text[0], padding).ConfigureAwait(false);
        await UnprintedAssert.AreEqual(text[^1], padding).ConfigureAwait(false);
        await UnprintedAssert.AreEqual(text[1..^1], id.Text).ConfigureAwait(false);
    }

    private static async Task AssertPlusAsync(string text, ValidGeneratedId id)
    {
        await UnprintedAssert.HasSeparator(text, '+').ConfigureAwait(false);
        await UnprintedAssert.AreEqual(text[..^(DigitsAfterPlus + 1)], id.Text[..^DigitsAfterPlus])
            .ConfigureAwait(false);
        await UnprintedAssert.AreEqual(text[^DigitsAfterPlus..], id.Text[^DigitsAfterPlus..]).ConfigureAwait(false);
    }

    [Test]
    public async Task Render_SupportsExactlyThePinnedCombinations()
    {
        IReadOnlyList<IdNotation> legalPersonOnly =
            [IdNotation.SixteenPrefix, IdNotation.PlusOnLegalPerson, IdNotation.ZeroSerialLegalPerson];
        var notations = Enum.GetValues<IdNotation>();
        var forPersons = notations.Except(legalPersonOnly).ToList();
        var forLegalPersons = notations.Where(n => n != IdNotation.ContradictoryPlus).ToList();

        await Assert.That(notations.Length).IsEqualTo(Notations);
        await Assert.That(forPersons.Count).IsEqualTo(PersonNotations);
        await Assert.That(forLegalPersons.Count).IsEqualTo(LegalPersonNotations);

        foreach (var (type, id) in Persons)
        {
            foreach (var notation in forPersons)
                await AssertAcceptedAsync(type, id, notation).ConfigureAwait(false);
            foreach (var notation in legalPersonOnly)
                await Assert.That(() => id.Render(notation)).Throws<ArgumentOutOfRangeException>();
        }

        foreach (var notation in forLegalPersons)
            await AssertAcceptedAsync(typeof(OrganisationId), LegalPerson, notation).ConfigureAwait(false);
        await Assert.That(() => LegalPerson.Render(IdNotation.ContradictoryPlus)).Throws<ArgumentOutOfRangeException>();
    }

    /// <summary>The same parts are accepted in every one of these notations but for their year.</summary>
    [Test]
    public async Task Render_BirthYearBelowMinimum_IsRejectedInEveryNotation()
    {
        IReadOnlyList<IdNotation> notations =
        [
            IdNotation.Canonical, IdNotation.AsciiPadded, IdNotation.NoBreakSpacePadded, IdNotation.SePrefix,
            IdNotation.OverLengthPadded
        ];
        IReadOnlyList<IdDraft> drafts =
        [
            Generation.DraftPersonnummer(Centenarian, Serial, GenderDigit),
            Generation.DraftSamordningsnummer(Centenarian, Serial, GenderDigit)
        ];

        foreach (var notation in notations)
            foreach (var draft in drafts)
            {
                var tooEarly = draft with { Century = 0 };
                await Assert.That(ParserProbe.AcceptedBy(NotationCatalog.Render(notation, draft, Today))).IsNotEmpty();
                await Assert.That(ParserProbe.AcceptedBy(NotationCatalog.Render(notation, tooEarly, Today))).IsEmpty();
            }
    }

    [Test]
    public async Task Render_UndefinedNotation_Throws()
    {
        foreach (var (_, id) in All)
        {
            await Assert.That(() => id.Render((IdNotation)(-1))).Throws<ArgumentOutOfRangeException>();
            await Assert.That(() => id.Render((IdNotation)Notations)).Throws<ArgumentOutOfRangeException>();
        }
    }

    [Test]
    public async Task Render_ContradictoryPlusForACentenarian_Throws()
    {
        var id = (ValidGeneratedId)new PersonalIdFaker(Seed).Generate(IdValidity.Valid, Centenarian);

        await Assert.That(() => id.Render(IdNotation.ContradictoryPlus)).Throws<ArgumentOutOfRangeException>();
    }

    [Test]
    public async Task Render_ContradictoryPlusBeforeTheHundredthYear_IsWritten()
    {
        var id = BornIn(Today.Year - PlusAge + 1);

        await AssertPlusAsync(id.Render(IdNotation.ContradictoryPlus, Today), id).ConfigureAwait(false);
    }

    [Test]
    public async Task Render_ContradictoryPlusInTheHundredthYear_Throws()
    {
        var id = BornIn(Today.Year - PlusAge);

        await Assert.That(() => { _ = id.Render(IdNotation.ContradictoryPlus, Today); })
            .Throws<ArgumentOutOfRangeException>();
    }

    [Test]
    public async Task Render_NotationsOfEveryKind_HaveTheirShape()
    {
        foreach (var (_, id) in All)
        {
            await UnprintedAssert.AreEqual(id.Render(IdNotation.Canonical), id.Text).ConfigureAwait(false);
            await AssertWrappedAsync(id.Render(IdNotation.AsciiPadded), id, ' ').ConfigureAwait(false);
            await AssertWrappedAsync(id.Render(IdNotation.NoBreakSpacePadded), id, '\u00A0').ConfigureAwait(false);
            await AssertPrefixedAsync(id.Render(IdNotation.SePrefix), id, "SE").ConfigureAwait(false);
            await Assert.That(id.Render(IdNotation.OverLengthPadded).Length).IsEqualTo(LongestAccepted + 1);
            await UnprintedAssert.AreEqual(id.Render(IdNotation.OverLengthPadded).TrimStart(' '), id.Text)
                .ConfigureAwait(false);
        }
    }

    [Test]
    public async Task Render_NotationsOfOneKind_HaveTheirShape()
    {
        foreach (var (_, id) in Persons)
            await AssertPlusAsync(id.Render(IdNotation.ContradictoryPlus), id).ConfigureAwait(false);
        await AssertPlusAsync(LegalPerson.Render(IdNotation.PlusOnLegalPerson), LegalPerson).ConfigureAwait(false);
        await AssertPrefixedAsync(LegalPerson.Render(IdNotation.SixteenPrefix), LegalPerson, "16").ConfigureAwait(false);

        var zeroSerial = LegalPerson.Render(IdNotation.ZeroSerialLegalPerson);
        await Assert.That(zeroSerial.Length).IsEqualTo(LegalPerson.Text.Length);
        await UnprintedAssert.AreEqual(zeroSerial[..^DigitsAfterPlus], LegalPerson.Text[..^DigitsAfterPlus])
            .ConfigureAwait(false);
        await UnprintedAssert.AreEqual(zeroSerial[^DigitsAfterPlus..^1], "000").ConfigureAwait(false);
        await Assert.That(IndependentLuhn.IsValid(zeroSerial)).IsTrue();
    }
}
