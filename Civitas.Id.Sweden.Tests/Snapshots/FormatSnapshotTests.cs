using System.Globalization;
using System.Text;
using Civitas.Id.Sweden.Core;
using Civitas.Id.Sweden.Format;
using Civitas.Id.Sweden.Tests.Helpers;
using Microsoft.Extensions.Time.Testing;

namespace Civitas.Id.Sweden.Tests.Snapshots;

/// <summary>
///     Snapshot tests pinning the public formatter outputs. Verified files live next to this
///     source and are committed; received files are gitignored.
/// </summary>
public sealed class FormatSnapshotTests
{
    private static readonly DateOnly[] PersonalDates =
    [
        new(2000, 1, 1),
        new(2000, 2, 29), // leap-day birthday
        new(2005, 7, 14),
        new(2008, 12, 31),
        new(2009, 6, 6)
    ];

    private static readonly DateOnly[] CoordinationDates =
    [
        new(1955, 8, 11),
        new(1981, 12, 15),
        new(2000, 6, 6),
        new(2008, 12, 27),
        new(2020, 2, 29) // leap-day birthday
    ];

    /// <summary>a) PersonalId formatter outputs across a representative date set.</summary>
    [Test]
    public Task PersonalId_Formatters()
    {
        var sb = new StringBuilder();
        foreach (var date in PersonalDates)
        {
            var id = PersonalId.Parse(OfficialNumbers.Personnummer(date).Long);
            sb.AppendLine(CultureInfo.InvariantCulture, $"BirthDate={date:yyyy-MM-dd}");
            sb.AppendLine(CultureInfo.InvariantCulture, $"  LongFormat                       = {id.LongFormat()}");
            sb.AppendLine(CultureInfo.InvariantCulture, $"  ShortFormat                      = {id.ShortFormat()}");
            sb.AppendLine(CultureInfo.InvariantCulture,
                $"  Format(LongFormat)               = {id.Format(PnrFormat.LongFormat)}");
            sb.AppendLine(CultureInfo.InvariantCulture,
                $"  Format(ShortFormatWithStandard)  = {id.Format(PnrFormat.ShortFormatWithStandardSeparator)}");
            sb.AppendLine();
        }

        return Verify(sb.ToString());
    }

    /// <summary>b) CoordinationId formatter outputs.</summary>
    [Test]
    public Task CoordinationId_Formatters()
    {
        var sb = new StringBuilder();
        foreach (var date in CoordinationDates)
        {
            var id = CoordinationId.Parse(OfficialNumbers.Samordningsnummer(date).Long);
            sb.AppendLine(CultureInfo.InvariantCulture, $"BirthDate={date:yyyy-MM-dd}");
            sb.AppendLine(CultureInfo.InvariantCulture, $"  LongFormat                       = {id.LongFormat()}");
            sb.AppendLine(CultureInfo.InvariantCulture, $"  ShortFormat                      = {id.ShortFormat()}");
            sb.AppendLine(CultureInfo.InvariantCulture,
                $"  Format(LongFormat)               = {id.Format(PnrFormat.LongFormat)}");
            sb.AppendLine(CultureInfo.InvariantCulture,
                $"  Format(ShortFormatWithStandard)  = {id.Format(PnrFormat.ShortFormatWithStandardSeparator)}");
            sb.AppendLine();
        }

        return Verify(sb.ToString());
    }

    /// <summary>c) OrganisationId formatter outputs across legal-person + Enskild firma forms.</summary>
    [Test]
    public Task OrganisationId_Formatters()
    {
        var sb = new StringBuilder();

        var legal = OrganisationId.Parse(LegalPersonNumbers.Aktiebolag.Digits);
        sb.AppendLine("LegalPerson:");
        sb.AppendLine(CultureInfo.InvariantCulture, $"  LongFormat       = {legal.LongFormat()}");
        sb.AppendLine(CultureInfo.InvariantCulture, $"  ShortFormat      = {legal.ShortFormat()}");
        sb.AppendLine(CultureInfo.InvariantCulture, $"  Form             = {legal.Form}");
        sb.AppendLine();

        var enskild = OrganisationId.Parse(OfficialNumbers.Modern.Long);
        sb.AppendLine("EnskildFirma:");
        sb.AppendLine(CultureInfo.InvariantCulture, $"  LongFormat       = {enskild.LongFormat()}");
        sb.AppendLine(CultureInfo.InvariantCulture, $"  ShortFormat      = {enskild.ShortFormat()}");
        sb.AppendLine(CultureInfo.InvariantCulture, $"  Form             = {enskild.Form}");
        sb.AppendLine();

        return Verify(sb.ToString());
    }

    /// <summary>
    ///     d) Centenarian-separator inference: born 1895-05-08, viewed at "today" dates spanning the
    ///     100-year boundary. The "+" appears when the bearer's age is 100+ at the inference date. The short form
    ///     is asserted and not stored: without a plus it reads as a person born in 1995.
    /// </summary>
    [Test]
    public async Task PersonalId_CentenarianSeparator()
    {
        var birthDate = new DateOnly(1895, 5, 8);
        var number = OfficialNumbers.Personnummer(birthDate);
        var id = PersonalId.Parse(number.Long);
        var anchors = new[]
        {
            new DateOnly(1994, 5, 7), // age 98
            new DateOnly(1995, 5, 7), // age 99 (one day before 100th)
            new DateOnly(1995, 5, 8), // age 100 (exactly 100th)
            new DateOnly(1995, 5, 9), // age 100
            new DateOnly(2000, 1, 1) // age 104
        };

        var sb = new StringBuilder();
        sb.AppendLine(CultureInfo.InvariantCulture, $"BirthDate={birthDate:yyyy-MM-dd}, canonical={id.LongFormat()}");
        sb.AppendLine();
        foreach (var anchor in anchors)
        {
            var tp = new FakeTimeProvider(new DateTimeOffset(anchor.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero));
            var longSep = id.Format(PnrFormat.LongFormatWithSeparator, tp);
            var shortSep = id.Format(PnrFormat.ShortFormatWithSeparator, tp);
            sb.AppendLine(CultureInfo.InvariantCulture, $"today={anchor:yyyy-MM-dd}");
            sb.AppendLine(CultureInfo.InvariantCulture, $"  LongFormatWithSeparator   = {longSep}");
            await Assert.That(shortSep)
                .IsEqualTo(anchor < birthDate.AddYears(100) ? number.ShortWithHyphen : number.ShortWithPlus);
        }

        await Verify(sb.ToString());
    }

    /// <summary>The short forms of numbers that are not stored: without a plus they read as younger persons.</summary>
    [Test]
    public async Task Centenarian_ShortForms()
    {
        var person = PersonalId.Parse(OfficialNumbers.Centenarian.Long);
        await Assert.That(person.ShortFormat()).IsEqualTo(OfficialNumbers.Centenarian.Short);
        await Assert.That(person.Format(PnrFormat.ShortFormatWithStandardSeparator))
            .IsEqualTo(OfficialNumbers.Centenarian.ShortWithHyphen);

        var coordination = CoordinationId.Parse(OfficialNumbers.CentenarianSamordningsnummer.Long);
        await Assert.That(coordination.ShortFormat()).IsEqualTo(OfficialNumbers.CentenarianSamordningsnummer.Short);
        await Assert.That(coordination.Format(PnrFormat.ShortFormatWithStandardSeparator))
            .IsEqualTo(OfficialNumbers.CentenarianSamordningsnummer.ShortWithHyphen);
    }

    private static SettingsTask Verify(string target)
    {
        return Verifier.Verify(target).UseDirectory("VerifiedSnapshots");
    }
}
