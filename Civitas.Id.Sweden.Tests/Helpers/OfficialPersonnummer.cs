namespace Civitas.Id.Sweden.Tests.Helpers;

/// <summary>A published test personnummer.</summary>
internal sealed record OfficialPersonnummer : OfficialNumber
{
    public OfficialPersonnummer(string digits) : base(digits)
    {
    }

    private protected override int CalendarDay(int encodedDay)
    {
        return encodedDay;
    }
}
