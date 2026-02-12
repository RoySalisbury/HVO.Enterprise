# HVO.Enterprise.Telemetry

Core telemetry library providing unified observability (distributed tracing, metrics, structured logging) across all .NET platforms.

## Features

- **Distributed Tracing** — OpenTelemetry-compatible Activity-based tracing
- **Metrics** — Counter, histogram, and gauge instrumentation
- **Structured Logging** — ILogger integration with correlation context
- **Correlation** — Automatic request/operation correlation across service boundaries
- **Health Checks** — Built-in telemetry health monitoring
- **Configuration** — Hierarchical, hot-reloadable telemetry settings

## Installation

```
dotnet add package HVO.Enterprise.Telemetry
```

## Quick Start

```csharp
using HVO.Enterprise.Telemetry;

// Register telemetry services
services.AddTelemetry(options =>
{
    options.ServiceName = "MyService";
    options.DefaultSamplingRate = 0.1;
});

// Track operations
using var operation = telemetry.TrackOperation("ProcessOrder");
operation.AddProperty("orderId", orderId);
```

## Target Framework

- .NET Standard 2.0 (compatible with .NET Framework 4.8+ and .NET Core 2.0+)

## License

MIT — see [LICENSE](https://github.com/RoySalisbury/HVO.Enterprise/blob/main/LICENSE) for details.
