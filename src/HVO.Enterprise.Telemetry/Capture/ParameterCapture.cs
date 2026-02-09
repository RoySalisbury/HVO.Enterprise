using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using HVO.Enterprise.Telemetry.Proxies;

namespace HVO.Enterprise.Telemetry.Capture
{
    /// <summary>
    /// Default implementation of <see cref="IParameterCapture"/> providing tiered
    /// parameter capture with automatic sensitive data detection, configurable
    /// redaction strategies, and depth/size limits.
    /// </summary>
    public sealed class ParameterCapture : IParameterCapture
    {
        private static readonly HashSet<Type> PrimitiveTypes = new HashSet<Type>
        {
            typeof(bool), typeof(byte), typeof(sbyte),
            typeof(char), typeof(decimal), typeof(double), typeof(float),
            typeof(int), typeof(uint), typeof(long), typeof(ulong),
            typeof(short), typeof(ushort), typeof(string),
            typeof(DateTime), typeof(DateTimeOffset), typeof(TimeSpan),
            typeof(Guid)
        };

        private readonly ConcurrentDictionary<string, SensitiveMatch?> _sensitiveCache =
            new ConcurrentDictionary<string, SensitiveMatch?>(StringComparer.OrdinalIgnoreCase);

        private readonly List<SensitivePattern> _sensitivePatterns = new List<SensitivePattern>();
        private readonly object _patternLock = new object();

        /// <summary>
        /// Initializes a new instance of <see cref="ParameterCapture"/> with default
        /// sensitive data patterns for common PII, authentication, and financial data.
        /// </summary>
        public ParameterCapture()
        {
            RegisterDefaultPatterns();
        }

        /// <summary>
        /// Initializes a new instance of <see cref="ParameterCapture"/>, optionally
        /// skipping default sensitive patterns.
        /// </summary>
        /// <param name="registerDefaults">
        /// When <c>true</c>, registers built-in sensitive patterns. When <c>false</c>,
        /// starts with an empty pattern list.
        /// </param>
        public ParameterCapture(bool registerDefaults)
        {
            if (registerDefaults)
            {
                RegisterDefaultPatterns();
            }
        }

        /// <inheritdoc />
        public object? CaptureParameter(
            string parameterName,
            object? value,
            Type parameterType,
            ParameterCaptureOptions options)
        {
            if (string.IsNullOrEmpty(parameterName))
                throw new ArgumentNullException(nameof(parameterName));
            if (parameterType == null)
                throw new ArgumentNullException(nameof(parameterType));
            if (options == null)
                throw new ArgumentNullException(nameof(options));

            if (options.Level == CaptureLevel.None)
                return null;

            // Check for sensitive data by name.
            if (options.AutoDetectSensitiveData)
            {
                var match = FindSensitiveMatch(parameterName);
                if (match != null)
                    return RedactValue(value, match.Value.Strategy);
            }

            return CaptureValue(value, parameterType, options, 0);
        }

        /// <inheritdoc />
        public IDictionary<string, object?> CaptureParameters(
            ParameterInfo[] parameters,
            object?[] values,
            ParameterCaptureOptions options)
        {
            if (parameters == null)
                throw new ArgumentNullException(nameof(parameters));
            if (values == null)
                throw new ArgumentNullException(nameof(values));
            if (options == null)
                throw new ArgumentNullException(nameof(options));

            var result = new Dictionary<string, object?>(parameters.Length);

            if (options.Level == CaptureLevel.None)
                return result;

            for (int i = 0; i < parameters.Length && i < values.Length; i++)
            {
                var param = parameters[i];
                var value = values[i];
                var name = param.Name ?? $"arg{i}";

                // [SensitiveData] attribute on the parameter takes priority.
                var sensitiveAttr = param.GetCustomAttribute<SensitiveDataAttribute>();
                if (sensitiveAttr != null)
                {
                    result[name] = RedactValue(value, sensitiveAttr.Strategy);
                    continue;
                }

                var captured = CaptureParameter(name, value, param.ParameterType, options);
                result[name] = captured;
            }

            return result;
        }

        /// <inheritdoc />
        public void RegisterSensitivePattern(string pattern, RedactionStrategy strategy)
        {
            if (string.IsNullOrEmpty(pattern))
                throw new ArgumentNullException(nameof(pattern));

            lock (_patternLock)
            {
                _sensitivePatterns.Add(new SensitivePattern(pattern, strategy));
            }

            // Clear cache when patterns change.
            _sensitiveCache.Clear();
        }

        /// <inheritdoc />
        public bool IsSensitive(string parameterName)
        {
            if (string.IsNullOrEmpty(parameterName))
                return false;

            return FindSensitiveMatch(parameterName) != null;
        }

        /// <inheritdoc />
        public RedactionStrategy GetRedactionStrategy(string parameterName)
        {
            if (string.IsNullOrEmpty(parameterName))
                return RedactionStrategy.Mask;

            var match = FindSensitiveMatch(parameterName);
            return match?.Strategy ?? RedactionStrategy.Mask;
        }

        // ─── Value capture ───────────────────────────────────────────────

        private object? CaptureValue(object? value, Type type, ParameterCaptureOptions options, int depth)
        {
            if (value == null)
                return null;

            // Handle primitive types — always captured regardless of depth or level.
            if (IsPrimitiveType(type) || IsPrimitiveType(value.GetType()))
                return CapturePrimitive(value, type, options);

            if (depth >= options.MaxDepth)
                return $"[Max depth {options.MaxDepth} reached]";

            // Minimal level: only primitives and strings.
            if (options.Level == CaptureLevel.Minimal)
                return null;

            // Handle collections (but not string — already handled above).
            if (value is IEnumerable enumerable && type != typeof(string))
                return CaptureCollection(enumerable, options, depth);

            // Standard level: primitives and collections only;
            // complex types get ToString representation.
            if (options.Level == CaptureLevel.Standard)
                return CaptureToString(value, options);

            // Verbose level: capture complex objects with property traversal.
            if (options.Level == CaptureLevel.Verbose)
                return CaptureComplexObject(value, value.GetType(), options, depth);

            return null;
        }

        private object? CapturePrimitive(object value, Type type, ParameterCaptureOptions options)
        {
            // Handle strings with max length.
            if (value is string str)
            {
                if (str.Length > options.MaxStringLength)
                    return str.Substring(0, options.MaxStringLength) + $"... ({str.Length} chars)";
                return str;
            }

            // Handle enums — return string representation.
            if (value.GetType().IsEnum)
                return value.ToString();

            return value;
        }

        private object? CaptureCollection(IEnumerable enumerable, ParameterCaptureOptions options, int depth)
        {
            var items = new List<object?>();
            int count = 0;

            foreach (var item in enumerable)
            {
                if (count >= options.MaxCollectionItems)
                {
                    var total = GetCollectionCount(enumerable);
                    items.Add($"... (total: {total} items)");
                    break;
                }

                var itemType = item?.GetType() ?? typeof(object);
                items.Add(CaptureValue(item, itemType, options, depth + 1));
                count++;
            }

            return items;
        }

        private object? CaptureComplexObject(
            object value,
            Type type,
            ParameterCaptureOptions options,
            int depth)
        {
            // Check for custom serializer first.
            if (options.CustomSerializers != null
                && options.CustomSerializers.TryGetValue(type, out var serializer))
            {
                try
                {
                    return serializer(value);
                }
                catch
                {
                    // Fall through to default capture on serializer failure.
                }
            }

            // Use custom ToString if available and configured.
            if (options.UseCustomToString && HasCustomToString(type))
                return CaptureToString(value, options);

            // Capture properties as a dictionary.
            if (options.CapturePropertyNames)
            {
                var properties = new Dictionary<string, object?>();

                foreach (var prop in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
                {
                    // Skip indexed properties.
                    if (prop.GetIndexParameters().Length > 0)
                        continue;

                    if (!prop.CanRead)
                        continue;

                    // [SensitiveData] on the property.
                    var sensitiveAttr = prop.GetCustomAttribute<SensitiveDataAttribute>();
                    if (sensitiveAttr != null)
                    {
                        properties[prop.Name] = RedactValue(null, sensitiveAttr.Strategy);
                        continue;
                    }

                    // Auto-detect PII on property name.
                    if (options.AutoDetectSensitiveData)
                    {
                        var match = FindSensitiveMatch(prop.Name);
                        if (match != null)
                        {
                            properties[prop.Name] = RedactValue(null, match.Value.Strategy);
                            continue;
                        }
                    }

                    try
                    {
                        var propValue = prop.GetValue(value);
                        properties[prop.Name] = CaptureValue(propValue, prop.PropertyType, options, depth + 1);
                    }
                    catch
                    {
                        properties[prop.Name] = "[Error reading property]";
                    }
                }

                return properties;
            }

            return CaptureToString(value, options);
        }

        private object? CaptureToString(object value, ParameterCaptureOptions options)
        {
            try
            {
                var str = value.ToString();
                if (str != null && str.Length > options.MaxStringLength)
                    return str.Substring(0, options.MaxStringLength) + $"... ({str.Length} chars)";
                return str;
            }
            catch
            {
                return value.GetType().Name;
            }
        }

        // ─── Redaction ───────────────────────────────────────────────────

        internal static object? RedactValue(object? value, RedactionStrategy strategy)
        {
            switch (strategy)
            {
                case RedactionStrategy.Remove:
                    return null;
                case RedactionStrategy.Mask:
                    return "***";
                case RedactionStrategy.Hash:
                    return HashValue(value);
                case RedactionStrategy.Partial:
                    return PartialRedact(value);
                case RedactionStrategy.TypeName:
                    return value?.GetType().Name ?? "null";
                default:
                    return "***";
            }
        }

        internal static string HashValue(object? value)
        {
            if (value == null)
                return "***";

            var text = value.ToString() ?? string.Empty;
            using (var sha = SHA256.Create())
            {
                var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(text));
                // Return first 8 hex chars as a short, non-reversible identifier.
                var sb = new StringBuilder(8);
                for (int i = 0; i < 4; i++)
                {
                    sb.Append(bytes[i].ToString("x2"));
                }
                return sb.ToString();
            }
        }

        internal static string PartialRedact(object? value)
        {
            if (value == null)
                return "***";

            var str = value.ToString();
            if (string.IsNullOrEmpty(str) || str!.Length <= 4)
                return "***";

            return $"{str.Substring(0, 2)}***{str.Substring(str.Length - 2)}";
        }

        // ─── Sensitive pattern matching ──────────────────────────────────

        private SensitiveMatch? FindSensitiveMatch(string name)
        {
            return _sensitiveCache.GetOrAdd(name, n =>
            {
                var lower = n.ToLowerInvariant();
                lock (_patternLock)
                {
                    for (int i = 0; i < _sensitivePatterns.Count; i++)
                    {
                        var pattern = _sensitivePatterns[i];
                        if (lower.Contains(pattern.Pattern))
                            return new SensitiveMatch(pattern.Strategy);
                    }
                }
                return null;
            });
        }

        // ─── Type helpers ────────────────────────────────────────────────

        private static bool IsPrimitiveType(Type type)
        {
            if (type.IsEnum)
                return true;

            var underlyingType = Nullable.GetUnderlyingType(type);
            if (underlyingType != null)
                return PrimitiveTypes.Contains(underlyingType);

            return PrimitiveTypes.Contains(type);
        }

        private static bool HasCustomToString(Type type)
        {
            var toStringMethod = type.GetMethod("ToString", Type.EmptyTypes);
            return toStringMethod?.DeclaringType != typeof(object);
        }

        private static int GetCollectionCount(IEnumerable enumerable)
        {
            if (enumerable is ICollection collection)
                return collection.Count;

            int count = 0;
            foreach (var _ in enumerable)
                count++;
            return count;
        }

        // ─── Default patterns ────────────────────────────────────────────

        private void RegisterDefaultPatterns()
        {
            // Authentication & Authorization
            RegisterSensitivePattern("password", RedactionStrategy.Mask);
            RegisterSensitivePattern("passwd", RedactionStrategy.Mask);
            RegisterSensitivePattern("pwd", RedactionStrategy.Mask);
            RegisterSensitivePattern("secret", RedactionStrategy.Mask);
            RegisterSensitivePattern("token", RedactionStrategy.Mask);
            RegisterSensitivePattern("apikey", RedactionStrategy.Mask);
            RegisterSensitivePattern("api_key", RedactionStrategy.Mask);
            RegisterSensitivePattern("accesskey", RedactionStrategy.Mask);
            RegisterSensitivePattern("privatekey", RedactionStrategy.Mask);
            RegisterSensitivePattern("credential", RedactionStrategy.Mask);
            RegisterSensitivePattern("authorization", RedactionStrategy.Mask);

            // Financial
            RegisterSensitivePattern("creditcard", RedactionStrategy.Hash);
            RegisterSensitivePattern("credit_card", RedactionStrategy.Hash);
            RegisterSensitivePattern("cardnumber", RedactionStrategy.Hash);
            RegisterSensitivePattern("card_number", RedactionStrategy.Hash);
            RegisterSensitivePattern("cvv", RedactionStrategy.Mask);
            RegisterSensitivePattern("cvc", RedactionStrategy.Mask);
            RegisterSensitivePattern("pin", RedactionStrategy.Mask);
            RegisterSensitivePattern("accountnumber", RedactionStrategy.Hash);
            RegisterSensitivePattern("routingnumber", RedactionStrategy.Hash);

            // PII
            RegisterSensitivePattern("ssn", RedactionStrategy.Hash);
            RegisterSensitivePattern("socialsecurity", RedactionStrategy.Hash);
            RegisterSensitivePattern("taxid", RedactionStrategy.Hash);
            RegisterSensitivePattern("driverslicense", RedactionStrategy.Hash);
            RegisterSensitivePattern("passport", RedactionStrategy.Hash);

            // Contact Information
            RegisterSensitivePattern("email", RedactionStrategy.Partial);
            RegisterSensitivePattern("phone", RedactionStrategy.Partial);
            RegisterSensitivePattern("phonenumber", RedactionStrategy.Partial);
            RegisterSensitivePattern("mobile", RedactionStrategy.Partial);
        }

        // ─── Internal types ──────────────────────────────────────────────

        private readonly struct SensitivePattern
        {
            public readonly string Pattern;
            public readonly RedactionStrategy Strategy;

            public SensitivePattern(string pattern, RedactionStrategy strategy)
            {
                Pattern = pattern.ToLowerInvariant();
                Strategy = strategy;
            }
        }

        private readonly struct SensitiveMatch
        {
            public readonly RedactionStrategy Strategy;

            public SensitiveMatch(RedactionStrategy strategy)
            {
                Strategy = strategy;
            }
        }
    }
}
