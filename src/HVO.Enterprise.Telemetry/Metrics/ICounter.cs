namespace HVO.Enterprise.Telemetry.Metrics
{
    /// <summary>
    /// Counter that only increases over time (monotonic).
    /// </summary>
    /// <typeparam name="T">Value type.</typeparam>
    public interface ICounter<T> where T : struct
    {
        /// <summary>
        /// Adds the provided value.
        /// </summary>
        /// <param name="value">Value to add.</param>
        void Add(T value);

        /// <summary>
        /// Adds the provided value with one tag.
        /// </summary>
        /// <param name="value">Value to add.</param>
        /// <param name="tag1">First tag.</param>
        void Add(T value, in MetricTag tag1);

        /// <summary>
        /// Adds the provided value with two tags.
        /// </summary>
        /// <param name="value">Value to add.</param>
        /// <param name="tag1">First tag.</param>
        /// <param name="tag2">Second tag.</param>
        void Add(T value, in MetricTag tag1, in MetricTag tag2);

        /// <summary>
        /// Adds the provided value with three tags.
        /// </summary>
        /// <param name="value">Value to add.</param>
        /// <param name="tag1">First tag.</param>
        /// <param name="tag2">Second tag.</param>
        /// <param name="tag3">Third tag.</param>
        void Add(T value, in MetricTag tag1, in MetricTag tag2, in MetricTag tag3);

        /// <summary>
        /// Adds the provided value with an arbitrary number of tags.
        /// </summary>
        /// <param name="value">Value to add.</param>
        /// <param name="tags">Tags to associate.</param>
        void Add(T value, params MetricTag[] tags);
    }
}
