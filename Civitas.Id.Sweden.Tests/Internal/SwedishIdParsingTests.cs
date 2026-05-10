using Civitas.Id.Sweden.Internal;

namespace Civitas.Id.Sweden.Tests.Internal;

public class SwedishIdParsingTests
{
    public class ResolveCenturyMethod
    {
        [Test]
        [Arguments(2026, 8, false, 2008, "08 in 2026 with '-' → 2008")]
        [Arguments(2026, 99, false, 1999, "99 in 2026 with '-' → 1999 (sliding window past)")]
        [Arguments(2026, 26, true, 1926, "26 in 2026 with '+' → 1926 (centenarian convention)")]
        public async Task ResolveCentury_NoExplicitCentury_UsesSlidingWindow(
            int currentYear, int twoDigitYear, bool plusSeparator, int expected, string scenario)
        {
            _ = scenario;
            var century = SwedishIdParsing.ResolveCentury(
                null,
                twoDigitYear,
                currentYear,
                plusSeparator);

            var fullYear = century * 100 + twoDigitYear;
            await Assert.That(fullYear).IsEqualTo(expected);
        }

        [Test]
        public async Task ResolveCentury_ExplicitCentury_OverridesSlidingWindow()
        {
            var century = SwedishIdParsing.ResolveCentury(
                19,
                25,
                2026,
                false);

            await Assert.That(century).IsEqualTo(19);
        }
    }
}
