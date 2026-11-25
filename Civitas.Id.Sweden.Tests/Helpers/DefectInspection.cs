using System.Globalization;
using Civitas.Id.Sweden.Fakers.Internal;

namespace Civitas.Id.Sweden.Tests.Helpers;

/// <summary>Reads generated numbers and reports on them. It never repairs or builds one.</summary>
internal static class DefectInspection
{
    private const int DigitsAfterSeparator = 4;

    /// <summary>
    ///     Whether <paramref name="id" /> differs from the valid number it was made from only by its own defect.
    /// </summary>
    public static bool BreaksOnlyItsRule(InvalidGeneratedId id, string valid)
    {
        var text = id.Text;
        return id.Defect switch
        {
            IdDefect.WrongCheckDigit =>
                text.Length == valid.Length && text[..^1] == valid[..^1] && char.IsAsciiDigit(text[^1]) &&
                !IndependentLuhn.IsValid(text),
            IdDefect.TooShort => text.Length == valid.Length - 1 && IsSubsequence(text, valid),
            IdDefect.TooLong =>
                text.Length == valid.Length + 1 && text.All(char.IsAsciiDigit) && IsSubsequence(valid, text),
            IdDefect.NonAsciiDigit => HasOneNonAsciiDigit(text, valid),
            IdDefect.MisplacedSeparator =>
                text.Length == valid.Length + 1 && IsSubsequence(valid, text) &&
                text.IndexOf('-', StringComparison.Ordinal) is >= 0 and var index &&
                index < valid.Length - DigitsAfterSeparator,
            _ => false
        };
    }

    private static bool HasOneNonAsciiDigit(string text, string valid)
    {
        return text.Length == valid.Length && text.Count(c => !char.IsAscii(c)) == 1 &&
               text.Zip(valid).All(pair => CharUnicodeInfo.GetDecimalDigitValue(pair.First) == pair.Second - '0');
    }

    private static bool IsSubsequence(string part, string whole)
    {
        var matched = whole.Aggregate(0, (count, c) => count < part.Length && part[count] == c ? count + 1 : count);
        return matched == part.Length;
    }
}
