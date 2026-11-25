using Civitas.Id.Sweden.AspNetCore.OpenApi;
using Civitas.Id.Sweden.Core;
using Microsoft.OpenApi;

namespace Civitas.Id.Sweden.AspNetCore.Tests.OpenApi;

/// <summary>
/// Unit-tests <see cref="SwedishIdSchemaTransformer.ApplyMetadata(OpenApiSchema, Type)"/>
/// directly because <c>OpenApiSchemaTransformerContext</c> has no public
/// constructor in Microsoft.AspNetCore.OpenApi 10.0.x. End-to-end coverage
/// via a <c>WebApplicationFactory</c> host is deferred to Task 3.8.
/// </summary>
public class SwedishIdSchemaTransformerTests
{
    public class ApplyMetadata
    {
        [Test]
        public async Task PersonalId_SetsPersonnummerFormat()
        {
            var schema = new OpenApiSchema();
            SwedishIdSchemaTransformer.ApplyMetadata(schema, typeof(PersonalId));

            await Assert.That(schema.Type).IsEqualTo(JsonSchemaType.String);
            await Assert.That(schema.Format).IsEqualTo("personnummer");
            await Assert.That(schema.Pattern).IsNotNull();
            await Assert.That(schema.Example).IsNotNull();
            await Assert.That(schema.Description).IsNotNull();
        }

        [Test]
        public async Task CoordinationId_SetsSamordningsnummerFormat()
        {
            var schema = new OpenApiSchema();
            SwedishIdSchemaTransformer.ApplyMetadata(schema, typeof(CoordinationId));

            await Assert.That(schema.Type).IsEqualTo(JsonSchemaType.String);
            await Assert.That(schema.Format).IsEqualTo("samordningsnummer");
        }

        [Test]
        public async Task OrganisationId_SetsOrganisationsnummerFormat()
        {
            var schema = new OpenApiSchema();
            SwedishIdSchemaTransformer.ApplyMetadata(schema, typeof(OrganisationId));

            await Assert.That(schema.Type).IsEqualTo(JsonSchemaType.String);
            await Assert.That(schema.Format).IsEqualTo("organisationsnummer");
            await Assert.That(schema.Pattern).IsEqualTo(@"^\d{10}$");
        }

        [Test]
        public async Task SwedishOfficialId_SetsPolymorphicFormat()
        {
            var schema = new OpenApiSchema();
            SwedishIdSchemaTransformer.ApplyMetadata(schema, typeof(SwedishOfficialId));

            await Assert.That(schema.Type).IsEqualTo(JsonSchemaType.String);
            await Assert.That(schema.Format).IsEqualTo("swedish-official-id");
        }

        /// <summary>Each described type with the number its example must be.</summary>
        public static IEnumerable<(Type Type, string Example)> Examples()
        {
            yield return (typeof(PersonalId), OfficialNumbers.Centenarian.Long);
            yield return (typeof(CoordinationId), OfficialNumbers.CentenarianSamordningsnummer.Long);
            yield return (typeof(OrganisationId), LegalPersonNumbers.Aktiebolag.Digits);
            yield return (typeof(SwedishOfficialId), OfficialNumbers.Centenarian.Long);
        }

        [Test]
        [MethodDataSource(nameof(Examples))]
        public async Task Example_IsAPinnedTestNumber(Type type, string example)
        {
            var schema = new OpenApiSchema();
            SwedishIdSchemaTransformer.ApplyMetadata(schema, type);

            await Assert.That(schema.Example!.GetValue<string>()).IsEqualTo(example);
        }

        [Test]
        public async Task UnrelatedType_LeavesSchemaUntouched()
        {
            var schema = new OpenApiSchema();
            SwedishIdSchemaTransformer.ApplyMetadata(schema, typeof(string));

            await Assert.That(schema.Format).IsNull();
            await Assert.That(schema.Pattern).IsNull();
            await Assert.That(schema.Example).IsNull();
        }
    }

    public class Construction
    {
        [Test]
        public async Task DefaultConstructor_DoesNotThrow()
        {
            var sut = new SwedishIdSchemaTransformer();
            await Assert.That(sut).IsNotNull();
        }
    }
}
