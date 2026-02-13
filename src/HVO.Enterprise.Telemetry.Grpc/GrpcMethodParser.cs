using System;

namespace HVO.Enterprise.Telemetry.Grpc
{
    /// <summary>
    /// Parses gRPC method full names into service and method components.
    /// </summary>
    internal static class GrpcMethodParser
    {
        /// <summary>
        /// Parses a gRPC full method name into its service and method components.
        /// </summary>
        /// <param name="fullMethod">
        /// The full gRPC method name in the format <c>"/package.ServiceName/MethodName"</c>.
        /// </param>
        /// <returns>A tuple of (serviceName, methodName).</returns>
        /// <remarks>
        /// gRPC full method format is: <c>"/package.ServiceName/MethodName"</c>.
        /// If the format cannot be parsed, returns <c>("unknown", "unknown")</c>.
        /// </remarks>
        internal static (string Service, string Method) Parse(string fullMethod)
        {
            if (string.IsNullOrEmpty(fullMethod) || !fullMethod.StartsWith("/", StringComparison.Ordinal))
                return ("unknown", "unknown");

            var trimmed = fullMethod.Substring(1); // Remove leading "/"
            var slashIndex = trimmed.IndexOf('/');
            if (slashIndex < 0)
                return ("unknown", fullMethod);

            return (trimmed.Substring(0, slashIndex), trimmed.Substring(slashIndex + 1));
        }

        /// <summary>
        /// Parses a gRPC <see cref="global::Grpc.Core.Method{TRequest, TResponse}"/> into its
        /// service and method components.
        /// </summary>
        /// <typeparam name="TRequest">The request type.</typeparam>
        /// <typeparam name="TResponse">The response type.</typeparam>
        /// <param name="method">The gRPC method descriptor.</param>
        /// <returns>A tuple of (serviceName, methodName).</returns>
        internal static (string Service, string Method) Parse<TRequest, TResponse>(
            global::Grpc.Core.Method<TRequest, TResponse> method)
            where TRequest : class
            where TResponse : class
        {
            return (method.ServiceName, method.Name);
        }
    }
}
