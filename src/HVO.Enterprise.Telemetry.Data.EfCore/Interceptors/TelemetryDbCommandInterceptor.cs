using System;
using System.Data.Common;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using HVO.Enterprise.Telemetry.Data.Common;
using HVO.Enterprise.Telemetry.Data.EfCore.Configuration;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace HVO.Enterprise.Telemetry.Data.EfCore.Interceptors
{
    /// <summary>
    /// EF Core interceptor that creates <see cref="Activity"/> instances for database operations,
    /// following OpenTelemetry semantic conventions.
    /// </summary>
    public sealed class TelemetryDbCommandInterceptor : DbCommandInterceptor
    {
        private readonly ActivitySource _activitySource;
        private readonly EfCoreTelemetryOptions _options;

        /// <summary>
        /// Initializes a new instance of the <see cref="TelemetryDbCommandInterceptor"/> class.
        /// </summary>
        /// <param name="options">Optional configuration for EF Core telemetry.</param>
        public TelemetryDbCommandInterceptor(EfCoreTelemetryOptions? options = null)
        {
            _activitySource = EfCoreActivitySource.Source;
            _options = options ?? new EfCoreTelemetryOptions();
        }

        /// <summary>
        /// Initializes a new instance using a custom <see cref="ActivitySource"/>.
        /// </summary>
        /// <param name="activitySource">The activity source to use.</param>
        /// <param name="options">Optional configuration.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="activitySource"/> is null.</exception>
        public TelemetryDbCommandInterceptor(ActivitySource activitySource, EfCoreTelemetryOptions? options = null)
        {
            _activitySource = activitySource ?? throw new ArgumentNullException(nameof(activitySource));
            _options = options ?? new EfCoreTelemetryOptions();
        }

        /// <inheritdoc/>
        public override InterceptionResult<DbDataReader> ReaderExecuting(
            DbCommand command,
            CommandEventData eventData,
            InterceptionResult<DbDataReader> result)
        {
            StartActivity(command, "SELECT");
            return base.ReaderExecuting(command, eventData, result);
        }

        /// <inheritdoc/>
        public override Task<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
            DbCommand command,
            CommandEventData eventData,
            InterceptionResult<DbDataReader> result,
            CancellationToken cancellationToken = default)
        {
            StartActivity(command, "SELECT");
            return base.ReaderExecutingAsync(command, eventData, result, cancellationToken);
        }

        /// <inheritdoc/>
        public override DbDataReader ReaderExecuted(
            DbCommand command,
            CommandExecutedEventData eventData,
            DbDataReader result)
        {
            StopActivity(null, eventData.Duration);
            return base.ReaderExecuted(command, eventData, result);
        }

        /// <inheritdoc/>
        public override Task<DbDataReader> ReaderExecutedAsync(
            DbCommand command,
            CommandExecutedEventData eventData,
            DbDataReader result,
            CancellationToken cancellationToken = default)
        {
            StopActivity(null, eventData.Duration);
            return base.ReaderExecutedAsync(command, eventData, result, cancellationToken);
        }

        /// <inheritdoc/>
        public override InterceptionResult<object> ScalarExecuting(
            DbCommand command,
            CommandEventData eventData,
            InterceptionResult<object> result)
        {
            StartActivity(command, "SCALAR");
            return base.ScalarExecuting(command, eventData, result);
        }

        /// <inheritdoc/>
        public override Task<InterceptionResult<object>> ScalarExecutingAsync(
            DbCommand command,
            CommandEventData eventData,
            InterceptionResult<object> result,
            CancellationToken cancellationToken = default)
        {
            StartActivity(command, "SCALAR");
            return base.ScalarExecutingAsync(command, eventData, result, cancellationToken);
        }

        /// <inheritdoc/>
        public override object ScalarExecuted(
            DbCommand command,
            CommandExecutedEventData eventData,
            object result)
        {
            StopActivity(null, eventData.Duration);
            return base.ScalarExecuted(command, eventData, result);
        }

        /// <inheritdoc/>
        public override InterceptionResult<int> NonQueryExecuting(
            DbCommand command,
            CommandEventData eventData,
            InterceptionResult<int> result)
        {
            StartActivity(command, DetectOperation(command.CommandText));
            return base.NonQueryExecuting(command, eventData, result);
        }

        /// <inheritdoc/>
        public override Task<InterceptionResult<int>> NonQueryExecutingAsync(
            DbCommand command,
            CommandEventData eventData,
            InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
        {
            StartActivity(command, DetectOperation(command.CommandText));
            return base.NonQueryExecutingAsync(command, eventData, result, cancellationToken);
        }

        /// <inheritdoc/>
        public override int NonQueryExecuted(
            DbCommand command,
            CommandExecutedEventData eventData,
            int result)
        {
            StopActivity(null, eventData.Duration, result);
            return base.NonQueryExecuted(command, eventData, result);
        }

        /// <inheritdoc/>
        public override void CommandFailed(
            DbCommand command,
            CommandErrorEventData eventData)
        {
            StopActivity(eventData.Exception, eventData.Duration);
            base.CommandFailed(command, eventData);
        }

        /// <inheritdoc/>
        public override Task CommandFailedAsync(
            DbCommand command,
            CommandErrorEventData eventData,
            CancellationToken cancellationToken = default)
        {
            StopActivity(eventData.Exception, eventData.Duration);
            return base.CommandFailedAsync(command, eventData, cancellationToken);
        }

        private void StartActivity(DbCommand command, string operationType)
        {
            if (_options.OperationFilter != null && !_options.OperationFilter(operationType))
                return;

            var activityName = $"db.{operationType}";
            var activity = _activitySource.StartActivity(activityName, ActivityKind.Client);

            if (activity == null)
                return;

            var dbSystem = DatabaseSystemDetector.DetectSystem(
                command.Connection?.ConnectionString,
                command.Connection?.GetType().Namespace);

            activity.SetTag(DataActivityTags.DbSystem, dbSystem);
            activity.SetTag(DataActivityTags.DbOperation, operationType);

            if (!string.IsNullOrEmpty(command.Connection?.Database))
            {
                activity.SetTag(DataActivityTags.DbName, command.Connection!.Database);
            }

            if (_options.RecordStatements && !string.IsNullOrWhiteSpace(command.CommandText))
            {
                var statement = ParameterSanitizer.SanitizeStatement(
                    command.CommandText, _options.MaxStatementLength);
                activity.SetTag(DataActivityTags.DbStatement, statement);
            }

            if (_options.RecordConnectionInfo && command.Connection != null)
            {
                try
                {
                    var builder = new DbConnectionStringBuilder
                    {
                        ConnectionString = command.Connection.ConnectionString
                    };

                    if (builder.TryGetValue("Server", out var server) ||
                        builder.TryGetValue("Data Source", out server))
                    {
                        activity.SetTag(DataActivityTags.ServerAddress, server?.ToString());
                    }
                }
                catch
                {
                    // Best effort — malformed connection strings
                }
            }

            if (_options.RecordParameters && command.Parameters.Count > 0)
            {
                var count = Math.Min(command.Parameters.Count, _options.MaxParameters);
                for (int i = 0; i < count; i++)
                {
                    var param = command.Parameters[i];
                    var paramName = param.ParameterName ?? $"param{i}";
                    var value = ParameterSanitizer.FormatParameterValue(paramName, param.Value);
                    activity.SetTag($"db.parameter.{paramName}", value);
                }
            }
        }

        private static void StopActivity(Exception? exception, TimeSpan duration, int? rowsAffected = null)
        {
            var activity = Activity.Current;
            if (activity == null || activity.Source.Name != EfCoreActivitySource.Name)
                return;

            if (exception != null)
            {
                activity.SetStatus(ActivityStatusCode.Error, exception.Message);
                activity.AddEvent(new ActivityEvent("exception", default, new ActivityTagsCollection
                {
                    { "exception.type", exception.GetType().FullName },
                    { "exception.message", exception.Message }
                }));
            }
            else
            {
                activity.SetStatus(ActivityStatusCode.Ok);
            }

            activity.SetTag(DataActivityTags.DbOperationDuration, duration.TotalMilliseconds);

            if (rowsAffected.HasValue)
            {
                activity.SetTag(DataActivityTags.DbRowsAffected, rowsAffected.Value);
            }

            activity.Stop();
        }

        internal static string DetectOperation(string? commandText)
        {
            if (string.IsNullOrWhiteSpace(commandText))
                return "EXECUTE";

            var trimmed = commandText!.TrimStart();
            if (trimmed.StartsWith("INSERT", StringComparison.OrdinalIgnoreCase))
                return "INSERT";
            if (trimmed.StartsWith("UPDATE", StringComparison.OrdinalIgnoreCase))
                return "UPDATE";
            if (trimmed.StartsWith("DELETE", StringComparison.OrdinalIgnoreCase))
                return "DELETE";
            if (trimmed.StartsWith("SELECT", StringComparison.OrdinalIgnoreCase))
                return "SELECT";
            if (trimmed.StartsWith("MERGE", StringComparison.OrdinalIgnoreCase))
                return "MERGE";
            if (trimmed.StartsWith("CREATE", StringComparison.OrdinalIgnoreCase))
                return "CREATE";
            if (trimmed.StartsWith("ALTER", StringComparison.OrdinalIgnoreCase))
                return "ALTER";
            if (trimmed.StartsWith("DROP", StringComparison.OrdinalIgnoreCase))
                return "DROP";
            if (trimmed.StartsWith("EXEC", StringComparison.OrdinalIgnoreCase))
                return "EXECUTE";

            return "EXECUTE";
        }
    }
}
