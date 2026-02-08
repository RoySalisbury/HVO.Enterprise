using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace HVO.Enterprise.Telemetry.Exceptions
{
    /// <summary>
    /// Generates stable fingerprints for exceptions to enable grouping and aggregation.
    /// </summary>
    public static class ExceptionFingerprinter
    {
        private static readonly Regex GuidPattern = new Regex(
            @"\b[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}\b",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static readonly Regex NumberPattern = new Regex(
            @"\b\d{2,}\b",
            RegexOptions.Compiled);

        private static readonly Regex UrlPattern = new Regex(
            @"https?://[^\s]+",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static readonly Regex WhitespacePattern = new Regex(
            @"\s+",
            RegexOptions.Compiled);

        /// <summary>
        /// Generates a stable fingerprint for the exception.
        /// </summary>
        /// <param name="exception">Exception to fingerprint.</param>
        /// <returns>SHA256 hash representing the exception fingerprint.</returns>
        /// <remarks>
        /// Uses the exception type, normalized message, and the first three stack frames. For
        /// <see cref="AggregateException"/>, only the first three inner exceptions are included
        /// to limit fingerprint variability and processing overhead.
        /// </remarks>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="exception"/> is null.</exception>
        public static string GenerateFingerprint(Exception exception)
        {
            if (exception == null)
                throw new ArgumentNullException(nameof(exception));

            var components = new List<string>
            {
                exception.GetType().FullName ?? exception.GetType().Name,
                NormalizeMessage(exception.Message),
                NormalizeStackTrace(exception.StackTrace)
            };

            if (exception is AggregateException aggregateException)
            {
                foreach (var inner in aggregateException.InnerExceptions.Take(3))
                {
                    components.Add(GenerateFingerprint(inner));
                }
            }
            else if (exception.InnerException != null)
            {
                components.Add(GenerateFingerprint(exception.InnerException));
            }

            var combined = string.Join("|", components);
            return ComputeHash(combined);
        }

        private static string NormalizeMessage(string message)
        {
            if (string.IsNullOrEmpty(message))
                return string.Empty;

            message = GuidPattern.Replace(message, "{guid}");
            message = NumberPattern.Replace(message, "{number}");
            message = UrlPattern.Replace(message, "{url}");
            message = WhitespacePattern.Replace(message, " ").Trim();

            return message;
        }

        private static string NormalizeStackTrace(string? stackTrace)
        {
            if (string.IsNullOrEmpty(stackTrace))
                return string.Empty;

            var frames = stackTrace!
                .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                .Take(3);

            var normalized = new List<string>();
            foreach (var frame in frames)
            {
                var cleaned = Regex.Replace(frame, @" in .+:line \d+", string.Empty);
                cleaned = Regex.Replace(cleaned, @"\[.+?\]", string.Empty);
                cleaned = WhitespacePattern.Replace(cleaned, " ").Trim();
                normalized.Add(cleaned);
            }

            return string.Join("|", normalized);
        }

        private static string ComputeHash(string input)
        {
            using var sha256 = SHA256.Create();
            var bytes = Encoding.UTF8.GetBytes(input);
            var hash = sha256.ComputeHash(bytes);
            return BitConverter.ToString(hash).Replace("-", string.Empty).ToLowerInvariant();
        }
    }
}
