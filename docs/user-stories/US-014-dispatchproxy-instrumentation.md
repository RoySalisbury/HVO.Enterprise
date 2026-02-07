# US-014: DispatchProxy Instrumentation

**Status**: ❌ Not Started  
**Category**: Core Package  
**Effort**: 8 story points  
**Sprint**: 6

## Description

As a **developer instrumenting service interfaces**,  
I want **attribute-based automatic instrumentation using DispatchProxy**,  
So that **I can add telemetry to all interface methods without modifying implementation code or writing boilerplate instrumentation**.

## Acceptance Criteria

1. **DispatchProxy Implementation**
   - [ ] Generic `TelemetryDispatchProxy<T>` that wraps interface methods
   - [ ] Automatic Activity creation for instrumented methods
   - [ ] Automatic timing and success/failure tracking
   - [ ] Support for sync and async methods
   - [ ] Parameter capture with configurable sensitivity

2. **Attribute-Based Configuration**
   - [ ] `[InstrumentMethod]` attribute for explicit instrumentation
   - [ ] `[InstrumentClass]` attribute for class-level instrumentation
   - [ ] `[SensitiveData]` attribute for parameter/property exclusion
   - [ ] Attribute inheritance support
   - [ ] Per-method configuration overrides

3. **Factory Integration**
   - [ ] `ITelemetryProxyFactory` for creating instrumented proxies
   - [ ] Support for constructor dependency injection
   - [ ] Integration with DI container (IServiceCollection)
   - [ ] Proxy caching for performance

4. **Parameter Capture**
   - [ ] Capture method parameters as Activity tags
   - [ ] Support primitive types, strings, collections
   - [ ] Custom serialization for complex types
   - [ ] Automatic PII detection and redaction
   - [ ] Configurable capture depth for nested objects

5. **Performance**
   - [ ] Proxy creation: <1μs
   - [ ] Method invocation overhead: <200ns
   - [ ] Async method overhead: <500ns
   - [ ] Zero allocations for non-instrumented methods

## Technical Requirements

### Core API

```csharp
namespace HVO.Enterprise.Telemetry.Instrumentation
{
    /// <summary>
    /// Marks a method for automatic instrumentation.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, Inherited = true, AllowMultiple = false)]
    public sealed class InstrumentMethodAttribute : Attribute
    {
        /// <summary>
        /// Gets or sets the operation name (defaults to method name).
        /// </summary>
        public string? OperationName { get; set; }
        
        /// <summary>
        /// Gets or sets the ActivityKind for this operation.
        /// </summary>
        public ActivityKind ActivityKind { get; set; } = ActivityKind.Internal;
        
        /// <summary>
        /// Gets or sets whether to capture method parameters.
        /// </summary>
        public bool CaptureParameters { get; set; } = true;
        
        /// <summary>
        /// Gets or sets whether to capture the return value.
        /// </summary>
        public bool CaptureReturnValue { get; set; } = false;
        
        /// <summary>
        /// Gets or sets whether to log method entry/exit.
        /// </summary>
        public bool LogEvents { get; set; } = true;
        
        /// <summary>
        /// Gets or sets the log level for method events.
        /// </summary>
        public LogLevel LogLevel { get; set; } = LogLevel.Debug;
    }
    
    /// <summary>
    /// Marks a class for automatic instrumentation of all public methods.
    /// </summary>
    [AttributeUsage(AttributeTargets.Interface | AttributeTargets.Class, Inherited = true, AllowMultiple = false)]
    public sealed class InstrumentClassAttribute : Attribute
    {
        /// <summary>
        /// Gets or sets the default operation name prefix.
        /// </summary>
        public string? OperationPrefix { get; set; }
        
        /// <summary>
        /// Gets or sets the default ActivityKind.
        /// </summary>
        public ActivityKind ActivityKind { get; set; } = ActivityKind.Internal;
        
        /// <summary>
        /// Gets or sets whether to capture parameters by default.
        /// </summary>
        public bool CaptureParameters { get; set; } = true;
        
        /// <summary>
        /// Gets or sets whether to log events by default.
        /// </summary>
        public bool LogEvents { get; set; } = true;
    }
    
    /// <summary>
    /// Marks a parameter or property as containing sensitive data that should not be captured.
    /// </summary>
    [AttributeUsage(AttributeTargets.Parameter | AttributeTargets.Property, Inherited = true, AllowMultiple = false)]
    public sealed class SensitiveDataAttribute : Attribute
    {
        /// <summary>
        /// Gets or sets the redaction strategy.
        /// </summary>
        public RedactionStrategy Strategy { get; set; } = RedactionStrategy.Mask;
    }
    
    /// <summary>
    /// Strategies for redacting sensitive data.
    /// </summary>
    public enum RedactionStrategy
    {
        /// <summary>Remove the parameter entirely.</summary>
        Remove,
        
        /// <summary>Replace with masked value.</summary>
        Mask,
        
        /// <summary>Replace with hash.</summary>
        Hash
    }
}
```

### DispatchProxy Implementation

```csharp
namespace HVO.Enterprise.Telemetry.Instrumentation
{
    using System.Reflection;
    using System.Diagnostics;
    using System.Runtime.CompilerServices;
    
    /// <summary>
    /// DispatchProxy that automatically instruments interface methods.
    /// </summary>
    /// <typeparam name="T">The interface type to instrument.</typeparam>
    public sealed class TelemetryDispatchProxy<T> : DispatchProxy where T : class
    {
        private T? _target;
        private IOperationScopeFactory? _scopeFactory;
        private ILogger? _logger;
        private InstrumentationOptions? _options;
        private readonly ConcurrentDictionary<MethodInfo, MethodInstrumentationInfo> _methodCache = new();
        
        /// <summary>
        /// Initializes the proxy with the target instance and dependencies.
        /// </summary>
        internal void Initialize(
            T target,
            IOperationScopeFactory scopeFactory,
            ILogger? logger,
            InstrumentationOptions options)
        {
            _target = target ?? throw new ArgumentNullException(nameof(target));
            _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
            _logger = logger;
            _options = options ?? new InstrumentationOptions();
        }
        
        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            if (targetMethod == null || _target == null)
                throw new InvalidOperationException("Proxy not initialized");
            
            // Get or create instrumentation info for this method
            var methodInfo = _methodCache.GetOrAdd(targetMethod, CreateMethodInfo);
            
            // If not instrumented, just invoke directly
            if (!methodInfo.IsInstrumented)
                return targetMethod.Invoke(_target, args);
            
            // Check if method returns Task or Task<T>
            var returnType = targetMethod.ReturnType;
            if (typeof(Task).IsAssignableFrom(returnType))
                return InvokeAsyncMethod(targetMethod, args, methodInfo);
            
            // Synchronous method
            return InvokeSyncMethod(targetMethod, args, methodInfo);
        }
        
        private object? InvokeSyncMethod(MethodInfo method, object?[]? args, MethodInstrumentationInfo methodInfo)
        {
            using var scope = _scopeFactory!.Begin(methodInfo.OperationName, new OperationScopeOptions
            {
                ActivityKind = methodInfo.ActivityKind,
                LogEvents = methodInfo.LogEvents,
                LogLevel = methodInfo.LogLevel
            });
            
            // Capture parameters
            if (methodInfo.CaptureParameters && args != null)
                CaptureParameters(scope, method, args);
            
            try
            {
                var result = method.Invoke(_target, args);
                
                // Capture return value
                if (methodInfo.CaptureReturnValue && result != null)
                    CaptureReturnValue(scope, result);
                
                scope.Succeed();
                return result;
            }
            catch (TargetInvocationException ex)
            {
                var innerException = ex.InnerException ?? ex;
                scope.Fail(innerException);
                throw innerException;
            }
            catch (Exception ex)
            {
                scope.Fail(ex);
                throw;
            }
        }
        
        private object InvokeAsyncMethod(MethodInfo method, object?[]? args, MethodInstrumentationInfo methodInfo)
        {
            var result = method.Invoke(_target, args);
            if (result == null)
                throw new InvalidOperationException("Async method returned null Task");
            
            var task = (Task)result;
            
            // Check if Task<T>
            var returnType = method.ReturnType;
            if (returnType.IsGenericType && returnType.GetGenericTypeDefinition() == typeof(Task<>))
            {
                var resultType = returnType.GetGenericArguments()[0];
                var wrapMethod = typeof(TelemetryDispatchProxy<T>)
                    .GetMethod(nameof(WrapTaskWithResultInternal), BindingFlags.NonPublic | BindingFlags.Instance)!
                    .MakeGenericMethod(resultType);
                return wrapMethod.Invoke(this, new object[] { task, method, args, methodInfo })!;
            }
            
            return WrapTaskInternal(task, method, args, methodInfo);
        }
        
        private async Task WrapTaskInternal(Task task, MethodInfo method, object?[]? args, MethodInstrumentationInfo methodInfo)
        {
            using var scope = _scopeFactory!.Begin(methodInfo.OperationName, new OperationScopeOptions
            {
                ActivityKind = methodInfo.ActivityKind,
                LogEvents = methodInfo.LogEvents,
                LogLevel = methodInfo.LogLevel
            });
            
            // Capture parameters
            if (methodInfo.CaptureParameters && args != null)
                CaptureParameters(scope, method, args);
            
            try
            {
                await task.ConfigureAwait(false);
                scope.Succeed();
            }
            catch (Exception ex)
            {
                scope.Fail(ex);
                throw;
            }
        }
        
        private async Task<TResult> WrapTaskWithResultInternal<TResult>(
            Task task,
            MethodInfo method,
            object?[]? args,
            MethodInstrumentationInfo methodInfo)
        {
            using var scope = _scopeFactory!.Begin(methodInfo.OperationName, new OperationScopeOptions
            {
                ActivityKind = methodInfo.ActivityKind,
                LogEvents = methodInfo.LogEvents,
                LogLevel = methodInfo.LogLevel
            });
            
            // Capture parameters
            if (methodInfo.CaptureParameters && args != null)
                CaptureParameters(scope, method, args);
            
            try
            {
                var typedTask = (Task<TResult>)task;
                var result = await typedTask.ConfigureAwait(false);
                
                // Capture return value
                if (methodInfo.CaptureReturnValue && result != null)
                    CaptureReturnValue(scope, result);
                
                scope.Succeed();
                return result;
            }
            catch (Exception ex)
            {
                scope.Fail(ex);
                throw;
            }
        }
        
        private void CaptureParameters(IOperationScope scope, MethodInfo method, object?[] args)
        {
            var parameters = method.GetParameters();
            for (int i = 0; i < parameters.Length && i < args.Length; i++)
            {
                var parameter = parameters[i];
                var value = args[i];
                
                // Check for sensitive data attribute
                if (parameter.GetCustomAttribute<SensitiveDataAttribute>() != null)
                {
                    scope.WithTag($"param.{parameter.Name}", "***");
                    continue;
                }
                
                // Capture parameter value
                if (value != null)
                {
                    var capturedValue = CaptureValue(value, _options!.MaxCaptureDepth);
                    scope.WithTag($"param.{parameter.Name}", capturedValue);
                }
            }
        }
        
        private void CaptureReturnValue(IOperationScope scope, object value)
        {
            var capturedValue = CaptureValue(value, _options!.MaxCaptureDepth);
            scope.WithTag("result", capturedValue);
        }
        
        private object? CaptureValue(object value, int maxDepth)
        {
            if (value == null) return null;
            if (maxDepth <= 0) return value.GetType().Name;
            
            var type = value.GetType();
            
            // Primitive types and strings
            if (type.IsPrimitive || type == typeof(string) || type == typeof(decimal) || 
                type == typeof(DateTime) || type == typeof(DateTimeOffset) || type == typeof(Guid))
                return value;
            
            // Collections
            if (value is System.Collections.IEnumerable enumerable and not string)
            {
                var items = new List<object?>();
                int count = 0;
                foreach (var item in enumerable)
                {
                    if (count++ >= _options!.MaxCollectionItems)
                    {
                        items.Add($"... ({count} total items)");
                        break;
                    }
                    items.Add(CaptureValue(item, maxDepth - 1));
                }
                return items;
            }
            
            // Complex objects - capture public properties
            if (_options!.CaptureComplexTypes)
            {
                var properties = new Dictionary<string, object?>();
                foreach (var prop in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
                {
                    // Check for sensitive data
                    if (prop.GetCustomAttribute<SensitiveDataAttribute>() != null)
                        continue;
                    
                    try
                    {
                        var propValue = prop.GetValue(value);
                        properties[prop.Name] = CaptureValue(propValue, maxDepth - 1);
                    }
                    catch
                    {
                        // Ignore property access failures
                    }
                }
                return properties;
            }
            
            return value.ToString();
        }
        
        private MethodInstrumentationInfo CreateMethodInfo(MethodInfo method)
        {
            // Check for method-level attribute
            var methodAttr = method.GetCustomAttribute<InstrumentMethodAttribute>();
            if (methodAttr != null)
            {
                return new MethodInstrumentationInfo
                {
                    IsInstrumented = true,
                    OperationName = methodAttr.OperationName ?? $"{typeof(T).Name}.{method.Name}",
                    ActivityKind = methodAttr.ActivityKind,
                    CaptureParameters = methodAttr.CaptureParameters,
                    CaptureReturnValue = methodAttr.CaptureReturnValue,
                    LogEvents = methodAttr.LogEvents,
                    LogLevel = methodAttr.LogLevel
                };
            }
            
            // Check for class-level attribute
            var classAttr = typeof(T).GetCustomAttribute<InstrumentClassAttribute>();
            if (classAttr != null)
            {
                var operationName = string.IsNullOrEmpty(classAttr.OperationPrefix)
                    ? $"{typeof(T).Name}.{method.Name}"
                    : $"{classAttr.OperationPrefix}.{method.Name}";
                
                return new MethodInstrumentationInfo
                {
                    IsInstrumented = true,
                    OperationName = operationName,
                    ActivityKind = classAttr.ActivityKind,
                    CaptureParameters = classAttr.CaptureParameters,
                    CaptureReturnValue = false,
                    LogEvents = classAttr.LogEvents,
                    LogLevel = LogLevel.Debug
                };
            }
            
            // Not instrumented
            return new MethodInstrumentationInfo { IsInstrumented = false };
        }
        
        private sealed class MethodInstrumentationInfo
        {
            public bool IsInstrumented { get; set; }
            public string OperationName { get; set; } = string.Empty;
            public ActivityKind ActivityKind { get; set; }
            public bool CaptureParameters { get; set; }
            public bool CaptureReturnValue { get; set; }
            public bool LogEvents { get; set; }
            public LogLevel LogLevel { get; set; }
        }
    }
    
    /// <summary>
    /// Options for configuring instrumentation behavior.
    /// </summary>
    public sealed class InstrumentationOptions
    {
        /// <summary>
        /// Maximum depth for capturing nested objects.
        /// </summary>
        public int MaxCaptureDepth { get; set; } = 2;
        
        /// <summary>
        /// Maximum number of items to capture from collections.
        /// </summary>
        public int MaxCollectionItems { get; set; } = 10;
        
        /// <summary>
        /// Whether to capture complex types (serialize properties).
        /// </summary>
        public bool CaptureComplexTypes { get; set; } = true;
        
        /// <summary>
        /// Whether to automatically detect and redact PII.
        /// </summary>
        public bool AutoDetectPii { get; set; } = true;
    }
}
```

### Factory for Creating Proxies

```csharp
namespace HVO.Enterprise.Telemetry.Instrumentation
{
    /// <summary>
    /// Factory for creating instrumented proxy instances.
    /// </summary>
    public interface ITelemetryProxyFactory
    {
        /// <summary>
        /// Creates an instrumented proxy for the specified interface.
        /// </summary>
        T CreateProxy<T>(T target, InstrumentationOptions? options = null) where T : class;
    }
    
    /// <summary>
    /// Default implementation of ITelemetryProxyFactory.
    /// </summary>
    public sealed class TelemetryProxyFactory : ITelemetryProxyFactory
    {
        private readonly IOperationScopeFactory _scopeFactory;
        private readonly ILoggerFactory? _loggerFactory;
        
        public TelemetryProxyFactory(
            IOperationScopeFactory scopeFactory,
            ILoggerFactory? loggerFactory = null)
        {
            _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
            _loggerFactory = loggerFactory;
        }
        
        public T CreateProxy<T>(T target, InstrumentationOptions? options = null) where T : class
        {
            if (target == null) throw new ArgumentNullException(nameof(target));
            if (!typeof(T).IsInterface)
                throw new ArgumentException($"Type {typeof(T).Name} must be an interface", nameof(T));
            
            var proxy = DispatchProxy.Create<T, TelemetryDispatchProxy<T>>() as TelemetryDispatchProxy<T>;
            if (proxy == null)
                throw new InvalidOperationException("Failed to create proxy");
            
            var logger = _loggerFactory?.CreateLogger(typeof(T).Name);
            
            proxy.Initialize(
                target,
                _scopeFactory,
                logger,
                options ?? new InstrumentationOptions());
            
            return (proxy as T)!;
        }
    }
}
```

### DI Extension Methods

```csharp
namespace Microsoft.Extensions.DependencyInjection
{
    using HVO.Enterprise.Telemetry.Instrumentation;
    
    /// <summary>
    /// Extension methods for registering instrumented services.
    /// </summary>
    public static class TelemetryInstrumentationExtensions
    {
        /// <summary>
        /// Adds the telemetry proxy factory to the service collection.
        /// </summary>
        public static IServiceCollection AddTelemetryProxyFactory(this IServiceCollection services)
        {
            if (services == null) throw new ArgumentNullException(nameof(services));
            
            services.AddSingleton<ITelemetryProxyFactory, TelemetryProxyFactory>();
            return services;
        }
        
        /// <summary>
        /// Registers an instrumented service.
        /// </summary>
        public static IServiceCollection AddInstrumentedTransient<TInterface, TImplementation>(
            this IServiceCollection services,
            InstrumentationOptions? options = null)
            where TInterface : class
            where TImplementation : class, TInterface
        {
            if (services == null) throw new ArgumentNullException(nameof(services));
            
            services.AddTransient<TImplementation>();
            services.AddTransient<TInterface>(sp =>
            {
                var implementation = sp.GetRequiredService<TImplementation>();
                var factory = sp.GetRequiredService<ITelemetryProxyFactory>();
                return factory.CreateProxy<TInterface>(implementation, options);
            });
            
            return services;
        }
        
        /// <summary>
        /// Registers an instrumented singleton service.
        /// </summary>
        public static IServiceCollection AddInstrumentedSingleton<TInterface, TImplementation>(
            this IServiceCollection services,
            InstrumentationOptions? options = null)
            where TInterface : class
            where TImplementation : class, TInterface
        {
            if (services == null) throw new ArgumentNullException(nameof(services));
            
            services.AddSingleton<TImplementation>();
            services.AddSingleton<TInterface>(sp =>
            {
                var implementation = sp.GetRequiredService<TImplementation>();
                var factory = sp.GetRequiredService<ITelemetryProxyFactory>();
                return factory.CreateProxy<TInterface>(implementation, options);
            });
            
            return services;
        }
        
        /// <summary>
        /// Registers an instrumented scoped service.
        /// </summary>
        public static IServiceCollection AddInstrumentedScoped<TInterface, TImplementation>(
            this IServiceCollection services,
            InstrumentationOptions? options = null)
            where TInterface : class
            where TImplementation : class, TInterface
        {
            if (services == null) throw new ArgumentNullException(nameof(services));
            
            services.AddScoped<TImplementation>();
            services.AddScoped<TInterface>(sp =>
            {
                var implementation = sp.GetRequiredService<TImplementation>();
                var factory = sp.GetRequiredService<ITelemetryProxyFactory>();
                return factory.CreateProxy<TInterface>(implementation, options);
            });
            
            return services;
        }
    }
}
```

### Usage Examples

```csharp
// Example 1: Interface with method-level instrumentation
[InstrumentClass(OperationPrefix = "OrderService")]
public interface IOrderService
{
    [InstrumentMethod(CaptureParameters = true, CaptureReturnValue = true)]
    Task<Order> GetOrderAsync(int orderId);
    
    [InstrumentMethod(ActivityKind = ActivityKind.Internal)]
    Task ProcessOrderAsync(Order order);
    
    // Not instrumented (no attribute)
    void LogInternalState();
}

// Example 2: Parameter sensitivity
public interface IPaymentService
{
    Task<PaymentResult> ProcessPaymentAsync(
        int orderId,
        [SensitiveData] string creditCardNumber,
        [SensitiveData] string cvv);
}

// Example 3: DI registration
public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddTelemetry();
        services.AddTelemetryProxyFactory();
        
        // Register instrumented service
        services.AddInstrumentedScoped<IOrderService, OrderService>();
        services.AddInstrumentedSingleton<IPaymentService, PaymentService>();
    }
}

// Example 4: Manual proxy creation
var orderService = new OrderService();
var proxyFactory = serviceProvider.GetRequiredService<ITelemetryProxyFactory>();
var instrumentedService = proxyFactory.CreateProxy<IOrderService>(orderService);

// All calls to instrumentedService are automatically instrumented
var order = await instrumentedService.GetOrderAsync(12345);

// Example 5: Custom options
var options = new InstrumentationOptions
{
    MaxCaptureDepth = 3,
    MaxCollectionItems = 20,
    CaptureComplexTypes = true,
    AutoDetectPii = true
};

services.AddInstrumentedScoped<IOrderService, OrderService>(options);

// Example 6: Class-level instrumentation
[InstrumentClass(
    OperationPrefix = "CustomerService",
    ActivityKind = ActivityKind.Server,
    CaptureParameters = true)]
public interface ICustomerService
{
    // All methods automatically instrumented
    Task<Customer> GetCustomerAsync(int id);
    Task UpdateCustomerAsync(Customer customer);
    Task DeleteCustomerAsync(int id);
}
```

## Testing Requirements

### Unit Tests

1. **Proxy Creation Tests**
   ```csharp
   [Fact]
   public void ProxyFactory_CreatesValidProxy()
   {
       var service = new TestService();
       var proxy = _factory.CreateProxy<ITestService>(service);
       
       Assert.NotNull(proxy);
       Assert.IsAssignableFrom<ITestService>(proxy);
   }
   
   [Fact]
   public void ProxyFactory_ThrowsForNonInterface()
   {
       var service = new TestService();
       Assert.Throws<ArgumentException>(() => _factory.CreateProxy(service));
   }
   ```

2. **Instrumentation Tests**
   ```csharp
   [Fact]
   public async Task Proxy_CreatesActivityForInstrumentedMethod()
   {
       var service = new TestService();
       var proxy = _factory.CreateProxy<ITestService>(service);
       
       Activity? capturedActivity = null;
       using var listener = new ActivityListener
       {
           ShouldListenTo = _ => true,
           Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
           ActivityStarted = activity => capturedActivity = activity
       };
       ActivitySource.AddActivityListener(listener);
       
       await proxy.DoSomethingAsync();
       
       Assert.NotNull(capturedActivity);
       Assert.Equal("TestService.DoSomethingAsync", capturedActivity.OperationName);
   }
   ```

3. **Parameter Capture Tests**
   ```csharp
   [Fact]
   public async Task Proxy_CapturesMethodParameters()
   {
       var service = new TestService();
       var proxy = _factory.CreateProxy<ITestService>(service);
       
       await proxy.ProcessOrderAsync(12345, "Test");
       
       var activity = Activity.Current;
       Assert.Equal(12345, activity?.GetTagItem("param.orderId"));
       Assert.Equal("Test", activity?.GetTagItem("param.name"));
   }
   
   [Fact]
   public async Task Proxy_RedactsSensitiveParameters()
   {
       var service = new TestService();
       var proxy = _factory.CreateProxy<ITestService>(service);
       
       await proxy.ProcessPaymentAsync(12345, "4111111111111111");
       
       var activity = Activity.Current;
       Assert.Equal("***", activity?.GetTagItem("param.creditCard"));
   }
   ```

4. **Async Method Tests**
   ```csharp
   [Fact]
   public async Task Proxy_HandlesAsyncMethodsCorrectly()
   {
       var service = new TestService();
       var proxy = _factory.CreateProxy<ITestService>(service);
       
       var result = await proxy.GetDataAsync();
       
       Assert.NotNull(result);
       Assert.Equal("test data", result);
   }
   
   [Fact]
   public async Task Proxy_CapturesExceptionsInAsyncMethods()
   {
       var service = new TestService();
       var proxy = _factory.CreateProxy<ITestService>(service);
       
       await Assert.ThrowsAsync<InvalidOperationException>(() => 
           proxy.ThrowExceptionAsync());
       
       var activity = Activity.Current;
       Assert.Equal(ActivityStatusCode.Error, activity?.Status);
   }
   ```

### Performance Tests

```csharp
[Benchmark]
public void ProxyCreation()
{
    var service = new TestService();
    _factory.CreateProxy<ITestService>(service);
}

[Benchmark]
public void ProxyInvocation_NoInstrumentation()
{
    _proxy.NonInstrumentedMethod();
}

[Benchmark]
public void ProxyInvocation_WithInstrumentation()
{
    _proxy.InstrumentedMethod();
}

[Benchmark]
public async Task ProxyInvocation_AsyncWithInstrumentation()
{
    await _proxy.InstrumentedMethodAsync();
}
```

### Integration Tests

1. **DI Integration**
   - [ ] Service registered correctly
   - [ ] Proxy resolves from container
   - [ ] Dependencies injected properly

2. **Real-World Scenarios**
   - [ ] Database operations instrumented
   - [ ] HTTP client calls instrumented
   - [ ] Background jobs instrumented

## Performance Requirements

- **Proxy creation**: <1μs
- **Method invocation overhead (non-instrumented)**: <50ns
- **Method invocation overhead (instrumented)**: <200ns
- **Async method overhead**: <500ns
- **Parameter capture**: <100ns per parameter
- **Memory allocation**: <1KB per instrumented call

## Dependencies

**Blocked By**: 
- US-001 (Core Package Setup)
- US-002 (Auto-Managed Correlation)
- US-012 (Operation Scope)
- US-015 (Parameter Capture)

**Blocks**: None

## Definition of Done

- [ ] `TelemetryDispatchProxy<T>` implementation complete
- [ ] `ITelemetryProxyFactory` implementation complete
- [ ] Attribute classes implemented
- [ ] DI extension methods
- [ ] Parameter capture with sensitivity handling
- [ ] All unit tests passing (>85% coverage)
- [ ] Performance benchmarks meet requirements
- [ ] Integration tests with DI
- [ ] XML documentation complete
- [ ] Code reviewed and approved
- [ ] Zero warnings in build

## Notes

### Design Decisions

1. **Why DispatchProxy?**
   - Built-in .NET framework (no dependencies)
   - Dynamic proxy generation at runtime
   - Works with interfaces (common pattern)
   - Supports async/await naturally

2. **Why attribute-based configuration?**
   - Declarative and self-documenting
   - Minimal code changes to existing interfaces
   - Easy to override per method
   - Supports inheritance

3. **Why cache MethodInstrumentationInfo?**
   - Method reflection is expensive
   - Attributes don't change at runtime
   - Significant performance improvement
   - Small memory overhead

4. **Why separate handling for sync/async?**
   - Different return types (object vs Task)
   - Need to await async methods properly
   - Proper exception propagation
   - Activity scope must span entire async operation

### Implementation Tips

- Use `ConcurrentDictionary` for method info cache
- Consider using `ConditionalWeakTable` for proxy caching
- Add `[MethodImpl(MethodImplOptions.AggressiveInlining)]` for hot paths
- Test with both sync and async methods thoroughly
- Handle `ValueTask` and `ValueTask<T>` if needed

### Common Pitfalls

- Don't forget to unwrap `TargetInvocationException`
- Ensure Activity scope spans entire async operation
- Watch for deadlocks when calling async methods synchronously
- Be careful with capturing large objects (memory leak)
- Test with generic methods and constraints

### Advanced Patterns

1. **Conditional Instrumentation**
   ```csharp
   public interface IFeatureFlags
   {
       bool IsEnabled(string feature);
   }
   
   // Only instrument when feature is enabled
   var shouldInstrument = featureFlags.IsEnabled("DetailedTelemetry");
   ```

2. **Custom Serializers**
   ```csharp
   public interface IParameterSerializer
   {
       object? Serialize(object? value, Type type);
   }
   ```

3. **Result<T> Integration**
   ```csharp
   [InstrumentMethod]
   Task<Result<Order>> GetOrderAsync(int orderId);
   
   // Automatically mark as failed if Result.IsFailure
   ```

## Related Documentation

- [Project Plan](../project-plan.md#14-implement-dispatchproxy-instrumentation)
- [DispatchProxy Documentation](https://learn.microsoft.com/en-us/dotnet/api/system.reflection.dispatchproxy)
- [Dynamic Proxy Patterns](https://learn.microsoft.com/en-us/dotnet/standard/design-guidelines/proxy-pattern)
