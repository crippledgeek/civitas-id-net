using Civitas.Id.Sweden.Internal;

namespace Civitas.Id.Sweden.Tests.Internal;

public class SwedishIdParsingTests
{
    public class ResolveBirthYearMethod
    {
        private static BirthYearDigits Inferred(int twoDigitYear, bool plusSeparator)
        {
            return new BirthYearDigits
            {
                ExplicitCentury = null,
                TwoDigitYear = twoDigitYear,
                PlusSeparator = plusSeparator
            };
        }

        [Test]
        [Arguments(2026, 8, 2008)]
        [Arguments(2026, 26, 2026)]
        [Arguments(2026, 27, 1927)]
        [Arguments(2026, 99, 1999)]
        [Arguments(1800, 1, 1701)]
        [Arguments(100, 0, 100)]
        [Arguments(100, 1, 1)]
        [Arguments(99, 99, 99)]
        [Arguments(50, 51, -49)]
        [Arguments(1, 99, -1)]
        public async Task WithoutPlus_IsTheLatestYearNotAfterTheReference(
            int referenceYear, int twoDigitYear, int expected)
        {
            var year = SwedishIdParsing.ResolveBirthYear(Inferred(twoDigitYear, false), referenceYear);

            await Assert.That(year).IsEqualTo(expected);
        }

        [Test]
        [Arguments(2026, 26, 1926)]
        [Arguments(2026, 27, 1827)]
        [Arguments(100, 0, 0)]
        [Arguments(1, 0, -100)]
        [Arguments(1, 99, -101)]
        public async Task WithPlus_IsACenturyEarlier(int referenceYear, int twoDigitYear, int expected)
        {
            var year = SwedishIdParsing.ResolveBirthYear(Inferred(twoDigitYear, true), referenceYear);

            await Assert.That(year).IsEqualTo(expected);
        }

        [Test]
        [Arguments(19, 25, 1925)]
        [Arguments(0, 0, 0)]
        [Arguments(99, 99, 9999)]
        public async Task ExplicitCentury_IsUsedAsItIs(int century, int twoDigitYear, int expected)
        {
            var digits = new BirthYearDigits
            {
                ExplicitCentury = century,
                TwoDigitYear = twoDigitYear,
                PlusSeparator = true
            };

            await Assert.That(SwedishIdParsing.ResolveBirthYear(digits, 2026)).IsEqualTo(expected);
        }
    }
}
