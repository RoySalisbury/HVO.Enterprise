using System;
using System.Text.Json;

namespace HVO.Common.OneOf;

/// <summary>
/// Represents a discriminated union type that can hold one of several possible types
/// </summary>
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
