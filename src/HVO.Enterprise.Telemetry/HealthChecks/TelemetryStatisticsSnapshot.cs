using System;
using System.Collections.Generic;
using System.Text;

namespace HVO.Enterprise.Telemetry.HealthChecks
{
    /// <summary>
    /// Immutable snapshot of telemetry statistics captured at a specific point in time.
    /// Thread-safe to read and suitable for serialization to monitoring systems.
    /// </summary>
    public sealed class TelemetryStatisticsSnapshot
    {
        /// <summary>
        /// Gets or sets the timestamp when this snapshot was captured.
        /// </summary>
        public DateTimeOffset Timestamp { get; set; }

        /// <summary>
        /// Gets or sets the timestamp when statistics collection started.
        /// </summary>
        public DateTimeOffset StartTime { get; set; }

        /// <summary>
        /// Gets the uptime of the telemetry system.
        /// </summary>
        public TimeSpan Uptime
        {
            get { return Timestamp - StartTime; }
        }

        /// <summary>
        /// Gets or sets the total number of activities created.
        /// </summary>
        public long ActivitiesCreated { get; set; }

        /// <summary>
        /// Gets or sets the total number of activities completed.
        /// </summary>
        public long ActivitiesCompleted { get; set; }

        /// <summary>
        /// Gets or sets the number of currently active (in-flight) activities.
        /// </summary>
        public long ActiveActivities { get; set; }

        /// <summary>
        /// Gets or sets the total number of exceptions tracked.
        /// </summary>
        public long ExceptionsTracked { get; set; }

        /// <summary>
        /// Gets or sets the total number of custom events recorded.
        /// </summary>
        public long EventsRecorded { get; set; }

        /// <summary>
        /// Gets or sets the total number of metric measurements recorded.
        /// </summary>
        public long MetricsRecorded { get; set; }

        /// <summary>
        /// Gets or sets the current depth of the background processing queue.
        /// </summary>
        public int QueueDepth { get; set; }

        /// <summary>
        /// Gets or sets the maximum queue depth reached since startup.
        /// </summary>
        public int MaxQueueDepth { get; set; }

        /// <summary>
        /// Gets or sets the total number of items enqueued for background processing.
        /// </summary>
        public long ItemsEnqueued { get; set; }

        /// <summary>
        /// Gets or sets the total number of items processed by background workers.
        /// </summary>
        public long ItemsProcessed { get; set; }

        /// <summary>
        /// Gets or sets the total number of items dropped due to queue overflow.
        /// </summary>
        public long ItemsDropped { get; set; }

        /// <summary>
        /// Gets or sets the number of background processing errors.
        /// </summary>
        public long ProcessingErrors { get; set; }

        /// <summary>
        /// Gets or sets the average time spent processing queue items in milliseconds.
        /// </summary>
        public double AverageProcessingTimeMs { get; set; }

        /// <summary>
        /// Gets or sets the number of correlation IDs generated.
        /// </summary>
        public long CorrelationIdsGenerated { get; set; }

        /// <summary>
        /// Gets or sets the current error rate (errors per second over last minute).
        /// </summary>
        public double CurrentErrorRate { get; set; }

        /// <summary>
        /// Gets or sets the current throughput (operations per second over last minute).
        /// </summary>
        public double CurrentThroughput { get; set; }

        /// <summary>
        /// Gets or sets the per-source statistics dictionary.
        /// </summary>
        public IReadOnlyDictionary<string, ActivitySourceStatistics> PerSourceStatistics { get; set; }
            = new Dictionary<string, ActivitySourceStatistics>();

        /// <summary>
        /// Formats statistics as human-readable text for logging and diagnostics.
        /// </summary>
        /// <returns>A formatted multi-line string containing all statistics.</returns>
        public string ToFormattedString()
        {
            var sb = new StringBuilder();
            sb.AppendLine(string.Format("Telemetry Statistics ({0:yyyy-MM-dd HH:mm:ss})", Timestamp));
            sb.AppendLine("========================================");
            sb.AppendLine(string.Format("Uptime: {0:F2}h", Uptime.TotalHours));
            sb.AppendLine();
            sb.AppendLine("Activities:");
            sb.AppendLine(string.Format("  Created: {0:N0}", ActivitiesCreated));
            sb.AppendLine(string.Format("  Completed: {0:N0}", ActivitiesCompleted));
            sb.AppendLine(string.Format("  Active: {0:N0}", ActiveActivities));
            sb.AppendLine();
            sb.AppendLine("Queue:");
            sb.AppendLine(string.Format("  Current Depth: {0:N0}", QueueDepth));
            sb.AppendLine(string.Format("  Max Depth: {0:N0}", MaxQueueDepth));
            sb.AppendLine(string.Format("  Enqueued: {0:N0}", ItemsEnqueued));
            sb.AppendLine(string.Format("  Processed: {0:N0}", ItemsProcessed));
            sb.AppendLine(string.Format("  Dropped: {0:N0}", ItemsDropped));
            sb.AppendLine(string.Format("  Avg Processing: {0:F2}ms", AverageProcessingTimeMs));
            sb.AppendLine();
            sb.AppendLine("Errors & Events:");
            sb.AppendLine(string.Format("  Exceptions: {0:N0}", ExceptionsTracked));
            sb.AppendLine(string.Format("  Events: {0:N0}", EventsRecorded));
            sb.AppendLine(string.Format("  Metrics: {0:N0}", MetricsRecorded));
            sb.AppendLine(string.Format("  Processing Errors: {0:N0}", ProcessingErrors));
            sb.AppendLine();
            sb.AppendLine("Rates:");
            sb.AppendLine(string.Format("  Error Rate: {0:F2}/sec", CurrentErrorRate));
            sb.Append(string.Format("  Throughput: {0:F2}/sec", CurrentThroughput));

            return sb.ToString();
        }
    }
}
