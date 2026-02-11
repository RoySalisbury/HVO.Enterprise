using System;
using System.Collections.Generic;
using System.Net.Http;
using HVO.Enterprise.Samples.Net8.BackgroundServices;
using HVO.Enterprise.Samples.Net8.HealthChecks;
using HVO.Enterprise.Samples.Net8.Services;
using HVO.Enterprise.Telemetry;
using HVO.Enterprise.Telemetry.Configuration;
using HVO.Enterprise.Telemetry.HealthChecks;
using HVO.Enterprise.Telemetry.Http;
using HVO.Enterprise.Telemetry.Logging;
using HVO.Enterprise.Telemetry.Proxies;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;

namespace HVO.Enterprise.Samples.Net8.Configuration
{
    /// <summary>
    /// Centralised service registration for the sample application.
    /// Demonstrates the recommended way to wire up all HVO.Enterprise.Telemetry
    /// features via Dependency Injection, including features that are intentionally
    /// disabled (WCF, RabbitMQ, Redis, Datadog, Serilog, App Insights).
    /// </summary>
    public static class ServiceConfiguration
    {
        /// <summary>
        /// Registers all application services including telemetry.
        /// </summary>
        public static IServiceCollection AddSampleServices(
            this IServiceCollection services, IConfiguration configuration)
        {
            // ────────────────────────────────────────────────────────
            // 1. CORE TELEMETRY (always enabled)
            // ────────────────────────────────────────────────────────

            // Option A: Configure from appsettings.json (recommended for production)
            services.AddTelemetry(configuration.GetSection("Telemetry"));

            // Option B: Configure fluently in code (useful for quick setup)
            // services.AddTelemetry(options =>
            // {
            //     options.ServiceName = "HVO.Samples.Net8";
            //     options.ServiceVersion = "1.0.0";
            //     options.Environment = "Development";
            //     options.Enabled = true;
            //     options.DefaultSamplingRate = 1.0;
            //     options.Queue.Capacity = 10000;
            //     options.Queue.BatchSize = 100;
            //     options.Features.EnableHttpInstrumentation = true;
            //     options.Features.EnableExceptionTracking = true;
            //     options.Features.EnableParameterCapture = true;
            //     options.Features.EnableProxyInstrumentation = true;
            //     options.Logging.EnableCorrelationEnrichment = true;
            // });

            // Option C: Builder pattern for advanced setup
            // services.AddTelemetry(builder => builder
            //     .Configure(o => { o.ServiceName = "HVO.Samples.Net8"; })
            //     .AddActivitySource("HVO.Samples.Weather")
            //     .AddHttpInstrumentation(http =>
            //     {
            //         http.RedactQueryStrings = true;
            //         http.CaptureRequestHeaders = false;
            //         http.CaptureResponseHeaders = false;
            //     }));

            // ────────────────────────────────────────────────────────
            // 2. LOGGING ENRICHMENT
            //    Automatically adds CorrelationId, TraceId, SpanId
            //    to all ILogger log entries.
            // ────────────────────────────────────────────────────────

            services.AddTelemetryLoggingEnrichment(options =>
            {
                options.EnableEnrichment = true;
                options.IncludeCorrelationId = true;
                options.IncludeTraceId = true;
                options.IncludeSpanId = true;

                // Add custom enrichers
                options.CustomEnrichers ??= new();
                options.CustomEnrichers.Add(
                    new HVO.Enterprise.Telemetry.Logging.Enrichers.EnvironmentLogEnricher());
            });

            // ────────────────────────────────────────────────────────
            // 2b. FIRST-CHANCE EXCEPTION MONITORING (opt-in)
            //     Detects exceptions the instant they are thrown, even
            //     if they are subsequently caught and suppressed. Useful
            //     for diagnosing hidden failures at runtime.
            //     Configuration is hot-reloadable via appsettings.json.
            // ────────────────────────────────────────────────────────

            services.AddFirstChanceExceptionMonitoring(options =>
            {
                // Disabled by default — enable via appsettings.json or here
                options.Enabled = false;

                // Log first-chance exceptions at Warning level
                options.MinimumLogLevel = LogLevel.Warning;

                // Cap at 100 events/second to prevent log flooding
                options.MaxEventsPerSecond = 100;

                // Ignore common harmless cancellation exceptions
                options.ExcludeExceptionTypes.Add("System.OperationCanceledException");
                options.ExcludeExceptionTypes.Add("System.Threading.Tasks.TaskCanceledException");
            });

            // ────────────────────────────────────────────────────────
            // 3. TELEMETRY HEALTH CHECKS
            //    Monitors queue depth, drop rate, error rate.
            // ────────────────────────────────────────────────────────

            services.AddTelemetryStatistics();
            services.AddTelemetryHealthCheck(new HVO.Enterprise.Telemetry.HealthChecks.TelemetryHealthCheckOptions
            {
                DegradedErrorRateThreshold = 5.0,
                UnhealthyErrorRateThreshold = 20.0,
                MaxExpectedQueueDepth = 10000,
                DegradedQueueDepthPercent = 75.0,
                UnhealthyQueueDepthPercent = 95.0,
            });

            services.AddHealthChecks()
                .AddCheck<HVO.Enterprise.Telemetry.HealthChecks.TelemetryHealthCheck>(
                    "telemetry",
                    failureStatus: HealthStatus.Degraded,
                    tags: new[] { "ready" });

            // ────────────────────────────────────────────────────────
            // 4. PROXY INSTRUMENTATION (DispatchProxy)
            //    Wraps IWeatherService with automatic operation scopes,
            //    parameter capture, and metric recording.
            // ────────────────────────────────────────────────────────

            services.AddTelemetryProxyFactory();

            // Register WeatherService with the instrumented wrapper.
            // The proxy automatically creates operation scopes for every
            // interface method call, capturing parameters, return values,
            // timing, and exceptions.
            services.AddInstrumentedScoped<IWeatherService, WeatherService>(
                new InstrumentationOptions
                {
                    CaptureComplexTypes = true,
                    MaxCaptureDepth = 2,
                    MaxCollectionItems = 10,
                    AutoDetectPii = true,
                });

            // ────────────────────────────────────────────────────────
            // 5. HTTP CLIENT with TELEMETRY HANDLER
            //    Automatically instruments outbound HTTP calls with
            //    correlation propagation and operation scopes.
            // ────────────────────────────────────────────────────────

            services.AddHttpClient("OpenMeteo", client =>
            {
                client.BaseAddress = new Uri("https://api.open-meteo.com");
                client.Timeout = TimeSpan.FromSeconds(30);
                client.DefaultRequestHeaders.Add("Accept", "application/json");
            })
            .AddHttpMessageHandler(sp =>
            {
                var logger = sp.GetService<ILogger<TelemetryHttpMessageHandler>>();
                return new TelemetryHttpMessageHandler(
                    new HttpInstrumentationOptions
                    {
                        RedactQueryStrings = false, // Weather API queries are not sensitive
                        CaptureRequestHeaders = false,
                        CaptureResponseHeaders = false,
                    },
                    logger);
            });

            // Default HttpClient for other uses
            services.AddHttpClient();

            // Register HttpClient as a resolvable service so that
            // WeatherService (registered via AddInstrumentedScoped) can
            // receive it through constructor injection.
            services.AddScoped<HttpClient>(sp =>
                sp.GetRequiredService<IHttpClientFactory>().CreateClient("OpenMeteo"));

            // ────────────────────────────────────────────────────────
            // 6. BACKGROUND SERVICES
            // ────────────────────────────────────────────────────────

            services.AddHostedService<WeatherCollectorService>();
            services.AddHostedService<TelemetryReporterService>();

            // ────────────────────────────────────────────────────────
            // 7. HEALTH CHECKS (ASP.NET Core)
            // ────────────────────────────────────────────────────────

            services.AddHealthChecks()
                .AddCheck<WeatherApiHealthCheck>(
                    "weather-api",
                    failureStatus: HealthStatus.Degraded,
                    tags: new[] { "ready", "external" });

            // ────────────────────────────────────────────────────────
            // 8. MULTI-LEVEL CONFIGURATION (demonstrate hierarchy)
            //    Global → Namespace → Type → Method
            // ────────────────────────────────────────────────────────

            ConfigureMultiLevelTelemetry();

            // ════════════════════════════════════════════════════════
            // DISABLED / OPTIONAL INTEGRATIONS
            // Uncomment the sections below when infrastructure is
            // available. Each section shows the correct setup pattern.
            // ════════════════════════════════════════════════════════

            // ── Serilog Extension ──────────────────────────────────
            // Requires: HVO.Enterprise.Telemetry.Serilog NuGet package
            //
            // builder.Host.UseSerilog((context, config) =>
            // {
            //     config
            //         .ReadFrom.Configuration(context.Configuration)
            //         .Enrich.WithHvoTelemetry()  // Adds CorrelationId, TraceId, etc.
            //         .WriteTo.Console(
            //             outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] " +
            //                           "{CorrelationId} | {Message:lj}{NewLine}{Exception}");
            // });

            // ── Application Insights Extension ─────────────────────
            // Requires: HVO.Enterprise.Telemetry.AppInsights NuGet package
            //
            // services.AddApplicationInsightsTelemetry(options =>
            // {
            //     options.ConnectionString = configuration["ApplicationInsights:ConnectionString"];
            // });
            // services.AddHvoAppInsightsTelemetry(); // Bridge HVO telemetry → App Insights

            // ── Datadog Extension ──────────────────────────────────
            // Requires: HVO.Enterprise.Telemetry.Datadog NuGet package
            //
            // services.AddHvoDatadogTelemetry(options =>
            // {
            //     options.AgentHost = "localhost";
            //     options.AgentPort = 8126;
            //     options.ServiceName = "hvo-samples-net8";
            //     options.Environment = "development";
            //     options.EnableMetrics = true;
            //     options.EnableTracing = true;
            // });

            // ── IIS Extension ──────────────────────────────────────
            // For ASP.NET (non-Core) hosted in IIS. Not applicable to
            // this .NET 8 sample but shown for documentation purposes.
            //
            // services.AddHvoIisTelemetry(options =>
            // {
            //     options.EnableModuleInstrumentation = true;
            //     options.EnableHandlerInstrumentation = true;
            //     options.CorrelationHeaderName = "X-Correlation-ID";
            // });

            // ── WCF Extension ──────────────────────────────────────
            // For WCF services. Requires HVO.Enterprise.Telemetry.Wcf.
            //
            // services.AddHvoWcfTelemetry(options =>
            // {
            //     options.EnableMessageInspector = true;
            //     options.EnableOperationBehavior = true;
            //     options.PropagateCorrelation = true;
            //     options.CaptureMessageHeaders = true;
            // });

            // ── Database Extension (EF Core) ───────────────────────
            // Requires: HVO.Enterprise.Telemetry.Data.EfCore
            //
            // services.AddDbContext<MyDbContext>(options =>
            // {
            //     options.UseSqlServer(configuration.GetConnectionString("DefaultConnection"));
            //     options.AddInterceptors(new HvoTelemetryDbInterceptor());
            // });
            // services.AddHvoDatabaseTelemetry(options =>
            // {
            //     options.CaptureCommandText = false;  // PII safety
            //     options.CaptureParameters = false;
            //     options.SlowQueryThresholdMs = 500;
            // });

            // ── Database Extension (ADO.NET) ───────────────────────
            // Requires: HVO.Enterprise.Telemetry.Data.AdoNet
            //
            // services.AddHvoAdoNetTelemetry(options =>
            // {
            //     options.CaptureCommandText = false;
            //     options.SlowQueryThresholdMs = 500;
            // });

            // ── Redis Extension ────────────────────────────────────
            // Requires: HVO.Enterprise.Telemetry.Data.Redis
            //
            // services.AddStackExchangeRedisCache(options =>
            // {
            //     options.Configuration = configuration["Redis:ConnectionString"];
            // });
            // services.AddHvoRedisTelemetry(options =>
            // {
            //     options.CaptureCommandText = true;
            //     options.SlowCommandThresholdMs = 50;
            // });

            // ── RabbitMQ Extension ─────────────────────────────────
            // Requires: HVO.Enterprise.Telemetry.Data.RabbitMQ
            //
            // services.AddHvoRabbitMqTelemetry(options =>
            // {
            //     options.HostName = "localhost";
            //     options.Port = 5672;
            //     options.PropagateCorrelation = true;
            //     options.CaptureMessageHeaders = true;
            //     options.CaptureMessageBody = false;  // PII safety
            // });

            return services;
        }

        /// <summary>
        /// Demonstrates the multi-level configuration hierarchy:
        /// Global → Namespace → Type → Method.
        /// Each level can override sampling, parameter capture, and other settings.
        /// </summary>
        private static void ConfigureMultiLevelTelemetry()
        {
            var configurator = new TelemetryConfigurator();

            // Global defaults — applies to everything
            configurator.Global()
                .SamplingRate(1.0)           // Sample 100% in dev
                .CaptureParameters(ParameterCaptureMode.NamesOnly)
                .RecordExceptions(true)
                .TimeoutThreshold(5000)      // 5 seconds
                .AddTag("app", "HVO.Samples.Net8")
                .Apply();

            // Namespace-level — reduce sampling for noisy namespaces
            configurator.Namespace("HVO.Enterprise.Samples.Net8.BackgroundServices")
                .SamplingRate(0.5)           // 50% sampling for background work
                .CaptureParameters(ParameterCaptureMode.None)
                .Apply();

            // Type-level — detailed capture for the core weather service
            configurator.ForType<WeatherService>()
                .SamplingRate(1.0)
                .CaptureParameters(ParameterCaptureMode.NamesAndValues)
                .RecordExceptions(true)
                .TimeoutThreshold(10000)     // 10 seconds (external API)
                .Apply();

            // Method-level — full capture for a specific critical method
            // (Using MethodInfo lookup — in real code, get via reflection)
            // configurator.ForMethod(typeof(WeatherService).GetMethod("GetCurrentWeatherAsync")!)
            //     .CaptureParameters(ParameterCaptureMode.Full)
            //     .TimeoutThreshold(15000)
            //     .Apply();
        }
    }
}
