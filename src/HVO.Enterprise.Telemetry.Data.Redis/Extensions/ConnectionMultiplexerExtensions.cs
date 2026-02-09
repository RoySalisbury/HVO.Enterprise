using System;
using HVO.Enterprise.Telemetry.Data.Redis.Configuration;
using HVO.Enterprise.Telemetry.Data.Redis.Profiling;
using StackExchange.Redis;

namespace HVO.Enterprise.Telemetry.Data.Redis.Extensions
{
    /// <summary>
    /// Extension methods for adding Redis telemetry instrumentation.
    /// </summary>
    public static class ConnectionMultiplexerExtensions
    {
        /// <summary>
        /// Registers the HVO telemetry profiler with the <see cref="ConnectionMultiplexer"/>
        /// for automatic distributed tracing of Redis commands.
        /// </summary>
        /// <param name="multiplexer">The connection multiplexer to instrument.</param>
        /// <param name="options">Optional telemetry options.</param>
        /// <returns>The <see cref="RedisTelemetryProfiler"/> that was registered.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="multiplexer"/> is null.</exception>
        public static RedisTelemetryProfiler WithHvoTelemetry(
            this IConnectionMultiplexer multiplexer,
            RedisTelemetryOptions? options = null)
        {
            if (multiplexer == null)
                throw new ArgumentNullException(nameof(multiplexer));

            var profiler = new RedisTelemetryProfiler(options);
            multiplexer.RegisterProfiler(profiler.GetSessionFactory());

            return profiler;
        }
    }
}
