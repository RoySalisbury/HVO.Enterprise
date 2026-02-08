using System;
using System.Collections.Generic;

namespace HVO.Enterprise.Telemetry.Configuration
{
    /// <summary>
    /// Represents configuration for a telemetry operation.
    /// Supports hierarchical precedence: Call > Method > Type > Namespace > Global.
    /// </summary>
    public sealed class OperationConfiguration
    {
        /// <summary>
        /// Gets or sets the sampling rate (0.0 to 1.0). Null means inherit from parent.
        /// </summary>
        public double? SamplingRate { get; set; }

        /// <summary>
        /// Gets or sets whether instrumentation is enabled. Null means inherit from parent.
        /// </summary>
        public bool? Enabled { get; set; }

        /// <summary>
        /// Gets or sets parameter capture mode. Null means inherit from parent.
        /// </summary>
        public ParameterCaptureMode? ParameterCapture { get; set; }

        /// <summary>
        /// Gets or sets custom tags to add to operations.
        /// </summary>
        public Dictionary<string, object?> Tags { get; set; } =
            new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Gets or sets timeout threshold in milliseconds. Null means no threshold.
        /// </summary>
        public int? TimeoutThresholdMs { get; set; }

        /// <summary>
        /// Gets or sets whether to record exceptions. Null means inherit from parent.
        /// </summary>
        public bool? RecordExceptions { get; set; }

        /// <summary>
        /// Merges this configuration with a parent configuration.
        /// This configuration takes precedence over parent.
        /// </summary>
        /// <param name="parent">Parent configuration.</param>
        /// <returns>Merged configuration.</returns>
        public OperationConfiguration MergeWith(OperationConfiguration? parent)
        {
            if (parent == null)
                return Clone();

            EnsureDefaults();
            parent.EnsureDefaults();

            return new OperationConfiguration
            {
                SamplingRate = SamplingRate ?? parent.SamplingRate,
                Enabled = Enabled ?? parent.Enabled,
                ParameterCapture = ParameterCapture ?? parent.ParameterCapture,
                TimeoutThresholdMs = TimeoutThresholdMs ?? parent.TimeoutThresholdMs,
                RecordExceptions = RecordExceptions ?? parent.RecordExceptions,
                Tags = MergeTags(parent.Tags, Tags)
            };
        }

        /// <summary>
        /// Validates configuration values.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown when values are invalid.</exception>
        public void Validate()
        {
            EnsureDefaults();

            if (SamplingRate.HasValue && (SamplingRate.Value < 0.0 || SamplingRate.Value > 1.0))
                throw new InvalidOperationException("SamplingRate must be between 0.0 and 1.0.");

            if (TimeoutThresholdMs.HasValue && TimeoutThresholdMs.Value < 0)
                throw new InvalidOperationException("TimeoutThresholdMs must be non-negative.");
        }

        /// <summary>
        /// Creates a copy of this configuration.
        /// </summary>
        /// <returns>Cloned configuration.</returns>
        public OperationConfiguration Clone()
        {
            EnsureDefaults();

            return new OperationConfiguration
            {
                SamplingRate = SamplingRate,
                Enabled = Enabled,
                ParameterCapture = ParameterCapture,
                TimeoutThresholdMs = TimeoutThresholdMs,
                RecordExceptions = RecordExceptions,
                Tags = new Dictionary<string, object?>(Tags, StringComparer.OrdinalIgnoreCase)
            };
        }

        internal void EnsureDefaults()
        {
            Tags ??= new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        }

        private static Dictionary<string, object?> MergeTags(
            Dictionary<string, object?> parentTags,
            Dictionary<string, object?> childTags)
        {
            var merged = new Dictionary<string, object?>(parentTags, StringComparer.OrdinalIgnoreCase);

            foreach (var kvp in childTags)
            {
                merged[kvp.Key] = kvp.Value;
            }

            return merged;
        }
    }
}
