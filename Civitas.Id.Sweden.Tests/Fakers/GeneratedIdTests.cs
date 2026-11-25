using System.Reflection;
using Civitas.Id.Sweden.Fakers.Internal;
using Civitas.Id.Sweden.Tests.Helpers;

namespace Civitas.Id.Sweden.Tests.Fakers;

public class GeneratedIdTests
{
    private static readonly IdDraft First = OfficialDrafts.Personnummer[0];
    private static readonly IdDraft Second = OfficialDrafts.Personnummer[1];

    [Test]
    public async Task Valid_SameDraft_AreEqual()
    {
        await Assert.That(new ValidGeneratedId(First)).IsEqualTo(new ValidGeneratedId(First));
        await Assert.That(new ValidGeneratedId(First)).IsNotEqualTo(new ValidGeneratedId(Second));
    }

    [Test]
    public async Task Invalid_SameTextAndDefect_AreEqual()
    {
        var id = new InvalidGeneratedId(First.Render(), IdDefect.TooLong);

        await Assert.That(new InvalidGeneratedId(First.Render(), IdDefect.TooLong)).IsEqualTo(id);
        await Assert.That(new InvalidGeneratedId(First.Render(), IdDefect.TooShort)).IsNotEqualTo(id);
        await Assert.That(new InvalidGeneratedId(Second.Render(), IdDefect.TooLong)).IsNotEqualTo(id);
    }

    [Test]
    public async Task Defect_IsTheBrokenRule()
    {
        await Assert.That(new InvalidGeneratedId(First.Render(), IdDefect.TooLong).Defect)
            .IsEqualTo(IdDefect.TooLong);
    }

    /// <summary>Catalogues list their defects in this order.</summary>
    [Test]
    public async Task Defects_AreTheAgreedOnesInOrder()
    {
        IdDefect[] agreed =
        [
            IdDefect.WrongCheckDigit, IdDefect.ImpossibleMonth, IdDefect.ImpossibleDay, IdDefect.NonLeapFebruary29,
            IdDefect.TooShort, IdDefect.TooLong, IdDefect.NonAsciiDigit, IdDefect.MisplacedSeparator
        ];

        await Assert.That(Enum.GetValues<IdDefect>().SequenceEqual(agreed)).IsTrue();
    }

    [Test]
    public async Task ValidAndInvalid_SameText_AreNotEqual()
    {
        GeneratedId valid = new ValidGeneratedId(First);
        GeneratedId invalid = new InvalidGeneratedId(First.Render(), IdDefect.TooLong);

        await UnprintedAssert.AreEqual(valid.Text, invalid.Text).ConfigureAwait(false);
        await Assert.That(valid).IsNotEqualTo(invalid);
    }

    [Test]
    public async Task ToString_HoldsNoNumber()
    {
        await Assert.That(new ValidGeneratedId(First).ToString()).IsEqualTo("ValidGeneratedId");
        await Assert.That(new InvalidGeneratedId(First.Render(), IdDefect.TooLong).ToString())
            .IsEqualTo("InvalidGeneratedId (TooLong)");
    }

    [Test]
    public async Task Hierarchy_HasExactlyTwoSealedLeaves()
    {
        var leaves = typeof(GeneratedId).Assembly.GetTypes()
            .Where(type => type.BaseType == typeof(GeneratedId)).ToList();

        await Assert.That(leaves).IsEquivalentTo([typeof(ValidGeneratedId), typeof(InvalidGeneratedId)]);
        await Assert.That(leaves.All(type => type.IsSealed)).IsTrue();
    }

    [Test]
    public async Task Hierarchy_IsClosedToOtherAssemblies()
    {
        const BindingFlags instance = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;

        var constructor = typeof(GeneratedId).GetConstructors(instance).Single(c => c.GetParameters().Length == 0);
        var closing = typeof(GeneratedId).GetMethods(instance)
            .Where(method => method is { IsAbstract: true, IsFamilyAndAssembly: true });

        await Assert.That(constructor.IsFamilyAndAssembly).IsTrue();
        await Assert.That(closing).HasSingleItem();
    }
}
