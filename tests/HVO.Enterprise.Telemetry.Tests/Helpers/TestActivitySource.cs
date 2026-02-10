using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace HVO.Enterprise.Telemetry.Tests.Helpers
{
    /// <summary>
    /// Provides a self-contained <see cref="ActivitySource"/> with an auto-registered
    /// <see cref="ActivityListener"/> that records all sampled data. Disposing this
    /// instance tears down both the listener and the source, ensuring test isolation.
    /// </summary>
    public sealed class TestActivitySource : IDisposable
    {
        private readonly ActivitySource _source;
        private readonly ActivityListener _listener;
        private readonly List<Activity> _startedActivities = new List<Activity>();
        private readonly List<Activity> _stoppedActivities = new List<Activity>();

        /// <summary>
        /// Creates a new test activity source with full sampling enabled.
        /// </summary>
        /// <param name="name">Source name. Defaults to a unique test name.</param>
        public TestActivitySource(string? name = null)
        {
            name ??= "test-source-" + Guid.NewGuid().ToString("N").Substring(0, 8);

            _source = new ActivitySource(name, "1.0.0");
            _listener = new ActivityListener
            {
                ShouldListenTo = source => source.Name == name,
                Sample = (ref ActivityCreationOptions<ActivityContext> _) =>
                    ActivitySamplingResult.AllDataAndRecorded,
                ActivityStarted = activity =>
                {
                    lock (_startedActivities) { _startedActivities.Add(activity); }
                },
                ActivityStopped = activity =>
                {
                    lock (_stoppedActivities) { _stoppedActivities.Add(activity); }
                }
            };
            ActivitySource.AddActivityListener(_listener);
        }

        /// <summary>Gets the underlying <see cref="ActivitySource"/>.</summary>
        public ActivitySource Source => _source;

        /// <summary>Gets the source name.</summary>
        public string Name => _source.Name;

        /// <summary>Gets a snapshot of all activities that have been started.</summary>
        public IReadOnlyList<Activity> StartedActivities
        {
            get { lock (_startedActivities) { return _startedActivities.ToArray(); } }
        }

        /// <summary>Gets a snapshot of all activities that have been stopped.</summary>
        public IReadOnlyList<Activity> StoppedActivities
        {
            get { lock (_stoppedActivities) { return _stoppedActivities.ToArray(); } }
        }

        /// <summary>Starts an activity on the test source.</summary>
        public Activity? StartActivity(string name, ActivityKind kind = ActivityKind.Internal)
        {
            return _source.StartActivity(name, kind);
        }

        /// <summary>Resets all captured activity lists.</summary>
        public void Reset()
        {
            lock (_startedActivities) { _startedActivities.Clear(); }
            lock (_stoppedActivities) { _stoppedActivities.Clear(); }
        }

        public void Dispose()
        {
            _listener.Dispose();
            _source.Dispose();
        }
    }
}
