using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography;
using Civitas.Id.Sweden.Core;
using Civitas.Id.Sweden.Fakers.Internal;
using Civitas.Id.Sweden.Internal;
using JetBrains.Annotations;

namespace Civitas.Id.Sweden.Fakers;

/// <summary>
///     Algorithmic faker for Swedish personnummer.
/// </summary>
/// <remarks>
///     <inheritdoc cref="IIdFaker{T}" />
/// </remarks>
[PublicAPI]
public sealed class PersonalIdFaker : IPersonIdFaker<PersonalId>
{
    private readonly PersonIdGenerator _generator;
    private readonly Func<int, int, int> _next;

    /// <summary>
    ///     Constructs a faker backed by
    ///     <see cref="RandomNumberGenerator.GetInt32(int, int)" />
    ///     (cryptographically secure; thread-safe; non-deterministic).
    /// </summary>
    public PersonalIdFaker() : this(RandomNumberGenerator.GetInt32)
    {
    }

    /// <summary>Constructs a deterministic faker with the given <paramref name="seed" />.</summary>
    /// <param name="seed">The seed for the underlying <see cref="Random" />.</param>
    /// <remarks>The underlying <see cref="Random" /> instance is single-threaded — NOT safe for concurrent calls.</remarks>
    [SuppressMessage("Security", "CA5394:Do not use insecure randomness",
        Justification =
            "Seeded ctor is intentionally deterministic for test fixture reproducibility; not used for security-sensitive values.")]
    public PersonalIdFaker(int seed) : this(new Random(seed))
    {
    }

    /// <summary>Constructs a faker backed by a caller-supplied <paramref name="random" />.</summary>
    /// <param name="random">The random source.</param>
    /// <exception cref="ArgumentNullException"><paramref name="random" /> is <see langword="null" />.</exception>
    [SuppressMessage("Security", "CA5394:Do not use insecure randomness",
        Justification = "Caller-injected Random is by design for deterministic test fixtures; not security-sensitive.")]
    public PersonalIdFaker(Random random)
        : this((random ?? throw new ArgumentNullException(nameof(random))).Next)
    {
    }

    private PersonalIdFaker(Func<int, int, int> next)
    {
        _next = next;
        _generator = new PersonIdGenerator(Generation.DraftPersonnummer, DefectCatalog.Personnummer, next);
    }

    /// <summary>The default thread-safe non-deterministic faker (uses <see cref="RandomNumberGenerator" />).</summary>
    public static PersonalIdFaker Default { get; } = new();

    /// <summary>The defects a number of this kind can have, in a stable order.</summary>
    internal IReadOnlyList<IdDefect> SupportedDefects { get; } = DefectCatalog.Personnummer.Supported;

    /// <inheritdoc />
    public string CountryCode => "SE";

    /// <inheritdoc />
    public PersonalId Generate()
    {
        return Generate(Generation.RandomBirthDate(_next));
    }

    /// <inheritdoc />
    public PersonalId Generate(DateOnly birthDate)
    {
        return GenerateFor(birthDate, null);
    }

    /// <inheritdoc />
    public PersonalId Generate(int year, int month, int day)
    {
        return Generate(Generation.ParseBirthDateOrThrow(year, month, day));
    }

    /// <inheritdoc />
    public PersonalId GenerateMale()
    {
        return GenerateMale(Generation.RandomBirthDate(_next));
    }

    /// <inheritdoc />
    public PersonalId GenerateMale(DateOnly birthDate)
    {
        return GenerateFor(birthDate, IdGender.Male);
    }

    /// <inheritdoc />
    public PersonalId GenerateMale(int year, int month, int day)
    {
        return GenerateMale(Generation.ParseBirthDateOrThrow(year, month, day));
    }

    /// <inheritdoc />
    public PersonalId GenerateFemale()
    {
        return GenerateFemale(Generation.RandomBirthDate(_next));
    }

    /// <inheritdoc />
    public PersonalId GenerateFemale(DateOnly birthDate)
    {
        return GenerateFor(birthDate, IdGender.Female);
    }

    /// <inheritdoc />
    public PersonalId GenerateFemale(int year, int month, int day)
    {
        return GenerateFemale(Generation.ParseBirthDateOrThrow(year, month, day));
    }

    /// <inheritdoc />
    public PersonalId GenerateCentenarian(TimeProvider? timeProvider = null)
    {
        var today = SwedenClock.Today(timeProvider ?? TimeProvider.System);
        var birth = Generation.RandomCentenarianBirthDate(_next, today);
        return Generate(birth);
    }

    /// <summary>Constructs a deterministic faker with the given seed.</summary>
    /// <param name="seed">The seed for the underlying <see cref="Random" />.</param>
    /// <returns>A deterministic faker.</returns>
    public static PersonalIdFaker WithSeed(int seed)
    {
        return new PersonalIdFaker(seed);
    }

    /// <inheritdoc cref="PersonIdGenerator.Generate(IdValidity, DateOnly?, IdGender?)" />
    internal GeneratedId Generate(IdValidity validity, DateOnly? birthDate = null, IdGender? gender = null)
    {
        return _generator.Generate(validity, birthDate, gender);
    }

    /// <inheritdoc cref="PersonIdGenerator.Generate(IdDefect, DateOnly?, IdGender?)" />
    internal InvalidGeneratedId Generate(IdDefect defect, DateOnly? birthDate = null, IdGender? gender = null)
    {
        return _generator.Generate(defect, birthDate, gender);
    }

    private PersonalId GenerateFor(DateOnly birthDate, IdGender? gender)
    {
        return PersonalId.FromValidated(_generator.Draw(birthDate, gender).Render());
    }
}
