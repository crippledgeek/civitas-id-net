using System.Globalization;
using System.Text;
using Civitas.Id.Sweden.Fakers;
using Civitas.Id.Sweden.Format;
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
        new(1900, 1, 1),
        new(1955, 7, 14),
        new(1988, 2, 29), // leap-day birthday
        new(2008, 12, 31),
        new(2024, 6, 6)
    ];

    /// <summary>a) PersonalId formatter outputs across a representative date set.</summary>
    [Test]
    public Task PersonalId_Formatters()
    {
        var faker = new PersonalIdFaker(42);
        var sb = new StringBuilder();
        foreach (var date in PersonalDates)
        {
            var id = faker.Generate(date);
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
        var faker = new CoordinationIdFaker(42);
        var sb = new StringBuilder();
        foreach (var date in PersonalDates)
        {
            var id = faker.Generate(date);
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
        var faker = new OrganisationIdFaker(42);
        var sb = new StringBuilder();

        var legal = faker.GenerateLegalPerson();
        sb.AppendLine("LegalPerson:");
        sb.AppendLine(CultureInfo.InvariantCulture, $"  LongFormat       = {legal.LongFormat()}");
        sb.AppendLine(CultureInfo.InvariantCulture, $"  ShortFormat      = {legal.ShortFormat()}");
        sb.AppendLine(CultureInfo.InvariantCulture, $"  Form             = {legal.Form}");
        sb.AppendLine();

        var enskild = faker.GeneratePhysicalPerson();
        sb.AppendLine("EnskildFirma:");
        sb.AppendLine(CultureInfo.InvariantCulture, $"  LongFormat       = {enskild.LongFormat()}");
        sb.AppendLine(CultureInfo.InvariantCulture, $"  ShortFormat      = {enskild.ShortFormat()}");
        sb.AppendLine(CultureInfo.InvariantCulture, $"  Form             = {enskild.Form}");
        sb.AppendLine();

        return Verify(sb.ToString());
    }

    /// <summary>
    ///     d) Centenarian-separator inference: born 1920-05-08, viewed at "today" dates spanning the
    ///     100-year boundary. The "+" appears when the bearer's age is 100+ at the inference date.
    /// </summary>
    [Test]
    public Task PersonalId_CentenarianSeparator()
    {
        var faker = new PersonalIdFaker(1920);
        var id = faker.Generate(1920, 5, 8);
        var anchors = new[]
        {
            new DateOnly(2019, 5, 7), // age 98
            new DateOnly(2020, 5, 7), // age 99 (one day before 100th)
            new DateOnly(2020, 5, 8), // age 100 (exactly 100th)
            new DateOnly(2020, 5, 9), // age 100
            new DateOnly(2025, 1, 1) // age 104
        };

        var sb = new StringBuilder();
        sb.AppendLine(CultureInfo.InvariantCulture, $"BirthDate=1920-05-08, canonical={id.LongFormat()}");
        sb.AppendLine();
        foreach (var anchor in anchors)
        {
            var tp = new FakeTimeProvider(new DateTimeOffset(anchor.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero));
            var longSep = id.Format(PnrFormat.LongFormatWithSeparator, tp);
            var shortSep = id.Format(PnrFormat.ShortFormatWithSeparator, tp);
            sb.AppendLine(CultureInfo.InvariantCulture, $"today={anchor:yyyy-MM-dd}");
            sb.AppendLine(CultureInfo.InvariantCulture, $"  LongFormatWithSeparator   = {longSep}");
            sb.AppendLine(CultureInfo.InvariantCulture, $"  ShortFormatWithSeparator  = {shortSep}");
        }

        return Verify(sb.ToString());
    }

    private static SettingsTask Verify(string target)
    {
        return Verifier.Verify(target).UseDirectory("VerifiedSnapshots");
    }
}
