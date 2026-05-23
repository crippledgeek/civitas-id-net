namespace Civitas.Id.Sweden.Dapper.Tests.Handlers;

using System.Data;
using Civitas.Id.Sweden.Core;
using Civitas.Id.Sweden.Dapper;
using Civitas.Id.Sweden.Errors;
using Microsoft.Data.Sqlite;

public class PersonalIdHandlerTests
{
    private const string Valid12 = "189001019802";

    // Use SqliteParameter directly as a concrete IDbDataParameter for testing.
    // No connection needed — the parameter is exercised in isolation.
    // Return type is the interface to mirror how the handler is invoked in production.
#pragma warning disable CA1859
    private static IDbDataParameter NewParameter() => new SqliteParameter();
#pragma warning restore CA1859

    public class SetValueBehavior
    {
        [Test]
        public async Task SetValue_WithValidId_WritesLongFormatString()
        {
            var p = NewParameter();
            PersonalIdHandler.Default.SetValue(p, PersonalId.Parse(Valid12));

            await Assert.That(p.Value).IsEqualTo(Valid12);
        }

        [Test]
        public async Task SetValue_WithValidId_SetsDbTypeAnsiString()
        {
            var p = NewParameter();
            PersonalIdHandler.Default.SetValue(p, PersonalId.Parse(Valid12));

            await Assert.That(p.DbType).IsEqualTo(DbType.AnsiString);
        }

        [Test]
        public async Task SetValue_WithValidId_SetsSizeTo12()
        {
            var p = NewParameter();
            PersonalIdHandler.Default.SetValue(p, PersonalId.Parse(Valid12));

            await Assert.That(p.Size).IsEqualTo(12);
        }

        [Test]
        public async Task SetValue_WithNullValue_WritesDbNull()
        {
            var p = NewParameter();
            PersonalIdHandler.Default.SetValue(p, value: null);

            await Assert.That(p.Value).IsEqualTo(DBNull.Value);
        }

        [Test]
        public async Task SetValue_WithNullParameter_ThrowsArgumentNullException()
        {
            await Assert.That(() =>
                PersonalIdHandler.Default.SetValue(parameter: null!, value: PersonalId.Parse(Valid12)))
                .Throws<ArgumentNullException>();
        }
    }

    public class ParseBehavior
    {
        [Test]
        public async Task Parse_WithValidString_ReturnsEquivalentValue()
        {
            var parsed = PersonalIdHandler.Default.Parse(Valid12);

            await Assert.That(parsed).IsEqualTo(PersonalId.Parse(Valid12));
        }

        [Test]
        public async Task Parse_WithNonStringObject_ThrowsDataException()
        {
            await Assert.That(() => PersonalIdHandler.Default.Parse(value: 12345))
                .Throws<DataException>();
        }

        [Test]
        public async Task Parse_WithInvalidString_ThrowsInvalidIdNumberException()
        {
            await Assert.That(() => PersonalIdHandler.Default.Parse(value: "not a personnummer"))
                .Throws<InvalidIdNumberException>();
        }
    }

    public class DefaultSingleton
    {
        [Test]
        public async Task Default_ReturnsSameInstanceAcrossAccesses()
        {
            var a = PersonalIdHandler.Default;
            var b = PersonalIdHandler.Default;

            await Assert.That(ReferenceEquals(a, b)).IsTrue();
        }
    }
}
