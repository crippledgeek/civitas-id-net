using Civitas.Id.Sweden.Fakers.Internal;
using Civitas.Id.Sweden.Tests.Helpers;

namespace Civitas.Id.Sweden.Tests.Fakers;

public class IdDraftTests
{
    private const int LegalPersons = 3;
    private const int TenDigits = 10;
    private const int TooWideForTwoDigits = 100;
    private const int TooWideForThreeDigits = 1000;

    public static IEnumerable<string> OfficialRows()
    {
        return OfficialNumbers.All.Select(number => number.Long);
    }

    [Test]
    [MethodDataSource(nameof(OfficialRows))]
    public async Task Render_ReproducesOfficialRows(string row)
    {
        await UnprintedAssert.AreEqual(OfficialDrafts.Of(row).Render(), row).ConfigureAwait(false);
    }

    [Test]
    public async Task ToString_HoldsNoNumber()
    {
        await Assert.That(OfficialDrafts.Personnummer[0].ToString()).IsEqualTo(nameof(IdDraft));
    }

    [Test]
    public async Task Render_WithoutCentury_IsTenValidDigits()
    {
        await Assert.That(OfficialDrafts.LegalPersons.Count).IsEqualTo(LegalPersons);
        foreach (var text in OfficialDrafts.LegalPersons.Select(draft => draft.Render()))
        {
            await Assert.That(text.Length).IsEqualTo(TenDigits);
            await Assert.That(IndependentLuhn.IsValid(text)).IsTrue();
        }
    }

    [Test]
    public async Task Render_PartThatDoesNotFit_Throws()
    {
        var draft = OfficialDrafts.Personnummer[0];

        await Assert.That(() => (draft with { Century = TooWideForTwoDigits }).Render())
            .ThrowsExactly<ArgumentException>();
        await Assert.That(() => (draft with { Year = TooWideForTwoDigits }).Render())
            .ThrowsExactly<ArgumentException>();
        await Assert.That(() => (draft with { Month = -1 }).Render()).ThrowsExactly<ArgumentException>();
        await Assert.That(() => (draft with { Day = TooWideForTwoDigits }).Render())
            .ThrowsExactly<ArgumentException>();
        await Assert.That(() => (draft with { Serial = TooWideForThreeDigits }).Render())
            .ThrowsExactly<ArgumentException>();
    }

    [Test]
    public async Task FullYear_OfAPerson_IsTheYearOfBirth()
    {
        var number = OfficialNumbers.Centenarian;

        await Assert.That(OfficialDrafts.Of(number.Long).FullYear).IsEqualTo(number.BirthDate.Year);
    }

    [Test]
    [Arguments(0)]
    [Arguments(1799)]
    [Arguments(9999)]
    public async Task BornIn_ReplacesTheYearAlone(int year)
    {
        var draft = OfficialDrafts.Personnummer[0];

        await Assert.That(draft.BornIn(year).FullYear).IsEqualTo(year);
        await Assert.That(draft.BornIn(year).BornIn(draft.FullYear)).IsEqualTo(draft);
    }

    [Test]
    public async Task FullYear_OfALegalPerson_Throws()
    {
        await Assert.That(() => OfficialDrafts.LegalPersons[0].FullYear).Throws<InvalidOperationException>();
    }

    [Test]
    public async Task IndependentLuhn_NineDigitsOrASeparator_IsRejected()
    {
        await Assert.That(OfficialNumbers.All.IndexesWhere(number => IndependentLuhn.IsValid(number.Body))).IsEmpty();
        await Assert.That(OfficialNumbers.All.IndexesWhere(number => IndependentLuhn.IsValid(number.LongWithHyphen)))
            .IsEmpty();
    }

    [Test]
    public async Task IndependentLuhn_WrongCheckDigit_IsRejected()
    {
        foreach (var number in OfficialNumbers.All)
            await Assert.That(IndependentLuhn.IsValid(number.WithWrongCheckDigit)).IsFalse();
        await Assert.That(IndependentLuhn.IsValid(LegalPersonNumbers.Aktiebolag.WithWrongCheckDigit)).IsFalse();
    }
}
