using System;
using HVO.Enterprise.Telemetry.Configuration;
using Microsoft.Extensions.Options;

namespace HVO.Enterprise.Telemetry.Configuration
{
    /// <summary>
    /// Validates <see cref="TelemetryOptions"/> during DI container build.
    /// Delegates to <see cref="TelemetryOptions.Validate"/> for validation logic.
    /// </summary>
    internal sealed class TelemetryOptionsValidator : IValidateOptions<TelemetryOptions>
    {
        /// <inheritdoc />
        public ValidateOptionsResult Validate(string? name, TelemetryOptions options)
        {
            if (options == null)
                return ValidateOptionsResult.Fail("TelemetryOptions must not be null.");

            try
            {
                options.Validate();
                return ValidateOptionsResult.Success;
            }
            catch (InvalidOperationException ex)
            {
                return ValidateOptionsResult.Fail(ex.Message);
            }
        }
    }
}
