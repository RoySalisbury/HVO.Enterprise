using System;
using System.Collections.Generic;
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

            // 4. Enrich the ILogger scope for all downstream logging
            using (_logger.BeginScope(new Dictionary<string, object>
            {
                ["CorrelationId"] = correlationId,
                ["TraceId"] = Activity.Current?.TraceId.ToString() ?? "none",
                ["RequestPath"] = context.Request.Path.Value ?? "/",
                ["RequestMethod"] = context.Request.Method,
            }))
            {
                _logger.LogDebug("Request started — CorrelationId={CorrelationId}", correlationId);

                await _next(context);

                _logger.LogDebug(
                    "Request completed — StatusCode={StatusCode}, CorrelationId={CorrelationId}",
                    context.Response.StatusCode, correlationId);
            }
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
