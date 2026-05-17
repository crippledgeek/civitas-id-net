using System.ComponentModel;
using System.Globalization;
using Civitas.Id.Sweden.Core;
using Civitas.Id.Sweden.Errors;
using JetBrains.Annotations;

namespace Civitas.Id.Sweden.TypeConverters;

/// <summary>
///     Converts <see cref="SwedishOfficialId" /> values (any subtype) to and
///     from their canonical string form via
///     <see cref="SwedishOfficialId.TryParseAny(string, out SwedishOfficialId)" />.
/// </summary>
[PublicAPI]
public sealed class SwedishOfficialIdTypeConverter : TypeConverter
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
            string s when SwedishOfficialId.TryParseAny(s, out var id) => id,
            string s => throw new InvalidIdNumberException(s, InvalidIdNumberReason.UnsupportedIdType),
            _ => base.ConvertFrom(context, culture, value),
        };

    /// <inheritdoc />
    public override object? ConvertTo(
        ITypeDescriptorContext? context, CultureInfo? culture,
        object? value, Type destinationType)
        => destinationType == typeof(string) && value is SwedishOfficialId id
            ? id.LongFormat()
            : base.ConvertTo(context, culture, value, destinationType);
}
