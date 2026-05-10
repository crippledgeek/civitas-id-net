using System.Diagnostics;
using Civitas.Id.Sweden.Core;
using Civitas.Id.Sweden.Errors;
using Civitas.Id.Sweden.Tests.Helpers;
using Microsoft.Extensions.Time.Testing;

namespace Civitas.Id.Sweden.Tests.Core;

/// <summary>
///     Tests for the static companion methods on <see cref="SwedishOfficialId" />.
/// </summary>
public class SwedishOfficialIdTests
{
    // Skatteverket-published valid samples — one of each subtype.
    private const string ValidPersonalId = "189001019802"; // 1890s personnummer (female)
    private const string ValidCoordinationId = "191401682396"; // 1914 samordningsnummer (male)
    private const string ValidOrganisationId = "5560160680"; // EuropakooperativEgtsEric

    public class TryParseAny
    {
        [Test]
        public async Task PersonalIdInput_ReturnsPersonalId()
        {
            var ok = SwedishOfficialId.TryParseAny(ValidPersonalId, out var id);
            await Assert.That(ok).IsTrue();
            await Assert.That(id).IsTypeOf<PersonalId>();
        }

        [Test]
        public async Task CoordinationIdInput_ReturnsCoordinationId()
        {
            var ok = SwedishOfficialId.TryParseAny(ValidCoordinationId, out var id);
            await Assert.That(ok).IsTrue();
            await Assert.That(id).IsTypeOf<CoordinationId>();
        }

        [Test]
        public async Task OrganisationIdInput_ReturnsOrganisationId()
        {
            var ok = SwedishOfficialId.TryParseAny(ValidOrganisationId, out var id);
            await Assert.That(ok).IsTrue();
            await Assert.That(id).IsTypeOf<OrganisationId>();
        }

        [Test]
        [Arguments(null)]
        [Arguments("")]
        [Arguments("garbage")]
        public async Task InvalidInput_ReturnsFalse(string? input)
        {
            var ok = SwedishOfficialId.TryParseAny(input, out var id);
            await Assert.That(ok).IsFalse();
            await Assert.That(id).IsNull();
        }
    }

    public class ParseAny
    {
        [Test]
        public async Task ValidPersonalId_ReturnsInstance()
        {
            var id = SwedishOfficialId.ParseAny(ValidPersonalId);
            await Assert.That(id).IsTypeOf<PersonalId>();
        }

        [Test]
        public async Task Invalid_ThrowsInvalidIdNumberExceptionWithReasonUnsupported()
        {
            try
            {
                _ = SwedishOfficialId.ParseAny("garbage");
                throw new InvalidOperationException("Expected exception not thrown.");
            }
            catch (InvalidIdNumberException ex)
            {
                await Assert.That(ex.Reason).IsEqualTo(InvalidIdNumberReason.UnsupportedIdType);
                await Assert.That(ex.RedactedInput).IsEqualTo("ga*bage");
            }
        }

        [Test]
        public async Task Null_ThrowsArgumentNullException()
        {
            await Assert.That(() => SwedishOfficialId.ParseAny(null!))
                .Throws<ArgumentNullException>();
        }
    }

    public class IsValid
    {
        [Test]
        [Arguments(ValidPersonalId)]
        [Arguments(ValidCoordinationId)]
        [Arguments(ValidOrganisationId)]
        public async Task AcceptsAllThreeTypes(string input)
        {
            await Assert.That(SwedishOfficialId.IsValid(input)).IsTrue();
        }

        [Test]
        [Arguments(null)]
        [Arguments("")]
        [Arguments("garbage")]
        public async Task RejectsInvalid(string? input)
        {
            await Assert.That(SwedishOfficialId.IsValid(input)).IsFalse();
        }
    }

    public class PatternMatching
    {
        private static readonly string[] ExpectedLabels = ["person", "coord", "org"];

        [Test]
        public async Task SwitchExpression_DispatchesByConcreteType()
        {
            // Compile-time verification of the closed-set switch with UnreachableException safety net.
            // C# 14 does NOT recognise sealed-record hierarchies as closed for exhaustiveness;
            // the `_ =>` arm is the documented workaround per the design spec.
            SwedishOfficialId id = PersonalId.Parse(ValidPersonalId);
            var label = id switch
            {
                PersonalId => "person",
                CoordinationId => "coord",
                OrganisationId => "org",
                _ => throw new UnreachableException()
            };
            await Assert.That(label).IsEqualTo("person");
        }

        [Test]
        public async Task SwitchExpression_HandlesAllThreeSubtypes()
        {
            var ids = new SwedishOfficialId[]
            {
                PersonalId.Parse(ValidPersonalId),
                CoordinationId.Parse(ValidCoordinationId),
                OrganisationId.Parse(ValidOrganisationId)
            };

            var labels = ids.Select(id => id switch
            {
                PersonalId => "person",
                CoordinationId => "coord",
                OrganisationId => "org",
                _ => throw new UnreachableException()
            }).ToArray();

            await Assert.That(labels).IsEquivalentTo(ExpectedLabels);
        }
    }

    public class ParseAnyWithTimeProvider
    {
        private static FakeTimeProvider StockholmFake(DateTimeOffset utcNow)
        {
            return new FakeTimeProvider(utcNow);
        }

        [Test]
        public async Task ParseAny_PersonalId_TwoDigitYear_UsesProviderYear()
        {
            // Year 2050 — chosen to differ from wall-clock year (2026) so that this test
            // documents intent to use the provider rather than DateTime.UtcNow. The deeper
            // "provider-year is required" guard lives in PersonalIdTests.TimeProviderParsing
            // (Task 5); this layer asserts dispatch + threading + a tightened result check.
            var fake = StockholmFake(new DateTimeOffset(2050, 6, 15, 12, 0, 0, TimeSpan.Zero));
            var id = SwedishOfficialId.ParseAny("080508-0009", fake);

            await Assert.That(id).IsTypeOf<PersonalId>();
            await Assert.That(((PersonalId)id).BirthDate.Year).IsEqualTo(2008);
        }

        [Test]
        public async Task ParseAny_NullTimeProvider_Throws()
        {
            await Assert.That(() => SwedishOfficialId.ParseAny("080508-0009", null!))
                .Throws<ArgumentNullException>();
        }

        [Test]
        public async Task TryParseAny_PersonalId_TwoDigitYear_UsesProviderYear()
        {
            // See ParseAny_PersonalId_TwoDigitYear_UsesProviderYear for rationale on the 2050 anchor.
            var fake = StockholmFake(new DateTimeOffset(2050, 6, 15, 12, 0, 0, TimeSpan.Zero));
            var ok = SwedishOfficialId.TryParseAny("080508-0009", fake, out var id);

            await Assert.That(ok).IsTrue();
            await Assert.That(id).IsTypeOf<PersonalId>();
            await Assert.That(((PersonalId)id!).BirthDate.Year).IsEqualTo(2008);
        }

        [Test]
        public async Task TryParseAny_NullTimeProvider_Throws()
        {
            await Assert.That(() => SwedishOfficialId.TryParseAny("080508-0009", null!, out _))
                .Throws<ArgumentNullException>();
        }

        [Test]
        public async Task TryParseAny_WithTimeProvider_DispatchesToCoordinationIdBranch()
        {
            var fake = new FakeTimeProvider(new DateTimeOffset(2026, 1, 1, 12, 0, 0, TimeSpan.Zero));
            var success = SwedishOfficialId.TryParseAny(ValidCoordinationId, fake, out var id);

            await Assert.That(success).IsTrue();
            await Assert.That(id).IsTypeOf<CoordinationId>();
        }

        [Test]
        public async Task TryParseAny_WithTimeProvider_DispatchesToOrganisationIdBranch()
        {
            var fake = new FakeTimeProvider(new DateTimeOffset(2026, 1, 1, 12, 0, 0, TimeSpan.Zero));
            var success = SwedishOfficialId.TryParseAny(ValidOrganisationId, fake, out var id);

            await Assert.That(success).IsTrue();
            await Assert.That(id).IsTypeOf<OrganisationId>();
        }
    }

    public class ParseAnyWithExplicitToday
    {
        [Test]
        public async Task TryParseAny_StringDateOnly_DispatchesToPersonalIdBranch()
        {
            var ok = SwedishOfficialId.TryParseAny(
                ValidPersonalId, new DateOnly(2026, 5, 10), out var id);
            await Assert.That(ok).IsTrue();
            await Assert.That(id).IsTypeOf<PersonalId>();
        }

        [Test]
        public async Task TryParseAny_StringDateOnly_DispatchesToCoordinationIdBranch()
        {
            var ok = SwedishOfficialId.TryParseAny(
                ValidCoordinationId, new DateOnly(2026, 5, 10), out var id);
            await Assert.That(ok).IsTrue();
            await Assert.That(id).IsTypeOf<CoordinationId>();
        }

        [Test]
        public async Task TryParseAny_StringDateOnly_DispatchesToOrganisationIdBranch()
        {
            var ok = SwedishOfficialId.TryParseAny(
                ValidOrganisationId, new DateOnly(2026, 5, 10), out var id);
            await Assert.That(ok).IsTrue();
            await Assert.That(id).IsTypeOf<OrganisationId>();
        }

        [Test]
        public async Task ParseAny_StringDateOnly_ValidInput_ReturnsParsed()
        {
            var id = SwedishOfficialId.ParseAny(ValidPersonalId, new DateOnly(2026, 5, 10));
            await Assert.That(id).IsTypeOf<PersonalId>();
        }

        [Test]
        public async Task ParseAny_StringDateOnly_InvalidInput_Throws()
        {
            await Assert.That(() =>
                    SwedishOfficialId.ParseAny("not-an-id", new DateOnly(2026, 5, 10)))
                .Throws<InvalidIdNumberException>();
        }

        [Test]
        public async Task TryParseAny_SpanDateOnly_EquivalentToStringOverload()
        {
            var today = new DateOnly(2026, 5, 10);
            var okString = SwedishOfficialId.TryParseAny(ValidPersonalId, today, out var fromString);
            var okSpan = SwedishOfficialId.TryParseAny(
                ValidPersonalId.AsSpan(), today, out var fromSpan);
            await Assert.That(okString).IsTrue();
            await Assert.That(okSpan).IsTrue();
            await Assert.That(fromSpan).IsEqualTo(fromString);
        }

        [Test]
        public async Task TryParseAny_SpanDateOnly_DispatchesToCoordinationIdBranch()
        {
            var ok = SwedishOfficialId.TryParseAny(
                ValidCoordinationId.AsSpan(), new DateOnly(2026, 5, 10), out var id);
            await Assert.That(ok).IsTrue();
            await Assert.That(id).IsTypeOf<CoordinationId>();
        }

        [Test]
        public async Task TryParseAny_SpanDateOnly_DispatchesToOrganisationIdBranch()
        {
            var ok = SwedishOfficialId.TryParseAny(
                ValidOrganisationId.AsSpan(), new DateOnly(2026, 5, 10), out var id);
            await Assert.That(ok).IsTrue();
            await Assert.That(id).IsTypeOf<OrganisationId>();
        }

        [Test]
        public async Task TryParseAny_SpanDateOnly_InvalidInput_ReturnsFalse()
        {
            var ok = SwedishOfficialId.TryParseAny(
                "not-an-id".AsSpan(), new DateOnly(2026, 5, 10), out var id);
            await Assert.That(ok).IsFalse();
            await Assert.That(id).IsNull();
        }

        [Test]
        public async Task ParseAny_SpanDateOnly_InvalidInput_Throws()
        {
            await Assert.That(() =>
                    SwedishOfficialId.ParseAny("not-an-id".AsSpan(), new DateOnly(2026, 5, 10)))
                .Throws<InvalidIdNumberException>();
        }
    }

    public class CountryCodeProperty
    {
        [Test]
        public async Task CountryCode_StaticProperty_IsSE()
        {
            await Assert.That(SwedishOfficialId.CountryCode).IsEqualTo("SE");
        }
    }

    /// <summary>
    ///     Fixture-driven fan-out mirroring the Java sibling's
    ///     <c>SwedishOfficialIdTest</c> parameterized tests. Each fixture is consumed
    ///     by multiple test methods to exercise the public surface area uniformly.
    /// </summary>
    public class FixtureDriven
    {
        // ── Data sources ────────────────────────────────────────────────

        public static IEnumerable<string> Personnummer1890S()
        {
            return CsvLoader.LoadColumn("Testpersonnummer 1890-1899.csv");
        }

        public static IEnumerable<string> PersonnummerFinal()
        {
            var rows = CsvLoader.Load("testpersonnummer_final.csv").Skip(1);
            foreach (var r in rows) yield return r[0];
        }

        public static IEnumerable<string> Samordningsnummer()
        {
            var rows = CsvLoader.Load("Testsamordningsnummer_2019_corrected.csv").Skip(1);
            foreach (var r in rows) yield return r[0];
        }

        public static IEnumerable<string> OrganisationsnummerTextual()
        {
            return CsvLoader.LoadColumn("testorganisationsnummer.txt");
        }

        public static IEnumerable<string> InvalidIds()
        {
            return CsvLoader.LoadColumn("invalid_swedish_ids.csv");
        }

        // ── IsValid fan-out ─────────────────────────────────────────────

        [Test]
        [MethodDataSource(nameof(Personnummer1890S))]
        public async Task IsValid_Personnummer1890s_ReturnsTrue(string pin)
        {
            await Assert.That(SwedishOfficialId.IsValid(pin)).IsTrue();
        }

        [Test]
        [MethodDataSource(nameof(PersonnummerFinal))]
        public async Task IsValid_PersonnummerFinal_ReturnsTrue(string pin)
        {
            await Assert.That(SwedishOfficialId.IsValid(pin)).IsTrue();
        }

        [Test]
        [MethodDataSource(nameof(Samordningsnummer))]
        public async Task IsValid_Samordningsnummer_ReturnsTrue(string pin)
        {
            await Assert.That(SwedishOfficialId.IsValid(pin)).IsTrue();
        }

        [Test]
        [MethodDataSource(nameof(OrganisationsnummerTextual))]
        public async Task IsValid_OrganisationsnummerTextual_ReturnsTrue(string pin)
        {
            await Assert.That(SwedishOfficialId.IsValid(pin)).IsTrue();
        }

        [Test]
        [MethodDataSource(nameof(InvalidIds))]
        public async Task IsValid_InvalidIds_ReturnsFalse(string pin)
        {
            await Assert.That(SwedishOfficialId.IsValid(pin)).IsFalse();
        }

        // ── TryParseAny fan-out (mirrors Java parseAny) ────────────────

        [Test]
        [MethodDataSource(nameof(Personnummer1890S))]
        public async Task TryParseAny_Personnummer1890s_ReturnsPersonalId(string pin)
        {
            var ok = SwedishOfficialId.TryParseAny(pin, out var id);
            await Assert.That(ok).IsTrue();
            await Assert.That(id).IsTypeOf<PersonalId>();
        }

        [Test]
        [MethodDataSource(nameof(PersonnummerFinal))]
        public async Task TryParseAny_PersonnummerFinal_ReturnsPersonalId(string pin)
        {
            var ok = SwedishOfficialId.TryParseAny(pin, out var id);
            await Assert.That(ok).IsTrue();
            await Assert.That(id).IsTypeOf<PersonalId>();
        }

        [Test]
        [MethodDataSource(nameof(Samordningsnummer))]
        public async Task TryParseAny_Samordningsnummer_ReturnsCoordinationId(string pin)
        {
            var ok = SwedishOfficialId.TryParseAny(pin, out var id);
            await Assert.That(ok).IsTrue();
            await Assert.That(id).IsTypeOf<CoordinationId>();
        }

        [Test]
        [MethodDataSource(nameof(OrganisationsnummerTextual))]
        public async Task TryParseAny_OrganisationsnummerTextual_ReturnsSwedishOfficialId(string pin)
        {
            // Java note: organisation numbers can be parsed as PersonalId (physical
            // person) or OrganisationId (legal person). We only assert resolution.
            var ok = SwedishOfficialId.TryParseAny(pin, out var id);
            await Assert.That(ok).IsTrue();
            await Assert.That(id).IsNotNull();
        }

        [Test]
        [MethodDataSource(nameof(InvalidIds))]
        public async Task TryParseAny_InvalidIds_ReturnsFalse(string pin)
        {
            var ok = SwedishOfficialId.TryParseAny(pin, out var id);
            await Assert.That(ok).IsFalse();
            await Assert.That(id).IsNull();
        }

        // ── ParseAny fan-out (mirrors Java parseAnyOrThrow) ────────────

        [Test]
        [MethodDataSource(nameof(Personnummer1890S))]
        public async Task ParseAny_Personnummer1890s_ReturnsPersonalId(string pin)
        {
            var id = SwedishOfficialId.ParseAny(pin);
            await Assert.That(id).IsTypeOf<PersonalId>();
        }

        [Test]
        [MethodDataSource(nameof(PersonnummerFinal))]
        public async Task ParseAny_PersonnummerFinal_ReturnsPersonalId(string pin)
        {
            var id = SwedishOfficialId.ParseAny(pin);
            await Assert.That(id).IsTypeOf<PersonalId>();
        }

        [Test]
        [MethodDataSource(nameof(Samordningsnummer))]
        public async Task ParseAny_Samordningsnummer_ReturnsCoordinationId(string pin)
        {
            var id = SwedishOfficialId.ParseAny(pin);
            await Assert.That(id).IsTypeOf<CoordinationId>();
        }

        [Test]
        [MethodDataSource(nameof(OrganisationsnummerTextual))]
        public async Task ParseAny_OrganisationsnummerTextual_DoesNotThrow(string pin)
        {
            var id = SwedishOfficialId.ParseAny(pin);
            await Assert.That(id).IsNotNull();
        }

        [Test]
        [MethodDataSource(nameof(InvalidIds))]
        public async Task ParseAny_InvalidIds_Throws(string pin)
        {
            await Assert.That(() => SwedishOfficialId.ParseAny(pin))
                .Throws<InvalidIdNumberException>();
        }

        // ── Pattern-matching fan-out (mirrors Java getIdType) ──────────

        [Test]
        [MethodDataSource(nameof(Personnummer1890S))]
        public async Task PatternMatch_Personnummer1890s_IsPersonalId(string pin)
        {
            var id = SwedishOfficialId.ParseAny(pin);
            await Assert.That(id is PersonalId).IsTrue();
        }
    }
}
