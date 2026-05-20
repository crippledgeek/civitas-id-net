namespace Civitas.Id.Sweden.EntityFrameworkCore;

using Civitas.Id.Sweden.Core;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

/// <summary>
///     EF Core <see cref="ValueConverter{TModel,TProvider}"/> mapping
///     <see cref="OrganisationId"/> to a canonical string form.
/// </summary>
/// <remarks>
///     <para>
///         Storage shape: <c>varchar(12)</c> on SQL Server / Postgres / MySQL,
///         <c>TEXT</c> on SQLite. Column width is intentionally variable:
///         legal-person rows store 10 ASCII digit characters;
///         Enskild firma (physical-person) rows store 12 ASCII digit characters
///         (the full personnummer / samordningsnummer form that preserves century).
///     </para>
///     <para>
///         Encode logic: <c>ToPhysicalPersonId() != null ? ToPhysicalPersonId()!.LongFormat() : LongFormat()</c>.
///         For Enskild firma the 12-digit form is used so that the century is preserved and
///         the value round-trips through <see cref="OrganisationId.Parse(string)"/>.
///         A naive <c>LongFormat()</c>-only encode would produce a 10-digit form that
///         drops the century and cannot be re-parsed.
///         Decodes via <see cref="OrganisationId.Parse(string)"/>, which accepts both
///         10-digit legal-person and 12-digit Enskild firma forms without branching.
///     </para>
///     <para>
///         Stateless and thread-safe — EF Core reuses a single instance across
///         <see cref="Microsoft.EntityFrameworkCore.DbContext"/> instances.
///     </para>
/// </remarks>
[PublicAPI]
public sealed class OrganisationIdConverter : ValueConverter<OrganisationId, string>
{
    /// <summary>Initialises a new instance of the <see cref="OrganisationIdConverter"/> class.</summary>
    public OrganisationIdConverter()
        : base(
            id => id.ToPhysicalPersonId() != null
                ? id.ToPhysicalPersonId()!.LongFormat()
                : id.LongFormat(),
            s => OrganisationId.Parse(s),
            new ConverterMappingHints(size: 12, unicode: false))
    {
    }
}
