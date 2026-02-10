using System;
using Serilog.Core;
using Serilog.Events;

namespace HVO.Enterprise.Telemetry.Serilog.Tests
{
    /// <summary>
    /// A Serilog sink that delegates log event handling to a callback action.
    /// Used for capturing log events in tests without requiring external sink packages.
    /// </summary>
    internal sealed class DelegatingLogEventSink : ILogEventSink
    {
        private readonly Action<LogEvent> _onEmit;

        /// <summary>
        /// Initializes a new instance of the <see cref="DelegatingLogEventSink"/> class.
        /// </summary>
        /// <param name="onEmit">Callback invoked for each emitted log event.</param>
        public DelegatingLogEventSink(Action<LogEvent> onEmit)
        {
            _onEmit = onEmit ?? throw new ArgumentNullException(nameof(onEmit));
        }

        /// <inheritdoc />
        public void Emit(LogEvent logEvent)
        {
            _onEmit(logEvent);
        }
    }
}
