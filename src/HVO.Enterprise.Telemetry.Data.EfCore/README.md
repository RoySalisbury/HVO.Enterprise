# HVO.Enterprise.Telemetry.Data.EfCore

Entity Framework Core instrumentation for [HVO.Enterprise.Telemetry](../../README.md) via `DbCommandInterceptor`. Automatically traces EF Core database commands with full SQL statement capture and OpenTelemetry semantic conventions.

## Installation

```shell
dotnet add package HVO.Enterprise.Telemetry.Data.EfCore
```

### Dependencies

| Package | Version |
|---------|---------|
| Microsoft.EntityFrameworkCore.Relational | 3.1.0+ |
| HVO.Enterprise.Telemetry.Data | latest |

## Quick Start

### Via Dependency Injection

```csharp
using HVO.Enterprise.Telemetry.Data.EfCore;

services.AddEfCoreTelemetry();

services.AddDbContext<AppDbContext>(options =>
{
    options.UseSqlServer(connectionString);
});
```

### Via DbContext Configuration

```csharp
protected override void OnConfiguring(DbContextOptionsBuilder options)
{
    options.UseSqlServer(connectionString)
           .AddHvoTelemetry();
}
```

### With Options

```csharp
services.AddEfCoreTelemetry(options =>
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
| `TelemetryDbCommandInterceptor` | EF Core `DbCommandInterceptor` that creates spans for database commands |
| `EfCoreTelemetryOptions` | Configuration options extending `DataExtensionOptions` |

## Configuration Options (`EfCoreTelemetryOptions`)

Inherits all options from [`DataExtensionOptions`](../HVO.Enterprise.Telemetry.Data/README.md#configuration-options-dataextensionoptions), plus:

| Option | Type | Default | Description |
|--------|------|---------|-------------|
| `RecordConnectionInfo` | `bool` | `false` | Include server/database name on spans |

## Further Reading

- [Data Base Package](../HVO.Enterprise.Telemetry.Data/README.md)
- [HVO.Enterprise.Telemetry Documentation](../../docs/)
- [Main README](../../README.md)
