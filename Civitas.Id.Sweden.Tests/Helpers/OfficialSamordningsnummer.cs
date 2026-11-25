namespace Civitas.Id.Sweden.Tests.Helpers;

/// <summary>A published test samordningsnummer, whose day part is the calendar day plus 60.</summary>
internal sealed record OfficialSamordningsnummer : OfficialNumber
{
    public OfficialSamordningsnummer(string digits) : base(digits)
    {
    }

    private const int DayOffset = 60;

    private protected override int CalendarDay(int encodedDay)
    {
        return encodedDay - DayOffset;
    }
}
