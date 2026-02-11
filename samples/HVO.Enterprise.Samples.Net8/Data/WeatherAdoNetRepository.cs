using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using HVO.Enterprise.Telemetry.Data.AdoNet;
using Microsoft.Extensions.Logging;

namespace HVO.Enterprise.Samples.Net8.Data
{
    /// <summary>
    /// Repository demonstrating raw ADO.NET queries with HVO telemetry instrumentation.
    /// Uses <see cref="DbConnection.WithTelemetry"/> to wrap the connection.
    /// </summary>
    public class WeatherAdoNetRepository
    {
        private readonly DbConnection _connection;
        private readonly ILogger<WeatherAdoNetRepository> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="WeatherAdoNetRepository"/> class.
        /// </summary>
        /// <param name="connection">An instrumented database connection.</param>
        /// <param name="logger">Logger instance.</param>
        public WeatherAdoNetRepository(DbConnection connection, ILogger<WeatherAdoNetRepository> logger)
        {
            _connection = connection ?? throw new ArgumentNullException(nameof(connection));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Bulk-inserts multiple weather readings using raw ADO.NET.
        /// Demonstrates instrumented command execution.
        /// </summary>
        /// <param name="readings">Readings to insert.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Number of rows inserted.</returns>
        public async Task<int> BulkInsertReadingsAsync(
            IReadOnlyList<WeatherReadingEntity> readings, CancellationToken cancellationToken = default)
        {
            await EnsureOpenAsync(cancellationToken).ConfigureAwait(false);

            int inserted = 0;
            using var transaction = await _connection.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);

            try
            {
                foreach (var reading in readings)
                {
                    using var cmd = _connection.CreateCommand();
                    cmd.Transaction = transaction;
                    cmd.CommandText = @"
                        INSERT INTO WeatherReadings (Location, TemperatureCelsius, Humidity, WindSpeedKmh, Condition, RecordedAtUtc, CorrelationId)
                        VALUES (@location, @temp, @humidity, @wind, @condition, @recordedAt, @correlationId)";

                    AddParameter(cmd, "@location", reading.Location);
                    AddParameter(cmd, "@temp", reading.TemperatureCelsius);
                    AddParameter(cmd, "@humidity", reading.Humidity as object ?? DBNull.Value);
                    AddParameter(cmd, "@wind", reading.WindSpeedKmh as object ?? DBNull.Value);
                    AddParameter(cmd, "@condition", reading.Condition as object ?? DBNull.Value);
                    AddParameter(cmd, "@recordedAt", reading.RecordedAtUtc);
                    AddParameter(cmd, "@correlationId", reading.CorrelationId as object ?? DBNull.Value);

                    inserted += await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
                }

                await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);

                _logger.LogDebug("Bulk inserted {Count} weather readings via ADO.NET", inserted);
                return inserted;
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
                throw;
            }
        }

        /// <summary>
        /// Gets aggregate statistics using a raw SQL query.
        /// Demonstrates instrumented reader execution.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Dictionary of location → average temperature.</returns>
        public async Task<Dictionary<string, double>> GetLocationAveragesAsync(
            CancellationToken cancellationToken = default)
        {
            await EnsureOpenAsync(cancellationToken).ConfigureAwait(false);

            using var cmd = _connection.CreateCommand();
            cmd.CommandText = @"
                SELECT Location, AVG(TemperatureCelsius) AS AvgTemp, COUNT(*) AS ReadingCount
                FROM WeatherReadings
                GROUP BY Location
                ORDER BY ReadingCount DESC";

            var results = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
            using var reader = await cmd.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);

            while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                var location = reader.GetString(0);
                var avgTemp = reader.GetDouble(1);
                results[location] = avgTemp;
            }

            _logger.LogDebug("Retrieved averages for {Count} locations via ADO.NET", results.Count);
            return results;
        }

        /// <summary>
        /// Gets the total row count using a scalar query.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Total reading count.</returns>
        public async Task<long> GetTotalCountAsync(CancellationToken cancellationToken = default)
        {
            await EnsureOpenAsync(cancellationToken).ConfigureAwait(false);

            using var cmd = _connection.CreateCommand();
            cmd.CommandText = "SELECT COUNT(*) FROM WeatherReadings";

            var result = await cmd.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
            return Convert.ToInt64(result);
        }

        private async Task EnsureOpenAsync(CancellationToken cancellationToken)
        {
            if (_connection.State != ConnectionState.Open)
            {
                await _connection.OpenAsync(cancellationToken).ConfigureAwait(false);
            }
        }

        private static void AddParameter(DbCommand command, string name, object value)
        {
            var param = command.CreateParameter();
            param.ParameterName = name;
            param.Value = value;
            command.Parameters.Add(param);
        }
    }
}
