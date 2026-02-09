using System;
using System.Collections.Generic;
using System.Reflection;
using HVO.Enterprise.Telemetry.Proxies;

namespace HVO.Enterprise.Telemetry.Capture
{
    /// <summary>
    /// Interface for capturing method parameters with sensitivity awareness
    /// and configurable verbosity levels.
    /// </summary>
    public interface IParameterCapture
    {
        /// <summary>
        /// Captures a single parameter value based on configured capture level,
        /// performing sensitive data detection and redaction as configured.
        /// </summary>
        /// <param name="parameterName">The parameter name (used for sensitive data detection).</param>
        /// <param name="value">The parameter value to capture.</param>
        /// <param name="parameterType">The declared type of the parameter.</param>
        /// <param name="options">Capture options controlling depth, level, and redaction.</param>
        /// <returns>
        /// The captured representation of the value, or <c>null</c> when capture is suppressed.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="parameterName"/> or <paramref name="parameterType"/> or
        /// <paramref name="options"/> is <c>null</c>.
        /// </exception>
        object? CaptureParameter(
            string parameterName,
            object? value,
            Type parameterType,
            ParameterCaptureOptions options);

        /// <summary>
        /// Captures multiple parameters from a method invocation, respecting
        /// <see cref="SensitiveDataAttribute"/> annotations on individual parameters.
        /// </summary>
        /// <param name="parameters">The method parameter metadata.</param>
        /// <param name="values">The actual parameter values.</param>
        /// <param name="options">Capture options controlling depth, level, and redaction.</param>
        /// <returns>
        /// A dictionary mapping parameter names to their captured values.
        /// Entries are omitted for parameters whose capture level prevents capture.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="parameters"/>, <paramref name="values"/>, or
        /// <paramref name="options"/> is <c>null</c>.
        /// </exception>
        IDictionary<string, object?> CaptureParameters(
            ParameterInfo[] parameters,
            object?[] values,
            ParameterCaptureOptions options);

        /// <summary>
        /// Registers a custom sensitive data pattern. Parameters or property names
        /// matching this pattern will be redacted using the specified strategy.
        /// </summary>
        /// <param name="pattern">
        /// A pattern to match against parameter/property names (case-insensitive substring match).
        /// </param>
        /// <param name="strategy">The redaction strategy to apply.</param>
        /// <exception cref="ArgumentNullException"><paramref name="pattern"/> is <c>null</c> or empty.</exception>
        void RegisterSensitivePattern(string pattern, RedactionStrategy strategy);

        /// <summary>
        /// Checks whether a parameter or property name matches any registered
        /// sensitive data pattern.
        /// </summary>
        /// <param name="parameterName">The name to check.</param>
        /// <returns><c>true</c> if the name matches a sensitive pattern.</returns>
        bool IsSensitive(string parameterName);

        /// <summary>
        /// Gets the redaction strategy for a sensitive parameter name.
        /// Returns the strategy of the first matching pattern, or the default
        /// <see cref="RedactionStrategy.Mask"/> when not found.
        /// </summary>
        /// <param name="parameterName">The name to check.</param>
        /// <returns>The redaction strategy for the matched pattern.</returns>
        RedactionStrategy GetRedactionStrategy(string parameterName);
    }
}
