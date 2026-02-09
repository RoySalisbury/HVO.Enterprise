using System;
using HVO.Enterprise.Telemetry.Data.EfCore.Configuration;
using HVO.Enterprise.Telemetry.Data.EfCore.Interceptors;
using Microsoft.EntityFrameworkCore;

namespace HVO.Enterprise.Telemetry.Data.EfCore.Extensions
{
    /// <summary>
    /// Extension methods for <see cref="DbContextOptionsBuilder"/> to add HVO telemetry.
    /// </summary>
    public static class DbContextOptionsExtensions
    {
        /// <summary>
        /// Adds HVO.Enterprise telemetry interceptor to EF Core for automatic
        /// distributed tracing with OpenTelemetry semantic conventions.
        /// </summary>
        /// <param name="optionsBuilder">The DbContext options builder.</param>
        /// <param name="options">Optional EF Core telemetry configuration.</param>
        /// <returns>The options builder for chaining.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="optionsBuilder"/> is null.</exception>
        public static DbContextOptionsBuilder AddHvoTelemetry(
            this DbContextOptionsBuilder optionsBuilder,
            EfCoreTelemetryOptions? options = null)
        {
            if (optionsBuilder == null)
                throw new ArgumentNullException(nameof(optionsBuilder));

            var interceptor = new TelemetryDbCommandInterceptor(options);
            optionsBuilder.AddInterceptors(interceptor);

            return optionsBuilder;
        }
    }
}
