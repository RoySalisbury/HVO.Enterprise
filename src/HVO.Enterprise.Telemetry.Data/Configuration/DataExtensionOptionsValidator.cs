using System;
using Microsoft.Extensions.Options;

namespace HVO.Enterprise.Telemetry.Data.Configuration
{
    /// <summary>
    /// Validates <see cref="DataExtensionOptions"/> configuration.
    /// Also provides <see cref="ValidateBaseOptions"/> for derived option validators
    /// to reuse common range checks without duplication.
    /// </summary>
    public class DataExtensionOptionsValidator : IValidateOptions<DataExtensionOptions>
    {
        /// <inheritdoc/>
        public ValidateOptionsResult Validate(string? name, DataExtensionOptions options)
        {
            return ValidateBaseOptions(options);
        }

        /// <summary>
        /// Validates the base <see cref="DataExtensionOptions"/> properties shared by all
        /// data extension packages (MaxStatementLength, MaxParameters).
        /// Derived validators should call this method to avoid duplicating range checks.
        /// </summary>
        /// <param name="options">The options instance to validate (may be a derived type).</param>
        /// <returns>
        /// <see cref="ValidateOptionsResult.Success"/> if all base properties are valid;
        /// otherwise a <see cref="ValidateOptionsResult"/> with the failure message.
        /// </returns>
        public static ValidateOptionsResult ValidateBaseOptions(DataExtensionOptions? options)
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
