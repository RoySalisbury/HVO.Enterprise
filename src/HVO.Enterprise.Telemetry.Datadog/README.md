# HVO.Enterprise.Telemetry.Datadog

Exports traces and metrics from [HVO.Enterprise.Telemetry](../../README.md) to Datadog. Supports dual-mode export via OTLP or DogStatsD.

## Installation

```shell
dotnet add package HVO.Enterprise.Telemetry.Datadog
```

### Dependencies

| Package | Version |
|---------|---------|
| DogStatsD-CSharp-Client | 7.0.0 |
| HVO.Enterprise.Telemetry | latest |

## Quick Start

```csharp
using HVO.Enterprise.Telemetry.Datadog;

// Explicit configuration
services.AddDatadogTelemetry(options =>
{
    options.ServiceName = "my-service";
    options.Environment = "production";
    options.AgentHost  = "localhost";
});

// Or auto-configure from DD_* environment variables
services.AddDatadogTelemetryFromEnvironment();
```

## Key Types

| Type | Description |
|------|-------------|
| `DatadogTraceExporter` | Exports distributed traces to the Datadog Agent |
| `DatadogMetricsExporter` | Exports metrics via DogStatsD or OTLP |
| `DatadogOptions` | Configuration options for Datadog export |
| `DatadogExportMode` | Export mode enum: `Auto`, `OTLP`, `DogStatsD` |

## Configuration Options

| Option | Type | Default | Description |
|--------|------|---------|-------------|
| `ServiceName` | `string` | — | Datadog service name |
| `Environment` | `string` | — | Deployment environment (e.g., `production`) |
| `Version` | `string` | — | Application version |
| `AgentHost` | `string` | `localhost` | Datadog Agent host |
| `AgentPort` | `int` | `8125` | DogStatsD port |
| `UseUnixDomainSocket` | `bool` | `false` | Use a Unix domain socket for the Agent connection |
| `Mode` | `DatadogExportMode` | `Auto` | Export mode: `Auto`, `OTLP`, or `DogStatsD` |
| `MetricPrefix` | `string` | — | Optional prefix for all metric names |
| `GlobalTags` | `IDictionary<string,string>` | — | Tags applied to every metric and trace |

## Environment Variables

The `AddDatadogTelemetryFromEnvironment()` method reads standard Datadog variables:

| Variable | Maps To |
|----------|---------|
| `DD_SERVICE` | `ServiceName` |
| `DD_ENV` | `Environment` |
| `DD_VERSION` | `Version` |

## Export Modes

| Mode | Description |
|------|-------------|
| **Auto** | Detects the best available mode at runtime |
| **OTLP** | Sends telemetry via OpenTelemetry OTLP to the Datadog Agent |
| **DogStatsD** | Sends metrics over the DogStatsD UDP/UDS protocol |

## Further Reading

- [HVO.Enterprise.Telemetry Documentation](../../docs/)
- [Main README](../../README.md)
