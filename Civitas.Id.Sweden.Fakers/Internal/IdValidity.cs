namespace Civitas.Id.Sweden.Fakers.Internal;

/// <summary>Whether a generated number is valid.</summary>
internal enum IdValidity
{
    /// <summary>The number follows every rule.</summary>
    Valid,

    /// <summary>The number breaks exactly one rule.</summary>
    Invalid
}
