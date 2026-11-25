namespace Civitas.Id.Sweden.Fakers.Internal;

/// <summary>Turns the draft of a valid number into text that breaks one rule.</summary>
/// <param name="draft">The valid number's parts.</param>
/// <param name="next">The random source, <c>(minInclusive, maxExclusive) =&gt; int</c>.</param>
/// <returns>The invalid text.</returns>
internal delegate string Corruption(IdDraft draft, Func<int, int, int> next);
