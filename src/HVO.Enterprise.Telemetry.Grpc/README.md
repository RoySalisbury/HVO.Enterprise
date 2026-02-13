# HVO.Enterprise.Telemetry.Grpc

gRPC interceptor integration for [HVO.Enterprise.Telemetry](https://www.nuget.org/packages/HVO.Enterprise.Telemetry) — automatic distributed tracing for gRPC services and clients with W3C TraceContext propagation, correlation flow, and OpenTelemetry `rpc.*` semantic conventions.

## Features

- **Server Interceptor** — Automatic `Activity` creation for incoming gRPC calls (unary, client-streaming, server-streaming, duplex)
- **Client Interceptor** — Automatic `Activity` creation for outgoing gRPC calls with trace context injection
- **W3C TraceContext Propagation** — Extracts/injects `traceparent`/`tracestate` via gRPC metadata headers
- **Correlation Flow** — Propagates `x-correlation-id` through gRPC metadata alongside W3C trace context
- **OpenTelemetry Semantic Conventions** — Tags activities with `rpc.system`, `rpc.service`, `rpc.method`, `rpc.grpc.status_code`, `server.address`, `server.port`
- **Health Check Suppression** — Optionally suppresses instrumentation for health checks and server reflection calls
- **Error Tracking** — Records gRPC status codes and exceptions on activities

## Installation

```bash
dotnet add package HVO.Enterprise.Telemetry.Grpc
```

## Quick Start

### Register with Dependency Injection

```csharp
// Option 1: Standalone registration
services.AddGrpcTelemetry(options =>
{
    options.SuppressHealthChecks = true;
});

// Option 2: Via TelemetryBuilder fluent API
services.AddTelemetry(builder =>
{
    builder.WithGrpcInstrumentation(options =>
    {
        options.SuppressHealthChecks = true;
    });
});
```

### ASP.NET Core gRPC Server

```csharp
// Register the server interceptor with ASP.NET Core gRPC
builder.Services.AddGrpc(options =>
{
    options.Interceptors.Add<TelemetryServerInterceptor>();
});

// Register telemetry
builder.Services.AddGrpcTelemetry();
```

### gRPC Client (via GrpcClientFactory)

```csharp
builder.Services.AddGrpcClient<MyService.MyServiceClient>(options =>
{
    options.Address = new Uri("https://localhost:5001");
})
.AddInterceptor<TelemetryClientInterceptor>();
```

### Manual Client Usage

```csharp
var options = new GrpcTelemetryOptions();
var interceptor = new TelemetryClientInterceptor(options);

var channel = GrpcChannel.ForAddress("https://localhost:5001");
var invoker = channel.Intercept(interceptor);
var client = new MyService.MyServiceClient(invoker);
```

## Configuration

| Option | Default | Description |
|---|---|---|
| `EnableServerInterceptor` | `true` | Enable/disable server-side instrumentation |
| `EnableClientInterceptor` | `true` | Enable/disable client-side instrumentation |
| `CorrelationHeaderName` | `"x-correlation-id"` | gRPC metadata key for correlation ID |
| `SuppressHealthChecks` | `true` | Suppress `grpc.health.v1.Health` instrumentation |
| `SuppressReflection` | `true` | Suppress `grpc.reflection` instrumentation |

## Activity Tags (OpenTelemetry Semantic Conventions)

| Tag | Example | Description |
|---|---|---|
| `rpc.system` | `"grpc"` | Always `"grpc"` |
| `rpc.service` | `"mypackage.OrderService"` | gRPC service name |
| `rpc.method` | `"GetOrder"` | gRPC method name |
| `rpc.grpc.status_code` | `0` | Numeric gRPC status code |
| `server.address` | `"api.example.com"` | Server hostname (client side) |
| `server.port` | `443` | Server port (client side) |

## Related Packages

| Package | Description |
|---|---|
| [HVO.Enterprise.Telemetry](https://www.nuget.org/packages/HVO.Enterprise.Telemetry) | Core telemetry library (required) |
| [HVO.Enterprise.Telemetry.Wcf](https://www.nuget.org/packages/HVO.Enterprise.Telemetry.Wcf) | WCF message inspector instrumentation |
| [HVO.Enterprise.Telemetry.Serilog](https://www.nuget.org/packages/HVO.Enterprise.Telemetry.Serilog) | Serilog enricher integration |
| [HVO.Enterprise.Telemetry.AppInsights](https://www.nuget.org/packages/HVO.Enterprise.Telemetry.AppInsights) | Application Insights exporter |
