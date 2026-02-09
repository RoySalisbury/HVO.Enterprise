using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace HVO.Enterprise.Telemetry.Proxies
{
    /// <summary>
    /// DispatchProxy that automatically instruments interface methods with telemetry.
    /// Creates an <see cref="IOperationScope"/> around each instrumented method call,
    /// providing automatic timing, Activity creation, parameter capture, and exception tracking.
    /// </summary>
    /// <typeparam name="T">The interface type being proxied.</typeparam>
    public class TelemetryDispatchProxy<T> : DispatchProxy where T : class
    {
        private T? _target;
        private IOperationScopeFactory? _scopeFactory;
        private ILogger? _logger;
        private InstrumentationOptions? _options;
        private readonly ConcurrentDictionary<MethodInfo, MethodInstrumentationInfo> _methodCache = new ConcurrentDictionary<MethodInfo, MethodInstrumentationInfo>();

        // Well-known PII field name patterns for auto-detection.
        private static readonly string[] PiiPatterns = new[]
        {
            "password", "passwd", "pwd",
            "token", "apikey", "api_key",
            "secret", "credential",
            "ssn", "socialsecurity",
            "creditcard", "credit_card", "cardnumber", "card_number",
            "cvv", "cvc",
            "authorization"
        };

        /// <summary>
        /// Initializes the proxy with the target instance and dependencies.
        /// Called by <see cref="TelemetryProxyFactory"/> after <see cref="DispatchProxy.Create{T, TProxy}"/>.
        /// </summary>
        /// <param name="target">The real implementation to delegate to.</param>
        /// <param name="scopeFactory">Factory for creating operation scopes.</param>
        /// <param name="logger">Optional logger for diagnostics.</param>
        /// <param name="options">Instrumentation options.</param>
        internal void Initialize(
            T target,
            IOperationScopeFactory scopeFactory,
            ILogger? logger,
            InstrumentationOptions options)
        {
            _target = target ?? throw new ArgumentNullException(nameof(target));
            _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
            _logger = logger;
            _options = options ?? new InstrumentationOptions();
        }

        /// <summary>
        /// Intercepts every method call on the proxy and routes it through
        /// instrumentation (if configured) or directly to the target.
        /// </summary>
        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            if (targetMethod == null || _target == null || _scopeFactory == null)
            {
                throw new InvalidOperationException("Proxy not initialized.");
            }

            var methodInfo = _methodCache.GetOrAdd(targetMethod, CreateMethodInfo);

            // Fast path: non-instrumented methods skip all telemetry overhead.
            if (!methodInfo.IsInstrumented)
            {
                return InvokeTarget(targetMethod, args);
            }

            // Detect async return type.
            var returnType = targetMethod.ReturnType;
            if (typeof(Task).IsAssignableFrom(returnType))
            {
                return InvokeAsyncMethod(targetMethod, args, methodInfo);
            }

            return InvokeSyncMethod(targetMethod, args, methodInfo);
        }

        // ─── Sync path ──────────────────────────────────────────────────────

        private object? InvokeSyncMethod(
            MethodInfo method,
            object?[]? args,
            MethodInstrumentationInfo methodInfo)
        {
            using (var scope = _scopeFactory!.Begin(methodInfo.OperationName, CreateScopeOptions(methodInfo)))
            {
                CaptureParametersIfEnabled(scope, method, args, methodInfo);

                try
                {
                    var result = InvokeTarget(method, args);

                    if (methodInfo.CaptureReturnValue && result != null)
                    {
                        CaptureReturnValue(scope, result);
                    }

                    scope.Succeed();
                    return result;
                }
                catch (TargetInvocationException ex)
                {
                    var inner = ex.InnerException ?? ex;
                    scope.Fail(inner);
                    throw inner;
                }
                catch (Exception ex)
                {
                    scope.Fail(ex);
                    throw;
                }
            }
        }

        // ─── Async path ─────────────────────────────────────────────────────

        private object InvokeAsyncMethod(
            MethodInfo method,
            object?[]? args,
            MethodInstrumentationInfo methodInfo)
        {
            // Invoke the real method to obtain the Task / Task<T>.
            object? rawResult;
            try
            {
                rawResult = InvokeTarget(method, args);
            }
            catch (TargetInvocationException ex)
            {
                throw ex.InnerException ?? ex;
            }

            if (rawResult == null)
            {
                throw new InvalidOperationException(
                    $"Async method {method.Name} returned null Task.");
            }

            var task = (Task)rawResult;
            var returnType = method.ReturnType;

            // Task<TResult>
            if (returnType.IsGenericType && returnType.GetGenericTypeDefinition() == typeof(Task<>))
            {
                var resultType = returnType.GetGenericArguments()[0];
                var wrapMethod = typeof(TelemetryDispatchProxy<T>)
                    .GetMethod(nameof(WrapGenericTaskAsync), BindingFlags.NonPublic | BindingFlags.Instance)!
                    .MakeGenericMethod(resultType);
                return wrapMethod.Invoke(this, new object[] { task, method, args!, methodInfo })!;
            }

            // Plain Task
            return WrapTaskAsync(task, method, args, methodInfo);
        }

        private async Task WrapTaskAsync(
            Task task,
            MethodInfo method,
            object?[]? args,
            MethodInstrumentationInfo methodInfo)
        {
            using (var scope = _scopeFactory!.Begin(methodInfo.OperationName, CreateScopeOptions(methodInfo)))
            {
                CaptureParametersIfEnabled(scope, method, args, methodInfo);

                try
                {
                    await task.ConfigureAwait(false);
                    scope.Succeed();
                }
                catch (Exception ex)
                {
                    scope.Fail(ex);
                    throw;
                }
            }
        }

        private async Task<TResult> WrapGenericTaskAsync<TResult>(
            Task task,
            MethodInfo method,
            object?[]? args,
            MethodInstrumentationInfo methodInfo)
        {
            using (var scope = _scopeFactory!.Begin(methodInfo.OperationName, CreateScopeOptions(methodInfo)))
            {
                CaptureParametersIfEnabled(scope, method, args, methodInfo);

                try
                {
                    var typedTask = (Task<TResult>)task;
                    var result = await typedTask.ConfigureAwait(false);

                    if (methodInfo.CaptureReturnValue && result != null)
                    {
                        CaptureReturnValue(scope, result);
                    }

                    scope.Succeed();
                    return result;
                }
                catch (Exception ex)
                {
                    scope.Fail(ex);
                    throw;
                }
            }
        }

        // ─── Target invocation ───────────────────────────────────────────────

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private object? InvokeTarget(MethodInfo method, object?[]? args)
        {
            try
            {
                return method.Invoke(_target, args);
            }
            catch (TargetInvocationException ex)
            {
                // Unwrap so callers see the real exception.
                throw ex.InnerException ?? ex;
            }
        }

        // ─── Scope options ───────────────────────────────────────────────────

        private static OperationScopeOptions CreateScopeOptions(MethodInstrumentationInfo info)
        {
            return new OperationScopeOptions
            {
                ActivityKind = info.ActivityKind,
                LogEvents = info.LogEvents,
                LogLevel = info.LogLevel
            };
        }

        // ─── Parameter / return capture ──────────────────────────────────────

        private void CaptureParametersIfEnabled(
            IOperationScope scope,
            MethodInfo method,
            object?[]? args,
            MethodInstrumentationInfo methodInfo)
        {
            if (!methodInfo.CaptureParameters || args == null)
            {
                return;
            }

            var parameters = method.GetParameters();
            for (int i = 0; i < parameters.Length && i < args.Length; i++)
            {
                var param = parameters[i];
                var value = args[i];
                var tagKey = $"param.{param.Name}";

                // Check [SensitiveData] attribute on the parameter.
                var sensitiveAttr = param.GetCustomAttribute<SensitiveDataAttribute>();
                if (sensitiveAttr != null)
                {
                    scope.WithTag(tagKey, ApplyRedaction(value, sensitiveAttr.Strategy));
                    continue;
                }

                // Auto-detect PII by parameter name.
                if (_options!.AutoDetectPii && IsPiiName(param.Name))
                {
                    scope.WithTag(tagKey, "***");
                    continue;
                }

                if (value == null)
                {
                    scope.WithTag(tagKey, null);
                }
                else
                {
                    var captured = CaptureValue(value, _options!.MaxCaptureDepth);
                    scope.WithTag(tagKey, captured);
                }
            }
        }

        private void CaptureReturnValue(IOperationScope scope, object value)
        {
            var captured = CaptureValue(value, _options!.MaxCaptureDepth);
            scope.WithTag("result", captured);
        }

        /// <summary>
        /// Recursively captures a value for tagging, respecting depth and collection limits.
        /// </summary>
        private object? CaptureValue(object? value, int remainingDepth)
        {
            if (value == null)
            {
                return null;
            }

            if (remainingDepth <= 0)
            {
                return value.GetType().Name;
            }

            var type = value.GetType();

            // Primitives, strings, well-known value types → capture as-is.
            if (type.IsPrimitive
                || type == typeof(string)
                || type == typeof(decimal)
                || type == typeof(DateTime)
                || type == typeof(DateTimeOffset)
                || type == typeof(Guid)
                || type == typeof(TimeSpan)
                || type.IsEnum)
            {
                return value;
            }

            // Collections (but not string — already handled above).
            if (value is IEnumerable enumerable)
            {
                var items = new List<object?>();
                int count = 0;
                foreach (var item in enumerable)
                {
                    if (count >= _options!.MaxCollectionItems)
                    {
                        items.Add("...(truncated)");
                        break;
                    }
                    items.Add(CaptureValue(item, remainingDepth - 1));
                    count++;
                }
                return items;
            }

            // Complex types → property dictionary.
            if (_options!.CaptureComplexTypes)
            {
                var dict = new Dictionary<string, object?>();
                foreach (var prop in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
                {
                    if (!prop.CanRead)
                    {
                        continue;
                    }

                    // Skip [SensitiveData]-annotated properties.
                    if (prop.GetCustomAttribute<SensitiveDataAttribute>() != null)
                    {
                        continue;
                    }

                    // Auto PII detection on property names.
                    if (_options.AutoDetectPii && IsPiiName(prop.Name))
                    {
                        continue;
                    }

                    try
                    {
                        var propValue = prop.GetValue(value);
                        dict[prop.Name] = CaptureValue(propValue, remainingDepth - 1);
                    }
                    catch
                    {
                        // Ignore properties that throw on access.
                    }
                }
                return dict;
            }

            return value.ToString();
        }

        // ─── Redaction helpers ───────────────────────────────────────────────

        private static object? ApplyRedaction(object? value, RedactionStrategy strategy)
        {
            switch (strategy)
            {
                case RedactionStrategy.Remove:
                    return null;
                case RedactionStrategy.Hash:
                    return HashValue(value);
                case RedactionStrategy.Mask:
                default:
                    return "***";
            }
        }

        private static string HashValue(object? value)
        {
            if (value == null)
            {
                return "***";
            }

            var text = value.ToString() ?? string.Empty;
            using (var sha = SHA256.Create())
            {
                var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(text));
                // Return first 8 hex chars as a short, non-reversible identifier.
                var sb = new StringBuilder(16);
                for (int i = 0; i < 4; i++)
                {
                    sb.Append(bytes[i].ToString("x2"));
                }
                return sb.ToString();
            }
        }

        private static bool IsPiiName(string? name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return false;
            }

            var lower = name!.ToLowerInvariant();
            for (int i = 0; i < PiiPatterns.Length; i++)
            {
                if (lower.Contains(PiiPatterns[i]))
                {
                    return true;
                }
            }
            return false;
        }

        // ─── Method info builder ─────────────────────────────────────────────

        private MethodInstrumentationInfo CreateMethodInfo(MethodInfo method)
        {
            // [NoTelemetry] on the method wins immediately.
            if (method.GetCustomAttribute<NoTelemetryAttribute>() != null)
            {
                return new MethodInstrumentationInfo { IsInstrumented = false };
            }

            // Check for explicit [InstrumentMethod] on the method.
            var methodAttr = method.GetCustomAttribute<InstrumentMethodAttribute>();
            if (methodAttr != null)
            {
                return new MethodInstrumentationInfo
                {
                    IsInstrumented = true,
                    OperationName = methodAttr.OperationName ?? $"{typeof(T).Name}.{method.Name}",
                    ActivityKind = methodAttr.ActivityKind,
                    CaptureParameters = methodAttr.CaptureParameters,
                    CaptureReturnValue = methodAttr.CaptureReturnValue,
                    LogEvents = methodAttr.LogEvents,
                    LogLevel = methodAttr.LogLevel
                };
            }

            // Check for [InstrumentClass] on the interface.
            var classAttr = typeof(T).GetCustomAttribute<InstrumentClassAttribute>();
            if (classAttr != null)
            {
                var prefix = string.IsNullOrEmpty(classAttr.OperationPrefix)
                    ? typeof(T).Name
                    : classAttr.OperationPrefix!;

                return new MethodInstrumentationInfo
                {
                    IsInstrumented = true,
                    OperationName = $"{prefix}.{method.Name}",
                    ActivityKind = classAttr.ActivityKind,
                    CaptureParameters = classAttr.CaptureParameters,
                    CaptureReturnValue = false,
                    LogEvents = classAttr.LogEvents,
                    LogLevel = LogLevel.Debug
                };
            }

            // No instrumentation attributes found.
            return new MethodInstrumentationInfo { IsInstrumented = false };
        }
    }
}
