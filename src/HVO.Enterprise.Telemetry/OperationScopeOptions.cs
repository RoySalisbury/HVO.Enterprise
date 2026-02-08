using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.Json;
using HVO.Enterprise.Telemetry.Context;
using Microsoft.Extensions.Logging;

namespace HVO.Enterprise.Telemetry
{
    /// <summary>
    /// Options for configuring operation scope behavior.
    /// </summary>
    public sealed class OperationScopeOptions
    {
        /// <summary>
        /// Gets or sets whether to create an Activity for this operation.
        /// </summary>
        public bool CreateActivity { get; set; } = true;

        /// <summary>
        /// Gets or sets the ActivityKind for the created Activity.
        /// </summary>
        public ActivityKind ActivityKind { get; set; } = ActivityKind.Internal;

        /// <summary>
        /// Gets or sets whether to log operation start/end.
        /// </summary>
        public bool LogEvents { get; set; } = true;

        /// <summary>
        /// Gets or sets the log level for operation events.
        /// </summary>
        public LogLevel LogLevel { get; set; } = LogLevel.Information;

        /// <summary>
        /// Gets or sets whether to record metrics for this operation.
        /// </summary>
        public bool RecordMetrics { get; set; } = true;

        /// <summary>
        /// Gets or sets whether to enrich with context (user, request, environment).
        /// </summary>
        public bool EnrichContext { get; set; } = true;

        /// <summary>
        /// Gets or sets whether to capture exception details on failure.
        /// </summary>
        public bool CaptureExceptions { get; set; } = true;

        /// <summary>
        /// Gets or sets initial tags to add to the operation.
        /// </summary>
        public Dictionary<string, object?>? InitialTags { get; set; }

        /// <summary>
        /// Gets or sets PII redaction options for tags and properties.
        /// </summary>
        public EnrichmentOptions? PiiOptions { get; set; } = new EnrichmentOptions();

        /// <summary>
        /// Gets or sets whether to serialize complex types for tags and properties.
        /// </summary>
        public bool SerializeComplexTypes { get; set; } = true;

        /// <summary>
        /// Gets or sets custom serializer for complex types.
        /// </summary>
        public Func<object, string>? ComplexTypeSerializer { get; set; }

        /// <summary>
        /// Gets or sets JSON serialization options for complex types.
        /// </summary>
        public JsonSerializerOptions? JsonSerializerOptions { get; set; }

        internal OperationScopeOptions CreateChildOptions()
        {
            return new OperationScopeOptions
            {
                CreateActivity = CreateActivity,
                ActivityKind = ActivityKind.Internal,
                LogEvents = LogEvents,
                LogLevel = LogLevel,
                RecordMetrics = RecordMetrics,
                EnrichContext = false,
                CaptureExceptions = CaptureExceptions,
                PiiOptions = PiiOptions,
                SerializeComplexTypes = SerializeComplexTypes,
                ComplexTypeSerializer = ComplexTypeSerializer,
                JsonSerializerOptions = JsonSerializerOptions
            };
        }
    }
}
