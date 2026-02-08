using System;
using System.Threading;
using System.Threading.Tasks;

namespace HVO.Enterprise.Telemetry.Lifecycle
{
    /// <summary>
    /// Interface for custom telemetry lifetime management.
    /// </summary>
    public interface ITelemetryLifetime
    {
        /// <summary>
        /// Gets a value indicating whether shutdown is in progress.
        /// </summary>
        bool IsShuttingDown { get; }

        /// <summary>
        /// Initiates graceful shutdown with the specified timeout.
        /// </summary>
        /// <param name="timeout">Maximum time to wait for shutdown to complete.</param>
        /// <param name="cancellationToken">Cancellation token for early abort.</param>
        /// <returns>Result indicating success, items flushed, and items remaining.</returns>
        Task<ShutdownResult> ShutdownAsync(TimeSpan timeout, CancellationToken cancellationToken = default);
    }
}
