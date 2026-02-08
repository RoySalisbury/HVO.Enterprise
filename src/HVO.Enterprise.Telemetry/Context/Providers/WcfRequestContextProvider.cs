using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace HVO.Enterprise.Telemetry.Context.Providers
{
    /// <summary>
    /// Enriches telemetry with WCF request context.
    /// </summary>
    public sealed class WcfRequestContextProvider : IContextProvider
    {
        private readonly IWcfRequestAccessor _requestAccessor;

        /// <summary>
        /// Initializes a new instance of the <see cref="WcfRequestContextProvider"/> class.
        /// </summary>
        /// <param name="requestAccessor">Optional request accessor.</param>
        public WcfRequestContextProvider(IWcfRequestAccessor? requestAccessor = null)
        {
            _requestAccessor = requestAccessor ?? new DefaultWcfRequestAccessor();
        }

        /// <inheritdoc />
        public string Name => "WcfRequest";

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

            var action = request.Action;
            if (action != null && action.Length > 0)
                activity.SetTag("wcf.action", action);

            var endpoint = request.Endpoint;
            if (endpoint != null && endpoint.Length > 0)
                activity.SetTag("wcf.endpoint", endpoint);

            var binding = request.Binding;
            if (binding != null && binding.Length > 0)
                activity.SetTag("wcf.binding", binding);
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

            var action = request.Action;
            if (action != null && action.Length > 0)
                properties["wcf.action"] = action;

            var endpoint = request.Endpoint;
            if (endpoint != null && endpoint.Length > 0)
                properties["wcf.endpoint"] = endpoint;

            var binding = request.Binding;
            if (binding != null && binding.Length > 0)
                properties["wcf.binding"] = binding;
        }
    }
}
