using System;
using Microsoft.Extensions.Options;

namespace HVO.Enterprise.Telemetry.Grpc
{
    /// <summary>
    /// Validates <see cref="GrpcTelemetryOptions"/> configuration.
    /// </summary>
    internal sealed class GrpcTelemetryOptionsValidator : IValidateOptions<GrpcTelemetryOptions>
    {
        /// <summary>
        /// Validates the specified <see cref="GrpcTelemetryOptions"/> instance.
        /// </summary>
        /// <param name="name">The name of the options instance being validated.</param>
        /// <param name="options">The options instance to validate.</param>
        /// <returns>A <see cref="ValidateOptionsResult"/> indicating success or failure.</returns>
        public ValidateOptionsResult Validate(string? name, GrpcTelemetryOptions options)
        {
            if (options == null)
                return ValidateOptionsResult.Fail("GrpcTelemetryOptions cannot be null.");

            if (string.IsNullOrWhiteSpace(options.CorrelationHeaderName))
                return ValidateOptionsResult.Fail("CorrelationHeaderName cannot be null or empty.");

            return ValidateOptionsResult.Success;
        }
    }
}
