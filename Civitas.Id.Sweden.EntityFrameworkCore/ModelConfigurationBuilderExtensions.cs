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
    ///     <see cref="ModelConfigurationBuilder"/> and configures column facets
    ///     (<c>MaxLength(12)</c>, <c>Unicode(false)</c>) for all three types. Call from
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
    ///     <para>
    ///         Nullable reference types (<c>PersonalId?</c> etc.) are matched in
    ///         addition to non-nullable types because NRT is compile-time-only
    ///         metadata — the CLR property type is identical, and
    ///         <c>ModelConfigurationBuilder.Properties&lt;TProperty&gt;()</c>
    ///         matches on the CLR type.
    ///     </para>
    ///     <para>
    ///         Column facets are set explicitly (not only via
    ///         <see cref="Microsoft.EntityFrameworkCore.Storage.ValueConversion.ConverterMappingHints"/>)
    ///         so that <c>IProperty.GetMaxLength()</c> and <c>IProperty.IsUnicode()</c>
    ///         reflect the intended schema in migration snapshots and tooling.
    ///     </para>
    /// </remarks>
    public static ModelConfigurationBuilder UseCivitasIdSweden(this ModelConfigurationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Properties<PersonalId>()
            .HaveConversion<PersonalIdConverter>()
            .HaveMaxLength(12)
            .AreUnicode(false);

        builder.Properties<CoordinationId>()
            .HaveConversion<CoordinationIdConverter>()
            .HaveMaxLength(12)
            .AreUnicode(false);

        builder.Properties<OrganisationId>()
            .HaveConversion<OrganisationIdConverter>()
            .HaveMaxLength(12)
            .AreUnicode(false);

        return builder;
    }
}
