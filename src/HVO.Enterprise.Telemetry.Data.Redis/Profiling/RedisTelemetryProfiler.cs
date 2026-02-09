using System;
using HVO.Enterprise.Telemetry.Data.Redis.Configuration;
using StackExchange.Redis.Profiling;

namespace HVO.Enterprise.Telemetry.Data.Redis.Profiling
{
    /// <summary>
    /// Provides a <see cref="Func{ProfilingSession}"/> for StackExchange.Redis profiling
    /// that creates telemetry Activities for Redis commands via
    /// <see cref="StackExchange.Redis.ConnectionMultiplexer.RegisterProfiler(Func{ProfilingSession})"/>.
    /// </summary>
    public sealed class RedisTelemetryProfiler
    {
        private readonly Func<ProfilingSession> _sessionFactory;

        /// <summary>
        /// Gets the command processor used to create Activities from profiled commands.
        /// </summary>
        public RedisCommandProcessor CommandProcessor { get; }

        /// <summary>
        /// Initializes a new instance of <see cref="RedisTelemetryProfiler"/>.
        /// </summary>
        /// <param name="options">Telemetry options.</param>
        public RedisTelemetryProfiler(RedisTelemetryOptions? options = null)
        {
            CommandProcessor = new RedisCommandProcessor(options);
            _sessionFactory = () => new ProfilingSession();
        }

        /// <summary>
        /// Initializes a new instance using a custom session factory.
        /// </summary>
        /// <param name="sessionFactory">Factory for creating profiling sessions.</param>
        /// <param name="options">Telemetry options.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="sessionFactory"/> is null.</exception>
        public RedisTelemetryProfiler(Func<ProfilingSession> sessionFactory, RedisTelemetryOptions? options = null)
        {
            _sessionFactory = sessionFactory ?? throw new ArgumentNullException(nameof(sessionFactory));
            CommandProcessor = new RedisCommandProcessor(options);
        }

        /// <summary>
        /// Gets the session factory delegate suitable for passing to
        /// <see cref="StackExchange.Redis.ConnectionMultiplexer.RegisterProfiler(Func{ProfilingSession})"/>.
        /// </summary>
        /// <returns>A delegate that creates profiling sessions.</returns>
        public Func<ProfilingSession> GetSessionFactory()
        {
            return _sessionFactory;
        }

        /// <summary>
        /// Creates a new profiling session.
        /// </summary>
        /// <returns>A new <see cref="ProfilingSession"/>.</returns>
        public ProfilingSession GetSession()
        {
            return _sessionFactory();
        }
    }
}
