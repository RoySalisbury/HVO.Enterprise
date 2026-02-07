# US-026: Unit Test Project

**Status**: ❌ Not Started  
**Category**: Testing  
**Effort**: 30 story points  
**Sprint**: 9

## Description

As a **library developer**,  
I want to **create a comprehensive unit test suite with >85% code coverage and robust mocking strategies**,  
So that **I can ensure the HVO.Enterprise.Telemetry library works correctly across all scenarios and platforms**.

## Acceptance Criteria

1. **Test Project Structure**
   - [ ] `HVO.Enterprise.Telemetry.Tests` project created targeting `net8.0` and `net48`
   - [ ] All core telemetry features have comprehensive test coverage
   - [ ] Test project builds successfully with zero warnings
   - [ ] Tests run on both .NET Framework 4.8 and .NET 8

2. **Code Coverage**
   - [ ] >85% line coverage for core telemetry package
   - [ ] >90% coverage for critical paths (correlation, lifecycle, metrics)
   - [ ] Coverage reports generated automatically in CI/CD
   - [ ] Uncovered code documented with justification

3. **Test Categories Implemented**
   - [ ] Unit tests for all public APIs
   - [ ] Integration tests for cross-component scenarios
   - [ ] Performance tests using BenchmarkDotNet
   - [ ] Thread safety tests for concurrent scenarios
   - [ ] Platform compatibility tests (.NET Framework vs .NET 8)

4. **Mocking Strategy**
   - [ ] Activity and ActivitySource mocking patterns established
   - [ ] ILogger mocking using xUnit captured logging
   - [ ] Configuration mocking with IConfiguration/IOptionsMonitor
   - [ ] Time abstraction for deterministic timing tests
   - [ ] Background queue testing without actual delays

5. **Test Quality**
   - [ ] All tests follow AAA pattern (Arrange, Act, Assert)
   - [ ] Test names clearly describe what is being tested
   - [ ] Tests are independent and can run in any order
   - [ ] No flaky tests (deterministic execution)
   - [ ] Fast execution (<5 minutes for full suite)

## Technical Requirements

### Test Project Configuration

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFrameworks>net8.0;net48</TargetFrameworks>
    <LangVersion>latest</LangVersion>
    <Nullable>enable</Nullable>
    <ImplicitUsings>disable</ImplicitUsings>
    <IsPackable>false</IsPackable>
    <IsTestProject>true</IsTestProject>
  </PropertyGroup>

  <ItemGroup>
    <!-- Testing Frameworks -->
    <PackageReference Include="xunit" Version="2.9.0" />
    <PackageReference Include="xunit.runner.visualstudio" Version="2.8.2" />
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.11.0" />
    
    <!-- Mocking and Assertions -->
    <PackageReference Include="Moq" Version="4.20.70" />
    <PackageReference Include="FluentAssertions" Version="6.12.0" />
    
    <!-- Code Coverage -->
    <PackageReference Include="coverlet.collector" Version="6.0.2" />
    <PackageReference Include="coverlet.msbuild" Version="6.0.2" />
    
    <!-- Performance Testing -->
    <PackageReference Include="BenchmarkDotNet" Version="0.13.12" />
    
    <!-- Test Utilities -->
    <PackageReference Include="Microsoft.Extensions.Logging.Testing" Version="8.0.0" />
    <PackageReference Include="Microsoft.Extensions.DependencyInjection" Version="8.0.0" />
  </ItemGroup>

  <ItemGroup>
    <!-- Project Under Test -->
    <ProjectReference Include="..\HVO.Enterprise.Telemetry\HVO.Enterprise.Telemetry.csproj" />
  </ItemGroup>
</Project>
```

### Test Folder Structure

```
HVO.Enterprise.Telemetry.Tests/
├── Correlation/
│   ├── CorrelationManagerTests.cs
│   ├── AsyncLocalCorrelationTests.cs
│   └── CorrelationPropagationTests.cs
├── ActivitySources/
│   ├── ActivitySourceManagerTests.cs
│   ├── ActivityEnrichmentTests.cs
│   └── SamplingTests.cs
├── Metrics/
│   ├── MetricsRecorderTests.cs
│   ├── RuntimeMetricsTests.cs
│   └── PerformanceCounterTests.cs
├── BackgroundQueue/
│   ├── BoundedQueueTests.cs
│   ├── QueueWorkerTests.cs
│   └── BacklogHandlingTests.cs
├── Lifecycle/
│   ├── LifecycleManagerTests.cs
│   ├── ShutdownTests.cs
│   └── AppDomainHooksTests.cs
├── Configuration/
│   ├── TelemetryConfigurationTests.cs
│   ├── HotReloadTests.cs
│   └── PrecedenceTests.cs
├── Instrumentation/
│   ├── OperationScopeTests.cs
│   ├── DispatchProxyTests.cs
│   └── ParameterCaptureTests.cs
├── Enrichers/
│   ├── LoggerEnrichmentTests.cs
│   ├── ContextEnrichmentTests.cs
│   └── PropertyCaptureTests.cs
├── Http/
│   ├── TelemetryHttpMessageHandlerTests.cs
│   └── W3CTraceContextTests.cs
├── Exceptions/
│   ├── ExceptionTrackingTests.cs
│   └── FingerprintingTests.cs
├── HealthChecks/
│   ├── TelemetryHealthCheckTests.cs
│   └── StatisticsTests.cs
├── Integration/
│   ├── EndToEndTests.cs
│   ├── CrossComponentTests.cs
│   └── PlatformCompatibilityTests.cs
├── Performance/
│   ├── CorrelationBenchmarks.cs
│   ├── MetricsBenchmarks.cs
│   ├── QueueBenchmarks.cs
│   └── InstrumentationBenchmarks.cs
└── Helpers/
    ├── TestActivitySource.cs
    ├── TestLogger.cs
    ├── TimeProvider.cs
    └── TestHelpers.cs
```

### Core Test Patterns

#### 1. Activity and Tracing Tests

```csharp
using System;
using System.Diagnostics;
using Xunit;
using FluentAssertions;

namespace HVO.Enterprise.Telemetry.Tests.ActivitySources
{
    public class ActivitySourceManagerTests
    {
        [Fact]
        public void StartActivity_ShouldCreateActivityWithCorrelationId()
        {
            // Arrange
            using var listener = new ActivityListener
            {
                ShouldListenTo = _ => true,
                Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded
            };
            ActivitySource.AddActivityListener(listener);
            
            var manager = new ActivitySourceManager("test-source");
            var expectedCorrelationId = Guid.NewGuid().ToString();
            CorrelationManager.CurrentCorrelationId = expectedCorrelationId;

            // Act
            using var activity = manager.StartActivity("test-operation");

            // Assert
            activity.Should().NotBeNull();
            activity!.GetBaggageItem("correlation-id").Should().Be(expectedCorrelationId);
        }

        [Fact]
        public void StartActivity_WithParent_ShouldPreserveTraceContext()
        {
            // Arrange
            using var listener = new ActivityListener
            {
                ShouldListenTo = _ => true,
                Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded
            };
            ActivitySource.AddActivityListener(listener);
            
            var manager = new ActivitySourceManager("test-source");

            // Act
            using var parent = manager.StartActivity("parent");
            var parentTraceId = parent!.TraceId;
            
            using var child = manager.StartActivity("child");

            // Assert
            child.Should().NotBeNull();
            child!.ParentId.Should().Be(parent.Id);
            child.TraceId.Should().Be(parentTraceId);
        }

        [Fact]
        public void Dispose_ShouldStopAllActiveSources()
        {
            // Arrange
            var manager = new ActivitySourceManager("test-source");
            using var activity = manager.StartActivity("test");

            // Act
            manager.Dispose();

            // Assert
            activity?.IsAllDataRequested.Should().BeFalse();
        }
    }
}
```

#### 2. Correlation Tests

```csharp
using System;
using System.Threading.Tasks;
using Xunit;
using FluentAssertions;

namespace HVO.Enterprise.Telemetry.Tests.Correlation
{
    public class AsyncLocalCorrelationTests
    {
        [Fact]
        public void CurrentCorrelationId_ShouldBeThreadLocal()
        {
            // Arrange
            var correlationId1 = Guid.NewGuid().ToString();
            var correlationId2 = Guid.NewGuid().ToString();
            string? taskCorrelationId = null;

            // Act
            CorrelationManager.CurrentCorrelationId = correlationId1;
            
            var task = Task.Run(() =>
            {
                CorrelationManager.CurrentCorrelationId = correlationId2;
                taskCorrelationId = CorrelationManager.CurrentCorrelationId;
            });
            task.Wait();

            var mainCorrelationId = CorrelationManager.CurrentCorrelationId;

            // Assert
            mainCorrelationId.Should().Be(correlationId1);
            taskCorrelationId.Should().Be(correlationId2);
        }

        [Fact]
        public async Task CurrentCorrelationId_ShouldFlowAcrossAsyncBoundaries()
        {
            // Arrange
            var correlationId = Guid.NewGuid().ToString();
            CorrelationManager.CurrentCorrelationId = correlationId;

            // Act
            var result = await GetCorrelationIdAsync();

            // Assert
            result.Should().Be(correlationId);
        }

        private async Task<string?> GetCorrelationIdAsync()
        {
            await Task.Delay(10);
            return CorrelationManager.CurrentCorrelationId;
        }

        [Fact]
        public void GenerateNewCorrelationId_ShouldCreateValidGuid()
        {
            // Act
            var correlationId = CorrelationManager.GenerateNewCorrelationId();

            // Assert
            correlationId.Should().NotBeNullOrWhiteSpace();
            Guid.TryParse(correlationId, out _).Should().BeTrue();
        }
    }
}
```

#### 3. Bounded Queue Tests

```csharp
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using FluentAssertions;

namespace HVO.Enterprise.Telemetry.Tests.BackgroundQueue
{
    public class BoundedQueueTests
    {
        [Fact]
        public async Task EnqueueAsync_ShouldProcessItems()
        {
            // Arrange
            var processed = 0;
            var queue = new BoundedTelemetryQueue(
                capacity: 100,
                processAsync: async (items, ct) =>
                {
                    Interlocked.Add(ref processed, items.Count);
                    await Task.CompletedTask;
                });

            queue.Start();

            // Act
            await queue.EnqueueAsync("item1");
            await queue.EnqueueAsync("item2");
            await queue.EnqueueAsync("item3");
            
            await Task.Delay(100); // Allow processing

            // Assert
            processed.Should().Be(3);
            
            // Cleanup
            await queue.StopAsync(CancellationToken.None);
        }

        [Fact]
        public async Task EnqueueAsync_WhenFull_ShouldDropOldestItems()
        {
            // Arrange
            var processedItems = new System.Collections.Concurrent.ConcurrentBag<string>();
            var processingDelay = new TaskCompletionSource<bool>();
            
            var queue = new BoundedTelemetryQueue(
                capacity: 2,
                processAsync: async (items, ct) =>
                {
                    await processingDelay.Task;
                    foreach (var item in items)
                    {
                        processedItems.Add(item);
                    }
                });

            queue.Start();

            // Act
            await queue.EnqueueAsync("item1");
            await queue.EnqueueAsync("item2");
            await queue.EnqueueAsync("item3"); // Should drop item1
            
            processingDelay.SetResult(true);
            await Task.Delay(100);

            // Assert
            processedItems.Should().NotContain("item1");
            processedItems.Should().Contain("item2");
            processedItems.Should().Contain("item3");
            
            // Cleanup
            await queue.StopAsync(CancellationToken.None);
        }

        [Fact]
        public async Task StopAsync_ShouldProcessRemainingItems()
        {
            // Arrange
            var processed = 0;
            var queue = new BoundedTelemetryQueue(
                capacity: 100,
                processAsync: async (items, ct) =>
                {
                    Interlocked.Add(ref processed, items.Count);
                    await Task.Delay(10, ct);
                });

            queue.Start();
            await queue.EnqueueAsync("item1");
            await queue.EnqueueAsync("item2");

            // Act
            await queue.StopAsync(CancellationToken.None);

            // Assert
            processed.Should().Be(2);
        }

        [Fact]
        public void GetStatistics_ShouldReturnAccurateMetrics()
        {
            // Arrange
            var queue = new BoundedTelemetryQueue(capacity: 100);
            queue.Start();

            // Act
            var stats = queue.GetStatistics();

            // Assert
            stats.Capacity.Should().Be(100);
            stats.CurrentCount.Should().BeGreaterOrEqualTo(0);
            stats.TotalEnqueued.Should().BeGreaterOrEqualTo(0);
            stats.TotalDropped.Should().BeGreaterOrEqualTo(0);
        }
    }
}
```

#### 4. Configuration Tests

```csharp
using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Xunit;
using FluentAssertions;
using Moq;

namespace HVO.Enterprise.Telemetry.Tests.Configuration
{
    public class TelemetryConfigurationTests
    {
        [Fact]
        public void LoadConfiguration_ShouldApplyDefaults()
        {
            // Arrange
            var config = new ConfigurationBuilder().Build();
            var options = new TelemetryOptions();

            // Act
            options.LoadFromConfiguration(config);

            // Assert
            options.ServiceName.Should().NotBeNullOrEmpty();
            options.EnableCorrelation.Should().BeTrue();
            options.EnableMetrics.Should().BeTrue();
            options.QueueCapacity.Should().Be(10000);
        }

        [Fact]
        public void LoadConfiguration_ShouldOverrideFromAppSettings()
        {
            // Arrange
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new[]
                {
                    new KeyValuePair<string, string>("Telemetry:ServiceName", "TestService"),
                    new KeyValuePair<string, string>("Telemetry:EnableCorrelation", "false"),
                    new KeyValuePair<string, string>("Telemetry:QueueCapacity", "5000")
                })
                .Build();

            var options = new TelemetryOptions();

            // Act
            options.LoadFromConfiguration(config);

            // Assert
            options.ServiceName.Should().Be("TestService");
            options.EnableCorrelation.Should().BeFalse();
            options.QueueCapacity.Should().Be(5000);
        }

        [Fact]
        public void HotReload_ShouldUpdateConfiguration()
        {
            // Arrange
            var configMock = new Mock<IOptionsMonitor<TelemetryOptions>>();
            var initialOptions = new TelemetryOptions { ServiceName = "Initial" };
            var updatedOptions = new TelemetryOptions { ServiceName = "Updated" };

            configMock.Setup(m => m.CurrentValue).Returns(initialOptions);

            var manager = new TelemetryConfigurationManager(configMock.Object);
            var changedCount = 0;

            manager.OnConfigurationChanged += (sender, args) => changedCount++;

            // Act
            configMock.Setup(m => m.CurrentValue).Returns(updatedOptions);
            configMock.Raise(m => m.OnChange += null, updatedOptions);

            // Assert
            changedCount.Should().Be(1);
            manager.CurrentConfiguration.ServiceName.Should().Be("Updated");
        }
    }
}
```

#### 5. Instrumentation Tests

```csharp
using System;
using System.Reflection;
using System.Threading.Tasks;
using Xunit;
using FluentAssertions;

namespace HVO.Enterprise.Telemetry.Tests.Instrumentation
{
    public class DispatchProxyTests
    {
        [Fact]
        public void CreateProxy_ShouldInterceptMethodCalls()
        {
            // Arrange
            var target = new TestService();
            var intercepted = false;

            var proxy = TelemetryProxy.Create<ITestService>(
                target,
                onInvoke: (method, args) => intercepted = true);

            // Act
            proxy.DoWork();

            // Assert
            intercepted.Should().BeTrue();
        }

        [Fact]
        public async Task CreateProxy_ShouldTrackAsyncMethods()
        {
            // Arrange
            var target = new TestService();
            Activity? capturedActivity = null;

            var proxy = TelemetryProxy.Create<ITestService>(
                target,
                activitySourceName: "test",
                onActivityCreated: activity => capturedActivity = activity);

            // Act
            await proxy.DoWorkAsync();

            // Assert
            capturedActivity.Should().NotBeNull();
            capturedActivity!.OperationName.Should().Contain("DoWorkAsync");
            capturedActivity.Status.Should().Be(ActivityStatusCode.Ok);
        }

        [Fact]
        public async Task CreateProxy_ShouldCaptureExceptions()
        {
            // Arrange
            var target = new TestService();
            Exception? capturedException = null;

            var proxy = TelemetryProxy.Create<ITestService>(
                target,
                onException: ex => capturedException = ex);

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(
                async () => await proxy.ThrowErrorAsync());

            capturedException.Should().NotBeNull();
            capturedException.Should().BeOfType<InvalidOperationException>();
        }

        public interface ITestService
        {
            void DoWork();
            Task DoWorkAsync();
            Task ThrowErrorAsync();
        }

        private class TestService : ITestService
        {
            public void DoWork() { }
            
            public async Task DoWorkAsync()
            {
                await Task.Delay(10);
            }

            public Task ThrowErrorAsync()
            {
                throw new InvalidOperationException("Test error");
            }
        }
    }
}
```

#### 6. Performance Benchmarks

```csharp
using System;
using System.Diagnostics;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;

namespace HVO.Enterprise.Telemetry.Tests.Performance
{
    [MemoryDiagnoser]
    [SimpleJob(warmupCount: 3, iterationCount: 5)]
    public class CorrelationBenchmarks
    {
        [Benchmark]
        public void GetCurrentCorrelationId()
        {
            var id = CorrelationManager.CurrentCorrelationId;
        }

        [Benchmark]
        public void SetCurrentCorrelationId()
        {
            CorrelationManager.CurrentCorrelationId = "test-correlation-id";
        }

        [Benchmark]
        public void GenerateNewCorrelationId()
        {
            var id = CorrelationManager.GenerateNewCorrelationId();
        }

        [Benchmark]
        public void WithCorrelationScope()
        {
            using (var scope = CorrelationManager.CreateScope())
            {
                var id = CorrelationManager.CurrentCorrelationId;
            }
        }
    }

    [MemoryDiagnoser]
    [SimpleJob(warmupCount: 3, iterationCount: 5)]
    public class ActivityBenchmarks
    {
        private ActivitySource _activitySource = null!;

        [GlobalSetup]
        public void Setup()
        {
            _activitySource = new ActivitySource("benchmark-source");
            
            var listener = new ActivityListener
            {
                ShouldListenTo = _ => true,
                Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded
            };
            ActivitySource.AddActivityListener(listener);
        }

        [Benchmark]
        public void StartStopActivity()
        {
            using var activity = _activitySource.StartActivity("test");
        }

        [Benchmark]
        public void StartActivityWithTags()
        {
            using var activity = _activitySource.StartActivity("test");
            activity?.SetTag("key1", "value1");
            activity?.SetTag("key2", "value2");
            activity?.SetTag("key3", "value3");
        }

        [GlobalCleanup]
        public void Cleanup()
        {
            _activitySource?.Dispose();
        }
    }
}
```

### Test Helpers

#### Time Provider for Deterministic Testing

```csharp
using System;

namespace HVO.Enterprise.Telemetry.Tests.Helpers
{
    /// <summary>
    /// Provides deterministic time for testing.
    /// </summary>
    public class TestTimeProvider : ITimeProvider
    {
        private DateTimeOffset _currentTime;

        public TestTimeProvider(DateTimeOffset? initialTime = null)
        {
            _currentTime = initialTime ?? DateTimeOffset.UtcNow;
        }

        public DateTimeOffset UtcNow => _currentTime;

        public void Advance(TimeSpan duration)
        {
            _currentTime = _currentTime.Add(duration);
        }

        public void SetTime(DateTimeOffset time)
        {
            _currentTime = time;
        }
    }

    public interface ITimeProvider
    {
        DateTimeOffset UtcNow { get; }
    }
}
```

#### Test Activity Source

```csharp
using System;
using System.Diagnostics;

namespace HVO.Enterprise.Telemetry.Tests.Helpers
{
    public class TestActivitySource : IDisposable
    {
        private readonly ActivitySource _source;
        private readonly ActivityListener _listener;

        public TestActivitySource(string name = "test-source")
        {
            _source = new ActivitySource(name);
            _listener = new ActivityListener
            {
                ShouldListenTo = source => source.Name == name,
                Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
                ActivityStarted = activity => ActivityStarted?.Invoke(activity),
                ActivityStopped = activity => ActivityStopped?.Invoke(activity)
            };
            
            ActivitySource.AddActivityListener(_listener);
        }

        public ActivitySource Source => _source;
        
        public event Action<Activity>? ActivityStarted;
        public event Action<Activity>? ActivityStopped;

        public void Dispose()
        {
            _listener?.Dispose();
            _source?.Dispose();
        }
    }
}
```

## Testing Requirements

### Unit Tests

1. **Correlation Tests** (>95% coverage)
   - AsyncLocal storage and flow
   - Cross-thread isolation
   - Scope management
   - Background job propagation

2. **Activity Source Tests** (>90% coverage)
   - Activity creation and disposal
   - Parent-child relationships
   - Baggage propagation
   - Sampling decisions

3. **Metrics Tests** (>85% coverage)
   - Counter increments
   - Histogram recordings
   - Runtime metrics collection
   - Platform-specific behavior

4. **Background Queue Tests** (>90% coverage)
   - Enqueue/dequeue operations
   - Capacity management
   - Backpressure handling
   - Graceful shutdown

5. **Configuration Tests** (>85% coverage)
   - Default values
   - Override precedence
   - Hot reload
   - Validation

### Integration Tests

1. **End-to-End Scenarios**
   - HTTP request with full telemetry
   - Background job with correlation
   - Exception tracking and aggregation
   - Cross-component trace propagation

2. **Platform Compatibility**
   - .NET Framework 4.8 specific tests
   - .NET 8 specific tests
   - Runtime feature detection

3. **DI Integration**
   - Service collection registration
   - Singleton lifecycle
   - Options pattern integration

### Performance Tests

1. **Overhead Benchmarks**
   - Correlation operations: <50ns
   - Activity start/stop: <100ns
   - Metric recording: <100ns
   - Queue enqueue: <100ns

2. **Throughput Tests**
   - Queue processing: >10,000 items/sec
   - Metric recording: >1,000,000 ops/sec
   - Activity creation: >100,000 ops/sec

3. **Memory Tests**
   - No memory leaks in long-running tests
   - Bounded memory growth
   - GC pressure analysis

## Performance Requirements

- **Test Execution Time**: Full suite <5 minutes
- **Individual Test Time**: <1 second per test
- **Coverage Calculation**: <30 seconds
- **Benchmark Execution**: <2 minutes per benchmark suite
- **CI/CD Integration**: Tests run on every commit

## Dependencies

**Blocked By**: 
- US-001 through US-018 (all core features must be implemented)
- US-020 through US-025 (extension packages to test)

**Blocks**: 
- US-029 (documentation depends on test results)
- Release readiness

## Definition of Done

- [ ] Test project created and configured
- [ ] >85% line coverage achieved for core package
- [ ] >70% coverage for extension packages
- [ ] All unit tests passing on .NET Framework 4.8
- [ ] All unit tests passing on .NET 8
- [ ] Performance benchmarks documented
- [ ] No flaky tests (100 consecutive runs pass)
- [ ] Code coverage reports integrated into CI/CD
- [ ] Test documentation complete
- [ ] Code reviewed and approved
- [ ] Zero warnings in test builds

## Notes

### Design Decisions

1. **Why xUnit over NUnit/MSTest?**
   - Modern, fast, parallel execution by default
   - Excellent integration with .NET Core and .NET Framework
   - Strong community and tooling support

2. **Why both Moq and FluentAssertions?**
   - Moq for mocking dependencies
   - FluentAssertions for readable, expressive assertions
   - Both are industry standards with great .NET Standard 2.0 support

3. **Why BenchmarkDotNet?**
   - De facto standard for .NET performance testing
   - Accurate measurements with statistical analysis
   - Prevents common benchmarking mistakes

4. **Why multi-target tests?**
   - Ensures compatibility on both platforms
   - Catches platform-specific bugs early
   - Validates runtime feature detection

### Implementation Tips

- Start with core features (correlation, activities, metrics)
- Write tests alongside implementation (TDD where possible)
- Use test helpers to reduce boilerplate
- Mock external dependencies (time, random, external services)
- Test both success and failure paths
- Include edge cases and boundary conditions
- Use theory tests for parameterized scenarios

### Common Pitfalls

- **Timing-based tests**: Use TestTimeProvider instead of actual delays
- **Random values**: Seed random generators for deterministic results
- **Static state**: Reset static state in test constructors/destructors
- **Async void**: Never use async void in tests
- **Test interdependence**: Each test must be fully independent
- **Over-mocking**: Mock only external dependencies, not internal logic

### Testing Anti-Patterns to Avoid

1. **Test interdependence**: Tests must not rely on execution order
2. **Hidden test setup**: All setup should be visible in the test or clearly named helper
3. **Testing implementation details**: Test behavior, not internal implementation
4. **Excessive mocking**: Don't mock what you own
5. **Slow tests**: Use TestTimeProvider, avoid actual delays

## Related Documentation

- [Project Plan](../project-plan.md#26-create-comprehensive-unit-tests)
- [xUnit Documentation](https://xunit.net/)
- [BenchmarkDotNet Documentation](https://benchmarkdotnet.org/)
- [Code Coverage Best Practices](https://learn.microsoft.com/en-us/dotnet/core/testing/unit-testing-code-coverage)
