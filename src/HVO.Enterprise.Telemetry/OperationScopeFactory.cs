using System;
using System.Diagnostics;
using HVO.Enterprise.Telemetry.Context;
using HVO.Enterprise.Telemetry.Internal;
using HVO.Enterprise.Telemetry.Sampling;
using Microsoft.Extensions.Logging;

namespace HVO.Enterprise.Telemetry
{
    /// <summary>
    /// Default implementation of <see cref="IOperationScopeFactory"/>.
    /// </summary>
    public sealed class OperationScopeFactory : IOperationScopeFactory
    {
        private readonly ActivitySource _activitySource;
        private readonly ILogger? _logger;
        private readonly IContextEnricher? _enricher;

        /// <summary>
        /// Initializes a new instance of the <see cref="OperationScopeFactory"/> class.
        /// </summary>
        /// <param name="activitySourceName">ActivitySource name.</param>
        /// <param name="activitySourceVersion">Optional ActivitySource version.</param>
        /// <param name="loggerFactory">Optional logger factory.</param>
        /// <param name="enricher">Optional context enricher.</param>
        public OperationScopeFactory(
            string activitySourceName,
            string? activitySourceVersion = null,
            ILoggerFactory? loggerFactory = null,
            IContextEnricher? enricher = null)
        {
            if (string.IsNullOrEmpty(activitySourceName))
                throw new ArgumentNullException(nameof(activitySourceName));

            _activitySource = SamplingActivitySourceExtensions.CreateWithSampling(activitySourceName, activitySourceVersion);
            _logger = loggerFactory?.CreateLogger("HVO.Enterprise.Telemetry.OperationScope");
            _enricher = enricher;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="OperationScopeFactory"/> class.
        /// </summary>
        /// <param name="activitySource">ActivitySource instance.</param>
        /// <param name="loggerFactory">Optional logger factory.</param>
        /// <param name="enricher">Optional context enricher.</param>
        public OperationScopeFactory(
            ActivitySource activitySource,
            ILoggerFactory? loggerFactory = null,
            IContextEnricher? enricher = null)
        {
            _activitySource = activitySource ?? throw new ArgumentNullException(nameof(activitySource));
            _logger = loggerFactory?.CreateLogger("HVO.Enterprise.Telemetry.OperationScope");
            _enricher = enricher;
        }

        /// <inheritdoc />
        public IOperationScope Begin(string name, OperationScopeOptions? options = null)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentNullException(nameof(name));

            var scopeOptions = options ?? new OperationScopeOptions();
            return new OperationScope(name, scopeOptions, _activitySource, _logger, _enricher, null);
        }
    }
}
