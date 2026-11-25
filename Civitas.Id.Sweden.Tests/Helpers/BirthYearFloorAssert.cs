namespace Civitas.Id.Sweden.Tests.Helpers;

/// <summary>Asserts the first accepted birth year of a reader that throws. A number is never printed.</summary>
internal static class BirthYearFloorAssert
{
    /// <summary>
    ///     Fails unless <paramref name="read" /> takes the accepted numbers and rejects the early ones for the reason
    ///     it gives for a wrong check digit.
    /// </summary>
    public static async Task HoldsAsync(Func<string, object?> read, IReadOnlyList<BirthYearCases> kinds)
    {
        foreach (var cases in kinds)
        {
            await Assert.That(read(cases.Accepted) is not null).IsTrue();

            var reason = Rejections.ReasonOf(() => read(cases.WithWrongCheckDigit));
            await Assert.That(reason).IsNotNull();
            foreach (var text in cases.Early)
                await Assert.That(Rejections.ReasonOf(() => read(text))).IsEqualTo(reason);
        }
    }
}
