using Civitas.Id.Sweden.Core;

namespace Civitas.Id.Sweden.Tests.Helpers;

public class ParseSignatureTests
{
    private static readonly ParseSignature OnADate = Of(typeof(string), typeof(DateOnly));
    private static readonly ParseSignature OnChars = Of(typeof(ReadOnlySpan<char>), typeof(IFormatProvider));
    private static readonly ParseSignature OnBytes = Of(typeof(ReadOnlySpan<byte>), typeof(IFormatProvider));

    private static ParseSignature Of(params Type[] inputs)
    {
        return ParseSignature.Of<PersonalId>(nameof(PersonalId.TryParse), inputs);
    }

    public class AsText
    {
        [Test]
        public async Task Of_NamesTheTypeTheMethodAndTheInputs()
        {
            await Assert.That(OnADate.ToString()).IsEqualTo("PersonalId.TryParse(String, DateOnly)");
        }

        [Test]
        public async Task Of_GenericInput_NamesItsTypeArguments()
        {
            await Assert.That(OnChars.ToString()).IsEqualTo("PersonalId.TryParse(ReadOnlySpan<Char>, IFormatProvider)");
            await Assert.That(OnBytes.ToString()).IsEqualTo("PersonalId.TryParse(ReadOnlySpan<Byte>, IFormatProvider)");
        }

        [Test]
        public async Task Of_NoInputs_HasEmptyParentheses()
        {
            await Assert.That(ParseSignature.Of<PersonalId>(nameof(PersonalId.Parse)).ToString())
                .IsEqualTo("PersonalId.Parse()");
        }
    }

    public class Equality
    {
        [Test]
        public async Task SameTypeMethodAndInputs_AreEqual()
        {
            await Assert.That(Of(typeof(string), typeof(DateOnly))).IsEqualTo(OnADate);
        }

        [Test]
        public async Task SpansOfDifferentElements_AreNotEqual()
        {
            await Assert.That(OnChars).IsNotEqualTo(OnBytes);
        }

        [Test]
        public async Task OtherType_IsNotEqual()
        {
            await Assert.That(ParseSignature.Of<CoordinationId>(nameof(CoordinationId.TryParse), typeof(string),
                typeof(DateOnly))).IsNotEqualTo(OnADate);
        }
    }

    public class Differences
    {
        private static readonly IReadOnlySet<ParseSignature> Methods = new HashSet<ParseSignature> { OnADate, OnChars };

        [Test]
        public async Task SameSets_HaveNone()
        {
            await Assert.That(ParseSignature.Differences(Methods, new HashSet<ParseSignature> { OnChars, OnADate }))
                .IsEmpty();
        }

        [Test]
        public async Task AMethodNotProbed_IsNamed()
        {
            await Assert.That(ParseSignature.Differences(Methods, new HashSet<ParseSignature> { OnADate }))
                .IsEquivalentTo([$"not probed: {OnChars}"]);
        }

        [Test]
        public async Task AProbeOfNoMethod_IsNamed()
        {
            await Assert.That(ParseSignature.Differences(Methods, new HashSet<ParseSignature> { OnADate, OnChars, OnBytes }))
                .IsEquivalentTo([$"probed but not a parse method: {OnBytes}"]);
        }

        [Test]
        public async Task AnAddedMethodAndARemovedProbe_AreBothNamed()
        {
            var methods = new HashSet<ParseSignature> { OnADate, OnChars, OnBytes };

            await Assert.That(ParseSignature.Differences(methods, new HashSet<ParseSignature> { OnADate }))
                .IsEquivalentTo([$"not probed: {OnChars}", $"not probed: {OnBytes}"]);
        }

        [Test]
        public async Task SetsOfEqualSizeThatDiffer_NameBothSides()
        {
            var methods = new HashSet<ParseSignature> { OnADate, OnBytes };

            await Assert.That(ParseSignature.Differences(methods, Methods))
                .IsEquivalentTo([$"not probed: {OnBytes}", $"probed but not a parse method: {OnChars}"]);
        }
    }
}
