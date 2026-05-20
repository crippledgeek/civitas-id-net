using Civitas.Id.Sweden.Core;

namespace Civitas.Id.Sweden.Internal;

/// <summary>
///     Internal CRTP hook contract for the
///     <c>PhysicalPersonId.TryParseCore&lt;TSelf&gt;</c>
///     generic dispatcher. Mirrors the BCL pattern
///     (<c>IBinaryIntegerParseAndFormatInfo&lt;TSelf&gt;</c> in
///     <c>System.Private.CoreLib/src/System/Number.Parsing.cs</c>) — internal,
///     CRTP-constrained, exclusively dispatched through the generic constraint.
///     NOT public API.
/// </summary>
/// <typeparam name="TSelf">The sealed-record subtype implementing this contract.</typeparam>
// ReSharper disable once TypeParameterCanBeVariant
internal interface ISwedishPersonIdHooks<TSelf>
    where TSelf : PhysicalPersonId, ISwedishPersonIdHooks<TSelf>
{
    /// <summary>
    ///     Validates the encoded day component
    ///     (1..31 for personnummer, 61..91 for samordningsnummer).
    /// </summary>
    static abstract bool IsDayValid(int encodedDay);

    /// <summary>
    ///     Maps encoded day → calendar day
    ///     (identity for personnummer, <c>day - 60</c> for samordningsnummer).
    /// </summary>
    static abstract int CalendarDay(int encodedDay);

    /// <summary>
    ///     Constructs the sealed subtype from a validated 12-digit normalised string.
    /// </summary>
    /// <param name="normalised12">A canonical 12-digit normalised body (YYYYMMDDXXXX).</param>
    static abstract TSelf FromValidated(string normalised12);
}
