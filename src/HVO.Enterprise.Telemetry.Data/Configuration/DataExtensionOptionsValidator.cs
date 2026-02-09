using System;
using Microsoft.Extensions.Options;

namespace HVO.Enterprise.Telemetry.Data.Configuration
{
    /// <summary>
    /// Validates <see cref="DataExtensionOptions"/> configuration.
    /// </summary>
    public class DataExtensionOptionsValidator : IValidateOptions<DataExtensionOptions>
    {
        /// <inheritdoc/>
        public ValidateOptionsResult Validate(string? name, DataExtensionOptions options)
        {
            if (options == null)
                return ValidateOptionsResult.Fail("Options cannot be null.");

            if (options.MaxStatementLength < 100 || options.MaxStatementLength > 50000)
                return ValidateOptionsResult.Fail("MaxStatementLength must be between 100 and 50000.");

            if (options.MaxParameters < 0 || options.MaxParameters > 100)
                return ValidateOptionsResult.Fail("MaxParameters must be between 0 and 100.");

            return ValidateOptionsResult.Success;
        }
    }
}
