namespace Civitas.Id.Sweden.Tests.Helpers;

/// <summary>
///     The date each data file's age columns are read on. Row tests that read a number use the date overloads; the
///     entry points without a date are asked only through the probes, about numbers whose outcome does not depend on it.
/// </summary>
internal static class ReferenceDates
{
    /// <summary>
    ///     The one day on which every row of <c>testpersonnummer_final.csv</c> holds its age, adult and child columns:
    ///     intersecting the (year + age, month, day) bounds of all rows leaves this date.
    /// </summary>
    public static readonly DateOnly PersonnummerFinal = new(2025, 4, 23);

    /// <summary>
    ///     The one day on which every row of <c>Testsamordningsnummer_2019_corrected.csv</c> holds its age, adult and
    ///     child columns, found the same way.
    /// </summary>
    public static readonly DateOnly SamordningsnummerCorrected = new(2025, 5, 1);

    /// <summary>
    ///     A day on which every sole trader of <c>testorganisationsnummer_enskild_firma.csv</c> has the separator the
    ///     file gives: the rows' birth dates allow any day from the 100th birthday of the youngest bearer written with
    ///     <c>+</c> to the day before that of the oldest written with <c>-</c>. A test derives that window from the rows
    ///     and checks this day lies inside it. The extended organisation file holds only legal persons, which take no
    ///     <c>+</c>.
    /// </summary>
    public static readonly DateOnly SoleTrader = PersonnummerFinal;

    /// <summary>
    ///     The day for files with no column that depends on it: every row carries its century, so the day decides
    ///     nothing.
    /// </summary>
    public static readonly DateOnly Undated = ParserProbe.Today;
}
