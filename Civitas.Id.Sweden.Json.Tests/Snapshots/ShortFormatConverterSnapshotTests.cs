using System.Globalization;
using System.Text;
using System.Text.Json;
using Civitas.Id.Sweden.Core;
using Civitas.Id.Sweden.Fakers;
using Civitas.Id.Sweden.Json.Converters;

namespace Civitas.Id.Sweden.Json.Tests.Snapshots;

/// <summary>
///     Snapshot tests pinning the ShortFormat JSON converter outputs.
///     Verified files live in VerifiedSnapshots/ next to this source and are committed;
///     received files are gitignored.
/// </summary>
public sealed class ShortFormatConverterSnapshotTests
{
    private static readonly DateOnly[] Dates =
    [
        new(1920, 5, 8),   // centenarian boundary fixture
        new(1955, 7, 14),
        new(1988, 2, 29),  // leap-day birthday
        new(2008, 12, 31)
    ];

    /// <summary>a) PersonalIdShortFormatJsonConverter wire outputs across a representative date set.</summary>
    [Test]
    public Task PersonalId_ShortFormat()
    {
        var opts = new JsonSerializerOptions { Converters = { new PersonalIdShortFormatJsonConverter() } };
        var faker = new PersonalIdFaker(42);
        var sb = new StringBuilder();
        foreach (var date in Dates)
        {
            var id = faker.Generate(date);
            var serialized = JsonSerializer.Serialize(id, opts);
            sb.AppendLine(CultureInfo.InvariantCulture, $"BirthDate={date:yyyy-MM-dd}");
            sb.AppendLine(CultureInfo.InvariantCulture, $"  LongFormat (canonical) = {id.LongFormat()}");
            sb.AppendLine(CultureInfo.InvariantCulture, $"  JSON wire form         = {serialized}");
            sb.AppendLine();
        }

        return Verify(sb.ToString());
    }

    /// <summary>b) CoordinationIdShortFormatJsonConverter wire outputs across a representative date set.</summary>
    [Test]
    public Task CoordinationId_ShortFormat()
    {
        var opts = new JsonSerializerOptions { Converters = { new CoordinationIdShortFormatJsonConverter() } };
        var faker = new CoordinationIdFaker(42);
        var sb = new StringBuilder();
        foreach (var date in Dates)
        {
            var id = faker.Generate(date);
            var serialized = JsonSerializer.Serialize(id, opts);
            sb.AppendLine(CultureInfo.InvariantCulture, $"BirthDate={date:yyyy-MM-dd}");
            sb.AppendLine(CultureInfo.InvariantCulture, $"  LongFormat (canonical) = {id.LongFormat()}");
            sb.AppendLine(CultureInfo.InvariantCulture, $"  JSON wire form         = {serialized}");
            sb.AppendLine();
        }

        return Verify(sb.ToString());
    }

    /// <summary>
    ///     c) SwedishOfficialIdShortFormatJsonConverter polymorphic dispatch:
    ///     PersonalId and CoordinationId use ShortFormat; OrganisationId uses LongFormat.
    /// </summary>
    [Test]
    public Task SwedishOfficialId_ShortFormat_PolymorphicDispatch()
    {
        var opts = new JsonSerializerOptions { Converters = { new SwedishOfficialIdShortFormatJsonConverter() } };
        var personalFaker = new PersonalIdFaker(42);
        var coordFaker = new CoordinationIdFaker(42);
        var orgFaker = new OrganisationIdFaker(42);

        var sb = new StringBuilder();

        var personalId = personalFaker.Generate(new DateOnly(1988, 6, 15));
        SwedishOfficialId asBase = personalId;
        sb.AppendLine("PersonalId (via base type):");
        sb.AppendLine(CultureInfo.InvariantCulture, $"  LongFormat (canonical) = {personalId.LongFormat()}");
        sb.AppendLine(CultureInfo.InvariantCulture, $"  JSON wire form         = {JsonSerializer.Serialize(asBase, opts)}");
        sb.AppendLine();

        var coordId = coordFaker.Generate(new DateOnly(1975, 3, 20));
        asBase = coordId;
        sb.AppendLine("CoordinationId (via base type):");
        sb.AppendLine(CultureInfo.InvariantCulture, $"  LongFormat (canonical) = {coordId.LongFormat()}");
        sb.AppendLine(CultureInfo.InvariantCulture, $"  JSON wire form         = {JsonSerializer.Serialize(asBase, opts)}");
        sb.AppendLine();

        var legalOrg = orgFaker.GenerateLegalPerson();
        asBase = legalOrg;
        sb.AppendLine("OrganisationId/LegalPerson (via base type, always LongFormat):");
        sb.AppendLine(CultureInfo.InvariantCulture, $"  LongFormat (canonical) = {legalOrg.LongFormat()}");
        sb.AppendLine(CultureInfo.InvariantCulture, $"  JSON wire form         = {JsonSerializer.Serialize(asBase, opts)}");
        sb.AppendLine();

        return Verify(sb.ToString());
    }

    private static SettingsTask Verify(string target)
    {
        return Verifier.Verify(target).UseDirectory("VerifiedSnapshots");
    }
}
