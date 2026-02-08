using System;
using System.Text.Json;

namespace HVO.Common.OneOf;

/// <summary>
/// Represents a discriminated union type that can hold one of several possible types
/// </summary>
/// <remarks>
/// <para><b>RawJson coupling:</b> The <see cref="RawJson"/> property creates a compile-time
/// dependency on <c>System.Text.Json</c> (<c>JsonElement?</c>). This is an intentional design
/// trade-off: it enables round-trip JSON deserialization scenarios where the concrete type is
/// unknown at parse time, allowing callers to access the raw JSON for deferred or manual
/// deserialization. Factoring it into a separate interface was considered but rejected because
/// every IOneOf consumer in HVO already references System.Text.Json, and splitting the
/// interface would complicate the common deserialization code path for no practical benefit.
/// If a future consumer needs IOneOf without a System.Text.Json dependency, this decision
/// should be revisited.</para>
/// </remarks>
public interface IOneOf
{
    /// <summary>
    /// Gets the underlying value
    /// </summary>
    object? Value { get; }

    /// <summary>
    /// Gets the type of the underlying value
    /// </summary>
    Type? ValueType { get; }

    /// <summary>
    /// Gets the raw JSON element if available (fallback for unknown types)
    /// </summary>
    JsonElement? RawJson { get; }
}
