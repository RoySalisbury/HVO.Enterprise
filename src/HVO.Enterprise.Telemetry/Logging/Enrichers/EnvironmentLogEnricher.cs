using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace HVO.Enterprise.Telemetry.Logging.Enrichers
{
    /// <summary>
    /// Enriches log entries with static environment information (machine name, process ID).
    /// </summary>
    /// <remarks>
    /// Values are captured once at construction time and cached — this enricher has
    /// near-zero per-call cost. Safe for use in performance-sensitive logging paths.
    /// </remarks>
    public sealed class EnvironmentLogEnricher : ILogEnricher
    {
        private static readonly string CachedMachineName = GetMachineName();
        private static readonly string CachedProcessId = GetProcessId();

        /// <inheritdoc />
        public void Enrich(IDictionary<string, object?> properties)
        {
            if (properties == null)
                throw new ArgumentNullException(nameof(properties));

            properties["MachineName"] = CachedMachineName;
            properties["ProcessId"] = CachedProcessId;
        }

        private static string GetMachineName()
        {
            try
            {
                return Environment.MachineName;
            }
            catch (InvalidOperationException)
            {
                // Can throw on some restricted platforms (e.g., sandboxed environments).
                return "unknown";
            }
        }

        private static string GetProcessId()
        {
            try
            {
                using (var process = Process.GetCurrentProcess())
                {
                    return process.Id.ToString();
                }
            }
            catch (Exception)
            {
                // Process.GetCurrentProcess() may throw on restricted platforms.
                return "0";
            }
        }
    }
}
