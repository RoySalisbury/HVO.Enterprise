using Microsoft.Extensions.Options;

namespace HVO.Enterprise.Telemetry.Data.Redis.Configuration
{
    /// <summary>
    /// Validates <see cref="RedisTelemetryOptions"/> configuration.
    /// </summary>
    public sealed class RedisTelemetryOptionsValidator : IValidateOptions<RedisTelemetryOptions>
    {
        /// <inheritdoc/>
        public ValidateOptionsResult Validate(string? name, RedisTelemetryOptions options)
        {
            if (options == null)
                return ValidateOptionsResult.Fail("Options cannot be null.");

            if (options.MaxKeyLength < 10 || options.MaxKeyLength > 1000)
                return ValidateOptionsResult.Fail("MaxKeyLength must be between 10 and 1000.");

            return ValidateOptionsResult.Success;
        }
    }
}
