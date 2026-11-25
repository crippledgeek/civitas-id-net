namespace Civitas.Id.Sweden.Fakers.Internal;

/// <summary>The one place that decides what an <see cref="IdValidity" /> value asks for.</summary>
internal static class IdValidityExtensions
{
    /// <summary>Runs the function for <paramref name="validity" />.</summary>
    /// <param name="validity">The value to decide on.</param>
    /// <param name="valid">Runs for <see cref="IdValidity.Valid" />.</param>
    /// <param name="invalid">Runs for <see cref="IdValidity.Invalid" />.</param>
    /// <returns>What the function returned.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="validity" /> is not defined.</exception>
    public static GeneratedId Match(this IdValidity validity, Func<GeneratedId> valid, Func<GeneratedId> invalid)
    {
        return validity switch
        {
            IdValidity.Valid => valid(),
            IdValidity.Invalid => invalid(),
            _ => throw new ArgumentOutOfRangeException(nameof(validity), validity, null)
        };
    }
}
