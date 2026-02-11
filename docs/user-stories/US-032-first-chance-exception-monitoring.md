# US-032: First Chance Exception Monitoring

**Status**: 🚧 In Progress  
**GitHub Issue**: [#74](https://github.com/RoySalisbury/HVO.Enterprise/issues/74)  
**Category**: Core Package  
**Effort**: 5 story points  
**Sprint**: 8

## Description

As a **developer diagnosing suppressed or swallowed exceptions in production**,  
I want **opt-in first-chance exception monitoring that logs and tracks exceptions as they are thrown (before any catch handler runs)**,  
So that **I can detect hidden failures, diagnose intermittent issues, and enable runtime diagnostics without redeploying the application**.

## Background

.NET's `AppDomain.CurrentDomain.FirstChanceException` event fires for **every** exception the instant it is thrown, even if a `catch` block ultimately handles it. This is invaluable for discovering:

- Exceptions silently swallowed inside library code or third-party packages
- Intermittent failures masked by retry logic
- Performance-degrading exception storms (e.g., thousands of `SocketException` per second)
- Logic errors hidden behind empty `catch {}` blocks

Because the event fires on **every** exception (including harmless ones like `OperationCanceledException` during shutdown), the feature must be **opt-in** and highly **configurable** so it can be enabled selectively at runtime without generating excessive noise.

## Acceptance Criteria

1. **Core Monitor Service**
   - [ ] `FirstChanceExceptionMonitor` subscribes to `AppDomain.CurrentDomain.FirstChanceException`
   - [ ] Implements `IHostedService` for clean startup/shutdown lifecycle
   - [ ] Implements `IDisposable` to unsubscribe from the event
   - [ ] Thread-safe and re-entrant safe (the handler itself may trigger exceptions)

2. **Configurable Filtering**
   - [ ] `Enabled` master toggle (default: `false` — opt-in only)
   - [ ] `IncludeExceptionTypes` list — only monitor these types when non-empty (whitelist)
   - [ ] `ExcludeExceptionTypes` list — never monitor these types (blacklist, default includes `OperationCanceledException`, `TaskCanceledException`)
   - [ ] `IncludeNamespacePatterns` list — only monitor exceptions from these source namespaces
   - [ ] `ExcludeNamespacePatterns` list — ignore exceptions originating from these namespaces
   - [ ] `MinimumLogLevel` — the `LogLevel` at which first-chance exceptions are logged (default: `Warning`)
   - [ ] `MaxEventsPerSecond` — rate-limiting to prevent log flooding (default: `100`)
   - [ ] Filtering evaluates type lists first, then namespace patterns, then rate limit

3. **Runtime Configuration Changes**
   - [ ] Uses `IOptionsMonitor<FirstChanceExceptionOptions>` for hot-reload support
   - [ ] Changes to `Enabled`, filter lists, or rate limits take effect immediately without restart
   - [ ] Configurable from `appsettings.json` section `Telemetry:FirstChanceExceptions`

4. **Telemetry Integration**
   - [ ] Logs each accepted exception via `ILogger<FirstChanceExceptionMonitor>` at the configured level
   - [ ] Integrates with existing `ExceptionAggregator` for fingerprinting and grouping
   - [ ] Records `firstchance.exceptions.total` metric counter
   - [ ] Records `firstchance.exceptions.suppressed` counter for rate-limited/filtered exceptions

5. **DI Registration (follows existing patterns)**
   - [ ] `services.AddFirstChanceExceptionMonitoring(Action<FirstChanceExceptionOptions>?)` on `IServiceCollection`
   - [ ] `builder.WithFirstChanceExceptionMonitoring(Action<FirstChanceExceptionOptions>?)` on `TelemetryBuilder`
   - [ ] Idempotent registration (skip if already added)

6. **Safety Guarantees**
   - [ ] Re-entrance guard: if the handler itself throws, it must not recurse
   - [ ] Never throws from the event handler (all errors silently suppressed)
   - [ ] Minimal allocation in the hot path
   - [ ] Does not hold references to exception objects beyond the handler scope

## Technical Requirements

### Options Class

```csharp
public sealed class FirstChanceExceptionOptions
{
    public bool Enabled { get; set; } = false;
    public LogLevel MinimumLogLevel { get; set; } = LogLevel.Warning;
    public int MaxEventsPerSecond { get; set; } = 100;
    public List<string> IncludeExceptionTypes { get; set; } = new();
    public List<string> ExcludeExceptionTypes { get; set; } = new()
    {
        "System.OperationCanceledException",
        "System.Threading.Tasks.TaskCanceledException"
    };
    public List<string> IncludeNamespacePatterns { get; set; } = new();
    public List<string> ExcludeNamespacePatterns { get; set; } = new();
}
```

### Registration

```csharp
// Option A: Direct service collection
services.AddFirstChanceExceptionMonitoring(options =>
{
    options.Enabled = true;
    options.MaxEventsPerSecond = 50;
    options.IncludeExceptionTypes.Add("System.InvalidOperationException");
});

// Option B: Builder pattern
services.AddTelemetry(builder => builder
    .WithFirstChanceExceptionMonitoring(options =>
    {
        options.Enabled = true;
    }));

// Option C: appsettings.json (hot-reloadable)
// {
//   "Telemetry": {
//     "FirstChanceExceptions": {
//       "Enabled": true,
//       "MinimumLogLevel": "Warning",
//       "MaxEventsPerSecond": 100,
//       "ExcludeExceptionTypes": [
//         "System.OperationCanceledException",
//         "System.Threading.Tasks.TaskCanceledException"
//       ]
//     }
//   }
// }
```

## Testing Requirements

### Unit Tests

1. **Options validation and defaults**
2. **Filter logic**: include/exclude type lists, namespace patterns
3. **Rate limiting**: verify MaxEventsPerSecond throttling
4. **Re-entrance guard**: handler does not recurse
5. **Enable/disable toggle**: exceptions not logged when disabled
6. **Hot-reload**: options changes detected at runtime
7. **Integration with ExceptionAggregator**
8. **Metric counter increments**
9. **Lifecycle**: proper subscribe/unsubscribe on start/stop

### Integration Tests (Sample App)

1. **Suppressed exception detected**: throw and catch an exception, verify telemetry log
2. **Filtered exception ignored**: throw excluded type, verify no log
3. **Rate limiting active**: throw many exceptions rapidly, verify capped output
4. **Configuration hot-reload**: change appsettings, verify new behavior

## Dependencies

**Blocked By**:
- US-001 (Core Package Setup)
- US-007 (Exception Tracking)

**Blocks**: None (standalone diagnostic feature)

## Definition of Done

- [ ] `FirstChanceExceptionOptions` class implemented
- [ ] `FirstChanceExceptionMonitor` implemented with IHostedService
- [ ] DI extension methods on IServiceCollection and TelemetryBuilder
- [ ] Runtime hot-reload via IOptionsMonitor
- [ ] All unit tests passing (>90% coverage)
- [ ] Sample app updated with opt-in configuration
- [ ] appsettings.json updated with commented-out section
- [ ] XML documentation on all public APIs
- [ ] Zero warnings in build

## Notes

### Design Decisions

1. **Opt-in by default**: `Enabled = false` because first-chance exceptions are extremely noisy in most apps. This must be deliberately enabled.
2. **Rate limiting**: Without rate limiting, an exception storm could fill disk or overwhelm a logging sink.
3. **IOptionsMonitor for hot-reload**: The primary value of this feature is enabling it at runtime when diagnosing an issue, then disabling it again.
4. **Lives in core package**: Uses `AppDomain.CurrentDomain.FirstChanceException` which is available everywhere, no external dependencies needed.
5. **Re-entrance guard via `[ThreadStatic]`**: The FirstChanceException handler fires for ALL exceptions, including any thrown inside the handler itself.

### Common Pitfalls

- The event fires on **every** exception, including ones from framework internals (System.IO, System.Net, etc.)
- String formatting inside the handler can itself throw and cause recursion
- Holding references to exception objects can prevent GC and cause memory pressure
- On high-throughput services, even the filtering logic must be fast (<1μs)
