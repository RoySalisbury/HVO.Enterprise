using System;
using System.Collections.Generic;
using HVO.Enterprise.Telemetry.HealthChecks;

namespace HVO.Enterprise.Telemetry.Abstractions
{
    /// <summary>
    /// Provides real-time statistics about the telemetry system's operation.
    /// </summary>
    public interface ITelemetryStatistics
    {
        /// <summary>
        /// Gets the timestamp when statistics collection started.
        /// </summary>
        DateTimeOffset StartTime { get; }

        /// <summary>
        /// Gets the total number of activities created since startup.
        /// </summary>
        long ActivitiesCreated { get; }

        /// <summary>
        /// Gets the total number of activities completed.
        /// </summary>
        long ActivitiesCompleted { get; }

        /// <summary>
        /// Gets the number of currently active (in-flight) activities.
        /// </summary>
        long ActiveActivities { get; }

        /// <summary>
        /// Gets the total number of exceptions tracked.
        /// </summary>
        long ExceptionsTracked { get; }

        /// <summary>
        /// Gets the total number of custom events recorded.
        /// </summary>
        long EventsRecorded { get; }

        /// <summary>
        /// Gets the total number of metric measurements recorded.
        /// </summary>
        long MetricsRecorded { get; }

        /// <summary>
        /// Gets the current depth of the background processing queue.
        /// </summary>
        int QueueDepth { get; }

        /// <summary>
        /// Gets the maximum queue depth reached since startup.
        /// </summary>
        int MaxQueueDepth { get; }

        /// <summary>
        /// Gets the total number of items enqueued for background processing.
        /// </summary>
        long ItemsEnqueued { get; }

        /// <summary>
        /// Gets the total number of items processed by background workers.
        /// </summary>
        long ItemsProcessed { get; }

        /// <summary>
        /// Gets the total number of items dropped due to queue overflow.
        /// </summary>
        long ItemsDropped { get; }

        /// <summary>
        /// Gets the number of background processing errors.
        /// </summary>
        long ProcessingErrors { get; }

        /// <summary>
        /// Gets the average time spent processing queue items (milliseconds).
        /// </summary>
        double AverageProcessingTimeMs { get; }

        /// <summary>
        /// Gets the number of correlation IDs generated.
        /// </summary>
        long CorrelationIdsGenerated { get; }

        /// <summary>
        /// Gets the current error rate (errors per second over last minute).
        /// </summary>
        double CurrentErrorRate { get; }

        /// <summary>
        /// Gets the current throughput (operations per second over last minute).
        /// </summary>
        double CurrentThroughput { get; }

        /// <summary>
        /// Gets a dictionary of per-source statistics.
        /// </summary>
        IReadOnlyDictionary<string, ActivitySourceStatistics> PerSourceStatistics { get; }

        /// <summary>
        /// Captures a point-in-time snapshot of all statistics.
        /// </summary>
        /// <returns>An immutable snapshot of the current statistics.</returns>
        TelemetryStatisticsSnapshot GetSnapshot();

        /// <summary>
        /// Resets all counters to zero (administrative use only).
        /// </summary>
        void Reset();
    }
}
