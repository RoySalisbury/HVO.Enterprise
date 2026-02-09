using System;
using System.Data;
using System.Data.Common;
using HVO.Enterprise.Telemetry.Data.AdoNet.Configuration;

namespace HVO.Enterprise.Telemetry.Data.AdoNet.Instrumentation
{
    /// <summary>
    /// Wraps a <see cref="DbConnection"/> to return instrumented commands
    /// that create Activities for each database operation.
    /// </summary>
    public sealed class InstrumentedDbConnection : DbConnection
    {
        private readonly DbConnection _inner;
        private readonly AdoNetTelemetryOptions _options;

        /// <summary>
        /// Initializes a new instance of <see cref="InstrumentedDbConnection"/>.
        /// </summary>
        /// <param name="inner">The inner connection to wrap.</param>
        /// <param name="options">Optional telemetry options.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="inner"/> is null.</exception>
        public InstrumentedDbConnection(DbConnection inner, AdoNetTelemetryOptions? options = null)
        {
            _inner = inner ?? throw new ArgumentNullException(nameof(inner));
            _options = options ?? new AdoNetTelemetryOptions();
        }

        /// <summary>
        /// Gets the underlying connection being wrapped.
        /// </summary>
        public DbConnection InnerConnection => _inner;

        /// <inheritdoc/>
        public override string ConnectionString
        {
            get => _inner.ConnectionString;
            set => _inner.ConnectionString = value;
        }

        /// <inheritdoc/>
        public override string Database => _inner.Database;

        /// <inheritdoc/>
        public override string DataSource => _inner.DataSource;

        /// <inheritdoc/>
        public override string ServerVersion => _inner.ServerVersion;

        /// <inheritdoc/>
        public override ConnectionState State => _inner.State;

        /// <inheritdoc/>
        public override void ChangeDatabase(string databaseName) => _inner.ChangeDatabase(databaseName);

        /// <inheritdoc/>
        public override void Close() => _inner.Close();

        /// <inheritdoc/>
        public override void Open() => _inner.Open();

        /// <inheritdoc/>
        protected override DbTransaction BeginDbTransaction(IsolationLevel isolationLevel)
            => _inner.BeginTransaction(isolationLevel);

        /// <inheritdoc/>
        protected override DbCommand CreateDbCommand()
        {
            var innerCommand = _inner.CreateCommand();
            return new InstrumentedDbCommand(innerCommand, _options);
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
    }
}
