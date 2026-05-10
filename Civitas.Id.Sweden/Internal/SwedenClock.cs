namespace Civitas.Id.Sweden.Internal;

/// <summary>
///     Single source of truth for Sweden's civil-time anchor. All zero-argument
///     "today" defaults across the library MUST delegate through this helper.
/// </summary>
/// <remarks>
///     <para>
///         Pinned to the IANA <c>Europe/Stockholm</c> identifier, which composes
///         Förordning (1979:988) om svensk normaltid (UTC+1) and Förordning
///         (2001:127) om sommartid. UTC anchors are legally incorrect for Swedish
///         civil-date computation; see <c>~/.claude/rules/swedish-civil-age-law.md</c>.
///     </para>
///     <para>
///         <b>Runtime requirements:</b> The host must provide either the IANA
///         <c>Europe/Stockholm</c> timezone (Linux/macOS with <c>tzdata</c>; Windows
///         with ICU enabled — default since .NET 6 on Windows 10+), or the Windows
///         <c>W. Europe Standard Time</c> mapping. Containers running with
///         <c>&lt;InvariantGlobalization&gt;true&lt;/InvariantGlobalization&gt;</c> or
///         stripped <c>tzdata</c> are not supported and will produce a clear
///         <see cref="System.InvalidOperationException" /> on first access.
///     </para>
/// </remarks>
internal static class SwedenClock
{
    private static readonly Lazy<TimeZoneInfo> LazyTimeZone = new(ResolveStockholmTimeZone);

    /// <summary>The <c>Europe/Stockholm</c> civil timezone.</summary>
    /// <exception cref="System.InvalidOperationException">
    ///     Thrown when neither the IANA <c>Europe/Stockholm</c> identifier nor
    ///     the Windows <c>W. Europe Standard Time</c> identifier resolves on the
    ///     host. The exception message points to the deployment fix
    ///     (install <c>tzdata</c> on Alpine; do not use invariant globalization).
    /// </exception>
    public static TimeZoneInfo TimeZone => LazyTimeZone.Value;

    /// <summary>
    ///     Returns the current calendar date as observed in Sweden's civil
    ///     timezone, regardless of the host process's local timezone.
    /// </summary>
    public static DateOnly Today()
    {
        return DateOnly.FromDateTime(
            TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, TimeZone).DateTime);
    }

    /// <summary>
    ///     Returns the current calendar date in Sweden's civil timezone, using
    ///     <paramref name="timeProvider" /> for the UTC instant. The provider's
    ///     <see cref="TimeProvider.LocalTimeZone" /> is intentionally ignored —
    ///     the timezone is library-fixed.
    /// </summary>
    /// <exception cref="System.ArgumentNullException">
    ///     Thrown when <paramref name="timeProvider" /> is <see langword="null" />.
    /// </exception>
    public static DateOnly Today(TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(timeProvider);
        return DateOnly.FromDateTime(
            TimeZoneInfo.ConvertTime(timeProvider.GetUtcNow(), TimeZone).DateTime);
    }

    private static TimeZoneInfo ResolveStockholmTimeZone()
    {
        // Probe 1: IANA — works on Linux/macOS with tzdata, and on Windows
        // with ICU (default since .NET 6 on Win10+).
        if (TimeZoneInfo.TryFindSystemTimeZoneById("Europe/Stockholm", out var tz)) return tz;

        // Probe 2: Windows registry ID. CLDR maps "W. Europe Standard Time"
        // to Europe/Stockholm (alongside other UTC+1 EU zones).
        if (TimeZoneInfo.TryFindSystemTimeZoneById("W. Europe Standard Time", out tz)) return tz;

        // No fixed-offset fallback — Stockholm observes UTC+1 (CET) winter
        // and UTC+2 (CEST) summer per Förordning (2001:127). Any fixed
        // offset would silently produce wrong civil dates for ~6 months/year.
        throw new InvalidOperationException(
            "Cannot resolve the Europe/Stockholm time zone. Ensure timezone " +
            "data is available at runtime: on Alpine Linux, install the " +
            "'tzdata' package ('apk add --no-cache tzdata'); containers with " +
            "InvariantGlobalization=true or NLS-only Windows are not supported. " +
            "See https://aka.ms/dotnet-globalization-invariant-mode for details.");
    }
}
