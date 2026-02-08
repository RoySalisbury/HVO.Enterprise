using System;
using System.Collections.Generic;
using HVO.Enterprise.Telemetry.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace HVO.Enterprise.Telemetry.Tests.Configuration
{
    [TestClass]
    public class OptionsMonitorConfigurationProviderTests
    {
        [TestMethod]
        public void OptionsMonitorConfigurationProvider_IgnoresInvalidUpdates()
        {
            var monitor = new TestOptionsMonitor(new TelemetryOptions());
            var provider = new OptionsMonitorConfigurationProvider(monitor);

            var invalidOptions = new TelemetryOptions { DefaultSamplingRate = 2.0 };
            monitor.TriggerChange(invalidOptions);

            Assert.AreEqual(1.0, provider.CurrentOptions.DefaultSamplingRate, 0.0001);

            var validOptions = new TelemetryOptions { DefaultSamplingRate = 0.25 };
            monitor.TriggerChange(validOptions);

            Assert.AreEqual(0.25, provider.CurrentOptions.DefaultSamplingRate, 0.0001);
        }

        private sealed class TestOptionsMonitor : IOptionsMonitor<TelemetryOptions>
        {
            private TelemetryOptions _current;
            private readonly List<Action<TelemetryOptions, string?>> _listeners = new List<Action<TelemetryOptions, string?>>();

            public TestOptionsMonitor(TelemetryOptions current)
            {
                _current = current;
            }

            public TelemetryOptions CurrentValue => _current;

            public TelemetryOptions Get(string? name)
            {
                return _current;
            }

            public IDisposable OnChange(Action<TelemetryOptions, string?> listener)
            {
                _listeners.Add(listener);
                return new ChangeToken(() => _listeners.Remove(listener));
            }

            public void TriggerChange(TelemetryOptions options)
            {
                _current = options;
                foreach (var listener in _listeners.ToArray())
                {
                    listener(options, string.Empty);
                }
            }

            private sealed class ChangeToken : IDisposable
            {
                private readonly Action _disposeAction;
                private bool _disposed;

                public ChangeToken(Action disposeAction)
                {
                    _disposeAction = disposeAction;
                }

                public void Dispose()
                {
                    if (_disposed)
                        return;

                    _disposed = true;
                    _disposeAction();
                }
            }
        }
    }
}
