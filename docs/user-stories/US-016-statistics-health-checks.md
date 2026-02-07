# US-016: Statistics and Health Checks

**Status**: ❌ Not Started  
**Category**: Core Package  
**Effort**: 5 story points  
**Sprint**: 4

## Description

As a **DevOps engineer or developer monitoring application health**,  
I want **real-time telemetry statistics and ASP.NET Core health check integration**,  
So that **I can monitor telemetry system health, detect issues, and troubleshoot performance problems in production**.

## Acceptance Criteria

1. **Telemetry Statistics API**
   - [ ] `ITelemetryStatistics` interface defined with comprehensive metrics
   - [ ] `TelemetryStatistics` implementation tracks all key metrics
   - [ ] Statistics available through `Telemetry.Statistics` property
   - [ ] Thread-safe atomic counters for concurrent access
   - [ ] Zero allocation on read operations

2. **Health Check Integration**
   - [ ] `TelemetryHealthCheck` implements `IHealthCheck`
   - [ ] Health status determined by error rates and queue depth
   - [ ] Configurable thresholds for warning/unhealthy states
   - [ ] Detailed health report with statistics snapshot
   - [ ] Works with ASP.NET Core health check middleware

3. **Performance Metrics**
   - [ ] Activity creation count and duration statistics
   - [ ] Queue depth, enqueue/dequeue rates
   - [ ] Background worker throughput
   - [ ] Error rates and exception counts
   - [ ] Memory allocation tracking (optional)

4. **Diagnostic APIs**
   - [ ] `GetSnapshot()` captures point-in-time statistics
   - [ ] `Reset()` clears all counters (admin only)
   - [ ] Statistics formatted for logging and monitoring
   - [ ] JSON serialization support for HTTP endpoints

## Technical Requirements

### ITelemetryStatistics Interface

```csharp
using System;
using System.Collections.Generic;

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
        TelemetryStatisticsSnapshot GetSnapshot();

        /// <summary>
        /// Resets all counters to zero (administrative use only).
        /// </summary>
        void Reset();
    }

    /// <summary>
    /// Per-ActivitySource statistics.
    /// </summary>
    public sealed class ActivitySourceStatistics
    {
        public string SourceName { get; init; } = string.Empty;
        public long ActivitiesCreated { get; init; }
        public long ActivitiesCompleted { get; init; }
        public double AverageDurationMs { get; init; }
    }

    /// <summary>
    /// Immutable snapshot of telemetry statistics.
    /// </summary>
    public sealed class TelemetryStatisticsSnapshot
    {
        public DateTimeOffset Timestamp { get; init; }
        public DateTimeOffset StartTime { get; init; }
        public TimeSpan Uptime => Timestamp - StartTime;
        
        public long ActivitiesCreated { get; init; }
        public long ActivitiesCompleted { get; init; }
        public long ActiveActivities { get; init; }
        public long ExceptionsTracked { get; init; }
        public long EventsRecorded { get; init; }
        public long MetricsRecorded { get; init; }
        
        public int QueueDepth { get; init; }
        public int MaxQueueDepth { get; init; }
        public long ItemsEnqueued { get; init; }
        public long ItemsProcessed { get; init; }
        public long ItemsDropped { get; init; }
        public long ProcessingErrors { get; init; }
        public double AverageProcessingTimeMs { get; init; }
        
        public long CorrelationIdsGenerated { get; init; }
        public double CurrentErrorRate { get; init; }
        public double CurrentThroughput { get; init; }
        
        public IReadOnlyDictionary<string, ActivitySourceStatistics> PerSourceStatistics { get; init; } 
            = new Dictionary<string, ActivitySourceStatistics>();

        /// <summary>
        /// Formats statistics as human-readable text.
        /// </summary>
        public string ToFormattedString()
        {
            return $"""
                Telemetry Statistics ({Timestamp:yyyy-MM-dd HH:mm:ss})
                ========================================
                Uptime: {Uptime.TotalHours:F2}h
                
                Activities:
                  Created: {ActivitiesCreated:N0}
                  Completed: {ActivitiesCompleted:N0}
                  Active: {ActiveActivities:N0}
                
                Queue:
                  Current Depth: {QueueDepth:N0}
                  Max Depth: {MaxQueueDepth:N0}
                  Enqueued: {ItemsEnqueued:N0}
                  Processed: {ItemsProcessed:N0}
                  Dropped: {ItemsDropped:N0}
                  Avg Processing: {AverageProcessingTimeMs:F2}ms
                
                Errors & Events:
                  Exceptions: {ExceptionsTracked:N0}
                  Events: {EventsRecorded:N0}
                  Metrics: {MetricsRecorded:N0}
                  Processing Errors: {ProcessingErrors:N0}
                
                Rates:
                  Error Rate: {CurrentErrorRate:F2}/sec
                  Throughput: {CurrentThroughput:F2}/sec
                """;
        }
    }
}
```

### TelemetryStatistics Implementation

```csharp
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace HVO.Enterprise.Telemetry
{
    /// <summary>
    /// Thread-safe implementation of telemetry statistics tracking.
    /// </summary>
    internal sealed class TelemetryStatistics : ITelemetryStatistics
    {
        private readonly DateTimeOffset _startTime = DateTimeOffset.UtcNow;
        private readonly ConcurrentDictionary<string, SourceStats> _sourceStats = new();
        private readonly RollingWindow _errorWindow = new(TimeSpan.FromMinutes(1));
        private readonly RollingWindow _throughputWindow = new(TimeSpan.FromMinutes(1));

        // Atomic counters for thread-safe increments
        private long _activitiesCreated;
        private long _activitiesCompleted;
        private long _exceptionsTracked;
        private long _eventsRecorded;
        private long _metricsRecorded;
        private long _itemsEnqueued;
        private long _itemsProcessed;
        private long _itemsDropped;
        private long _processingErrors;
        private long _correlationIdsGenerated;
        private int _queueDepth;
        private int _maxQueueDepth;

        // For average calculation
        private long _totalProcessingTimeMs;
        private long _processingCount;

        public DateTimeOffset StartTime => _startTime;
        public long ActivitiesCreated => Interlocked.Read(ref _activitiesCreated);
        public long ActivitiesCompleted => Interlocked.Read(ref _activitiesCompleted);
        public long ActiveActivities => ActivitiesCreated - ActivitiesCompleted;
        public long ExceptionsTracked => Interlocked.Read(ref _exceptionsTracked);
        public long EventsRecorded => Interlocked.Read(ref _eventsRecorded);
        public long MetricsRecorded => Interlocked.Read(ref _metricsRecorded);
        public int QueueDepth => Interlocked.CompareExchange(ref _queueDepth, 0, 0);
        public int MaxQueueDepth => Interlocked.CompareExchange(ref _maxQueueDepth, 0, 0);
        public long ItemsEnqueued => Interlocked.Read(ref _itemsEnqueued);
        public long ItemsProcessed => Interlocked.Read(ref _itemsProcessed);
        public long ItemsDropped => Interlocked.Read(ref _itemsDropped);
        public long ProcessingErrors => Interlocked.Read(ref _processingErrors);
        public long CorrelationIdsGenerated => Interlocked.Read(ref _correlationIdsGenerated);
        
        public double AverageProcessingTimeMs
        {
            get
            {
                var count = Interlocked.Read(ref _processingCount);
                if (count == 0) return 0;
                var total = Interlocked.Read(ref _totalProcessingTimeMs);
                return (double)total / count;
            }
        }

        public double CurrentErrorRate => _errorWindow.GetRate();
        public double CurrentThroughput => _throughputWindow.GetRate();

        public IReadOnlyDictionary<string, ActivitySourceStatistics> PerSourceStatistics =>
            _sourceStats.ToDictionary(
                kvp => kvp.Key,
                kvp => new ActivitySourceStatistics
                {
                    SourceName = kvp.Key,
                    ActivitiesCreated = kvp.Value.Created,
                    ActivitiesCompleted = kvp.Value.Completed,
                    AverageDurationMs = kvp.Value.AverageDurationMs
                });

        // Internal increment methods (called by telemetry system)
        internal void IncrementActivitiesCreated(string sourceName)
        {
            Interlocked.Increment(ref _activitiesCreated);
            _throughputWindow.Record(DateTimeOffset.UtcNow);
            
            var stats = _sourceStats.GetOrAdd(sourceName, _ => new SourceStats());
            stats.IncrementCreated();
        }

        internal void IncrementActivitiesCompleted(string sourceName, TimeSpan duration)
        {
            Interlocked.Increment(ref _activitiesCompleted);
            
            if (_sourceStats.TryGetValue(sourceName, out var stats))
            {
                stats.IncrementCompleted(duration.TotalMilliseconds);
            }
        }

        internal void IncrementExceptionsTracked()
        {
            Interlocked.Increment(ref _exceptionsTracked);
            _errorWindow.Record(DateTimeOffset.UtcNow);
        }

        internal void IncrementEventsRecorded() => 
            Interlocked.Increment(ref _eventsRecorded);

        internal void IncrementMetricsRecorded() => 
            Interlocked.Increment(ref _metricsRecorded);

        internal void IncrementItemsEnqueued() => 
            Interlocked.Increment(ref _itemsEnqueued);

        internal void IncrementItemsProcessed(TimeSpan processingTime)
        {
            Interlocked.Increment(ref _itemsProcessed);
            Interlocked.Add(ref _totalProcessingTimeMs, (long)processingTime.TotalMilliseconds);
            Interlocked.Increment(ref _processingCount);
        }

        internal void IncrementItemsDropped() => 
            Interlocked.Increment(ref _itemsDropped);

        internal void IncrementProcessingErrors()
        {
            Interlocked.Increment(ref _processingErrors);
            _errorWindow.Record(DateTimeOffset.UtcNow);
        }

        internal void IncrementCorrelationIdsGenerated() => 
            Interlocked.Increment(ref _correlationIdsGenerated);

        internal void UpdateQueueDepth(int newDepth)
        {
            Interlocked.Exchange(ref _queueDepth, newDepth);
            
            // Update max if needed
            int currentMax;
            do
            {
                currentMax = Interlocked.CompareExchange(ref _maxQueueDepth, 0, 0);
                if (newDepth <= currentMax) break;
            }
            while (Interlocked.CompareExchange(ref _maxQueueDepth, newDepth, currentMax) != currentMax);
        }

        public TelemetryStatisticsSnapshot GetSnapshot()
        {
            return new TelemetryStatisticsSnapshot
            {
                Timestamp = DateTimeOffset.UtcNow,
                StartTime = StartTime,
                ActivitiesCreated = ActivitiesCreated,
                ActivitiesCompleted = ActivitiesCompleted,
                ActiveActivities = ActiveActivities,
                ExceptionsTracked = ExceptionsTracked,
                EventsRecorded = EventsRecorded,
                MetricsRecorded = MetricsRecorded,
                QueueDepth = QueueDepth,
                MaxQueueDepth = MaxQueueDepth,
                ItemsEnqueued = ItemsEnqueued,
                ItemsProcessed = ItemsProcessed,
                ItemsDropped = ItemsDropped,
                ProcessingErrors = ProcessingErrors,
                AverageProcessingTimeMs = AverageProcessingTimeMs,
                CorrelationIdsGenerated = CorrelationIdsGenerated,
                CurrentErrorRate = CurrentErrorRate,
                CurrentThroughput = CurrentThroughput,
                PerSourceStatistics = PerSourceStatistics
            };
        }

        public void Reset()
        {
            Interlocked.Exchange(ref _activitiesCreated, 0);
            Interlocked.Exchange(ref _activitiesCompleted, 0);
            Interlocked.Exchange(ref _exceptionsTracked, 0);
            Interlocked.Exchange(ref _eventsRecorded, 0);
            Interlocked.Exchange(ref _metricsRecorded, 0);
            Interlocked.Exchange(ref _itemsEnqueued, 0);
            Interlocked.Exchange(ref _itemsProcessed, 0);
            Interlocked.Exchange(ref _itemsDropped, 0);
            Interlocked.Exchange(ref _processingErrors, 0);
            Interlocked.Exchange(ref _correlationIdsGenerated, 0);
            Interlocked.Exchange(ref _queueDepth, 0);
            Interlocked.Exchange(ref _maxQueueDepth, 0);
            Interlocked.Exchange(ref _totalProcessingTimeMs, 0);
            Interlocked.Exchange(ref _processingCount, 0);
            
            _sourceStats.Clear();
            _errorWindow.Clear();
            _throughputWindow.Clear();
        }

        private sealed class SourceStats
        {
            private long _created;
            private long _completed;
            private long _totalDurationMs;

            public long Created => Interlocked.Read(ref _created);
            public long Completed => Interlocked.Read(ref _completed);
            
            public double AverageDurationMs
            {
                get
                {
                    var count = Interlocked.Read(ref _completed);
                    if (count == 0) return 0;
                    var total = Interlocked.Read(ref _totalDurationMs);
                    return (double)total / count;
                }
            }

            public void IncrementCreated() => Interlocked.Increment(ref _created);
            
            public void IncrementCompleted(double durationMs)
            {
                Interlocked.Increment(ref _completed);
                Interlocked.Add(ref _totalDurationMs, (long)durationMs);
            }
        }

        // Rolling window for rate calculations
        private sealed class RollingWindow
        {
            private readonly TimeSpan _duration;
            private readonly ConcurrentQueue<DateTimeOffset> _timestamps = new();

            public RollingWindow(TimeSpan duration)
            {
                _duration = duration;
            }

            public void Record(DateTimeOffset timestamp)
            {
                _timestamps.Enqueue(timestamp);
                CleanOld(timestamp);
            }

            public double GetRate()
            {
                var now = DateTimeOffset.UtcNow;
                CleanOld(now);
                var count = _timestamps.Count;
                return count / _duration.TotalSeconds;
            }

            public void Clear()
            {
                while (_timestamps.TryDequeue(out _)) { }
            }

            private void CleanOld(DateTimeOffset now)
            {
                var cutoff = now - _duration;
                while (_timestamps.TryPeek(out var timestamp) && timestamp < cutoff)
                {
                    _timestamps.TryDequeue(out _);
                }
            }
        }
    }
}
```

### TelemetryHealthCheck Implementation

```csharp
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace HVO.Enterprise.Telemetry.HealthChecks
{
    /// <summary>
    /// ASP.NET Core health check for the telemetry system.
    /// </summary>
    public sealed class TelemetryHealthCheck : IHealthCheck
    {
        private readonly ITelemetryStatistics _statistics;
        private readonly TelemetryHealthCheckOptions _options;

        public TelemetryHealthCheck(
            ITelemetryStatistics statistics,
            TelemetryHealthCheckOptions? options = null)
        {
            _statistics = statistics ?? throw new ArgumentNullException(nameof(statistics));
            _options = options ?? TelemetryHealthCheckOptions.Default;
        }

        public Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context,
            CancellationToken cancellationToken = default)
        {
            var snapshot = _statistics.GetSnapshot();
            var status = DetermineHealthStatus(snapshot);
            var data = BuildHealthData(snapshot);
            var description = BuildDescription(snapshot, status);

            var result = new HealthCheckResult(
                status,
                description,
                data: data);

            return Task.FromResult(result);
        }

        private HealthStatus DetermineHealthStatus(TelemetryStatisticsSnapshot snapshot)
        {
            // Check error rate
            if (snapshot.CurrentErrorRate > _options.UnhealthyErrorRateThreshold)
                return HealthStatus.Unhealthy;
            
            if (snapshot.CurrentErrorRate > _options.DegradedErrorRateThreshold)
                return HealthStatus.Degraded;

            // Check queue depth percentage
            var queueDepthPercent = (double)snapshot.QueueDepth / _options.MaxExpectedQueueDepth * 100;
            if (queueDepthPercent > _options.UnhealthyQueueDepthPercent)
                return HealthStatus.Unhealthy;
            
            if (queueDepthPercent > _options.DegradedQueueDepthPercent)
                return HealthStatus.Degraded;

            // Check dropped items
            if (snapshot.ItemsDropped > 0)
            {
                var dropRate = (double)snapshot.ItemsDropped / snapshot.ItemsEnqueued * 100;
                if (dropRate > _options.UnhealthyDropRatePercent)
                    return HealthStatus.Unhealthy;
                
                if (dropRate > _options.DegradedDropRatePercent)
                    return HealthStatus.Degraded;
            }

            return HealthStatus.Healthy;
        }

        private Dictionary<string, object> BuildHealthData(TelemetryStatisticsSnapshot snapshot)
        {
            return new Dictionary<string, object>
            {
                ["uptime"] = snapshot.Uptime.ToString(),
                ["activitiesCreated"] = snapshot.ActivitiesCreated,
                ["activitiesActive"] = snapshot.ActiveActivities,
                ["queueDepth"] = snapshot.QueueDepth,
                ["maxQueueDepth"] = snapshot.MaxQueueDepth,
                ["itemsDropped"] = snapshot.ItemsDropped,
                ["errorRate"] = Math.Round(snapshot.CurrentErrorRate, 2),
                ["throughput"] = Math.Round(snapshot.CurrentThroughput, 2),
                ["processingErrors"] = snapshot.ProcessingErrors
            };
        }

        private string BuildDescription(TelemetryStatisticsSnapshot snapshot, HealthStatus status)
        {
            var sb = new StringBuilder();
            sb.Append($"Telemetry system is {status}. ");

            if (status != HealthStatus.Healthy)
            {
                if (snapshot.CurrentErrorRate > _options.DegradedErrorRateThreshold)
                {
                    sb.Append($"Error rate: {snapshot.CurrentErrorRate:F2}/sec. ");
                }

                var queuePercent = (double)snapshot.QueueDepth / _options.MaxExpectedQueueDepth * 100;
                if (queuePercent > _options.DegradedQueueDepthPercent)
                {
                    sb.Append($"Queue depth: {snapshot.QueueDepth} ({queuePercent:F0}%). ");
                }

                if (snapshot.ItemsDropped > 0)
                {
                    sb.Append($"Items dropped: {snapshot.ItemsDropped}. ");
                }
            }
            else
            {
                sb.Append($"Uptime: {snapshot.Uptime.TotalHours:F1}h, ");
                sb.Append($"Throughput: {snapshot.CurrentThroughput:F0}/sec");
            }

            return sb.ToString();
        }
    }

    /// <summary>
    /// Configuration options for telemetry health checks.
    /// </summary>
    public sealed class TelemetryHealthCheckOptions
    {
        public static readonly TelemetryHealthCheckOptions Default = new();

        /// <summary>
        /// Error rate (errors/sec) above which system is degraded.
        /// </summary>
        public double DegradedErrorRateThreshold { get; init; } = 1.0;

        /// <summary>
        /// Error rate (errors/sec) above which system is unhealthy.
        /// </summary>
        public double UnhealthyErrorRateThreshold { get; init; } = 10.0;

        /// <summary>
        /// Maximum expected queue depth for percentage calculations.
        /// </summary>
        public int MaxExpectedQueueDepth { get; init; } = 10000;

        /// <summary>
        /// Queue depth percentage above which system is degraded.
        /// </summary>
        public double DegradedQueueDepthPercent { get; init; } = 75.0;

        /// <summary>
        /// Queue depth percentage above which system is unhealthy.
        /// </summary>
        public double UnhealthyQueueDepthPercent { get; init; } = 95.0;

        /// <summary>
        /// Dropped item percentage above which system is degraded.
        /// </summary>
        public double DegradedDropRatePercent { get; init; } = 0.1;

        /// <summary>
        /// Dropped item percentage above which system is unhealthy.
        /// </summary>
        public double UnhealthyDropRatePercent { get; init; } = 1.0;
    }
}
```

### DI Extension Methods

```csharp
using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace HVO.Enterprise.Telemetry.HealthChecks
{
    /// <summary>
    /// Extension methods for registering telemetry health checks.
    /// </summary>
    public static class TelemetryHealthCheckExtensions
    {
        /// <summary>
        /// Adds telemetry health check to the service collection.
        /// </summary>
        public static IHealthChecksBuilder AddTelemetryHealthCheck(
            this IHealthChecksBuilder builder,
            string name = "telemetry",
            HealthStatus? failureStatus = null,
            IEnumerable<string>? tags = null,
            TelemetryHealthCheckOptions? options = null)
        {
            if (builder == null)
                throw new ArgumentNullException(nameof(builder));

            return builder.Add(new HealthCheckRegistration(
                name,
                sp => new TelemetryHealthCheck(
                    sp.GetRequiredService<ITelemetryStatistics>(),
                    options),
                failureStatus,
                tags));
        }
    }
}
```

## Testing Requirements

### Unit Tests

```csharp
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using FluentAssertions;

namespace HVO.Enterprise.Telemetry.Tests
{
    public class TelemetryStatisticsTests
    {
        [Fact]
        public void Statistics_InitialState_AllCountersZero()
        {
            // Arrange & Act
            var stats = new TelemetryStatistics();

            // Assert
            stats.ActivitiesCreated.Should().Be(0);
            stats.ActivitiesCompleted.Should().Be(0);
            stats.ActiveActivities.Should().Be(0);
            stats.QueueDepth.Should().Be(0);
            stats.ItemsDropped.Should().Be(0);
        }

        [Fact]
        public void IncrementActivitiesCreated_ThreadSafe_AccurateCount()
        {
            // Arrange
            var stats = new TelemetryStatistics();
            const int iterations = 10000;
            const int threadCount = 10;

            // Act
            Parallel.For(0, threadCount, _ =>
            {
                for (int i = 0; i < iterations; i++)
                {
                    stats.IncrementActivitiesCreated("test-source");
                }
            });

            // Assert
            stats.ActivitiesCreated.Should().Be(threadCount * iterations);
        }

        [Fact]
        public void ActiveActivities_CalculatedCorrectly()
        {
            // Arrange
            var stats = new TelemetryStatistics();

            // Act
            stats.IncrementActivitiesCreated("source1");
            stats.IncrementActivitiesCreated("source1");
            stats.IncrementActivitiesCreated("source1");
            stats.IncrementActivitiesCompleted("source1", TimeSpan.FromMilliseconds(10));

            // Assert
            stats.ActivitiesCreated.Should().Be(3);
            stats.ActivitiesCompleted.Should().Be(1);
            stats.ActiveActivities.Should().Be(2);
        }

        [Fact]
        public void UpdateQueueDepth_TracksMaximum()
        {
            // Arrange
            var stats = new TelemetryStatistics();

            // Act
            stats.UpdateQueueDepth(10);
            stats.UpdateQueueDepth(50);
            stats.UpdateQueueDepth(25);

            // Assert
            stats.QueueDepth.Should().Be(25);
            stats.MaxQueueDepth.Should().Be(50);
        }

        [Fact]
        public void GetSnapshot_CreatesImmutableCopy()
        {
            // Arrange
            var stats = new TelemetryStatistics();
            stats.IncrementActivitiesCreated("source1");
            stats.IncrementItemsEnqueued();

            // Act
            var snapshot1 = stats.GetSnapshot();
            stats.IncrementActivitiesCreated("source1");
            var snapshot2 = stats.GetSnapshot();

            // Assert
            snapshot1.ActivitiesCreated.Should().Be(1);
            snapshot2.ActivitiesCreated.Should().Be(2);
        }

        [Fact]
        public void CurrentErrorRate_CalculatesCorrectly()
        {
            // Arrange
            var stats = new TelemetryStatistics();

            // Act - Record 10 errors over 2 seconds
            for (int i = 0; i < 10; i++)
            {
                stats.IncrementExceptionsTracked();
                Thread.Sleep(200);
            }

            // Assert - Should be ~5 errors/sec
            stats.CurrentErrorRate.Should().BeInRange(4.0, 6.0);
        }

        [Fact]
        public void PerSourceStatistics_TrackedSeparately()
        {
            // Arrange
            var stats = new TelemetryStatistics();

            // Act
            stats.IncrementActivitiesCreated("source1");
            stats.IncrementActivitiesCreated("source1");
            stats.IncrementActivitiesCreated("source2");
            stats.IncrementActivitiesCompleted("source1", TimeSpan.FromMilliseconds(100));

            // Assert
            var perSource = stats.PerSourceStatistics;
            perSource.Should().ContainKey("source1");
            perSource.Should().ContainKey("source2");
            perSource["source1"].ActivitiesCreated.Should().Be(2);
            perSource["source2"].ActivitiesCreated.Should().Be(1);
        }

        [Fact]
        public void Reset_ClearsAllCounters()
        {
            // Arrange
            var stats = new TelemetryStatistics();
            stats.IncrementActivitiesCreated("source1");
            stats.IncrementItemsEnqueued();
            stats.IncrementExceptionsTracked();

            // Act
            stats.Reset();

            // Assert
            var snapshot = stats.GetSnapshot();
            snapshot.ActivitiesCreated.Should().Be(0);
            snapshot.ItemsEnqueued.Should().Be(0);
            snapshot.ExceptionsTracked.Should().Be(0);
        }
    }

    public class TelemetryHealthCheckTests
    {
        [Fact]
        public async Task CheckHealth_LowErrorRate_ReturnsHealthy()
        {
            // Arrange
            var stats = CreateStatistics(errorRate: 0.1, queueDepth: 10);
            var healthCheck = new TelemetryHealthCheck(stats);

            // Act
            var result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

            // Assert
            result.Status.Should().Be(HealthStatus.Healthy);
            result.Description.Should().Contain("Healthy");
        }

        [Fact]
        public async Task CheckHealth_HighErrorRate_ReturnsUnhealthy()
        {
            // Arrange
            var stats = CreateStatistics(errorRate: 15.0, queueDepth: 10);
            var healthCheck = new TelemetryHealthCheck(stats);

            // Act
            var result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

            // Assert
            result.Status.Should().Be(HealthStatus.Unhealthy);
            result.Description.Should().Contain("Error rate");
        }

        [Fact]
        public async Task CheckHealth_HighQueueDepth_ReturnsDegraded()
        {
            // Arrange
            var options = new TelemetryHealthCheckOptions
            {
                MaxExpectedQueueDepth = 100
            };
            var stats = CreateStatistics(errorRate: 0.1, queueDepth: 80);
            var healthCheck = new TelemetryHealthCheck(stats, options);

            // Act
            var result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

            // Assert
            result.Status.Should().Be(HealthStatus.Degraded);
            result.Description.Should().Contain("Queue depth");
        }

        [Fact]
        public async Task CheckHealth_IncludesDataDictionary()
        {
            // Arrange
            var stats = CreateStatistics(errorRate: 0.1, queueDepth: 10);
            var healthCheck = new TelemetryHealthCheck(stats);

            // Act
            var result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

            // Assert
            result.Data.Should().ContainKey("uptime");
            result.Data.Should().ContainKey("queueDepth");
            result.Data.Should().ContainKey("errorRate");
            result.Data.Should().ContainKey("throughput");
        }

        private ITelemetryStatistics CreateStatistics(double errorRate, int queueDepth)
        {
            // Mock implementation for testing
            var stats = new TelemetryStatistics();
            stats.UpdateQueueDepth(queueDepth);
            
            // Simulate error rate
            var errorCount = (int)(errorRate * 60); // Errors over 1 minute
            for (int i = 0; i < errorCount; i++)
            {
                stats.IncrementExceptionsTracked();
            }

            return stats;
        }
    }
}
```

## Performance Requirements

- **Counter operations**: <10ns overhead per increment
- **GetSnapshot()**: <100μs for full snapshot creation
- **Thread safety**: No locks on hot paths (use Interlocked operations)
- **Memory**: <50KB overhead for statistics storage
- **Rate calculations**: <1ms for rolling window queries

## Dependencies

**Blocked By**:
- US-001: Core Package Setup (for project structure)
- US-004: Bounded Queue Worker (queue statistics)

**Blocks**:
- US-018: DI and Static Initialization (health check registration)
- US-029: Project Documentation (monitoring examples)

## Definition of Done

- [ ] `ITelemetryStatistics` interface implemented with all metrics
- [ ] `TelemetryStatistics` class with thread-safe counters
- [ ] `TelemetryHealthCheck` with configurable thresholds
- [ ] All unit tests passing (>90% coverage)
- [ ] Health check integration tested with ASP.NET Core
- [ ] Performance benchmarks meet targets
- [ ] XML documentation complete
- [ ] Code reviewed and approved
- [ ] Zero warnings in build

## Notes

### Design Decisions

1. **Why atomic counters instead of locks?**
   - Lock-free operations are much faster (<10ns vs 50-100ns)
   - Better scalability under high concurrency
   - Simpler implementation without deadlock concerns

2. **Why rolling window for rates?**
   - Provides real-time rate calculations
   - More useful than lifetime averages
   - Bounded memory usage (drops old data)

3. **Why immutable snapshots?**
   - Thread-safe to read without locking
   - Can be safely passed across threads
   - Suitable for serialization to monitoring systems

### Implementation Tips

- Use `Interlocked.Increment()` for all counter updates
- Use `Interlocked.Read()` for reading 64-bit values on 32-bit platforms
- Consider memory barriers when reading multiple related values
- Test under high concurrency (10+ threads, 100K+ ops/sec)

### Integration Examples

```csharp
// ASP.NET Core Startup
public void ConfigureServices(IServiceCollection services)
{
    services.AddTelemetry(options => { /* config */ });
    
    services.AddHealthChecks()
        .AddTelemetryHealthCheck(
            name: "telemetry",
            tags: new[] { "ready" },
            options: new TelemetryHealthCheckOptions
            {
                UnhealthyErrorRateThreshold = 5.0,
                MaxExpectedQueueDepth = 5000
            });
}

// Logging statistics periodically
var timer = new Timer(_ =>
{
    var snapshot = Telemetry.Statistics.GetSnapshot();
    _logger.LogInformation(snapshot.ToFormattedString());
}, null, TimeSpan.Zero, TimeSpan.FromMinutes(5));

// Exposing as HTTP endpoint
app.MapGet("/admin/telemetry/stats", (ITelemetryStatistics stats) =>
{
    return Results.Ok(stats.GetSnapshot());
});
```

## Related Documentation

- [Project Plan](../project-plan.md#16-statistics-and-health-checks)
- [Health Checks in ASP.NET Core](https://learn.microsoft.com/en-us/aspnet/core/host-and-deploy/health-checks)
- [Interlocked Operations](https://learn.microsoft.com/en-us/dotnet/api/system.threading.interlocked)
