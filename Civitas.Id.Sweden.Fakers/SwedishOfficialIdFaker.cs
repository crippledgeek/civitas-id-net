using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography;
using Civitas.Id.Sweden.Core;
using JetBrains.Annotations;

namespace Civitas.Id.Sweden.Fakers;

/// <summary>
///     Composite faker that generates a uniformly-random Swedish official ID
///     of any subtype (personnummer, samordningsnummer, organisationsnummer).
/// </summary>
/// <remarks>
///     <inheritdoc cref="IIdFaker{T}" />
/// </remarks>
[PublicAPI]
public sealed class SwedishOfficialIdFaker
{
    private readonly CoordinationIdFaker _coordinationIdFaker;
    private readonly Func<int, int, int> _next;
    private readonly OrganisationIdFaker _organisationIdFaker;
    private readonly PersonalIdFaker _personalIdFaker;

    /// <summary>
    ///     Constructs a composite faker backed by
    ///     <see cref="RandomNumberGenerator.GetInt32(int, int)" />
    ///     (cryptographically secure; thread-safe; non-deterministic).
    /// </summary>
    public SwedishOfficialIdFaker()
    {
        _personalIdFaker = new PersonalIdFaker();
        _coordinationIdFaker = new CoordinationIdFaker();
        _organisationIdFaker = new OrganisationIdFaker();
        _next = RandomNumberGenerator.GetInt32;
    }

    /// <summary>Constructs a deterministic composite faker with the given <paramref name="seed" />.</summary>
    [SuppressMessage("Security", "CA5394:Do not use insecure randomness",
        Justification =
            "Seeded ctor is intentionally deterministic for test fixture reproducibility; not used for security-sensitive values.")]
    public SwedishOfficialIdFaker(int seed) : this(new Random(seed))
    {
    }

    /// <summary>Constructs a composite faker backed by a caller-supplied <paramref name="random" />.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="random" /> is <see langword="null" />.</exception>
    [SuppressMessage("Security", "CA5394:Do not use insecure randomness",
        Justification = "Caller-injected Random is by design for deterministic test fixtures; not security-sensitive.")]
    public SwedishOfficialIdFaker(Random random)
    {
        ArgumentNullException.ThrowIfNull(random);
        _next = random.Next;
        _personalIdFaker = new PersonalIdFaker(random);
        _coordinationIdFaker = new CoordinationIdFaker(random);
        _organisationIdFaker = new OrganisationIdFaker(random);
    }

    /// <summary>The default thread-safe non-deterministic composite faker.</summary>
    public static SwedishOfficialIdFaker Default { get; } = new();

    /// <summary>The ISO 3166-1 alpha-2 country code. Always <c>"SE"</c>.</summary>
#pragma warning disable CA1822 // Mark members as static — kept instance for parity with IIdFaker<T> implementations.
    public string CountryCode => "SE";
#pragma warning restore CA1822

    /// <summary>Constructs a deterministic composite faker with the given seed.</summary>
    public static SwedishOfficialIdFaker WithSeed(int seed)
    {
        return new SwedishOfficialIdFaker(seed);
    }

    /// <summary>Generates a uniformly-random valid Swedish official ID.</summary>
    public SwedishOfficialId Generate()
    {
        return _next(0, 3) switch
        {
            0 => _personalIdFaker.Generate(),
            1 => _coordinationIdFaker.Generate(),
            _ => _organisationIdFaker.GenerateLegalPerson()
        };
    }

    /// <summary>
    ///     Generates a uniformly-random valid Swedish official ID for the given
    ///     <paramref name="date" />.
    /// </summary>
    public SwedishOfficialId Generate(DateOnly date)
    {
        return _next(0, 3) switch
        {
            0 => _personalIdFaker.Generate(date),
            1 => _coordinationIdFaker.Generate(date),
            _ => _organisationIdFaker.Generate(date)
        };
    }

    /// <summary>Generates a list of <paramref name="count" /> random Swedish official IDs.</summary>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="count" /> is negative.</exception>
    public IReadOnlyList<SwedishOfficialId> Generate(int count)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(count);
        var list = new List<SwedishOfficialId>(count);
        for (var i = 0; i < count; i++) list.Add(Generate());
        return list;
    }
}
