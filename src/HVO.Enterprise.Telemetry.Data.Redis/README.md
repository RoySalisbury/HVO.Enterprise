# HVO.Enterprise.Telemetry.Data.Redis

StackExchange.Redis command instrumentation for [HVO.Enterprise.Telemetry](../../README.md) via the Redis profiling API. Automatically traces Redis commands with key, endpoint, and database index capture.

## Installation

```shell
dotnet add package HVO.Enterprise.Telemetry.Data.Redis
```

### Dependencies

| Package | Version |
|---------|---------|
| StackExchange.Redis | 2.6.122 |
| HVO.Enterprise.Telemetry.Data | latest |

## Quick Start

### Via Dependency Injection

```csharp
using HVO.Enterprise.Telemetry.Data.Redis;

services.AddRedisTelemetry();
```

### Via Extension Method

```csharp
using HVO.Enterprise.Telemetry.Data.Redis;

var multiplexer = await ConnectionMultiplexer.ConnectAsync("localhost");
multiplexer.WithHvoTelemetry();

var db = multiplexer.GetDatabase();
await db.StringSetAsync("key", "value");
```

### With Options

```csharp
services.AddRedisTelemetry(options =>
{
    options.RecordKeys = true;
    options.MaxKeyLength = 100;
    options.RecordCommands = true;
    options.RecordDatabaseIndex = true;
    options.RecordEndpoint = true;
});
```

## Key Types

| Type | Description |
|------|-------------|
| `RedisTelemetryProfiler` | Implements `IProfiler` to attach telemetry spans to Redis commands |
| `RedisCommandProcessor` | Processes profiled Redis commands into telemetry spans |

## Configuration Options (`RedisTelemetryOptions`)

Inherits all options from [`DataExtensionOptions`](../HVO.Enterprise.Telemetry.Data/README.md#configuration-options-dataextensionoptions), plus:

| Option | Type | Default | Description |
|--------|------|---------|-------------|
| `RecordKeys` | `bool` | `true` | Capture Redis key names on spans |
| `MaxKeyLength` | `int` | `100` | Truncate key names longer than this value |
| `RecordCommands` | `bool` | `true` | Capture Redis command names (GET, SET, etc.) |
| `RecordDatabaseIndex` | `bool` | `true` | Include the Redis database index on spans |
| `RecordEndpoint` | `bool` | `true` | Include the Redis server endpoint on spans |

## Further Reading

- [Data Base Package](../HVO.Enterprise.Telemetry.Data/README.md)
- [HVO.Enterprise.Telemetry Documentation](../../docs/)
- [Main README](../../README.md)
