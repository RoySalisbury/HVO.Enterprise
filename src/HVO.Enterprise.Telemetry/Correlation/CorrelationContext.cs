using System;
using System.Diagnostics;
using System.Threading;

namespace HVO.Enterprise.Telemetry.Correlation
{
    /// <summary>
    /// Manages correlation IDs using AsyncLocal for automatic async flow.
    /// Provides thread-safe correlation ID management that flows through async/await boundaries.
    /// </summary>
    public static class CorrelationContext
    {
        private static readonly AsyncLocal<string?> _correlationId = new AsyncLocal<string?>();

        /// <summary>
        /// Gets or sets the current correlation ID. Auto-generates if not set.
        /// </summary>
        /// <remarks>
        /// <para>Uses a three-tier fallback mechanism:</para>
        /// <list type="number">
        /// <item><description>AsyncLocal storage - Explicitly set correlation ID takes precedence</description></item>
        /// <item><description>Activity.Current?.TraceId - Automatic integration with distributed tracing</description></item>
        /// <item><description>Auto-generated Guid - Ensures there's always a correlation ID</description></item>
        /// </list>
        /// </remarks>
        public static string Current
        {
            get
            {
                // Check AsyncLocal first
                if (!string.IsNullOrEmpty(_correlationId.Value))
                {
                    return _correlationId.Value!;
                }

                // Fallback to Activity.Current?.TraceId
                var activity = Activity.Current;
                if (activity != null)
                {
                    var traceId = activity.TraceId.ToString();
                    _correlationId.Value = traceId;
                    return traceId;
                }

                // Auto-generate new ID
                var newId = Guid.NewGuid().ToString("N");
                _correlationId.Value = newId;
                return newId;
            }
            set => _correlationId.Value = value;
        }

        /// <summary>
        /// Creates a new correlation scope with the specified ID.
        /// The previous correlation ID is restored when the scope is disposed.
        /// </summary>
        /// <param name="correlationId">The correlation ID for this scope. Cannot be null or empty.</param>
        /// <returns>A disposable scope that restores the previous correlation ID on disposal.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="correlationId"/> is null or empty.</exception>
        /// <example>
        /// <code>
        /// using (CorrelationContext.BeginScope("my-correlation-id"))
        /// {
        ///     // All operations within this scope share the same correlation ID
        ///     DoWork();
        /// }
        /// // Previous correlation ID is automatically restored
        /// </code>
        /// </example>
        public static IDisposable BeginScope(string correlationId)
        {
            if (string.IsNullOrEmpty(correlationId))
            {
                throw new ArgumentNullException(nameof(correlationId));
            }

            return new CorrelationScope(correlationId);
        }

        /// <summary>
        /// Clears the current correlation ID from AsyncLocal storage.
        /// This does not affect Activity.Current or auto-generation behavior.
        /// </summary>
        /// <remarks>
        /// Use this method to reset the correlation context, typically in test scenarios.
        /// After clearing, the next access to Current will use the three-tier fallback mechanism.
        /// </remarks>
        internal static void Clear()
        {
            _correlationId.Value = null;
        }
    }
}
