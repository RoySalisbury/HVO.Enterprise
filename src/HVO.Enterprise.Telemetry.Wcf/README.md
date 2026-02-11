# HVO.Enterprise.Telemetry.Wcf

WCF service and client instrumentation for [HVO.Enterprise.Telemetry](../../README.md) with automatic W3C TraceContext propagation via SOAP headers. Provides distributed tracing across WCF service boundaries.

## Installation

```shell
dotnet add package HVO.Enterprise.Telemetry.Wcf
```

### Dependencies

| Package | Version |
|---------|---------|
| System.ServiceModel.Primitives | 4.10.3 |
| HVO.Enterprise.Telemetry | latest |

## Quick Start

### Server-Side

```csharp
using HVO.Enterprise.Telemetry.Wcf;

[WcfTelemetryBehavior]
public class OrderService : IOrderService
{
    public Order GetOrder(int id) => _repository.Find(id);
}
```

### Client-Side

```csharp
using HVO.Enterprise.Telemetry.Wcf;

var client = new OrderServiceClient();
client.AddTelemetryBehavior(); // extension on ClientBase<T>
var order = await client.GetOrderAsync(42);
```

### Dependency Injection

```csharp
services.AddWcfTelemetryInstrumentation(options =>
{
    options.PropagateTraceContextInReply = true;
    options.CaptureFaultDetails = true;
});
```

## Key Types

| Type | Description |
|------|-------------|
| `WcfTelemetryBehaviorAttribute` | Service behavior attribute for server-side instrumentation |
| `WcfDispatchInspectorProxy` | Server-side message inspector that extracts/injects trace context |
| `TelemetryClientEndpointBehavior` | Client-side endpoint behavior for outgoing calls |
| `TelemetryClientMessageInspector` | Client-side message inspector that propagates trace context |
| `W3CTraceContextPropagator` | Propagates W3C TraceContext via SOAP headers |

## Configuration Options

| Option | Type | Default | Description |
|--------|------|---------|-------------|
| `PropagateTraceContextInReply` | `bool` | `true` | Include trace context headers in service replies |
| `OperationFilter` | `Func<string, bool>?` | `null` | Filter which WCF operations are instrumented |
| `CaptureFaultDetails` | `bool` | `true` | Record fault details on spans when service faults occur |

## Further Reading

- [HVO.Enterprise.Telemetry Documentation](../../docs/)
- [Main README](../../README.md)
