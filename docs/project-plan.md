# HVO.Enterprise — Design Decisions & Background

> This document captures the original design rationale and key architectural decisions for the HVO.Enterprise telemetry library. For current project status, see the [Roadmap](ROADMAP.md). For technical details, see the [Architecture](ARCHITECTURE.md) guide.

Create a modular .NET Standard 2.0 telemetry library with core package (`HVO.Enterprise.Telemetry`) using Microsoft netstandard2.0 abstractions, extension packages for platform-specific integrations (`IIS`, `Wcf`, `Serilog`, `AppInsights`, `Datadog`, `Database`), a common library (`HVO.Common`) for shared patterns (Result<T>, OneOf<T>, etc.) usable across all HVO projects, automatic logging enrichment with Activity/Correlation context, user/request context capture, background job correlation utilities, exception aggregation, configuration hot reload, comprehensive unit tests (.NET 8+), and sample applications demonstrating .NET Framework 4.8 and .NET 8+ usage patterns with clear documentation of platform differences and future roadmap.

## Background & Context

### Original Requirements
The goal is to standardize logging and performance telemetry across **all .NET projects** (Framework 4.8 through .NET 8+) including legacy WCF services, ASP.NET applications, and modern ASP.NET Core APIs. The library must:

1. **Support diverse platforms**: .NET Framework 4.8, .NET 5+, .NET 6+, .NET 8+ with a **single binary** (no multi-targeting compilation)
2. **Provide unified observability**: Distributed tracing (ActivitySource), metrics (runtime-adaptive), and structured logging (ILogger integration)
3. **Work with existing infrastructure**: Integrate with existing Serilog setup, Application Insights, and Datadog
4. **Support legacy scenarios**: WCF services (both client and server), IIS hosting, non-DI environments
5. **Minimize friction**: Auto-enrichment, auto-correlation, minimal code changes required

### Key Design Decisions & Rationale

#### 1. **Single Binary Strategy (.NET Standard 2.0 Only)**
**Decision**: Target only `netstandard2.0`, not multi-target (net48, net6.0, net8.0)  
**Rationale**: 
- User explicitly wanted one binary that works everywhere to avoid deployment complexity
- .NET Standard 2.0 is compatible with .NET Framework 4.6.1+ and all modern .NET versions
- Use runtime detection (reflection-based checks) for features unavailable in older frameworks (e.g., Meter API)
- Accepted slight performance cost of runtime checks vs compile-time optimization for deployment simplicity

#### 2. **Runtime-Adaptive Metrics (Not Multi-Targeted)**
**Decision**: Use runtime detection to choose Meter API (.NET 6+) or EventCounters (.NET Framework 4.8)  
**Rationale**:
- System.Diagnostics.Metrics.Meter requires .NET 6+ runtime, not available in .NET Framework
- Cannot use compile-time conditionals (#if) in single-binary approach
- Implemented transparent fallback: same IMetricsRecorder interface, different backend based on runtime capability detection
- User prioritized deployment simplicity over micro-optimizations

#### 3. **Extension Packages for Platform-Specific Features**
**Decision**: Separate core from platform-specific integrations (IIS, WCF, Serilog, AppInsights, Datadog, Database)  
**Rationale**:
- Core package has minimal dependencies (no System.Web, no Serilog, no AppInsights SDKs)
- Reduces conflicts and allows users to opt-in only to needed integrations
- Each extension provides dual-mode bridges for .NET Framework 4.8 (EventCounter → Platform) vs .NET 6+ (Meter → OpenTelemetry)
- WCF is legacy tech primarily for .NET Framework 4.8, so separate package makes sense

#### 4. **Automatic ILogger Enrichment (Not Manual Scopes)**
**Decision**: Wrap ILoggerFactory to automatically inject Activity/Correlation context into all log statements  
**Rationale**:
- User wanted standard `ILogger.LogDebug()` calls to "just work" without passing extra parameters
- Opted for Option A (automatic enrichment via provider wrapper) over Option B (explicit scopes)
- Adds ~5-10μs overhead per log statement but eliminates manual correlation management
- Works with any ILogger implementation (Serilog, NLog, Console, App Insights)

#### 5. **Performance Requirements (Non-Blocking, Low-Overhead)**
**Critical Constraints**:
- Telemetry **MUST NOT block** normal execution
- Overhead must be **<100ns per operation** for tracking (excluding Dispose)
- Accurate timing is **mandatory** - no inaccurate CPU/memory metrics
- Heavy operations (JSON serialization, network calls) queued to background thread with bounded capacity (10,000 default)
- Drop-oldest strategy on queue overflow (prefer losing telemetry over OOM or blocking application)

**Performance Budget**:
- Activity start: ~5-30ns (depending on sampling)
- Property addition: <10ns (fast-path primitives)
- Dispose (flush): ~1-5μs (synchronous timing calculation)
- Background work: non-blocking (JSON, exporters)
- DispatchProxy overhead: 50-200ns per call (acceptable for business logic methods)

#### 6. **Correlation ID Management**
**Decision**: Auto-generate correlation IDs using AsyncLocal, fallback to Activity.TraceId  
**Rationale**:
- User wanted correlation "baked into the implementation" - no manual passing
- AsyncLocal (available in .NET Framework 4.6.1+ via netstandard2.0) flows through async/await
- Check AsyncLocal first, then Activity.Current?.TraceId, then auto-generate Guid
- `Telemetry.BeginCorrelationScope(id)` available for explicit control (e.g., reading from HTTP header `X_HGV_TransactionId`)
- Background jobs capture correlation at enqueue time, restore at execution time

#### 7. **Configuration Precedence & Hot Reload**
**Decision**: Four-level precedence (call > method > type > global) with runtime hot reload  
**Rationale**:
- Developers need fine-grained control: global defaults, type-level attributes, method-level overrides, call-level overrides
- Hot reload enables production troubleshooting without restart (critical for debugging issues)
- Configuration changes via file watcher or optional HTTP endpoint (opt-in for security)
- Diagnostic API (`GetEffectiveConfiguration(Type, MethodInfo)`) helps troubleshoot config issues

#### 8. **Exception Tracking & Aggregation**
**Decision**: Auto-generate exception fingerprints (type + normalized message + top 3 stack frames)  
**Rationale**:
- User wanted "as much insight as possible" without manual tracking at every line
- Fingerprinting enables grouping similar exceptions, tracking error rates, identifying top errors
- `IOperationScope.SetException(ex)` automatically calculates fingerprint, increments counters
- Exposed in statistics: ErrorRatePercent, UniqueExceptionTypes, TopExceptionTypes[10]

#### 9. **User & Request Context Enrichment**
**Decision**: Automatically capture authenticated user (id, roles) and request context (IP, user-agent, environment) when enabled  
**Rationale**:
- Essential for troubleshooting but opt-in due to PII concerns
- Applied to all Activities and operation scopes via enrichers when `.WithUserContextEnrichment()` / `.WithRequestContextEnrichment()` configured
- Follows OpenTelemetry semantic conventions (user.*, http.*, deployment.*, host.*)
- Documentation must address PII considerations and data retention policies

#### 10. **Health Checks using Microsoft Abstractions**
**Decision**: Use `Microsoft.Extensions.Diagnostics.HealthChecks.Abstractions` (netstandard2.0 package)  
**Rationale**:
- User wanted forward/backward compatibility - same interface on .NET Framework 4.8 and .NET 8
- Microsoft's abstraction package works everywhere (netstandard2.0)
- .NET 8 gets automatic middleware integration, .NET Framework 4.8 uses manual HttpHandler/WebAPI controller
- Seamless upgrade path - same `TelemetryHealthCheck` class works on both platforms

#### 11. **Lifecycle Management Strategy**
**Decision**: AppDomain.DomainUnload for automatic cleanup, optional IIS integration via separate package  
**Rationale**:
- User required "no memory leaks" and "IIS compatibility" but didn't want reflection or hard System.Web dependencies in core
- Core package: hooks AppDomain.DomainUnload (available everywhere), detects IIS via `HostingEnvironment.IsHosted` property check (no reflection)
- Separate IIS package: implements `IRegisteredObject` for graceful AppDomain recycle, calls `HostingEnvironment.RegisterObject()`
- User preference: separate packages over reflection-heavy core ("if a referenced DLL is not available... we should not include it")

#### 12. **Manual vs Automatic Instrumentation**
**Decision**: Provide both - manual `TrackOperation()` primary, DispatchProxy for interfaces, plan for future source generators  
**Rationale**:
- User initially wanted attribute-based method tracking `[TrackPerformance]` 
- Evaluated options: Source Generators (additive only), IL Weaving (Fody - complex), DispatchProxy (built-in, interface-only), Castle DynamicProxy (external dependency)
- Chose DispatchProxy (built-in, zero dependencies, ~50-200ns overhead) for v1.0
- Manual `TrackOperation()` remains primary pattern for full control and explicit visibility
- Future v1.1+: Source generators for sealed classes/non-virtual methods (zero overhead, but more complex)

#### 13. **Database Instrumentation Scope**
**Decision**: Separate Database extension package supporting EF Core, EF6, Dapper, ADO.NET, Redis, MongoDB  
**Rationale**:
- User mentioned "database calls" as common pain point for performance tracking
- High ROI feature for v1.0 - automatic db.* semantic convention tagging (db.system, db.statement, db.operation)
- Separate package keeps core minimal, users opt-in to provider-specific dependencies
- Configurable query text capture with parameter redaction (PII concerns)

#### 14. **Background Job Correlation**
**Decision**: Provide `[TelemetryJobContext]` attribute and manual `correlationId.EnqueueJob()` helpers  
**Rationale**:
- Common scenario: HTTP request → enqueue background job → lose correlation
- Capture correlation ID, user context, parent Activity at enqueue time
- Restore context at job execution time via attribute or manual correlation scope
- Patterns documented for Hangfire, Quartz, IHostedService

#### 15. **Logging vs Metrics vs Traces (Separate Pipelines)**
**Decision**: Three separate pipelines that share correlation context  
**Rationale**:
- User confusion about "does `.WithDatadogExporter()` also send logs?"
- **Logs** (ILogger) → Serilog sinks → Datadog Logs / App Insights Traces / Files
- **Metrics** (Telemetry) → Meter/EventCounters → Datadog Metrics / App Insights Metrics (via exporters)
- **Traces** (Activity) → ActivitySource → Datadog APM / App Insights Dependencies (via exporters)
- `.WithLoggingEnrichment()` adds Activity/Correlation to logs for correlation across all three
- `.WithDatadogExporter()` only sends metrics/traces, NOT logs (logs require separate Serilog sink)

### v1.0 vs Future Features Decisions

**Included in v1.0 (High ROI, Commonly Needed)**:
- ✅ User/Request context enrichment - Essential for troubleshooting
- ✅ Database instrumentation - Common performance pain point
- ✅ Background job correlation - Frequent scenario in distributed systems
- ✅ Exception aggregation - Useful for error monitoring
- ✅ Configuration hot reload - Critical for production debugging

**Deferred to v1.1+ (Lower Priority or Complexity)**:
- 🔮 Local development dashboard - Nice-to-have, can be built separately
- 🔮 Message queue instrumentation - Specialized, warrants separate package
- 🔮 Smart adaptive sampling - Complex algorithm, refine based on v1.0 feedback
- 🔮 Memory profiling - Niche, high overhead (~5-10%), opt-in only
- 🔮 Audit trail - Specialized compliance need, different from telemetry
- 🔮 Span links - Advanced distributed tracing scenario
- 🔮 Request/response body capture - PII concerns, expensive

### Technical Constraints Summary

**Must-Have**:
- Single binary deployment (netstandard2.0)
- Works on .NET Framework 4.8 through .NET 8+
- Non-blocking execution (<100ns overhead for tracking)
- Accurate timing metrics (skip inaccurate CPU/memory)
- Minimal dependencies in core package
- Forward/backward compatible using Microsoft abstractions where possible

**Must-Avoid**:
- Multi-targeting compilation (separate binaries per platform)
- Hard dependencies on platform-specific assemblies in core (System.Web, Serilog, etc.)
- Blocking application threads for telemetry operations
- Excessive reflection in hot paths (cache MethodInfo/PropertyInfo)
- Inaccurate metrics (better to skip than report wrong data)

**Performance Targets**:
- Activity start: ~5-30ns
- Property addition: <10ns (primitives)
- Operation Dispose: ~1-5μs
- DispatchProxy interception: 50-200ns
- ILogger enrichment: ~5-10μs
- Background queue: non-blocking with drop-oldest on overflow

This context should enable another agent to understand not just the implementation plan, but the reasoning behind key architectural decisions.

## Integration Flow: Logs, Metrics, and Traces

**The Three Pipelines (separate but correlated):**

1. **Logs** (via `ILogger`) → Log destinations (files, Datadog Logs, App Insights Traces)
2. **Metrics** (via Telemetry) → Metrics backends (Datadog Metrics, App Insights Metrics)
3. **Traces** (via Activity) → Trace backends (Datadog APM, App Insights Dependencies)

**Configuration Example (.NET 8):**

```csharp
// 1. Configure WHERE logs go (log destinations)
builder.Host.UseSerilog((context, config) =>
    config.ReadFrom.Configuration(context.Configuration)
          .WriteTo.Console()
          .WriteTo.File("logs/app.log")
          .WriteTo.DatadogLogs(apiKey: "xxx")); // Serilog sink for Datadog LOGS

// 2. Configure App Insights logs/traces
builder.Services.AddApplicationInsightsTelemetry(
    builder.Configuration["ApplicationInsights:ConnectionString"]);

// 3. Add Telemetry - enriches ILogger + sends metrics/traces separately
builder.Services.AddTelemetry(options => ...)
    .WithLoggingEnrichment() // ← Enriches all ILogger calls with Activity/Correlation
    .WithDatadogExporter(options =>
    {
        options.AgentEndpoint = "http://localhost:4317"; // For METRICS + TRACES
    })
    .WithAppInsightsIntegration(); // For METRICS + TRACES (separate from logs)
```

**What Goes Where:**

| Data Type | Source | Destination | Configuration |
|-----------|--------|-------------|---------------|
| **Logs** | `ILogger.LogDebug()` | File, Console | `builder.Logging.AddConsole()` |
| **Logs** | `ILogger.LogDebug()` | Datadog Logs | Serilog `.WriteTo.DatadogLogs()` |
| **Logs** | `ILogger.LogDebug()` | App Insights Traces | `AddApplicationInsightsTelemetry()` |
| **Metrics** | `Telemetry.TrackOperation()` | Datadog Metrics | `.WithDatadogExporter()` on Telemetry |
| **Metrics** | `Telemetry.TrackOperation()` | App Insights Metrics | `.WithAppInsightsIntegration()` on Telemetry |
| **Traces** | Activity from `TrackOperation()` | Datadog APM | `.WithDatadogExporter()` on Telemetry |
| **Traces** | Activity from `TrackOperation()` | App Insights Dependencies | `.WithAppInsightsIntegration()` on Telemetry |

**Automatic Correlation:**

```csharp
public async Task ProcessOrder(int orderId)
{
    using (var operation = _telemetry.TrackOperation("ProcessOrder"))
    {
        operation.AddProperty("orderId", orderId);

        // ILogger calls are AUTOMATICALLY enriched with:
        // - TraceId from Activity.Current
        // - SpanId from Activity.Current
        // - CorrelationId from CorrelationContext.Current
        _logger.LogInformation("Processing order {OrderId}", orderId);

        await _orderRepository.SaveAsync(order);

        _logger.LogDebug("Order saved to database");
    }
}
```
