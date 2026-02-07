using System;
using System.Text.Json;

namespace HVO.Common.Options;

/// <summary>
/// Represents an optional value that may or may not be present
/// </summary>
/// <typeparam name="T">The type of the value, which must be non-null</typeparam>
public readonly struct Option<T> where T : notnull
{
    /// <summary>
    /// Gets the contained value, or null if no value is present
    /// </summary>
    public T? Value { get; }

    /// <summary>
    /// Gets a value indicating whether this option contains a value
    /// </summary>
    public bool HasValue { get; }

    /// <summary>
    /// Gets the raw JSON element if the value could not be deserialized to the expected type
    /// </summary>
    public JsonElement? RawJson { get; }

    /// <summary>
    /// Initializes a new instance of the Option struct
    /// </summary>
    /// <param name="value">The optional value</param>
    /// <param name="rawJson">The raw JSON element for fallback scenarios</param>
    public Option(T? value, JsonElement? rawJson = null)
    {
        Value = value;
        HasValue = value is not null;
        RawJson = rawJson;
    }

    /// <summary>
    /// Creates an empty Option with no value
    /// </summary>
    /// <param name="rawJson">Optional raw JSON for fallback scenarios</param>
    /// <returns>An Option with no value</returns>
    public static Option<T> None(JsonElement? rawJson = null) => new Option<T>(default, rawJson);

    /// <summary>
    /// Returns a string representation of the option
    /// </summary>
    /// <returns>The value's string representation, or "&lt;None&gt;" if no value is present</returns>
    public override string ToString() => HasValue ? Value?.ToString() ?? "" : "<None>";
}
