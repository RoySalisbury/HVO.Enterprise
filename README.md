# HVO.Enterprise

[![Build Status](https://github.com/RoySalisbury/HVO.Enterprise/workflows/CI/badge.svg)](https://github.com/RoySalisbury/HVO.Enterprise/actions)
[![.NET Standard](https://img.shields.io/badge/.NET%20Standard-2.0-blue)](https://docs.microsoft.com/en-us/dotnet/standard/net-standard)
[![License](https://img.shields.io/badge/license-MIT-green)](LICENSE)

> Unified telemetry and observability for all .NET platforms — single binary from .NET Framework 4.8 to .NET 10+

## Features

- **🔄 Automatic Correlation** — AsyncLocal-based correlation across async boundaries
- **📊 Adaptive Metrics** — Meter API (.NET 6+) with EventCounters fallback (.NET Framework)
- **📈 Distributed Tracing** — W3C TraceContext with OpenTelemetry integration
- **⚡ High Performance** — <100ns overhead, lock-free queues, zero-allocation hot paths
- **🔌 Extensible** — Platform-specific extensions (IIS, WCF, Serilog, App Insights, Datadog, Database)
- **📦 Single Binary** — .NET Standard 2.0 for universal deployment
- **🛡️ Functional Patterns** — `Result<T>`, `Option<T>`, discriminated unions for robust error handling
- **🏥 Production-Ready** — Health checks, exception aggregation, configuration hot reload, lifecycle management

## Project Structure

```
HVO.Enterprise/
├── src/
│   ├── HVO.Common/                            # Shared utilities (Result<T>, Option<T>, OneOf)
│   ├── HVO.Enterprise.Telemetry/              # Core telemetry library
│   ├── HVO.Enterprise.Telemetry.IIS/          # IIS hosting integration
│   ├── HVO.Enterprise.Telemetry.Wcf/          # WCF instrumentation
│   ├── HVO.Enterprise.Telemetry.Serilog/      # Serilog enrichers
│   ├── HVO.Enterprise.Telemetry.AppInsights/  # Application Insights bridge
│   ├── HVO.Enterprise.Telemetry.Datadog/      # Datadog integration
│   ├── HVO.Enterprise.Telemetry.Data/         # Database instrumentation (shared)
│   ├── HVO.Enterprise.Telemetry.Data.EfCore/  # Entity Framework Core
│   ├── HVO.Enterprise.Telemetry.Data.AdoNet/  # Raw ADO.NET
│   ├── HVO.Enterprise.Telemetry.Data.Redis/   # StackExchange.Redis
│   └── HVO.Enterprise.Telemetry.Data.RabbitMQ/# RabbitMQ messaging
├── tests/                                     # Unit and integration tests
├── samples/
│   └── HVO.Enterprise.Samples.Net8/           # Weather monitoring API sample
├── benchmarks/                                # Performance benchmarks
└── docs/                                      # Documentation
```

## Packages

### [HVO.Common](src/HVO.Common/)

Shared utilities and functional programming patterns used across all HVO projects:

- **Result&lt;T&gt;** / **Result&lt;T, TEnum&gt;**: Functional error handling without exceptions
- **Option&lt;T&gt;**: Type-safe optional values
- **OneOf&lt;T1, T2, ...&gt;**: Discriminated unions for type-safe variants
- **Extensions**: String, collection, and enum utilities
- **Guard / Ensure**: Input validation and runtime assertions

**Target**: .NET Standard 2.0 (compatible with all .NET implementations)

### [HVO.Enterprise.Telemetry](src/HVO.Enterprise.Telemetry/)

Core telemetry library providing:

- ActivitySource-based distributed tracing
- Runtime-adaptive metrics (Meter API on .NET 6+, EventCounters on .NET Framework)
- Automatic correlation ID management with AsyncLocal
- Background job correlation utilities
- Automatic ILogger enrichment
- User and request context capture
- DispatchProxy-based automatic instrumentation
- Exception tracking and aggregation
- Configuration hot reload
- Health checks
- Performance monitoring

**Target**: .NET Standard 2.0

### Extension Packages

| Package | Description |
|---------|-------------|
| [`Telemetry.Serilog`](src/HVO.Enterprise.Telemetry.Serilog/) | Serilog enrichers (CorrelationId, TraceId, SpanId) |
| [`Telemetry.AppInsights`](src/HVO.Enterprise.Telemetry.AppInsights/) | Azure Application Insights bridge (OTLP / Direct) |
| [`Telemetry.Datadog`](src/HVO.Enterprise.Telemetry.Datadog/) | Datadog trace and metrics export (OTLP / DogStatsD) |
| [`Telemetry.IIS`](src/HVO.Enterprise.Telemetry.IIS/) | IIS hosting lifecycle management |
| [`Telemetry.Wcf`](src/HVO.Enterprise.Telemetry.Wcf/) | WCF instrumentation with W3C TraceContext |
| [`Telemetry.Data`](src/HVO.Enterprise.Telemetry.Data/) | Shared database instrumentation base |
| [`Telemetry.Data.EfCore`](src/HVO.Enterprise.Telemetry.Data.EfCore/) | Entity Framework Core interceptor |
| [`Telemetry.Data.AdoNet`](src/HVO.Enterprise.Telemetry.Data.AdoNet/) | Raw ADO.NET wrapper instrumentation |
| [`Telemetry.Data.Redis`](src/HVO.Enterprise.Telemetry.Data.Redis/) | StackExchange.Redis profiling |
| [`Telemetry.Data.RabbitMQ`](src/HVO.Enterprise.Telemetry.Data.RabbitMQ/) | RabbitMQ message instrumentation |

## Quick Start

### Installation

```bash
# Core library
dotnet add package HVO.Common
dotnet add package HVO.Enterprise.Telemetry

# Platform-specific extensions (as needed)
dotnet add package HVO.Enterprise.Telemetry.Serilog
dotnet add package HVO.Enterprise.Telemetry.AppInsights
dotnet add package HVO.Enterprise.Telemetry.Datadog
```

### .NET 8+ Example

```csharp
// Program.cs
using HVO.Enterprise.Telemetry;

var builder = WebApplication.CreateBuilder(args);

// Add telemetry with DI
builder.Services.AddTelemetry(options =>
{
    options.DefaultSamplingRate = 0.1;
    options.DefaultDetailLevel = DetailLevel.Normal;
})
.WithActivitySources("MyApp.*")
.WithLoggingEnrichment()
.WithDatadogExporter();

builder.Services.AddHealthChecks()
    .AddCheck<TelemetryHealthCheck>("telemetry");

var app = builder.Build();
app.MapHealthChecks("/health");
app.Run();
```

### .NET Framework 4.8 Example

```csharp
// Global.asax.cs
using HVO.Enterprise.Telemetry;

public class Global : HttpApplication
{
    protected void Application_Start(object sender, EventArgs e)
    {
        Telemetry.Initialize(config => config
            .WithActivitySources("MyApp.*")
            .WithSampling(samplingRate: 0.1)
            .WithLoggingEnrichment()
            .WithDatadogExporter()
            .ForIIS());
    }
    
    protected void Application_End(object sender, EventArgs e)
    {
        Telemetry.Shutdown(timeout: TimeSpan.FromSeconds(10));
    }
}
```

### Using Result&lt;T&gt; for Error Handling

```csharp
using HVO.Common.Results;

public Result<Customer> GetCustomer(int id)
{
    try
    {
        var customer = _repository.Find(id);
        if (customer == null)
            return Result<Customer>.Failure(
                new NotFoundException($"Customer {id} not found"));
        
        return Result<Customer>.Success(customer);
    }
    catch (Exception ex)
    {
        return ex; // Implicit conversion
    }
}

// Usage
var result = GetCustomer(customerId);
if (result.IsSuccessful)
{
    var customer = result.Value;
}
else
{
    _logger.LogError(result.Error, "Failed to get customer");
}
```

## Framework Compatibility

| Framework | Support Level | Notes |
|-----------|--------------|-------|
| .NET 10 | ✅ Full | All modern features available |
| .NET 8 | ✅ Full | All modern features available |
| .NET 6+ | ✅ Full | Meter API for metrics |
| .NET 5 | ✅ Compatible | Via .NET Standard 2.0 |
| .NET Core 2.0+ | ✅ Compatible | Via .NET Standard 2.0 |
| .NET Framework 4.8.1 | ✅ Compatible | EventCounters for metrics |
| .NET Framework 4.6.1-4.8 | ✅ Compatible | Via .NET Standard 2.0 |

## Performance Characteristics

- **Activity start**: ~5-30ns (depending on sampling)
- **Property addition**: <10ns (fast-path primitives)
- **Operation Dispose**: ~1-5μs (synchronous timing)
- **Background processing**: Non-blocking with bounded queue
- **Target overhead**: <100ns per operation (excluding Dispose)

## Project Planning & User Stories

This project follows a structured user story approach with 30 stories covering all features.

### Quick Links

- **[Quick Start: Creating GitHub Issues](QUICK-START-ISSUES.md)** - 5-minute guide to create all GitHub issues
- **[Validation Summary](VALIDATION-SUMMARY.md)** - Complete status report of all user stories
- **[User Stories Index](docs/user-stories/README.md)** - All 30 user stories (US-001 to US-030)
- **[Scripts Documentation](scripts/README.md)** - Automation tools for issue creation

### Project Status (30 User Stories, 180 Story Points)

| Category | Stories | SP | Status |
|----------|---------|-----|--------|
| ✅ Completed | 5 | 26 | 14% |
| 🚧 In Progress | 0 | 0 | 0% |
| ❌ Not Started | 25 | 154 | 86% |

**Completed Stories**:
- US-001: Core Package Setup (3 SP)
- US-002: Auto-Managed Correlation (5 SP)
- US-003: Background Job Correlation (5 SP)
- US-004: Bounded Queue Worker (8 SP)
- US-019: HVO.Common Library (5 SP)

### Creating GitHub Issues

All user story markdown files have been created. To convert them to GitHub issues:

```bash
# 1. Authenticate
gh auth login

# 2. Generate issue creation script
./scripts/generate-issue-commands.sh > create-all-issues.sh

# 3. Execute
chmod +x create-all-issues.sh && ./create-all-issues.sh
```

See [QUICK-START-ISSUES.md](QUICK-START-ISSUES.md) for detailed instructions.

## Documentation

| Document | Description |
|----------|-------------|
| [Architecture](docs/ARCHITECTURE.md) | System design, component diagrams, threading model |
| [Platform Differences](docs/DIFFERENCES.md) | .NET Framework 4.8 vs .NET 8+ comparison matrix |
| [Migration Guide](docs/MIGRATION.md) | Migrating from other telemetry libraries |
| [Roadmap](docs/ROADMAP.md) | Feature status, planned timeline, version compatibility |
| [Project Plan](docs/project-plan.md) | Detailed implementation plan and decisions |
| [Benchmarks](docs/benchmarks/benchmark-report-2026-02-08.md) | Performance benchmark results |
| [Sample App](samples/HVO.Enterprise.Samples.Net8/) | Weather monitoring API with full telemetry |
| [User Stories](docs/user-stories/README.md) | All 30 user stories with acceptance criteria |

## Development

### Prerequisites

- .NET SDK 8.0 or later
- VS Code with C# Dev Kit (recommended)
- Dev Container support (optional but recommended)

### Building

```bash
# Build entire solution
dotnet build

# Build specific project
cd src/HVO.Enterprise.Common
dotnet build

# Run tests (manual only - do not use IDE test runner)
dotnet test tests/HVO.Common.Tests/HVO.Common.Tests.csproj
dotnet test tests/HVO.Enterprise.Telemetry.Tests/HVO.Enterprise.Telemetry.Tests.csproj
```

### Design Principles

1. **Single Binary Deployment**: .NET Standard 2.0 for maximum compatibility
2. **Runtime Adaptation**: Feature detection for platform-specific capabilities
3. **Performance First**: Non-blocking, minimal allocations, <100ns overhead
4. **Functional Patterns**: Result&lt;T&gt;, Option&lt;T&gt; for robust error handling
5. **Explicit Over Implicit**: No magic, clear intent, explicit usings
6. **Zero Warnings**: All projects build with zero warnings
7. **Test Coverage**: >85% coverage on business logic

## Contributing

1. Follow coding standards in [.github/copilot-instructions.md](.github/copilot-instructions.md)
2. Use conventional commits: `type(scope): description`
3. Ensure all tests pass and build has zero warnings
4. Add XML documentation for all public APIs
5. Update relevant documentation

## License

[Add your license here]

## Status

🚧 **In Development** - Core packages and infrastructure in progress