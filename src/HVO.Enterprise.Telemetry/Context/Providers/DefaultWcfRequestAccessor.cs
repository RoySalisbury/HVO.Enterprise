using System;
using System.Reflection;

namespace HVO.Enterprise.Telemetry.Context.Providers
{
    /// <summary>
    /// Default WCF request accessor using async-local storage or OperationContext when available.
    /// </summary>
    internal sealed class DefaultWcfRequestAccessor : IWcfRequestAccessor
    {
        private static volatile bool _wcfChecked;
        private static Type? _contextType;
        private static PropertyInfo? _currentProperty;

        /// <inheritdoc />
        public WcfRequestInfo? GetCurrentRequest()
        {
            var asyncLocalRequest = WcfRequestContextStore.Current;
            if (asyncLocalRequest != null)
                return asyncLocalRequest;

            return TryGetOperationContext();
        }

        private static WcfRequestInfo? TryGetOperationContext()
        {
            // Cache type lookups on first call
            if (!_wcfChecked)
            {
                _contextType = Type.GetType("System.ServiceModel.OperationContext, System.ServiceModel");
                if (_contextType != null)
                {
                    _currentProperty = _contextType.GetProperty("Current", BindingFlags.Static | BindingFlags.Public);
                }
                _wcfChecked = true;
            }

            if (_contextType == null)
                return null;

            var context = _currentProperty?.GetValue(null, null);
            if (context == null)
                return null;

            var incomingHeaders = GetPropertyValue(context, _contextType, "IncomingMessageHeaders");
            var action = GetPropertyString(incomingHeaders, "Action");

            var endpoint = GetNestedPropertyString(context, "EndpointDispatcher", "EndpointAddress", "Uri");
            var binding = GetNestedPropertyString(context, "EndpointDispatcher", "ChannelDispatcher", "BindingName");

            return new WcfRequestInfo
            {
                Action = action,
                Endpoint = endpoint,
                Binding = binding
            };
        }

        private static object? GetPropertyValue(object target, Type targetType, string propertyName)
        {
            var property = targetType.GetProperty(propertyName);
            return property != null ? property.GetValue(target, null) : null;
        }

        private static string? GetPropertyString(object? target, string propertyName)
        {
            if (target == null)
                return null;

            var property = target.GetType().GetProperty(propertyName);
            var value = property != null ? property.GetValue(target, null) : null;
            return value != null ? value.ToString() : null;
        }

        private static string? GetNestedPropertyString(object target, string first, string second, string third)
        {
            var firstValue = GetPropertyValue(target, target.GetType(), first);
            if (firstValue == null)
                return null;

            var secondValue = GetPropertyValue(firstValue, firstValue.GetType(), second);
            if (secondValue == null)
                return null;

            var thirdValue = GetPropertyValue(secondValue, secondValue.GetType(), third);
            return thirdValue != null ? thirdValue.ToString() : null;
        }
    }
}
