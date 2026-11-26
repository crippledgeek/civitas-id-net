namespace Civitas.Id.Sweden.Tests.Helpers;

/// <summary>What a number as a file writes it holds, whatever its prefix and separator.</summary>
internal static class WrittenNumber
{
    private const int OrganisationLength = 10;

    /// <summary>The ASCII digits of the text, in order.</summary>
    public static string Digits(string text)
    {
        return string.Concat(text.Where(char.IsAsciiDigit));
    }

    /// <summary>The organisation number inside the text: its last ten digits.</summary>
    public static string LastTenDigits(string text)
    {
        return Digits(text)[^OrganisationLength..];
    }
}
