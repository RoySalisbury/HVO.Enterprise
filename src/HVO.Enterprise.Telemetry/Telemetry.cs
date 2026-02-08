using System;
using HVO.Enterprise.Telemetry.Exceptions;

namespace HVO.Enterprise.Telemetry
{
    /// <summary>
    /// Entry point for telemetry operations.
    /// </summary>
    public static class Telemetry
    {
        /// <summary>
        /// Records an exception with the telemetry system.
        /// </summary>
        /// <param name="exception">Exception to record.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="exception"/> is null.</exception>
        public static void RecordException(Exception exception)
        {
            if (exception == null)
                throw new ArgumentNullException(nameof(exception));

            exception.RecordException();
        }

        /// <summary>
        /// Gets the exception aggregator for querying statistics.
        /// </summary>
        /// <returns>Exception aggregator instance.</returns>
        public static ExceptionAggregator GetExceptionAggregator()
        {
            return TelemetryExceptionExtensions.GetAggregator();
        }
    }
}
