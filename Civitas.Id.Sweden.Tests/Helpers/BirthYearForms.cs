using Civitas.Id.Sweden.Fakers.Internal;

namespace Civitas.Id.Sweden.Tests.Helpers;

/// <summary>Further written forms of numbers that differ by their birth year.</summary>
internal static class BirthYearForms
{
    extension(BirthYearCases cases)
    {
        /// <summary>The forms without a century: no separator, hyphen and plus.</summary>
        public IReadOnlyList<string> ShortForms(int twoDigitYear)
        {
            var text = (cases.Draft with { Century = null, Year = twoDigitYear }).Render();
            return [text, Separated(text, "-"), Separated(text, "+")];
        }
    }

    /// <summary>The twelve digits with a separator, a prefix or padding.</summary>
    public static IReadOnlyList<string> DecoratedForms(string text)
    {
        return [Separated(text, "-"), Separated(text, "+"), $"SE{text}", $" {text} "];
    }

    private static string Separated(string text, string separator)
    {
        return text.Insert(text.Length - IdDraft.DigitsAfterSeparator, separator);
    }
}
