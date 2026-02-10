using System;
using System.Collections.Concurrent;
using System.Linq;
using Microsoft.Extensions.Logging;

namespace HVO.Enterprise.Telemetry.Tests.Helpers
{
    /// <summary>
    /// A lightweight in-memory logger that captures all log entries for assertion.
    /// Thread-safe and suitable for concurrent / integration tests.
    /// </summary>
    public sealed class FakeLogger<T> : FakeLogger, ILogger<T>
    {
        public FakeLogger() : base(typeof(T).FullName ?? typeof(T).Name) { }
    }

    /// <summary>
    /// Non-generic fake logger. Use <see cref="FakeLogger{T}"/> for the typed variant.
    /// </summary>
    public class FakeLogger : ILogger
    {
        private readonly string _categoryName;
        private readonly ConcurrentQueue<LogEntry> _entries = new ConcurrentQueue<LogEntry>();

        public FakeLogger(string categoryName = "Test")
        {
            _categoryName = categoryName;
        }

        /// <summary>Gets all captured log entries.</summary>
        public ConcurrentQueue<LogEntry> Entries => _entries;

        /// <summary>Gets the category name.</summary>
        public string CategoryName => _categoryName;

        /// <summary>Returns the number of captured entries.</summary>
        public int Count => _entries.Count;

        /// <summary>Checks if any entry was logged at the given level.</summary>
        public bool HasLoggedAtLevel(LogLevel level)
        {
            return _entries.Where(e => e.Level == level).Any();
        }

        /// <summary>Checks if any entry contains the given substring (case-sensitive).</summary>
        public bool HasLoggedContaining(string substring)
        {
            return HasLoggedContaining(substring, ignoreCase: false);
        }

        /// <summary>
        /// Checks if any entry contains the given substring, with an option for case-insensitive search.
        /// </summary>
        /// <param name="substring">The substring to search for in logged messages.</param>
        /// <param name="ignoreCase">
        /// When <c>true</c>, performs a case-insensitive comparison; otherwise, uses a case-sensitive comparison.
        /// </param>
        /// <returns><c>true</c> if any logged message contains the substring; otherwise, <c>false</c>.</returns>
        public bool HasLoggedContaining(string substring, bool ignoreCase)
        {
            var comparison = ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
            return _entries
                .Where(e => e.Message != null && e.Message.IndexOf(substring, comparison) >= 0)
                .Any();
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            _entries.Enqueue(new LogEntry
            {
                Level = logLevel,
                EventId = eventId,
                Message = formatter(state, exception),
                Exception = exception,
                Timestamp = DateTimeOffset.UtcNow
            });
        }

        public bool IsEnabled(LogLevel logLevel) => true;

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull
            => NullScope.Instance;

        /// <summary>Clears all captured entries.</summary>
        public void Clear()
        {
            while (_entries.TryDequeue(out _)) { }
        }

        private sealed class NullScope : IDisposable
        {
            public static readonly NullScope Instance = new NullScope();
            public void Dispose() { }
        }
    }

    /// <summary>Represents a single captured log entry.</summary>
    public sealed class LogEntry
    {
        public LogLevel Level { get; set; }
        public EventId EventId { get; set; }
        public string? Message { get; set; }
        public Exception? Exception { get; set; }
        public DateTimeOffset Timestamp { get; set; }

        public override string ToString() => $"[{Level}] {Message}";
    }

    /// <summary>
    /// A fake <see cref="ILoggerFactory"/> that creates <see cref="FakeLogger"/> instances
    /// and tracks all loggers created.
    /// </summary>
    public sealed class FakeLoggerFactory : ILoggerFactory
    {
        private readonly ConcurrentDictionary<string, FakeLogger> _loggers
            = new ConcurrentDictionary<string, FakeLogger>();

        /// <summary>Gets all loggers that have been created.</summary>
        public ConcurrentDictionary<string, FakeLogger> Loggers => _loggers;

        public ILogger CreateLogger(string categoryName)
        {
            return _loggers.GetOrAdd(categoryName, name => new FakeLogger(name));
        }

        public void AddProvider(ILoggerProvider provider) { }

        public void Dispose() { }
    }
}
