using System.Collections.Generic;
using System.Diagnostics;

namespace HVO.Enterprise.Telemetry.Sampling
{
    /// <summary>
    /// Context for sampling decisions.
    /// </summary>
    public sealed class SamplingContext
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SamplingContext"/> class.
        /// </summary>
        /// <param name="traceId">Trace identifier.</param>
        /// <param name="activityName">Activity name.</param>
        /// <param name="activitySourceName">ActivitySource name.</param>
        /// <param name="kind">Activity kind.</param>
        /// <param name="tags">Optional tags.</param>
        public SamplingContext(
            ActivityTraceId traceId,
            string activityName,
            string activitySourceName,
            ActivityKind kind,
            IEnumerable<KeyValuePair<string, object?>>? tags = null)
        {
            TraceId = traceId;
            ActivityName = activityName;
            ActivitySourceName = activitySourceName;
            Kind = kind;
            Tags = tags;
        }

        /// <summary>
        /// Gets the trace identifier.
        /// </summary>
        public ActivityTraceId TraceId { get; }

        /// <summary>
        /// Gets the activity name.
        /// </summary>
        public string ActivityName { get; }

        /// <summary>
        /// Gets the ActivitySource name.
        /// </summary>
        public string ActivitySourceName { get; }

        /// <summary>
        /// Gets the activity kind.
        /// </summary>
        public ActivityKind Kind { get; }

        /// <summary>
        /// Gets the tags associated with the activity.
        /// </summary>
        public IEnumerable<KeyValuePair<string, object?>>? Tags { get; }
    }
}
