# US-031: Sample Application — Extension Integration & End-to-End Telemetry Output

**GitHub Issue**: [#73](https://github.com/RoySalisbury/HVO.Enterprise/issues/73)  
**Status**: ❌ Not Started  
**Category**: Testing / Samples  
**Effort**: 13 story points  
**Sprint**: 11

## Description

As a **developer evaluating or integrating HVO.Enterprise.Telemetry**,  
I want to **see the sample application exercise every extension package with real or simulated infrastructure**,  
So that **I can verify that extension packages work correctly, see telemetry output in a readable format, and understand how to set up each integration in my own projects**.

## Background

US-028 (.NET 8 Sample Application) delivered a comprehensive weather monitoring Web API that exercises the **core telemetry library** features. However, several items were deferred because they require infrastructure (databases, message brokers) or extension packages that were only scaffolded as commented-out code:

### Deferred from US-028
1. ~~gRPC service + interceptor~~ *(deferred — library does not yet have gRPC interceptor support; out of scope)*
2. ~~OpenTelemetry exporter integration~~ *(deferred — library does not yet include OTel exporter packages)*
3. ~~Prometheus metrics endpoint~~ *(deferred — requires OTel exporter)*
4. Docker Compose file (Jaeger, Prometheus) — **optional** for this story
5. WebApplicationFactory integration tests
6. Native AOT compatibility notes

### Extension Packages with Commented-Out Registrations
The US-028 sample has disabled DI registration code for:
- **Serilog** — `HVO.Enterprise.Telemetry.Serilog`
- **Application Insights** — `HVO.Enterprise.Telemetry.AppInsights`
- **Datadog** — `HVO.Enterprise.Telemetry.Datadog`
- **IIS** — `HVO.Enterprise.Telemetry.IIS` *(not applicable to .NET 8; document only)*
- **WCF** — `HVO.Enterprise.Telemetry.Wcf` *(not applicable to .NET 8; document only)*
- **Database (EF Core)** — `HVO.Enterprise.Telemetry.Data.EfCore`
- **Database (ADO.NET)** — `HVO.Enterprise.Telemetry.Data.AdoNet`
- **Redis** — `HVO.Enterprise.Telemetry.Data.Redis`
- **RabbitMQ** — `HVO.Enterprise.Telemetry.Data.RabbitMQ`

### Goal
**Activate** as many of those integrations as possible using **lightweight, self-contained infrastructure** that doesn't require external services to be running. The sample should "just work" out of the box with `dotnet run`.

## Design Approach

### Strategy: No External Dependencies Required

The sample must remain runnable with zero external setup. Use the following approach for each integration:

| Extension | Strategy | Details |
|---|---|---|
| **Serilog** | Direct integration | Add Serilog console sink; enrich with HVO telemetry context |
| **App Insights** | Fake / in-memory channel | Use `InMemoryChannel` so telemetry serialises without needing Azure |
| **Datadog** | Console/text exporter | Write Datadog-formatted spans and metrics to the console or log file |
| **EF Core** | SQLite in-memory | `Data Source=:memory:` — creates a real relational DB that lives in-process |
| **ADO.NET** | SQLite via `Microsoft.Data.Sqlite` | Raw ADO.NET commands against the same or separate SQLite connection |
| **Redis** | In-process fake or no-op | Use `FakeRedis` or a stub `IDistributedCache` that logs operations |
| **RabbitMQ** | In-process channel simulation | Use `System.Threading.Channels` to simulate publish/consume with telemetry |
| **IIS** | Documentation only | Not applicable to .NET 8; document how it would work in .NET Framework |
| **WCF** | Documentation only | Not applicable to .NET 8; cross-reference with US-027 (.NET 4.8 sample) |

### Detailed Design Per Extension

#### 1. Serilog Integration
- Add `Serilog.AspNetCore` and `Serilog.Sinks.Console` packages
- Wire up `Host.UseSerilog()` with the HVO enricher (`Enrich.WithHvoTelemetry()`)
- Console output template includes `{CorrelationId}`, `{TraceId}`, `{SpanId}`
- Create a toggle in `appsettings.json` to enable/disable Serilog vs. default `ILogger`
- **Shows**: How Serilog enrichment works alongside the built-in `AddTelemetryLoggingEnrichment()`

#### 2. Application Insights (In-Memory)
- Add `Microsoft.ApplicationInsights.AspNetCore` package
- Configure with `InMemoryChannel` (no Azure connection needed)
- Wire up the HVO bridge (`AddHvoAppInsightsTelemetry()`) 
- Add a diagnostic endpoint or background logger that dumps tracked telemetry items to the console
- **Shows**: How HVO telemetry flows into App Insights items (requests, dependencies, exceptions, traces)

#### 3. Datadog (Console Exporter)
- Wire up `AddHvoDatadogTelemetry()` with a **console/text exporter** mode
- When no Datadog agent is reachable, fall back to logging spans and metrics to the console in Datadog-compatible format
- **Shows**: Trace data structure, tag propagation, metric names

#### 4. EF Core + SQLite
- Add `Microsoft.EntityFrameworkCore.Sqlite` package
- Create a `WeatherDbContext` with a `WeatherReadings` table
- Seed data from the weather collector to the DB
- Add controller endpoints: `GET /api/weather/history` (last N readings), `GET /api/weather/history/{location}`
- Use the HVO EF Core interceptor (`HvoTelemetryDbInterceptor`) to instrument all queries
- **Shows**: Query timing, slow query detection, command text capture (opt-in)

#### 5. ADO.NET + SQLite
- Use `Microsoft.Data.Sqlite` for raw ADO.NET operations alongside or instead of EF Core
- Example operations: bulk insert readings, aggregate queries
- Instrument with `HvoTelemetryDbCommandWrapper` or ADO.NET extensions
- **Shows**: How to instrument raw SQL without an ORM

#### 6. Redis (Simulated)
- Create a `FakeRedisCache : IDistributedCache` that uses `ConcurrentDictionary` internally
- Wrap it with HVO Redis instrumentation so telemetry fires even without a real Redis server
- Use it for caching the "latest weather" data to avoid repeated API calls
- If a real Redis is available (via Docker), upgrade seamlessly
- **Shows**: Cache get/set telemetry, TTL tracking, miss vs. hit metrics

#### 7. RabbitMQ (Simulated with Channels)
- Create a `FakeMessageBus` using `System.Threading.Channels<T>` 
- Implement publish/subscribe patterns: weather collector publishes observations, an alert processor consumes them
- Wrap with HVO RabbitMQ instrumentation for telemetry
- If a real RabbitMQ is available (via Docker), upgrade seamlessly
- **Shows**: Message publish/consume telemetry, correlation propagation across message boundaries

### Telemetry Output Visualization

A key goal is making telemetry **visible**. Add:

1. **Console Telemetry Sink** — A custom `IHostedService` that subscribes to the telemetry pipeline and writes a formatted, human-readable summary of:
   - Operation scopes (name, duration, status, tags)
   - Exceptions tracked (type, message, fingerprint)
   - Metrics recorded (name, value)
   - Correlation context (IDs flowing through the pipeline)

2. **Text Log Telemetry Export** — Write telemetry to a structured log file (`telemetry-output.log`) viewable alongside the application logs

3. **Admin Dashboard Endpoint** — Enhance the existing `/api/weather/diagnostics` endpoint with extension-specific stats:
   - DB queries executed, avg duration, slow query count
   - Cache hit/miss ratio
   - Messages published/consumed
   - Serilog events enriched
   - App Insights items queued

### WebApplicationFactory Integration Tests

Create integration tests using `WebApplicationFactory<Program>`:
- Verify correlation ID round-trip through HTTP headers
- Verify health check responses include all registered checks
- Verify weather endpoints return data
- Verify telemetry statistics increment after requests
- Verify error-demo endpoint tracks exceptions
- Verify Serilog enrichment produces correlation fields
- Verify EF Core operations create telemetry spans

### Docker Compose (Optional Enhancement)

Provide a `docker-compose.yml` that optionally spins up:
- **SQLite**: Not needed (in-process)
- **Redis**: `redis:7-alpine` on port 6379
- **RabbitMQ**: `rabbitmq:3-management-alpine` on ports 5672/15672
- **Jaeger**: `jaegertracing/all-in-one:latest` on port 16686 (for future OTel)
- **Prometheus**: `prom/prometheus:latest` on port 9090 (for future OTel)

The sample must work **without** Docker Compose (using in-process fakes). Docker Compose only upgrades fakes to real infrastructure.

## Acceptance Criteria

### Extension Integrations
- [ ] Serilog enrichment is active and console output shows `CorrelationId`, `TraceId`
- [ ] App Insights in-memory channel captures telemetry items; diagnostic endpoint shows counts
- [ ] Datadog console exporter logs trace spans and metric submissions
- [ ] EF Core + SQLite stores and retrieves weather history with instrumented queries
- [ ] ADO.NET + SQLite raw queries are instrumented with timing and telemetry
- [ ] Redis simulation caches weather data; telemetry shows cache hit/miss
- [ ] RabbitMQ simulation publishes/consumes weather observations with correlation

### Telemetry Visibility
- [ ] Console telemetry sink shows formatted operation scopes, metrics, exceptions
- [ ] Diagnostics endpoint includes extension-specific statistics
- [ ] All telemetry output includes correlation IDs for traceability

### Integration Tests
- [ ] WebApplicationFactory tests verify correlation round-trip
- [ ] Tests verify health check responses
- [ ] Tests verify telemetry statistics increment after API calls
- [ ] Tests verify EF Core instrumentation creates spans
- [ ] All integration tests pass in CI without external services

### Configuration
- [ ] Each extension can be toggled on/off via `appsettings.json`
- [ ] `README.md` documents each integration with setup and configuration
- [ ] Docker Compose file provided for optional real infrastructure

### Build Quality
- [ ] Solution builds with 0 warnings, 0 errors
- [ ] Existing tests pass (no regressions)
- [ ] New integration tests pass
- [ ] Sample starts and runs with `dotnet run` (no external setup)

## Technical Requirements

### New Packages (Sample Project)

```xml
<!-- Serilog -->
<PackageReference Include="Serilog.AspNetCore" Version="8.0.0" />
<PackageReference Include="Serilog.Sinks.Console" Version="5.0.1" />

<!-- Application Insights -->
<PackageReference Include="Microsoft.ApplicationInsights.AspNetCore" Version="2.22.0" />

<!-- EF Core + SQLite -->
<PackageReference Include="Microsoft.EntityFrameworkCore.Sqlite" Version="8.0.0" />
<PackageReference Include="Microsoft.EntityFrameworkCore.Design" Version="8.0.0" />

<!-- Raw SQLite for ADO.NET -->
<PackageReference Include="Microsoft.Data.Sqlite" Version="8.0.0" />

<!-- Extension package project references -->
<ProjectReference Include="../../src/HVO.Enterprise.Telemetry.Serilog/..." />
<ProjectReference Include="../../src/HVO.Enterprise.Telemetry.AppInsights/..." />
<ProjectReference Include="../../src/HVO.Enterprise.Telemetry.Datadog/..." />
<ProjectReference Include="../../src/HVO.Enterprise.Telemetry.Data.EfCore/..." />
<ProjectReference Include="../../src/HVO.Enterprise.Telemetry.Data.AdoNet/..." />
<ProjectReference Include="../../src/HVO.Enterprise.Telemetry.Data.Redis/..." />
<ProjectReference Include="../../src/HVO.Enterprise.Telemetry.Data.RabbitMQ/..." />
```

### New Files (Estimated)

```
samples/HVO.Enterprise.Samples.Net8/
├── Data/
│   ├── WeatherDbContext.cs              ← EF Core context + entity
│   ├── WeatherReadingEntity.cs          ← DB entity for weather history
│   ├── WeatherRepository.cs             ← EF Core repository with instrumented queries
│   └── WeatherAdoNetRepository.cs       ← Raw ADO.NET queries with instrumentation
├── Caching/
│   ├── FakeRedisCache.cs                ← In-process IDistributedCache with telemetry
│   └── WeatherCacheService.cs           ← Cache-aside pattern for weather data
├── Messaging/
│   ├── FakeMessageBus.cs                ← Channel-based pub/sub with telemetry
│   ├── WeatherObservationPublisher.cs   ← Publishes observations to message bus
│   └── AlertProcessorSubscriber.cs      ← Consumes observations, evaluates alerts
├── Telemetry/
│   ├── ConsoleTelemetrySink.cs          ← Human-readable telemetry console output
│   └── TelemetryTextExporter.cs         ← Writes telemetry to text log file
├── Controllers/
│   └── WeatherHistoryController.cs      ← New endpoints for DB-backed history
├── Tests/                                ← Or in tests/ project directory
│   └── Integration/
│       ├── WeatherApiTests.cs           ← WebApplicationFactory tests
│       ├── CorrelationTests.cs          ← Correlation round-trip tests
│       └── TelemetryIntegrationTests.cs ← Extension verification tests
├── docker-compose.yml                   ← Optional real infrastructure
└── (updated) Configuration/ServiceConfiguration.cs
```

### Configuration Structure (`appsettings.json`)

```jsonc
{
  "Extensions": {
    "Serilog": {
      "Enabled": true,
      "OutputTemplate": "[{Timestamp:HH:mm:ss} {Level:u3}] {CorrelationId} | {Message:lj}{NewLine}{Exception}"
    },
    "ApplicationInsights": {
      "Enabled": false,
      "UseInMemoryChannel": true,
      "ConnectionString": ""
    },
    "Datadog": {
      "Enabled": false,
      "UseConsoleExporter": true,
      "AgentHost": "localhost",
      "AgentPort": 8126
    },
    "Database": {
      "Enabled": true,
      "Provider": "Sqlite",
      "ConnectionString": "Data Source=weather.db",
      "CaptureCommandText": true,
      "SlowQueryThresholdMs": 100
    },
    "Redis": {
      "Enabled": true,
      "UseFakeCache": true,
      "ConnectionString": "localhost:6379"
    },
    "RabbitMQ": {
      "Enabled": true,
      "UseFakeMessageBus": true,
      "HostName": "localhost",
      "Port": 5672
    }
  },
  "Telemetry": {
    "ConsoleSink": {
      "Enabled": true,
      "IncludeScopes": true,
      "IncludeMetrics": true,
      "IncludeExceptions": true
    },
    "TextExporter": {
      "Enabled": false,
      "FilePath": "telemetry-output.log"
    }
  }
}
```

## Dependencies

**Blocked By**:
- US-028: .NET 8 Sample Application (✅ Complete — PR #72)
- US-022: Database Extension Package (✅ Complete)
- US-023: Serilog Extension Package (✅ Complete)
- US-024: Application Insights Extension (✅ Complete)
- US-025: Datadog Extension Package (✅ Complete)

**Blocks**:
- US-029: Project Documentation (references this sample for extension usage examples)

## Definition of Done

- [ ] All extension integrations active and producing telemetry
- [ ] Console telemetry sink shows human-readable output
- [ ] Diagnostics endpoint shows extension-specific statistics
- [ ] EF Core + SQLite stores and queries weather history
- [ ] Integration tests pass without external dependencies
- [ ] Docker Compose file provided for optional real infrastructure
- [ ] Each extension toggleable via configuration
- [ ] README updated with all integrations documented
- [ ] Solution builds with 0 warnings, 0 errors
- [ ] All existing tests pass (no regressions)
- [ ] Code reviewed and approved

## Notes

### Design Decisions

1. **Why SQLite?** — In-process, zero setup, real SQL with real EF Core migrations. Demonstrates a genuine database workflow without Docker.

2. **Why fake Redis/RabbitMQ?** — The telemetry library instruments at the abstraction level (`IDistributedCache`, message bus interfaces). Fakes exercise the same instrumentation path while requiring zero infrastructure.

3. **Why console telemetry output?** — The biggest frustration when evaluating a telemetry library is not being able to *see* what it's doing. A dedicated console sink makes telemetry visible without requiring Jaeger or Zipkin.

4. **Why WebApplicationFactory tests?** — The sample app is complex enough to warrant proper integration tests. `WebApplicationFactory` verifies the full middleware pipeline, DI registration, and telemetry flow end-to-end.

5. **Why toggleable extensions?** — Different teams will adopt different subsets of extensions. The configuration pattern demonstrates how to conditionally register services.

### Implementation Tips

- Use feature flags (`Extensions:Serilog:Enabled`) and `ConditionalServiceRegistration` patterns
- For the EF Core integration, use `EnsureCreated()` on startup (not migrations) for simplicity
- The fake message bus should simulate realistic latency (10-50ms) to exercise timeout/cancellation paths
- Integration tests should use `builder.ConfigureServices()` to override fakes when needed
- Consider creating a `/api/admin/extensions` endpoint that lists which extensions are active

### Risks

- Extension packages may have API surface that doesn't match the commented-out code in US-028 (verify before integrating)
- SQLite has some EF Core limitations (no concurrent writes in WAL mode with in-memory)
- App Insights `InMemoryChannel` may have quirks with SDK version mismatches

## Related Documentation

- [US-028: .NET 8 Sample Application](US-028-net8-sample.md)
- [US-022: Database Extension Package](US-022-database-extension.md)
- [US-023: Serilog Extension Package](US-023-serilog-extension.md)
- [US-024: Application Insights Extension](US-024-appinsights-extension.md)
- [US-025: Datadog Extension Package](US-025-datadog-extension.md)
- [US-027: .NET Framework 4.8 Sample](US-027-net48-sample.md) *(for WCF/IIS reference)*
