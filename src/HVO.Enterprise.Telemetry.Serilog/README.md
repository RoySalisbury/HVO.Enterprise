# HVO.Enterprise.Telemetry.Serilog

Serilog enrichers for [HVO.Enterprise.Telemetry](../../README.md) correlation context. Automatically enriches Serilog log events with distributed tracing identifiers such as `CorrelationId`, `TraceId`, `SpanId`, and `ParentId`.

## Installation

```shell
dotnet add package HVO.Enterprise.Telemetry.Serilog
```

### Dependencies

| Package | Version |
|---------|---------|
| Serilog | 3.1.1   |
| HVO.Enterprise.Telemetry | latest |

## Quick Start

```csharp
using Serilog;
using HVO.Enterprise.Telemetry.Serilog;

Log.Logger = new LoggerConfiguration()
    .Enrich.WithTelemetry()   // adds both correlation and activity enrichers
    .WriteTo.Console(outputTemplate:
        "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} " +
        "{CorrelationId} {TraceId}{NewLine}{Exception}")
    .CreateLogger();
```

### Individual Enrichers

```csharp
Log.Logger = new LoggerConfiguration()
    .Enrich.WithCorrelation()  // adds CorrelationId only
    .Enrich.WithActivity()     // adds TraceId, SpanId, ParentId
    .CreateLogger();
```

## Key Types

| Type | Description |
|------|-------------|
| `CorrelationEnricher` | Enriches log events with the current `CorrelationId` |
| `ActivityEnricher` | Enriches log events with `TraceId`, `SpanId`, and `ParentId` from the current `Activity` |

## Extension Methods

| Method | Description |
|--------|-------------|
| `.Enrich.WithTelemetry()` | Adds both `CorrelationEnricher` and `ActivityEnricher` |
| `.Enrich.WithCorrelation()` | Adds `CorrelationEnricher` only |
| `.Enrich.WithActivity()` | Adds `ActivityEnricher` only |

## Enriched Properties

| Property | Source |
|----------|--------|
| `CorrelationId` | HVO correlation context |
| `TraceId` | `System.Diagnostics.Activity.Current.TraceId` |
| `SpanId` | `System.Diagnostics.Activity.Current.SpanId` |
| `ParentId` | `System.Diagnostics.Activity.Current.ParentId` |

## Further Reading

- [HVO.Enterprise.Telemetry Documentation](../../docs/)
- [Main README](../../README.md)
