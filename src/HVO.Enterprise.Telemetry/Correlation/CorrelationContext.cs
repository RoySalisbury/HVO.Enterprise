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
                // Fast path: check AsyncLocal directly (avoid IsNullOrEmpty overhead)
                var value = _correlationId.Value;
                if (value != null)
                {
                    return value;
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
                // Note: Using format "N" (32 hexadecimal digits without hyphens) for consistency
                // Activity.TraceId is a 16-byte identifier rendered as 32 lowercase hexadecimal characters without dashes (W3C trace-id format)
                // Both this GUID format and Activity.TraceId's representation are valid correlation IDs and can be used interchangeably
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
        /// Attempts to get the explicitly set correlation ID without triggering fallback
        /// or auto-generation logic.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Returns only values explicitly set via <see cref="BeginScope"/>, direct assignment
        /// to <see cref="Current"/>, or the Activity-derived value cached from a prior read.
        /// Does NOT trigger the three-tier fallback mechanism.
        /// </para>
        /// <para>
        /// This method is useful for enrichers and integrations that need to distinguish
        /// explicit correlation IDs from auto-generated or Activity-derived values.
        /// </para>
        /// </remarks>
        /// <param name="correlationId">
        /// When this method returns <see langword="true"/>, contains the explicitly set
        /// correlation ID; otherwise, <see langword="null"/>.
        /// </param>
        /// <returns>
        /// <see langword="true"/> if an explicit correlation ID is available;
        /// otherwise, <see langword="false"/>.
        /// </returns>
        /// <example>
        /// <code>
        /// using (CorrelationContext.BeginScope("my-id"))
        /// {
        ///     if (CorrelationContext.TryGetExplicitCorrelationId(out var id))
        ///     {
        ///         Console.WriteLine(id); // "my-id"
        ///     }
        /// }
        /// </code>
        /// </example>
        public static bool TryGetExplicitCorrelationId(out string? correlationId)
        {
            correlationId = _correlationId.Value;
            return correlationId != null;
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

        /// <summary>
        /// Gets the raw AsyncLocal value without triggering fallback logic.
        /// Used internally by CorrelationScope to capture the true previous state.
        /// </summary>
        internal static string? GetRawValue()
        {
            return _correlationId.Value;
        }

        /// <summary>
        /// Sets the raw AsyncLocal value without any processing.
        /// Used internally by CorrelationScope to restore the previous state.
        /// </summary>
        internal static void SetRawValue(string? value)
        {
            _correlationId.Value = value;
        }
    }
}
