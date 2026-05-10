using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Security.Cryptography;
using Civitas.Id.Sweden.Core;
using Civitas.Id.Sweden.Fakers.Internal;
using Civitas.Id.Sweden.Format;
using Civitas.Id.Sweden.Internal;
using JetBrains.Annotations;

namespace Civitas.Id.Sweden.Fakers;

/// <summary>
///     Algorithmic faker for Swedish organisationsnummer.
/// </summary>
/// <remarks>
///     <inheritdoc cref="IIdFaker{T}" />
/// </remarks>
[PublicAPI]
public sealed class OrganisationIdFaker : IOrganisationIdFaker
{
    private static readonly OrganisationForm[] NamedForms =
        [.. Enum.GetValues<OrganisationForm>().Where(f => f != OrganisationForm.None)];

    private readonly CoordinationIdFaker _coordinationIdFaker;
    private readonly Func<int, int, int> _next;
    private readonly PersonalIdFaker _personalIdFaker;

    /// <summary>
    ///     Constructs a faker backed by
    ///     <see cref="RandomNumberGenerator.GetInt32(int, int)" />
    ///     (cryptographically secure; thread-safe; non-deterministic).
    /// </summary>
    public OrganisationIdFaker()
    {
        _personalIdFaker = new PersonalIdFaker();
        _coordinationIdFaker = new CoordinationIdFaker();
        _next = RandomNumberGenerator.GetInt32;
    }

    /// <summary>Constructs a deterministic faker with the given <paramref name="seed" />.</summary>
    /// <param name="seed">The seed for the underlying <see cref="Random" />.</param>
    /// <remarks>The underlying <see cref="Random" /> instance is single-threaded — NOT safe for concurrent calls.</remarks>
    [SuppressMessage("Security", "CA5394:Do not use insecure randomness",
        Justification =
            "Seeded ctor is intentionally deterministic for test fixture reproducibility; not used for security-sensitive values.")]
    public OrganisationIdFaker(int seed) : this(new Random(seed))
    {
    }

    /// <summary>Constructs a faker backed by a caller-supplied <paramref name="random" />.</summary>
    /// <param name="random">The random source.</param>
    /// <exception cref="ArgumentNullException"><paramref name="random" /> is <see langword="null" />.</exception>
    [SuppressMessage("Security", "CA5394:Do not use insecure randomness",
        Justification = "Caller-injected Random is by design for deterministic test fixtures; not security-sensitive.")]
    public OrganisationIdFaker(Random random)
    {
        ArgumentNullException.ThrowIfNull(random);
        _next = random.Next;
        _personalIdFaker = new PersonalIdFaker(random);
        _coordinationIdFaker = new CoordinationIdFaker(random);
    }

    /// <summary>The default thread-safe non-deterministic faker (uses <see cref="RandomNumberGenerator" />).</summary>
    public static OrganisationIdFaker Default { get; } = new();

    /// <inheritdoc />
    public string CountryCode => "SE";

    /// <inheritdoc />
    public OrganisationId Generate()
    {
        return _next(0, 2) == 0 ? GenerateLegalPerson() : GeneratePhysicalPerson();
    }

    /// <inheritdoc />
    public OrganisationId Generate(DateOnly birthDate)
    {
        return GenerateLegalPersonForRegistration(birthDate);
    }

    /// <inheritdoc />
    public OrganisationId Generate(int year, int month, int day)
    {
        return GenerateLegalPersonForRegistration(Generation.ParseDateOrThrow(year, month, day));
    }

    /// <inheritdoc />
    public OrganisationId GenerateLegalPerson()
    {
        var form = NamedForms[_next(0, NamedForms.Length)];
        return Generate(form);
    }

    /// <inheritdoc />
    public OrganisationId GeneratePhysicalPerson()
    {
        // Enskild firma: random PersonalId or CoordinationId, converted via OrganisationId.FromPhysicalPersonId.
        PhysicalPersonId bearer = _next(0, 2) == 0
            ? _personalIdFaker.Generate()
            : _coordinationIdFaker.Generate();
        var twelve = bearer.LongFormat();
        var century = int.Parse(twelve.AsSpan(0, 2), CultureInfo.InvariantCulture);
        var ten = twelve[2..]; // strip century to get 10-digit form
        return OrganisationId.FromPhysicalPersonId(ten, century);
    }

    /// <summary>Constructs a deterministic faker with the given seed.</summary>
    /// <param name="seed">The seed for the underlying <see cref="Random" />.</param>
    /// <returns>A deterministic faker.</returns>
    public static OrganisationIdFaker WithSeed(int seed)
    {
        return new OrganisationIdFaker(seed);
    }

    /// <summary>
    ///     Generates an <see cref="OrganisationId" /> with the specified legal
    ///     <paramref name="form" />.
    /// </summary>
    /// <param name="form">The legal form to encode at positions 1–2 of the orgnummer.</param>
    /// <returns>A valid orgnummer with the specified form.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    ///     Thrown when <paramref name="form" /> is <see cref="OrganisationForm.None" />
    ///     (Enskild firma is not a legal-person form). Use
    ///     <see cref="GeneratePhysicalPerson" /> instead.
    /// </exception>
    public OrganisationId Generate(OrganisationForm form)
    {
        if (form == OrganisationForm.None)
            throw new ArgumentOutOfRangeException(nameof(form),
                "Use GeneratePhysicalPerson() for Enskild firma (OrganisationForm.None).");

        // Build a 10-digit body: form code (2) + encoded month (2) + year (2) + serial (3) + Luhn (1).
        // Per the regex layout (year-month-day-unique), positions 2-3 of the 10-digit form
        // are the "month" group; OrganisationId.TryParse requires this group to be >= 20
        // to identify legal-person orgnummer. Encoded month is randomised in [21, 32].
        var formCode = (int)form; // 2-digit Bolagsverket code
        var encodedMonth = _next(1, 13) + 20; // 21–32, sibling-parity encoding
        var year = _next(0, 100);
        var serial = _next(0, 1000);

        var body9 =
            $"{formCode:D2}" +
            $"{encodedMonth:D2}{year:D2}" +
            $"{serial:D3}"; // 9 digits: form(2) + encodedMonth(2) + year(2) + serial(3)
        var check = SwedishLuhnAlgorithm.ComputeCheckDigit(body9);
        var ten = $"{body9}{check}";
        return OrganisationId.FromValidated(ten);
    }

    /// <summary>
    ///     Generates an <see cref="OrganisationId" /> with the specified
    ///     <paramref name="type" />.
    /// </summary>
    /// <param name="type">Whether to generate a legal-person or physical-person orgnummer.</param>
    /// <returns>A valid orgnummer of the specified type.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    ///     Thrown when <paramref name="type" /> is not a defined <see cref="OrganisationNumberType" />.
    /// </exception>
    public OrganisationId Generate(OrganisationNumberType type)
    {
        return type switch
        {
            OrganisationNumberType.LegalPerson => GenerateLegalPerson(),
            OrganisationNumberType.PhysicalPerson => GeneratePhysicalPerson(),
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
        };
    }

    private OrganisationId GenerateLegalPersonForRegistration(DateOnly registrationDate)
    {
        var form = NamedForms[_next(0, NamedForms.Length)];
        var formCode = (int)form;
        var encodedMonth = registrationDate.Month + 20; // sibling-parity: 21–32
        var year2 = registrationDate.Year % 100;
        var serial = _next(0, 1000);
        var body9 =
            $"{formCode:D2}" +
            $"{encodedMonth:D2}{year2:D2}" +
            $"{serial:D3}";
        var check = SwedishLuhnAlgorithm.ComputeCheckDigit(body9);
        return OrganisationId.FromValidated($"{body9}{check}");
    }
}
