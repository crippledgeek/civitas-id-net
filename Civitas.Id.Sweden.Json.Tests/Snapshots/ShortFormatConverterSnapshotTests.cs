using System.Globalization;
using System.Text;
using System.Text.Json;
using Civitas.Id.Sweden.Core;
using Civitas.Id.Sweden.Json.Converters;

namespace Civitas.Id.Sweden.Json.Tests.Snapshots;

/// <summary>
///     Snapshot tests pinning the ShortFormat JSON converter outputs.
///     Verified files live in VerifiedSnapshots/ next to this source and are committed;
///     received files are gitignored.
/// </summary>
public sealed class ShortFormatConverterSnapshotTests
{
    private static readonly DateOnly[] PersonalDates =
    [
        new(2000, 1, 1),
        new(2005, 7, 14),
        new(2000, 2, 29),  // leap-day birthday
        new(2008, 12, 31)
    ];

    private static readonly DateOnly[] CoordinationDates =
    [
        new(1955, 8, 11),
        new(1981, 12, 15),
        new(2000, 6, 6),
        new(2008, 12, 27),
        new(2020, 2, 29)   // leap-day birthday
    ];

    /// <summary>a) PersonalIdShortFormatJsonConverter wire outputs across a representative date set.</summary>
    [Test]
    public Task PersonalId_ShortFormat()
    {
        var opts = new JsonSerializerOptions { Converters = { new PersonalIdShortFormatJsonConverter() } };
        var sb = new StringBuilder();
        foreach (var date in PersonalDates)
        {
            var id = PersonalId.Parse(OfficialNumbers.Personnummer(date).Long);
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
        var sb = new StringBuilder();
        foreach (var date in CoordinationDates)
        {
            var id = CoordinationId.Parse(OfficialNumbers.Samordningsnummer(date).Long);
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
        var sb = new StringBuilder();

        var personalId = PersonalId.Parse(OfficialNumbers.Modern.Long);
        SwedishOfficialId asBase = personalId;
        sb.AppendLine("PersonalId (via base type):");
        sb.AppendLine(CultureInfo.InvariantCulture, $"  LongFormat (canonical) = {personalId.LongFormat()}");
        sb.AppendLine(CultureInfo.InvariantCulture, $"  JSON wire form         = {JsonSerializer.Serialize(asBase, opts)}");
        sb.AppendLine();

        var coordId = CoordinationId.Parse(OfficialNumbers.TwentiethCenturySamordningsnummer.Long);
        asBase = coordId;
        sb.AppendLine("CoordinationId (via base type):");
        sb.AppendLine(CultureInfo.InvariantCulture, $"  LongFormat (canonical) = {coordId.LongFormat()}");
        sb.AppendLine(CultureInfo.InvariantCulture, $"  JSON wire form         = {JsonSerializer.Serialize(asBase, opts)}");
        sb.AppendLine();

        var legalOrg = OrganisationId.Parse(LegalPersonNumbers.Aktiebolag.Digits);
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
