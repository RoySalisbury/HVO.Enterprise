using System;
using System.Data;
using System.Data.Common;
using HVO.Enterprise.Telemetry.Data.AdoNet.Configuration;
using HVO.Enterprise.Telemetry.Data.AdoNet.Instrumentation;

namespace HVO.Enterprise.Telemetry.Data.AdoNet.Extensions
{
    /// <summary>
    /// Extension methods for adding ADO.NET telemetry instrumentation.
    /// </summary>
    public static class DbConnectionExtensions
    {
        /// <summary>
        /// Wraps a <see cref="DbConnection"/> with telemetry instrumentation that creates
        /// Activities for each database command.
        /// </summary>
        /// <param name="connection">The connection to instrument.</param>
        /// <param name="options">Optional telemetry options.</param>
        /// <returns>An instrumented connection that wraps the original.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="connection"/> is null.</exception>
        public static DbConnection WithTelemetry(
            this DbConnection connection,
            AdoNetTelemetryOptions? options = null)
        {
            if (connection == null)
                throw new ArgumentNullException(nameof(connection));

            // Don't double-wrap
            if (connection is InstrumentedDbConnection)
                return connection;

            return new InstrumentedDbConnection(connection, options);
        }
    }
}
