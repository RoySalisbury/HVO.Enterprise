# HVO.Enterprise.Telemetry.OpenTelemetry

OpenTelemetry OTLP exporter integration for **HVO.Enterprise.Telemetry** — exports traces, metrics, and logs to any OTLP-compatible backend (Jaeger, Zipkin, Grafana Tempo, Honeycomb, Dynatrace, New Relic, Splunk, Elastic, Prometheus).

## Quick Start

```csharp
// Option 1: Direct service collection registration
services.AddTelemetry();
services.AddOpenTelemetryExport(options =>
{
    options.ServiceName = "my-service";
    options.Endpoint = "http://otel-collector:4317";
    options.EnableTraceExport = true;
    options.EnableMetricsExport = true;
});

// Option 2: Fluent builder API
services.AddTelemetry(builder => builder
    .WithOpenTelemetry(options =>
    {
        options.Endpoint = "http://otel-collector:4317";
        options.ServiceName = "my-service";
    })
    .WithPrometheusEndpoint("/metrics")
    .WithOtlpLogExport());
```

## Configuration

| Property | Default | Env Var |
|---|---|---|
| `Endpoint` | `http://localhost:4317` | `OTEL_EXPORTER_OTLP_ENDPOINT` |
| `Transport` | `Grpc` | — |
| `ServiceName` | `null` | `OTEL_SERVICE_NAME` |
| `Environment` | `null` | `OTEL_RESOURCE_ATTRIBUTES` (`deployment.environment`) |
| `EnableTraceExport` | `true` | — |
| `EnableMetricsExport` | `true` | — |
| `EnableLogExport` | `false` | — |
| `EnablePrometheusEndpoint` | `false` | — |
| `Headers` | `{}` | `OTEL_EXPORTER_OTLP_HEADERS` |

## Transport Options

- **gRPC** (default, port 4317) — most efficient transport
- **HTTP/protobuf** (port 4318) — works through HTTP proxies and load balancers

## Dependencies

- `OpenTelemetry.Extensions.Hosting` 1.9.0
- `OpenTelemetry.Exporter.OpenTelemetryProtocol` 1.9.0
- `HVO.Enterprise.Telemetry` (core library)
