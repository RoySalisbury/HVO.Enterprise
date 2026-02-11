# HVO.Enterprise.Telemetry.Data.AdoNet

Raw ADO.NET instrumentation for [HVO.Enterprise.Telemetry](../../README.md) via the wrapper pattern. Wraps `DbConnection` and `DbCommand` to automatically trace database operations without code changes to existing query logic.

## Installation

```shell
dotnet add package HVO.Enterprise.Telemetry.Data.AdoNet
```

### Dependencies

| Package | Version |
|---------|---------|
| HVO.Enterprise.Telemetry.Data | latest |

No additional external dependencies required.

## Quick Start

### Via Dependency Injection

```csharp
using HVO.Enterprise.Telemetry.Data.AdoNet;

services.AddAdoNetTelemetry();
```

### Via Extension Method

```csharp
using HVO.Enterprise.Telemetry.Data.AdoNet;

using var connection = new SqlConnection(connectionString).WithTelemetry();
using var command = connection.CreateCommand();
command.CommandText = "SELECT * FROM Orders WHERE Id = @id";
command.Parameters.AddWithValue("@id", orderId);
using var reader = await command.ExecuteReaderAsync();
```

### With Options

```csharp
services.AddAdoNetTelemetry(options =>
{
    options.RecordStatements = true;
    options.RecordParameters = false;
    options.RecordConnectionInfo = false;
    options.MaxStatementLength = 2000;
});
```

## Key Types

| Type | Description |
|------|-------------|
| `InstrumentedDbConnection` | Wraps `DbConnection` to create spans for connection lifecycle |
| `InstrumentedDbCommand` | Wraps `DbCommand` to create spans for query execution |

## Configuration Options

Inherits all options from [`DataExtensionOptions`](../HVO.Enterprise.Telemetry.Data/README.md#configuration-options-dataextensionoptions), plus:

| Option | Type | Default | Description |
|--------|------|---------|-------------|
| `RecordConnectionInfo` | `bool` | `false` | Include server/database name on spans |

## Further Reading

- [Data Base Package](../HVO.Enterprise.Telemetry.Data/README.md)
- [HVO.Enterprise.Telemetry Documentation](../../docs/)
- [Main README](../../README.md)
