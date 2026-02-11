# HVO.Enterprise.Telemetry.Data.RabbitMQ

RabbitMQ message publish and consume instrumentation for [HVO.Enterprise.Telemetry](../../README.md) with automatic W3C TraceContext propagation via message headers. Provides distributed tracing across message-driven service boundaries.

## Installation

```shell
dotnet add package HVO.Enterprise.Telemetry.Data.RabbitMQ
```

### Dependencies

| Package | Version |
|---------|---------|
| RabbitMQ.Client | 6.8.1 |
| HVO.Enterprise.Telemetry.Data | latest |

## Quick Start

### Via Dependency Injection

```csharp
using HVO.Enterprise.Telemetry.Data.RabbitMQ;

services.AddRabbitMqTelemetry();
```

### Via Extension Method

```csharp
using HVO.Enterprise.Telemetry.Data.RabbitMQ;

using var channel = connection.CreateModel().WithTelemetry();

// Publish — trace context is automatically injected into message headers
channel.BasicPublish("exchange", "routing.key", null, body);

// Consume — trace context is automatically extracted from message headers
var consumer = new EventingBasicConsumer(channel);
consumer.Received += (sender, args) => { /* traced automatically */ };
channel.BasicConsume("queue", true, consumer);
```

### With Options

```csharp
services.AddRabbitMqTelemetry(options =>
{
    options.PropagateTraceContext = true;
    options.RecordExchange = true;
    options.RecordRoutingKey = true;
    options.RecordBodySize = true;
    options.RecordMessageIds = true;
    options.RecordQueueName = true;
});
```

## Key Types

| Type | Description |
|------|-------------|
| `TelemetryModel` | Wraps `IModel` to create spans for publish/consume operations |
| `RabbitMqHeaderPropagator` | Injects/extracts W3C TraceContext via RabbitMQ message headers |

## Configuration Options (`RabbitMqTelemetryOptions`)

Inherits all options from [`DataExtensionOptions`](../HVO.Enterprise.Telemetry.Data/README.md#configuration-options-dataextensionoptions), plus:

| Option | Type | Default | Description |
|--------|------|---------|-------------|
| `PropagateTraceContext` | `bool` | `true` | Inject/extract W3C TraceContext in message headers |
| `RecordExchange` | `bool` | `true` | Capture exchange name on spans |
| `RecordRoutingKey` | `bool` | `true` | Capture routing key on spans |
| `RecordBodySize` | `bool` | `true` | Record message body size in bytes on spans |
| `RecordMessageIds` | `bool` | `true` | Capture message and correlation IDs on spans |
| `RecordQueueName` | `bool` | `true` | Capture queue name on consume spans |

## Further Reading

- [Data Base Package](../HVO.Enterprise.Telemetry.Data/README.md)
- [HVO.Enterprise.Telemetry Documentation](../../docs/)
- [Main README](../../README.md)
