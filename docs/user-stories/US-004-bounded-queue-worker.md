# US-004: Bounded Queue with Channel-Based Worker

**Status**: ❌ Not Started  
**Category**: Core Package  
**Effort**: 8 story points  
**Sprint**: 2

## Description

As a **telemetry library developer**,  
I want **a high-performance bounded queue with backpressure handling**,  
So that **expensive telemetry operations don't block application threads and the system gracefully handles overload**.

## Acceptance Criteria

1. **Channel-Based Implementation**
   - [ ] Uses `System.Threading.Channels` for thread-safe queue
   - [ ] `BoundedChannelFullMode.DropOldest` for backpressure
   - [ ] Default capacity: 10,000 items (configurable)
   - [ ] Single dedicated background thread processes queue

2. **Background Processing**
   - [ ] JSON serialization performed on background thread
   - [ ] Exporter calls performed on background thread
   - [ ] Exception aggregation on background thread
   - [ ] No blocking of application threads

3. **Monitoring**
   - [ ] `DroppedEventsCount` metric tracked
   - [ ] One-time warning logged per operation type when drops occur
   - [ ] Current queue depth exposed in statistics
   - [ ] Processing rate (items/sec) tracked

4. **Graceful Shutdown**
   - [ ] `FlushAsync(TimeSpan timeout)` drains queue before shutdown
   - [ ] `CancellationToken` support for early abort
   - [ ] Partial flush if timeout exceeded
   - [ ] Remaining items count reported on timeout

5. **Error Handling**
   - [ ] Processing exceptions don't crash worker thread
   - [ ] Failed items logged and counted
   - [ ] Worker thread auto-restarts on unexpected termination

## Technical Requirements

### Core Implementation

```csharp
namespace HVO.Enterprise.Telemetry
{
    /// <summary>
    /// Background worker for processing telemetry operations asynchronously.
    /// </summary>
    internal sealed class TelemetryBackgroundWorker : IDisposable
    {
        private readonly Channel<TelemetryWorkItem> _channel;
        private readonly Thread _workerThread;
        private readonly CancellationTokenSource _shutdownCts;
        private volatile bool _disposed;
        
        // Metrics
        private long _processedCount;
        private long _droppedCount;
        private long _failedCount;
        private readonly ConcurrentDictionary<string, bool> _dropWarningsLogged;
        
        public TelemetryBackgroundWorker(int capacity = 10000)
        {
            if (capacity <= 0)
                throw new ArgumentException("Capacity must be positive", nameof(capacity));
            
            _channel = Channel.CreateBounded<TelemetryWorkItem>(
                new BoundedChannelOptions(capacity)
                {
                    FullMode = BoundedChannelFullMode.DropOldest,
                    SingleReader = true,
                    SingleWriter = false
                });
            
            _shutdownCts = new CancellationTokenSource();
            _dropWarningsLogged = new ConcurrentDictionary<string, bool>();
            
            _workerThread = new Thread(WorkerLoop)
            {
                Name = "TelemetryWorker",
                IsBackground = true,
                Priority = ThreadPriority.BelowNormal
            };
            _workerThread.Start();
        }
        
        /// <summary>
        /// Gets current queue depth.
        /// </summary>
        public int QueueDepth => _channel.Reader.Count;
        
        /// <summary>
        /// Gets total items dropped due to backpressure.
        /// </summary>
        public long DroppedCount => Interlocked.Read(ref _droppedCount);
        
        /// <summary>
        /// Gets total items processed successfully.
        /// </summary>
        public long ProcessedCount => Interlocked.Read(ref _processedCount);
        
        /// <summary>
        /// Gets total items that failed processing.
        /// </summary>
        public long FailedCount => Interlocked.Read(ref _failedCount);
        
        /// <summary>
        /// Enqueues work item for background processing.
        /// </summary>
        public bool TryEnqueue(TelemetryWorkItem item)
        {
            if (_disposed)
                return false;
            
            // TryWrite returns false if channel is full and item was dropped
            if (!_channel.Writer.TryWrite(item))
            {
                Interlocked.Increment(ref _droppedCount);
                LogDropWarning(item.OperationType);
                return false;
            }
            
            return true;
        }
        
        /// <summary>
        /// Flushes pending items with timeout.
        /// </summary>
        public async Task<FlushResult> FlushAsync(TimeSpan timeout, CancellationToken cancellationToken = default)
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(timeout);
            
            // Mark channel as complete (no more writes)
            _channel.Writer.Complete();
            
            try
            {
                // Wait for queue to drain or timeout
                await _channel.Reader.Completion.WaitAsync(cts.Token);
                
                return new FlushResult
                {
                    Success = true,
                    ItemsFlushed = ProcessedCount,
                    ItemsRemaining = 0
                };
            }
            catch (OperationCanceledException)
            {
                return new FlushResult
                {
                    Success = false,
                    ItemsFlushed = ProcessedCount,
                    ItemsRemaining = QueueDepth,
                    TimedOut = true
                };
            }
        }
        
        private void WorkerLoop()
        {
            try
            {
                var reader = _channel.Reader;
                
                while (!_shutdownCts.Token.IsCancellationRequested)
                {
                    // Wait for items or cancellation
                    if (!reader.WaitToReadAsync(_shutdownCts.Token).AsTask().Result)
                        break; // Channel completed
                    
                    // Process all available items
                    while (reader.TryRead(out var item))
                    {
                        ProcessWorkItem(item);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // Expected during shutdown
            }
            catch (Exception ex)
            {
                // Log and restart worker if possible
                LogError("Worker thread crashed", ex);
            }
        }
        
        private void ProcessWorkItem(TelemetryWorkItem item)
        {
            try
            {
                item.Execute();
                Interlocked.Increment(ref _processedCount);
            }
            catch (Exception ex)
            {
                Interlocked.Increment(ref _failedCount);
                LogError($"Failed to process {item.OperationType}", ex);
            }
        }
        
        private void LogDropWarning(string operationType)
        {
            // Log warning only once per operation type
            if (_dropWarningsLogged.TryAdd(operationType, true))
            {
                LogWarning($"Telemetry queue full: dropping {operationType} operations. " +
                          $"Consider increasing queue capacity or reducing telemetry volume.");
            }
        }
        
        public void Dispose()
        {
            if (_disposed)
                return;
            
            _disposed = true;
            _shutdownCts.Cancel();
            
            // Wait for worker thread to exit (with timeout)
            if (!_workerThread.Join(TimeSpan.FromSeconds(5)))
            {
                LogWarning("Worker thread did not exit gracefully");
            }
            
            _shutdownCts.Dispose();
        }
    }
    
    /// <summary>
    /// Represents work to be processed on background thread.
    /// </summary>
    internal abstract class TelemetryWorkItem
    {
        public abstract string OperationType { get; }
        public abstract void Execute();
    }
    
    /// <summary>
    /// Result of flush operation.
    /// </summary>
    public sealed class FlushResult
    {
        public bool Success { get; init; }
        public long ItemsFlushed { get; init; }
        public int ItemsRemaining { get; init; }
        public bool TimedOut { get; init; }
    }
}
```

### Work Item Types

```csharp
internal sealed class JsonSerializationWorkItem : TelemetryWorkItem
{
    private readonly object _data;
    private readonly Action<string> _callback;
    
    public override string OperationType => "JsonSerialization";
    
    public JsonSerializationWorkItem(object data, Action<string> callback)
    {
        _data = data;
        _callback = callback;
    }
    
    public override void Execute()
    {
        var json = JsonSerializer.Serialize(_data);
        _callback(json);
    }
}

internal sealed class ExporterWorkItem : TelemetryWorkItem
{
    private readonly IEnumerable<Activity> _activities;
    private readonly IActivityExporter _exporter;
    
    public override string OperationType => "ActivityExport";
    
    public override void Execute()
    {
        _exporter.Export(_activities);
    }
}
```

## Testing Requirements

### Unit Tests

1. **Basic Queue Operations**
   ```csharp
   [Fact]
   public void BackgroundWorker_ProcessesEnqueuedItems()
   {
       var worker = new TelemetryBackgroundWorker(capacity: 100);
       var processed = 0;
       
       for (int i = 0; i < 10; i++)
       {
           worker.TryEnqueue(new TestWorkItem(() => Interlocked.Increment(ref processed)));
       }
       
       Thread.Sleep(100); // Allow processing
       
       Assert.Equal(10, processed);
       Assert.Equal(10, worker.ProcessedCount);
   }
   ```

2. **Backpressure Tests**
   ```csharp
   [Fact]
   public void BackgroundWorker_DropsOldestWhenFull()
   {
       var worker = new TelemetryBackgroundWorker(capacity: 10);
       var barrier = new Barrier(2);
       
       // Fill queue with blocking items
       for (int i = 0; i < 10; i++)
       {
           worker.TryEnqueue(new TestWorkItem(() => barrier.SignalAndWait()));
       }
       
       // Next item should cause drop
       var enqueued = worker.TryEnqueue(new TestWorkItem(() => { }));
       
       Assert.False(enqueued);
       Assert.Equal(1, worker.DroppedCount);
   }
   ```

3. **Flush Tests**
   ```csharp
   [Fact]
   public async Task BackgroundWorker_FlushWaitsForCompletion()
   {
       var worker = new TelemetryBackgroundWorker();
       var processed = 0;
       
       for (int i = 0; i < 100; i++)
       {
           worker.TryEnqueue(new TestWorkItem(() => 
           {
               Thread.Sleep(10);
               Interlocked.Increment(ref processed);
           }));
       }
       
       var result = await worker.FlushAsync(TimeSpan.FromSeconds(30));
       
       Assert.True(result.Success);
       Assert.Equal(100, processed);
       Assert.Equal(0, result.ItemsRemaining);
   }
   
   [Fact]
   public async Task BackgroundWorker_FlushTimesOutGracefully()
   {
       var worker = new TelemetryBackgroundWorker();
       
       for (int i = 0; i < 100; i++)
       {
           worker.TryEnqueue(new TestWorkItem(() => Thread.Sleep(1000)));
       }
       
       var result = await worker.FlushAsync(TimeSpan.FromMilliseconds(100));
       
       Assert.False(result.Success);
       Assert.True(result.TimedOut);
       Assert.True(result.ItemsRemaining > 0);
   }
   ```

4. **Error Handling Tests**
   ```csharp
   [Fact]
   public void BackgroundWorker_ContinuesAfterItemFailure()
   {
       var worker = new TelemetryBackgroundWorker();
       var processed = 0;
       
       worker.TryEnqueue(new TestWorkItem(() => throw new Exception("Test")));
       worker.TryEnqueue(new TestWorkItem(() => Interlocked.Increment(ref processed)));
       
       Thread.Sleep(100);
       
       Assert.Equal(1, processed);
       Assert.Equal(1, worker.FailedCount);
       Assert.Equal(1, worker.ProcessedCount);
   }
   ```

### Performance Tests

```csharp
[Fact]
public void BackgroundWorker_HighThroughput()
{
    var worker = new TelemetryBackgroundWorker(capacity: 100000);
    var sw = Stopwatch.StartNew();
    
    for (int i = 0; i < 100000; i++)
    {
        worker.TryEnqueue(new TestWorkItem(() => { }));
    }
    
    sw.Stop();
    
    // Should enqueue 100k items in <100ms
    Assert.True(sw.ElapsedMilliseconds < 100);
}
```

## Performance Requirements

- **TryEnqueue**: <100ns (fast path, no drops)
- **TryEnqueue with drop**: <200ns (includes counter increment and drop check)
- **ProcessWorkItem**: Depends on work, but framework overhead <1μs
- **Queue depth check**: <10ns
- **Throughput**: >1M items/sec on modern hardware

## Dependencies

**Blocked By**: US-001 (Core Package Setup)  
**Blocks**: All features that queue background work (metrics, exporters, logging)

## Definition of Done

- [ ] `TelemetryBackgroundWorker` implemented with Channel
- [ ] Drop-oldest backpressure working correctly
- [ ] Graceful shutdown with flush support
- [ ] Worker thread auto-restart on crashes
- [ ] All unit tests passing (>95% coverage)
- [ ] Performance tests meet requirements
- [ ] Memory leak tests passing (no leaked work items)
- [ ] XML documentation complete
- [ ] Code reviewed and approved

## Notes

### Design Decisions

1. **Why Channels over BlockingCollection?**
   - Better async/await support
   - More efficient (fewer allocations)
   - Built-in backpressure strategies
   - Modern API design

2. **Why DropOldest vs DropNewest?**
   - Oldest data least relevant for debugging
   - Recent data more actionable
   - Matches industry standards (Prometheus, etc.)

3. **Why single dedicated thread vs ThreadPool?**
   - Predictable performance
   - No thread pool starvation
   - Can set priority (BelowNormal)
   - Easier to monitor and debug

### Implementation Tips

- Use `Stopwatch` for accurate processing rate calculation
- Consider batching exports (e.g., 100 activities at a time)
- Monitor queue depth trend (growing = problem)
- Add circuit breaker for failing exporters

### Common Pitfalls

- Don't forget to call `Complete()` on channel during shutdown
- Worker thread must handle all exceptions (or it crashes)
- Be careful with async work items (use `.GetAwaiter().GetResult()`)

## Related Documentation

- [Project Plan](../project-plan.md#4-build-bounded-queue-with-channel-based-worker)
- [System.Threading.Channels](https://learn.microsoft.com/en-us/dotnet/api/system.threading.channels)
