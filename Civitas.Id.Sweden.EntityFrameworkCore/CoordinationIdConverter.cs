using Civitas.Id.Sweden.Core;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Civitas.Id.Sweden.EntityFrameworkCore;

/// <summary>
///     EF Core <see cref="ValueConverter{TModel,TProvider}"/> mapping
///     <see cref="CoordinationId"/> to its canonical 12-digit string form.
/// </summary>
/// <remarks>
///     Storage shape: <c>varchar(12)</c> on SQL Server / Postgres / MySQL,
///     <c>TEXT</c> on SQLite. Encodes via <see cref="SwedishOfficialId.LongFormat"/>,
///     decodes via <see cref="CoordinationId.Parse(string)"/>.
///     Stateless and thread-safe — EF Core reuses a single instance across
///     <see cref="Microsoft.EntityFrameworkCore.DbContext"/> instances.
/// </remarks>
[PublicAPI]
public sealed class CoordinationIdConverter : ValueConverter<CoordinationId, string>
{
    /// <summary>Initialises a new instance of the <see cref="CoordinationIdConverter"/> class.</summary>
    public CoordinationIdConverter()
        : base(
            id => id.LongFormat(),
            s => CoordinationId.Parse(s),
            new ConverterMappingHints(size: 12, unicode: false))
    {
    }
}
