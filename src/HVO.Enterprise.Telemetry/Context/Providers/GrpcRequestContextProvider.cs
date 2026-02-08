using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace HVO.Enterprise.Telemetry.Context.Providers
{
    /// <summary>
    /// Enriches telemetry with gRPC request context.
    /// </summary>
    public sealed class GrpcRequestContextProvider : IContextProvider
    {
        private readonly IGrpcRequestAccessor _requestAccessor;

        /// <summary>
        /// Initializes a new instance of the <see cref="GrpcRequestContextProvider"/> class.
        /// </summary>
        /// <param name="requestAccessor">Optional request accessor.</param>
        public GrpcRequestContextProvider(IGrpcRequestAccessor? requestAccessor = null)
        {
            _requestAccessor = requestAccessor ?? new DefaultGrpcRequestAccessor();
        }

        /// <inheritdoc />
        public string Name => "GrpcRequest";

        /// <inheritdoc />
        public EnrichmentLevel Level => EnrichmentLevel.Standard;

        /// <inheritdoc />
        public void EnrichActivity(Activity activity, EnrichmentOptions options)
        {
            if (activity == null)
                throw new ArgumentNullException(nameof(activity));
            if (options == null)
                throw new ArgumentNullException(nameof(options));

            var request = _requestAccessor.GetCurrentRequest();
            if (request == null)
                return;

            var service = request.Service;
            if (service != null && service.Length > 0)
                activity.SetTag("rpc.service", service);

            var method = request.Method;
            if (method != null && method.Length > 0)
                activity.SetTag("rpc.method", method);

            if (options.MaxLevel >= EnrichmentLevel.Verbose)
            {
                foreach (var metadata in request.Metadata.Where(m => !options.ExcludedHeaders.Contains(m.Key)))
                {
                    activity.SetTag("rpc.metadata." + metadata.Key.ToLowerInvariant(), metadata.Value);
                }
            }
        }

        /// <inheritdoc />
        public void EnrichProperties(IDictionary<string, object> properties, EnrichmentOptions options)
        {
            if (properties == null)
                throw new ArgumentNullException(nameof(properties));
            if (options == null)
                throw new ArgumentNullException(nameof(options));

            var request = _requestAccessor.GetCurrentRequest();
            if (request == null)
                return;

            var service = request.Service;
            if (service != null && service.Length > 0)
                properties["rpc.service"] = service;

            var method = request.Method;
            if (method != null && method.Length > 0)
                properties["rpc.method"] = method;
        }
    }
}
