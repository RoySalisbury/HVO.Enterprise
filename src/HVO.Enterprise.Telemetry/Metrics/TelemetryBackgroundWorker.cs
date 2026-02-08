using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace HVO.Enterprise.Telemetry.Metrics
{
    /// <summary>
    /// Background worker for processing telemetry operations asynchronously.
    /// Uses a bounded channel with drop-oldest backpressure strategy.
    /// Includes circuit breaker pattern for automatic restart on transient failures.
    /// </summary>
    internal sealed class TelemetryBackgroundWorker : IDisposable
    {
        private readonly Channel<TelemetryWorkItem> _channel;
        private readonly CancellationTokenSource _shutdownCts;
        private readonly ILogger<TelemetryBackgroundWorker> _logger;
        private readonly int _capacity;
        private readonly int _maxRestartAttempts;
        private readonly TimeSpan _baseRestartDelay;
        private volatile bool _disposed;
        private volatile bool _circuitOpen;
        
        // Thread management
        private Thread? _workerThread;
        private readonly object _threadLock = new object();
        
        // Metrics
        private long _processedCount;
        private long _droppedCount;
        private long _failedCount;
        private long _restartCount;
        private readonly ConcurrentDictionary<string, bool> _dropWarningsLogged;
        
        /// <summary>
        /// Creates a new background worker with the specified capacity.
        /// </summary>
        /// <param name="capacity">Maximum number of items in queue. Default is 10,000.</param>
        /// <param name="maxRestartAttempts">Maximum number of restart attempts before giving up. Default is 3.</param>
        /// <param name="baseRestartDelay">Base delay between restart attempts (exponential backoff applied). Default is 1 second.</param>
        /// <param name="logger">Optional logger for diagnostics.</param>
        public TelemetryBackgroundWorker(
            int capacity = 10000, 
            int maxRestartAttempts = 3,
            TimeSpan? baseRestartDelay = null,
            ILogger<TelemetryBackgroundWorker>? logger = null)
        {
            if (capacity <= 0)
                throw new ArgumentException("Capacity must be positive", nameof(capacity));
            
            if (maxRestartAttempts < 0)
                throw new ArgumentException("Max restart attempts must be non-negative", nameof(maxRestartAttempts));
            
            var effectiveDelay = baseRestartDelay ?? TimeSpan.FromSeconds(1);
            if (effectiveDelay < TimeSpan.Zero)
                throw new ArgumentException("Base restart delay must be non-negative", nameof(baseRestartDelay));
            
            if (effectiveDelay > TimeSpan.FromMinutes(5))
                throw new ArgumentException("Base restart delay must not exceed 5 minutes", nameof(baseRestartDelay));
            
            _logger = logger ?? NullLogger<TelemetryBackgroundWorker>.Instance;
            _capacity = capacity;
            _maxRestartAttempts = maxRestartAttempts;
            _baseRestartDelay = effectiveDelay;
            
            _channel = Channel.CreateBounded<TelemetryWorkItem>(
                new BoundedChannelOptions(capacity)
                {
                    FullMode = BoundedChannelFullMode.DropOldest,
                    SingleReader = true,
                    SingleWriter = false
                });
            
            _shutdownCts = new CancellationTokenSource();
            _dropWarningsLogged = new ConcurrentDictionary<string, bool>();
            
            StartWorkerThread();
            
            _logger.LogDebug(
                "TelemetryBackgroundWorker started with capacity {Capacity}, maxRestarts {MaxRestarts}, baseDelay {BaseDelay}ms",
                capacity, maxRestartAttempts, _baseRestartDelay.TotalMilliseconds);
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
        /// Gets the number of times the worker processing loop has been restarted due to unexpected failures.
        /// </summary>
        public long RestartCount => Interlocked.Read(ref _restartCount);
        
        /// <summary>
        /// Gets a value indicating whether the circuit breaker is open (worker has stopped due to repeated failures).
        /// When true, no new items will be processed.
        /// </summary>
        public bool IsCircuitOpen => _circuitOpen;
        
        /// <summary>
        /// Enqueues work item for background processing.
        /// Returns false if queue is full and item was dropped.
        /// </summary>
        /// <param name="item">Work item to process.</param>
        /// <returns>True if enqueued successfully, false if dropped.</returns>
        public bool TryEnqueue(TelemetryWorkItem item)
        {
            if (item == null)
                throw new ArgumentNullException(nameof(item));
            
            if (_disposed)
            {
                _logger.LogWarning("Attempted to enqueue item after worker disposed");
                return false;
            }
            
            if (_circuitOpen)
            {
                _logger.LogWarning("Attempted to enqueue item after circuit breaker opened. Worker has stopped.");
                return false;
            }
            
            // With DropOldest mode, TryWrite always returns true but may drop oldest item.
            // Check if queue is at capacity before writing to detect drops.
            var wasAtCapacity = _channel.Reader.Count >= _capacity;
            
            _channel.Writer.TryWrite(item);
            
            if (wasAtCapacity)
            {
                // An old item was dropped to make room
                Interlocked.Increment(ref _droppedCount);
                LogDropWarning(item.OperationType);
                return false;
            }
            
            return true;
        }
        
        /// <summary>
        /// Flushes pending items with timeout.
        /// </summary>
        /// <param name="timeout">Maximum time to wait for queue to drain.</param>
        /// <param name="cancellationToken">Cancellation token for early abort.</param>
        /// <returns>Result indicating success, items flushed, and items remaining.</returns>
        public async Task<FlushResult> FlushAsync(TimeSpan timeout, CancellationToken cancellationToken = default)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(TelemetryBackgroundWorker));
            
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(timeout);
            
            var startCount = ProcessedCount;
            var startQueueDepth = QueueDepth;
            
            // Mark channel as complete (no more writes)
            _channel.Writer.Complete();
            
            try
            {
                // Wait for queue to drain or timeout
#if NET8_0_OR_GREATER
                await _channel.Reader.Completion.WaitAsync(cts.Token);
#else
                // .NET Standard 2.0 doesn't have WaitAsync extension
                var tcs = new TaskCompletionSource<bool>();
                using (cts.Token.Register(() => tcs.TrySetCanceled()))
                {
                    var completedTask = await Task.WhenAny(_channel.Reader.Completion, tcs.Task);
                    if (completedTask == tcs.Task)
                        throw new OperationCanceledException(cts.Token);
                }
#endif
                
                // Wait a bit for final processing
                await Task.Delay(50, CancellationToken.None);
                
                var flushed = ProcessedCount - startCount;
                _logger.LogInformation("Flush completed successfully. Flushed {Count} items", flushed);
                
                return new FlushResult
                {
                    Success = true,
                    ItemsFlushed = flushed,
                    ItemsRemaining = 0
                };
            }
            catch (OperationCanceledException)
            {
                var flushed = ProcessedCount - startCount;
                var remaining = QueueDepth;
                
                _logger.LogWarning("Flush timed out after {Timeout}. Flushed {Flushed} items, {Remaining} remaining",
                    timeout, flushed, remaining);
                
                return new FlushResult
                {
                    Success = false,
                    ItemsFlushed = flushed,
                    ItemsRemaining = remaining,
                    TimedOut = true
                };
            }
        }
        
        private void StartWorkerThread()
        {
            lock (_threadLock)
            {
                if (_disposed)
                {
                    _logger.LogWarning("Cannot start worker thread after disposal");
                    return;
                }
                
                _workerThread = new Thread(WorkerLoop)
                {
                    Name = "TelemetryWorker",
                    IsBackground = true,
                    Priority = ThreadPriority.BelowNormal
                };
                _workerThread.Start();
            }
        }
        
        private void WorkerLoop()
        {
            var consecutiveFailures = 0;
            
            while (!_disposed && consecutiveFailures <= _maxRestartAttempts)
            {
                try
                {
                    RunProcessingLoop();
                    
                    // Normal exit (channel completed, shutdown requested)
                    _logger.LogDebug("Worker loop exiting normally");
                    break;
                }
                catch (OperationCanceledException)
                {
                    // Expected during shutdown
                    _logger.LogDebug("Worker loop cancelled");
                    break;
                }
                catch (Exception ex)
                {
                    consecutiveFailures++;
                    
                    if (consecutiveFailures > _maxRestartAttempts)
                    {
                        _circuitOpen = true;
                        _channel.Writer.TryComplete(); // Complete channel to fail future enqueues gracefully
                        
                        _logger.LogCritical(ex, 
                            "Worker processing loop crashed after {Failures} consecutive failures. Circuit breaker open - worker will not restart.",
                            consecutiveFailures);
                        break;
                    }
                    
                    // Calculate exponential backoff delay using non-overflowing arithmetic
                    const double maxBackoffMs = 30000d;
                    double rawDelayMs = _baseRestartDelay.TotalMilliseconds * Math.Pow(2d, consecutiveFailures - 1);
                    double clampedDelayMs = Math.Min(Math.Max(rawDelayMs, 0d), maxBackoffMs);
                    var delay = TimeSpan.FromMilliseconds(clampedDelayMs);
                    
                    _logger.LogError(ex,
                        "Worker processing loop crashed unexpectedly (failure {Failure}/{MaxFailures}). Restarting after {Delay}ms...",
                        consecutiveFailures, _maxRestartAttempts, delay.TotalMilliseconds);
                    
                    // Wait before restart (allow cancellation during wait)
                    try
                    {
                        Task.Delay(delay, _shutdownCts.Token).GetAwaiter().GetResult();
                    }
                    catch (OperationCanceledException)
                    {
                        _logger.LogDebug("Restart cancelled during backoff delay");
                        break;
                    }
                    
                    if (!_disposed)
                    {
                        Interlocked.Increment(ref _restartCount);
                        _logger.LogInformation("Worker processing loop restarting (attempt {Attempt})", consecutiveFailures + 1);
                    }
                }
            }
        }
        
        private void RunProcessingLoop()
        {
            var reader = _channel.Reader;
            
            while (!_shutdownCts.Token.IsCancellationRequested)
            {
                try
                {
                    // Wait for items or cancellation
                    var waitTask = reader.WaitToReadAsync(_shutdownCts.Token);
                    if (!waitTask.AsTask().Result)
                        break; // Channel completed
                    
                    // Process all available items
                    while (reader.TryRead(out var item))
                    {
                        ProcessWorkItem(item);
                    }
                }
                catch (OperationCanceledException)
                {
                    // Expected during shutdown
                    break;
                }
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
                _logger.LogError(ex, "Failed to process {OperationType} work item", item.OperationType);
            }
        }
        
        private void LogDropWarning(string operationType)
        {
            // Log warning only once per operation type to avoid log spam
            if (_dropWarningsLogged.TryAdd(operationType, true))
            {
                _logger.LogWarning("Telemetry queue full: dropping {OperationType} operations. " +
                    "Consider increasing queue capacity or reducing telemetry volume. " +
                    "Total drops: {DroppedCount}",
                    operationType, DroppedCount);
            }
        }
        
        /// <summary>
        /// Disposes the worker, cancelling the background thread and releasing resources.
        /// </summary>
        public void Dispose()
        {
            if (_disposed)
                return;
            
            _disposed = true;
            
            _logger.LogDebug("Disposing TelemetryBackgroundWorker. Processed: {Processed}, Failed: {Failed}, Dropped: {Dropped}, Restarts: {Restarts}",
                ProcessedCount, FailedCount, DroppedCount, RestartCount);
            
            _shutdownCts.Cancel();
            
            // Wait for worker thread to exit (with timeout)
            lock (_threadLock)
            {
                if (_workerThread != null && !_workerThread.Join(TimeSpan.FromSeconds(5)))
                {
                    _logger.LogWarning("Worker thread did not exit gracefully within timeout");
                }
            }
            
            _shutdownCts.Dispose();
        }
    }
}
