# HVO.Enterprise.Telemetry.IIS

IIS hosting integration for [HVO.Enterprise.Telemetry](../../README.md). Provides graceful telemetry lifecycle management under IIS by detecting the hosting environment at runtime and coordinating flush/shutdown during application pool recycling.

## Installation

```shell
dotnet add package HVO.Enterprise.Telemetry.IIS
```

### Dependencies

No external dependencies — uses `System.Web` hosting environment detection.

| Package | Version |
|---------|---------|
| HVO.Enterprise.Telemetry | latest |

## Quick Start

```csharp
using HVO.Enterprise.Telemetry.IIS;

services.AddIisTelemetryIntegration(options =>
{
    options.ShutdownTimeout = TimeSpan.FromSeconds(25);
    options.OnPreShutdown  = () => Log.Information("IIS shutting down…");
});
```

The integration automatically detects whether the application is hosted under IIS. When running outside IIS (e.g., Kestrel, console), it remains dormant with zero overhead.

## Key Types

| Type | Description |
|------|-------------|
| `IisLifecycleManager` | Coordinates telemetry initialization and shutdown with the IIS lifecycle |
| `IisShutdownHandler` | Registered with the IIS hosting environment to receive shutdown notifications |
| `IisExtensionOptions` | Configuration options for the IIS integration |

## Configuration Options

| Option | Type | Default | Description |
|--------|------|---------|-------------|
| `ShutdownTimeout` | `TimeSpan` | `25s` | Maximum time to wait for telemetry flush on shutdown |
| `AutoInitialize` | `bool` | `true` | Automatically initialize the lifecycle manager on startup |
| `RegisterWithHostingEnvironment` | `bool` | `true` | Register the shutdown handler with `HostingEnvironment` |
| `OnPreShutdown` | `Action` | — | Callback invoked before telemetry shutdown begins |
| `OnPostShutdown` | `Action` | — | Callback invoked after telemetry shutdown completes |

## How It Works

1. **Detection** — On startup, `IisLifecycleManager` checks whether the process is hosted in IIS.
2. **Registration** — If IIS is detected, `IisShutdownHandler` registers with `HostingEnvironment.RegisterObject()`.
3. **Shutdown** — When IIS signals an app-pool recycle, the handler flushes all pending telemetry within the configured `ShutdownTimeout` before allowing the process to exit.

## Further Reading

- [HVO.Enterprise.Telemetry Documentation](../../docs/)
- [Main README](../../README.md)
