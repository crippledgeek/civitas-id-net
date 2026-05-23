using System.Data;
using Civitas.Id.Sweden.Core;
using Dapper;
using JetBrains.Annotations;

namespace Civitas.Id.Sweden.Dapper;

/// <summary>
///     Dapper <see cref="SqlMapper.TypeHandler{T}"/> mapping <see cref="CoordinationId"/>
///     to a 12-digit canonical string (<c>YYYYMM(DD+60)NNNN</c> per Skatteverket
///     samordningsnummer rules).
/// </summary>
/// <remarks>
///     See <see cref="PersonalIdHandler"/> for the parameter-shape rationale.
/// </remarks>
[PublicAPI]
public sealed class CoordinationIdHandler : SqlMapper.TypeHandler<CoordinationId>
{
    /// <summary>Shared singleton instance; allocated once per process.</summary>
    public static readonly CoordinationIdHandler Default = new();

    private CoordinationIdHandler() { }

    /// <summary>
    ///     Writes a <see cref="CoordinationId"/> to a parameter as <see cref="DbType.AnsiString"/>
    ///     of length 12. Null values are written as <see cref="DBNull.Value"/>.
    /// </summary>
    /// <param name="parameter">The Dapper-supplied parameter.</param>
    /// <param name="value">The value to write, or <see langword="null"/> for SQL NULL.</param>
    /// <exception cref="ArgumentNullException">
    ///     Thrown when <paramref name="parameter"/> is <see langword="null"/>.
    /// </exception>
    public override void SetValue(IDbDataParameter parameter, CoordinationId? value)
    {
        ArgumentNullException.ThrowIfNull(parameter);
        // Type the parameter unconditionally — typed NULLs avoid sql_variant /
        // implicit-conversion plan instability against indexed varchar(12) columns.
        parameter.DbType = DbType.AnsiString;
        parameter.Size = 12;
        parameter.Value = value is null ? DBNull.Value : value.LongFormat();
    }

    /// <summary>
    ///     Decodes a column value into a <see cref="CoordinationId"/>.
    /// </summary>
    /// <remarks>
    ///     The Dapper base-class bridge short-circuits <see cref="DBNull"/> before dispatching
    ///     here, so this method never sees <see cref="DBNull"/>. A non-<see cref="string"/>
    ///     <paramref name="value"/> is exceptional (provider/column mismatch) and produces
    ///     a <see cref="DataException"/> with the unexpected type's name in the message.
    /// </remarks>
    /// <param name="value">The provider-supplied column value; expected to be <see cref="string"/>.</param>
    /// <returns>The parsed <see cref="CoordinationId"/>; never <see langword="null"/> in practice.</returns>
    /// <exception cref="DataException">
    ///     Thrown when <paramref name="value"/> is not a <see cref="string"/>.
    /// </exception>
    // Override returns nullable T? to match Dapper 2.x's base signature
    // (public abstract T? Parse(object value)) — avoids CS8765 under NRT.
    // In practice we never return null: we throw on bad input.
    public override CoordinationId? Parse(object value)
    {
        if (value is not string s)
        {
            throw new DataException(
                $"Cannot convert {value?.GetType().FullName ?? "null"} to {nameof(CoordinationId)}.");
        }
        return CoordinationId.Parse(s);
    }
}
