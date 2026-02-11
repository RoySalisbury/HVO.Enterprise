# HVO.Enterprise.Telemetry.Data

Shared base package for all [HVO.Enterprise.Telemetry](../../README.md) database extensions. Provides common utilities for parameter sanitization, database system detection, SQL operation parsing, and OpenTelemetry semantic convention tags.

## Installation

```shell
dotnet add package HVO.Enterprise.Telemetry.Data
```

### Dependencies

| Package | Version |
|---------|---------|
| HVO.Enterprise.Telemetry | latest |

> **Note:** This is the base package. For specific database support, install the appropriate sub-package: `Data.EfCore`, `Data.AdoNet`, `Data.Redis`, or `Data.RabbitMQ`.

## Quick Start

```csharp
using HVO.Enterprise.Telemetry.Data;

services.AddDataTelemetryBase(options =>
{
    options.RecordStatements = true;
    options.MaxStatementLength = 2000;
    options.RecordParameters = false; // enable with caution - may capture PII
});
```

## Key Types

| Type | Description |
|------|-------------|
| `ParameterSanitizer` | Sanitizes SQL parameters to prevent PII leakage in telemetry |
| `DatabaseSystemDetector` | Detects database system type from connection strings |
| `SqlOperationDetector` | Parses SQL statements to extract operation type (SELECT, INSERT, etc.) |
| `DataActivityTags` | Constants for OpenTelemetry database semantic conventions |

## Configuration Options (`DataExtensionOptions`)

| Option | Type | Default | Description |
|--------|------|---------|-------------|
| `RecordStatements` | `bool` | `true` | Capture SQL statements on spans |
| `MaxStatementLength` | `int` | `2000` | Truncate statements longer than this value |
| `RecordParameters` | `bool` | `false` | Capture query parameters (**caution: may contain PII**) |
| `MaxParameters` | `int` | `10` | Maximum number of parameters to record per statement |
| `OperationFilter` | `Func<string, bool>?` | `null` | Filter which database operations are instrumented |

## Sub-Packages

| Package | Description |
|---------|-------------|
| `HVO.Enterprise.Telemetry.Data.EfCore` | Entity Framework Core instrumentation |
| `HVO.Enterprise.Telemetry.Data.AdoNet` | Raw ADO.NET instrumentation |
| `HVO.Enterprise.Telemetry.Data.Redis` | StackExchange.Redis instrumentation |
| `HVO.Enterprise.Telemetry.Data.RabbitMQ` | RabbitMQ publish/consume instrumentation |

## Further Reading

- [HVO.Enterprise.Telemetry Documentation](../../docs/)
- [Main README](../../README.md)
