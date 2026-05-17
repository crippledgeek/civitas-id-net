using System.ComponentModel;
using System.Globalization;
using Civitas.Id.Sweden.Core;
using JetBrains.Annotations;

namespace Civitas.Id.Sweden.TypeConverters;

/// <summary>
///     Converts <see cref="OrganisationId" /> values to and from their canonical
///     string form for use with <see cref="TypeDescriptor" />-driven scenarios
///     (IConfiguration, designer host, third-party serializers).
/// </summary>
/// <remarks>
///     Per Lag (1974:174) §4, organisationsnummer is always 10 digits.
///     <see cref="OrganisationId.LongFormat" /> emits the 10-digit form
///     for both legal persons and Enskild firma; the 12-digit personnummer
///     view of Enskild firma is reached via
///     <see cref="OrganisationId.ToPhysicalPersonId" />.
/// </remarks>
[PublicAPI]
public sealed class OrganisationIdTypeConverter : TypeConverter
{
    /// <inheritdoc />
    public override bool CanConvertFrom(ITypeDescriptorContext? context, Type sourceType)
        => sourceType == typeof(string) || base.CanConvertFrom(context, sourceType);

    /// <inheritdoc />
    public override bool CanConvertTo(ITypeDescriptorContext? context, Type? destinationType)
        => destinationType == typeof(string) || base.CanConvertTo(context, destinationType);

    /// <inheritdoc />
    public override object? ConvertFrom(ITypeDescriptorContext? context, CultureInfo? culture, object value)
        => value switch
        {
            string s when OrganisationId.TryParse(s, out var id) => id,
            string s => OrganisationId.Parse(s), // throws — reached only when TryParse already returned false
            _ => base.ConvertFrom(context, culture, value),
        };

    /// <inheritdoc />
    public override object? ConvertTo(
        ITypeDescriptorContext? context, CultureInfo? culture,
        object? value, Type destinationType)
        => destinationType == typeof(string) && value is OrganisationId id
            ? id.LongFormat()
            : base.ConvertTo(context, culture, value, destinationType);
}
