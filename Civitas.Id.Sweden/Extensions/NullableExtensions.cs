using System.Diagnostics.CodeAnalysis;
using JetBrains.Annotations;

namespace Civitas.Id.Sweden.Extensions;

/// <summary>
///     Optional pipeline-style extension methods on nullable reference types.
///     Opt-in via <c>using Civitas.Id.Sweden.Extensions;</c>.
/// </summary>
/// <remarks>
///     These mirror the Java <c>Optional</c> / TypeScript <c>T | undefined</c> idiom in C#
///     without requiring a third-party Maybe/Option dependency. The .NET-canonical alternative
///     is the <c>TryParse</c> + <c>out</c> + nullable-flow pattern; these extensions are an
///     opt-in convenience for callers who prefer fluent monadic chaining.
/// </remarks>
[SuppressMessage(
    "Design", "CA1034:Nested types should not be visible",
    Justification = "C# 14 'extension(T)' block is compiled to a nested type by the language; not authored by us.")]
public static class NullableExtensions
{
    extension<T>(T? source)
        where T : class
    {
        /// <summary>Applies <paramref name="f" /> to <paramref name="source" /> if non-null; returns null otherwise.</summary>
        /// <typeparam name="TResult">The result reference type.</typeparam>
        /// <param name="f">The mapping function.</param>
        /// <returns>The mapped value, or null when <paramref name="source" /> is null.</returns>
        [Pure]
        public TResult? Map<TResult>(Func<T, TResult> f)
            where TResult : class
        {
            ArgumentNullException.ThrowIfNull(f);
            return source is null ? null : f(source);
        }

        /// <summary>Returns <paramref name="source" /> when non-null and <paramref name="predicate" /> is true; null otherwise.</summary>
        /// <param name="predicate">The predicate to apply.</param>
        /// <returns><paramref name="source" /> when both non-null and predicate-true; otherwise null.</returns>
        [Pure]
        public T? Filter(Func<T, bool> predicate)
        {
            ArgumentNullException.ThrowIfNull(predicate);
            return source is not null && predicate(source) ? source : null;
        }

        /// <summary>
        ///     Calls <paramref name="some" /> when <paramref name="source" /> is non-null; otherwise calls
        ///     <paramref name="none" />.
        /// </summary>
        /// <typeparam name="TResult">The result type (may be reference or value).</typeparam>
        /// <param name="some">The function applied when source is non-null.</param>
        /// <param name="none">The function applied when source is null.</param>
        /// <returns>The result of either <paramref name="some" /> or <paramref name="none" />.</returns>
        [Pure]
        public TResult Match<TResult>(
            Func<T, TResult> some,
            Func<TResult> none)
        {
            ArgumentNullException.ThrowIfNull(some);
            ArgumentNullException.ThrowIfNull(none);
            return source is null ? none() : some(source);
        }

        /// <summary>
        ///     Monadic bind: chains a nullable-returning function on a possibly-null source.
        /// </summary>
        /// <typeparam name="TResult">The result reference type.</typeparam>
        /// <param name="f">The chaining function (may itself return null).</param>
        /// <returns>The result of <paramref name="f" /> when source is non-null; otherwise null.</returns>
        [Pure]
        public TResult? Bind<TResult>(Func<T, TResult?> f)
            where TResult : class
        {
            ArgumentNullException.ThrowIfNull(f);
            return source is null ? null : f(source);
        }
    }
}
