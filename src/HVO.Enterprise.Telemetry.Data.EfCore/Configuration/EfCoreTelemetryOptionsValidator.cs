using HVO.Enterprise.Telemetry.Data.Configuration;
using Microsoft.Extensions.Options;

namespace HVO.Enterprise.Telemetry.Data.EfCore.Configuration
{
    /// <summary>
    /// Validates <see cref="EfCoreTelemetryOptions"/> configuration.
    /// Delegates base property validation to <see cref="DataExtensionOptionsValidator.ValidateBaseOptions"/>.
    /// </summary>
    public sealed class EfCoreTelemetryOptionsValidator : IValidateOptions<EfCoreTelemetryOptions>
    {
        /// <inheritdoc/>
        public ValidateOptionsResult Validate(string? name, EfCoreTelemetryOptions options)
        {
            var baseResult = DataExtensionOptionsValidator.ValidateBaseOptions(options);
            if (baseResult.Failed)
                return baseResult;

            // Add EfCore-specific validation here as needed.

            return ValidateOptionsResult.Success;
        }
    }
}
