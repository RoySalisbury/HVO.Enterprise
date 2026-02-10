using System;
using System.Collections.Generic;
using System.Linq;
using Serilog.Core;
using Serilog.Events;
using Serilog.Parsing;

namespace HVO.Enterprise.Telemetry.Serilog.Tests
{
    /// <summary>
    /// Helper methods for creating Serilog objects in tests.
    /// </summary>
    internal static class SerilogTestHelpers
    {
        private static readonly MessageTemplateParser _parser = new MessageTemplateParser();

        /// <summary>
        /// Creates a minimal <see cref="LogEvent"/> for testing enrichers.
        /// </summary>
        public static LogEvent CreateLogEvent(
            string messageTemplate = "Test message",
            LogEventLevel level = LogEventLevel.Information)
        {
            var template = _parser.Parse(messageTemplate);
            return new LogEvent(
                DateTimeOffset.UtcNow,
                level,
                null,
                template,
                Array.Empty<LogEventProperty>());
        }

        /// <summary>
        /// Creates a <see cref="LogEventPropertyFactory"/> for testing enrichers.
        /// </summary>
        public static ILogEventPropertyFactory CreatePropertyFactory()
        {
            return new SimplePropertyFactory();
        }

        /// <summary>
        /// Gets a scalar property value from a log event as a string.
        /// </summary>
        public static string? GetScalarValue(LogEvent logEvent, string propertyName)
        {
            if (logEvent.Properties.TryGetValue(propertyName, out var value) &&
                value is ScalarValue scalar)
            {
                return scalar.Value?.ToString();
            }
            return null;
        }

        /// <summary>
        /// Simple property factory implementation for testing.
        /// </summary>
        private sealed class SimplePropertyFactory : ILogEventPropertyFactory
        {
            public LogEventProperty CreateProperty(string name, object? value, bool destructureObjects = false)
            {
                return new LogEventProperty(name, new ScalarValue(value));
            }
        }
    }
}
