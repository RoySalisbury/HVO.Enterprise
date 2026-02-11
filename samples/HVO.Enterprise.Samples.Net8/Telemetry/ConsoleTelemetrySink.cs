using System;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace HVO.Enterprise.Samples.Net8.Telemetry
{
    /// <summary>
    /// Background service that listens to <see cref="ActivitySource"/> events and writes
    /// human-readable, formatted telemetry output to the console. Makes telemetry
    /// visible without requiring Jaeger, Zipkin, or other external tools.
    /// </summary>
    public sealed class ConsoleTelemetrySink : BackgroundService
    {
        private readonly ILogger<ConsoleTelemetrySink> _logger;
        private ActivityListener? _listener;
        private long _activitiesStarted;
        private long _activitiesStopped;

        /// <summary>
        /// Initializes a new instance of the <see cref="ConsoleTelemetrySink"/> class.
        /// </summary>
        /// <param name="logger">Logger instance.</param>
        public ConsoleTelemetrySink(ILogger<ConsoleTelemetrySink> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>Total activities started (observed).</summary>
        public long ActivitiesStarted => Interlocked.Read(ref _activitiesStarted);

        /// <summary>Total activities stopped (observed).</summary>
        public long ActivitiesStopped => Interlocked.Read(ref _activitiesStopped);

        /// <inheritdoc />
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("ConsoleTelemetrySink started — listening for Activity events");

            _listener = new ActivityListener
            {
                ShouldListenTo = source =>
                    source.Name.StartsWith("HVO.", StringComparison.OrdinalIgnoreCase)
                    || source.Name.StartsWith("System.Net.Http", StringComparison.OrdinalIgnoreCase),
                Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
                ActivityStarted = OnActivityStarted,
                ActivityStopped = OnActivityStopped,
            };

            ActivitySource.AddActivityListener(_listener);

            // Keep the service alive while listener processes events inline
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
            }

            _listener.Dispose();
            _listener = null;
            _logger.LogInformation("ConsoleTelemetrySink stopped");
        }

        private void OnActivityStarted(Activity activity)
        {
            Interlocked.Increment(ref _activitiesStarted);
        }

        private void OnActivityStopped(Activity activity)
        {
            Interlocked.Increment(ref _activitiesStopped);

            // Format and log the completed activity
            var duration = activity.Duration;
            var status = activity.Status;
            var statusDesc = activity.StatusDescription;

            var tagBuilder = new StringBuilder();
            foreach (var tag in activity.Tags)
            {
                tagBuilder.Append($"\n       {tag.Key} = {tag.Value}");
            }

            var tagInfo = tagBuilder.ToString();

            var logLevel = status == ActivityStatusCode.Error ? LogLevel.Error : LogLevel.Information;

            _logger.Log(logLevel,
                "📊 [{Source}] {OperationName} ({Duration:F1}ms) Status={Status}{StatusDesc}{Tags}",
                activity.Source.Name,
                activity.OperationName,
                duration.TotalMilliseconds,
                status,
                string.IsNullOrEmpty(statusDesc) ? "" : $" ({statusDesc})",
                string.IsNullOrEmpty(tagInfo) ? "" : $"\n     Tags:{tagInfo}");
        }
    }
}
