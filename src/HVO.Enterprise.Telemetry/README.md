# HVO.Enterprise.Telemetry

Core telemetry library providing unified observability — distributed tracing, metrics, and structured logging — across all .NET platforms (.NET Framework 4.8 through .NET 10+).

Built on **.NET Standard 2.0** for single-binary universal deployment.

## Installation

```shell
dotnet add package HVO.Enterprise.Telemetry
```

## Quick Start

### Dependency Injection (recommended)

```csharp
using HVO.Enterprise.Telemetry;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddTelemetry(options =>
{
    options.ServiceName = "MyService";
    options.EnableDistributedTracing = true;
    options.EnableMetrics = true;
});

var app = builder.Build();
app.Run();
```

### Static Initialization (non-DI / legacy)

```csharp
using HVO.Enterprise.Telemetry;

Telemetry.Initialize();

// Create an operation scope
using (var scope = Telemetry.CreateScope("ProcessOrder"))
{
    scope.SetTag("orderId", orderId);
    // ... business logic
}

// Shutdown cleanly
Telemetry.Shutdown();
```

## Key Features

| Feature | Description |
|---|---|
| **Distributed Tracing** | `ActivitySource`-based tracing with automatic context propagation |
| **Runtime-Adaptive Metrics** | `IMetricRecorder` abstraction; adapts to available runtime APIs |
| **Correlation Management** | `CorrelationContext` uses `AsyncLocal<T>` for ambient correlation IDs |
| **Operation Scoping** | `IOperationScopeFactory` / `OperationScopeFactory` for structured operation tracking |
| **Exception Aggregation** | `ExceptionAggregator` groups exceptions by fingerprint to reduce noise |
| **Health Checks** | `TelemetryHealthCheck` implements `IHealthCheck` for liveness/readiness probes |
| **Configuration Hot Reload** | `ConfigurationProvider` with a 4-level hierarchy (defaults → file → env → runtime) |
| **Background Processing** | `BackgroundJobContext` captures and restores correlation across threads/jobs |
| **Lifecycle Management** | `TelemetryHostedService` handles startup/shutdown in hosted environments |
| **Sampling** | `ISampler` with Probabilistic, Adaptive, PerSource, and Conditional strategies |

## Main Public APIs

### Entry Points

- **`Telemetry`** — Static class for non-DI scenarios. Call `Initialize()` / `Shutdown()`.
- **`TelemetryServiceCollectionExtensions.AddTelemetry()`** — Registers all services with DI. Idempotent.
- **`TelemetryBuilder`** — Fluent configuration builder for advanced setup.

### Core Services

- **`IOperationScopeFactory`** / **`OperationScopeFactory`** — Create structured operation scopes for tracing.
- **`CorrelationContext`** — Manage ambient correlation IDs via `AsyncLocal<T>`.
- **`BackgroundJobContext`** — Capture/restore correlation state for background jobs.
- **`IMetricRecorder`** — Record counters, histograms, and gauges.

### Infrastructure

- **`ExceptionAggregator`** — Fingerprint-based exception grouping and rate limiting.
- **`TelemetryHealthCheck`** — `IHealthCheck` implementation reporting telemetry subsystem status.
- **`TelemetryHostedService`** — `IHostedService` for automatic lifecycle management.
- **`ISampler`** — Sampling strategy interface (Probabilistic, Adaptive, PerSource, Conditional).
- **`ConfigurationProvider`** — 4-level configuration hierarchy with hot-reload support.

## Dependencies

| Package | Version |
|---|---|
| OpenTelemetry.Api | 1.9.0 |
| System.Diagnostics.DiagnosticSource | 8.0.1 |
| Microsoft.Extensions.Logging.Abstractions | 8.0.0 |
| Microsoft.Extensions.DependencyInjection.Abstractions | 8.0.0 |
| Microsoft.Extensions.Configuration.Abstractions | 8.0.0 |
| Microsoft.Extensions.Configuration.Binder | 8.0.0 |
| Microsoft.Extensions.Options | 8.0.0 |
| Microsoft.Extensions.Hosting.Abstractions | 8.0.0 |
| Microsoft.Extensions.Diagnostics.HealthChecks.Abstractions | 8.0.0 |
| System.Threading.Channels | 7.0.0 |

## Performance Targets

| Metric | Target |
|---|---|
| Operation scope creation | < 1 μs |
| Metric recording (single point) | < 500 ns |
| Correlation context read | < 100 ns |
| Memory per active scope | < 1 KB |
| Background channel throughput | > 100K items/sec |

## Documentation

For detailed documentation, see the [`docs/`](../../docs/) folder:

- [Architecture](../../docs/ARCHITECTURE.md) — System design, component interactions, and data flow
- [Migration Guide](../../docs/MIGRATION.md) — Upgrading from previous versions
- [Roadmap](../../docs/ROADMAP.md) — Planned features and timeline
- [Platform Differences](../../docs/DIFFERENCES.md) — Behavior across .NET runtimes

## License

See the repository [LICENSE](../../LICENSE) for details.
