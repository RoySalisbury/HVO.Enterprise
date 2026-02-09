using System;
using System.Data;
using System.Data.Common;
using System.Diagnostics;
using HVO.Enterprise.Telemetry.Data.AdoNet.Configuration;
using HVO.Enterprise.Telemetry.Data.Common;

namespace HVO.Enterprise.Telemetry.Data.AdoNet.Instrumentation
{
    /// <summary>
    /// Wraps a <see cref="DbCommand"/> to create <see cref="Activity"/> instances
    /// for each database operation with OpenTelemetry semantic conventions.
    /// </summary>
    public sealed class InstrumentedDbCommand : DbCommand
    {
        private readonly DbCommand _inner;
        private readonly ActivitySource _activitySource;
        private readonly AdoNetTelemetryOptions _options;

        /// <summary>
        /// Initializes a new instance of <see cref="InstrumentedDbCommand"/>.
        /// </summary>
        /// <param name="inner">The inner command to wrap.</param>
        /// <param name="options">Telemetry options.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="inner"/> is null.</exception>
        public InstrumentedDbCommand(DbCommand inner, AdoNetTelemetryOptions? options = null)
        {
            _inner = inner ?? throw new ArgumentNullException(nameof(inner));
            _activitySource = AdoNetActivitySource.Source;
            _options = options ?? new AdoNetTelemetryOptions();
        }

        /// <inheritdoc/>
        public override string CommandText
        {
            get => _inner.CommandText;
            set => _inner.CommandText = value;
        }

        /// <inheritdoc/>
        public override int CommandTimeout
        {
            get => _inner.CommandTimeout;
            set => _inner.CommandTimeout = value;
        }

        /// <inheritdoc/>
        public override CommandType CommandType
        {
            get => _inner.CommandType;
            set => _inner.CommandType = value;
        }

        /// <inheritdoc/>
        public override bool DesignTimeVisible
        {
            get => _inner.DesignTimeVisible;
            set => _inner.DesignTimeVisible = value;
        }

        /// <inheritdoc/>
        public override UpdateRowSource UpdatedRowSource
        {
            get => _inner.UpdatedRowSource;
            set => _inner.UpdatedRowSource = value;
        }

        /// <inheritdoc/>
        protected override DbConnection? DbConnection
        {
            get => _inner.Connection;
            set => _inner.Connection = value;
        }

        /// <inheritdoc/>
        protected override DbParameterCollection DbParameterCollection => _inner.Parameters;

        /// <inheritdoc/>
        protected override DbTransaction? DbTransaction
        {
            get => _inner.Transaction;
            set => _inner.Transaction = value;
        }

        /// <inheritdoc/>
        public override void Cancel() => _inner.Cancel();

        /// <inheritdoc/>
        public override void Prepare() => _inner.Prepare();

        /// <inheritdoc/>
        protected override DbParameter CreateDbParameter() => _inner.CreateParameter();

        /// <inheritdoc/>
        public override int ExecuteNonQuery()
        {
            var operation = DetectOperation(_inner.CommandText);
            using (var activity = StartActivity(operation))
            {
                try
                {
                    var result = _inner.ExecuteNonQuery();

                    if (activity != null)
                    {
                        activity.SetStatus(ActivityStatusCode.Ok);
                        activity.SetTag(DataActivityTags.DbRowsAffected, result);
                    }

                    return result;
                }
                catch (Exception ex)
                {
                    SetException(activity, ex);
                    throw;
                }
            }
        }

        /// <inheritdoc/>
        public override object ExecuteScalar()
        {
            using (var activity = StartActivity("SCALAR"))
            {
                try
                {
                    var result = _inner.ExecuteScalar();
                    activity?.SetStatus(ActivityStatusCode.Ok);
                    return result!;
                }
                catch (Exception ex)
                {
                    SetException(activity, ex);
                    throw;
                }
            }
        }

        /// <inheritdoc/>
        protected override DbDataReader ExecuteDbDataReader(CommandBehavior behavior)
        {
            using (var activity = StartActivity("SELECT"))
            {
                try
                {
                    var result = _inner.ExecuteReader(behavior);
                    activity?.SetStatus(ActivityStatusCode.Ok);
                    return result;
                }
                catch (Exception ex)
                {
                    SetException(activity, ex);
                    throw;
                }
            }
        }

        /// <inheritdoc/>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _inner.Dispose();
            }

            base.Dispose(disposing);
        }

        private Activity? StartActivity(string operationType)
        {
            if (_options.OperationFilter != null && !_options.OperationFilter(operationType))
                return null;

            var activity = _activitySource.StartActivity($"db.{operationType}", ActivityKind.Client);

            if (activity == null)
                return null;

            var dbSystem = DatabaseSystemDetector.DetectSystem(
                _inner.Connection?.ConnectionString,
                _inner.Connection?.GetType().Namespace);

            activity.SetTag(DataActivityTags.DbSystem, dbSystem);
            activity.SetTag(DataActivityTags.DbOperation, operationType);

            if (!string.IsNullOrEmpty(_inner.Connection?.Database))
            {
                activity.SetTag(DataActivityTags.DbName, _inner.Connection!.Database);
            }

            if (_options.RecordStatements && !string.IsNullOrWhiteSpace(_inner.CommandText))
            {
                var statement = ParameterSanitizer.SanitizeStatement(
                    _inner.CommandText, _options.MaxStatementLength);
                activity.SetTag(DataActivityTags.DbStatement, statement);
            }

            if (_options.RecordParameters && _inner.Parameters.Count > 0)
            {
                var count = Math.Min(_inner.Parameters.Count, _options.MaxParameters);
                for (int i = 0; i < count; i++)
                {
                    var param = _inner.Parameters[i];
                    var paramName = param.ParameterName ?? $"param{i}";
                    var value = ParameterSanitizer.FormatParameterValue(paramName, param.Value);
                    activity.SetTag($"db.parameter.{paramName}", value);
                }
            }

            return activity;
        }

        private static void SetException(Activity? activity, Exception ex)
        {
            if (activity == null)
                return;

            activity.SetStatus(ActivityStatusCode.Error, ex.Message);
            activity.AddEvent(new ActivityEvent("exception", default, new ActivityTagsCollection
            {
                { "exception.type", ex.GetType().FullName },
                { "exception.message", ex.Message }
            }));
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
            if (trimmed.StartsWith("EXEC", StringComparison.OrdinalIgnoreCase))
                return "EXECUTE";

            return "EXECUTE";
        }
    }
}
