using System.Text.RegularExpressions;

namespace Civitas.Id.Sweden.Tests.Helpers;

public partial class OfficialNumbersTests
{
    [GeneratedRegex(@"\A[0-9]{8}-[0-9]{4}\z")]
    private static partial Regex HyphenatedLongForm();

    [GeneratedRegex(@"\A[0-9]{6}\+[0-9]{4}\z")]
    private static partial Regex PlusShortForm();

    public class Lookup
    {
        [Test]
        public async Task All_HoldsEveryRowOfTheFourOfficialFiles()
        {
            FixtureSpec[] official =
            [
                Fixtures.Personnummer1890S, Fixtures.Personnummer2000S,
                Fixtures.SamordningsnummerOfficial, Fixtures.Samordningsnummer2020
            ];
            var rows = official.SelectMany(spec => CsvLoader.Column(spec)).ToHashSet();
            await Assert.That(rows.SetEquals(OfficialNumbers.All.Select(n => n.Long))).IsTrue();
        }

        [Test]
        public async Task Personnummer_HasTheRequestedBirthDate()
        {
            var leapDay = new DateOnly(2008, 2, 29);
            await Assert.That(OfficialNumbers.Personnummer(leapDay).BirthDate).IsEqualTo(leapDay);
        }

        [Test]
        public async Task Samordningsnummer_HasTheRequestedBirthDate()
        {
            var leapDay = new DateOnly(2020, 2, 29);
            await Assert.That(OfficialNumbers.Samordningsnummer(leapDay).BirthDate).IsEqualTo(leapDay);
        }

        [Test]
        public async Task DateInNoSet_Throws()
        {
            var absent = new DateOnly(1955, 7, 14);
            await Assert.That(() => OfficialNumbers.Personnummer(absent)).Throws<InvalidOperationException>();
        }
    }

    public class WrittenForms
    {
        [Test]
        public async Task WrittenForms_AreSlicesOfThePublishedRow()
        {
            var row = CsvLoader.Column(Fixtures.Personnummer1890S)[0];
            var number = OfficialNumbers.Centenarian;

            await Assert.That(number.Long).IsEqualTo(row);
            await Assert.That("18" + number.Short).IsEqualTo(row);
            await Assert.That(number.LongWithHyphen.Replace("-", "", StringComparison.Ordinal)).IsEqualTo(row);
            await Assert.That(number.LongWithPlus.Replace("+", "", StringComparison.Ordinal)).IsEqualTo(row);
            await Assert.That("18" + number.ShortWithHyphen.Replace("-", "", StringComparison.Ordinal)).IsEqualTo(row);
            await Assert.That(HyphenatedLongForm().IsMatch(number.LongWithHyphen)).IsTrue();
            await Assert.That(PlusShortForm().IsMatch(number.ShortWithPlus)).IsTrue();
        }

        [Test]
        [Arguments("")]
        [Arguments("12345678901")]
        [Arguments("12345678901a")]
        public async Task NotTwelveDigits_Throws(string digits)
        {
            await Assert.That(() => new OfficialPersonnummer(digits)).Throws<ArgumentException>();
        }
    }
}
