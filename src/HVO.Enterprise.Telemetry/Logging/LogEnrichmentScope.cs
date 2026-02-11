using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace HVO.Enterprise.Telemetry.Logging
{
    /// <summary>
    /// A log scope that wraps enrichment key-value pairs and provides a human-readable
    /// <see cref="ToString"/> representation for console formatters while exposing the
    /// pairs for structured providers (Serilog, Application Insights, etc.).
    /// </summary>
    /// <remarks>
    /// <para>The default ASP.NET Core console formatter calls <c>scope.ToString()</c> when
    /// rendering scope information. A plain <c>Dictionary&lt;string, object&gt;</c> produces
    /// the type name (e.g., <c>System.Collections.Generic.Dictionary`2[…]</c>), which is
    /// not useful. This wrapper produces output like:</para>
    /// <code>CorrelationId:abc123, TraceId:def456, SpanId:789012</code>
    /// <para>Structured logging providers that understand
    /// <see cref="IEnumerable{T}">IEnumerable&lt;KeyValuePair&lt;string, object&gt;&gt;</see>
    /// can still iterate the pairs to extract individual properties.</para>
    /// </remarks>
    internal sealed class LogEnrichmentScope : IReadOnlyList<KeyValuePair<string, object?>>
    {
        private readonly KeyValuePair<string, object?>[] _entries;
        private string? _cachedToString;

        /// <summary>
        /// Initializes a new <see cref="LogEnrichmentScope"/> from the given enrichment dictionary.
        /// </summary>
        /// <param name="enrichmentData">
        /// The enrichment key-value pairs. The dictionary is copied into an array so the
        /// scope is immutable after creation.
        /// </param>
        internal LogEnrichmentScope(Dictionary<string, object?> enrichmentData)
        {
            if (enrichmentData == null)
                throw new ArgumentNullException(nameof(enrichmentData));

            _entries = new KeyValuePair<string, object?>[enrichmentData.Count];
            int i = 0;
            foreach (var kvp in enrichmentData)
            {
                _entries[i++] = kvp;
            }
        }

        /// <inheritdoc />
        public int Count => _entries.Length;

        /// <inheritdoc />
        public KeyValuePair<string, object?> this[int index] => _entries[index];

        /// <summary>
        /// Returns a human-readable representation of the enrichment data, formatted as
        /// <c>Key1:Value1, Key2:Value2</c>. The result is cached for subsequent calls.
        /// </summary>
        public override string ToString()
        {
            if (_cachedToString != null)
                return _cachedToString;

            if (_entries.Length == 0)
            {
                _cachedToString = string.Empty;
                return _cachedToString;
            }

            var sb = new StringBuilder(64);
            for (int i = 0; i < _entries.Length; i++)
            {
                if (i > 0)
                    sb.Append(", ");

                sb.Append(_entries[i].Key);
                sb.Append(':');
                sb.Append(_entries[i].Value ?? "null");
            }

            _cachedToString = sb.ToString();
            return _cachedToString;
        }

        /// <inheritdoc />
        public IEnumerator<KeyValuePair<string, object?>> GetEnumerator()
        {
            for (int i = 0; i < _entries.Length; i++)
            {
                yield return _entries[i];
            }
        }

        /// <inheritdoc />
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
