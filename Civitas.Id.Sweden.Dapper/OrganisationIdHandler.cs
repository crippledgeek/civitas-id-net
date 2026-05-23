using System.Data;
using Civitas.Id.Sweden.Core;
using Dapper;
using JetBrains.Annotations;

namespace Civitas.Id.Sweden.Dapper;

/// <summary>
///     Dapper <see cref="SqlMapper.TypeHandler{T}"/> mapping <see cref="OrganisationId"/>
///     to a canonical string form. Variable-width: legal-person rows store 10 ASCII
///     digit characters; Enskild firma rows store 12 ASCII digit characters (the full
///     personnummer / samordningsnummer form that preserves century).
/// </summary>
/// <remarks>
///     <para>
///         Encode delegates to <see cref="Encode"/>, which calls
///         <see cref="OrganisationId.ToPhysicalPersonId"/> exactly once per row on Save.
///         For Enskild firma the 12-digit form is used so the century is preserved and
///         the value round-trips through <see cref="OrganisationId.Parse(string)"/>.
///         A naive <c>LongFormat()</c>-only encode would produce a 10-digit form that
///         drops the century and cannot be re-parsed.
///         Decodes via <see cref="OrganisationId.Parse(string)"/>, which accepts both
///         10-digit legal-person and 12-digit Enskild firma forms without branching.
///     </para>
///     <para>
///         Column width: must be at least <c>varchar(12)</c> to fit Enskild firma values.
///     </para>
///     <para>
///         See <see cref="PersonalIdHandler"/> for the parameter-shape rationale.
///     </para>
/// </remarks>
[PublicAPI]
public sealed class OrganisationIdHandler : SqlMapper.TypeHandler<OrganisationId>
{
    /// <summary>Shared singleton instance.</summary>
    public static readonly OrganisationIdHandler Default = new();

    private OrganisationIdHandler() { }

    /// <inheritdoc cref="PersonalIdHandler.SetValue"/>
    public override void SetValue(IDbDataParameter parameter, OrganisationId? value)
    {
        ArgumentNullException.ThrowIfNull(parameter);
        // Type the parameter unconditionally; see PersonalIdHandler.SetValue rationale.
        parameter.DbType = DbType.AnsiString;
        parameter.Size = 12;
        parameter.Value = value is null ? DBNull.Value : Encode(value);
    }

    /// <inheritdoc cref="PersonalIdHandler.Parse"/>
    public override OrganisationId? Parse(object value)
    {
        if (value is not string s)
        {
            throw new DataException(
                $"Cannot convert {value?.GetType().FullName ?? "null"} to {nameof(OrganisationId)}.");
        }
        return OrganisationId.Parse(s);
    }

    /// <summary>
    ///     Encodes an <see cref="OrganisationId"/> to its lossless canonical storage string.
    ///     Calls <see cref="OrganisationId.ToPhysicalPersonId"/> exactly once.
    /// </summary>
    /// <param name="id">The non-null organisation identifier to encode.</param>
    /// <returns>
    ///     12-digit personnummer / samordningsnummer form for Enskild firma;
    ///     10-digit legal-person form otherwise.
    /// </returns>
    [Pure]
    internal static string Encode(OrganisationId id)
    {
        var physical = id.ToPhysicalPersonId();
        return physical is not null ? physical.LongFormat() : id.LongFormat();
    }
}
