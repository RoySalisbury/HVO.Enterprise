using HVO.Enterprise.Telemetry.Data.Configuration;
using Microsoft.Extensions.Options;

namespace HVO.Enterprise.Telemetry.Data.AdoNet.Configuration
{
    /// <summary>
    /// Validates <see cref="AdoNetTelemetryOptions"/> configuration.
    /// Delegates base property validation to <see cref="DataExtensionOptionsValidator.ValidateBaseOptions"/>.
    /// </summary>
    public sealed class AdoNetTelemetryOptionsValidator : IValidateOptions<AdoNetTelemetryOptions>
    {
        /// <inheritdoc/>
        public ValidateOptionsResult Validate(string? name, AdoNetTelemetryOptions options)
        {
            var baseResult = DataExtensionOptionsValidator.ValidateBaseOptions(options);
            if (baseResult.Failed)
                return baseResult;

            // Add ADO.NET-specific validation here as needed.

            return ValidateOptionsResult.Success;
        }
    }
}
