# HVO.Enterprise

A modular .NET telemetry and logging library providing unified observability across all .NET platforms.

## Overview

HVO.Enterprise standardizes logging, distributed tracing, and performance telemetry across diverse .NET platforms including legacy WCF services, ASP.NET applications, and modern ASP.NET Core APIs.

### Key Features

- **Cross-Platform Support**: Single binary (.NET Standard 2.0) works on .NET Framework 4.8 through .NET 10+
- **Unified Observability**: Distributed tracing (ActivitySource), metrics (runtime-adaptive), and structured logging
- **Functional Patterns**: `Result<T>`, `Option<T>`, and discriminated unions for robust error handling
- **Platform Integrations**: Extensions for IIS, WCF, Serilog, Application Insights, Datadog, and databases
- **Auto-Correlation**: Automatic correlation ID management across async/await boundaries
- **Performance-Focused**: <100ns overhead per operation, non-blocking background processing
- **Production-Ready**: Configuration hot reload, health checks, exception aggregation, lifecycle management

## Project Structure

```
HVO.Enterprise/
├── src/
│   ├── HVO.Common/                       # Shared utilities (Result<T>, Option<T>, IOneOf)
│   ├── HVO.Enterprise.Telemetry/        # Core telemetry library
│   ├── HVO.Enterprise.Telemetry.IIS/    # IIS hosting integration
│   ├── HVO.Enterprise.Telemetry.Wcf/    # WCF instrumentation
│   ├── HVO.Enterprise.Telemetry.Database/    # Database instrumentation
│   ├── HVO.Enterprise.Telemetry.Serilog/     # Serilog enrichers
│   ├── HVO.Enterprise.Telemetry.AppInsights/ # Application Insights
│   └── HVO.Enterprise.Telemetry.Datadog/     # Datadog integration
├── tests/                               # Unit and integration tests
├── docs/
│   └── project-plan.md                  # Detailed implementation plan
└── .github/
    └── copilot-instructions.md          # Development guidelines
```

## Core Packages

### HVO.Common

Shared utilities and functional programming patterns used across all HVO projects (not limited to Enterprise telemetry):

- **Result&lt;T&gt;**: Functional error handling without exceptions
- **Result&lt;T, TEnum&gt;**: Typed error codes with enum-based errors
- **Option&lt;T&gt;**: Type-safe optional values
- **IOneOf**: Discriminated union interface for type-safe variants
- **Extension Methods**: Common utilities (EnumExtensions, etc.)

**Target**: .NET Standard 2.0 (compatible with all .NET implementations)

### HVO.Enterprise.Telemetry

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

## Documentation

- [Project Plan](docs/project-plan.md) - Detailed implementation plan and architecture decisions
- [Copilot Instructions](.github/copilot-instructions.md) - Development guidelines and coding standards

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

# Run tests
dotnet test
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