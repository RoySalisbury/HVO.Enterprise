using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HVO.Enterprise.Samples.Net8.Data
{
    /// <summary>
    /// EF Core entity representing a stored weather reading.
    /// </summary>
    [Table("WeatherReadings")]
    public class WeatherReadingEntity
    {
        /// <summary>
        /// Auto-incremented primary key.
        /// </summary>
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        /// <summary>
        /// Location name (city or coordinates).
        /// </summary>
        [Required]
        [MaxLength(200)]
        public string Location { get; set; } = string.Empty;

        /// <summary>
        /// Temperature in Celsius.
        /// </summary>
        public double TemperatureCelsius { get; set; }

        /// <summary>
        /// Humidity percentage (0-100).
        /// </summary>
        public double? Humidity { get; set; }

        /// <summary>
        /// Wind speed in km/h.
        /// </summary>
        public double? WindSpeedKmh { get; set; }

        /// <summary>
        /// Weather condition description.
        /// </summary>
        [MaxLength(100)]
        public string? Condition { get; set; }

        /// <summary>
        /// UTC timestamp when the reading was recorded.
        /// </summary>
        public DateTime RecordedAtUtc { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Correlation ID from the request that produced this reading.
        /// </summary>
        [MaxLength(50)]
        public string? CorrelationId { get; set; }
    }
}
