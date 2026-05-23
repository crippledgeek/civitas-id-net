using System.Data;
using Civitas.Id.Sweden.Core;
using Dapper;
using JetBrains.Annotations;

namespace Civitas.Id.Sweden.Dapper;

/// <summary>
///     Dapper <see cref="SqlMapper.TypeHandler{T}"/> mapping <see cref="PersonalId"/>
///     to a 12-digit canonical string. Stored as <c>varchar(12)</c> on SQL Server /
///     Postgres / MySQL, <c>TEXT</c> on SQLite.
/// </summary>
/// <remarks>
///     <para>
///         Storage shape: <see cref="DbType.AnsiString"/>, <see cref="IDbDataParameter.Size"/> = 12.
///         Setting both in <see cref="SetValue"/> prevents the SQL Server
///         <c>nvarchar</c> implicit-conversion footgun that defeats index seeks
///         (Dapper does NOT override <see cref="DbType"/> / <see cref="IDbDataParameter.Size"/>
///         post-handler for custom types — only for <see cref="string"/>-typed properties;
///         see Dapper issue #2141).
///     </para>
///     <para>
///         Stateless and thread-safe — register the singleton <see cref="Default"/>
///         via <c>CivitasIdSwedenDapperSetup.Register</c>.
///     </para>
/// </remarks>
[PublicAPI]
public sealed class PersonalIdHandler : SqlMapper.TypeHandler<PersonalId>
{
    /// <summary>Shared singleton instance; allocated once per process.</summary>
    public static readonly PersonalIdHandler Default = new();

    private PersonalIdHandler() { }

    /// <summary>
    ///     Writes a <see cref="PersonalId"/> to a parameter as <see cref="DbType.AnsiString"/>
    ///     of length 12. Null values are written as <see cref="DBNull.Value"/>.
    /// </summary>
    /// <param name="parameter">The Dapper-supplied parameter.</param>
    /// <param name="value">The value to write, or <see langword="null"/> for SQL NULL.</param>
    /// <exception cref="ArgumentNullException">
    ///     Thrown when <paramref name="parameter"/> is <see langword="null"/>.
    /// </exception>
    public override void SetValue(IDbDataParameter parameter, PersonalId? value)
    {
        ArgumentNullException.ThrowIfNull(parameter);
        if (value is null)
        {
            parameter.Value = DBNull.Value;
            return;
        }
        parameter.DbType = DbType.AnsiString;
        parameter.Size = 12;
        parameter.Value = value.LongFormat();
    }

    /// <summary>
    ///     Decodes a column value into a <see cref="PersonalId"/>.
    /// </summary>
    /// <remarks>
    ///     The Dapper base-class bridge short-circuits <see cref="DBNull"/> before dispatching
    ///     here, so this method never sees <see cref="DBNull"/>. A non-<see cref="string"/>
    ///     <paramref name="value"/> is exceptional (provider/column mismatch) and produces
    ///     a <see cref="DataException"/> with the unexpected type's name in the message.
    /// </remarks>
    /// <param name="value">The provider-supplied column value; expected to be <see cref="string"/>.</param>
    /// <returns>The parsed <see cref="PersonalId"/>; never <see langword="null"/> in practice.</returns>
    /// <exception cref="DataException">
    ///     Thrown when <paramref name="value"/> is not a <see cref="string"/>.
    /// </exception>
    // Override returns nullable T? to match Dapper 2.x's base signature
    // (public abstract T? Parse(object value)) — avoids CS8765 under NRT.
    // In practice we never return null: we throw on bad input.
    public override PersonalId? Parse(object value)
    {
        if (value is not string s)
        {
            throw new DataException(
                $"Cannot convert {value?.GetType().FullName ?? "null"} to {nameof(PersonalId)}.");
        }
        return PersonalId.Parse(s);
    }
}
