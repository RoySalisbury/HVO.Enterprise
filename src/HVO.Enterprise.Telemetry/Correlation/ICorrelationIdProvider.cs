using System;

namespace HVO.Enterprise.Telemetry.Correlation
{
    /// <summary>
    /// Defines an interface for custom correlation ID providers.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Implement this interface to provide custom correlation ID generation logic
    /// or to integrate with external correlation systems.
    /// </para>
    /// <para>
    /// Example use cases:
    /// <list type="bullet">
    /// <item><description>Reading correlation IDs from HTTP headers</description></item>
    /// <item><description>Integrating with message queue correlation schemes</description></item>
    /// <item><description>Using custom ID formats or generation algorithms</description></item>
    /// </list>
    /// </para>
    /// </remarks>
    public interface ICorrelationIdProvider
    {
        /// <summary>
        /// Generates a new correlation ID.
        /// </summary>
        /// <returns>A newly generated correlation ID string.</returns>
        /// <remarks>
        /// The returned value should be non-null and non-empty.
        /// The format and structure of the ID is implementation-specific.
        /// </remarks>
        string GenerateCorrelationId();

        /// <summary>
        /// Attempts to retrieve an existing correlation ID from the current context.
        /// </summary>
        /// <param name="correlationId">
        /// When this method returns, contains the correlation ID if one was found; otherwise, null.
        /// </param>
        /// <returns>
        /// <c>true</c> if a correlation ID was found; otherwise, <c>false</c>.
        /// </returns>
        /// <remarks>
        /// This method should not generate a new ID. It should only return true if an existing
        /// correlation ID is available in the provider's context.
        /// </remarks>
        bool TryGetCorrelationId(out string? correlationId);
    }
}
