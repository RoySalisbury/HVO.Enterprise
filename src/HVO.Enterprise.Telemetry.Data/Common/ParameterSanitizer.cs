using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace HVO.Enterprise.Telemetry.Data.Common
{
    /// <summary>
    /// Sanitizes database parameters and connection strings to remove sensitive data.
    /// </summary>
    public static class ParameterSanitizer
    {
        private static readonly Regex PasswordPattern = new Regex(
            @"(password|pwd|secret|token|key)\s*=\s*[^;]+",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static readonly HashSet<string> SensitiveParamNames = new HashSet<string>(
            StringComparer.OrdinalIgnoreCase)
        {
            "password", "pwd", "secret", "token", "apikey", "api_key",
            "ssn", "credit_card", "creditcard", "cvv", "pin",
            "authorization", "auth_token", "access_token", "refresh_token"
        };

        /// <summary>
        /// The redacted value placeholder.
        /// </summary>
        public const string RedactedValue = "***REDACTED***";

        /// <summary>
        /// Sanitizes a connection string by removing password-like values.
        /// </summary>
        /// <param name="connectionString">The connection string to sanitize.</param>
        /// <returns>A sanitized connection string with sensitive values replaced.</returns>
        public static string SanitizeConnectionString(string? connectionString)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
                return string.Empty;

            return PasswordPattern.Replace(connectionString!, "$1=" + RedactedValue);
        }

        /// <summary>
        /// Determines if a parameter name suggests sensitive data.
        /// </summary>
        /// <param name="parameterName">The parameter name to check.</param>
        /// <returns><c>true</c> if the parameter name matches a known sensitive pattern.</returns>
        public static bool IsSensitiveParameter(string? parameterName)
        {
            if (string.IsNullOrWhiteSpace(parameterName))
                return false;

            // Remove common DB parameter prefixes
            var cleanName = parameterName!.TrimStart('@', ':', '?');

            return SensitiveParamNames.Contains(cleanName);
        }

        /// <summary>
        /// Sanitizes a SQL statement by truncating if it exceeds the maximum length.
        /// </summary>
        /// <param name="statement">The SQL statement to sanitize.</param>
        /// <param name="maxLength">The maximum allowed length. Default: 2000.</param>
        /// <returns>The sanitized (possibly truncated) statement.</returns>
        public static string SanitizeStatement(string? statement, int maxLength = 2000)
        {
            if (string.IsNullOrWhiteSpace(statement))
                return string.Empty;

            if (statement!.Length <= maxLength)
                return statement;

            return statement.Substring(0, maxLength) + "... [truncated]";
        }

        /// <summary>
        /// Formats a parameter value safely for telemetry, redacting sensitive values.
        /// </summary>
        /// <param name="name">The parameter name.</param>
        /// <param name="value">The parameter value.</param>
        /// <returns>A safe string representation of the value.</returns>
        public static string FormatParameterValue(string? name, object? value)
        {
            if (IsSensitiveParameter(name))
                return RedactedValue;

            if (value == null || value == DBNull.Value)
                return "NULL";

            if (value is string str)
            {
                if (str.Length > 100)
                    return $"\"{str.Substring(0, 100)}...\" (truncated)";
                return $"\"{str}\"";
            }

            if (value is byte[] bytes)
                return $"<binary {bytes.Length} bytes>";

            return value.ToString() ?? "NULL";
        }
    }
}
