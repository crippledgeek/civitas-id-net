namespace Civitas.Id.Sweden.Tests.Helpers;

/// <summary>A published test personnummer.</summary>
internal sealed record OfficialPersonnummer(string Digits) : OfficialNumber(Digits)
{
    private protected override int CalendarDay(int encodedDay)
    {
        return encodedDay;
    }
}
