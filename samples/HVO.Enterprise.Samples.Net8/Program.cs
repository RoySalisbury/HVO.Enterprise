using System;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using HVO.Enterprise.Samples.Net8.Configuration;
using HVO.Enterprise.Samples.Net8.Data;
using HVO.Enterprise.Samples.Net8.Middleware;
using HVO.Enterprise.Telemetry.Abstractions;
using HVO.Enterprise.Telemetry.Correlation;
using HVO.Enterprise.Telemetry.Serilog;
using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi;
using Serilog;
using Serilog.Events;

// ╔═══════════════════════════════════════════════════════════════════════╗
// ║  HVO.Enterprise.Telemetry — Sample Application                      ║
// ║                                                                       ║
// ║  A real-time weather monitoring API that demonstrates comprehensive   ║
// ║  usage of the HVO.Enterprise.Telemetry library including:             ║
// ║   • Correlation context & distributed tracing                         ║
// ║   • Operation scopes with tags, properties, and results               ║
// ║   • DispatchProxy-based automatic instrumentation                     ║
// ║   • Background service telemetry with BackgroundJobContext             ║
// ║   • Exception tracking and aggregation                                ║
// ║   • Telemetry statistics and health checks                            ║
// ║   • HTTP client instrumentation                                       ║
// ║   • ILogger enrichment with correlation & trace IDs                   ║
// ║   • Multi-level configuration hierarchy                               ║
// ║   • Disabled service scaffolding (WCF, Redis, RabbitMQ, etc.)         ║
// ║                                                                       ║
// ║  Endpoints:                                                           ║
// ║   GET  /api/weather/summary         — All monitored locations         ║
// ║   GET  /api/weather/{locationName}  — Single location weather         ║
// ║   GET  /api/weather/locations       — List monitored locations        ║
// ║   POST /api/weather/locations       — Add a monitored location        ║
// ║   DELETE /api/weather/locations/{n} — Remove a monitored location     ║
// ║   GET  /api/weather/alerts          — Current weather alerts          ║
// ║   GET  /api/weather/diagnostics     — Telemetry statistics            ║
// ║   GET  /api/weather/error-demo      — Deliberate exception demo       ║
// ║   GET  /health                      — Health check endpoint           ║
// ║   GET  /health/ready                — Readiness check                 ║
// ║   GET  /health/live                 — Liveness check                  ║
// ╚═══════════════════════════════════════════════════════════════════════╝

var builder = WebApplication.CreateBuilder(args);

// ── Logging ──────────────────────────────────────────────────────────
// Serilog replaces the default logging when enabled (Extensions:Serilog:Enabled).
// Otherwise, the standard console/debug providers are used.
var serilogEnabled = builder.Configuration.GetValue("Extensions:Serilog:Enabled", true);
if (serilogEnabled)
{
    var serilogOutputTemplate = builder.Configuration.GetValue(
        "Extensions:Serilog:OutputTemplate",
        "[{Timestamp:yyyy-MM-dd HH:mm:ss} {Level:u3}] {CorrelationId} | {TraceId} | {Message:lj}{NewLine}{Exception}");

    builder.Host.UseSerilog((context, loggerConfig) =>
    {
        loggerConfig
            .MinimumLevel.Debug()
            .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
            .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
            .Enrich.WithTelemetry() // HVO: Adds CorrelationId, TraceId, SpanId
            .WriteTo.Console(outputTemplate: serilogOutputTemplate);
    });
}
else
{
    builder.Logging.ClearProviders();
    builder.Logging.AddConsole();
    builder.Logging.AddDebug();
    builder.Logging.SetMinimumLevel(LogLevel.Debug);
}

// ── Services ─────────────────────────────────────────────────────────
// Central registration of all services including telemetry.
// See Configuration/ServiceConfiguration.cs for full details.
builder.Services.AddSampleServices(builder.Configuration);

// ── Controllers ──────────────────────────────────────────────────────
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.WriteIndented = true;
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    });

// ── Swagger / OpenAPI ────────────────────────────────────────────────
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "HVO Telemetry Sample — Weather Monitor",
        Version = "v1",
        Description = "A sample Web API that demonstrates the HVO.Enterprise.Telemetry library. "
            + "All endpoints are instrumented with operation scopes, correlation IDs, and structured logging.",
    });
});

// Build the app
var app = builder.Build();

// ── Database Initialization ──────────────────────────────────────────
// Create SQLite database and tables on startup (no migrations needed).
if (builder.Configuration.GetValue("Extensions:Database:Enabled", true))
{
    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<WeatherDbContext>();
        db.Database.EnsureCreated();
        app.Logger.LogInformation("SQLite database initialised: {DbPath}",
            db.Database.GetDbConnection().DataSource);
    }
}

// ── Middleware pipeline ──────────────────────────────────────────────

// Correlation ID middleware (must be first so all downstream logs are enriched)
app.UseCorrelation();

// Error handling
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

// Swagger (available in all environments for this sample)
app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "Weather Monitor v1");
    options.RoutePrefix = string.Empty; // Serve Swagger UI at root
});

// Routing
app.UseRouting();

// ── Health Checks ────────────────────────────────────────────────────
// Three health check endpoints with different filters:
//   /health       — All health checks with detailed JSON
//   /health/ready — Only "ready" tagged checks (external dependencies)
//   /health/live  — Lightweight liveness check

app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse,
    ResultStatusCodes =
    {
        [HealthStatus.Healthy] = 200,
        [HealthStatus.Degraded] = 200,
        [HealthStatus.Unhealthy] = 503,
    }
});

app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready"),
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse,
});

app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = _ => false, // No checks — just confirms the app is running
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse,
});

// ── Controllers ──────────────────────────────────────────────────────
app.MapControllers();

// ── Minimal API endpoints (additional telemetry demos) ───────────────
// These complement the controller-based endpoints to show both patterns.

app.MapGet("/ping", () => Results.Ok(new
{
    status = "ok",
    timestamp = DateTimeOffset.UtcNow,
    correlationId = CorrelationContext.Current,
}))
.WithName("Ping")
.WithTags("diagnostics");

app.MapGet("/info", (ITelemetryService telemetry) =>
{
    return Results.Ok(new
    {
        application = "HVO.Enterprise.Samples.Net8",
        version = "1.0.0",
        telemetryEnabled = telemetry.IsEnabled,
        environment = builder.Environment.EnvironmentName,
        runtime = RuntimeInformation.FrameworkDescription,
    });
})
.WithName("Info")
.WithTags("diagnostics");

// ── Start ────────────────────────────────────────────────────────────
app.Logger.LogInformation(
    "╔═══════════════════════════════════════════════════════════════╗\n" +
    "║  HVO.Enterprise Telemetry Sample Application                  ║\n" +
    "║  Swagger UI: http://localhost:5133                             ║\n" +
    "║  Health:     http://localhost:5133/health                      ║\n" +
    "╚═══════════════════════════════════════════════════════════════╝");

app.Run();

// ── WebApplicationFactory Support ─────────────────────────────────────
// Partial class allows WebApplicationFactory<Program> to discover the entry point.
namespace HVO.Enterprise.Samples.Net8
{
    /// <summary>
    /// Marker class for WebApplicationFactory integration tests.
    /// </summary>
    public partial class Program { }
}
