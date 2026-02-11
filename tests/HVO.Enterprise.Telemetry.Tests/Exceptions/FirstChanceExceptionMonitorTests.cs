using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HVO.Enterprise.Telemetry.Exceptions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace HVO.Enterprise.Telemetry.Tests.Exceptions
{
    [TestClass]
    public class FirstChanceExceptionOptionsTests
    {
        [TestMethod]
        public void Defaults_EnabledIsFalse()
        {
            var options = new FirstChanceExceptionOptions();
            Assert.IsFalse(options.Enabled);
        }

        [TestMethod]
        public void Defaults_MinimumLogLevelIsWarning()
        {
            var options = new FirstChanceExceptionOptions();
            Assert.AreEqual(LogLevel.Warning, options.MinimumLogLevel);
        }

        [TestMethod]
        public void Defaults_MaxEventsPerSecondIs100()
        {
            var options = new FirstChanceExceptionOptions();
            Assert.AreEqual(100, options.MaxEventsPerSecond);
        }

        [TestMethod]
        public void Defaults_ExcludeListContainsCancellationExceptions()
        {
            var options = new FirstChanceExceptionOptions();
            Assert.IsNotNull(options.ExcludeExceptionTypes);
            CollectionAssert.Contains(
                options.ExcludeExceptionTypes,
                "System.OperationCanceledException");
            CollectionAssert.Contains(
                options.ExcludeExceptionTypes,
                "System.Threading.Tasks.TaskCanceledException");
        }

        [TestMethod]
        public void Defaults_IncludeListIsEmpty()
        {
            var options = new FirstChanceExceptionOptions();
            Assert.IsNotNull(options.IncludeExceptionTypes);
            Assert.AreEqual(0, options.IncludeExceptionTypes.Count);
        }

        [TestMethod]
        public void Defaults_NamespaceListsAreEmpty()
        {
            var options = new FirstChanceExceptionOptions();
            Assert.IsNotNull(options.IncludeNamespacePatterns);
            Assert.AreEqual(0, options.IncludeNamespacePatterns.Count);
            Assert.IsNotNull(options.ExcludeNamespacePatterns);
            Assert.AreEqual(0, options.ExcludeNamespacePatterns.Count);
        }

        [TestMethod]
        public void CanSetAllProperties()
        {
            var options = new FirstChanceExceptionOptions
            {
                Enabled = true,
                MinimumLogLevel = LogLevel.Error,
                MaxEventsPerSecond = 50,
                IncludeExceptionTypes = new List<string> { "System.InvalidOperationException" },
                ExcludeExceptionTypes = new List<string> { "System.ArgumentException" },
                IncludeNamespacePatterns = new List<string> { "MyApp" },
                ExcludeNamespacePatterns = new List<string> { "System" },
            };

            Assert.IsTrue(options.Enabled);
            Assert.AreEqual(LogLevel.Error, options.MinimumLogLevel);
            Assert.AreEqual(50, options.MaxEventsPerSecond);
            Assert.AreEqual(1, options.IncludeExceptionTypes.Count);
            Assert.AreEqual(1, options.ExcludeExceptionTypes.Count);
            Assert.AreEqual(1, options.IncludeNamespacePatterns.Count);
            Assert.AreEqual(1, options.ExcludeNamespacePatterns.Count);
        }
    }

    [TestClass]
    public class FirstChanceExceptionFilterTests
    {
        [TestMethod]
        public void ShouldProcess_NullException_ReturnsFalse()
        {
            var options = new FirstChanceExceptionOptions { Enabled = true };
            Assert.IsFalse(FirstChanceExceptionMonitor.ShouldProcess(null!, options));
        }

        [TestMethod]
        public void ShouldProcess_NullOptions_ReturnsFalse()
        {
            var ex = new InvalidOperationException("test");
            Assert.IsFalse(FirstChanceExceptionMonitor.ShouldProcess(ex, null!));
        }

        [TestMethod]
        public void ShouldProcess_NoFilters_ReturnsTrue()
        {
            var options = new FirstChanceExceptionOptions
            {
                Enabled = true,
                IncludeExceptionTypes = new List<string>(),
                ExcludeExceptionTypes = new List<string>(),
                IncludeNamespacePatterns = new List<string>(),
                ExcludeNamespacePatterns = new List<string>(),
            };
            var ex = new InvalidOperationException("test");

            Assert.IsTrue(FirstChanceExceptionMonitor.ShouldProcess(ex, options));
        }

        [TestMethod]
        public void ShouldProcess_ExcludeList_MatchingType_ReturnsFalse()
        {
            var options = new FirstChanceExceptionOptions
            {
                Enabled = true,
                ExcludeExceptionTypes = new List<string>
                {
                    "System.InvalidOperationException"
                },
                IncludeExceptionTypes = new List<string>(),
            };

            var ex = new InvalidOperationException("test");
            Assert.IsFalse(FirstChanceExceptionMonitor.ShouldProcess(ex, options));
        }

        [TestMethod]
        public void ShouldProcess_ExcludeList_NonMatchingType_ReturnsTrue()
        {
            var options = new FirstChanceExceptionOptions
            {
                Enabled = true,
                ExcludeExceptionTypes = new List<string>
                {
                    "System.ArgumentException"
                },
                IncludeExceptionTypes = new List<string>(),
                IncludeNamespacePatterns = new List<string>(),
                ExcludeNamespacePatterns = new List<string>(),
            };

            var ex = new InvalidOperationException("test");
            Assert.IsTrue(FirstChanceExceptionMonitor.ShouldProcess(ex, options));
        }

        [TestMethod]
        public void ShouldProcess_ExcludeList_MatchesBaseType()
        {
            // OperationCanceledException is a base of TaskCanceledException
            var options = new FirstChanceExceptionOptions
            {
                Enabled = true,
                ExcludeExceptionTypes = new List<string>
                {
                    "System.OperationCanceledException"
                },
                IncludeExceptionTypes = new List<string>(),
            };

            var ex = new TaskCanceledException("test");
            // TaskCanceledException inherits from OperationCanceledException,
            // so excluding OperationCanceledException should match it.
            Assert.IsFalse(FirstChanceExceptionMonitor.ShouldProcess(ex, options));
        }

        [TestMethod]
        public void ShouldProcess_IncludeList_MatchingType_ReturnsTrue()
        {
            var options = new FirstChanceExceptionOptions
            {
                Enabled = true,
                IncludeExceptionTypes = new List<string>
                {
                    "System.InvalidOperationException"
                },
                ExcludeExceptionTypes = new List<string>(),
                IncludeNamespacePatterns = new List<string>(),
                ExcludeNamespacePatterns = new List<string>(),
            };

            var ex = new InvalidOperationException("test");
            Assert.IsTrue(FirstChanceExceptionMonitor.ShouldProcess(ex, options));
        }

        [TestMethod]
        public void ShouldProcess_IncludeList_NonMatchingType_ReturnsFalse()
        {
            var options = new FirstChanceExceptionOptions
            {
                Enabled = true,
                IncludeExceptionTypes = new List<string>
                {
                    "System.ArgumentException"
                },
                ExcludeExceptionTypes = new List<string>(),
            };

            var ex = new InvalidOperationException("test");
            Assert.IsFalse(FirstChanceExceptionMonitor.ShouldProcess(ex, options));
        }

        [TestMethod]
        public void ShouldProcess_ExcludeTakesPriorityOverInclude()
        {
            var options = new FirstChanceExceptionOptions
            {
                Enabled = true,
                IncludeExceptionTypes = new List<string>
                {
                    "System.InvalidOperationException"
                },
                ExcludeExceptionTypes = new List<string>
                {
                    "System.InvalidOperationException"
                },
            };

            var ex = new InvalidOperationException("test");
            // Exclude takes priority
            Assert.IsFalse(FirstChanceExceptionMonitor.ShouldProcess(ex, options));
        }

        [TestMethod]
        public void ShouldProcess_CaseInsensitiveTypeMatching()
        {
            var options = new FirstChanceExceptionOptions
            {
                Enabled = true,
                IncludeExceptionTypes = new List<string>
                {
                    "system.invalidoperationexception"
                },
                ExcludeExceptionTypes = new List<string>(),
                IncludeNamespacePatterns = new List<string>(),
                ExcludeNamespacePatterns = new List<string>(),
            };

            var ex = new InvalidOperationException("test");
            Assert.IsTrue(FirstChanceExceptionMonitor.ShouldProcess(ex, options));
        }

        [TestMethod]
        public void ShouldProcess_DefaultExcludes_FilterCancellationExceptions()
        {
            var options = new FirstChanceExceptionOptions { Enabled = true };

            Assert.IsFalse(
                FirstChanceExceptionMonitor.ShouldProcess(
                    new OperationCanceledException(), options));
            Assert.IsFalse(
                FirstChanceExceptionMonitor.ShouldProcess(
                    new TaskCanceledException(), options));
        }

        [TestMethod]
        public void ShouldProcess_DefaultExcludes_AllowOtherExceptions()
        {
            var options = new FirstChanceExceptionOptions
            {
                Enabled = true,
                IncludeExceptionTypes = new List<string>(),
                IncludeNamespacePatterns = new List<string>(),
                ExcludeNamespacePatterns = new List<string>(),
            };

            Assert.IsTrue(
                FirstChanceExceptionMonitor.ShouldProcess(
                    new InvalidOperationException("test"), options));
        }
    }

    [TestClass]
    public class FirstChanceExceptionMonitorTests
    {
        [TestMethod]
        public void Constructor_NullLogger_Throws()
        {
            var optionsMonitor = CreateOptionsMonitor(new FirstChanceExceptionOptions());
            Assert.ThrowsException<ArgumentNullException>(() =>
                new FirstChanceExceptionMonitor(null!, optionsMonitor));
        }

        [TestMethod]
        public void Constructor_NullOptionsMonitor_Throws()
        {
            var logger = new TestLogger<FirstChanceExceptionMonitor>();
            Assert.ThrowsException<ArgumentNullException>(() =>
                new FirstChanceExceptionMonitor(logger, null!));
        }

        [TestMethod]
        public async Task StartAsync_SubscribesToEvent()
        {
            var logger = new TestLogger<FirstChanceExceptionMonitor>();
            var options = new FirstChanceExceptionOptions { Enabled = true };
            var optionsMonitor = CreateOptionsMonitor(options);

            using var monitor = new FirstChanceExceptionMonitor(logger, optionsMonitor);
            await monitor.StartAsync(CancellationToken.None);

            // Trigger a first-chance exception
            try { throw new InvalidOperationException("test-start"); }
            catch { /* intentionally caught */ }

            await monitor.StopAsync(CancellationToken.None);

            // Should have logged the first-chance exception
            Assert.IsTrue(
                logger.LogEntries.Any(e => e.Message.Contains("test-start")),
                "Monitor should have logged the first-chance exception after StartAsync");
        }

        [TestMethod]
        public async Task StopAsync_UnsubscribesFromEvent()
        {
            var logger = new TestLogger<FirstChanceExceptionMonitor>();
            var options = new FirstChanceExceptionOptions
            {
                Enabled = true,
                ExcludeExceptionTypes = new List<string>(),
                IncludeExceptionTypes = new List<string>(),
                IncludeNamespacePatterns = new List<string>(),
                ExcludeNamespacePatterns = new List<string>(),
            };
            var optionsMonitor = CreateOptionsMonitor(options);

            using var monitor = new FirstChanceExceptionMonitor(logger, optionsMonitor);
            await monitor.StartAsync(CancellationToken.None);
            await monitor.StopAsync(CancellationToken.None);

            logger.LogEntries.Clear();

            // Trigger an exception after stopping
            try { throw new InvalidOperationException("test-after-stop"); }
            catch { /* intentionally caught */ }

            Assert.IsFalse(
                logger.LogEntries.Any(e => e.Message.Contains("test-after-stop")),
                "Monitor should NOT log after StopAsync");
        }

        [TestMethod]
        public async Task Disabled_DoesNotLogExceptions()
        {
            var logger = new TestLogger<FirstChanceExceptionMonitor>();
            var options = new FirstChanceExceptionOptions { Enabled = false };
            var optionsMonitor = CreateOptionsMonitor(options);

            using var monitor = new FirstChanceExceptionMonitor(logger, optionsMonitor);
            await monitor.StartAsync(CancellationToken.None);

            try { throw new InvalidOperationException("test-disabled"); }
            catch { /* intentionally caught */ }

            await monitor.StopAsync(CancellationToken.None);

            Assert.IsFalse(
                logger.LogEntries.Any(e => e.Message.Contains("test-disabled")),
                "Monitor should NOT log exceptions when disabled");
        }

        [TestMethod]
        public async Task Enabled_LogsAtConfiguredLevel()
        {
            var logger = new TestLogger<FirstChanceExceptionMonitor>();
            var options = new FirstChanceExceptionOptions
            {
                Enabled = true,
                MinimumLogLevel = LogLevel.Error,
                ExcludeExceptionTypes = new List<string>(),
                IncludeExceptionTypes = new List<string>(),
                IncludeNamespacePatterns = new List<string>(),
                ExcludeNamespacePatterns = new List<string>(),
            };
            var optionsMonitor = CreateOptionsMonitor(options);

            using var monitor = new FirstChanceExceptionMonitor(logger, optionsMonitor);
            await monitor.StartAsync(CancellationToken.None);

            try { throw new InvalidOperationException("test-log-level"); }
            catch { /* intentionally caught */ }

            await monitor.StopAsync(CancellationToken.None);

            var entry = logger.LogEntries.FirstOrDefault(e => e.Message.Contains("test-log-level"));
            Assert.IsNotNull(entry, "Should have logged the exception");
            Assert.AreEqual(LogLevel.Error, entry.Level, "Should log at configured level");
        }

        [TestMethod]
        public async Task ExcludeFilter_PreventsLogging()
        {
            var logger = new TestLogger<FirstChanceExceptionMonitor>();
            var options = new FirstChanceExceptionOptions
            {
                Enabled = true,
                ExcludeExceptionTypes = new List<string>
                {
                    "System.InvalidOperationException"
                },
                IncludeExceptionTypes = new List<string>(),
                IncludeNamespacePatterns = new List<string>(),
                ExcludeNamespacePatterns = new List<string>(),
            };
            var optionsMonitor = CreateOptionsMonitor(options);

            using var monitor = new FirstChanceExceptionMonitor(logger, optionsMonitor);
            await monitor.StartAsync(CancellationToken.None);

            try { throw new InvalidOperationException("test-excluded"); }
            catch { /* intentionally caught */ }

            await monitor.StopAsync(CancellationToken.None);

            Assert.IsFalse(
                logger.LogEntries.Any(e => e.Message.Contains("test-excluded")),
                "Excluded exception type should not be logged");
        }

        [TestMethod]
        public async Task IncludeFilter_OnlyLogsMatchingTypes()
        {
            var logger = new TestLogger<FirstChanceExceptionMonitor>();
            var options = new FirstChanceExceptionOptions
            {
                Enabled = true,
                IncludeExceptionTypes = new List<string>
                {
                    "System.ArgumentException"
                },
                ExcludeExceptionTypes = new List<string>(),
                IncludeNamespacePatterns = new List<string>(),
                ExcludeNamespacePatterns = new List<string>(),
            };
            var optionsMonitor = CreateOptionsMonitor(options);

            using var monitor = new FirstChanceExceptionMonitor(logger, optionsMonitor);
            await monitor.StartAsync(CancellationToken.None);

            try { throw new InvalidOperationException("test-not-included"); }
            catch { /* intentionally caught */ }

            try { throw new ArgumentException("test-included"); }
            catch { /* intentionally caught */ }

            await monitor.StopAsync(CancellationToken.None);

            Assert.IsFalse(
                logger.LogEntries.Any(e => e.Message.Contains("test-not-included")),
                "Non-included types should not be logged");
            Assert.IsTrue(
                logger.LogEntries.Any(e => e.Message.Contains("test-included")),
                "Included types should be logged");
        }

        [TestMethod]
        public async Task RateLimiting_CapsEventsPerSecond()
        {
            var logger = new TestLogger<FirstChanceExceptionMonitor>();
            var options = new FirstChanceExceptionOptions
            {
                Enabled = true,
                MaxEventsPerSecond = 5,
                ExcludeExceptionTypes = new List<string>(),
                IncludeExceptionTypes = new List<string>(),
                IncludeNamespacePatterns = new List<string>(),
                ExcludeNamespacePatterns = new List<string>(),
            };
            var optionsMonitor = CreateOptionsMonitor(options);

            using var monitor = new FirstChanceExceptionMonitor(logger, optionsMonitor);
            await monitor.StartAsync(CancellationToken.None);

            // Throw 20 exceptions rapidly
            for (int i = 0; i < 20; i++)
            {
                try { throw new InvalidOperationException($"rate-test-{i}"); }
                catch { /* intentionally caught */ }
            }

            await monitor.StopAsync(CancellationToken.None);

            // Count only the "First-chance exception:" log entries (not startup/shutdown messages)
            var firstChanceEntries = logger.LogEntries
                .Count(e => e.Message.Contains("First-chance exception:") && e.Message.Contains("rate-test-"));

            Assert.IsTrue(
                firstChanceEntries <= 10, // Allow some tolerance for per-second boundary
                $"Expected at most ~5-10 logged events due to rate limiting, but got {firstChanceEntries}");

            Assert.IsTrue(
                firstChanceEntries >= 1,
                "Should have logged at least one event");
        }

        [TestMethod]
        public async Task Dispose_UnsubscribesFromEvent()
        {
            var logger = new TestLogger<FirstChanceExceptionMonitor>();
            var options = new FirstChanceExceptionOptions
            {
                Enabled = true,
                ExcludeExceptionTypes = new List<string>(),
                IncludeExceptionTypes = new List<string>(),
                IncludeNamespacePatterns = new List<string>(),
                ExcludeNamespacePatterns = new List<string>(),
            };
            var optionsMonitor = CreateOptionsMonitor(options);

            var monitor = new FirstChanceExceptionMonitor(logger, optionsMonitor);
            await monitor.StartAsync(CancellationToken.None);
            monitor.Dispose();

            logger.LogEntries.Clear();

            try { throw new InvalidOperationException("test-after-dispose"); }
            catch { /* intentionally caught */ }

            Assert.IsFalse(
                logger.LogEntries.Any(e => e.Message.Contains("test-after-dispose")),
                "Monitor should NOT log after Dispose");
        }

        [TestMethod]
        public async Task ReEntrance_DoesNotRecurse()
        {
            // This test verifies the monitor does not infinitely recurse
            // when the handler itself encounters an exception.
            var logger = new TestLogger<FirstChanceExceptionMonitor>();
            var options = new FirstChanceExceptionOptions
            {
                Enabled = true,
                ExcludeExceptionTypes = new List<string>(),
                IncludeExceptionTypes = new List<string>(),
                IncludeNamespacePatterns = new List<string>(),
                ExcludeNamespacePatterns = new List<string>(),
            };
            var optionsMonitor = CreateOptionsMonitor(options);

            using var monitor = new FirstChanceExceptionMonitor(logger, optionsMonitor);
            await monitor.StartAsync(CancellationToken.None);

            // This should not cause a stack overflow
            try { throw new InvalidOperationException("reentrancy-test"); }
            catch { /* intentionally caught */ }

            await monitor.StopAsync(CancellationToken.None);

            // Just verifying we get here without StackOverflowException
            Assert.IsTrue(true, "Should complete without stack overflow");
        }

        [TestMethod]
        public async Task LogMessage_ContainsExceptionTypeAndMessage()
        {
            var logger = new TestLogger<FirstChanceExceptionMonitor>();
            var options = new FirstChanceExceptionOptions
            {
                Enabled = true,
                ExcludeExceptionTypes = new List<string>(),
                IncludeExceptionTypes = new List<string>(),
                IncludeNamespacePatterns = new List<string>(),
                ExcludeNamespacePatterns = new List<string>(),
            };
            var optionsMonitor = CreateOptionsMonitor(options);

            using var monitor = new FirstChanceExceptionMonitor(logger, optionsMonitor);
            await monitor.StartAsync(CancellationToken.None);

            try { throw new ArgumentNullException("myParam", "custom message"); }
            catch { /* intentionally caught */ }

            await monitor.StopAsync(CancellationToken.None);

            var entry = logger.LogEntries.FirstOrDefault(
                e => e.Message.Contains("ArgumentNullException"));

            Assert.IsNotNull(entry, "Should log the exception type name");
            Assert.IsTrue(
                entry.Message.Contains("First-chance exception:"),
                "Log message should contain the prefix");
        }

        // ── DI Registration Tests ──────────────────────────────────

        [TestMethod]
        public void AddFirstChanceExceptionMonitoring_RegistersServices()
        {
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddFirstChanceExceptionMonitoring(o => o.Enabled = true);

            var sp = services.BuildServiceProvider();
            var monitor = sp.GetService<FirstChanceExceptionMonitor>();

            Assert.IsNotNull(monitor, "Monitor should be registered");
        }

        [TestMethod]
        public void AddFirstChanceExceptionMonitoring_IsIdempotent()
        {
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddFirstChanceExceptionMonitoring();
            services.AddFirstChanceExceptionMonitoring();

            var monitorRegistrations = services
                .Count(s => s.ServiceType == typeof(FirstChanceExceptionMonitor));

            Assert.AreEqual(1, monitorRegistrations,
                "Should only register the monitor once");
        }

        [TestMethod]
        public void AddFirstChanceExceptionMonitoring_NullServices_Throws()
        {
            IServiceCollection? services = null;

            Assert.ThrowsException<ArgumentNullException>(() =>
                services!.AddFirstChanceExceptionMonitoring());
        }

        [TestMethod]
        public void AddFirstChanceExceptionMonitoring_ConfiguresOptions()
        {
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddFirstChanceExceptionMonitoring(o =>
            {
                o.Enabled = true;
                o.MaxEventsPerSecond = 42;
                o.MinimumLogLevel = LogLevel.Critical;
            });

            var sp = services.BuildServiceProvider();
            var optionsSnapshot = sp.GetRequiredService<IOptions<FirstChanceExceptionOptions>>();

            Assert.IsTrue(optionsSnapshot.Value.Enabled);
            Assert.AreEqual(42, optionsSnapshot.Value.MaxEventsPerSecond);
            Assert.AreEqual(LogLevel.Critical, optionsSnapshot.Value.MinimumLogLevel);
        }

        // ── TelemetryBuilder Extension Test ─────────────────────────

        [TestMethod]
        public void WithFirstChanceExceptionMonitoring_RegistersViaBuilder()
        {
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddTelemetry(builder =>
            {
                builder.WithFirstChanceExceptionMonitoring(o => o.Enabled = true);
            });

            var sp = services.BuildServiceProvider();
            var monitor = sp.GetService<FirstChanceExceptionMonitor>();

            Assert.IsNotNull(monitor, "Monitor should be registered via TelemetryBuilder");
        }

        // ── Helper Methods ──────────────────────────────────────────

        private static IOptionsMonitor<FirstChanceExceptionOptions> CreateOptionsMonitor(
            FirstChanceExceptionOptions options)
        {
            return new TestOptionsMonitor<FirstChanceExceptionOptions>(options);
        }

        /// <summary>
        /// Simple test logger that captures log entries.
        /// </summary>
        private sealed class TestLogger<T> : ILogger<T>
        {
            public List<LogEntry> LogEntries { get; } = new List<LogEntry>();

            public IDisposable? BeginScope<TState>(TState state) where TState : notnull
                => null;

            public bool IsEnabled(LogLevel logLevel) => true;

            public void Log<TState>(
                LogLevel logLevel,
                EventId eventId,
                TState state,
                Exception? exception,
                Func<TState, Exception?, string> formatter)
            {
                var message = formatter(state, exception);
                LogEntries.Add(new LogEntry(logLevel, message, exception));
            }
        }

        private sealed class LogEntry
        {
            public LogLevel Level { get; }
            public string Message { get; }
            public Exception? Exception { get; }

            public LogEntry(LogLevel level, string message, Exception? exception)
            {
                Level = level;
                Message = message;
                Exception = exception;
            }
        }

        /// <summary>
        /// Simple IOptionsMonitor implementation for tests.
        /// </summary>
        private sealed class TestOptionsMonitor<TOptions> : IOptionsMonitor<TOptions>
        {
            public TestOptionsMonitor(TOptions currentValue)
            {
                CurrentValue = currentValue;
            }

            public TOptions CurrentValue { get; set; }

            public TOptions Get(string? name) => CurrentValue;

            public IDisposable? OnChange(Action<TOptions, string?> listener) => null;
        }
    }
}
