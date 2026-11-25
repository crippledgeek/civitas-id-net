namespace Civitas.Id.Sweden.Fakers.Internal;

/// <summary>What the two person fakers share; they differ only in the values they pass in.</summary>
/// <param name="draft">Puts a birth date, a serial and a gender digit into the parts of this kind of number.</param>
/// <param name="defects">The defects this kind of number supports.</param>
/// <param name="next">The random source.</param>
internal sealed class PersonIdGenerator(
    Func<DateOnly, int, int, IdDraft> draft,
    DefectCatalog defects,
    Func<int, int, int> next)
{
    /// <summary>The sides of the coin that decides the gender when none is asked for.</summary>
    private const int CoinSides = 2;

    /// <summary>Draws the parts of a valid number. Seeded output depends on the order of these draws.</summary>
    /// <param name="birthDate">The date of birth.</param>
    /// <param name="gender">The gender, or <see langword="null" /> for a random one.</param>
    /// <returns>The parts.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    ///     <paramref name="gender" /> is not defined, or <paramref name="birthDate" /> is before
    ///     <see cref="Generation.MinimumBirthDate" />.
    /// </exception>
    public IdDraft Draw(DateOnly birthDate, IdGender? gender)
    {
        RequireAccepted(birthDate);
        var serial = Generation.RandomSerial(next);
        var male = gender switch
        {
            IdGender.Male => true,
            IdGender.Female => false,
            null => next(0, CoinSides) == 1,
            _ => throw new ArgumentOutOfRangeException(nameof(gender), gender, null)
        };
        return draft(birthDate, serial, Generation.RandomGenderDigit(next, male));
    }

    /// <summary>Generates a valid number, or one with a random defect that goes together with the date.</summary>
    /// <param name="validity">Whether the number is valid.</param>
    /// <param name="birthDate">The date of birth, or <see langword="null" /> for a random one.</param>
    /// <param name="gender">The gender, or <see langword="null" /> for a random one.</param>
    /// <returns>The number.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    ///     An argument is not defined, or <paramref name="birthDate" /> is before
    ///     <see cref="Generation.MinimumBirthDate" />.
    /// </exception>
    public GeneratedId Generate(IdValidity validity, DateOnly? birthDate, IdGender? gender)
    {
        RequireAccepted(birthDate);
        return validity.Match(
            () => new ValidGeneratedId(DrawFor(birthDate, gender)),
            () => Generate(defects.Pick(birthDate, next), birthDate, gender));
    }

    /// <summary>Generates a number with one defect, keeping every given part that the defect does not replace.</summary>
    /// <param name="defect">The rule to break.</param>
    /// <param name="birthDate">The date of birth, or <see langword="null" /> for a random one.</param>
    /// <param name="gender">The gender, or <see langword="null" /> for a random one.</param>
    /// <returns>The invalid number.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    ///     An argument is not defined or not supported, or <paramref name="birthDate" /> is before
    ///     <see cref="Generation.MinimumBirthDate" />.
    /// </exception>
    /// <exception cref="ArgumentException">The defect cannot be made from <paramref name="birthDate" />.</exception>
    public InvalidGeneratedId Generate(IdDefect defect, DateOnly? birthDate, IdGender? gender)
    {
        RequireAccepted(birthDate);
        return defects.Allows(defect, birthDate)
            ? defects.Corrupt(defect, DrawFor(birthDate, gender), next)
            : throw new ArgumentException($"{defect} cannot be made from this date.", nameof(birthDate));
    }

    /// <summary>Called before anything is drawn or decided, so that an early date always gives the same exception.</summary>
    private static void RequireAccepted(DateOnly? birthDate)
    {
        if (birthDate is { } date)
            ArgumentOutOfRangeException.ThrowIfLessThan(date, Generation.MinimumBirthDate, nameof(birthDate));
    }

    private IdDraft DrawFor(DateOnly? birthDate, IdGender? gender)
    {
        return Draw(birthDate ?? Generation.RandomBirthDate(next), gender);
    }
}
