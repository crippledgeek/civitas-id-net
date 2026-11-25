using Microsoft.Extensions.Time.Testing;

namespace Civitas.Id.Sweden.Tests.Helpers;

/// <summary>A date and a clock inside a year, so that the time zone cannot move either into another.</summary>
internal static class MidYear
{
    public static DateOnly DateIn(int year)
    {
        return new DateOnly(year, 6, 15);
    }

    public static FakeTimeProvider ClockIn(int year)
    {
        return new FakeTimeProvider(new DateTimeOffset(DateIn(year), new TimeOnly(12, 0), TimeSpan.Zero));
    }
}
