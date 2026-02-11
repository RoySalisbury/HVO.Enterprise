# HVO.Enterprise.Telemetry.AppInsights

Bridges [HVO.Enterprise.Telemetry](../../README.md) to Azure Application Insights. Supports dual-mode export via OTLP (OpenTelemetry) or the direct Application Insights SDK.

## Installation

```shell
dotnet add package HVO.Enterprise.Telemetry.AppInsights
```

### Dependencies

| Package | Version |
|---------|---------|
| Microsoft.ApplicationInsights | 2.22.0 |
| HVO.Enterprise.Telemetry | latest |

## Quick Start

```csharp
using HVO.Enterprise.Telemetry.AppInsights;

services.AddAppInsightsTelemetry(options =>
{
    options.ConnectionString = "InstrumentationKey=...;IngestionEndpoint=...";
});
```

The bridge automatically forwards HVO telemetry (traces, metrics, and logs) to Application Insights while preserving correlation context.

## Key Types

| Type | Description |
|------|-------------|
| `ApplicationInsightsBridge` | Core bridge that forwards telemetry data to Application Insights |
| `AppInsightsOptions` | Configuration options for the bridge |
| `CorrelationTelemetryInitializer` | Injects HVO correlation IDs into App Insights telemetry |
| `ActivityTelemetryInitializer` | Syncs `System.Diagnostics.Activity` context with App Insights operations |

## Configuration Options

| Option | Type | Default | Description |
|--------|------|---------|-------------|
| `ConnectionString` | `string` | — | Application Insights connection string (preferred) |
| `InstrumentationKey` | `string` | — | Legacy instrumentation key |
| `EnableBridge` | `bool` | `true` | Enable or disable the telemetry bridge |
| `EnableActivityInitializer` | `bool` | `true` | Register the `ActivityTelemetryInitializer` |
| `EnableCorrelationInitializer` | `bool` | `true` | Register the `CorrelationTelemetryInitializer` |
| `ForceOtlpMode` | `bool` | `false` | Force OTLP export mode instead of direct SDK |

## Export Modes

| Mode | Description |
|------|-------------|
| **Direct SDK** | Uses the Application Insights SDK for telemetry export (default) |
| **OTLP** | Routes telemetry through OpenTelemetry's OTLP exporter to Application Insights |

Set `ForceOtlpMode = true` to use OTLP when both modes are available.

## Further Reading

- [HVO.Enterprise.Telemetry Documentation](../../docs/)
- [Main README](../../README.md)
