using System;
using Microsoft.Extensions.Options;

namespace HVO.Enterprise.Telemetry.IIS.Configuration
{
    /// <summary>
    /// Validates <see cref="IisExtensionOptions"/> when resolved via the Options pattern.
    /// </summary>
    internal sealed class IisExtensionOptionsValidator : IValidateOptions<IisExtensionOptions>
    {
        /// <inheritdoc />
        public ValidateOptionsResult Validate(string? name, IisExtensionOptions options)
        {
            if (options == null)
                return ValidateOptionsResult.Fail("Options instance is null.");

            if (options.ShutdownTimeout < TimeSpan.Zero)
                return ValidateOptionsResult.Fail("ShutdownTimeout cannot be negative.");

            if (options.ShutdownTimeout > TimeSpan.FromSeconds(120))
                return ValidateOptionsResult.Fail("ShutdownTimeout cannot exceed 120 seconds.");

            return ValidateOptionsResult.Success;
        }
    }
}
