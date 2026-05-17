using System.ComponentModel;
using System.Globalization;
using Civitas.Id.Sweden.Core;
using Civitas.Id.Sweden.Errors;
using JetBrains.Annotations;

namespace Civitas.Id.Sweden.TypeConverters;

/// <summary>
///     Converts <see cref="CoordinationId" /> values to and from their canonical
///     12-digit string form for use with <see cref="TypeDescriptor" />-driven
///     scenarios (IConfiguration, designer host, third-party serializers).
/// </summary>
[PublicAPI]
public sealed class CoordinationIdTypeConverter : TypeConverter
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
            string s when CoordinationId.TryParse(s, out var id) => id,
            string s => throw new InvalidIdNumberException(s, ClassifyFailure(s)),
            _ => base.ConvertFrom(context, culture, value),
        };

    /// <inheritdoc />
    public override object? ConvertTo(
        ITypeDescriptorContext? context, CultureInfo? culture,
        object? value, Type destinationType)
        => destinationType == typeof(string) && value is CoordinationId id
            ? id.LongFormat()
            : base.ConvertTo(context, culture, value, destinationType);

    private static InvalidIdNumberReason ClassifyFailure(string s)
    {
        try
        {
            _ = CoordinationId.Parse(s);
            return InvalidIdNumberReason.Unknown;
        }
        catch (InvalidIdNumberException ex)
        {
            return ex.Reason;
        }
    }
}
