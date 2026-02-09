using System;
using Microsoft.Extensions.Options;

namespace HVO.Enterprise.Telemetry.Wcf.Configuration
{
    /// <summary>
    /// Validates <see cref="WcfExtensionOptions"/> when resolved via the Options pattern.
    /// </summary>
    internal sealed class WcfExtensionOptionsValidator : IValidateOptions<WcfExtensionOptions>
    {
        /// <inheritdoc />
        public ValidateOptionsResult Validate(string? name, WcfExtensionOptions options)
        {
            if (options == null)
                return ValidateOptionsResult.Fail("Options instance is null.");

            if (options.MaxMessageBodySize < 0)
                return ValidateOptionsResult.Fail("MaxMessageBodySize cannot be negative.");

            if (options.MaxMessageBodySize > 1_048_576)
                return ValidateOptionsResult.Fail("MaxMessageBodySize cannot exceed 1,048,576 bytes (1 MB).");

            return ValidateOptionsResult.Success;
        }
    }
}
