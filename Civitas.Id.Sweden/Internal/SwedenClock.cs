using Civitas.Id.Time; // NOT redundant — Civitas.Id.Time is a different root namespace.

namespace Civitas.Id.Sweden.Internal;

/// <summary>
///     Single source of truth for Sweden's civil-time anchor within this package. Delegates to
///     <see cref="Civitas.Id.Time.Sweden.Stockholm" />; retained as the in-package static entry
///     point so existing call sites and the architecture rule
///     (<c>SwedenClock_LivesInInternalNamespace</c>, which requires a class named SwedenClock to
///     reside in this namespace) remain valid.
/// </summary>
internal static class SwedenClock
{
    /// <summary>The <c>Europe/Stockholm</c> civil timezone.</summary>
    public static TimeZoneInfo TimeZone => Civitas.Id.Time.Sweden.Stockholm.TimeZone;

    /// <summary>Current calendar date in Sweden's civil timezone.</summary>
    public static DateOnly Today() => Civitas.Id.Time.Sweden.Stockholm.Today();

    /// <summary>Current calendar date in Sweden's civil timezone, using <paramref name="timeProvider" /> for the instant.</summary>
    /// <exception cref="System.ArgumentNullException">Thrown when <paramref name="timeProvider" /> is null.</exception>
    public static DateOnly Today(TimeProvider timeProvider) => Civitas.Id.Time.Sweden.Stockholm.Today(timeProvider);

    /// <summary>Resolves the Stockholm timezone via the shared two-probe resolver. Test seam.</summary>
    /// <exception cref="System.ArgumentNullException">Thrown when <paramref name="find" /> is null.</exception>
    /// <exception cref="System.InvalidOperationException">Thrown when neither probe id resolves.</exception>
    internal static TimeZoneInfo ResolveStockholmTimeZone(Func<string, TimeZoneInfo?> find)
    {
        ArgumentNullException.ThrowIfNull(find);
        return find("Europe/Stockholm")
            ?? find("W. Europe Standard Time")
            ?? throw new InvalidOperationException(
                "Cannot resolve the Europe/Stockholm time zone. Ensure timezone " +
                "data is available at runtime: on Alpine Linux, install the " +
                "'tzdata' package ('apk add --no-cache tzdata'); containers with " +
                "InvariantGlobalization=true or NLS-only Windows are not supported. " +
                "See https://aka.ms/dotnet-globalization-invariant-mode for details.");
    }

    /// <summary>Default production finder: <see cref="TimeZoneInfo.TryFindSystemTimeZoneById(string, out TimeZoneInfo)" /> → null on miss.</summary>
    internal static TimeZoneInfo? DefaultFind(string id)
        => TimeZoneInfo.TryFindSystemTimeZoneById(id, out var tz) ? tz : null;
}
