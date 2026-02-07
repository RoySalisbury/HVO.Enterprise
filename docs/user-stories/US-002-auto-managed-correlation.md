# US-002: Auto-Managed Correlation with AsyncLocal

**Status**: ❌ Not Started  
**Category**: Core Package  
**Effort**: 5 story points  
**Sprint**: 1

## Description

As a **developer using the telemetry library**,  
I want **automatic correlation ID management that flows through async/await boundaries**,  
So that **I can trace related operations across asynchronous code without manually passing correlation IDs**.

## Acceptance Criteria

1. **AsyncLocal Storage**
   - [ ] `CorrelationContext` class uses `AsyncLocal<string>` for thread-safe async flow
   - [ ] Correlation ID flows automatically through async/await calls
   - [ ] Works in .NET Framework 4.6.1+ and all modern .NET versions

2. **Automatic ID Generation**
   - [ ] Check `AsyncLocal<string>` first
   - [ ] Fallback to `Activity.Current?.TraceId` if AsyncLocal is empty
   - [ ] Auto-generate new `Guid` if both are empty
   - [ ] Generated ID format is consistent and traceable

3. **Manual Control**
   - [ ] `Telemetry.BeginCorrelationScope(string correlationId)` creates scope
   - [ ] Scope implements `IDisposable` for proper cleanup
   - [ ] Nested scopes work correctly (restore previous ID on dispose)
   - [ ] Thread-safe scope creation and disposal

4. **Activity Integration**
   - [ ] Correlation ID automatically added to Activity tags
   - [ ] Activity creation respects existing correlation context
   - [ ] Correlation ID propagated in distributed trace context

5. **Public API**
   - [ ] `CorrelationContext.Current` property returns current ID
   - [ ] `ICorrelationIdProvider` interface for custom providers
   - [ ] `CorrelationScope : IDisposable` for manual control

## Technical Requirements

### Core Implementation

```csharp
namespace HVO.Enterprise.Telemetry.Correlation
{
    /// <summary>
    /// Manages correlation IDs using AsyncLocal for automatic async flow.
    /// </summary>
    public static class CorrelationContext
    {
        private static readonly AsyncLocal<string?> _correlationId = new AsyncLocal<string?>();
        
        /// <summary>
        /// Gets or sets the current correlation ID. Auto-generates if not set.
        /// </summary>
        public static string Current
        {
            get
            {
                // Check AsyncLocal first
                if (!string.IsNullOrEmpty(_correlationId.Value))
                    return _correlationId.Value;
                
                // Fallback to Activity.Current?.TraceId
                var activity = Activity.Current;
                if (activity != null)
                {
                    var traceId = activity.TraceId.ToString();
                    _correlationId.Value = traceId;
                    return traceId;
                }
                
                // Auto-generate new ID
                var newId = Guid.NewGuid().ToString("N");
                _correlationId.Value = newId;
                return newId;
            }
            set => _correlationId.Value = value;
        }
        
        /// <summary>
        /// Creates a new correlation scope with the specified ID.
        /// </summary>
        public static IDisposable BeginScope(string correlationId)
        {
            if (string.IsNullOrEmpty(correlationId))
                throw new ArgumentNullException(nameof(correlationId));
            
            return new CorrelationScope(correlationId);
        }
    }
    
    /// <summary>
    /// Represents a correlation scope that restores the previous ID on disposal.
    /// </summary>
    public sealed class CorrelationScope : IDisposable
    {
        private readonly string? _previousId;
        private bool _disposed;
        
        internal CorrelationScope(string correlationId)
        {
            _previousId = CorrelationContext._correlationId.Value;
            CorrelationContext._correlationId.Value = correlationId;
        }
        
        public void Dispose()
        {
            if (_disposed)
                return;
            
            CorrelationContext._correlationId.Value = _previousId;
            _disposed = true;
        }
    }
    
    /// <summary>
    /// Interface for custom correlation ID providers.
    /// </summary>
    public interface ICorrelationIdProvider
    {
        string GenerateCorrelationId();
        bool TryGetCorrelationId(out string? correlationId);
    }
}
```

### Integration with IOperationScope

```csharp
public interface IOperationScope : IDisposable
{
    /// <summary>
    /// Gets the correlation ID for this operation.
    /// </summary>
    string CorrelationId { get; }
    
    // ... other members
}

// Implementation captures correlation ID at creation
internal class OperationScope : IOperationScope
{
    public string CorrelationId { get; }
    
    public OperationScope(string name)
    {
        // Capture current correlation ID
        CorrelationId = CorrelationContext.Current;
        
        // Add to Activity tags if Activity is active
        if (Activity.Current != null)
        {
            Activity.Current.SetTag("correlation.id", CorrelationId);
        }
    }
}
```

### HTTP Header Integration

```csharp
// Read correlation ID from HTTP header
public static class HttpContextExtensions
{
    public static string GetOrCreateCorrelationId(this HttpContext context)
    {
        // Check X-Correlation-Id header first
        if (context.Request.Headers.TryGetValue("X-Correlation-Id", out var headerValue))
        {
            var correlationId = headerValue.FirstOrDefault();
            if (!string.IsNullOrEmpty(correlationId))
            {
                CorrelationContext.Current = correlationId;
                return correlationId;
            }
        }
        
        // Check legacy header names
        if (context.Request.Headers.TryGetValue("X_HGV_TransactionId", out var legacyValue))
        {
            var correlationId = legacyValue.FirstOrDefault();
            if (!string.IsNullOrEmpty(correlationId))
            {
                CorrelationContext.Current = correlationId;
                return correlationId;
            }
        }
        
        // Auto-generate
        return CorrelationContext.Current;
    }
}
```

## Testing Requirements

### Unit Tests

1. **AsyncLocal Flow Tests**
   ```csharp
   [Fact]
   public async Task CorrelationId_FlowsThroughAsyncAwait()
   {
       var testId = Guid.NewGuid().ToString();
       CorrelationContext.Current = testId;
       
       await Task.Run(() =>
       {
           // Should have same ID in async context
           Assert.Equal(testId, CorrelationContext.Current);
       });
   }
   
   [Fact]
   public async Task CorrelationId_IsolatedBetweenAsyncContexts()
   {
       var id1 = Guid.NewGuid().ToString();
       var id2 = Guid.NewGuid().ToString();
       
       var task1 = Task.Run(() =>
       {
           CorrelationContext.Current = id1;
           Thread.Sleep(50);
           Assert.Equal(id1, CorrelationContext.Current);
       });
       
       var task2 = Task.Run(() =>
       {
           CorrelationContext.Current = id2;
           Thread.Sleep(50);
           Assert.Equal(id2, CorrelationContext.Current);
       });
       
       await Task.WhenAll(task1, task2);
   }
   ```

2. **Scope Tests**
   ```csharp
   [Fact]
   public void CorrelationScope_RestoresPreviousIdOnDispose()
   {
       var originalId = CorrelationContext.Current;
       var scopeId = Guid.NewGuid().ToString();
       
       using (CorrelationContext.BeginScope(scopeId))
       {
           Assert.Equal(scopeId, CorrelationContext.Current);
       }
       
       Assert.Equal(originalId, CorrelationContext.Current);
   }
   
   [Fact]
   public void CorrelationScope_SupportsNesting()
   {
       var id1 = "id1";
       var id2 = "id2";
       var id3 = "id3";
       
       using (CorrelationContext.BeginScope(id1))
       {
           Assert.Equal(id1, CorrelationContext.Current);
           
           using (CorrelationContext.BeginScope(id2))
           {
               Assert.Equal(id2, CorrelationContext.Current);
               
               using (CorrelationContext.BeginScope(id3))
               {
                   Assert.Equal(id3, CorrelationContext.Current);
               }
               
               Assert.Equal(id2, CorrelationContext.Current);
           }
           
           Assert.Equal(id1, CorrelationContext.Current);
       }
   }
   ```

3. **Activity Integration Tests**
   ```csharp
   [Fact]
   public void CorrelationId_AddedToActivityTags()
   {
       var correlationId = Guid.NewGuid().ToString();
       CorrelationContext.Current = correlationId;
       
       var activitySource = new ActivitySource("Test");
       using var activity = activitySource.StartActivity("TestOp");
       
       // Correlation ID should be in Activity tags
       Assert.Contains(activity!.Tags, t => 
           t.Key == "correlation.id" && t.Value == correlationId);
   }
   ```

4. **Auto-Generation Tests**
   ```csharp
   [Fact]
   public void CorrelationId_AutoGeneratesWhenEmpty()
   {
       // Clear AsyncLocal
       CorrelationContext.Current = null;
       
       // Should auto-generate
       var id1 = CorrelationContext.Current;
       Assert.False(string.IsNullOrEmpty(id1));
       
       // Should return same ID on subsequent calls
       var id2 = CorrelationContext.Current;
       Assert.Equal(id1, id2);
   }
   
   [Fact]
   public void CorrelationId_UsesActivityTraceIdAsFallback()
   {
       CorrelationContext.Current = null;
       
       var activitySource = new ActivitySource("Test");
       using var activity = activitySource.StartActivity("TestOp");
       
       var correlationId = CorrelationContext.Current;
       Assert.Equal(activity!.TraceId.ToString(), correlationId);
   }
   ```

### Integration Tests

1. **HTTP Request Correlation**
   - [ ] Correlation ID extracted from `X-Correlation-Id` header
   - [ ] Correlation ID extracted from `X_HGV_TransactionId` header (legacy)
   - [ ] Auto-generated if no header present
   - [ ] Correlation ID included in response headers

2. **Cross-Service Correlation**
   - [ ] Correlation ID propagated through HTTP client calls
   - [ ] Correlation ID maintained across WCF service boundaries
   - [ ] Correlation ID flows through background job queues

## Performance Requirements

- **AsyncLocal access**: <5ns per read
- **Scope creation**: <50ns
- **Scope disposal**: <50ns
- **Auto-generation (Guid)**: ~100ns
- **Activity tag addition**: <20ns

## Dependencies

**Blocked By**: US-001 (Core Package Setup)  
**Blocks**: 
- US-003 (Background Job Correlation)
- US-010 (ActivitySource Sampling)
- US-012 (Operation Scope)
- US-013 (ILogger Enrichment)

## Definition of Done

- [ ] `CorrelationContext` class implemented with AsyncLocal
- [ ] `CorrelationScope` class implements IDisposable correctly
- [ ] All unit tests passing (>95% coverage)
- [ ] Integration tests passing
- [ ] Performance benchmarks meet requirements
- [ ] XML documentation complete
- [ ] Code reviewed and approved
- [ ] Zero warnings in build

## Notes

### Design Decisions

1. **Why AsyncLocal instead of ThreadLocal?**
   - AsyncLocal flows through async/await boundaries automatically
   - ThreadLocal would lose context on thread pool threads
   - AsyncLocal available in .NET Framework 4.6.1+ via .NET Standard 2.0

2. **Why three-tier fallback (AsyncLocal → Activity → Generate)?**
   - AsyncLocal: Explicitly set correlation ID takes precedence
   - Activity.TraceId: Automatic integration with distributed tracing
   - Guid: Ensures there's always a correlation ID

3. **Why restore previous ID on dispose?**
   - Enables nested scopes (e.g., batch job → individual item processing)
   - Matches ILogger scope semantics
   - Prevents ID leakage across operations

### Implementation Tips

- Use `AsyncLocal<string?>` nullable for better null handling
- Cache Activity.Current to avoid multiple lookups
- Consider pooling CorrelationScope instances for high-throughput scenarios
- Add diagnostic logging for correlation ID changes (debug builds only)

### Common Pitfalls

- Don't call `CorrelationContext.Current` inside property getter (infinite recursion)
- Ensure CorrelationScope is disposed even on exceptions (use `using` statement)
- Be careful with long-lived scopes (memory implications)

## Related Documentation

- [Project Plan](../project-plan.md#2-implement-auto-managed-correlation-with-asynclocal)
- [AsyncLocal Documentation](https://learn.microsoft.com/en-us/dotnet/api/system.threading.asynclocal-1)
- [Activity Documentation](https://learn.microsoft.com/en-us/dotnet/api/system.diagnostics.activity)
