using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography;
using Civitas.Id.Sweden.Core;
using Civitas.Id.Sweden.Fakers.Internal;
using Civitas.Id.Sweden.Internal;
using JetBrains.Annotations;

namespace Civitas.Id.Sweden.Fakers;

/// <summary>
///     Algorithmic faker for Swedish samordningsnummer (coordination IDs).
/// </summary>
/// <remarks>
///     <inheritdoc cref="IIdFaker{T}" />
/// </remarks>
[PublicAPI]
public sealed class CoordinationIdFaker : IPersonIdFaker<CoordinationId>
{
    private readonly Func<int, int, int> _next;

    /// <summary>
    ///     Constructs a faker backed by
    ///     <see cref="RandomNumberGenerator.GetInt32(int, int)" />
    ///     (cryptographically secure; thread-safe; non-deterministic).
    /// </summary>
    public CoordinationIdFaker()
    {
        _next = RandomNumberGenerator.GetInt32;
    }

    /// <summary>Constructs a deterministic faker with the given <paramref name="seed" />.</summary>
    /// <param name="seed">The seed for the underlying <see cref="Random" />.</param>
    /// <remarks>The underlying <see cref="Random" /> instance is single-threaded — NOT safe for concurrent calls.</remarks>
    [SuppressMessage("Security", "CA5394:Do not use insecure randomness",
        Justification =
            "Seeded ctor is intentionally deterministic for test fixture reproducibility; not used for security-sensitive values.")]
    public CoordinationIdFaker(int seed)
    {
        var rng = new Random(seed);
        _next = rng.Next;
    }

    /// <summary>Constructs a faker backed by a caller-supplied <paramref name="random" />.</summary>
    /// <param name="random">The random source.</param>
    /// <exception cref="ArgumentNullException"><paramref name="random" /> is <see langword="null" />.</exception>
    [SuppressMessage("Security", "CA5394:Do not use insecure randomness",
        Justification = "Caller-injected Random is by design for deterministic test fixtures; not security-sensitive.")]
    public CoordinationIdFaker(Random random)
    {
        ArgumentNullException.ThrowIfNull(random);
        _next = random.Next;
    }

    /// <summary>The default thread-safe non-deterministic faker (uses <see cref="RandomNumberGenerator" />).</summary>
    public static CoordinationIdFaker Default { get; } = new();

    /// <inheritdoc />
    public string CountryCode => "SE";

    /// <inheritdoc />
    public CoordinationId Generate()
    {
        return Generate(Generation.RandomBirthDate(_next));
    }

    /// <inheritdoc />
    public CoordinationId Generate(DateOnly birthDate)
    {
        var serial = Generation.RandomSerial(_next);
        var gender = Generation.RandomGenderDigit(_next, _next(0, 2) == 1);
        var sn = Generation.BuildSamordningsnummer(birthDate, serial, gender);
        return CoordinationId.FromValidated(sn);
    }

    /// <inheritdoc />
    public CoordinationId Generate(int year, int month, int day)
    {
        return Generate(Generation.ParseDateOrThrow(year, month, day));
    }

    /// <inheritdoc />
    public CoordinationId GenerateMale()
    {
        return GenerateMale(Generation.RandomBirthDate(_next));
    }

    /// <inheritdoc />
    public CoordinationId GenerateMale(DateOnly birthDate)
    {
        return GenerateForDate(birthDate, true);
    }

    /// <inheritdoc />
    public CoordinationId GenerateMale(int year, int month, int day)
    {
        return GenerateMale(Generation.ParseDateOrThrow(year, month, day));
    }

    /// <inheritdoc />
    public CoordinationId GenerateFemale()
    {
        return GenerateFemale(Generation.RandomBirthDate(_next));
    }

    /// <inheritdoc />
    public CoordinationId GenerateFemale(DateOnly birthDate)
    {
        return GenerateForDate(birthDate, false);
    }

    /// <inheritdoc />
    public CoordinationId GenerateFemale(int year, int month, int day)
    {
        return GenerateFemale(Generation.ParseDateOrThrow(year, month, day));
    }

    /// <inheritdoc />
    public CoordinationId GenerateCentenarian(TimeProvider? timeProvider = null)
    {
        var today = SwedenClock.Today(timeProvider ?? TimeProvider.System);
        var birth = Generation.RandomCentenarianBirthDate(_next, today);
        return Generate(birth);
    }

    /// <summary>Constructs a deterministic faker with the given seed.</summary>
    /// <param name="seed">The seed for the underlying <see cref="Random" />.</param>
    /// <returns>A deterministic faker.</returns>
    public static CoordinationIdFaker WithSeed(int seed)
    {
        return new CoordinationIdFaker(seed);
    }

    private CoordinationId GenerateForDate(DateOnly birthDate, bool male)
    {
        var serial = Generation.RandomSerial(_next);
        var gender = Generation.RandomGenderDigit(_next, male);
        var sn = Generation.BuildSamordningsnummer(birthDate, serial, gender);
        return CoordinationId.FromValidated(sn);
    }
}
