using System;
using System.Collections.Generic;
using System.Diagnostics;
using HVO.Enterprise.Telemetry.Abstractions;

namespace HVO.Enterprise.Telemetry.Wcf.Tests.Fakes
{
    /// <summary>
    /// Fake telemetry service for testing WCF extension components.
    /// Records method calls for verification.
    /// </summary>
    internal sealed class FakeTelemetryService : ITelemetryService
    {
        private readonly List<string> _methodCalls = new List<string>();

        public bool IsEnabled { get; set; } = true;

        public ITelemetryStatistics Statistics =>
            throw new NotImplementedException("Not needed for WCF extension tests");

        public bool StartCalled => _methodCalls.Contains("Start");
        public bool ShutdownCalled => _methodCalls.Contains("Shutdown");
        public int ShutdownCallCount { get; private set; }
        public IReadOnlyList<string> MethodCalls => _methodCalls;

        public IOperationScope StartOperation(string operationName)
        {
            _methodCalls.Add($"StartOperation:{operationName}");
            return new FakeOperationScope(operationName);
        }

        public void TrackException(Exception exception)
        {
            _methodCalls.Add($"TrackException:{exception.GetType().Name}");
        }

        public void TrackEvent(string eventName)
        {
            _methodCalls.Add($"TrackEvent:{eventName}");
        }

        public void RecordMetric(string metricName, double value)
        {
            _methodCalls.Add($"RecordMetric:{metricName}={value}");
        }

        public void Start()
        {
            _methodCalls.Add("Start");
        }

        public void Shutdown()
        {
            _methodCalls.Add("Shutdown");
            ShutdownCallCount++;
        }

        public void Reset()
        {
            _methodCalls.Clear();
            ShutdownCallCount = 0;
        }
    }

    internal sealed class FakeOperationScope : IOperationScope
    {
        public FakeOperationScope(string operationName)
        {
            Name = operationName;
            CorrelationId = Guid.NewGuid().ToString("N");
        }

        public string Name { get; }
        public string CorrelationId { get; }
        public Activity? Activity => null;
        public TimeSpan Elapsed => TimeSpan.Zero;

        public IOperationScope WithTag(string key, object? value) => this;
        public IOperationScope WithTags(IEnumerable<KeyValuePair<string, object?>> tags) => this;
        public IOperationScope WithProperty(string key, Func<object?> valueFactory) => this;
        public IOperationScope Fail(Exception exception) => this;
        public IOperationScope Succeed() => this;
        public IOperationScope WithResult(object? result) => this;
        public IOperationScope CreateChild(string name) => new FakeOperationScope(name);
        public void RecordException(Exception exception) { }
        public void Dispose() { }
    }
}
