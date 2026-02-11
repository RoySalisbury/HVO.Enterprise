using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using HVO.Enterprise.Telemetry.Correlation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace HVO.Enterprise.Samples.Net8.Middleware
{
    /// <summary>
    /// ASP.NET Core middleware that establishes a correlation context for each HTTP request.
    /// Demonstrates:
    ///   • Reading/writing the X-Correlation-ID header
    ///   • Integrating with <see cref="CorrelationContext"/> (AsyncLocal-backed)
    ///   • Enriching log scopes with correlation + trace IDs
    ///   • Setting response headers before the body is written
    /// </summary>
    public sealed class CorrelationMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<CorrelationMiddleware> _logger;

        public CorrelationMiddleware(RequestDelegate next, ILogger<CorrelationMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // 1. Extract or generate a correlation ID
            var correlationId = context.Request.Headers["X-Correlation-ID"].FirstOrDefault()
                ?? Activity.Current?.TraceId.ToString()
                ?? Guid.NewGuid().ToString("N");

            // 2. Set the ambient correlation context
            using var scope = CorrelationContext.BeginScope(correlationId);

            // 3. Ensure the response carries the same correlation ID
            context.Response.OnStarting(() =>
            {
                context.Response.Headers["X-Correlation-ID"] = correlationId;
                return Task.CompletedTask;
            });

            // 4. Log request lifecycle — CorrelationId and TraceId are
            //    automatically enriched by AddTelemetryLoggingEnrichment(),
            //    so there is no need for a manual BeginScope here.
            _logger.LogDebug("Request started — CorrelationId={CorrelationId}", correlationId);

            await _next(context);

            _logger.LogDebug(
                "Request completed — StatusCode={StatusCode}, CorrelationId={CorrelationId}",
                context.Response.StatusCode, correlationId);
        }
    }

    /// <summary>
    /// Extension methods for registering the correlation middleware.
    /// </summary>
    public static class CorrelationMiddlewareExtensions
    {
        /// <summary>
        /// Adds the <see cref="CorrelationMiddleware"/> to the pipeline.
        /// Should be registered early so all downstream components see the correlation ID.
        /// </summary>
        public static IApplicationBuilder UseCorrelation(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<CorrelationMiddleware>();
        }
    }
}
