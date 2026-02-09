using System;
using System.Collections.Generic;
using System.Reflection;

namespace HVO.Enterprise.Telemetry.Capture
{
    /// <summary>
    /// Extension methods for integrating parameter capture with <see cref="IOperationScope"/>.
    /// </summary>
    public static class ParameterCaptureExtensions
    {
        /// <summary>
        /// Cached default <see cref="ParameterCapture"/> instance to avoid repeated allocations.
        /// </summary>
        private static readonly Lazy<IParameterCapture> DefaultCapture =
            new Lazy<IParameterCapture>(() => new ParameterCapture());

        /// <summary>
        /// Captures method parameters and adds them as tags to the operation scope.
        /// Each parameter is added with a "param.{name}" tag key.
        /// </summary>
        /// <param name="scope">The operation scope to tag.</param>
        /// <param name="parameters">Method parameter metadata.</param>
        /// <param name="values">Actual parameter values.</param>
        /// <param name="parameterCapture">The capture implementation. When <c>null</c>, a default instance is used.</param>
        /// <param name="options">Capture options. When <c>null</c>, default options are used.</param>
        /// <returns>The scope for chaining.</returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="scope"/>, <paramref name="parameters"/>, or <paramref name="values"/> is <c>null</c>.
        /// </exception>
        public static IOperationScope CaptureParameters(
            this IOperationScope scope,
            ParameterInfo[] parameters,
            object?[] values,
            IParameterCapture? parameterCapture = null,
            ParameterCaptureOptions? options = null)
        {
            if (scope == null) throw new ArgumentNullException(nameof(scope));
            if (parameters == null) throw new ArgumentNullException(nameof(parameters));
            if (values == null) throw new ArgumentNullException(nameof(values));

            parameterCapture = parameterCapture ?? DefaultCapture.Value;
            options = options ?? ParameterCaptureOptions.Default;

            var captured = parameterCapture.CaptureParameters(parameters, values, options);

            foreach (var kvp in captured)
            {
                scope.WithTag($"param.{kvp.Key}", kvp.Value);
            }

            return scope;
        }

        /// <summary>
        /// Captures a return value and adds it as a "result" tag on the operation scope.
        /// </summary>
        /// <param name="scope">The operation scope to tag.</param>
        /// <param name="returnValue">The return value to capture.</param>
        /// <param name="returnType">The declared return type of the method.</param>
        /// <param name="parameterCapture">The capture implementation. When <c>null</c>, a default instance is used.</param>
        /// <param name="options">Capture options. When <c>null</c>, default options are used.</param>
        /// <returns>The scope for chaining.</returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="scope"/> or <paramref name="returnType"/> is <c>null</c>.
        /// </exception>
        public static IOperationScope CaptureReturnValue(
            this IOperationScope scope,
            object? returnValue,
            Type returnType,
            IParameterCapture? parameterCapture = null,
            ParameterCaptureOptions? options = null)
        {
            if (scope == null) throw new ArgumentNullException(nameof(scope));
            if (returnType == null) throw new ArgumentNullException(nameof(returnType));

            parameterCapture = parameterCapture ?? DefaultCapture.Value;
            options = options ?? ParameterCaptureOptions.Default;

            var captured = parameterCapture.CaptureParameter("return", returnValue, returnType, options);
            if (captured != null)
            {
                scope.WithTag("result", captured);
            }

            return scope;
        }
    }
}
