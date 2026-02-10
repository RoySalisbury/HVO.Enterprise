using System;
using System.Collections.Generic;

namespace HVO.Enterprise.Telemetry.Datadog
{
    /// <summary>
    /// Configuration options for Datadog telemetry integration.
    /// </summary>
    /// <remarks>
    /// All properties support environment-variable fallbacks following Datadog conventions
    /// (<c>DD_SERVICE</c>, <c>DD_ENV</c>, <c>DD_VERSION</c>, etc.).
    /// Call <see cref="ApplyEnvironmentDefaults"/> to merge environment values into this instance.
    /// </remarks>
    public sealed class DatadogOptions
    {
        /// <summary>
        /// Gets or sets the Datadog service name.
        /// Falls back to <c>DD_SERVICE</c> environment variable when <see langword="null"/>.
        /// </summary>
        public string? ServiceName { get; set; }

        /// <summary>
        /// Gets or sets the Datadog environment tag (e.g., <c>"production"</c>, <c>"staging"</c>).
        /// Falls back to <c>DD_ENV</c> environment variable when <see langword="null"/>.
        /// </summary>
        public string? Environment { get; set; }

        /// <summary>
        /// Gets or sets the service version tag.
        /// Falls back to <c>DD_VERSION</c> environment variable when <see langword="null"/>.
        /// </summary>
        public string? Version { get; set; }

        /// <summary>
        /// Gets or sets the Datadog agent host.
        /// Falls back to <c>DD_AGENT_HOST</c> environment variable, then <c>"localhost"</c>.
        /// Default: <c>"localhost"</c>.
        /// </summary>
        public string AgentHost { get; set; } = "localhost";

        /// <summary>
        /// Gets or sets the DogStatsD UDP port.
        /// Falls back to <c>DD_DOGSTATSD_PORT</c> environment variable.
        /// Default: <c>8125</c>.
        /// </summary>
        public int AgentPort { get; set; } = 8125;

        /// <summary>
        /// Gets or sets the Unix domain socket path for DogStatsD (Linux only).
        /// Falls back to <c>DD_DOGSTATSD_SOCKET</c> environment variable.
        /// When set, takes precedence over UDP transport on supported platforms.
        /// </summary>
        public string? UnixDomainSocketPath { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to use Unix domain socket transport.
        /// Automatically set to <see langword="true"/> when <see cref="UnixDomainSocketPath"/> is configured
        /// or <c>DD_DOGSTATSD_SOCKET</c> environment variable is set.
        /// </summary>
        public bool UseUnixDomainSocket { get; set; }

        /// <summary>
        /// Gets or sets the export mode.
        /// Default: <see cref="DatadogExportMode.Auto"/>.
        /// </summary>
        /// <remarks>
        /// <list type="bullet">
        /// <item><description><see cref="DatadogExportMode.Auto"/>: auto-detects based on platform and OTLP configuration.</description></item>
        /// <item><description><see cref="DatadogExportMode.OTLP"/>: requires OpenTelemetry SDK.</description></item>
        /// <item><description><see cref="DatadogExportMode.DogStatsD"/>: uses native DogStatsD protocol.</description></item>
        /// </list>
        /// </remarks>
        public DatadogExportMode Mode { get; set; } = DatadogExportMode.Auto;

        /// <summary>
        /// Gets or sets global tags to apply to all telemetry.
        /// Unified service tags (<c>service</c>, <c>env</c>, <c>version</c>) are added automatically
        /// by <see cref="ApplyEnvironmentDefaults"/>.
        /// </summary>
        public Dictionary<string, string> GlobalTags { get; set; } = new Dictionary<string, string>();

        /// <summary>
        /// Gets or sets a value indicating whether to register the <see cref="DatadogMetricsExporter"/>.
        /// Default: <see langword="true"/>.
        /// </summary>
        public bool EnableMetricsExporter { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether to register the <see cref="DatadogTraceExporter"/>.
        /// Default: <see langword="true"/>.
        /// </summary>
        public bool EnableTraceExporter { get; set; } = true;

        /// <summary>
        /// Gets or sets an optional prefix prepended to all DogStatsD metric names.
        /// </summary>
        public string? MetricPrefix { get; set; }

        /// <summary>
        /// Merges environment-variable values into this options instance.
        /// Properties that are already set explicitly are not overwritten.
        /// Also populates <see cref="GlobalTags"/> with unified service tags.
        /// </summary>
        internal void ApplyEnvironmentDefaults()
        {
            ServiceName ??= System.Environment.GetEnvironmentVariable("DD_SERVICE");
            Environment ??= System.Environment.GetEnvironmentVariable("DD_ENV");
            Version ??= System.Environment.GetEnvironmentVariable("DD_VERSION");

            var agentHost = System.Environment.GetEnvironmentVariable("DD_AGENT_HOST");
            if (!string.IsNullOrEmpty(agentHost) && AgentHost == "localhost")
            {
                AgentHost = agentHost;
            }

            var agentPort = System.Environment.GetEnvironmentVariable("DD_DOGSTATSD_PORT");
            if (!string.IsNullOrEmpty(agentPort)
                && int.TryParse(agentPort, out var port)
                && AgentPort == 8125)
            {
                AgentPort = port;
            }

            var socketPath = System.Environment.GetEnvironmentVariable("DD_DOGSTATSD_SOCKET");
            if (!string.IsNullOrEmpty(socketPath) && string.IsNullOrEmpty(UnixDomainSocketPath))
            {
                UnixDomainSocketPath = socketPath;
                UseUnixDomainSocket = true;
            }

            // Add unified service tags
            if (!string.IsNullOrEmpty(ServiceName) && !GlobalTags.ContainsKey("service"))
            {
                GlobalTags["service"] = ServiceName;
            }
            if (!string.IsNullOrEmpty(Environment) && !GlobalTags.ContainsKey("env"))
            {
                GlobalTags["env"] = Environment;
            }
            if (!string.IsNullOrEmpty(Version) && !GlobalTags.ContainsKey("version"))
            {
                GlobalTags["version"] = Version;
            }
        }

        /// <summary>
        /// Returns the effective DogStatsD server name, accounting for Unix domain socket transport.
        /// </summary>
        internal string GetEffectiveServerName()
        {
            if (UseUnixDomainSocket && !string.IsNullOrEmpty(UnixDomainSocketPath))
            {
                return UnixDomainSocketPath!.StartsWith("unix://", StringComparison.OrdinalIgnoreCase)
                    ? UnixDomainSocketPath
                    : "unix://" + UnixDomainSocketPath;
            }

            return AgentHost;
        }
    }
}
