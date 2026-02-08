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
        /// Gets whether this tag is valid (has a non-null key).
        /// </summary>
        public bool IsValid => !string.IsNullOrEmpty(Key);

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

        /// <summary>
        /// Validates that this tag is not default and has a valid key.
        /// </summary>
        /// <exception cref="ArgumentException">Thrown when the tag is default or has a null/empty key.</exception>
        public void Validate()
        {
            if (!IsValid)
                throw new ArgumentException("MetricTag is invalid or default. Use a properly initialized tag.");
        }

        /// <summary>
        /// Validates an array of tags to ensure none are default or invalid.
        /// </summary>
        /// <param name="tags">The tags to validate.</param>
        /// <exception cref="ArgumentException">Thrown when any tag is invalid or default.</exception>
        internal static void ValidateTags(MetricTag[] tags)
        {
            for (int i = 0; i < tags.Length; i++)
            {
                tags[i].Validate();
            }
        }
    }
}
