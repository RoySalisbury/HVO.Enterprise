using System;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace HVO.Enterprise.Telemetry.Context
{
    /// <summary>
    /// Provides PII detection and redaction utilities.
    /// </summary>
    public sealed class PiiRedactor
    {
        private static readonly Regex EmailPattern = new Regex(
            @"[a-z0-9._%+-]+@[a-z0-9.-]+\.[a-z]{2,}",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static readonly Regex SsnPattern = new Regex(
            @"\b\d{3}-\d{2}-\d{4}\b",
            RegexOptions.Compiled);

        private static readonly Regex CreditCardPattern = new Regex(
            @"\b(?:\d[ -]*?){13,19}\b",
            RegexOptions.Compiled);

        private readonly ILogger<PiiRedactor> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="PiiRedactor"/> class.
        /// </summary>
        /// <param name="logger">Optional logger.</param>
        public PiiRedactor(ILogger<PiiRedactor>? logger = null)
        {
            _logger = logger ?? NullLogger<PiiRedactor>.Instance;
        }

        /// <summary>
        /// Attempts to redact a value based on options.
        /// </summary>
        /// <param name="key">Property key.</param>
        /// <param name="value">Property value.</param>
        /// <param name="options">Enrichment options.</param>
        /// <param name="redacted">Redacted value (null if removed).</param>
        /// <returns>True if redaction or detection occurred.</returns>
        public bool TryRedact(string key, string value, EnrichmentOptions options, out string? redacted)
        {
            if (options == null)
                throw new ArgumentNullException(nameof(options));

            options.EnsureDefaults();

            var isPii = IsPii(key, value, options);

            if (!isPii)
            {
                redacted = value;
                return false;
            }

            if (!options.RedactPii)
            {
                _logger.LogWarning("PII detected for key {Key}. Redaction disabled.", key);
                redacted = value;
                return true;
            }

            redacted = ApplyRedaction(value, options.RedactionStrategy);
            return true;
        }

        /// <summary>
        /// Determines if a key/value pair contains PII.
        /// </summary>
        /// <param name="key">Property key.</param>
        /// <param name="value">Property value.</param>
        /// <param name="options">Enrichment options.</param>
        /// <returns>True if PII is detected.</returns>
        public bool IsPii(string key, string value, EnrichmentOptions options)
        {
            if (options == null)
                throw new ArgumentNullException(nameof(options));

            options.EnsureDefaults();

            if (!string.IsNullOrEmpty(key) && options.PiiProperties.Contains(key))
                return true;

            if (string.IsNullOrEmpty(value))
                return false;

            return EmailPattern.IsMatch(value)
                || SsnPattern.IsMatch(value)
                || CreditCardPattern.IsMatch(value);
        }

        /// <summary>
        /// Checks if a member is marked as PII.
        /// </summary>
        /// <param name="member">Member info.</param>
        /// <returns>True if PII attribute is present.</returns>
        public static bool HasPiiAttribute(MemberInfo member)
        {
            if (member == null)
                throw new ArgumentNullException(nameof(member));

            return Attribute.IsDefined(member, typeof(PiiAttribute));
        }

        private static string? ApplyRedaction(string value, PiiRedactionStrategy strategy)
        {
            switch (strategy)
            {
                case PiiRedactionStrategy.Remove:
                    return null;
                case PiiRedactionStrategy.Mask:
                    return "***";
                case PiiRedactionStrategy.Hash:
                    return ComputeHash(value);
                case PiiRedactionStrategy.Partial:
                    return MaskPartial(value);
                default:
                    return "***";
            }
        }

        private static string ComputeHash(string value)
        {
            using (var sha256 = SHA256.Create())
            {
                var bytes = Encoding.UTF8.GetBytes(value);
                var hash = sha256.ComputeHash(bytes);
                return BitConverter.ToString(hash).Replace("-", string.Empty).ToLowerInvariant();
            }
        }

        private static string MaskPartial(string value)
        {
            if (value.Length <= 4)
                return "***";

            var first = value.Substring(0, 2);
            var last = value.Substring(value.Length - 2);
            return first + "***" + last;
        }
    }
}
