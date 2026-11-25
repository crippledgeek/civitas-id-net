using System.Globalization;

namespace Civitas.Id.Sweden.Tests.Helpers;

/// <summary>Makes a number invalid by its check digit alone.</summary>
internal static class CheckDigits
{
    /// <summary>The digits with the last one raised by one, nine becoming zero.</summary>
    public static string Replaced(string digits)
    {
        return digits[..^1] + ((digits[^1] - '0' + 1) % 10).ToString(CultureInfo.InvariantCulture);
    }
}
