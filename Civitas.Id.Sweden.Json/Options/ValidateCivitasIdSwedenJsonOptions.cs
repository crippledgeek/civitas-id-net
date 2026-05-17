using Microsoft.Extensions.Options;

namespace Civitas.Id.Sweden.Json.Options;

[OptionsValidator]
internal sealed partial class ValidateCivitasIdSwedenJsonOptions
    : IValidateOptions<CivitasIdSwedenJsonOptions>;
