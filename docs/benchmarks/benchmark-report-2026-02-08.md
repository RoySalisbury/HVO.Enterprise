# Benchmark Report (2026-02-08)

## Environment

- OS: Ubuntu 24.04.3 LTS (container)
- CPU: Arm64 (unknown model)
- .NET SDK: 10.0.102
- Runtime: .NET 10.0.2 (RyuJIT, AdvSIMD)
- BenchmarkDotNet: 0.13.12

## Commands

- dotnet run -c Release --project benchmarks/HVO.Common.Benchmarks
- dotnet run -c Release --project benchmarks/HVO.Enterprise.Telemetry.Benchmarks

## Notes

- Benchmarks run non-interactively by default (using --filter * when no args are supplied).
- Creation and simple accessor benchmarks use OperationsPerInvoke=1000 to avoid zero-measurement noise.
- The container does not allow high priority process scheduling, so BenchmarkDotNet reports a permission warning. Results are still produced.

## Optimization Summary (Run 2 vs Run 1)

| Metric | Before | After | Improvement |
| --- | ---: | ---: | --- |
| QueueDepth Read | 13.69 ns | 0.44 ns | **31x faster** |
| Probabilistic Sampling (alloc) | 213 B | 1 B | **213x less allocation** |
| PerSource Sampling | 616 ns / 345 B | 280 ns / 1 B | **2.2x faster, 345x less alloc** |
| Adaptive Sampling | 268 ns / 145 B | 49 ns / 1 B | **5.5x faster, 145x less alloc** |
| Adaptive Adjustment | 5,742 ns / 880 B | 979 ns / 736 B | **5.9x faster** |
| OperationScope Create (minimal) | 112.9 ns / 440 B | 93.5 ns / 368 B | **17% faster, 16% less alloc** |
| TryEnqueue Drop | 46.2 ns | 36.2 ns | **22% faster** |

### Optimizations Applied

1. **ProbabilisticSampler**: Pre-computed reason strings in constructor; inline hex parsing eliminates Substring allocation
2. **AdaptiveSampler**: Cached rate-based reason strings; static results for 100%/0% edge cases; eliminated double string.Format
3. **PerSourceSampler**: Pass-through of inner sampler results; eliminated string concatenation per call
4. **TelemetryBackgroundWorker**: Replaced `Channel.Reader.Count` with `Interlocked`-based atomic counter
5. **OperationScope**: Shared static `PiiRedactor` instance; `Stopwatch.GetTimestamp()` replaces `Stopwatch.StartNew()` allocation
6. **ConfigurationProvider**: Added effective configuration cache with auto-invalidation on mutations
7. **CorrelationContext**: Tighter null check on AsyncLocal hot path

## HVO.Common Benchmarks

### OptionBenchmarks

| Method | Category | Mean (ns) | Allocated |
| --- | --- | ---: | ---: |
| Some_Create | Acceptance | 1.2093 | - |
| None_Create | Acceptance | 2.0198 | - |
| GetValueOrDefault | Logic | 0.5902 | - |

### OneOfBenchmarks

| Method | Category | Mean (ns) | Allocated |
| --- | --- | ---: | ---: |
| Create_FromT1 | Acceptance | 1.0692 | - |
| Create_FromT2 | Acceptance | 1.0408 | - |
| Match_T1 | Logic | 0.5839 | - |
| Match_T2 | Logic | 0.7123 | - |

### ResultBenchmarks

| Method | Category | Mean (ns) | Allocated |
| --- | --- | ---: | ---: |
| Success_Create | Acceptance | 1.0107 | - |
| Failure_Create | Acceptance | 14.1499 | 56 B |
| Match_Success | Logic | 0.5656 | - |
| Match_Failure | Logic | 16.3459 | 56 B |

## HVO.Enterprise.Telemetry Benchmarks

### BackgroundJobBenchmarks

| Method | Category | Mean (ns) | Allocated |
| --- | --- | ---: | ---: |
| Attribute_ReflectionLookup | Attribute | 662.77 | 232 B |
| Capture_NoActivity | Capture | 37.44 | 72 B |
| Capture_WithActivity | Capture | 38.25 | 72 B |
| Restore_Scope | Restore | 34.15 | 72 B |

### ConfigurationBenchmarks

| Method | Category | Mean (ns) | Allocated |
| --- | --- | ---: | ---: |
| GetEffectiveConfiguration_Default | Lookup | 30.08 | 224 B |
| GetEffectiveConfiguration_AllOverrides | Lookup | 442.94 | 1,632 B |
| OperationConfiguration_Merge | Merge | 32.85 | 368 B |
| ApplyAttributeConfiguration | Reflection | 7,233.63 | 15,545 B |
| TelemetryOptions_Validate | Validate | 3.54 | - |

### ConfigurationHotReloadBenchmarks

| Method | Category | Mean (us) | Allocated |
| --- | --- | ---: | ---: |
| FileConfigurationReloader_ChangePropagation | FileReload | 506,071.9 | 97.96 KB |
| ConfigurationHttpEndpoint_Update | HttpEndpoint | 166.0 | 21.26 KB |

### ContextEnrichmentBenchmarks

| Method | Category | Mean (ns) | Allocated |
| --- | --- | ---: | ---: |
| EnrichActivity_Minimal | Activity | 254.8 | 68 B |
| EnrichActivity_Standard | Activity | 9,183.4 | 2,045 B |
| EnrichActivity_Verbose | Activity | 9,483.5 | 2,117 B |
| EnrichProperties_Minimal | Properties | 214.0 | 68 B |

### ContextProviderBenchmarks

| Method | Category | Mean (ns) | Allocated |
| --- | --- | ---: | ---: |
| EnvironmentProvider_EnrichActivity | Provider | 664.2 | 73 B |
| UserProvider_EnrichActivity | Provider | 506.9 | 1 B |
| RequestProvider_EnrichActivity | Provider | 1,753.0 | 65 B |
| EnvironmentProvider_EnrichProperties | Provider | 198.8 | 74 B |

### CorrelationBenchmarks

| Method | Category | Mean (ns) | Allocated |
| --- | --- | ---: | ---: |
| Activity_TagAddition | Activity | 0.025 | 1 B |
| Current_Read | HotPath | 19.30 | 1 B |
| Scope_CreateDispose | HotPath | 66.22 | 33 B |
| Current_AutoGenerate | HotPath | 457.19 | 167 B |
| Current_FromActivity | HotPath | 475.62 | 167 B |

### ExceptionTrackingBenchmarks

| Method | Category | Mean (ns) | Allocated |
| --- | --- | ---: | ---: |
| RecordException | Aggregation | 1,809.26 | 1,537 B |
| GetGroup | Aggregation | 25.82 | - |
| GenerateFingerprint | Fingerprint | 1,523.96 | 1,369 B |

### LifecycleBenchmarks

| Method | Category | Mean (ns) | Allocated |
| --- | --- | ---: | ---: |
| Register_Unregister_Events | Registration | 211.9 | 736 B |

| Method | Category | Mean (ms) | Allocated |
| --- | --- | ---: | ---: |
| ShutdownAsync_EmptyQueue | Shutdown | 53.2 | 2,768 B |

### MetricRecorderBenchmarks

| Method | Category | Mean (ns) | Allocated |
| --- | --- | ---: | ---: |
| Counter_Add_NoTags | Acceptance | 1.23 | - |
| Counter_Add_OneTag | Acceptance | 2.33 | - |
| Counter_Add_TwoTags | Acceptance | 2.11 | - |
| Counter_Add_ThreeTags | Acceptance | 2.66 | - |
| Histogram_Record_NoTags | Logic | 0.79 | - |
| Histogram_Record_OneTag | Logic | 1.67 | - |
| Histogram_Record_TwoTags | Logic | 2.33 | - |
| Histogram_Record_ThreeTags | Logic | 2.69 | - |
| HistogramDouble_Record_NoTags | Logic | 1.04 | - |

### MetricRecorderFactoryBenchmarks

| Method | Category | Mean (ns) | Allocated |
| --- | --- | ---: | ---: |
| Instance_Access | Runtime | 1.88 | - |

### MetricObservableGaugeBenchmarks

| Method | Category | Mean (ns) | Allocated |
| --- | --- | ---: | ---: |
| ObservableGauge_Callback | Gauge | 18.64 | 72 B |

### TelemetryBackgroundWorkerBenchmarks

| Method | Category | Mean (ns) | Allocated |
| --- | --- | ---: | ---: |
| TryEnqueue_FastPath | Enqueue | 96.86 | - |
| TryEnqueue_DropPath | Enqueue | 36.17 | - |
| QueueDepth_Read | Queue | 0.44 | - |
| TryEnqueue_Throughput | Throughput | 124.60 | - |

### OperationScopeBenchmarks

| Method | Category | Mean (ns) | Allocated |
| --- | --- | ---: | ---: |
| CreateDispose_Minimal | Create | 93.51 | 368 B |
| CreateDispose_Default | Create | 316.17 | 1,008 B |
| Dispose_Minimal | Dispose | 219.08 | 244 B |
| WithProperty_Minimal | Properties | 148.74 | 512 B |
| WithTag_Minimal | Tags | 210.55 | 368 B |

### SamplingBenchmarks

| Method | Category | Mean (ns) | Allocated |
| --- | --- | ---: | ---: |
| Adaptive_AdjustmentTriggered | Adjustment | 978.75 | 736 B |
| Probabilistic_ShouldSample | Decision | 293.29 | 1 B |
| Conditional_ShouldSample_ErrorTag | Decision | 106.54 | 49 B |
| PerSource_ShouldSample | Decision | 280.09 | 1 B |
| Adaptive_ShouldSample | Decision | 48.98 | 1 B |
