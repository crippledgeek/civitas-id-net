using JetBrains.Annotations;

namespace Civitas.Id.Time;

/// <summary>
///     A civil-time clock for a specific jurisdiction, anchored to the timezone mandated by
///     that jurisdiction's law. Provides "today" as a civil calendar date.
/// </summary>
/// <remarks>
///     The <see cref="TimeProvider" /> seam injects the UTC instant; the timezone is
///     jurisdiction-fixed and <see cref="TimeProvider.LocalTimeZone" /> is intentionally ignored.
///     Zero external dependencies; NativeAOT-compatible.
/// </remarks>
[PublicAPI]
public sealed class CivilClock
{
    private readonly Lazy<TimeZoneInfo> _lazyZone;

    /// <summary>
    ///     Creates a civil-time clock. <paramref name="ianaId" /> is probed first; on hosts
    ///     without IANA tzdata the <paramref name="windowsId" /> is probed as a fallback.
    /// </summary>
    /// <param name="ianaId">The IANA timezone identifier (e.g. <c>Europe/Stockholm</c>).</param>
    /// <param name="windowsId">The Windows-CLDR fallback identifier (e.g. <c>W. Europe Standard Time</c>).</param>
    /// <exception cref="System.ArgumentException">Thrown when either id is null or empty.</exception>
    public CivilClock(string ianaId, string windowsId)
    {
        ArgumentException.ThrowIfNullOrEmpty(ianaId);
        ArgumentException.ThrowIfNullOrEmpty(windowsId);
        _lazyZone = new Lazy<TimeZoneInfo>(() => Resolve(ianaId, windowsId));
    }

    /// <summary>The legally-mandated civil timezone for this jurisdiction.</summary>
    public TimeZoneInfo TimeZone => _lazyZone.Value;

    /// <summary>Returns today's civil calendar date in this jurisdiction (wall clock).</summary>
    [Pure]
    public DateOnly Today() =>
        DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, TimeZone).DateTime);

    /// <summary>
    ///     Returns today's civil calendar date using <paramref name="timeProvider" /> for the UTC
    ///     instant. The provider's <see cref="TimeProvider.LocalTimeZone" /> is ignored.
    /// </summary>
    /// <param name="timeProvider">Supplies the current UTC instant.</param>
    /// <exception cref="System.ArgumentNullException">Thrown when <paramref name="timeProvider" /> is null.</exception>
    [Pure]
    public DateOnly Today(TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(timeProvider);
        return DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(timeProvider.GetUtcNow(), TimeZone).DateTime);
    }

    /// <summary>
    ///     Two-probe timezone resolver: tries <paramref name="ianaId" />, then
    ///     <paramref name="windowsId" />, then throws. <paramref name="find" /> is an injectable
    ///     seam for testing the fallback/throw branches without altering the host.
    /// </summary>
    /// <exception cref="System.InvalidOperationException">Thrown when neither id resolves.</exception>
    internal static TimeZoneInfo Resolve(string ianaId, string windowsId, Func<string, TimeZoneInfo?>? find = null)
    {
        find ??= static id => TimeZoneInfo.TryFindSystemTimeZoneById(id, out var tz) ? tz : null;
        return find(ianaId)
            ?? find(windowsId)
            ?? throw new InvalidOperationException(
                $"Cannot resolve the '{ianaId}' / '{windowsId}' time zone. Ensure timezone data " +
                "is available at runtime (on Alpine: 'apk add --no-cache tzdata'); " +
                "InvariantGlobalization=true is not supported. " +
                "See https://aka.ms/dotnet-globalization-invariant-mode.");
    }
}
