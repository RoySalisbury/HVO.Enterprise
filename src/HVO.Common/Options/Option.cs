using System;
using System.Text.Json;

namespace HVO.Common.Options;

/// <summary>
/// Represents an optional value that may or may not be present
/// </summary>
/// <typeparam name="T">The type of the value, which must be non-null</typeparam>
/// <remarks>
/// <para><c>default(Option&lt;T&gt;)</c> is safe and equivalent to <see cref="None"/> (HasValue == false).</para>
/// <para><b>RawJson coupling:</b> The <see cref="RawJson"/> property creates a compile-time
/// dependency on <c>System.Text.Json</c>. See <see cref="HVO.Common.OneOf.IOneOf"/> remarks for the design
/// rationale. This enables fallback access to the original JSON when deserialization to
/// <typeparamref name="T"/> fails or is deferred.</para>
/// </remarks>
public readonly struct Option<T> where T : notnull
{
    private readonly T? _value;
    private readonly bool _hasValue;

    /// <summary>
    /// Gets the contained value, or null if no value is present
    /// </summary>
    public T? Value => _value;

    /// <summary>
    /// Gets a value indicating whether this option contains a value
    /// </summary>
    public bool HasValue => _hasValue;

    /// <summary>
    /// Gets the raw JSON element if the value could not be deserialized to the expected type
    /// </summary>
    public JsonElement? RawJson { get; }

    /// <summary>
    /// Initializes a new instance of the Option struct with a value
    /// </summary>
    /// <param name="value">The value to wrap</param>
    /// <param name="rawJson">The raw JSON element for fallback scenarios</param>
    public Option(T value, JsonElement? rawJson = null)
    {
        if (value == null) throw new ArgumentNullException(nameof(value));
        _value = value;
        _hasValue = true;
        RawJson = rawJson;
    }

    /// <summary>
    /// Initializes a new instance of the Option struct from a nullable value
    /// </summary>
    /// <param name="value">The optional value</param>
    /// <param name="hasValue">Whether the option has a value</param>
    /// <param name="rawJson">The raw JSON element for fallback scenarios</param>
    private Option(T? value, bool hasValue, JsonElement? rawJson = null)
    {
        _value = value;
        _hasValue = hasValue;
        RawJson = rawJson;
    }

    /// <summary>
    /// Creates an empty Option with no value
    /// </summary>
    /// <param name="rawJson">Optional raw JSON for fallback scenarios</param>
    /// <returns>An Option with no value</returns>
    public static Option<T> None(JsonElement? rawJson = null) => new Option<T>(default(T), false, rawJson);

    /// <summary>
    /// Returns a string representation of the option
    /// </summary>
    /// <returns>The value's string representation, or "&lt;None&gt;" if no value is present</returns>
    public override string ToString() => HasValue ? Value?.ToString() ?? "" : "<None>";
}
