# Quick Reference: User Story Outlines for Remaining Stories

This document provides abbreviated outlines for the remaining user stories. Use these as starting points and expand using the template in CREATION-GUIDE.md.

## Core Package Stories

### US-005: Lifecycle Management (5 SP)
**Focus**: AppDomain.DomainUnload hooks, graceful shutdown, IIS detection
- Hook AppDomain.DomainUnload for automatic cleanup
- Detect IIS via HostingEnvironment.IsHosted property check
- Static Telemetry.Initialize() with thread-safe Lazy<T>
- Explicit Telemetry.Shutdown(timeout) for guaranteed flush
- Integration with US-004 (background queue flush)

### US-006: Runtime-Adaptive Metrics (8 SP)
**Focus**: Meter API (.NET 6+) vs EventCounters (.NET Framework 4.8)
- IMetricsRecorder interface for abstraction
- Runtime detection for System.Diagnostics.Metrics.Meter availability
- Meter implementation for .NET 6+ (histogram, counter, gauge)
- EventCounter implementation for .NET Framework 4.8
- Reservoir sampling for P50/P95/P99 percentiles in EventCounters
- Skip inaccurate CPU/memory metrics per operation

### US-007: Exception Tracking (3 SP)
**Focus**: Fingerprinting, aggregation, error rates
- ExceptionTracker generates fingerprints (type + message pattern + top 3 frames)
- Track unique exception types, error rates per operation
- IOperationScope.SetException(ex) auto-calculates fingerprint
- Expose in statistics: ErrorRatePercent, TopExceptionTypes[10]

### US-008: Configuration Hot Reload (5 SP)
**Focus**: FileSystemWatcher, IOptionsMonitor, runtime updates
- TelemetryConfiguration with FileSystemWatcher
- UpdateSampling(), UpdateQueueCapacity() apply immediately
- Optional HTTP endpoint for remote config (opt-in, security)
- Emit log event on configuration change

### US-009: Multi-Level Configuration (5 SP)
**Focus**: Global > Type > Method > Call precedence
- Four-level precedence system
- [TelemetryOptions] attribute on classes/methods
- GetEffectiveConfiguration(Type, MethodInfo) diagnostic API
- Fluent builder + appsettings.json binding
- Method-specific overrides in configuration

### US-010: ActivitySource Sampling (5 SP)
**Focus**: Probabilistic sampling, per-source configuration
- TelemetrySources registry with ActivityListener
- Sampling modes: AllDataAndRecorded, PropagationData
- Per-source: AddActivitySource("App.*", samplingRate: 0.1)
- Per-call override in TrackOperation()
- Thread-static Random for sampling decision

### US-011: Context Enrichment (5 SP)
**Focus**: User context, request context, PII handling
- UserContextEnricher: user.id, user.roles, user.auth_type
- RequestContextEnricher: http.client_ip, user_agent, environment
- Automatic application to Activities and operations
- ITelemetryEnricher interface for custom enrichers
- PII documentation and opt-in design

### US-012: Operation Scope (8 SP)
**Focus**: IOperationScope, Stopwatch timing, property capture
- TrackOperation(name, detailLevel?, samplingRate?) creates scope
- Stopwatch.GetTimestamp() for precise timing
- Struct TagList for primitives (zero allocation)
- AddProperty(key, value) fast-path for primitives
- Dispose calculates duration, records metrics, applies enrichers

### US-013: ILogger Enrichment (5 SP)
**Focus**: Automatic Activity/Correlation injection
- TelemetryLoggerProvider wraps ILoggerFactory
- Intercept ILogger.Log() calls
- Auto-begin scope with Activity.Current properties + CorrelationId
- Works with any ILogger implementation
- ~5-10μs overhead per log statement

### US-014: DispatchProxy Instrumentation (8 SP)
**Focus**: Attribute-based automatic instrumentation
- TelemetryDispatchProxy<T> for interfaces
- Read [TelemetryOptions] from methods, cache in ConcurrentDictionary
- ~50-200ns per call overhead
- [NoTelemetry] attribute for passthrough
- Handle async methods (Task/ValueTask) correctly

### US-015: Parameter Capture (5 SP)
**Focus**: Tiered capture, sensitive data detection
- ParameterCaptureStrategy: None, NameOnly, Values, FullJson
- SensitiveDataDetector with regex (password, token, ssn, etc.)
- Respect [Sensitive] attribute for redaction
- FullJson with max depth 3, cycle detection

### US-016: Statistics & Health Checks (5 SP)
**Focus**: ITelemetryStatistics, TelemetryHealthCheck
- ITelemetryStatistics: operations/sec, P50/P95/P99, overhead, drops, errors
- TelemetryHealthCheck implements IHealthCheck
- Warn on: overhead >1%, queue >80%, drops >0, errors >5%
- Expose via Telemetry.GetStatistics()

### US-017: HTTP Instrumentation (3 SP)
**Focus**: HttpMessageHandler, W3C TraceContext
- TelemetryHttpMessageHandler : DelegatingHandler
- Start Activity with http.* semantic conventions
- Inject W3C TraceContext headers (traceparent/tracestate)
- ~50-100ns overhead per request
- Extension methods for DI registration

### US-018: DI & Static Initialization (5 SP)
**Focus**: AddTelemetry(), Telemetry.Initialize()
- ServiceCollectionExtensions.AddTelemetry(options)
- Fluent builder: WithActivitySources(), WithMetrics(), etc.
- Static API: Telemetry.Initialize(config => ...)
- Both implementations share core
- Thread-safe Lazy<T> initialization

## Extension Package Stories

### US-020: IIS Extension (3 SP)
**Focus**: IRegisteredObject, HostingEnvironment.RegisterObject()
- IISRegisteredObject : IRegisteredObject
- Hook Stop(immediate) to flush and shutdown
- Capture IIS-specific context: iis.site_name, iis.app_pool
- Document Global.asax.cs patterns

### US-021: WCF Extension (5 SP)
**Focus**: Message inspectors, W3C in SOAP headers
- TelemetryClientMessageInspector, TelemetryDispatchMessageInspector
- Inject/extract W3C TraceContext in SOAP headers
- Activities with rpc.* semantic conventions
- TelemetryEndpointBehavior for easy attachment

### US-022: Database Extension (8 SP)
**Focus**: EF Core, EF6, Dapper, ADO.NET, Redis, MongoDB
- EntityFrameworkCoreInterceptor : DbCommandInterceptor
- DapperInterceptor via CommandDefinition wrapper
- AdoNetProfiler via DbProviderFactory wrapper
- Activities with db.* semantic conventions
- Configurable query capture with parameter redaction

### US-023: Serilog Extension (3 SP)
**Focus**: Activity enricher, correlation enricher
- ActivityTelemetryEnricher : ILogEventEnricher
- CorrelationIdEnricher from CorrelationContext
- UserContextEnricher, RequestContextEnricher
- Document .Enrich.With<>() configuration

### US-024: AppInsights Extension (5 SP)
**Focus**: Dual-mode bridge, telemetry initializers
- ActivityTelemetryInitializer : ITelemetryInitializer
- EventCounterAppInsightsBridge for .NET Framework 4.8
- Azure.Monitor.OpenTelemetry.Exporter for .NET 6+
- Runtime detection selects appropriate path

### US-025: Datadog Extension (5 SP)
**Focus**: OTLP + DogStatsD dual-mode
- OTLP exporter for .NET 6+ (metrics + traces)
- EventCounterDatadogBridge for .NET Framework 4.8
- DogStatsD UDP client for metrics
- Auto-detect runtime, select implementation

## Testing & Sample Stories

### US-026: Unit Test Project (30 SP)
**Focus**: Comprehensive tests, >85% coverage
- Test all core features with xUnit
- Mock ILogger, Activity listeners, exporters
- AsyncLocal flow tests
- Background job context tests
- DispatchProxy attribute tests
- Sensitive data redaction tests
- Queue backpressure tests
- Health check tests

### US-027: .NET Framework 4.8 Sample (13 SP)
**Focus**: ASP.NET MVC, WebAPI, WCF, Hangfire
- ASP.NET MVC + WebAPI project
- WCF service (client and server)
- Hangfire background jobs
- Console app
- Demonstrate static Telemetry API
- Global.asax.cs initialization
- Health check via WebAPI endpoint

### US-028: .NET 8 Sample (13 SP)
**Focus**: ASP.NET Core, gRPC, IHostedService
- ASP.NET Core minimal API
- gRPC service
- Background worker (IHostedService)
- Demonstrate DI-based ITelemetryService
- Health checks endpoint
- appsettings.json configuration
- docker-compose for Datadog agent

## Documentation Stories

### US-029: Project Documentation (8 SP)
**Focus**: README, guides, migration
- README.md: quick start, features, packages
- DIFFERENCES.md: platform differences explained
- MIGRATION.md: .NET Framework → .NET 8 upgrade path
- ARCHITECTURE.md: design decisions, patterns
- ROADMAP.md: v1.0 vs future features

### US-030: Future Extensibility (3 SP)
**Focus**: Extension points for v1.1+
- IMethodInstrumentationStrategy interface
- Source generator preparation
- [TelemetryOptions] compile-time analyzable
- Document extension points
- Keep API surface stable

## Common Patterns Across Stories

### Every Story Should Have

1. **User Story Format**: "As a [role], I want [feature], so that [benefit]"
2. **Acceptance Criteria**: Testable, specific conditions
3. **Technical Requirements**: Code samples, interfaces
4. **Testing Requirements**: Unit tests with code examples
5. **Performance Requirements**: Specific metrics
6. **Dependencies**: Blocked by / Blocks other stories
7. **Definition of Done**: Comprehensive checklist

### Common Testing Patterns

```csharp
// AsyncLocal flow test pattern
[Fact]
public async Task Feature_FlowsThroughAsyncBoundaries()
{
    var testValue = "test";
    // Set in current context
    await Task.Run(() =>
    {
        // Verify in async context
        Assert.Equal(testValue, /* actual value */);
    });
}

// Scope disposal test pattern
[Fact]
public void Feature_RestoresStateOnDispose()
{
    var original = /* get original state */;
    using (/* create scope */)
    {
        // Verify changed state
    }
    // Verify restored state
    Assert.Equal(original, /* current state */);
}

// Performance test pattern
[Fact]
public void Feature_MeetsPerformanceTarget()
{
    var sw = Stopwatch.StartNew();
    for (int i = 0; i < 1000000; i++)
    {
        // Perform operation
    }
    sw.Stop();
    
    var avgNs = sw.Elapsed.TotalNanoseconds / 1000000;
    Assert.True(avgNs < /* target ns */);
}
```

### Common Acceptance Criteria Patterns

**For Infrastructure Stories**:
- [ ] Builds successfully with zero warnings
- [ ] Works on both .NET Framework 4.8 and .NET 8
- [ ] Thread-safe implementation
- [ ] Proper disposal of resources

**For Feature Stories**:
- [ ] Core functionality works as specified
- [ ] Performance requirements met
- [ ] Error handling implemented
- [ ] Integration with existing features

**For Extension Stories**:
- [ ] Separate NuGet package
- [ ] Minimal dependencies
- [ ] Dual-mode implementation (if needed)
- [ ] Documentation includes setup examples

**For Test Stories**:
- [ ] >85% code coverage (core) / >70% (extensions)
- [ ] All edge cases covered
- [ ] Performance benchmarks included
- [ ] Integration tests included

## Estimation Guidelines

- **1-2 SP**: Simple utilities, straightforward implementations
- **3 SP**: Standard features with moderate testing
- **5 SP**: Complex features or significant integration
- **8 SP**: Major features with multiple aspects
- **13 SP**: Epic-level work requiring multiple sub-tasks

## Implementation Checklist Per Story

- [ ] Create feature branch
- [ ] Implement core functionality
- [ ] Add unit tests (TDD preferred)
- [ ] Add XML documentation
- [ ] Run performance benchmarks
- [ ] Create integration tests
- [ ] Update relevant documentation
- [ ] Code review
- [ ] Merge to main

## Questions to Answer Per Story

1. What is the user-facing value?
2. How do we test this works correctly?
3. What is the performance impact?
4. How does this work on .NET Framework 4.8?
5. How does this work on .NET 8?
6. What dependencies exist?
7. What can go wrong?
8. How do users configure/customize this?

---

Use this as a quick reference when creating the remaining detailed user stories. Expand each outline using the full template from CREATION-GUIDE.md.
