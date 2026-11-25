using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Security.Cryptography;
using Civitas.Id.Sweden.Core;
using Civitas.Id.Sweden.Fakers.Internal;
using Civitas.Id.Sweden.Format;
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
    /// <summary>
    ///     Added to the month, because the parser takes a month group of 20 or more for a legal person's number.
    /// </summary>
    private const int LegalPersonMonthOffset = 20;

    private const int MonthsPerYear = 12;

    /// <summary>How many values three serial digits have.</summary>
    private const int SerialCount = 1000;

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

    /// <summary>The defects a legal person's number can have, in a stable order.</summary>
    internal IReadOnlyList<IdDefect> SupportedDefects { get; } = DefectCatalog.LegalPerson.Supported;

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
        return OrganisationId.FromValidated(DrawLegalPerson(DrawForm()).Render());
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

        return OrganisationId.FromValidated(DrawLegalPerson(form).Render());
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

    /// <summary>Generates a legal person's number, valid or with a random defect.</summary>
    /// <param name="validity">Whether the number is valid.</param>
    /// <returns>The number.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="validity" /> is not defined.</exception>
    internal GeneratedId Generate(IdValidity validity)
    {
        return validity.Match(
            () => new ValidGeneratedId(DrawLegalPerson(DrawForm())),
            () => Generate(DefectCatalog.LegalPerson.Pick(null, _next)));
    }

    /// <summary>Generates a legal person's number with one defect.</summary>
    /// <param name="defect">The rule to break.</param>
    /// <returns>The invalid number.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    ///     <paramref name="defect" /> is not defined, or breaks a date, which a legal person's number has none of.
    /// </exception>
    internal InvalidGeneratedId Generate(IdDefect defect)
    {
        return DefectCatalog.LegalPerson.Corrupt(defect, DrawLegalPerson(DrawForm()), _next);
    }

    private OrganisationId GenerateLegalPersonForRegistration(DateOnly registrationDate)
    {
        var draft = DrawSerial(DrawForm(), registrationDate.Month, registrationDate.Year % IdDraft.YearsPerCentury);
        return OrganisationId.FromValidated(draft.Render());
    }

    private OrganisationForm DrawForm()
    {
        return NamedForms[_next(0, NamedForms.Length)];
    }

    /// <summary>Draws month, year and serial. Seeded output depends on the order of these draws.</summary>
    private IdDraft DrawLegalPerson(OrganisationForm form)
    {
        var month = _next(1, MonthsPerYear + 1);
        var year = _next(0, IdDraft.YearsPerCentury);
        return DrawSerial(form, month, year);
    }

    /// <summary>The form digits stand where a person's number has the year, the year where it has the day.</summary>
    private IdDraft DrawSerial(OrganisationForm form, int month, int year)
    {
        return new IdDraft
        {
            Century = null,
            Year = (int)form,
            Month = month + LegalPersonMonthOffset,
            Day = year,
            Serial = _next(0, SerialCount)
        };
    }
}
