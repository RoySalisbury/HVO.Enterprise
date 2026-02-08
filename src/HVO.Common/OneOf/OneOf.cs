using System;
using System.Text.Json;

namespace HVO.Common.OneOf;

/// <summary>
/// Discriminated union of two types
/// </summary>
/// <typeparam name="T1">First possible type</typeparam>
/// <typeparam name="T2">Second possible type</typeparam>
/// <remarks>
/// <para><b>default(OneOf&lt;T1, T2&gt;) warning:</b> Because this is a <c>readonly struct</c>,
/// the <c>default</c> value has <c>_index == 0</c>, meaning <c>IsT1 == true</c> and
/// <c>Value == default(T1)</c>. This is a limitation of value types in .NET — there is no way
/// to distinguish "intentionally holds T1" from "uninitialized". Always construct instances via
/// <see cref="FromT1"/> or <see cref="FromT2"/>.</para>
/// </remarks>
public readonly struct OneOf<T1, T2> : IOneOf
{
    private readonly int _index;
    private readonly T1? _value1;
    private readonly T2? _value2;

    private OneOf(int index, T1? value1, T2? value2)
    {
        _index = index;
        _value1 = value1;
        _value2 = value2;
    }

    /// <summary>
    /// Creates a OneOf from the first type
    /// </summary>
    public static OneOf<T1, T2> FromT1(T1 value) => new OneOf<T1, T2>(0, value, default);

    /// <summary>
    /// Creates a OneOf from the second type
    /// </summary>
    public static OneOf<T1, T2> FromT2(T2 value) => new OneOf<T1, T2>(1, default, value);

    /// <summary>
    /// Gets the underlying value
    /// </summary>
    public object? Value => _index switch
    {
        0 => _value1,
        1 => _value2,
        _ => null
    };

    /// <summary>
    /// Gets the type of the underlying value
    /// </summary>
    public Type? ValueType => _index switch
    {
        0 => typeof(T1),
        1 => typeof(T2),
        _ => null
    };

    /// <summary>
    /// Gets the raw JSON element if available
    /// </summary>
    public JsonElement? RawJson => null;

    /// <summary>
    /// Gets whether this contains the first type
    /// </summary>
    public bool IsT1 => _index == 0;

    /// <summary>
    /// Gets whether this contains the second type
    /// </summary>
    public bool IsT2 => _index == 1;

    /// <summary>
    /// Gets the value as the first type
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when the value is not of the first type</exception>
    public T1 AsT1
    {
        get
        {
            if (_index != 0)
                throw new InvalidOperationException($"Cannot access as {typeof(T1).Name}, actual type is {ValueType?.Name}");
            return _value1!;
        }
    }

    /// <summary>
    /// Gets the value as the second type
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when the value is not of the second type</exception>
    public T2 AsT2
    {
        get
        {
            if (_index != 1)
                throw new InvalidOperationException($"Cannot access as {typeof(T2).Name}, actual type is {ValueType?.Name}");
            return _value2!;
        }
    }

    /// <summary>
    /// Pattern matches on the contained value
    /// </summary>
    public TResult Match<TResult>(Func<T1, TResult> f1, Func<T2, TResult> f2)
    {
        if (f1 == null) throw new ArgumentNullException(nameof(f1));
        if (f2 == null) throw new ArgumentNullException(nameof(f2));

        return _index switch
        {
            0 => f1(_value1!),
            1 => f2(_value2!),
            _ => throw new InvalidOperationException()
        };
    }

    /// <summary>
    /// Pattern matches on the contained value (void version)
    /// </summary>
    public void Switch(Action<T1> f1, Action<T2> f2)
    {
        if (f1 == null) throw new ArgumentNullException(nameof(f1));
        if (f2 == null) throw new ArgumentNullException(nameof(f2));

        switch (_index)
        {
            case 0: f1(_value1!); break;
            case 1: f2(_value2!); break;
            default: throw new InvalidOperationException();
        }
    }

    /// <summary>
    /// Implicitly converts a value of type T1 to a OneOf
    /// </summary>
    public static implicit operator OneOf<T1, T2>(T1 value) => FromT1(value);

    /// <summary>
    /// Implicitly converts a value of type T2 to a OneOf
    /// </summary>
    public static implicit operator OneOf<T1, T2>(T2 value) => FromT2(value);

    /// <summary>
    /// Returns a string representation of the contained value
    /// </summary>
    public override string ToString() => Value?.ToString() ?? string.Empty;
}

/// <summary>
/// Discriminated union of three types
/// </summary>
/// <typeparam name="T1">First possible type</typeparam>
/// <typeparam name="T2">Second possible type</typeparam>
/// <typeparam name="T3">Third possible type</typeparam>
/// <remarks>
/// <para><b>default warning:</b> <c>default(OneOf&lt;T1,T2,T3&gt;)</c> has <c>_index == 0</c>,
/// appearing as T1 with a null/default value. Always use the <c>FromT*</c> factory methods.</para>
/// </remarks>
public readonly struct OneOf<T1, T2, T3> : IOneOf
{
    private readonly int _index;
    private readonly T1? _value1;
    private readonly T2? _value2;
    private readonly T3? _value3;

    private OneOf(int index, T1? value1, T2? value2, T3? value3)
    {
        _index = index;
        _value1 = value1;
        _value2 = value2;
        _value3 = value3;
    }

    /// <summary>
    /// Creates a OneOf from the first type
    /// </summary>
    public static OneOf<T1, T2, T3> FromT1(T1 value) => new OneOf<T1, T2, T3>(0, value, default, default);

    /// <summary>
    /// Creates a OneOf from the second type
    /// </summary>
    public static OneOf<T1, T2, T3> FromT2(T2 value) => new OneOf<T1, T2, T3>(1, default, value, default);

    /// <summary>
    /// Creates a OneOf from the third type
    /// </summary>
    public static OneOf<T1, T2, T3> FromT3(T3 value) => new OneOf<T1, T2, T3>(2, default, default, value);

    /// <summary>
    /// Gets the underlying value
    /// </summary>
    public object? Value => _index switch
    {
        0 => _value1,
        1 => _value2,
        2 => _value3,
        _ => null
    };

    /// <summary>
    /// Gets the type of the underlying value
    /// </summary>
    public Type? ValueType => _index switch
    {
        0 => typeof(T1),
        1 => typeof(T2),
        2 => typeof(T3),
        _ => null
    };

    /// <summary>
    /// Gets the raw JSON element if available
    /// </summary>
    public JsonElement? RawJson => null;

    /// <summary>
    /// Gets whether this contains the first type
    /// </summary>
    public bool IsT1 => _index == 0;

    /// <summary>
    /// Gets whether this contains the second type
    /// </summary>
    public bool IsT2 => _index == 1;

    /// <summary>
    /// Gets whether this contains the third type
    /// </summary>
    public bool IsT3 => _index == 2;

    /// <summary>
    /// Gets the value as the first type
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when the value is not of the first type</exception>
    public T1 AsT1
    {
        get
        {
            if (_index != 0)
                throw new InvalidOperationException($"Cannot access as {typeof(T1).Name}, actual type is {ValueType?.Name}");
            return _value1!;
        }
    }

    /// <summary>
    /// Gets the value as the second type
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when the value is not of the second type</exception>
    public T2 AsT2
    {
        get
        {
            if (_index != 1)
                throw new InvalidOperationException($"Cannot access as {typeof(T2).Name}, actual type is {ValueType?.Name}");
            return _value2!;
        }
    }

    /// <summary>
    /// Gets the value as the third type
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when the value is not of the third type</exception>
    public T3 AsT3
    {
        get
        {
            if (_index != 2)
                throw new InvalidOperationException($"Cannot access as {typeof(T3).Name}, actual type is {ValueType?.Name}");
            return _value3!;
        }
    }

    /// <summary>
    /// Pattern matches on the contained value
    /// </summary>
    public TResult Match<TResult>(Func<T1, TResult> f1, Func<T2, TResult> f2, Func<T3, TResult> f3)
    {
        if (f1 == null) throw new ArgumentNullException(nameof(f1));
        if (f2 == null) throw new ArgumentNullException(nameof(f2));
        if (f3 == null) throw new ArgumentNullException(nameof(f3));

        return _index switch
        {
            0 => f1(_value1!),
            1 => f2(_value2!),
            2 => f3(_value3!),
            _ => throw new InvalidOperationException()
        };
    }

    /// <summary>
    /// Pattern matches on the contained value (void version)
    /// </summary>
    public void Switch(Action<T1> f1, Action<T2> f2, Action<T3> f3)
    {
        if (f1 == null) throw new ArgumentNullException(nameof(f1));
        if (f2 == null) throw new ArgumentNullException(nameof(f2));
        if (f3 == null) throw new ArgumentNullException(nameof(f3));

        switch (_index)
        {
            case 0: f1(_value1!); break;
            case 1: f2(_value2!); break;
            case 2: f3(_value3!); break;
            default: throw new InvalidOperationException();
        }
    }

    /// <summary>
    /// Implicitly converts a value of type T1 to a OneOf
    /// </summary>
    public static implicit operator OneOf<T1, T2, T3>(T1 value) => FromT1(value);

    /// <summary>
    /// Implicitly converts a value of type T2 to a OneOf
    /// </summary>
    public static implicit operator OneOf<T1, T2, T3>(T2 value) => FromT2(value);

    /// <summary>
    /// Implicitly converts a value of type T3 to a OneOf
    /// </summary>
    public static implicit operator OneOf<T1, T2, T3>(T3 value) => FromT3(value);

    /// <summary>
    /// Returns a string representation of the contained value
    /// </summary>
    public override string ToString() => Value?.ToString() ?? string.Empty;
}

/// <summary>
/// Discriminated union of four types
/// </summary>
/// <typeparam name="T1">First possible type</typeparam>
/// <typeparam name="T2">Second possible type</typeparam>
/// <typeparam name="T3">Third possible type</typeparam>
/// <typeparam name="T4">Fourth possible type</typeparam>
/// <remarks>
/// <para><b>default warning:</b> <c>default(OneOf&lt;T1,T2,T3,T4&gt;)</c> has <c>_index == 0</c>,
/// appearing as T1 with a null/default value. Always use the <c>FromT*</c> factory methods.</para>
/// </remarks>
public readonly struct OneOf<T1, T2, T3, T4> : IOneOf
{
    private readonly int _index;
    private readonly T1? _value1;
    private readonly T2? _value2;
    private readonly T3? _value3;
    private readonly T4? _value4;

    private OneOf(int index, T1? value1, T2? value2, T3? value3, T4? value4)
    {
        _index = index;
        _value1 = value1;
        _value2 = value2;
        _value3 = value3;
        _value4 = value4;
    }

    /// <summary>
    /// Creates a OneOf from the first type
    /// </summary>
    public static OneOf<T1, T2, T3, T4> FromT1(T1 value) => new OneOf<T1, T2, T3, T4>(0, value, default, default, default);

    /// <summary>
    /// Creates a OneOf from the second type
    /// </summary>
    public static OneOf<T1, T2, T3, T4> FromT2(T2 value) => new OneOf<T1, T2, T3, T4>(1, default, value, default, default);

    /// <summary>
    /// Creates a OneOf from the third type
    /// </summary>
    public static OneOf<T1, T2, T3, T4> FromT3(T3 value) => new OneOf<T1, T2, T3, T4>(2, default, default, value, default);

    /// <summary>
    /// Creates a OneOf from the fourth type
    /// </summary>
    public static OneOf<T1, T2, T3, T4> FromT4(T4 value) => new OneOf<T1, T2, T3, T4>(3, default, default, default, value);

    /// <summary>
    /// Gets the underlying value
    /// </summary>
    public object? Value => _index switch
    {
        0 => _value1,
        1 => _value2,
        2 => _value3,
        3 => _value4,
        _ => null
    };

    /// <summary>
    /// Gets the type of the underlying value
    /// </summary>
    public Type? ValueType => _index switch
    {
        0 => typeof(T1),
        1 => typeof(T2),
        2 => typeof(T3),
        3 => typeof(T4),
        _ => null
    };

    /// <summary>
    /// Gets the raw JSON element if available
    /// </summary>
    public JsonElement? RawJson => null;

    /// <summary>
    /// Gets whether this contains the first type
    /// </summary>
    public bool IsT1 => _index == 0;

    /// <summary>
    /// Gets whether this contains the second type
    /// </summary>
    public bool IsT2 => _index == 1;

    /// <summary>
    /// Gets whether this contains the third type
    /// </summary>
    public bool IsT3 => _index == 2;

    /// <summary>
    /// Gets whether this contains the fourth type
    /// </summary>
    public bool IsT4 => _index == 3;

    /// <summary>
    /// Gets the value as the first type
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when the value is not of the first type</exception>
    public T1 AsT1
    {
        get
        {
            if (_index != 0)
                throw new InvalidOperationException($"Cannot access as {typeof(T1).Name}, actual type is {ValueType?.Name}");
            return _value1!;
        }
    }

    /// <summary>
    /// Gets the value as the second type
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when the value is not of the second type</exception>
    public T2 AsT2
    {
        get
        {
            if (_index != 1)
                throw new InvalidOperationException($"Cannot access as {typeof(T2).Name}, actual type is {ValueType?.Name}");
            return _value2!;
        }
    }

    /// <summary>
    /// Gets the value as the third type
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when the value is not of the third type</exception>
    public T3 AsT3
    {
        get
        {
            if (_index != 2)
                throw new InvalidOperationException($"Cannot access as {typeof(T3).Name}, actual type is {ValueType?.Name}");
            return _value3!;
        }
    }

    /// <summary>
    /// Gets the value as the fourth type
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when the value is not of the fourth type</exception>
    public T4 AsT4
    {
        get
        {
            if (_index != 3)
                throw new InvalidOperationException($"Cannot access as {typeof(T4).Name}, actual type is {ValueType?.Name}");
            return _value4!;
        }
    }

    /// <summary>
    /// Pattern matches on the contained value
    /// </summary>
    public TResult Match<TResult>(Func<T1, TResult> f1, Func<T2, TResult> f2, Func<T3, TResult> f3, Func<T4, TResult> f4)
    {
        if (f1 == null) throw new ArgumentNullException(nameof(f1));
        if (f2 == null) throw new ArgumentNullException(nameof(f2));
        if (f3 == null) throw new ArgumentNullException(nameof(f3));
        if (f4 == null) throw new ArgumentNullException(nameof(f4));

        return _index switch
        {
            0 => f1(_value1!),
            1 => f2(_value2!),
            2 => f3(_value3!),
            3 => f4(_value4!),
            _ => throw new InvalidOperationException()
        };
    }

    /// <summary>
    /// Pattern matches on the contained value (void version)
    /// </summary>
    public void Switch(Action<T1> f1, Action<T2> f2, Action<T3> f3, Action<T4> f4)
    {
        if (f1 == null) throw new ArgumentNullException(nameof(f1));
        if (f2 == null) throw new ArgumentNullException(nameof(f2));
        if (f3 == null) throw new ArgumentNullException(nameof(f3));
        if (f4 == null) throw new ArgumentNullException(nameof(f4));

        switch (_index)
        {
            case 0: f1(_value1!); break;
            case 1: f2(_value2!); break;
            case 2: f3(_value3!); break;
            case 3: f4(_value4!); break;
            default: throw new InvalidOperationException();
        }
    }

    /// <summary>
    /// Implicitly converts a value of type T1 to a OneOf
    /// </summary>
    public static implicit operator OneOf<T1, T2, T3, T4>(T1 value) => FromT1(value);

    /// <summary>
    /// Implicitly converts a value of type T2 to a OneOf
    /// </summary>
    public static implicit operator OneOf<T1, T2, T3, T4>(T2 value) => FromT2(value);

    /// <summary>
    /// Implicitly converts a value of type T3 to a OneOf
    /// </summary>
    public static implicit operator OneOf<T1, T2, T3, T4>(T3 value) => FromT3(value);

    /// <summary>
    /// Implicitly converts a value of type T4 to a OneOf
    /// </summary>
    public static implicit operator OneOf<T1, T2, T3, T4>(T4 value) => FromT4(value);

    /// <summary>
    /// Returns a string representation of the contained value
    /// </summary>
    public override string ToString() => Value?.ToString() ?? string.Empty;
}
