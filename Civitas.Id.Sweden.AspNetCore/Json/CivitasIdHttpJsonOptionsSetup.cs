using Civitas.Id.Sweden.Json;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.Extensions.Options;

namespace Civitas.Id.Sweden.AspNetCore.Json;

/// <summary>
/// Bridges <see cref="CivitasIdSwedenJsonContext"/> into the ASP.NET Core
/// <see cref="JsonOptions"/> pipeline by chaining the source-generated
/// type info resolver onto the serializer options.
/// </summary>
/// <remarks>
/// Lives in the <c>.AspNetCore</c> assembly (not <c>.Json</c>) because
/// <see cref="JsonOptions"/> requires
/// <c>FrameworkReference Microsoft.AspNetCore.App</c>.
/// </remarks>
internal sealed class CivitasIdHttpJsonOptionsSetup : IConfigureOptions<JsonOptions>
{
    /// <summary>
    /// Adds <see cref="CivitasIdSwedenJsonContext.Default"/> to the
    /// <see cref="JsonOptions.SerializerOptions"/> type-info resolver chain.
    /// </summary>
    public void Configure(JsonOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        options.SerializerOptions.TypeInfoResolverChain.Add(CivitasIdSwedenJsonContext.Default);
    }
}
