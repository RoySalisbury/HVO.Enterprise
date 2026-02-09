using Microsoft.Extensions.Options;

namespace HVO.Enterprise.Telemetry.Data.RabbitMQ.Configuration
{
    /// <summary>
    /// Validates <see cref="RabbitMqTelemetryOptions"/> configuration.
    /// </summary>
    public sealed class RabbitMqTelemetryOptionsValidator : IValidateOptions<RabbitMqTelemetryOptions>
    {
        /// <inheritdoc />
        public ValidateOptionsResult Validate(string? name, RabbitMqTelemetryOptions options)
        {
            if (options == null)
            {
                return ValidateOptionsResult.Fail("RabbitMQ telemetry options must not be null.");
            }

            // All boolean options are valid in any combination.
            return ValidateOptionsResult.Success;
        }
    }
}
