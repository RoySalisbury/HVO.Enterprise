namespace HVO.Enterprise.Telemetry.Metrics
{
    /// <summary>
    /// Histogram for recording distribution of values.
    /// </summary>
    /// <typeparam name="T">Value type.</typeparam>
    public interface IHistogram<T> where T : struct
    {
        /// <summary>
        /// Records the provided value.
        /// </summary>
        /// <param name="value">Value to record.</param>
        void Record(T value);

        /// <summary>
        /// Records the provided value with one tag.
        /// </summary>
        /// <param name="value">Value to record.</param>
        /// <param name="tag1">First tag.</param>
        void Record(T value, in MetricTag tag1);

        /// <summary>
        /// Records the provided value with two tags.
        /// </summary>
        /// <param name="value">Value to record.</param>
        /// <param name="tag1">First tag.</param>
        /// <param name="tag2">Second tag.</param>
        void Record(T value, in MetricTag tag1, in MetricTag tag2);

        /// <summary>
        /// Records the provided value with three tags.
        /// </summary>
        /// <param name="value">Value to record.</param>
        /// <param name="tag1">First tag.</param>
        /// <param name="tag2">Second tag.</param>
        /// <param name="tag3">Third tag.</param>
        void Record(T value, in MetricTag tag1, in MetricTag tag2, in MetricTag tag3);

        /// <summary>
        /// Records the provided value with an arbitrary number of tags.
        /// </summary>
        /// <param name="value">Value to record.</param>
        /// <param name="tags">Tags to associate.</param>
        void Record(T value, params MetricTag[] tags);
    }
}
