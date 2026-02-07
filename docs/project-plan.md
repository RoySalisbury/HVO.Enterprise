# Plan: HVO.Enterprise Logging & Telemetry Library (Final)

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

## Steps

### 1. Create core netstandard2.0 package with Microsoft abstractions
Set up [HVO.Enterprise/HVO.Enterprise.Telemetry/](HVO.Enterprise/HVO.Enterprise.Telemetry/) targeting netstandard2.0; dependencies: `System.Diagnostics.DiagnosticSource` (v8.0.1), `OpenTelemetry.Api` (v1.9.0), `Microsoft.Extensions.Logging.Abstractions`, `Microsoft.Extensions.DependencyInjection.Abstractions`, `Microsoft.Extensions.Diagnostics.HealthChecks.Abstractions` (v8.0.0), `Microsoft.Extensions.Configuration.Abstractions`, `System.Threading.Channels` (v7.0.0), `System.Net.Http`; create folders: `Abstractions/`, `ActivitySources/`, `Metrics/`, `Correlation/`, `Proxies/`, `Http/`, `HealthChecks/`, `Configuration/`, `Lifecycle/`, `Enrichers/`, `BackgroundJobs/`, `Exceptions/`

### 2. Implement auto-managed correlation with AsyncLocal
Create `CorrelationContext` using `AsyncLocal<string>` for async-aware correlation ID storage (works .NET Framework 4.6.1+ via netstandard2.0); built into `IOperationScope`: check AsyncLocal, then `Activity.Current?.TraceId`, auto-generate Guid if missing; provide `Telemetry.BeginCorrelationScope(correlationId)` returning IDisposable that sets AsyncLocal; automatically tag Activities and metrics; expose `CorrelationContext.Current` property; support custom `ICorrelationIdProvider`

### 3. Build background job correlation utilities
Create `BackgroundJobContext` class capturing correlation ID, user context, parent Activity context at job enqueue time; provide `[TelemetryJobContext]` attribute for job methods that auto-restores context; implement `IBackgroundJobContextPropagator` interface for Hangfire/Quartz integration; extension methods: `correlationId.EnqueueJob(() => ...)` captures current context, job execution restores it; document integration patterns for Hangfire, Quartz, hosted services; ensure correlation flows through job queues

### 4. Build bounded queue with Channel-based worker
Implement `TelemetryBackgroundWorker` using `System.Threading.Channels` (netstandard2.0 via NuGet) with `BoundedChannelFullMode.DropOldest` (default 10,000, configurable); dedicated background thread processes expensive operations (JSON serialization, logging); track `DroppedEventsCount` with one-time warnings per operation type; expose queue metrics in statistics; graceful shutdown via `FlushAsync(timeout)` with CancellationToken; document Channel advantages over BlockingCollection

### 5. Create lifecycle management with AppDomain awareness
Implement `TelemetryLifecycleManager` hooking `AppDomain.DomainUnload` for automatic flush and disposal (all .NET Framework versions); detect IIS via `System.Web.Hosting.HostingEnvironment.IsHosted` property check (no reflection—property returns false if type unavailable); static initialization: `Telemetry.Initialize(config)` with thread-safe Lazy<T> pattern; automatic cleanup on AppDomain unload; explicit `Telemetry.Shutdown(timeout)` for guaranteed flush; document lifecycle differences: .NET Framework uses AppDomain events, .NET 8+ can also use IHostApplicationLifetime

### 6. Implement accurate timing with runtime-adaptive metrics
Design `IOperationScope` capturing: start/end via `Stopwatch.GetTimestamp()` (high-precision), duration, success/failure, exceptions; skip per-operation CPU/memory (inaccurate in multi-threaded); implement runtime detection for `System.Diagnostics.Metrics.Meter` (available .NET 6+); on .NET 6+: use Meter histogram, on .NET Framework 4.8: use EventCounters with reservoir sampling for P50/P95/P99 percentiles; Dispose synchronous (~1-5μs), queues expensive work; document Meter vs EventCounter differences

### 7. Build exception tracking and aggregation
Create `ExceptionTracker` that generates exception fingerprints based on: exception type, message pattern (via regex to normalize variable parts), stack trace top 3 frames; track unique exception types, error rates, exception counts per operation; expose in `ITelemetryStatistics`: `ErrorRatePercent`, `UniqueExceptionTypes`, `TopExceptionTypes`, `TotalExceptions`; `IOperationScope.SetException(ex)` automatically calculates fingerprint, increments counters; provide `ExceptionAggregator` for querying exception trends; document fingerprinting algorithm

### 8. Implement configuration hot reload
Create `TelemetryConfiguration` with file system watcher (or IOptionsMonitor integration for .NET Core); support runtime updates: `UpdateSampling(rate, detailLevel)`, `UpdateQueueCapacity(size)`, `UpdateActivitySourceSampling(sourceName, rate)`; changes apply immediately without restart; provide optional HTTP endpoint: `[HttpPost("/api/telemetry/config")]` for remote updates (opt-in for security); emit log event when configuration changes; persist changes to file (optional); document security considerations for production environments

### 9. Build multi-level configuration with diagnostic API
Create `TelemetryConfiguration` with precedence: call > method > type > global; properties: `DetailLevel`, `SamplingRate`, `CaptureParameters`, `QueueCapacity`, `EnableUserContext`, `EnableRequestContext`; `[TelemetryOptions]` attribute on classes/methods (read via reflection, cached in ConcurrentDictionary); provide `.GetEffectiveConfiguration(Type, MethodInfo)` for troubleshooting; fluent builder and appsettings.json binding via `Microsoft.Extensions.Configuration.Binder`; support method-specific overrides via configuration: `"MethodOverrides": { "MyService.MyMethod": { ... } }`

### 10. Create ActivitySource with probabilistic sampling
Build `TelemetrySources` registry with `ActivityListener` (netstandard2.0 via System.Diagnostics.DiagnosticSource 5.0+); sampling modes: `AllDataAndRecorded` (dev), `PropagationData` (prod—IDs only); per-source: `.AddActivitySource("OneConsole.*", 0.1)`; per-call override: `TrackOperation(name, samplingRate: 1.0)`; thread-static Random for sampling decision (avoid lock contention); sampled-out Activity ~5ns, sampled-in ~30ns; coordinate with detail level: sampled-out records minimal metrics only; Activity.TraceId used as correlation ID if none explicitly set

### 11. Implement automatic user and request context enrichment
Create `UserContextEnricher` capturing authenticated user: `user.id` (identity name), `user.roles` (claims), `user.auth_type` (authentication scheme); create `RequestContextEnricher` capturing: `http.client_ip` (remote IP), `http.user_agent`, `http.referer`, `http.request_id`, `deployment.environment`, `host.name` (machine name), `service.version` (assembly version); automatically apply to all Activities and operation scopes when enabled via `.WithUserContextEnrichment()` and `.WithRequestContextEnrichment()`; support custom enrichers via `ITelemetryEnricher` interface; document PII considerations and data retention policies

### 12. Implement lightweight scope with performance monitoring
Create `OperationScope : IOperationScope` from `TrackOperation(name, detailLevel?, samplingRate?)`: immediate timestamp capture (<5ns via Stopwatch.GetTimestamp), struct TagList for primitives (zero heap allocation); `AddProperty(key, value)` fast-path primitives (<10ns), deferred ToString for objects, queues JSON to background; Dispose calculates duration synchronously, records metrics, applies enrichers; if `.WithPerformanceMonitoring()`: nested Stopwatch tracks telemetry overhead itself, warns if >1%/>10ms; expose overhead statistics; typical overhead: <50ns for tracking, ~1-5μs for Dispose

### 13. Build automatic ILogger enrichment
Create `TelemetryLoggerProvider : ILoggerProvider` wrapping existing ILoggerFactory; intercept all `ILogger.Log()` calls, automatically begin scope with: `Activity.Current` properties (TraceId, SpanId, ParentSpanId, Baggage), `CorrelationContext.Current`, user context (if enabled), request context (if enabled); extension: `.WithLoggingEnrichment()` registers provider wrapper; works with any ILogger implementation (Serilog, NLog, Console, App Insights); overhead ~5-10μs per log statement; document that this enables automatic correlation between logs and traces without manual scope management

### 14. Build DispatchProxy with attribute caching
Implement `TelemetryDispatchProxy<T>` (DispatchProxy available in netstandard2.0 via System.Reflection.DispatchProxy NuGet or built-in .NET Core 2.0+); read `[TelemetryOptions]` from interface methods, cache in ConcurrentDictionary<MethodInfo, TelemetryOptions> (one-time reflection); interception ~50-200ns per call; timing always synchronous (Stopwatch wraps actual invocation), parameter capture respects configured detail level; support `[NoTelemetry]` attribute for passthrough (~5ns); factory `TelemetryProxyFactory.Create<T>(target, defaultOptions?)`; handle async (Task/Task<T>/ValueTask) correctly preserving contexts

### 15. Create tiered parameter capture with sensitivity
Implement `ParameterCaptureStrategy`: None (0ns—skip), NameOnly (<10ns—name + type), Values (50-500ns—ToString primitives), FullJson (100μs-10ms—background JSON with max depth 3, cycle detection via System.Text.Json); `SensitiveDataDetector` with cached regex checking parameter/property names (password, token, ssn, apikey, creditcard, authorization); respect `[Sensitive]` attribute—always redact regardless of level; emit one-time warning when FullJson first used per method; default Normal detail with NameOnly capture

### 16. Add comprehensive statistics API and health checks
Create `ITelemetryStatistics`: `TotalOperations`, `OperationsPerSecond`, `AverageDurationMs`, `P50/P95/P99DurationMs`, `TelemetryOverheadAvgNs`, `TelemetryOverheadMaxNs`, `DroppedEvents`, `BackgroundQueueDepth`, `ErrorRatePercent`, `UniqueExceptionTypes`, `TopExceptionTypes[10]`; enable performance tracking via `.WithPerformanceMonitoring()` (+~20ns overhead); implement `TelemetryHealthCheck : IHealthCheck` (Microsoft.Extensions.Diagnostics.HealthChecks.Abstractions—netstandard2.0); warn on: overhead >1%/>10ms, queue >80% full, drops >0, error rate >5%; expose via `Telemetry.GetStatistics()` static and `ITelemetryService.GetStatistics()`

### 17. Build HTTP instrumentation in core package
Create `TelemetryHttpMessageHandler : DelegatingHandler`: start Activity with http.* semantic conventions (OpenTelemetry spec—http.method, http.url, http.status_code, http.request.body.size, http.response.body.size), inject W3C TraceContext headers (traceparent/tracestate), track duration, handle exceptions, set Activity status on errors; overhead ~50-100ns per request; extension methods: `services.AddTelemetryHttpClient<TClient>()` or `httpClientBuilder.AddTelemetryHandler()`; works on both .NET Framework 4.8 and .NET Core via System.Net.Http; document HttpClient best practices per platform

### 18. Build DI registration and static initialization
Create `ServiceCollectionExtensions.AddTelemetry(options)` (Microsoft.Extensions.DependencyInjection.Abstractions—netstandard2.0): `.WithActivitySources()`, `.WithMetrics()`, `.WithSampling()`, `.WithPerformanceMonitoring()`, `.WithLoggingEnrichment()`, `.WithUserContextEnrichment()`, `.WithRequestContextEnrichment()`, `.EnableHotReload()`, `.WithQueueCapacity()`; static API: `Telemetry.Initialize(config => ...)` with Lazy<T> thread-safety; both share implementation; document DI availability: .NET Framework 4.6.1+ via NuGet, .NET Core 2.0+/.NET 5+ built-in

### 19. Create extension packages and common library structure
Set up separate projects: [HVO.Common/](HVO.Enterprise/HVO.Common/) (netstandard2.0 for Result<T>, OneOf<T>, common abstractions and patterns - usable across all HVO projects, not just Enterprise), [HVO.Enterprise.Telemetry.IIS/](HVO.Enterprise/HVO.Enterprise.Telemetry.IIS/) (System.Web), [HVO.Enterprise.Telemetry.Wcf/](HVO.Enterprise/HVO.Enterprise.Telemetry.Wcf/) (System.ServiceModel.Primitives), [HVO.Enterprise.Telemetry.Database/](HVO.Enterprise/HVO.Enterprise.Telemetry.Database/) (database instrumentation), [HVO.Enterprise.Telemetry.Serilog/](HVO.Enterprise/HVO.Enterprise.Telemetry.Serilog/) (Serilog), [HVO.Enterprise.Telemetry.AppInsights/](HVO.Enterprise/HVO.Enterprise.Telemetry.AppInsights/) (Microsoft.ApplicationInsights), [HVO.Enterprise.Telemetry.Datadog/](HVO.Enterprise/HVO.Enterprise.Telemetry.Datadog/) (OpenTelemetry exporters); each provides extension methods; include README.md per package

### 20. Implement IIS extension with HostingEnvironment integration
Create IIS package (netstandard2.0 with System.Web reference); implement `IISRegisteredObject : IRegisteredObject` for graceful AppDomain recycle integration; extension `.ForIIS()` calls `HostingEnvironment.RegisterObject()`, hooks `Stop(immediate)` to call `Telemetry.FlushAsync()` and `Shutdown()`; ensures telemetry queue flushed before IIS recycle; automatically captures IIS-specific context: `iis.site_name`, `iis.app_pool`; document Global.asax.cs initialization, Application_Start/Application_End patterns

### 21. Implement WCF extension with message inspectors
Create WCF package (netstandard2.0 with System.ServiceModel.Primitives); build `TelemetryClientMessageInspector : IClientMessageInspector` (client calls) and `TelemetryDispatchMessageInspector : IDispatchMessageInspector` (service operations); inject/extract W3C TraceContext in SOAP headers (traceparent/tracestate format); create Activities with rpc.* semantic conventions (rpc.system=wcf, rpc.service, rpc.method); `TelemetryEndpointBehavior : IEndpointBehavior` for easy attachment; overhead <1μs per call; document configuration-based (<behaviorExtensions>) and programmatic attachment patterns

### 22. Implement Database extension with multi-provider support
Create Database package (netstandard2.0 with provider-specific dependencies); build interceptors: `EntityFrameworkCoreInterceptor : DbCommandInterceptor` (EF Core), `DapperInterceptor` (via CommandDefinition wrapper), `AdoNetProfiler` (DbProviderFactory wrapper); create Activities with db.* semantic conventions (db.system=sqlserver/postgresql/mysql, db.name, db.statement, db.operation=SELECT/INSERT/UPDATE/DELETE, db.statement_type); configurable query text capture with parameter redaction; support Redis via StackExchange.Redis profiler, MongoDB via event subscribers; document provider-specific setup, PII concerns

### 23. Implement Serilog extension with enrichers
Create Serilog package (netstandard2.0 with Serilog dependency); build `ActivityTelemetryEnricher : ILogEventEnricher` adding Activity.Current properties (TraceId, SpanId, ParentId, Baggage, custom tags); `CorrelationIdEnricher` from `CorrelationContext.Current`; `UserContextEnricher` and `RequestContextEnricher` from telemetry context; extension `.WithSerilogEnrichment()`; document Serilog configuration: `.Enrich.With<ActivityTelemetryEnricher>()`, LogContext usage, structured logging patterns; ensure log-trace correlation via shared IDs

### 24. Implement AppInsights extension with dual-mode bridge
Create AppInsights package (netstandard2.0 with Microsoft.ApplicationInsights); build `ActivityTelemetryInitializer : ITelemetryInitializer` capturing Activity.Current tags in App Insights RequestTelemetry/DependencyTelemetry; `CorrelationInitializer` for correlation ID in custom dimensions; for .NET Framework 4.8: include `EventCounterAppInsightsBridge : EventListener` converting EventCounters to `MetricTelemetry`, push via `TelemetryClient`; for .NET 6+: document Azure.Monitor.OpenTelemetry.Exporter integration; automatic runtime detection selects appropriate path; document connection string, sampling, data retention

### 25. Implement Datadog extension with dual-mode export
Create Datadog package (netstandard2.0 with OpenTelemetry.Exporter.OpenTelemetryProtocol); configure OTLP exporter for Datadog agent (default localhost:4317); for .NET Framework 4.8: include `EventCounterDatadogBridge : EventListener` converting EventCounters to DogStatsD format (counters, histograms, gauges), push via UDP to agent (default localhost:8125); for .NET 6+: use OpenTelemetry Meter with OTLP exporter; extension `.WithDatadogExporter(endpoint)` auto-detects runtime, selects path; document agent configuration, unified service tagging, OpenTelemetry Collector pattern

### 26. Create comprehensive unit test project
Set up [HVO.Enterprise.Telemetry.Tests/](HVO.Enterprise/HVO.Enterprise.Telemetry.Tests/) targeting net8.0; use xUnit; test coverage: CorrelationContext AsyncLocal flow, background job context propagation, ActivitySource sampling decisions, DispatchProxy interception with attributes, parameter capture levels and sensitivity, sensitive data redaction, queue backpressure/drop behavior, performance monitoring accuracy, lifecycle (AppDomain.Unload simulation), configuration precedence and hot reload, exception fingerprinting/aggregation, health checks, statistics calculations, ILogger enrichment; integration tests for HTTP/WCF/Database instrumentation; mock ILogger, Activity listeners, exporters; aim for >85% code coverage; also include tests for HVO.Common (Result<T>, OneOf<T> patterns)

### 27. Create .NET Framework 4.8 sample application
Set up [HVO.Enterprise.Samples.Net48/](HVO.Enterprise/HVO.Enterprise.Samples.Net48/) targeting net48; structure: ASP.NET MVC + WebAPI + WCF service + Hangfire background jobs + Console app; demonstrate: static Telemetry API, IIS integration (.ForIIS()), WCF client/server with SOAP headers, manual TrackOperation scopes, DispatchProxy for interfaces, correlation ID flow across boundaries (HTTP → WCF → Background Job), health check via WebAPI endpoint, user/request context enrichment, Datadog EventCounter bridge, Serilog integration with enrichers, database instrumentation (EF6 + ADO.NET), exception aggregation dashboard, configuration hot reload, usage of HVO.Common (Result<T> for error handling); include Global.asax.cs, Web.config, README with setup

### 28. Create .NET 8 sample application
Set up [HVO.Enterprise.Samples.Net8/](HVO.Enterprise/HVO.Enterprise.Samples.Net8/) targeting net8.0; structure: ASP.NET Core minimal API + gRPC service + background worker + Hangfire jobs; demonstrate: DI-based ITelemetryService, .AddTelemetry() configuration, instrumented HttpClient, DispatchProxy via DI, automatic middleware Activity creation, health checks endpoint (`/health`, `/health/live`, `/health/ready`), OpenTelemetry Meter with Datadog OTLP export, Serilog enrichment, correlation in background jobs, appsettings.json configuration with hot reload, user/request context automatic capture, database instrumentation (EF Core), exception aggregation, configuration API endpoint, usage of HVO.Common (Result<T> for error handling); include Program.cs, controllers, services, docker-compose for Datadog agent, README with comparison to .NET Framework

### 29. Create comprehensive documentation
Create [README.md](HVO.Enterprise/README.md): quick start, feature matrix, package descriptions, performance characteristics, HVO.Common usage; [DIFFERENCES.md](HVO.Enterprise/DIFFERENCES.md): platform differences (AsyncLocal, Meter API, DispatchProxy, Health Checks, IHostApplicationLifetime, AppDomain.DomainUnload, IIS integration), logging pipeline explanation (logs vs metrics vs traces), correlation flow; [MIGRATION.md](HVO.Enterprise/MIGRATION.md): upgrade path .NET Framework 4.8 → .NET 8, breaking changes, code examples; [ARCHITECTURE.md](HVO.Enterprise/ARCHITECTURE.md): design decisions, extension points, performance budget; [ROADMAP.md](HVO.Enterprise/ROADMAP.md): v1.0 features vs future enhancements (v1.1+: local dev dashboard, message queue instrumentation, smart adaptive sampling, memory profiling, audit trail, span links, request/response body capture)

### 30. Design for future source generator extensibility
Define `IMethodInstrumentationStrategy` interface for DispatchProxyInstrumentation and future SourceGeneratorInstrumentation; ensure `[TelemetryOptions]` runtime-readable (reflection) and compile-time analyzable (source generators); structure for future `HVO.Enterprise.Telemetry.SourceGenerators` package; document: manual `TrackOperation()` primary pattern (full control), DispatchProxy for interfaces (automatic), future generators for sealed classes/non-virtual methods; keep API surface stable for v1.0; provide clear extension points for custom instrumentation strategies

## v1.0 Feature Summary

**Core Packages:**
- `HVO.Common` - Shared patterns (Result<T>, OneOf<T>, common abstractions) for all HVO projects
- `HVO.Enterprise.Telemetry` - Core telemetry library

**Core Telemetry Features (`HVO.Enterprise.Telemetry`):**
- ✅ ActivitySource distributed tracing with W3C TraceContext
- ✅ Runtime-adaptive metrics (Meter API on .NET 6+, EventCounters on .NET Framework 4.8)
- ✅ Auto-managed correlation IDs with AsyncLocal
- ✅ Background job correlation utilities
- ✅ Automatic ILogger enrichment (Activity/Correlation context injection)
- ✅ User and request context enrichment
- ✅ DispatchProxy automatic instrumentation for interfaces
- ✅ Manual TrackOperation scopes with IDisposable pattern
- ✅ Exception tracking and aggregation
- ✅ Configuration hot reload (file or API-based)
- ✅ Performance monitoring and statistics
- ✅ Health checks (Microsoft.Extensions.Diagnostics.HealthChecks.Abstractions)
- ✅ HTTP client instrumentation
- ✅ Lifecycle management (AppDomain-aware for .NET Framework 4.8)
- ✅ Bounded queue with drop-oldest backpressure
- ✅ Tiered parameter capture with sensitivity detection

**Extension Packages (v1.0):**
- ✅ `Telemetry.IIS` - IIS hosting integration
- ✅ `Telemetry.Wcf` - WCF service and client instrumentation
- ✅ `Telemetry.Database` - EF Core, EF6, Dapper, ADO.NET, Redis, MongoDB
- ✅ `Telemetry.Serilog` - Serilog enrichers
- ✅ `Telemetry.AppInsights` - Application Insights with dual-mode metrics bridge
- ✅ `Telemetry.Datadog` - Datadog with dual-mode metrics bridge

**Future Extensions (v1.1+):**
- 🔮 Local development dashboard (real-time telemetry viewer)
- 🔮 Message queue instrumentation (RabbitMQ, Service Bus, SQS)
- 🔮 Smart adaptive sampling (more on errors/slow operations)
- 🔮 Memory profiling (allocation tracking)
- 🔮 Audit trail (compliance logging)
- 🔮 Span links (complex distributed scenarios)
- 🔮 Request/response body capture
- 🔮 Business metrics tracking
- 🔮 Source generators (zero-overhead instrumentation)

## Project Structure

```
HVO.Enterprise/
├── HVO.Common/                             # Common library (netstandard2.0)
│   ├── Results/                                       # Result<T> pattern
│   ├── OneOf/                                         # OneOf<T> discriminated unions
│   ├── Abstractions/                                  # Common interfaces
│   ├── Extensions/                                    # Extension methods
│   └── Utilities/                                     # Shared utilities
├── HVO.Enterprise.Telemetry/                          # Core netstandard2.0 library
│   ├── Abstractions/
│   ├── ActivitySources/
│   ├── Metrics/
│   ├── Correlation/
│   ├── Proxies/
│   ├── Http/
│   ├── HealthChecks/
│   ├── Configuration/
│   ├── Lifecycle/
│   ├── Enrichers/
│   ├── BackgroundJobs/
│   ├── Exceptions/
│   └── Logging/
├── HVO.Enterprise.Telemetry.IIS/                      # IIS extension
├── HVO.Enterprise.Telemetry.Wcf/                      # WCF extension
├── HVO.Enterprise.Telemetry.Database/                 # Database extension
│   ├── EntityFramework/
│   ├── Dapper/
│   ├── AdoNet/
│   ├── Redis/
│   └── MongoDB/
├── HVO.Enterprise.Telemetry.Serilog/                  # Serilog extension
├── HVO.Enterprise.Telemetry.AppInsights/              # App Insights extension
├── HVO.Enterprise.Telemetry.Datadog/                  # Datadog extension
├── HVO.Enterprise.Telemetry.Tests/                    # Unit tests (net8.0)
├── HVO.Enterprise.Samples.Net48/                      # .NET Framework 4.8 sample
│   ├── WebApp/                                        # ASP.NET MVC + WebAPI
│   ├── WcfService/                                    # WCF service
│   ├── BackgroundJobs/                                # Hangfire jobs
│   └── ConsoleApp/                                    # Console app
├── HVO.Enterprise.Samples.Net8/                       # .NET 8 sample
│   ├── WebApi/                                        # ASP.NET Core API
│   ├── GrpcService/                                   # gRPC service
│   └── Worker/                                        # Background worker
├── README.md                                          # Main documentation
├── DIFFERENCES.md                                     # Platform differences
├── MIGRATION.md                                       # Upgrade guide
├── ARCHITECTURE.md                                    # Design documentation
├── ROADMAP.md                                         # v1.0 vs future features
└── HVO.Enterprise.sln                                 # Solution file
```

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
    // Start telemetry scope
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

## Usage Examples

### .NET Framework 4.8 Example

```csharp
// Global.asax.cs
using HVO.Enterprise.Telemetry;
using HVO.Enterprise.Telemetry.IIS;
using HVO.Enterprise.Telemetry.Wcf;
using HVO.Enterprise.Telemetry.Datadog;
using HVO.Common.Results; // Result<T> pattern

public class Global : HttpApplication
{
    protected void Application_Start(object sender, EventArgs e)
    {
        // Configure Serilog (logs destination)
        Log.Logger = new LoggerConfiguration()
            .WriteTo.File("logs/app.log")
            .WriteTo.DatadogLogs(apiKey: "xxx")
            .CreateLogger();
        
        // Initialize static telemetry singleton
        Telemetry.Initialize(config => config
            .WithActivitySources("MyApp.Orders", "MyApp.Customers")
            .WithSampling(samplingRate: 0.1, detailLevel: DetailLevel.Normal)
            .WithPerformanceMonitoring()
            .WithLoggingEnrichment(Log.Logger) // Auto-enrich logs
            .WithUserContextEnrichment()
            .WithRequestContextEnrichment()
            .WithDatadogExporter(options => 
            {
                options.AgentEndpoint = "http://localhost:8125"; // StatsD for metrics
            })
            .ForIIS()); // IIS-aware shutdown
    }
    
    protected void Application_End(object sender, EventArgs e)
    {
        Telemetry.Shutdown(timeout: TimeSpan.FromSeconds(10));
    }
}

// Service class - manual instrumentation
public class OrderService
{
    private readonly ILogger _logger = Log.ForContext<OrderService>();
    
    public Order CreateOrder(OrderRequest request)
    {
        using (var operation = Telemetry.TrackOperation("OrderService.CreateOrder"))
        {
            operation.AddProperty("orderId", request.OrderId);
            operation.AddProperty("customerId", request.CustomerId);
            
            // Logs automatically include TraceId, CorrelationId, user context
            _logger.Information("Processing order {OrderId}", request.OrderId);
            
            try
            {
                var order = ProcessOrder(request);
                operation.AddProperty("totalAmount", order.TotalAmount);
                return order;
            }
            catch (Exception ex)
            {
                operation.SetException(ex); // Tracks exception fingerprint
                throw;
            }
        }
    }
}

// WCF Service - automatic instrumentation
[ServiceContract]
[TelemetryOptions(DetailLevel = DetailLevel.Detailed, SamplingRate = 1.0)]
public interface IReservationService
{
    [OperationContract]
    [TelemetryOptions(CaptureParameters = CaptureLevel.Values)]
    Task<Reservation> GetReservationAsync(int reservationId);
    
    [OperationContract]
    [NoTelemetry] // Skip telemetry for health checks
    bool HealthCheck();
}

// Background job correlation
[TelemetryJobContext] // Auto-restores correlation context
public void ProcessOrderNotifications()
{
    using (var operation = Telemetry.TrackOperation("ProcessNotifications"))
    {
        // CorrelationContext.Current already set from job enqueue
        _logger.Information("Processing notifications");
    }
}

// Check telemetry health
var stats = Telemetry.GetStatistics();
Console.WriteLine($"Operations/sec: {stats.OperationsPerSecond}");
Console.WriteLine($"Error rate: {stats.ErrorRatePercent}%");
Console.WriteLine($"Telemetry overhead: {stats.TelemetryOverheadAvgNs}ns");
```

### .NET 8 Example

```csharp
// Program.cs
using HVO.Enterprise.Telemetry;
using HVO.Enterprise.Telemetry.Serilog;
using HVO.Enterprise.Telemetry.AppInsights;
using HVO.Enterprise.Telemetry.Datadog;
using HVO.Enterprise.Telemetry.Database;
using HVO.Common.Results; // Result<T> pattern

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
builder.Host.UseSerilog((context, config) =>
    config.ReadFrom.Configuration(context.Configuration)
          .WriteTo.Console()
          .WriteTo.DatadogLogs(apiKey: "xxx")
          .Enrich.WithSerilogEnrichment()); // Add Activity/Correlation enrichment

// Add App Insights for logs
builder.Services.AddApplicationInsightsTelemetry(
    builder.Configuration["ApplicationInsights:ConnectionString"]);

// Add telemetry with DI
builder.Services.AddTelemetry(options =>
{
    options.DefaultSamplingRate = 0.1;
    options.DefaultDetailLevel = DetailLevel.Normal;
})
.WithActivitySources("OneConsole.*")
.WithPerformanceMonitoring()
.WithLoggingEnrichment() // Auto-enrich all ILogger calls
.WithUserContextEnrichment()
.WithRequestContextEnrichment()
.EnableHotReload()
.WithAppInsightsIntegration(builder.Configuration["ApplicationInsights:ConnectionString"])
.WithDatadogExporter(options =>
{
    options.AgentEndpoint = "http://localhost:4317"; // OTLP for .NET 8
})
.WithDatabaseInstrumentation(); // Auto-instrument EF Core

// Register instrumented services
builder.Services.AddInstrumentedService<IReservationService, ReservationService>();

// Add instrumented HttpClient
builder.Services.AddTelemetryHttpClient<IPaymentServiceClient, PaymentServiceClient>(client =>
{
    client.BaseAddress = new Uri("https://api.payment.com");
});

// Add health checks
builder.Services.AddHealthChecks()
    .AddCheck<TelemetryHealthCheck>("telemetry");

var app = builder.Build();

app.MapHealthChecks("/health");
app.Run();

// Controller - manual instrumentation with DI
public class ReservationsController : ControllerBase
{
    private readonly IReservationService _reservationService;
    private readonly ITelemetryService _telemetry;
    private readonly ILogger<ReservationsController> _logger;
    
    public ReservationsController(
        IReservationService reservationService,
        ITelemetryService telemetry,
        ILogger<ReservationsController> logger)
    {
        _reservationService = reservationService;
        _telemetry = telemetry;
        _logger = logger;
    }
    
    [HttpGet("{id}")]
    public async Task<IActionResult> GetReservation(int id)
    {
        using (var operation = _telemetry.TrackOperation(
            "GetReservation",
            detailLevel: DetailLevel.Detailed,
            samplingRate: 1.0))
        {
            operation.AddProperty("reservationId", id);
            
            // Automatically enriched with TraceId, CorrelationId, user context
            _logger.LogInformation("Fetching reservation {Id}", id);
            
            var reservation = await _reservationService.GetAsync(id);
            
            if (reservation == null)
            {
                operation.AddProperty("found", false);
                return NotFound();
            }
            
            return Ok(reservation);
        }
    }
}

// Service with attributes
[TelemetryOptions(DetailLevel = DetailLevel.Normal)]
public interface IReservationService
{
    [TelemetryOptions(CaptureParameters = CaptureLevel.Values)]
    Task<Reservation> GetAsync(int id);
    
    [NoTelemetry]
    Task<bool> HealthCheckAsync();
}

// Background service with correlation
public class NotificationService : BackgroundService
{
    private readonly ITelemetryService _telemetry;
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            // Create correlation scope for batch
            using (var correlationScope = _telemetry.BeginCorrelationScope(
                $"batch-{DateTime.UtcNow:yyyyMMddHHmmss}"))
            {
                await ProcessBatch(stoppingToken);
            }
            
            await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
        }
    }
}

// Configuration in appsettings.json
{
  "Telemetry": {
    "DefaultSamplingRate": 0.1,
    "DefaultDetailLevel": "Normal",
    "QueueCapacity": 10000,
    "PerformanceMonitoring": true,
    "EnableUserContext": true,
    "EnableRequestContext": true,
    "ActivitySources": [
      { "Name": "OneConsole.*", "SamplingRate": 0.1 }
    ],
    "MethodOverrides": {
      "ReservationService.GetAsync": {
        "DetailLevel": "Full",
        "SamplingRate": 1.0
      }
    }
  }
}
```

This comprehensive plan provides a complete, production-ready telemetry library with clear separation between v1.0 features and future enhancements, extensive documentation, and samples demonstrating real-world usage patterns across both .NET Framework 4.8 and .NET 8.
