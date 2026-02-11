using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;

namespace HVO.Enterprise.Telemetry.Exceptions
{
    /// <summary>
    /// Options for configuring first-chance exception monitoring.
    /// First-chance exceptions are fired by the CLR the instant an exception is thrown,
    /// before any catch handler runs. This enables detection of suppressed or swallowed exceptions.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This feature is <strong>opt-in</strong> (disabled by default) because every exception in the
    /// process triggers the first-chance event — including harmless framework-internal exceptions.
    /// Enable it selectively for diagnostics and use the filtering options to reduce noise.
    /// </para>
    /// <para>
    /// Options are monitored at runtime via <c>IOptionsMonitor&lt;FirstChanceExceptionOptions&gt;</c>,
    /// so changes to <c>appsettings.json</c> take effect immediately without a restart.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// services.AddFirstChanceExceptionMonitoring(options =>
    /// {
    ///     options.Enabled = true;
    ///     options.MaxEventsPerSecond = 50;
    ///     options.IncludeExceptionTypes.Add("System.InvalidOperationException");
    /// });
    /// </code>
    /// </example>
    public sealed class FirstChanceExceptionOptions
    {
        /// <summary>
        /// Gets or sets whether first-chance exception monitoring is enabled.
        /// Default is <c>false</c> (opt-in only).
        /// </summary>
        public bool Enabled { get; set; } = false;

        /// <summary>
        /// Gets or sets the minimum log level at which first-chance exceptions are logged.
        /// Default is <see cref="LogLevel.Warning"/>.
        /// </summary>
        public LogLevel MinimumLogLevel { get; set; } = LogLevel.Warning;

        /// <summary>
        /// Gets or sets the maximum number of first-chance exception events to log per second.
        /// Exceptions beyond this limit are counted but not logged, preventing log flooding
        /// during exception storms. Default is <c>100</c>.
        /// </summary>
        public int MaxEventsPerSecond { get; set; } = 100;

        /// <summary>
        /// Gets or sets the list of exception type full names to include (whitelist).
        /// When non-empty, <strong>only</strong> exceptions whose type full name appears in
        /// this list will be monitored. An empty list means all types are eligible
        /// (subject to <see cref="ExcludeExceptionTypes"/>).
        /// </summary>
        /// <remarks>
        /// Use the full type name including namespace, e.g. <c>"System.InvalidOperationException"</c>.
        /// Matching is case-insensitive and checks the exception type and all base types.
        /// </remarks>
        public List<string> IncludeExceptionTypes { get; set; } = new List<string>();

        /// <summary>
        /// Gets or sets the list of exception type full names to exclude (blacklist).
        /// Exceptions whose type full name appears in this list are never monitored,
        /// even if they match <see cref="IncludeExceptionTypes"/>.
        /// </summary>
        /// <remarks>
        /// Default excludes common cancellation exceptions that are normal during shutdown.
        /// Use the full type name including namespace.
        /// </remarks>
        public List<string> ExcludeExceptionTypes { get; set; } = new List<string>
        {
            "System.OperationCanceledException",
            "System.Threading.Tasks.TaskCanceledException"
        };

        /// <summary>
        /// Gets or sets namespace prefixes for exception source filtering (whitelist).
        /// When non-empty, only exceptions whose throwing type's namespace starts with
        /// one of these prefixes will be monitored. An empty list means all namespaces
        /// are eligible (subject to <see cref="ExcludeNamespacePatterns"/>).
        /// </summary>
        public List<string> IncludeNamespacePatterns { get; set; } = new List<string>();

        /// <summary>
        /// Gets or sets namespace prefixes for exception source filtering (blacklist).
        /// Exceptions originating from types whose namespace starts with any of these
        /// prefixes are never monitored.
        /// </summary>
        public List<string> ExcludeNamespacePatterns { get; set; } = new List<string>();
    }
}
