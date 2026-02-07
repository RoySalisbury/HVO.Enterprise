# US-024: Application Insights Extension Package

**Status**: ❌ Not Started  
**Category**: Extension Package  
**Effort**: 5 story points  
**Sprint**: 8

## Description

As a **developer using Azure Application Insights**,  
I want **seamless integration between HVO telemetry and Application Insights with dual-mode support**,  
So that **my OpenTelemetry traces, metrics, and logs appear in Application Insights without manual bridging**.

## Acceptance Criteria

1. **Package Structure**
   - [ ] `HVO.Enterprise.Telemetry.AppInsights.csproj` created targeting `netstandard2.0`
   - [ ] Package builds with zero warnings
   - [ ] Dependencies: ApplicationInsights SDK + HVO.Enterprise.Telemetry

2. **Dual-Mode Bridge Support**
   - [ ] Detects if OpenTelemetry exporters are configured (OTLP mode)
   - [ ] Falls back to ApplicationInsights SDK directly (.NET Framework 4.8 mode)
   - [ ] Both modes work without code changes
   - [ ] Runtime mode detection based on environment/configuration

3. **Telemetry Initializers**
   - [ ] `ActivityTelemetryInitializer` propagates W3C TraceContext
   - [ ] `CorrelationTelemetryInitializer` adds correlation ID
   - [ ] `OperationTelemetryInitializer` enriches with operation scope data
   - [ ] All initializers thread-safe and AsyncLocal-aware

4. **Configuration Extensions**
   - [ ] `IServiceCollection.AddApplicationInsightsTelemetry()` extension
   - [ ] `TelemetryConfiguration.AddHvoEnrichers()` extension
   - [ ] Fluent API for configuring bridge mode
   - [ ] Support for both connection string and instrumentation key

5. **Metric and Trace Export**
   - [ ] Activity/spans exported as Request/Dependency telemetry
   - [ ] Metrics exported as MetricTelemetry
   - [ ] Logs correlated with traces via operation_Id
   - [ ] Custom properties preserved from Activity tags

6. **Cross-Platform Support**
   - [ ] Works on .NET Framework 4.8 (direct SDK integration)
   - [ ] Works on .NET 8+ (OTLP + ApplicationInsights exporter)
   - [ ] Automatic mode detection without explicit configuration

## Technical Requirements

### Package Configuration

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>netstandard2.0</TargetFramework>
    <LangVersion>latest</LangVersion>
    <Nullable>enable</Nullable>
    <ImplicitUsings>disable</ImplicitUsings>
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
    
    <!-- Package Information -->
    <PackageId>HVO.Enterprise.Telemetry.AppInsights</PackageId>
    <Version>1.0.0-preview.1</Version>
    <Authors>HVO Enterprise</Authors>
    <Description>Application Insights integration for HVO.Enterprise.Telemetry with dual-mode support</Description>
    <PackageTags>telemetry;applicationinsights;azure;tracing;metrics;opentelemetry</PackageTags>
    <RepositoryUrl>https://github.com/RoySalisbury/HVO.Enterprise</RepositoryUrl>
    <PackageLicenseExpression>MIT</PackageLicenseExpression>
    
    <GenerateDocumentationFile>true</GenerateDocumentationFile>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.ApplicationInsights" Version="2.22.0" />
    <PackageReference Include="Microsoft.Extensions.DependencyInjection.Abstractions" Version="8.0.0" />
    <ProjectReference Include="..\HVO.Enterprise.Telemetry\HVO.Enterprise.Telemetry.csproj" />
  </ItemGroup>
</Project>
```

### ActivityTelemetryInitializer Implementation

```csharp
using System;
using System.Diagnostics;
using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;

namespace HVO.Enterprise.Telemetry.AppInsights
{
    /// <summary>
    /// Initializes Application Insights telemetry with Activity tracing context.
    /// </summary>
    /// <remarks>
    /// Propagates W3C TraceContext from <see cref="Activity.Current"/> to Application Insights telemetry,
    /// ensuring proper distributed tracing correlation.
    /// </remarks>
    public sealed class ActivityTelemetryInitializer : ITelemetryInitializer
    {
        /// <inheritdoc />
        public void Initialize(ITelemetry telemetry)
        {
            if (telemetry == null)
            {
                throw new ArgumentNullException(nameof(telemetry));
            }

            var activity = Activity.Current;
            if (activity == null)
            {
                return;
            }

            // Set operation context from Activity
            var operationTelemetry = telemetry as OperationTelemetry;
            if (operationTelemetry != null && activity.IdFormat == ActivityIdFormat.W3C)
            {
                // W3C TraceContext format
                var traceId = activity.TraceId.ToString();
                var spanId = activity.SpanId.ToString();

                if (!string.IsNullOrEmpty(traceId) && traceId != "00000000000000000000000000000000")
                {
                    operationTelemetry.Context.Operation.Id = traceId;
                }

                if (!string.IsNullOrEmpty(spanId) && spanId != "0000000000000000")
                {
                    operationTelemetry.Id = spanId;
                }

                if (activity.ParentSpanId != default)
                {
                    operationTelemetry.Context.Operation.ParentId = activity.ParentSpanId.ToString();
                }

                // Add W3C trace state if present
                if (!string.IsNullOrEmpty(activity.TraceStateString))
                {
                    operationTelemetry.Properties["tracestate"] = activity.TraceStateString;
                }
            }
            else if (operationTelemetry != null && !string.IsNullOrEmpty(activity.Id))
            {
                // Hierarchical format fallback
                operationTelemetry.Context.Operation.Id = activity.RootId ?? activity.Id;
                operationTelemetry.Id = activity.Id;

                if (!string.IsNullOrEmpty(activity.ParentId))
                {
                    operationTelemetry.Context.Operation.ParentId = activity.ParentId;
                }
            }

            // Copy Activity tags to custom properties
            foreach (var tag in activity.Tags)
            {
                if (!string.IsNullOrEmpty(tag.Key) && !telemetry.Context.Properties.ContainsKey(tag.Key))
                {
                    telemetry.Context.Properties[tag.Key] = tag.Value ?? string.Empty;
                }
            }

            // Copy Activity baggage
            foreach (var baggage in activity.Baggage)
            {
                var key = $"baggage.{baggage.Key}";
                if (!telemetry.Context.Properties.ContainsKey(key))
                {
                    telemetry.Context.Properties[key] = baggage.Value ?? string.Empty;
                }
            }
        }
    }
}
```

### CorrelationTelemetryInitializer Implementation

```csharp
using System;
using System.Diagnostics;
using HVO.Enterprise.Telemetry.Correlation;
using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.Extensibility;

namespace HVO.Enterprise.Telemetry.AppInsights
{
    /// <summary>
    /// Initializes Application Insights telemetry with correlation ID from <see cref="CorrelationContext"/>.
    /// </summary>
    public sealed class CorrelationTelemetryInitializer : ITelemetryInitializer
    {
        private const string CorrelationIdKey = "CorrelationId";

        /// <inheritdoc />
        public void Initialize(ITelemetry telemetry)
        {
            if (telemetry == null)
            {
                throw new ArgumentNullException(nameof(telemetry));
            }

            var correlationId = CorrelationContext.Current.CorrelationId;

            // Fallback to Activity if no explicit correlation
            if (string.IsNullOrEmpty(correlationId))
            {
                var activity = Activity.Current;
                if (activity != null && activity.IdFormat == ActivityIdFormat.W3C)
                {
                    correlationId = activity.TraceId.ToString();
                }
                else if (activity != null)
                {
                    correlationId = activity.RootId ?? activity.Id;
                }
            }

            if (!string.IsNullOrEmpty(correlationId) && 
                !telemetry.Context.Properties.ContainsKey(CorrelationIdKey))
            {
                telemetry.Context.Properties[CorrelationIdKey] = correlationId;
            }
        }
    }
}
```

### OperationTelemetryInitializer Implementation

```csharp
using System;
using HVO.Enterprise.Telemetry.Operations;
using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;

namespace HVO.Enterprise.Telemetry.AppInsights
{
    /// <summary>
    /// Initializes Application Insights telemetry with operation scope context.
    /// </summary>
    /// <remarks>
    /// Enriches telemetry with properties captured in <see cref="IOperationScope"/>.
    /// </remarks>
    public sealed class OperationTelemetryInitializer : ITelemetryInitializer
    {
        /// <inheritdoc />
        public void Initialize(ITelemetry telemetry)
        {
            if (telemetry == null)
            {
                throw new ArgumentNullException(nameof(telemetry));
            }

            var operationScope = OperationScope.Current;
            if (operationScope == null)
            {
                return;
            }

            // Set operation name if available
            if (!string.IsNullOrEmpty(operationScope.OperationName))
            {
                telemetry.Context.Operation.Name = operationScope.OperationName;
            }

            // Copy operation properties to telemetry
            foreach (var property in operationScope.Properties)
            {
                if (!string.IsNullOrEmpty(property.Key) && 
                    !telemetry.Context.Properties.ContainsKey(property.Key))
                {
                    telemetry.Context.Properties[property.Key] = property.Value?.ToString() ?? string.Empty;
                }
            }

            // Add operation timing information
            if (operationScope.StartTime != default)
            {
                var operationTelemetry = telemetry as OperationTelemetry;
                if (operationTelemetry != null)
                {
                    operationTelemetry.Timestamp = operationScope.StartTime;
                    
                    if (operationScope.IsCompleted)
                    {
                        operationTelemetry.Duration = operationScope.Duration;
                    }
                }
            }
        }
    }
}
```

### Dual-Mode Bridge Implementation

```csharp
using System;
using System.Diagnostics;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.Extensions.Logging;
using HVO.Enterprise.Telemetry.Correlation;

namespace HVO.Enterprise.Telemetry.AppInsights
{
    /// <summary>
    /// Bridge between HVO telemetry and Application Insights with dual-mode support.
    /// </summary>
    /// <remarks>
    /// Supports two modes:
    /// 1. OTLP Mode: OpenTelemetry SDK exports to Application Insights via OTLP exporter
    /// 2. Direct Mode: Direct ApplicationInsights SDK integration for .NET Framework 4.8
    /// </remarks>
    public sealed class ApplicationInsightsBridge : IDisposable
    {
        private readonly TelemetryClient _telemetryClient;
        private readonly ILogger<ApplicationInsightsBridge>? _logger;
        private readonly bool _isOtlpMode;

        /// <summary>
        /// Initializes a new instance of the <see cref="ApplicationInsightsBridge"/> class.
        /// </summary>
        /// <param name="telemetryClient">Application Insights telemetry client</param>
        /// <param name="logger">Optional logger for diagnostics</param>
        public ApplicationInsightsBridge(
            TelemetryClient telemetryClient,
            ILogger<ApplicationInsightsBridge>? logger = null)
        {
            _telemetryClient = telemetryClient ?? throw new ArgumentNullException(nameof(telemetryClient));
            _logger = logger;
            _isOtlpMode = DetectOtlpMode();

            _logger?.LogInformation(
                "ApplicationInsightsBridge initialized in {Mode} mode",
                _isOtlpMode ? "OTLP" : "Direct");
        }

        /// <summary>
        /// Gets a value indicating whether OTLP mode is active.
        /// </summary>
        public bool IsOtlpMode => _isOtlpMode;

        /// <summary>
        /// Tracks a request (incoming operation).
        /// </summary>
        /// <remarks>
        /// In OTLP mode, this is a no-op (OpenTelemetry handles export).
        /// In Direct mode, explicitly sends to Application Insights.
        /// </remarks>
        public void TrackRequest(
            string name,
            DateTimeOffset timestamp,
            TimeSpan duration,
            string responseCode,
            bool success)
        {
            if (_isOtlpMode)
            {
                // OTLP mode: OpenTelemetry SDK handles export
                return;
            }

            // Direct mode: Send to Application Insights
            var request = new RequestTelemetry
            {
                Name = name,
                Timestamp = timestamp,
                Duration = duration,
                ResponseCode = responseCode,
                Success = success
            };

            EnrichFromContext(request);
            _telemetryClient.TrackRequest(request);
        }

        /// <summary>
        /// Tracks a dependency (outgoing operation).
        /// </summary>
        public void TrackDependency(
            string dependencyType,
            string target,
            string name,
            string data,
            DateTimeOffset timestamp,
            TimeSpan duration,
            string resultCode,
            bool success)
        {
            if (_isOtlpMode)
            {
                return;
            }

            var dependency = new DependencyTelemetry
            {
                Type = dependencyType,
                Target = target,
                Name = name,
                Data = data,
                Timestamp = timestamp,
                Duration = duration,
                ResultCode = resultCode,
                Success = success
            };

            EnrichFromContext(dependency);
            _telemetryClient.TrackDependency(dependency);
        }

        /// <summary>
        /// Tracks a metric.
        /// </summary>
        public void TrackMetric(string name, double value)
        {
            if (_isOtlpMode)
            {
                return;
            }

            _telemetryClient.TrackMetric(name, value);
        }

        /// <summary>
        /// Tracks an exception.
        /// </summary>
        public void TrackException(Exception exception)
        {
            if (exception == null)
            {
                throw new ArgumentNullException(nameof(exception));
            }

            if (_isOtlpMode)
            {
                return;
            }

            var telemetry = new ExceptionTelemetry(exception);
            EnrichFromContext(telemetry);
            _telemetryClient.TrackException(telemetry);
        }

        /// <summary>
        /// Flushes buffered telemetry.
        /// </summary>
        public void Flush()
        {
            _telemetryClient.Flush();
        }

        /// <inheritdoc />
        public void Dispose()
        {
            Flush();
        }

        private bool DetectOtlpMode()
        {
            // Check if OpenTelemetry SDK is configured
            // In OTLP mode, we don't need to manually track telemetry
            var otlpEndpoint = Environment.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT");
            var hasOpenTelemetry = !string.IsNullOrEmpty(otlpEndpoint) || 
                                   ActivitySource.Current != null;

            return hasOpenTelemetry;
        }

        private void EnrichFromContext(ITelemetry telemetry)
        {
            // Add correlation ID
            var correlationId = CorrelationContext.Current.CorrelationId;
            if (!string.IsNullOrEmpty(correlationId))
            {
                telemetry.Context.Properties["CorrelationId"] = correlationId;
            }

            // Add Activity context
            var activity = Activity.Current;
            if (activity != null)
            {
                foreach (var tag in activity.Tags)
                {
                    if (!string.IsNullOrEmpty(tag.Key) && 
                        !telemetry.Context.Properties.ContainsKey(tag.Key))
                    {
                        telemetry.Context.Properties[tag.Key] = tag.Value ?? string.Empty;
                    }
                }
            }
        }
    }
}
```

### Configuration Extensions

```csharp
using System;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace HVO.Enterprise.Telemetry.AppInsights
{
    /// <summary>
    /// Extension methods for configuring Application Insights integration.
    /// </summary>
    public static class ApplicationInsightsExtensions
    {
        /// <summary>
        /// Adds HVO telemetry enrichers to Application Insights configuration.
        /// </summary>
        /// <param name="configuration">Application Insights telemetry configuration</param>
        /// <returns>The configuration for method chaining</returns>
        public static TelemetryConfiguration AddHvoEnrichers(
            this TelemetryConfiguration configuration)
        {
            if (configuration == null)
            {
                throw new ArgumentNullException(nameof(configuration));
            }

            configuration.TelemetryInitializers.Add(new ActivityTelemetryInitializer());
            configuration.TelemetryInitializers.Add(new CorrelationTelemetryInitializer());
            configuration.TelemetryInitializers.Add(new OperationTelemetryInitializer());

            return configuration;
        }

        /// <summary>
        /// Adds Application Insights telemetry with HVO enrichment.
        /// </summary>
        /// <param name="services">Service collection</param>
        /// <param name="connectionString">Application Insights connection string</param>
        /// <returns>Service collection for method chaining</returns>
        public static IServiceCollection AddApplicationInsightsTelemetry(
            this IServiceCollection services,
            string connectionString)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }
            if (string.IsNullOrEmpty(connectionString))
            {
                throw new ArgumentNullException(nameof(connectionString));
            }

            // Add Application Insights
            services.AddApplicationInsightsTelemetry(options =>
            {
                options.ConnectionString = connectionString;
            });

            // Configure with HVO enrichers
            services.ConfigureTelemetryModule<TelemetryConfiguration>(config =>
            {
                config.AddHvoEnrichers();
            });

            // Register bridge
            services.AddSingleton<ApplicationInsightsBridge>();

            return services;
        }

        /// <summary>
        /// Adds Application Insights telemetry with HVO enrichment using instrumentation key.
        /// </summary>
        /// <param name="services">Service collection</param>
        /// <param name="instrumentationKey">Application Insights instrumentation key</param>
        /// <returns>Service collection for method chaining</returns>
        public static IServiceCollection AddApplicationInsightsTelemetryWithKey(
            this IServiceCollection services,
            string instrumentationKey)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }
            if (string.IsNullOrEmpty(instrumentationKey))
            {
                throw new ArgumentNullException(nameof(instrumentationKey));
            }

            // Convert instrumentation key to connection string format
            var connectionString = $"InstrumentationKey={instrumentationKey}";
            return services.AddApplicationInsightsTelemetry(connectionString);
        }
    }
}
```

### Usage Examples

#### ASP.NET Core with Dual-Mode Support

```csharp
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using HVO.Enterprise.Telemetry;
using HVO.Enterprise.Telemetry.AppInsights;

var builder = WebApplication.CreateBuilder(args);

// Add HVO telemetry
builder.Services.AddTelemetry(options =>
{
    options.ServiceName = "MyService";
});

// Add Application Insights with HVO enrichment
builder.Services.AddApplicationInsightsTelemetry(
    builder.Configuration["ApplicationInsights:ConnectionString"]);

var app = builder.Build();

app.MapGet("/", () => "Hello World");

app.Run();
```

#### .NET Framework 4.8 Direct Mode

```csharp
using System.Web.Http;
using Microsoft.ApplicationInsights.Extensibility;
using HVO.Enterprise.Telemetry;
using HVO.Enterprise.Telemetry.AppInsights;

public class WebApiApplication : System.Web.HttpApplication
{
    protected void Application_Start()
    {
        // Initialize HVO telemetry
        Telemetry.Initialize(options =>
        {
            options.ServiceName = "LegacyWebAPI";
        });

        // Configure Application Insights with HVO enrichers
        TelemetryConfiguration.Active.AddHvoEnrichers();
        TelemetryConfiguration.Active.InstrumentationKey = 
            ConfigurationManager.AppSettings["ApplicationInsights:InstrumentationKey"];

        GlobalConfiguration.Configure(WebApiConfig.Register);
    }

    protected void Application_End()
    {
        Telemetry.Shutdown();
    }
}
```

#### Manual Bridge Usage

```csharp
using System;
using Microsoft.ApplicationInsights;
using HVO.Enterprise.Telemetry.AppInsights;

public class OrderService
{
    private readonly ApplicationInsightsBridge _bridge;

    public OrderService(ApplicationInsightsBridge bridge)
    {
        _bridge = bridge;
    }

    public void ProcessOrder(Order order)
    {
        var start = DateTimeOffset.UtcNow;
        var success = false;

        try
        {
            // Process order logic
            success = true;
        }
        catch (Exception ex)
        {
            _bridge.TrackException(ex);
            throw;
        }
        finally
        {
            var duration = DateTimeOffset.UtcNow - start;
            _bridge.TrackRequest(
                "ProcessOrder",
                start,
                duration,
                success ? "200" : "500",
                success);
        }
    }
}
```

## Testing Requirements

### Unit Tests

```csharp
using System;
using System.Diagnostics;
using Microsoft.ApplicationInsights.DataContracts;
using HVO.Enterprise.Telemetry.AppInsights;
using HVO.Enterprise.Telemetry.Correlation;
using Xunit;

namespace HVO.Enterprise.Telemetry.AppInsights.Tests
{
    public class ActivityTelemetryInitializerTests
    {
        [Fact]
        public void Initialize_WithW3CActivity_SetsOperationContext()
        {
            // Arrange
            using var activity = new Activity("test")
                .SetIdFormat(ActivityIdFormat.W3C)
                .Start();

            var initializer = new ActivityTelemetryInitializer();
            var telemetry = new RequestTelemetry();

            // Act
            initializer.Initialize(telemetry);

            // Assert
            Assert.Equal(activity.TraceId.ToString(), telemetry.Context.Operation.Id);
            Assert.Equal(activity.SpanId.ToString(), telemetry.Id);
        }

        [Fact]
        public void Initialize_WithActivityTags_CopiesTagsToProperties()
        {
            // Arrange
            using var activity = new Activity("test")
                .SetIdFormat(ActivityIdFormat.W3C)
                .Start();
            activity.SetTag("user.id", "user-123");
            activity.SetTag("tenant.id", "tenant-456");

            var initializer = new ActivityTelemetryInitializer();
            var telemetry = new RequestTelemetry();

            // Act
            initializer.Initialize(telemetry);

            // Assert
            Assert.Equal("user-123", telemetry.Context.Properties["user.id"]);
            Assert.Equal("tenant-456", telemetry.Context.Properties["tenant.id"]);
        }

        [Fact]
        public void Initialize_WithBaggage_CopiesBaggageToProperties()
        {
            // Arrange
            using var activity = new Activity("test")
                .SetIdFormat(ActivityIdFormat.W3C)
                .Start();
            activity.AddBaggage("correlation-context", "ctx-789");

            var initializer = new ActivityTelemetryInitializer();
            var telemetry = new RequestTelemetry();

            // Act
            initializer.Initialize(telemetry);

            // Assert
            Assert.Equal("ctx-789", telemetry.Context.Properties["baggage.correlation-context"]);
        }

        [Fact]
        public void Initialize_WithNoActivity_DoesNotThrow()
        {
            // Arrange
            var initializer = new ActivityTelemetryInitializer();
            var telemetry = new RequestTelemetry();

            // Act & Assert
            initializer.Initialize(telemetry); // Should not throw
        }
    }

    public class CorrelationTelemetryInitializerTests
    {
        [Fact]
        public void Initialize_WithCorrelationContext_AddsCorrelationId()
        {
            // Arrange
            using var scope = CorrelationContext.BeginScope("corr-123");
            var initializer = new CorrelationTelemetryInitializer();
            var telemetry = new RequestTelemetry();

            // Act
            initializer.Initialize(telemetry);

            // Assert
            Assert.Equal("corr-123", telemetry.Context.Properties["CorrelationId"]);
        }

        [Fact]
        public void Initialize_WithNoCorrelation_FallsBackToActivity()
        {
            // Arrange
            using var activity = new Activity("test")
                .SetIdFormat(ActivityIdFormat.W3C)
                .Start();

            var initializer = new CorrelationTelemetryInitializer();
            var telemetry = new RequestTelemetry();

            // Act
            initializer.Initialize(telemetry);

            // Assert
            Assert.Equal(activity.TraceId.ToString(), 
                telemetry.Context.Properties["CorrelationId"]);
        }
    }

    public class ApplicationInsightsBridgeTests
    {
        [Fact]
        public void TrackRequest_InDirectMode_SendsTelemetry()
        {
            // Arrange
            var config = TelemetryConfiguration.CreateDefault();
            var client = new TelemetryClient(config);
            var bridge = new ApplicationInsightsBridge(client);

            // Act
            bridge.TrackRequest(
                "TestOperation",
                DateTimeOffset.UtcNow,
                TimeSpan.FromMilliseconds(100),
                "200",
                true);

            // Assert - verify telemetry was tracked
            // (Would check InMemoryChannel in real test)
        }
    }
}
```

### Integration Tests

```csharp
[Fact]
public void Integration_AppInsightsWithTelemetry_CorrelatesRequestsAndDependencies()
{
    // Arrange
    var services = new ServiceCollection();
    services.AddLogging();
    services.AddTelemetry(options => options.ServiceName = "TestService");
    services.AddApplicationInsightsTelemetry("InstrumentationKey=test-key");

    var provider = services.BuildServiceProvider();
    var bridge = provider.GetRequiredService<ApplicationInsightsBridge>();

    // Act
    using (var activity = new Activity("test-request").Start())
    {
        bridge.TrackRequest("GET /api/orders", 
            DateTimeOffset.UtcNow, 
            TimeSpan.FromMilliseconds(50), 
            "200", 
            true);

        bridge.TrackDependency("SQL", 
            "localhost", 
            "SELECT * FROM Orders", 
            "SELECT", 
            DateTimeOffset.UtcNow, 
            TimeSpan.FromMilliseconds(10), 
            "0", 
            true);
    }

    // Assert - telemetry should be correlated
}
```

### Performance Tests

```csharp
[Fact]
public void Performance_TelemetryInitializer_IsMinimal()
{
    // Arrange
    using var activity = new Activity("test").SetIdFormat(ActivityIdFormat.W3C).Start();
    var initializer = new ActivityTelemetryInitializer();
    var telemetry = new RequestTelemetry();

    // Act
    var stopwatch = Stopwatch.StartNew();
    for (int i = 0; i < 10_000; i++)
    {
        initializer.Initialize(telemetry);
    }
    stopwatch.Stop();

    // Assert - <1μs per initialization
    Assert.True(stopwatch.ElapsedMilliseconds < 10,
        $"Initialization too slow: {stopwatch.ElapsedMilliseconds}ms");
}
```

## Performance Requirements

- **Telemetry initializer overhead**: <1μs per telemetry item
- **Bridge overhead in OTLP mode**: Near-zero (no-op)
- **Bridge overhead in Direct mode**: <10μs per operation
- **Memory overhead**: <100KB for bridge instance
- **Thread-safe**: All components safe for concurrent use

## Dependencies

**Blocked By**: 
- US-002: Auto-Managed Correlation (CorrelationContext)
- US-012: Operation Scope (IOperationScope integration)

**Blocks**: None (extension package)

## Definition of Done

- [ ] All three telemetry initializers implemented
- [ ] Dual-mode bridge working in both OTLP and Direct modes
- [ ] Configuration extensions working with fluent API
- [ ] Unit tests passing (>85% coverage)
- [ ] Integration tests with real Application Insights passing
- [ ] Performance benchmarks meet requirements
- [ ] Works on .NET Framework 4.8 and .NET 8+
- [ ] XML documentation complete for all public APIs
- [ ] Code reviewed and approved
- [ ] Zero warnings in build
- [ ] NuGet package created and validated

## Notes

### Design Decisions

1. **Why dual-mode support?**
   - Modern .NET 8+ apps should use OpenTelemetry + OTLP exporter
   - Legacy .NET Framework 4.8 apps need direct SDK integration
   - Single package works for both scenarios without code changes

2. **Why telemetry initializers instead of processors?**
   - Application Insights SDK pattern for enrichment
   - Works consistently across SDK versions
   - Low overhead and well-documented

3. **Why runtime mode detection?**
   - Reduces configuration burden for users
   - Automatically adapts to environment
   - Explicit override available if needed

4. **Why bridge pattern?**
   - Abstraction over Application Insights SDK
   - Easier to test and mock
   - Provides consistent API regardless of mode

### Implementation Tips

- Use `ITelemetry.Context.Properties` for custom properties
- Check for existing properties before adding (don't overwrite)
- Application Insights has built-in batching and buffering
- Use `TelemetryClient.Flush()` sparingly (it's expensive)
- Test with both connection string and instrumentation key formats

### Common Pitfalls

- **Don't forget to flush on shutdown**: Telemetry may be lost otherwise
- **Don't track in OTLP mode**: Let OpenTelemetry SDK handle export
- **Don't modify telemetry after tracking**: It won't affect sent data
- **Don't create new TelemetryClient per request**: Use singleton

### Future Enhancements

- Support for Application Insights sampling configuration
- Custom metric aggregation strategies
- Live Metrics (QuickPulse) integration
- Snapshot debugger integration for exceptions
- Adaptive sampling based on volume

## Related Documentation

- [Project Plan - Step 24: Application Insights Extension](../project-plan.md#24-application-insights-extension-hvoenterprisetelemetryappinsights)
- [Application Insights .NET SDK](https://github.com/microsoft/ApplicationInsights-dotnet)
- [OpenTelemetry Azure Monitor Exporter](https://github.com/Azure/azure-sdk-for-net/tree/main/sdk/monitor/Azure.Monitor.OpenTelemetry.Exporter)
- [US-002: Auto-Managed Correlation](./US-002-auto-managed-correlation.md)
- [US-012: Operation Scope](./US-012-operation-scope.md)
