using Civitas.Id.Sweden.Json.Options;
using Microsoft.Extensions.Options;
using MvcJsonOptions = Microsoft.AspNetCore.Mvc.JsonOptions;

namespace Civitas.Id.Sweden.AspNetCore.Json;

/// <summary>
/// Mirrors <see cref="CivitasIdHttpJsonOptionsSetup"/> for MVC controllers.
/// Silent no-op when <c>AddControllers()</c> is never called — the
/// <c>Mvc.JsonOptions</c> instance is never resolved by the framework
/// without the MVC pipeline, so this <see cref="Configure"/> delegate
/// never fires.
/// </summary>
/// <remarks>
/// Lives in <c>Civitas.Id.Sweden.AspNetCore</c> (not <c>.Json</c>) because
/// <see cref="MvcJsonOptions"/> requires
/// <c>FrameworkReference Microsoft.AspNetCore.App</c>.
/// </remarks>
internal sealed class CivitasIdMvcJsonOptionsSetup(IOptions<CivitasIdSwedenJsonOptions> jsonOpts)
    : IConfigureOptions<MvcJsonOptions>
{
    /// <summary>
    /// Adds the source-generated type info resolver and format-aware
    /// converters to <see cref="MvcJsonOptions.JsonSerializerOptions"/>.
    /// </summary>
    /// <remarks>
    /// NOTE: <see cref="MvcJsonOptions"/> exposes
    /// <see cref="MvcJsonOptions.JsonSerializerOptions"/> — different spelling
    /// from <c>Http.Json.JsonOptions.SerializerOptions</c>. Two independent
    /// setups are required to cover both surfaces because
    /// <c>SystemTextJsonInputFormatter</c> reads ONLY <see cref="MvcJsonOptions"/>.
    /// </remarks>
    public void Configure(MvcJsonOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        ConverterInstaller.Install(options.JsonSerializerOptions, jsonOpts.Value.PersonnummerFormat);
    }
}
