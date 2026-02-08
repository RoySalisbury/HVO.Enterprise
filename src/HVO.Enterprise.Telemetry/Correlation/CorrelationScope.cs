using System;

namespace HVO.Enterprise.Telemetry.Correlation
{
    /// <summary>
    /// Represents a correlation scope that restores the previous correlation ID on disposal.
    /// This class enables nested scopes and matches ILogger scope semantics.
    /// </summary>
    /// <remarks>
    /// <para>
    /// CorrelationScope is designed to work with the using statement pattern for automatic cleanup.
    /// When disposed, it restores the correlation ID that was active before the scope was created.
    /// </para>
    /// <para>
    /// Multiple scopes can be nested, and each will restore its predecessor on disposal:
    /// </para>
    /// <code>
    /// using (CorrelationContext.BeginScope("outer"))
    /// {
    ///     Console.WriteLine(CorrelationContext.Current); // "outer"
    ///     using (CorrelationContext.BeginScope("inner"))
    ///     {
    ///         Console.WriteLine(CorrelationContext.Current); // "inner"
    ///     }
    ///     Console.WriteLine(CorrelationContext.Current); // "outer"
    /// }
    /// </code>
    /// </remarks>
    public sealed class CorrelationScope : IDisposable
    {
        private readonly string? _previousId;
        private bool _disposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="CorrelationScope"/> class.
        /// </summary>
        /// <param name="correlationId">The correlation ID for this scope.</param>
        internal CorrelationScope(string correlationId)
        {
            if (correlationId == null)
            {
                throw new ArgumentNullException(nameof(correlationId));
            }

            _previousId = CorrelationContext.Current;
            CorrelationContext.Current = correlationId;
        }

        /// <summary>
        /// Disposes the scope and restores the previous correlation ID.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This method is idempotent - calling it multiple times has the same effect as calling it once.
        /// </para>
        /// <para>
        /// If there was no previous correlation ID (it was null), this method restores the context to null,
        /// which will cause the next access to Current to use the three-tier fallback mechanism.
        /// </para>
        /// </remarks>
        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            CorrelationContext.Current = _previousId;
            _disposed = true;
        }
    }
}
