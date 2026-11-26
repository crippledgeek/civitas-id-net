using Civitas.Id.Sweden.Core;
using Civitas.Id.Sweden.Format;

namespace Civitas.Id.Sweden.Tests.Helpers;

/// <summary>
///     What a published person number decodes to, asserted by reading it on an explicit date. A failure names the
///     entry point, never the number.
/// </summary>
internal static class AcceptedPerson
{
    /// <summary>A published personnummer is taken by <see cref="PersonalId" /> and as a sole trader, and decodes.</summary>
    public static async Task<PersonalId> PersonnummerAsync(OfficialPersonnummer number, DateOnly reference)
    {
        await TakenByEveryEntryPointAsync(number, typeof(PersonalId)).ConfigureAwait(false);
        var parsed = PersonalId.TryParse(number.Long, reference, out var id);
        await Assert.That(parsed).IsTrue();
        ArgumentNullException.ThrowIfNull(id);

        await DecodesAsync(id, number).ConfigureAwait(false);
        await ParsesAnyAsAsync<PersonalId>(number, reference).ConfigureAwait(false);
        await SoleTraderAsync(number, reference).ConfigureAwait(false);
        return id;
    }

    /// <summary>A published samordningsnummer is taken by <see cref="CoordinationId" /> and as a sole trader, and decodes.</summary>
    public static async Task<CoordinationId> SamordningsnummerAsync(OfficialSamordningsnummer number, DateOnly reference)
    {
        await TakenByEveryEntryPointAsync(number, typeof(CoordinationId)).ConfigureAwait(false);
        var parsed = CoordinationId.TryParse(number.Long, reference, out var id);
        await Assert.That(parsed).IsTrue();
        ArgumentNullException.ThrowIfNull(id);

        await DecodesAsync(id, number).ConfigureAwait(false);
        await ParsesAnyAsAsync<CoordinationId>(number, reference).ConfigureAwait(false);
        await SoleTraderAsync(number, reference).ConfigureAwait(false);
        return id;
    }

    /// <summary>The person type, the sole-trader type and <see cref="SwedishOfficialId" /> all take the number, through every entry point.</summary>
    private static async Task TakenByEveryEntryPointAsync(OfficialNumber number, Type person)
    {
        await Assert.That(ExpectedAcceptance.NotAccepted(number.Long, person, typeof(OrganisationId), typeof(SwedishOfficialId)))
            .IsEmpty();
    }

    private static async Task DecodesAsync(PhysicalPersonId id, OfficialNumber number)
    {
        await UnprintedAssert.AreEqual(id.LongFormat(), number.Long).ConfigureAwait(false);
        await UnprintedAssert.AreEqual(id.ShortFormat(), number.Short).ConfigureAwait(false);
        await UnprintedAssert.AreEqual(id.BirthDate, number.BirthDate).ConfigureAwait(false);
        await Assert.That(id.IsFemale).IsEqualTo(number.IsFemale);
        await Assert.That(id.IsMale).IsEqualTo(!number.IsFemale);
    }

    private static async Task ParsesAnyAsAsync<T>(OfficialNumber number, DateOnly reference)
        where T : SwedishOfficialId
    {
        var parsed = SwedishOfficialId.TryParseAny(number.Long, reference, out var any);
        await Assert.That(parsed).IsTrue();
        await Assert.That(any).IsTypeOf<T>();
    }

    private static async Task SoleTraderAsync(OfficialNumber number, DateOnly reference)
    {
        var parsed = OrganisationId.TryParse(number.Long, reference, out var organisation);
        await Assert.That(parsed).IsTrue();
        ArgumentNullException.ThrowIfNull(organisation);

        await Assert.That(organisation.NumberType).IsEqualTo(OrganisationNumberType.PhysicalPerson);
        var person = organisation.ToPhysicalPersonId();
        ArgumentNullException.ThrowIfNull(person);
        await UnprintedAssert.AreEqual(person.LongFormat(), number.Long).ConfigureAwait(false);
    }
}
