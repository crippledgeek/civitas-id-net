using Civitas.Id.Sweden.Errors;

namespace Civitas.Id.Sweden.Tests.Helpers;

/// <summary>Why a reader that throws rejects a text.</summary>
internal static class Rejections
{
    /// <summary>
    ///     The reason, or <see langword="null" /> when the text is accepted. Any other exception passes through.
    /// </summary>
    public static InvalidIdNumberReason? ReasonOf(Func<object?> read)
    {
        try
        {
            _ = read();
            return null;
        }
        catch (InvalidIdNumberException exception)
        {
            return exception.Reason;
        }
    }
}
