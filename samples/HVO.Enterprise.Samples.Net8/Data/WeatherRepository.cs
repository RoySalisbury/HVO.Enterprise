using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace HVO.Enterprise.Samples.Net8.Data
{
    /// <summary>
    /// Repository for weather readings using EF Core with HVO telemetry instrumentation.
    /// All queries are automatically traced by the EF Core interceptor.
    /// </summary>
    public class WeatherRepository
    {
        private readonly WeatherDbContext _context;
        private readonly ILogger<WeatherRepository> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="WeatherRepository"/> class.
        /// </summary>
        /// <param name="context">EF Core database context.</param>
        /// <param name="logger">Logger instance.</param>
        public WeatherRepository(WeatherDbContext context, ILogger<WeatherRepository> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Stores a new weather reading.
        /// </summary>
        /// <param name="reading">The reading entity to persist.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The saved entity with its generated ID.</returns>
        public async Task<WeatherReadingEntity> AddReadingAsync(
            WeatherReadingEntity reading, CancellationToken cancellationToken = default)
        {
            _context.WeatherReadings.Add(reading);
            await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            _logger.LogDebug(
                "Stored weather reading for {Location}: {Temperature}°C (Id={ReadingId})",
                reading.Location, reading.TemperatureCelsius, reading.Id);

            return reading;
        }

        /// <summary>
        /// Gets the most recent weather readings, optionally filtered by location.
        /// </summary>
        /// <param name="location">Optional location filter (case-insensitive prefix match).</param>
        /// <param name="count">Maximum number of results (default 20).</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>List of recent readings, most recent first.</returns>
        public async Task<List<WeatherReadingEntity>> GetRecentReadingsAsync(
            string? location = null, int count = 20, CancellationToken cancellationToken = default)
        {
            IQueryable<WeatherReadingEntity> query = _context.WeatherReadings;

            if (!string.IsNullOrWhiteSpace(location))
            {
                // Use EF.Functions.Like() for server-side case-insensitive matching
                // instead of .ToLower() which causes client-side evaluation.
                var pattern = $"%{location}%";
                query = query.Where(r => EF.Functions.Like(r.Location, pattern));
            }

            var results = await query
                .OrderByDescending(r => r.RecordedAtUtc)
                .Take(count)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            _logger.LogDebug(
                "Retrieved {Count} weather readings (Location={Location})",
                results.Count, location ?? "all");

            return results;
        }

        /// <summary>
        /// Gets aggregate statistics for a specific location.
        /// </summary>
        /// <param name="location">Location to aggregate.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Aggregate statistics or null if no data found.</returns>
        public async Task<WeatherAggregateResult?> GetAggregateAsync(
            string location, CancellationToken cancellationToken = default)
        {
            // Use EF.Functions.Like() for server-side case-insensitive matching
            // and compute aggregates on the server to avoid loading all rows.
            var aggregate = await _context.WeatherReadings
                .Where(r => EF.Functions.Like(r.Location, location))
                .GroupBy(r => 1) // Single group for aggregate functions
                .Select(g => new WeatherAggregateResult
                {
                    Location = location,
                    ReadingCount = g.Count(),
                    AverageTemperature = g.Average(r => r.TemperatureCelsius),
                    MinTemperature = g.Min(r => r.TemperatureCelsius),
                    MaxTemperature = g.Max(r => r.TemperatureCelsius),
                    FirstReading = g.Min(r => r.RecordedAtUtc),
                    LastReading = g.Max(r => r.RecordedAtUtc),
                })
                .FirstOrDefaultAsync(cancellationToken)
                .ConfigureAwait(false);

            return aggregate?.ReadingCount > 0 ? aggregate : null;
        }

        /// <summary>
        /// Gets the total count of stored readings.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Total reading count.</returns>
        public async Task<int> GetTotalCountAsync(CancellationToken cancellationToken = default)
        {
            return await _context.WeatherReadings
                .CountAsync(cancellationToken)
                .ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Aggregate weather statistics for a location.
    /// </summary>
    public class WeatherAggregateResult
    {
        /// <summary>Location name.</summary>
        public string Location { get; set; } = string.Empty;

        /// <summary>Total number of readings.</summary>
        public int ReadingCount { get; set; }

        /// <summary>Average temperature in Celsius.</summary>
        public double AverageTemperature { get; set; }

        /// <summary>Minimum recorded temperature.</summary>
        public double MinTemperature { get; set; }

        /// <summary>Maximum recorded temperature.</summary>
        public double MaxTemperature { get; set; }

        /// <summary>Earliest reading timestamp.</summary>
        public DateTime FirstReading { get; set; }

        /// <summary>Most recent reading timestamp.</summary>
        public DateTime LastReading { get; set; }
    }
}
