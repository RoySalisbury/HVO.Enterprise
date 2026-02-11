using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;

namespace HVO.Enterprise.Telemetry.Tests.Logging
{
    /// <summary>
    /// Capturing logger that records all Log and BeginScope calls for assertion.
    /// </summary>
    internal sealed class CapturingLogger : ILogger
    {
        public readonly List<LogEntry> LogEntries = new List<LogEntry>();
        public readonly List<object?> Scopes = new List<object?>();
        public Func<LogLevel, bool> IsEnabledFunc { get; set; } = _ => true;

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull
        {
            Scopes.Add(state);
            return new ScopeDisposable(this);
        }

        public bool IsEnabled(LogLevel logLevel) => IsEnabledFunc(logLevel);

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            LogEntries.Add(new LogEntry(logLevel, eventId, formatter(state, exception), exception));
        }

        /// <summary>
        /// Gets the last scope as a dictionary, or null if no dictionary scope was captured.
        /// Handles both raw Dictionary scopes and LogEnrichmentScope wrappers.
        /// </summary>
        public IDictionary<string, object?>? GetLastDictionaryScope()
        {
            for (int i = Scopes.Count - 1; i >= 0; i--)
            {
                if (Scopes[i] is IDictionary<string, object?> dict)
                    return dict;

                // Handle LogEnrichmentScope which wraps enrichment data as
                // IReadOnlyList<KeyValuePair<string, object?>>
                if (Scopes[i] is IReadOnlyList<KeyValuePair<string, object?>> readOnlyList)
                {
                    var result = new Dictionary<string, object?>(readOnlyList.Count, StringComparer.Ordinal);
                    for (int j = 0; j < readOnlyList.Count; j++)
                    {
                        result[readOnlyList[j].Key] = readOnlyList[j].Value;
                    }
                    return result;
                }
            }
            return null;
        }

        private sealed class ScopeDisposable : IDisposable
        {
            private readonly CapturingLogger _logger;

            public ScopeDisposable(CapturingLogger logger)
            {
                _logger = logger;
            }

            public void Dispose()
            {
                // No-op for test purposes
            }
        }
    }

    /// <summary>
    /// Represents a captured log entry.
    /// </summary>
    internal sealed class LogEntry
    {
        public LogLevel Level { get; }
        public EventId EventId { get; }
        public string Message { get; }
        public Exception? Exception { get; }

        public LogEntry(LogLevel level, EventId eventId, string message, Exception? exception)
        {
            Level = level;
            EventId = eventId;
            Message = message;
            Exception = exception;
        }
    }

    /// <summary>
    /// Capturing logger provider that tracks CreateLogger and Dispose calls.
    /// </summary>
    internal sealed class CapturingLoggerProvider : ILoggerProvider
    {
        public readonly List<string> CreatedCategories = new List<string>();
        public readonly Dictionary<string, CapturingLogger> Loggers = new Dictionary<string, CapturingLogger>();
        public bool Disposed { get; private set; }

        public ILogger CreateLogger(string categoryName)
        {
            CreatedCategories.Add(categoryName);
            if (!Loggers.TryGetValue(categoryName, out var logger))
            {
                logger = new CapturingLogger();
                Loggers[categoryName] = logger;
            }
            return logger;
        }

        public void Dispose()
        {
            Disposed = true;
        }
    }

    /// <summary>
    /// Capturing logger factory that tracks CreateLogger and AddProvider calls.
    /// </summary>
    internal sealed class CapturingLoggerFactory : ILoggerFactory
    {
        public readonly List<string> CreatedCategories = new List<string>();
        public readonly Dictionary<string, CapturingLogger> Loggers = new Dictionary<string, CapturingLogger>();
        public readonly List<ILoggerProvider> AddedProviders = new List<ILoggerProvider>();
        public bool Disposed { get; private set; }

        public ILogger CreateLogger(string categoryName)
        {
            CreatedCategories.Add(categoryName);
            if (!Loggers.TryGetValue(categoryName, out var logger))
            {
                logger = new CapturingLogger();
                Loggers[categoryName] = logger;
            }
            return logger;
        }

        public void AddProvider(ILoggerProvider provider)
        {
            AddedProviders.Add(provider);
        }

        public void Dispose()
        {
            Disposed = true;
        }
    }

    /// <summary>
    /// Simple fake enricher for testing.
    /// </summary>
    internal sealed class FakeEnricher : HVO.Enterprise.Telemetry.Logging.ILogEnricher
    {
        private readonly Action<IDictionary<string, object?>> _enrichAction;

        public FakeEnricher(Action<IDictionary<string, object?>> enrichAction)
        {
            _enrichAction = enrichAction;
        }

        public void Enrich(IDictionary<string, object?> properties)
        {
            _enrichAction(properties);
        }
    }

    /// <summary>
    /// Enricher that always throws for testing exception suppression.
    /// </summary>
    internal sealed class ThrowingEnricher : HVO.Enterprise.Telemetry.Logging.ILogEnricher
    {
        public void Enrich(IDictionary<string, object?> properties)
        {
            throw new InvalidOperationException("Enricher failure!");
        }
    }

    /// <summary>
    /// Fake user context accessor for testing.
    /// </summary>
    internal sealed class FakeUserContextAccessor : HVO.Enterprise.Telemetry.Context.Providers.IUserContextAccessor
    {
        public HVO.Enterprise.Telemetry.Context.Providers.UserContext? UserContext { get; set; }

        public HVO.Enterprise.Telemetry.Context.Providers.UserContext? GetUserContext() => UserContext;
    }

    /// <summary>
    /// Fake HTTP request accessor for testing.
    /// </summary>
    internal sealed class FakeHttpRequestAccessor : HVO.Enterprise.Telemetry.Context.Providers.IHttpRequestAccessor
    {
        public HVO.Enterprise.Telemetry.Context.Providers.HttpRequestInfo? RequestInfo { get; set; }

        public HVO.Enterprise.Telemetry.Context.Providers.HttpRequestInfo? GetCurrentRequest() => RequestInfo;
    }
}
