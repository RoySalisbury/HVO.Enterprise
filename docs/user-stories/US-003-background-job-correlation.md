# US-003: Background Job Correlation Utilities

**Status**: ❌ Not Started  
**Category**: Core Package  
**Effort**: 5 story points  
**Sprint**: 1

## Description

As a **developer working with background jobs**,  
I want **utilities to automatically capture and restore correlation context across job boundaries**,  
So that **I can trace operations from HTTP requests through to background job execution without manual context management**.

## Acceptance Criteria

1. **Context Capture**
   - [ ] `BackgroundJobContext` class captures correlation ID at job enqueue time
   - [ ] User context captured (if available)
   - [ ] Parent Activity context captured
   - [ ] Timestamp of enqueue recorded
   - [ ] Optional custom metadata supported

2. **Context Restoration**
   - [ ] `[TelemetryJobContext]` attribute automatically restores context at job execution
   - [ ] Manual restoration via `BackgroundJobContext.Restore()` supported
   - [ ] Activity parent link maintained
   - [ ] Correlation ID propagated correctly

3. **Integration Helpers**
   - [ ] `IBackgroundJobContextPropagator` interface for framework integration
   - [ ] Extension method `correlationId.EnqueueJob(() => ...)` captures context
   - [ ] Hangfire integration helper
   - [ ] Quartz.NET integration helper
   - [ ] IHostedService integration pattern

4. **Thread Safety**
   - [ ] Context capture is thread-safe
   - [ ] Context restoration is thread-safe
   - [ ] No race conditions in async scenarios

## Technical Requirements

### Core Classes

```csharp
namespace HVO.Enterprise.Telemetry.BackgroundJobs
{
    /// <summary>
    /// Captures telemetry context for background job execution.
    /// </summary>
    public sealed class BackgroundJobContext
    {
        public string CorrelationId { get; }
        public string? ParentActivityId { get; }
        public string? ParentSpanId { get; }
        public Dictionary<string, string>? UserContext { get; }
        public DateTimeOffset EnqueuedAt { get; }
        public Dictionary<string, object>? CustomMetadata { get; }
        
        /// <summary>
        /// Captures current telemetry context.
        /// </summary>
        public static BackgroundJobContext Capture()
        {
            var activity = Activity.Current;
            return new BackgroundJobContext
            {
                CorrelationId = CorrelationContext.Current,
                ParentActivityId = activity?.TraceId.ToString(),
                ParentSpanId = activity?.SpanId.ToString(),
                UserContext = CaptureUserContext(),
                EnqueuedAt = DateTimeOffset.UtcNow,
                CustomMetadata = null
            };
        }
        
        /// <summary>
        /// Restores telemetry context for job execution.
        /// </summary>
        public IDisposable Restore()
        {
            return new BackgroundJobContextScope(this);
        }
        
        private static Dictionary<string, string>? CaptureUserContext()
        {
            // Implementation will use UserContextEnricher (US-011)
            return null; // Placeholder
        }
    }
    
    /// <summary>
    /// Scope that restores background job context.
    /// </summary>
    internal sealed class BackgroundJobContextScope : IDisposable
    {
        private readonly IDisposable _correlationScope;
        private readonly Activity? _activity;
        private bool _disposed;
        
        public BackgroundJobContextScope(BackgroundJobContext context)
        {
            // Restore correlation ID
            _correlationScope = CorrelationContext.BeginScope(context.CorrelationId);
            
            // Create Activity with parent link
            if (!string.IsNullOrEmpty(context.ParentActivityId))
            {
                var activitySource = new ActivitySource("HVO.Enterprise.Telemetry.BackgroundJobs");
                _activity = activitySource.StartActivity(
                    "BackgroundJob",
                    ActivityKind.Internal,
                    parentContext: new ActivityContext(
                        ActivityTraceId.CreateFromString(context.ParentActivityId.AsSpan()),
                        ActivitySpanId.CreateFromString(context.ParentSpanId.AsSpan()),
                        ActivityTraceFlags.None));
                
                // Add job metadata
                _activity?.SetTag("job.enqueued_at", context.EnqueuedAt);
                _activity?.SetTag("job.execution_delay_ms", 
                    (DateTimeOffset.UtcNow - context.EnqueuedAt).TotalMilliseconds);
            }
        }
        
        public void Dispose()
        {
            if (_disposed)
                return;
            
            _activity?.Dispose();
            _correlationScope.Dispose();
            _disposed = true;
        }
    }
}
```

### Attribute for Automatic Restoration

```csharp
/// <summary>
/// Automatically restores telemetry context for background job methods.
/// Apply to Hangfire, Quartz, or custom job methods.
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public sealed class TelemetryJobContextAttribute : Attribute
{
    /// <summary>
    /// Name of the parameter containing BackgroundJobContext.
    /// Defaults to "context".
    /// </summary>
    public string ContextParameterName { get; set; } = "context";
    
    /// <summary>
    /// Whether to create a new Activity for the job.
    /// Defaults to true.
    /// </summary>
    public bool CreateActivity { get; set; } = true;
}
```

### Integration Helpers

```csharp
/// <summary>
/// Interface for background job framework integration.
/// </summary>
public interface IBackgroundJobContextPropagator
{
    /// <summary>
    /// Captures context and adds to job data.
    /// </summary>
    void PropagateContext<TJob>(TJob job) where TJob : class;
    
    /// <summary>
    /// Restores context from job data before execution.
    /// </summary>
    IDisposable? RestoreContext<TJob>(TJob job) where TJob : class;
}

/// <summary>
/// Extension methods for background job correlation.
/// </summary>
public static class BackgroundJobExtensions
{
    /// <summary>
    /// Enqueues a background job with current correlation context.
    /// </summary>
    public static void EnqueueWithContext(this string correlationId, Action action)
    {
        var context = BackgroundJobContext.Capture();
        
        // Enqueue with captured context
        // Actual implementation depends on job framework
        ThreadPool.QueueUserWorkItem(_ =>
        {
            using (context.Restore())
            {
                action();
            }
        });
    }
    
    /// <summary>
    /// Enqueues an async background job with current correlation context.
    /// </summary>
    public static Task EnqueueWithContextAsync(this string correlationId, Func<Task> action)
    {
        var context = BackgroundJobContext.Capture();
        
        return Task.Run(async () =>
        {
            using (context.Restore())
            {
                await action();
            }
        });
    }
}
```

### Hangfire Integration

```csharp
namespace HVO.Enterprise.Telemetry.BackgroundJobs.Hangfire
{
    /// <summary>
    /// Hangfire filter for automatic context propagation.
    /// </summary>
    public class TelemetryJobFilter : IClientFilter, IServerFilter
    {
        // Capture context when job is created
        public void OnCreating(CreatingContext filterContext)
        {
            var context = BackgroundJobContext.Capture();
            filterContext.SetJobParameter("TelemetryContext", context);
        }
        
        // Restore context when job executes
        public void OnPerforming(PerformingContext filterContext)
        {
            if (filterContext.GetJobParameter<BackgroundJobContext>("TelemetryContext") is { } context)
            {
                var scope = context.Restore();
                filterContext.Items["TelemetryScope"] = scope;
            }
        }
        
        // Clean up
        public void OnPerformed(PerformedContext filterContext)
        {
            if (filterContext.Items["TelemetryScope"] is IDisposable scope)
            {
                scope.Dispose();
            }
        }
        
        public void OnCreated(CreatedContext filterContext) { }
    }
    
    /// <summary>
    /// Extension methods for Hangfire configuration.
    /// </summary>
    public static class HangfireExtensions
    {
        public static IGlobalConfiguration UseTelemetry(this IGlobalConfiguration configuration)
        {
            configuration.UseFilter(new TelemetryJobFilter());
            return configuration;
        }
    }
}
```

## Testing Requirements

### Unit Tests

1. **Context Capture Tests**
   ```csharp
   [Fact]
   public void BackgroundJobContext_CapturesCorrelationId()
   {
       var correlationId = Guid.NewGuid().ToString();
       CorrelationContext.Current = correlationId;
       
       var context = BackgroundJobContext.Capture();
       
       Assert.Equal(correlationId, context.CorrelationId);
   }
   
   [Fact]
   public void BackgroundJobContext_CapturesParentActivity()
   {
       var activitySource = new ActivitySource("Test");
       using var activity = activitySource.StartActivity("Parent");
       
       var context = BackgroundJobContext.Capture();
       
       Assert.Equal(activity!.TraceId.ToString(), context.ParentActivityId);
       Assert.Equal(activity.SpanId.ToString(), context.ParentSpanId);
   }
   
   [Fact]
   public void BackgroundJobContext_CapturesEnqueueTime()
   {
       var before = DateTimeOffset.UtcNow;
       var context = BackgroundJobContext.Capture();
       var after = DateTimeOffset.UtcNow;
       
       Assert.True(context.EnqueuedAt >= before);
       Assert.True(context.EnqueuedAt <= after);
   }
   ```

2. **Context Restoration Tests**
   ```csharp
   [Fact]
   public void BackgroundJobContext_RestoresCorrelationId()
   {
       var originalId = Guid.NewGuid().ToString();
       CorrelationContext.Current = originalId;
       var context = BackgroundJobContext.Capture();
       
       // Clear and set different ID
       CorrelationContext.Current = Guid.NewGuid().ToString();
       
       using (context.Restore())
       {
           Assert.Equal(originalId, CorrelationContext.Current);
       }
   }
   
   [Fact]
   public void BackgroundJobContext_CreatesChildActivity()
   {
       var activitySource = new ActivitySource("Test");
       using var parentActivity = activitySource.StartActivity("Parent");
       var parentTraceId = parentActivity!.TraceId;
       
       var context = BackgroundJobContext.Capture();
       parentActivity.Dispose();
       
       using (context.Restore())
       {
           var currentActivity = Activity.Current;
           Assert.NotNull(currentActivity);
           Assert.Equal(parentTraceId, currentActivity!.TraceId);
           Assert.NotEqual(parentActivity.SpanId, currentActivity.SpanId);
       }
   }
   ```

3. **Async Flow Tests**
   ```csharp
   [Fact]
   public async Task BackgroundJobContext_FlowsThroughAsyncBoundaries()
   {
       var correlationId = Guid.NewGuid().ToString();
       CorrelationContext.Current = correlationId;
       var context = BackgroundJobContext.Capture();
       
       await Task.Run(() =>
       {
           using (context.Restore())
           {
               Assert.Equal(correlationId, CorrelationContext.Current);
           }
       });
   }
   ```

### Integration Tests

1. **Hangfire Integration**
   - [ ] Context captured when job enqueued with `BackgroundJob.Enqueue()`
   - [ ] Context restored when job executes
   - [ ] Correlation ID flows through Hangfire storage
   - [ ] Activity parent link maintained

2. **IHostedService Integration**
   - [ ] Context captured in BackgroundService
   - [ ] Context flows through periodic tasks
   - [ ] Correlation maintained across service restarts

3. **Thread Pool Jobs**
   - [ ] Context captured for ThreadPool.QueueUserWorkItem
   - [ ] Context restored on worker thread
   - [ ] Multiple concurrent jobs don't interfere

## Performance Requirements

- **Context capture**: <500ns
- **Context restoration**: <1μs
- **Scope disposal**: <500ns
- **Attribute processing**: <2μs (one-time per method)

## Dependencies

**Blocked By**: 
- US-001 (Core Package Setup)
- US-002 (Auto-Managed Correlation)

**Blocks**: 
- US-027 (.NET Framework 4.8 Sample - uses Hangfire)
- US-028 (.NET 8 Sample - uses IHostedService)

## Definition of Done

- [ ] `BackgroundJobContext` class implemented with capture/restore
- [ ] `BackgroundJobContextScope` implements proper cleanup
- [ ] `[TelemetryJobContext]` attribute implemented
- [ ] Extension methods for common job frameworks
- [ ] Hangfire integration helper included
- [ ] All unit tests passing (>90% coverage)
- [ ] Integration tests with Hangfire passing
- [ ] Performance benchmarks meet requirements
- [ ] XML documentation complete
- [ ] Usage examples in doc comments
- [ ] Code reviewed and approved

## Notes

### Design Decisions

1. **Why capture at enqueue vs execution time?**
   - Enqueue time: Preserves request context (user, correlation ID)
   - Execution time: May be seconds/minutes later, original context lost
   - Captured context = "why was this job created?"

2. **Why Activity parent link instead of copying full Activity?**
   - Activity should represent actual work being done
   - Parent link maintains distributed trace while creating new span
   - Avoids confusion about when/where work actually happened

3. **Why interface for propagation?**
   - Different job frameworks have different storage mechanisms
   - Allows users to implement custom propagation strategies
   - Keeps core library framework-agnostic

### Implementation Tips

- Serialize `BackgroundJobContext` to JSON for storage in job frameworks
- Consider compression for large context objects
- Add timeout for old contexts (warn if job delayed >1 hour)
- Include original enqueue stack trace in debug builds

### Common Pitfalls

- Don't forget to dispose restoration scope (memory leak)
- Be careful with long-running jobs (context in memory)
- Handle serialization of user context carefully (PII concerns)

### Integration Patterns

**Hangfire**:
```csharp
GlobalConfiguration.Configuration.UseTelemetry();

BackgroundJob.Enqueue(() => ProcessOrder(orderId));
```

**IHostedService**:
```csharp
public class MyService : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var context = BackgroundJobContext.Capture();
        
        await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
        
        using (context.Restore())
        {
            // Work with restored context
        }
    }
}
```

## Related Documentation

- [Project Plan](../project-plan.md#3-build-background-job-correlation-utilities)
- [Hangfire Documentation](https://docs.hangfire.io/)
- [Background Tasks in .NET](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/host/hosted-services)
