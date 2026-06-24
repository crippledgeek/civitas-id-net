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
    public static TimeZoneInfo TimeZone => Time.Sweden.Stockholm.TimeZone;

    /// <summary>Current calendar date in Sweden's civil timezone.</summary>
    public static DateOnly Today() => Time.Sweden.Stockholm.Today();

    /// <summary>Current calendar date in Sweden's civil timezone, using <paramref name="timeProvider" /> for the instant.</summary>
    /// <exception cref="System.ArgumentNullException">Thrown when <paramref name="timeProvider" /> is null.</exception>
    public static DateOnly Today(TimeProvider timeProvider) => Time.Sweden.Stockholm.Today(timeProvider);
}
