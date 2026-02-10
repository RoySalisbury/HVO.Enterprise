using System;
using HVO.Enterprise.Telemetry.Metrics;

namespace HVO.Enterprise.Telemetry.Tests.Helpers
{
    /// <summary>
    /// Concrete test implementation of the abstract <see cref="TelemetryWorkItem"/>.
    /// Shared across test classes to avoid duplication.
    /// </summary>
    internal sealed class TestWorkItem : TelemetryWorkItem
    {
        private readonly Action _action;
        private readonly string _operationType;

        public TestWorkItem(string operationType, Action action)
        {
            _action = action;
            _operationType = operationType;
        }

        public override string OperationType => _operationType;

        public override void Execute() => _action();
    }
}
