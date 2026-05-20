namespace Civitas.Id.Sweden.EntityFrameworkCore;

using System;
using Civitas.Id.Sweden.Core;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;

/// <summary>
///     Extension methods that register the Civitas.Id.Sweden EF Core value converters
///     globally on a <see cref="ModelConfigurationBuilder"/>.
/// </summary>
[PublicAPI]
public static class ModelConfigurationBuilderExtensions
{
    /// <summary>
    ///     Registers <see cref="PersonalIdConverter"/>, <see cref="CoordinationIdConverter"/>,
    ///     and <see cref="OrganisationIdConverter"/> globally on this
    ///     <see cref="ModelConfigurationBuilder"/>. Call from
    ///     <see cref="DbContext.ConfigureConventions"/>.
    /// </summary>
    /// <param name="builder">The model configuration builder.</param>
    /// <returns>
    ///     The same <paramref name="builder"/> for fluent chaining.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    ///     Thrown when <paramref name="builder"/> is <see langword="null"/>.
    /// </exception>
    /// <remarks>
    ///     Nullable reference types (<c>PersonalId?</c> etc.) are matched in
    ///     addition to non-nullable types because NRT is compile-time-only
    ///     metadata — the CLR property type is identical, and
    ///     <c>ModelConfigurationBuilder.Properties&lt;TProperty&gt;()</c>
    ///     matches on the CLR type.
    /// </remarks>
    public static ModelConfigurationBuilder UseCivitasIdSweden(this ModelConfigurationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Properties<PersonalId>().HaveConversion<PersonalIdConverter>();
        builder.Properties<CoordinationId>().HaveConversion<CoordinationIdConverter>();
        builder.Properties<OrganisationId>().HaveConversion<OrganisationIdConverter>();

        return builder;
    }
}
