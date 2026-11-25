using Civitas.Id.Sweden.Internal;

namespace Civitas.Id.Sweden.Fakers.Internal;

/// <summary>
///     A number generated as text, valid or deliberately invalid. Exactly two types derive from it, and the
///     private protected abstract member keeps other assemblies from adding one: the constructor alone does
///     not, because the copy constructor of an abstract record is protected.
/// </summary>
internal abstract record GeneratedId
{
    private protected GeneratedId()
    {
    }

    /// <summary>The number. A plain string, because an invalid number has no valid type to be.</summary>
    public abstract string Text { get; }

    /// <summary>What <see cref="ToString" /> returns. Never the number, which may belong to someone.</summary>
    private protected abstract string Describe();

    /// <inheritdoc />
    public sealed override string ToString()
    {
        return Describe();
    }
}

/// <summary>A valid number.</summary>
internal sealed record ValidGeneratedId : GeneratedId
{
    private readonly IdDraft _draft;

    /// <summary>Creates the number that <paramref name="draft" /> describes.</summary>
    /// <param name="draft">The parts, which the caller has chosen from the valid ones.</param>
    public ValidGeneratedId(IdDraft draft)
    {
        _draft = draft;
    }

    /// <inheritdoc />
    public override string Text => _draft.Render();

    /// <summary>Writes the number in a notation, on the current Swedish date.</summary>
    /// <param name="notation">
    ///     The notation. <see cref="IdNotation.ZeroSerialLegalPerson" /> gives another number; the rest give this one.
    /// </param>
    /// <returns>The text.</returns>
    /// <exception cref="ArgumentOutOfRangeException">This number cannot be written that way.</exception>
    public string Render(IdNotation notation)
    {
        return Render(notation, SwedenClock.Today());
    }

    /// <inheritdoc cref="Render(IdNotation)" />
    /// <summary>Writes the number in a notation, on a given date.</summary>
    /// <param name="notation">The notation.</param>
    /// <param name="today">The date that decides whether a plus is contradictory.</param>
    public string Render(IdNotation notation, DateOnly today)
    {
        return NotationCatalog.Render(notation, _draft, today);
    }

    private protected override string Describe()
    {
        return nameof(ValidGeneratedId);
    }
}

/// <summary>A number that breaks exactly one rule.</summary>
internal sealed record InvalidGeneratedId : GeneratedId
{
    /// <summary>Creates an invalid number.</summary>
    /// <param name="text">The number.</param>
    /// <param name="defect">The rule it breaks.</param>
    public InvalidGeneratedId(string text, IdDefect defect)
    {
        Text = text;
        Defect = defect;
    }

    /// <inheritdoc />
    public override string Text { get; }

    /// <summary>The rule the number breaks.</summary>
    public IdDefect Defect { get; }

    private protected override string Describe()
    {
        return $"{nameof(InvalidGeneratedId)} ({Defect})";
    }
}
