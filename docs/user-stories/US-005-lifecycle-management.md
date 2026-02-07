# US-005: Lifecycle Management

**Status**: ❌ Not Started  
**Category**: Core Package  
**Effort**: 5 story points  
**Sprint**: 1

## Description

As a **library developer**,  
I want **automatic lifecycle management with AppDomain hooks and graceful shutdown**,  
So that **telemetry data is properly flushed before application termination and resources are cleaned up correctly**.

## Acceptance Criteria

1. **AppDomain Event Hooks**
   - [ ] Subscribe to `AppDomain.DomainUnload` event
   - [ ] Subscribe to `AppDomain.ProcessExit` event
   - [ ] Subscribe to `AppDomain.UnhandledException` event
   - [ ] Automatic registration on library initialization

2. **IIS Detection and Integration**
   - [ ] Detect IIS hosting environment
   - [ ] Register with `HostingEnvironment.RegisterObject()` if available
   - [ ] Implement `IRegisteredObject` for ASP.NET shutdown notification
   - [ ] Handle ASP.NET application shutdown gracefully

3. **Graceful Shutdown**
   - [ ] `FlushAsync(TimeSpan timeout)` drains telemetry queue
   - [ ] Automatic flush on domain unload with configurable timeout
   - [ ] Export pending spans and metrics
   - [ ] Close open Activities
   - [ ] Dispose resources properly

4. **IHostApplicationLifetime Integration**
   - [ ] Subscribe to `ApplicationStopping` token (.NET Core/5+)
   - [ ] Subscribe to `ApplicationStopped` token
   - [ ] Graceful shutdown before host terminates

5. **Public API**
   - [ ] `Telemetry.ShutdownAsync(TimeSpan timeout)` for manual control
   - [ ] `ITelemetryLifetime` interface for custom lifetime management
   - [ ] Shutdown progress reporting

## Technical Requirements

### Core Implementation

```csharp
namespace HVO.Enterprise.Telemetry.Lifecycle
{
    /// <summary>
    /// Manages telemetry library lifecycle and graceful shutdown.
    /// </summary>
    internal sealed class TelemetryLifetimeManager : IDisposable
    {
        private readonly TelemetryBackgroundWorker _worker;
        private readonly ILogger<TelemetryLifetimeManager>? _logger;
        private volatile bool _isShuttingDown;
        private volatile bool _disposed;
        
        // Default shutdown timeout
        private static readonly TimeSpan DefaultShutdownTimeout = TimeSpan.FromSeconds(5);
        
        public TelemetryLifetimeManager(
            TelemetryBackgroundWorker worker,
            ILogger<TelemetryLifetimeManager>? logger = null)
        {
            if (worker == null)
                throw new ArgumentNullException(nameof(worker));
            
            _worker = worker;
            _logger = logger;
            
            RegisterLifecycleHooks();
        }
        
        /// <summary>
        /// Gets whether shutdown is in progress.
        /// </summary>
        public bool IsShuttingDown => _isShuttingDown;
        
        private void RegisterLifecycleHooks()
        {
            // Register AppDomain events
            AppDomain.CurrentDomain.DomainUnload += OnDomainUnload;
            AppDomain.CurrentDomain.ProcessExit += OnProcessExit;
            AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
            
            // Register IIS shutdown (if running in IIS)
            RegisterIISShutdownHook();
            
            _logger?.LogDebug("Telemetry lifecycle hooks registered");
        }
        
        private void RegisterIISShutdownHook()
        {
            try
            {
                // Check if HostingEnvironment is available (ASP.NET)
                var hostingEnvironmentType = Type.GetType(
                    "System.Web.Hosting.HostingEnvironment, System.Web, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a");
                
                if (hostingEnvironmentType != null)
                {
                    var registerMethod = hostingEnvironmentType.GetMethod(
                        "RegisterObject",
                        new[] { typeof(IRegisteredObject) });
                    
                    if (registerMethod != null)
                    {
                        var registeredObject = new TelemetryRegisteredObject(this);
                        registerMethod.Invoke(null, new object[] { registeredObject });
                        
                        _logger?.LogDebug("Registered with IIS HostingEnvironment");
                    }
                }
            }
            catch (Exception ex)
            {
                // Not fatal - we'll still get AppDomain events
                _logger?.LogWarning(ex, "Failed to register with IIS HostingEnvironment");
            }
        }
        
        private void OnDomainUnload(object? sender, EventArgs e)
        {
            _logger?.LogInformation("AppDomain unloading - initiating telemetry shutdown");
            ShutdownInternal(DefaultShutdownTimeout);
        }
        
        private void OnProcessExit(object? sender, EventArgs e)
        {
            _logger?.LogInformation("Process exiting - initiating telemetry shutdown");
            ShutdownInternal(DefaultShutdownTimeout);
        }
        
        private void OnUnhandledException(object? sender, UnhandledExceptionEventArgs e)
        {
            _logger?.LogError("Unhandled exception - flushing telemetry before termination");
            
            // Attempt quick flush on unhandled exception
            ShutdownInternal(TimeSpan.FromSeconds(2));
        }
        
        /// <summary>
        /// Initiates graceful shutdown with timeout.
        /// </summary>
        public async Task<ShutdownResult> ShutdownAsync(
            TimeSpan timeout,
            CancellationToken cancellationToken = default)
        {
            if (_isShuttingDown)
            {
                return new ShutdownResult
                {
                    Success = false,
                    Reason = "Shutdown already in progress"
                };
            }
            
            _isShuttingDown = true;
            
            try
            {
                _logger?.LogInformation("Initiating telemetry shutdown (timeout: {Timeout})", timeout);
                
                var sw = Stopwatch.StartNew();
                
                // Close all open activities
                CloseOpenActivities();
                
                // Flush background queue
                var flushResult = await _worker.FlushAsync(timeout, cancellationToken);
                
                sw.Stop();
                
                _logger?.LogInformation(
                    "Telemetry shutdown complete. Flushed: {Flushed}, Remaining: {Remaining}, Duration: {Duration}ms",
                    flushResult.ItemsFlushed,
                    flushResult.ItemsRemaining,
                    sw.ElapsedMilliseconds);
                
                return new ShutdownResult
                {
                    Success = flushResult.Success,
                    ItemsFlushed = flushResult.ItemsFlushed,
                    ItemsRemaining = flushResult.ItemsRemaining,
                    Duration = sw.Elapsed
                };
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error during telemetry shutdown");
                
                return new ShutdownResult
                {
                    Success = false,
                    Reason = $"Shutdown failed: {ex.Message}"
                };
            }
        }
        
        private void ShutdownInternal(TimeSpan timeout)
        {
            if (_isShuttingDown)
                return;
            
            try
            {
                // Synchronous shutdown for event handlers
                ShutdownAsync(timeout).GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error during synchronous shutdown");
            }
        }
        
        private void CloseOpenActivities()
        {
            // Stop current activity and all parents
            var activity = Activity.Current;
            while (activity != null)
            {
                var parent = activity.Parent;
                activity.Dispose();
                activity = parent;
            }
        }
        
        public void Dispose()
        {
            if (_disposed)
                return;
            
            _disposed = true;
            
            // Unregister event handlers
            AppDomain.CurrentDomain.DomainUnload -= OnDomainUnload;
            AppDomain.CurrentDomain.ProcessExit -= OnProcessExit;
            AppDomain.CurrentDomain.UnhandledException -= OnUnhandledException;
        }
    }
    
    /// <summary>
    /// IIS HostingEnvironment integration.
    /// </summary>
    internal sealed class TelemetryRegisteredObject : IRegisteredObject
    {
        private readonly TelemetryLifetimeManager _lifetimeManager;
        
        public TelemetryRegisteredObject(TelemetryLifetimeManager lifetimeManager)
        {
            _lifetimeManager = lifetimeManager ?? throw new ArgumentNullException(nameof(lifetimeManager));
        }
        
        public void Stop(bool immediate)
        {
            if (!immediate)
            {
                // Graceful shutdown
                _lifetimeManager.ShutdownAsync(TimeSpan.FromSeconds(5)).GetAwaiter().GetResult();
            }
            
            // Unregister from HostingEnvironment
            var hostingEnvironmentType = Type.GetType(
                "System.Web.Hosting.HostingEnvironment, System.Web, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a");
            
            var unregisterMethod = hostingEnvironmentType?.GetMethod(
                "UnregisterObject",
                new[] { typeof(IRegisteredObject) });
            
            unregisterMethod?.Invoke(null, new object[] { this });
        }
    }
    
    /// <summary>
    /// Result of shutdown operation.
    /// </summary>
    public sealed class ShutdownResult
    {
        public bool Success { get; init; }
        public long ItemsFlushed { get; init; }
        public int ItemsRemaining { get; init; }
        public TimeSpan Duration { get; init; }
        public string? Reason { get; init; }
    }
}
```

### IHostApplicationLifetime Integration (.NET Core/5+)

```csharp
namespace HVO.Enterprise.Telemetry.Lifecycle
{
    /// <summary>
    /// Extension methods for IServiceCollection to register lifecycle management.
    /// </summary>
    public static class TelemetryLifetimeExtensions
    {
        public static IServiceCollection AddTelemetryLifetime(this IServiceCollection services)
        {
            services.AddSingleton<IHostedService, TelemetryLifetimeHostedService>();
            return services;
        }
    }
    
    /// <summary>
    /// IHostedService for ASP.NET Core lifecycle integration.
    /// </summary>
    internal sealed class TelemetryLifetimeHostedService : IHostedService
    {
        private readonly IHostApplicationLifetime _appLifetime;
        private readonly TelemetryLifetimeManager _lifetimeManager;
        private readonly ILogger<TelemetryLifetimeHostedService> _logger;
        
        public TelemetryLifetimeHostedService(
            IHostApplicationLifetime appLifetime,
            TelemetryLifetimeManager lifetimeManager,
            ILogger<TelemetryLifetimeHostedService> logger)
        {
            _appLifetime = appLifetime ?? throw new ArgumentNullException(nameof(appLifetime));
            _lifetimeManager = lifetimeManager ?? throw new ArgumentNullException(nameof(lifetimeManager));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }
        
        public Task StartAsync(CancellationToken cancellationToken)
        {
            _appLifetime.ApplicationStopping.Register(OnStopping);
            _appLifetime.ApplicationStopped.Register(OnStopped);
            
            _logger.LogInformation("Telemetry lifetime service started");
            return Task.CompletedTask;
        }
        
        public Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Telemetry lifetime service stopping");
            return Task.CompletedTask;
        }
        
        private void OnStopping()
        {
            _logger.LogInformation("Application stopping - flushing telemetry");
            _lifetimeManager.ShutdownAsync(TimeSpan.FromSeconds(5)).GetAwaiter().GetResult();
        }
        
        private void OnStopped()
        {
            _logger.LogInformation("Application stopped");
        }
    }
}
```

## Testing Requirements

### Unit Tests

1. **AppDomain Event Tests**
   ```csharp
   [Fact]
   public void LifetimeManager_RegistersAppDomainEvents()
   {
       var worker = new TelemetryBackgroundWorker();
       var manager = new TelemetryLifetimeManager(worker);
       
       // Verify events registered (check via reflection or integration test)
       Assert.False(manager.IsShuttingDown);
   }
   
   [Fact]
   public async Task LifetimeManager_ShutdownFlushesQueue()
   {
       var worker = new TelemetryBackgroundWorker();
       var manager = new TelemetryLifetimeManager(worker);
       
       // Enqueue items
       for (int i = 0; i < 100; i++)
       {
           worker.TryEnqueue(new TestWorkItem(() => Thread.Sleep(10)));
       }
       
       var result = await manager.ShutdownAsync(TimeSpan.FromSeconds(10));
       
       Assert.True(result.Success);
       Assert.Equal(100, result.ItemsFlushed);
       Assert.Equal(0, result.ItemsRemaining);
   }
   ```

2. **Shutdown Tests**
   ```csharp
   [Fact]
   public async Task Shutdown_WithTimeout_ReportsRemaining()
   {
       var worker = new TelemetryBackgroundWorker();
       var manager = new TelemetryLifetimeManager(worker);
       
       // Enqueue long-running items
       for (int i = 0; i < 100; i++)
       {
           worker.TryEnqueue(new TestWorkItem(() => Thread.Sleep(1000)));
       }
       
       var result = await manager.ShutdownAsync(TimeSpan.FromMilliseconds(100));
       
       Assert.False(result.Success);
       Assert.True(result.ItemsRemaining > 0);
   }
   
   [Fact]
   public async Task Shutdown_MultipleCallsIgnored()
   {
       var worker = new TelemetryBackgroundWorker();
       var manager = new TelemetryLifetimeManager(worker);
       
       var task1 = manager.ShutdownAsync(TimeSpan.FromSeconds(10));
       var task2 = manager.ShutdownAsync(TimeSpan.FromSeconds(10));
       
       var result1 = await task1;
       var result2 = await task2;
       
       Assert.True(result1.Success);
       Assert.False(result2.Success); // Second call should be ignored
   }
   ```

3. **Activity Cleanup Tests**
   ```csharp
   [Fact]
   public async Task Shutdown_ClosesOpenActivities()
   {
       var worker = new TelemetryBackgroundWorker();
       var manager = new TelemetryLifetimeManager(worker);
       
       var source = new ActivitySource("Test");
       using var activity = source.StartActivity("TestOp");
       
       Assert.NotNull(Activity.Current);
       
       await manager.ShutdownAsync(TimeSpan.FromSeconds(1));
       
       // Activity should be stopped
       Assert.True(activity!.Duration > TimeSpan.Zero);
   }
   ```

### Integration Tests

1. **IIS Integration**
   - [ ] LifetimeManager detects IIS hosting environment
   - [ ] Registers with HostingEnvironment successfully
   - [ ] Receives shutdown notification from IIS
   - [ ] Telemetry flushed before IIS recycle

2. **ASP.NET Core Integration**
   - [ ] IHostedService registers with DI
   - [ ] ApplicationStopping triggers shutdown
   - [ ] Telemetry flushed before application stops

## Performance Requirements

- **Event registration**: <1ms total for all events
- **Shutdown initiation**: <10ms to start shutdown sequence
- **IIS detection**: <5ms to check and register
- **Activity cleanup**: <100μs per open activity

## Dependencies

**Blocked By**: 
- US-001 (Core Package Setup)
- US-004 (Bounded Queue Worker)

**Blocks**: All features that need graceful shutdown

## Definition of Done

- [ ] TelemetryLifetimeManager implemented with AppDomain hooks
- [ ] IIS HostingEnvironment integration working
- [ ] IHostApplicationLifetime integration for .NET Core/5+
- [ ] Graceful shutdown with timeout support
- [ ] All unit tests passing (>90% coverage)
- [ ] Integration tests with IIS and ASP.NET Core passing
- [ ] XML documentation complete
- [ ] Code reviewed and approved
- [ ] Zero warnings

## Notes

### Design Decisions

1. **Why both AppDomain and IHostApplicationLifetime?**
   - AppDomain events: Work on .NET Framework 4.8
   - IHostApplicationLifetime: Modern .NET Core/5+ approach
   - Both needed for cross-platform support

2. **Why IRegisteredObject for IIS?**
   - IIS can notify about app pool recycle
   - Earlier notification than AppDomain.DomainUnload
   - Allows longer flush timeout before forced termination

3. **Why configurable timeout?**
   - Different applications have different flush requirements
   - Fast shutdown for development
   - Longer timeout for production with critical telemetry

### Implementation Tips

- Use reflection to check for HostingEnvironment (avoid hard dependency on System.Web)
- Register event handlers early in initialization
- Be defensive - shutdown must not throw exceptions
- Log all shutdown progress for debugging

### Common Pitfalls

- AppDomain events have very limited time (<2 seconds)
- Don't perform network I/O directly in event handlers
- IIS immediate shutdown bypasses graceful flush
- UnhandledException fires before process termination

## Related Documentation

- [Project Plan](../project-plan.md#5-implement-lifecycle-management)
- [AppDomain Events](https://learn.microsoft.com/en-us/dotnet/api/system.appdomain)
- [IHostApplicationLifetime](https://learn.microsoft.com/en-us/dotnet/api/microsoft.extensions.hosting.ihostapplicationlifetime)
- [IIS IRegisteredObject](https://learn.microsoft.com/en-us/dotnet/api/system.web.hosting.iregisteredobject)
