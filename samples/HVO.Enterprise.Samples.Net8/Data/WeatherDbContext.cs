using System;
using Microsoft.EntityFrameworkCore;

namespace HVO.Enterprise.Samples.Net8.Data
{
    /// <summary>
    /// EF Core database context for weather data persistence.
    /// Uses SQLite for zero-infrastructure setup.
    /// </summary>
    public class WeatherDbContext : DbContext
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="WeatherDbContext"/> class.
        /// </summary>
        /// <param name="options">Context configuration options.</param>
        public WeatherDbContext(DbContextOptions<WeatherDbContext> options)
            : base(options)
        {
        }

        /// <summary>
        /// Gets or sets the weather readings table.
        /// </summary>
        public DbSet<WeatherReadingEntity> WeatherReadings { get; set; } = null!;

        /// <inheritdoc />
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<WeatherReadingEntity>(entity =>
            {
                entity.HasIndex(e => e.Location);
                entity.HasIndex(e => e.RecordedAtUtc);
                entity.HasIndex(e => new { e.Location, e.RecordedAtUtc });
            });
        }
    }
}
