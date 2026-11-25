namespace Civitas.Id.Sweden.Tests.Provenance;

public class RelativePathTests
{
    [Test]
    [Arguments("")]
    [Arguments("/rooted")]
    public async Task EmptyOrRooted_Throws(string value)
    {
        await Assert.That(() => new RelativePath(value)).Throws<ArgumentException>();
    }

    [Test]
    [Arguments("a/Test 1890-1899.csv", "a/Test 1890-1899.csv")]
    [Arguments("a/123456-7890.txt", "a/######-####.txt")]
    public async Task ToString_HidesTenDigitsOrMore(string value, string expected)
    {
        ArgumentNullException.ThrowIfNull(value);
        await Assert.That(new RelativePath(value).ToString()).IsEqualTo(expected);
    }
}
