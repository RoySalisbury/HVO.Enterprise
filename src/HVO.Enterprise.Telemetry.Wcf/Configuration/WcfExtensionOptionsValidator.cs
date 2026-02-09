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

            return ValidateOptionsResult.Success;
        }
    }
}
