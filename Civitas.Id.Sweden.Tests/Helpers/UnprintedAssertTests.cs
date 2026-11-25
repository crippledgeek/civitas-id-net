using TUnit.Assertions.Exceptions;

namespace Civitas.Id.Sweden.Tests.Helpers;

public class UnprintedAssertTests
{
    private const string Actual = "first-value";
    private const string Expected = "second-value";

    [Test]
    public async Task AreEqual_OfEqualValues_Passes()
    {
        var copy = new string(Actual.AsSpan());
        await UnprintedAssert.AreEqual(copy, Actual).ConfigureAwait(false);
    }

    [Test]
    public async Task AreEqual_OfDifferentValues_FailsWithoutPrintingThem()
    {
        var ex = await Assert.That(() => UnprintedAssert.AreEqual(Actual, Expected)).Throws<AssertionException>();
        await Assert.That(ex!.ToString()).DoesNotContain(Actual);
        await Assert.That(ex.ToString()).DoesNotContain(Expected);
    }

    [Test]
    [Arguments("a-b", '-')]
    [Arguments("a+b", '+')]
    public async Task HasSeparator_OfThatOne_Passes(string text, char separator)
    {
        await UnprintedAssert.HasSeparator(text, separator).ConfigureAwait(false);
    }

    [Test]
    [Arguments("value")]
    [Arguments("value+value")]
    [Arguments("value-value+")]
    [Arguments("value-value-")]
    public async Task HasSeparator_OfNoneAnotherOrSeveral_FailsWithoutPrintingTheText(string text)
    {
        var ex = await Assert.That(() => UnprintedAssert.HasSeparator(text, '-')).Throws<AssertionException>();
        await Assert.That(ex!.ToString()).DoesNotContain(text);
    }
}
