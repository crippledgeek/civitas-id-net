namespace Civitas.Id.Sweden.Tests.Helpers;

/// <summary>
///     Assertions that do not print the values they compare, for numbers that are generated or that read as another
///     person's without their century.
/// </summary>
internal static class UnprintedAssert
{
    /// <summary>Fails when the values differ.</summary>
    public static async Task AreEqual<T>(T actual, T expected)
    {
        var equal = EqualityComparer<T>.Default.Equals(actual, expected);
        await Assert.That(equal).IsTrue();
    }

    /// <summary>Fails when the text's inferred separator is not <paramref name="separator" />, or when it has both.</summary>
    public static async Task HasSeparator(string text, char separator)
    {
        var separators = string.Concat(text.Where(c => c is '-' or '+'));
        await Assert.That(separators).IsEqualTo(separator.ToString());
    }
}
