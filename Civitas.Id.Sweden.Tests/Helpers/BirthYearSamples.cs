using Civitas.Id.Sweden.Fakers.Internal;

namespace Civitas.Id.Sweden.Tests.Helpers;

/// <summary>The numbers on both sides of the first accepted birth year that every reader is asked about.</summary>
internal static class BirthYearSamples
{
    private const int Seed = 1;

    /// <summary>Year zero, which no calendar type holds, and edges of centuries up to the last rejected year.</summary>
    private static readonly IReadOnlyList<int> EarlyYears =
        [0, 1, 99, 100, 1599, 1600, 1699, 1700, BirthYearCases.FirstAcceptedYear - 1];

    extension(BirthYearCases cases)
    {
        /// <summary>The number of someone born in the first accepted year.</summary>
        public string Accepted => cases.BornIn(BirthYearCases.FirstAcceptedYear);

        /// <summary>Numbers of someone born too early.</summary>
        public IReadOnlyList<string> Early =>
        [
            .. EarlyYears.Select(cases.BornIn),
            cases.WithDefect(IdDefect.BirthYearBelowMinimum)
        ];

        /// <summary>A number that is rejected for its check digit, as the early ones must be.</summary>
        public string WithWrongCheckDigit => cases.WithDefect(IdDefect.WrongCheckDigit);

        private string WithDefect(IdDefect defect)
        {
            return cases.Defects.Corrupt(defect, cases.Draft, new Random(Seed).Next).Text;
        }
    }
}
