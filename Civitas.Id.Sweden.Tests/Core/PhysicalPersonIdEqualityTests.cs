using Civitas.Id.Sweden.Core;

namespace Civitas.Id.Sweden.Tests.Core;

/// <summary>
///     Pins record value-equality semantics after lifting <c>_normalised</c>
///     from sealed subtypes to abstract base <see cref="PhysicalPersonId"/>.
///     Roslyn's <c>SynthesizedRecordEquals</c> chains <c>base.Equals(other)</c>;
///     sealed subtypes with no own fields reduce to base equality.
/// </summary>
public class PhysicalPersonIdEqualityTests
{
    // Known-valid pins from Skatteverket fixtures.
    // ValidPersonal: from testpersonnummer_final.csv (body 9001019802 -> check 2, verified).
    // ValidCoordination: Luhn-valid coordination number (body 9001610006 -> check 6, verified).
    private const string ValidPersonal = "189001019802";
    private const string ValidCoordination = "189001610006";

    [Test]
    public async Task PersonalId_ValueEquality_HoldsAcrossInstances()
    {
        var a = PersonalId.Parse(ValidPersonal);
        var b = PersonalId.Parse(ValidPersonal);
        await Assert.That(a).IsEqualTo(b);
        await Assert.That(a.GetHashCode()).IsEqualTo(b.GetHashCode());
        await Assert.That(a == b).IsTrue();
    }

    [Test]
    public async Task CoordinationId_ValueEquality_HoldsAcrossInstances()
    {
        var a = CoordinationId.Parse(ValidCoordination);
        var b = CoordinationId.Parse(ValidCoordination);
        await Assert.That(a).IsEqualTo(b);
        await Assert.That(a.GetHashCode()).IsEqualTo(b.GetHashCode());
    }

    [Test]
    public async Task DifferentSubtypes_AreNotEqual_EvenWithSameNormalisedBody()
    {
        var p = PersonalId.Parse(ValidPersonal);
        var c = CoordinationId.Parse(ValidCoordination);
        // EqualityContract differs across record subtypes.
        // ReSharper disable once SuspiciousTypeConversion.Global
        await Assert.That(((object)p).Equals(c)).IsFalse();
    }

    [Test]
    public async Task ToString_ReturnsCanonicalLongFormat()
    {
        var p = PersonalId.Parse(ValidPersonal);
        await Assert.That(p.ToString()).IsEqualTo(ValidPersonal);

        var c = CoordinationId.Parse(ValidCoordination);
        await Assert.That(c.ToString()).IsEqualTo(ValidCoordination);
    }
}
