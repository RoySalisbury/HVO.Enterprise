using System;
using System.Collections.Generic;
using System.Text;

namespace HVO.Enterprise.Telemetry.Configuration
{
    /// <summary>
    /// Represents a diagnostic report for configuration evaluation.
    /// </summary>
    public sealed class ConfigurationDiagnosticReport
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ConfigurationDiagnosticReport"/> class.
        /// </summary>
        /// <param name="layers">Applied configuration layers.</param>
        /// <param name="effectiveConfiguration">Effective configuration.</param>
        public ConfigurationDiagnosticReport(
            IReadOnlyList<ConfigurationLayer> layers,
            OperationConfiguration effectiveConfiguration)
        {
            Layers = layers ?? throw new ArgumentNullException(nameof(layers));
            EffectiveConfiguration = effectiveConfiguration ?? throw new ArgumentNullException(nameof(effectiveConfiguration));
        }

        /// <summary>
        /// Gets the applied configuration layers in order.
        /// </summary>
        public IReadOnlyList<ConfigurationLayer> Layers { get; }

        /// <summary>
        /// Gets the effective configuration.
        /// </summary>
        public OperationConfiguration EffectiveConfiguration { get; }

        /// <summary>
        /// Formats the report as a readable string.
        /// </summary>
        /// <returns>Formatted report.</returns>
        public string ToReportString()
        {
            var sb = new StringBuilder();
            sb.AppendLine("Configuration Precedence Chain:");
            sb.AppendLine();

            for (int i = 0; i < Layers.Count; i++)
            {
                var layer = Layers[i];
                var label = layer.Level + " (" + layer.Source + ")";
                sb.AppendLine((i + 1) + ". " + label + (string.IsNullOrEmpty(layer.Identifier) ? string.Empty : ": " + layer.Identifier));
                AppendConfiguration(sb, layer.Configuration);
                sb.AppendLine();
            }

            sb.AppendLine("Effective Configuration:");
            AppendConfiguration(sb, EffectiveConfiguration);

            return sb.ToString();
        }

        private static void AppendConfiguration(StringBuilder sb, OperationConfiguration configuration)
        {
            sb.AppendLine("   SamplingRate: " + (configuration.SamplingRate.HasValue ? configuration.SamplingRate.Value.ToString("F2") : "(inherit)"));
            sb.AppendLine("   Enabled: " + (configuration.Enabled.HasValue ? configuration.Enabled.Value.ToString() : "(inherit)"));
            sb.AppendLine("   ParameterCapture: " + (configuration.ParameterCapture.HasValue ? configuration.ParameterCapture.Value.ToString() : "(inherit)"));
            sb.AppendLine("   TimeoutThresholdMs: " + (configuration.TimeoutThresholdMs.HasValue ? configuration.TimeoutThresholdMs.Value.ToString() : "(inherit)"));
            sb.AppendLine("   RecordExceptions: " + (configuration.RecordExceptions.HasValue ? configuration.RecordExceptions.Value.ToString() : "(inherit)"));
            sb.AppendLine("   Tags: " + configuration.Tags.Count);
        }
    }
}
