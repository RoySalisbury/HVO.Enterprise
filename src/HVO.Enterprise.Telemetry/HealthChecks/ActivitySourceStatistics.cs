namespace HVO.Enterprise.Telemetry.HealthChecks
{
    /// <summary>
    /// Per-ActivitySource statistics.
    /// </summary>
    public sealed class ActivitySourceStatistics
    {
        /// <summary>
        /// Gets or sets the name of the activity source.
        /// </summary>
        public string SourceName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the total number of activities created for this source.
        /// </summary>
        public long ActivitiesCreated { get; set; }

        /// <summary>
        /// Gets or sets the total number of activities completed for this source.
        /// </summary>
        public long ActivitiesCompleted { get; set; }

        /// <summary>
        /// Gets or sets the average duration of completed activities in milliseconds.
        /// </summary>
        public double AverageDurationMs { get; set; }
    }
}
