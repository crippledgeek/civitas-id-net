namespace Civitas.Id.Sweden.Internal;

/// <summary>
///     Allocation-free value-type result of <see cref="SwedishIdParsing.TryMatchSpan"/>.
///     Holds indices/values into the source span and parsed integer components.
///     Replaces the reference-type <see cref="Core.SwedishOfficialId.SwedishIdMatcher"/>
///     for hot-path parsing (Match/Group object graph eliminated).
/// </summary>
internal readonly ref struct SwedishIdSpanMatch
{
    /// <summary>The trimmed source span (without leading/trailing whitespace; SE prefix retained — slices index past it).</summary>
    public ReadOnlySpan<char> Source { get; init; }

    /// <summary>True if the input contained an explicit 2-digit century prefix.</summary>
    public bool HasCentury { get; init; }

    /// <summary>Parsed century (e.g. 19 or 20). 0 when <see cref="HasCentury"/> is false.</summary>
    public int CenturyValue { get; init; }

    /// <summary>The 2-digit short year as an int (0..99).</summary>
    public int Year { get; init; }

    /// <summary>The 2-digit month as an int.</summary>
    public int Month { get; init; }

    /// <summary>The 2-digit day as an int (1..31 person, 61..91 samordning, ≥20 organisation "month").</summary>
    public int Day { get; init; }

    /// <summary>'-' or '+' when a delimiter was present, otherwise default ('\0').</summary>
    public char Delimiter { get; init; }

    /// <summary>Start index of 2-digit year text within Source.</summary>
    public int YearStart { get; init; }

    /// <summary>Start index of 2-digit month text within Source.</summary>
    public int MonthStart { get; init; }

    /// <summary>Start index of 2-digit day text within Source.</summary>
    public int DayStart { get; init; }

    /// <summary>Start index of 4-digit unique text within Source.</summary>
    public int UniqueStart { get; init; }

    /// <summary>Allocation-free span over the 2-digit year text.</summary>
    public ReadOnlySpan<char> YearTextSpan => Source.Slice(YearStart, 2);

    /// <summary>Allocation-free span over the 2-digit month text.</summary>
    public ReadOnlySpan<char> MonthTextSpan => Source.Slice(MonthStart, 2);

    /// <summary>Allocation-free span over the 2-digit day text.</summary>
    public ReadOnlySpan<char> DayTextSpan => Source.Slice(DayStart, 2);

    /// <summary>Allocation-free span over the 4-digit unique suffix.</summary>
    public ReadOnlySpan<char> UniqueSpan => Source.Slice(UniqueStart, 4);
}
