using System;
using System.Collections.Generic;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;
using HVO.Enterprise.Telemetry.Metrics;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace HVO.Enterprise.Telemetry.Exceptions
{
    /// <summary>
    /// Monitors first-chance exceptions via <see cref="AppDomain.CurrentDomain"/>
    /// and logs/tracks them according to configurable filtering rules.
    /// </summary>
    /// <remarks>
    /// <para>
    /// First-chance exceptions fire the instant an exception is thrown, before any catch
    /// handler runs. This enables detection of suppressed or swallowed exceptions that
    /// would otherwise be invisible to telemetry.
    /// </para>
    /// <para>
    /// The monitor is registered as an <see cref="IHostedService"/> so it automatically
    /// subscribes on application startup and unsubscribes on shutdown.
    /// Options are read via <see cref="IOptionsMonitor{TOptions}"/> so configuration
    /// changes (e.g. in <c>appsettings.json</c>) take effect immediately.
    /// </para>
    /// </remarks>
    public sealed class FirstChanceExceptionMonitor : IHostedService, IDisposable
    {
        private readonly ILogger<FirstChanceExceptionMonitor> _logger;
        private readonly IOptionsMonitor<FirstChanceExceptionOptions> _optionsMonitor;
        private readonly ExceptionAggregator _aggregator;

        // Metrics
        private static readonly ICounter<long> FirstChanceCounter;
        private static readonly ICounter<long> SuppressedCounter;

        // Rate limiting state
        private long _eventCountInCurrentSecond;
        private long _currentSecondTicks;

        // Re-entrance guard (per-thread)
        [ThreadStatic]
        private static bool _isHandling;

        private bool _subscribed;
        private bool _disposed;

        static FirstChanceExceptionMonitor()
        {
            var recorder = MetricRecorderFactory.Instance;

            FirstChanceCounter = recorder.CreateCounter(
                "firstchance.exceptions.total",
                "exceptions",
                "Total first-chance exceptions observed");

            SuppressedCounter = recorder.CreateCounter(
                "firstchance.exceptions.suppressed",
                "exceptions",
                "First-chance exceptions suppressed by filtering or rate limiting");
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FirstChanceExceptionMonitor"/> class.
        /// </summary>
        /// <param name="logger">Logger for diagnostic output.</param>
        /// <param name="optionsMonitor">Options monitor for runtime-reloadable configuration.</param>
        /// <param name="aggregator">Optional exception aggregator for fingerprinting and grouping.</param>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="logger"/> or <paramref name="optionsMonitor"/> is null.
        /// </exception>
        public FirstChanceExceptionMonitor(
            ILogger<FirstChanceExceptionMonitor> logger,
            IOptionsMonitor<FirstChanceExceptionOptions> optionsMonitor,
            ExceptionAggregator? aggregator = null)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _optionsMonitor = optionsMonitor ?? throw new ArgumentNullException(nameof(optionsMonitor));
            _aggregator = aggregator ?? TelemetryExceptionExtensions.GetAggregator();
        }

        /// <summary>
        /// Subscribes to the first-chance exception event.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A completed task.</returns>
        public Task StartAsync(CancellationToken cancellationToken)
        {
            if (!_subscribed)
            {
                AppDomain.CurrentDomain.FirstChanceException += OnFirstChanceException;
                _subscribed = true;

                _logger.LogInformation(
                    "First-chance exception monitoring started (Enabled={Enabled})",
                    _optionsMonitor.CurrentValue.Enabled);
            }

            return Task.CompletedTask;
        }

        /// <summary>
        /// Unsubscribes from the first-chance exception event.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A completed task.</returns>
        public Task StopAsync(CancellationToken cancellationToken)
        {
            Unsubscribe();

            _logger.LogInformation("First-chance exception monitoring stopped");

            return Task.CompletedTask;
        }

        /// <summary>
        /// Disposes the monitor and unsubscribes from events.
        /// </summary>
        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;
            Unsubscribe();
        }

        /// <summary>
        /// Determines whether the given exception passes the current filter rules.
        /// </summary>
        /// <param name="exception">The exception to evaluate.</param>
        /// <param name="options">The current options snapshot.</param>
        /// <returns><c>true</c> if the exception should be logged; otherwise <c>false</c>.</returns>
        internal static bool ShouldProcess(Exception exception, FirstChanceExceptionOptions options)
        {
            if (exception == null || options == null)
                return false;

            var exceptionTypeName = exception.GetType().FullName ?? exception.GetType().Name;

            // 1. Exclude list takes highest priority
            if (options.ExcludeExceptionTypes != null && options.ExcludeExceptionTypes.Count > 0)
            {
                if (MatchesTypeList(exception, options.ExcludeExceptionTypes))
                    return false;
            }

            // 2. Include list (whitelist) — when non-empty, must match
            if (options.IncludeExceptionTypes != null && options.IncludeExceptionTypes.Count > 0)
            {
                if (!MatchesTypeList(exception, options.IncludeExceptionTypes))
                    return false;
            }

            // 3. Exclude namespace patterns
            if (options.ExcludeNamespacePatterns != null && options.ExcludeNamespacePatterns.Count > 0)
            {
                var targetSite = exception.TargetSite;
                if (targetSite != null)
                {
                    var declaringType = targetSite.DeclaringType;
                    if (declaringType != null)
                    {
                        var ns = declaringType.Namespace ?? string.Empty;
                        for (int i = 0; i < options.ExcludeNamespacePatterns.Count; i++)
                        {
                            if (ns.StartsWith(options.ExcludeNamespacePatterns[i], StringComparison.OrdinalIgnoreCase))
                                return false;
                        }
                    }
                }
            }

            // 4. Include namespace patterns (whitelist) — when non-empty, must match
            if (options.IncludeNamespacePatterns != null && options.IncludeNamespacePatterns.Count > 0)
            {
                var targetSite = exception.TargetSite;
                if (targetSite == null)
                    return false;

                var declaringType = targetSite.DeclaringType;
                if (declaringType == null)
                    return false;

                var ns = declaringType.Namespace ?? string.Empty;
                bool matched = false;
                for (int i = 0; i < options.IncludeNamespacePatterns.Count; i++)
                {
                    if (ns.StartsWith(options.IncludeNamespacePatterns[i], StringComparison.OrdinalIgnoreCase))
                    {
                        matched = true;
                        break;
                    }
                }

                if (!matched)
                    return false;
            }

            return true;
        }

        private void OnFirstChanceException(object? sender, FirstChanceExceptionEventArgs e)
        {
            // Re-entrance guard — prevent infinite recursion if our handler causes an exception
            if (_isHandling)
                return;

            _isHandling = true;
            try
            {
                HandleFirstChanceException(e.Exception);
            }
            catch
            {
                // Never throw from the event handler — any failure is silently suppressed.
            }
            finally
            {
                _isHandling = false;
            }
        }

        private void HandleFirstChanceException(Exception exception)
        {
            var options = _optionsMonitor.CurrentValue;

            // Fast exit if disabled
            if (!options.Enabled)
                return;

            // Always increment the raw counter
            FirstChanceCounter.Add(1,
                new MetricTag("type", exception.GetType().Name));

            // Apply type and namespace filters
            if (!ShouldProcess(exception, options))
            {
                SuppressedCounter.Add(1,
                    new MetricTag("reason", "filtered"));
                return;
            }

            // Rate limiting
            if (!TryAcquireRateLimit(options.MaxEventsPerSecond))
            {
                SuppressedCounter.Add(1,
                    new MetricTag("reason", "rate_limited"));
                return;
            }

            // Record in aggregator
            _aggregator.RecordException(exception);

            // Log at the configured level
            LogException(exception, options.MinimumLogLevel);
        }

        private void LogException(Exception exception, Microsoft.Extensions.Logging.LogLevel level)
        {
            var typeName = exception.GetType().FullName ?? exception.GetType().Name;
            var targetSite = exception.TargetSite;
            var source = targetSite != null
                ? (targetSite.DeclaringType?.FullName ?? "Unknown") + "." + targetSite.Name
                : "Unknown";

            // Use the appropriate log level
            switch (level)
            {
                case Microsoft.Extensions.Logging.LogLevel.Trace:
                    _logger.LogTrace(
                        "First-chance exception: {ExceptionType} in {Source} — {Message}",
                        typeName, source, exception.Message);
                    break;
                case Microsoft.Extensions.Logging.LogLevel.Debug:
                    _logger.LogDebug(
                        "First-chance exception: {ExceptionType} in {Source} — {Message}",
                        typeName, source, exception.Message);
                    break;
                case Microsoft.Extensions.Logging.LogLevel.Information:
                    _logger.LogInformation(
                        "First-chance exception: {ExceptionType} in {Source} — {Message}",
                        typeName, source, exception.Message);
                    break;
                case Microsoft.Extensions.Logging.LogLevel.Warning:
                    _logger.LogWarning(
                        "First-chance exception: {ExceptionType} in {Source} — {Message}",
                        typeName, source, exception.Message);
                    break;
                case Microsoft.Extensions.Logging.LogLevel.Error:
                    _logger.LogError(
                        "First-chance exception: {ExceptionType} in {Source} — {Message}",
                        typeName, source, exception.Message);
                    break;
                case Microsoft.Extensions.Logging.LogLevel.Critical:
                    _logger.LogCritical(
                        "First-chance exception: {ExceptionType} in {Source} — {Message}",
                        typeName, source, exception.Message);
                    break;
                default:
                    _logger.LogWarning(
                        "First-chance exception: {ExceptionType} in {Source} — {Message}",
                        typeName, source, exception.Message);
                    break;
            }
        }

        /// <summary>
        /// Simple sliding-window rate limiter using Interlocked operations.
        /// Returns <c>true</c> if the event is within the per-second budget.
        /// </summary>
        private bool TryAcquireRateLimit(int maxPerSecond)
        {
            if (maxPerSecond <= 0)
                return false;

            var nowTicks = DateTime.UtcNow.Ticks;
            var currentSecond = nowTicks / TimeSpan.TicksPerSecond;
            var previousSecond = Interlocked.Read(ref _currentSecondTicks);

            if (currentSecond != previousSecond)
            {
                // New second — reset counter.
                // Race condition is acceptable: worst case we allow a few extra events.
                Interlocked.Exchange(ref _currentSecondTicks, currentSecond);
                Interlocked.Exchange(ref _eventCountInCurrentSecond, 0);
            }

            var count = Interlocked.Increment(ref _eventCountInCurrentSecond);
            return count <= maxPerSecond;
        }

        private static bool MatchesTypeList(Exception exception, List<string> typeNames)
        {
            var type = exception.GetType();

            // Walk the type hierarchy to match base types too
            while (type != null)
            {
                var fullName = type.FullName ?? type.Name;
                for (int i = 0; i < typeNames.Count; i++)
                {
                    if (string.Equals(fullName, typeNames[i], StringComparison.OrdinalIgnoreCase))
                        return true;
                }

                type = type.BaseType;
            }

            return false;
        }

        private void Unsubscribe()
        {
            if (_subscribed)
            {
                AppDomain.CurrentDomain.FirstChanceException -= OnFirstChanceException;
                _subscribed = false;
            }
        }
    }
}
