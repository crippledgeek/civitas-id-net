using JetBrains.Annotations;

namespace Civitas.Id.Time;

/// <summary>Civil-time clock for Sweden (single civil zone).</summary>
/// <remarks>Förordning (1979:988) om svensk normaltid + Förordning (2001:127) om sommartid.</remarks>
[PublicAPI]
public static class Sweden
{
    /// <summary>
    ///     Sweden — <c>Europe/Stockholm</c> (CET/CEST). Windows fallback
    ///     <c>W. Europe Standard Time</c> validated against CLDR windowsZones.xml.
    /// </summary>
    public static readonly CivilClock Stockholm = new("Europe/Stockholm", "W. Europe Standard Time");
}
