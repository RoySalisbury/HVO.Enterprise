using System;
using System.Collections.Generic;
using System.Diagnostics;
using HVO.Enterprise.Telemetry.Abstractions;

namespace HVO.Enterprise.Telemetry.Internal
{
    /// <summary>
    /// No-operation implementation of <see cref="IOperationScope"/> used when
    /// telemetry is disabled. All operations are no-ops with minimal overhead.
    /// </summary>
    internal sealed class NoOpOperationScope : IOperationScope
    {
        private readonly long _startTimestamp = Stopwatch.GetTimestamp();

        /// <summary>
        /// Initializes a new instance of the <see cref="NoOpOperationScope"/> class.
        /// </summary>
        /// <param name="name">The operation name.</param>
        public NoOpOperationScope(string name)
        {
            Name = name ?? string.Empty;
            CorrelationId = string.Empty;
        }

        /// <inheritdoc />
        public string Name { get; }

        /// <inheritdoc />
        public string CorrelationId { get; }

        /// <inheritdoc />
        public Activity? Activity => null;

        /// <inheritdoc />
        public TimeSpan Elapsed
        {
            get
            {
                var elapsed = Stopwatch.GetTimestamp() - _startTimestamp;
                return TimeSpan.FromTicks((long)(elapsed * ((double)TimeSpan.TicksPerSecond / Stopwatch.Frequency)));
            }
        }

        /// <inheritdoc />
        public IOperationScope WithTag(string key, object? value)
        {
            return this;
        }

        /// <inheritdoc />
        public IOperationScope WithTags(IEnumerable<KeyValuePair<string, object?>> tags)
        {
            return this;
        }

        /// <inheritdoc />
        public IOperationScope WithProperty(string key, Func<object?> valueFactory)
        {
            return this;
        }

        /// <inheritdoc />
        public IOperationScope Fail(Exception exception)
        {
            return this;
        }

        /// <inheritdoc />
        public IOperationScope Succeed()
        {
            return this;
        }

        /// <inheritdoc />
        public IOperationScope WithResult(object? result)
        {
            return this;
        }

        /// <inheritdoc />
        public IOperationScope CreateChild(string name)
        {
            return new NoOpOperationScope(name);
        }

        /// <inheritdoc />
        public void RecordException(Exception exception)
        {
            // No-op
        }

        /// <inheritdoc />
        public void Dispose()
        {
            // No-op
        }
    }
}
