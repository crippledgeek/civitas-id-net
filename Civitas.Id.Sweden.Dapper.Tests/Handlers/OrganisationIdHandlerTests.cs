namespace Civitas.Id.Sweden.Dapper.Tests.Handlers;

using System.Data;
using Civitas.Id.Sweden.Core;
using Civitas.Id.Sweden.Dapper;
using Civitas.Id.Sweden.Errors;
using Microsoft.Data.Sqlite;

public class OrganisationIdHandlerTests
{
    private const string LegalAktiebolag = "5560360793";
    private const string EnskildFirma12  = "199001019802";

    // Return type is the interface to mirror how the handler is invoked in production.
#pragma warning disable CA1859
    private static IDbDataParameter NewParameter() => new SqliteParameter();
#pragma warning restore CA1859

    public class SetValueBehavior
    {
        [Test]
        public async Task SetValue_LegalPerson_Writes10DigitForm()
        {
            var p = NewParameter();
            OrganisationIdHandler.Default.SetValue(p, OrganisationId.Parse(LegalAktiebolag));

            await Assert.That(p.Value).IsEqualTo(LegalAktiebolag);
        }

        [Test]
        public async Task SetValue_EnskildFirma_Writes12DigitForm()
        {
            var p = NewParameter();
            OrganisationIdHandler.Default.SetValue(p, OrganisationId.Parse(EnskildFirma12));

            await Assert.That(p.Value).IsEqualTo(EnskildFirma12);
        }

        [Test]
        public async Task SetValue_LegalPerson_SetsDbTypeAnsiString()
        {
            var p = NewParameter();
            OrganisationIdHandler.Default.SetValue(p, OrganisationId.Parse(LegalAktiebolag));

            await Assert.That(p.DbType).IsEqualTo(DbType.AnsiString);
        }

        [Test]
        public async Task SetValue_LegalPerson_SetsSizeTo12()
        {
            var p = NewParameter();
            OrganisationIdHandler.Default.SetValue(p, OrganisationId.Parse(LegalAktiebolag));

            await Assert.That(p.Size).IsEqualTo(12);
        }

        [Test]
        public async Task SetValue_WithNullValue_WritesDbNull()
        {
            var p = NewParameter();
            OrganisationIdHandler.Default.SetValue(p, value: null);

            await Assert.That(p.Value).IsEqualTo(DBNull.Value);
        }

        [Test]
        public async Task SetValue_WithNullParameter_ThrowsArgumentNullException()
        {
            await Assert.That(() =>
                OrganisationIdHandler.Default.SetValue(parameter: null!, value: OrganisationId.Parse(LegalAktiebolag)))
                .Throws<ArgumentNullException>();
        }
    }

    public class ParseBehavior
    {
        [Test]
        public async Task Parse_LegalPersonString_RoundTrips()
        {
            var parsed = OrganisationIdHandler.Default.Parse(LegalAktiebolag);

            await Assert.That(parsed).IsEqualTo(OrganisationId.Parse(LegalAktiebolag));
        }

        [Test]
        public async Task Parse_EnskildFirma12DigitString_RoundTrips()
        {
            var parsed = OrganisationIdHandler.Default.Parse(EnskildFirma12);

            await Assert.That(parsed).IsEqualTo(OrganisationId.Parse(EnskildFirma12));
        }

        [Test]
        public async Task Parse_WithNonStringObject_ThrowsDataException()
        {
            await Assert.That(() => OrganisationIdHandler.Default.Parse(value: 12345))
                .Throws<DataException>();
        }

        [Test]
        public async Task Parse_WithInvalidString_ThrowsInvalidIdNumberException()
        {
            await Assert.That(() => OrganisationIdHandler.Default.Parse(value: "not an organisation number"))
                .Throws<InvalidIdNumberException>();
        }
    }

    public class EncodeRegression
    {
        // Regression guard for the naive implementation that uses id.LongFormat()
        // unconditionally — that would drop the century for Enskild firma and
        // OrganisationId.Parse would fail to re-parse the encoded value.
        // See spec D2.
        [Test]
        public async Task Encode_EnskildFirma_Returns12DigitFormPreservingCentury()
        {
            var id = OrganisationId.Parse(EnskildFirma12);
            var encoded = OrganisationIdHandler.Encode(id);

            await Assert.That(encoded).IsEqualTo(EnskildFirma12);
            await Assert.That(encoded.Length).IsEqualTo(12);
        }

        [Test]
        public async Task Encode_LegalPerson_Returns10DigitForm()
        {
            var id = OrganisationId.Parse(LegalAktiebolag);
            var encoded = OrganisationIdHandler.Encode(id);

            await Assert.That(encoded).IsEqualTo(LegalAktiebolag);
            await Assert.That(encoded.Length).IsEqualTo(10);
        }

        [Test]
        public async Task Encode_RoundTripsThroughOrganisationIdParse_LegalPerson()
        {
            var original = OrganisationId.Parse(LegalAktiebolag);
            var roundTripped = OrganisationId.Parse(OrganisationIdHandler.Encode(original));

            await Assert.That(roundTripped).IsEqualTo(original);
        }

        [Test]
        public async Task Encode_RoundTripsThroughOrganisationIdParse_EnskildFirma()
        {
            var original = OrganisationId.Parse(EnskildFirma12);
            var roundTripped = OrganisationId.Parse(OrganisationIdHandler.Encode(original));

            await Assert.That(roundTripped).IsEqualTo(original);
        }

        // Verifies PeOrgNr "16NNNNNNNNNN" input is NOT reintroduced on output.
        [Test]
        public async Task Encode_PeOrgNrPrefixedInput_StripsLegalPersonPrefix()
        {
            var id = OrganisationId.Parse("16" + LegalAktiebolag);
            var encoded = OrganisationIdHandler.Encode(id);

            await Assert.That(encoded).IsEqualTo(LegalAktiebolag);
        }
    }

    public class DefaultSingleton
    {
        [Test]
        public async Task Default_ReturnsSameInstanceAcrossAccesses()
        {
            var a = OrganisationIdHandler.Default;
            var b = OrganisationIdHandler.Default;

            await Assert.That(ReferenceEquals(a, b)).IsTrue();
        }
    }
}
