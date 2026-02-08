using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text.Json;
using HVO.Enterprise.Telemetry.Context;
using HVO.Enterprise.Telemetry.Correlation;
using HVO.Enterprise.Telemetry.Exceptions;
using HVO.Enterprise.Telemetry.Metrics;
using Microsoft.Extensions.Logging;

namespace HVO.Enterprise.Telemetry.Internal
{
    /// <summary>
    /// Default implementation of <see cref="IOperationScope"/>.
    /// </summary>
    internal sealed class OperationScope : IOperationScope
    {
        private readonly string _name;
        private readonly OperationScopeOptions _options;
        private readonly ActivitySource? _activitySource;
        private readonly ILogger? _logger;
        private readonly IContextEnricher? _enricher;
        private readonly PiiRedactor _piiRedactor;
        private readonly EnrichmentOptions _piiOptions;
        private readonly Stopwatch _stopwatch;
        private readonly Activity? _activity;

        private Dictionary<string, object?>? _tags;
        private List<LazyProperty>? _lazyProperties;
        private bool _disposed;
        private bool _failed;
        private Exception? _exception;
        private object? _result;

        public OperationScope(
            string name,
            OperationScopeOptions options,
            ActivitySource? activitySource,
            ILogger? logger,
            IContextEnricher? enricher,
            ActivityContext? parentContext)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentNullException(nameof(name));
            if (options == null)
                throw new ArgumentNullException(nameof(options));

            _name = name;
            _options = options;
            _activitySource = activitySource;
            _logger = logger;
            _enricher = enricher;
            _piiRedactor = new PiiRedactor();
            _piiOptions = options.PiiOptions ?? new EnrichmentOptions();
            _piiOptions.EnsureDefaults();

            CorrelationId = CorrelationContext.Current;

            if (_options.CreateActivity && _activitySource != null)
            {
                _activity = parentContext.HasValue
                    ? _activitySource.StartActivity(_name, _options.ActivityKind, parentContext.Value)
                    : _activitySource.StartActivity(_name, _options.ActivityKind);

                if (_activity != null && _options.EnrichContext && _enricher != null)
                {
                    _enricher.EnrichActivity(_activity);
                }
            }

            if (_options.InitialTags != null)
            {
                foreach (var tag in _options.InitialTags)
                {
                    AddTagInternal(tag.Key, tag.Value);
                }
            }

            _stopwatch = Stopwatch.StartNew();

            if (_options.LogEvents && _logger != null)
            {
                _logger.Log(_options.LogLevel, "Operation {OperationName} started", _name);
            }
        }

        public string Name => _name;

        public string CorrelationId { get; }

        public Activity? Activity => _activity;

        public TimeSpan Elapsed => _stopwatch.Elapsed;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IOperationScope WithTag(string key, object? value)
        {
            if (_disposed)
                return this;
            if (string.IsNullOrEmpty(key))
                return this;

            AddTagInternal(key, value);
            return this;
        }

        public IOperationScope WithTags(IEnumerable<KeyValuePair<string, object?>> tags)
        {
            if (_disposed)
                return this;
            if (tags == null)
                return this;

            foreach (var tag in tags)
            {
                AddTagInternal(tag.Key, tag.Value);
            }

            return this;
        }

        public IOperationScope WithProperty(string key, Func<object?> valueFactory)
        {
            if (_disposed)
                return this;
            if (string.IsNullOrEmpty(key))
                return this;
            if (valueFactory == null)
                return this;

            if (_lazyProperties == null)
                _lazyProperties = new List<LazyProperty>();

            _lazyProperties.Add(new LazyProperty(key, valueFactory));
            return this;
        }

        public IOperationScope Fail(Exception exception)
        {
            if (_disposed)
                return this;
            if (exception == null)
                throw new ArgumentNullException(nameof(exception));

            _failed = true;
            _exception = exception;

            if (_activity != null)
            {
                _activity.SetStatus(ActivityStatusCode.Error, exception.Message);
            }

            if (_options.CaptureExceptions)
            {
                RecordExceptionOnActivity(exception);
            }

            return this;
        }

        public IOperationScope Succeed()
        {
            if (_disposed)
                return this;

            _failed = false;
            _exception = null;

            if (_activity != null)
            {
                _activity.SetStatus(ActivityStatusCode.Ok);
            }

            return this;
        }

        public IOperationScope WithResult(object? result)
        {
            if (_disposed)
                return this;

            _result = result;
            return this;
        }

        public IOperationScope CreateChild(string name)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(OperationScope));

            var childOptions = _options.CreateChildOptions();
            var parentContext = _activity != null ? _activity.Context : (ActivityContext?)null;

            return new OperationScope(
                name,
                childOptions,
                _activitySource,
                _logger,
                _enricher,
                parentContext);
        }

        public void RecordException(Exception exception)
        {
            Fail(exception);
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;

            _stopwatch.Stop();
            var duration = _stopwatch.Elapsed;

            EvaluateLazyProperties();

            if (_result != null)
            {
                AddTagInternal("operation.result", _result);
            }

            AddTagInternal("duration_ms", duration.TotalMilliseconds);

            if (_options.RecordMetrics)
            {
                OperationScopeMetrics.RecordDuration(_name, duration, _failed);
                if (_failed && _exception != null)
                    OperationScopeMetrics.RecordError(_name, _exception);
            }

            if (_options.LogEvents && _logger != null)
            {
                if (_failed)
                {
                    _logger.Log(
                        _options.LogLevel,
                        _exception,
                        "Operation {OperationName} failed after {DurationMs}ms",
                        _name,
                        duration.TotalMilliseconds);
                }
                else
                {
                    _logger.Log(
                        _options.LogLevel,
                        "Operation {OperationName} completed in {DurationMs}ms",
                        _name,
                        duration.TotalMilliseconds);
                }
            }

            _activity?.Dispose();
        }

        private void EvaluateLazyProperties()
        {
            if (_lazyProperties == null || _lazyProperties.Count == 0)
                return;

            foreach (var lazyProperty in _lazyProperties)
            {
                try
                {
                    var value = lazyProperty.ValueFactory();
                    AddTagInternal(lazyProperty.Key, value);
                }
                catch (Exception ex)
                {
                    if (_logger != null)
                    {
                        _logger.LogWarning(ex, "Failed to evaluate lazy property {PropertyKey}", lazyProperty.Key);
                    }
                }
            }
        }

        private void AddTagInternal(string key, object? value)
        {
            if (string.IsNullOrEmpty(key))
                return;

            var normalized = NormalizeTagValue(key, value);
            if (normalized == null)
                return;

            if (_tags == null)
                _tags = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

            _tags[key] = normalized;
            _activity?.SetTag(key, normalized);
        }

        private object? NormalizeTagValue(string key, object? value)
        {
            if (value == null)
                return null;

            if (value is string text)
            {
                return RedactIfNeeded(key, text);
            }

            if (IsSimpleValue(value))
                return value;

            if (!_options.SerializeComplexTypes)
                return value.ToString();

            try
            {
                if (_options.ComplexTypeSerializer != null)
                    return _options.ComplexTypeSerializer(value);

                return JsonSerializer.Serialize(value, value.GetType(), _options.JsonSerializerOptions);
            }
            catch (Exception ex)
            {
                if (_logger != null)
                    _logger.LogWarning(ex, "Failed to serialize value for tag {TagKey}", key);

                return value.ToString();
            }
        }

        private string? RedactIfNeeded(string key, string value)
        {
            if (_piiRedactor.TryRedact(key, value, _piiOptions, out var redacted))
                return redacted;

            return value;
        }

        private void RecordExceptionOnActivity(Exception exception)
        {
            var previous = Activity.Current;
            var restore = _activity != null && !ReferenceEquals(previous, _activity);

            if (restore)
                Activity.Current = _activity;

            try
            {
                exception.RecordException();
            }
            finally
            {
                if (restore)
                    Activity.Current = previous;
            }
        }

        private static bool IsSimpleValue(object value)
        {
            var type = value.GetType();
            if (type.IsPrimitive || value is decimal)
                return true;

            return value is DateTime
                || value is DateTimeOffset
                || value is TimeSpan
                || value is Guid;
        }

        private readonly struct LazyProperty
        {
            public readonly string Key;
            public readonly Func<object?> ValueFactory;

            public LazyProperty(string key, Func<object?> valueFactory)
            {
                Key = key;
                ValueFactory = valueFactory;
            }
        }
    }
}
