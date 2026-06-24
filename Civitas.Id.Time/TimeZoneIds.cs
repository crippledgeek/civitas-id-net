using JetBrains.Annotations;

namespace Civitas.Id.Time;

/// <summary>
///     The pair of platform timezone identifiers for a single civil zone: the primary IANA id and
///     the Windows-CLDR fallback id probed when IANA tzdata is unavailable at runtime.
/// </summary>
/// <remarks>
///     Both identifiers appear verbatim in the <see cref="System.InvalidOperationException" /> that
///     <see cref="CivilClock" /> throws when neither resolves. Treat them as non-sensitive: never
///     pass personal or secret data as a timezone identifier.
/// </remarks>
[PublicAPI]
public sealed record TimeZoneIds
{
    /// <summary>Creates a validated IANA/Windows identifier pair.</summary>
    /// <param name="iana">The IANA timezone identifier (e.g. <c>Europe/Stockholm</c>).</param>
    /// <param name="windows">The Windows-CLDR fallback identifier (e.g. <c>W. Europe Standard Time</c>).</param>
    /// <exception cref="System.ArgumentException">Thrown when either identifier is null or empty.</exception>
    public TimeZoneIds(string iana, string windows)
    {
        ArgumentException.ThrowIfNullOrEmpty(iana);
        ArgumentException.ThrowIfNullOrEmpty(windows);
        Iana = iana;
        Windows = windows;
    }

    /// <summary>The primary IANA timezone identifier (e.g. <c>Europe/Stockholm</c>), probed first.</summary>
    public string Iana { get; }

    /// <summary>The Windows-CLDR fallback identifier (e.g. <c>W. Europe Standard Time</c>), probed second.</summary>
    public string Windows { get; }
}
