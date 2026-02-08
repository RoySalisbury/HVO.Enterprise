using System;

namespace HVO.Enterprise.Telemetry.Metrics
{
    /// <summary>
    /// Tag (dimension) for metrics. Designed to be low allocation.
    /// </summary>
    public readonly struct MetricTag
    {
        /// <summary>
        /// Gets the tag key.
        /// </summary>
        public string Key { get; }

        /// <summary>
        /// Gets the tag value.
        /// </summary>
        public object? Value { get; }

        /// <summary>
        /// Creates a new tag with the provided key and value.
        /// </summary>
        /// <param name="key">Tag key.</param>
        /// <param name="value">Tag value.</param>
        /// <exception cref="ArgumentException">Thrown when the key is null or empty.</exception>
        public MetricTag(string key, object? value)
        {
            if (string.IsNullOrEmpty(key))
                throw new ArgumentException("Tag key must be non-empty.", nameof(key));

            Key = key;
            Value = value;
        }
    }
}
