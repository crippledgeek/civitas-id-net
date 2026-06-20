using Dapper;
using JetBrains.Annotations;

namespace Civitas.Id.Sweden.Dapper;

/// <summary>
///     One-call registration of all three Civitas.Id.Sweden Dapper type handlers.
/// </summary>
[PublicAPI]
public static class CivitasIdSwedenDapperSetup
{
    /// <summary>
    ///     Registers <see cref="PersonalIdHandler"/>, <see cref="CoordinationIdHandler"/>,
    ///     and <see cref="OrganisationIdHandler"/> with Dapper's global
    ///     <see cref="SqlMapper"/>. Call once at application startup, BEFORE any
    ///     Dapper query executes.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Dapper caches IL deserializers at first-query time per type
    ///         (Dapper issue #885). Handlers registered AFTER the first query for an
    ///         affected type will be silently bypassed by the cached deserializer.
    ///     </para>
    ///     <para>
    ///         <see cref="SqlMapper.AddTypeHandler{T}(SqlMapper.TypeHandler{T})"/>
    ///         was made thread-safe in a Dapper 2.x release (issue #1815 / PR #2107),
    ///         but concurrent startup registration remains a code smell. Call this
    ///         method from a single thread at application startup.
    ///     </para>
    /// </remarks>
    public static void Register()
    {
        SqlMapper.AddTypeHandler(PersonalIdHandler.Default);
        SqlMapper.AddTypeHandler(CoordinationIdHandler.Default);
        SqlMapper.AddTypeHandler(OrganisationIdHandler.Default);
    }
}
