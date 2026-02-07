using System;

namespace HVO.Common.OneOf;

/// <summary>
/// Attribute to mark classes for NamedOneOf source generation
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false)]
public sealed class NamedOneOfAttribute : Attribute
{
    /// <summary>
    /// Gets the defined cases for the OneOf type as (name, type) tuples
    /// </summary>
    public (string Name, Type Type)[] Cases { get; }

    /// <summary>
    /// Initializes a new instance of the NamedOneOfAttribute with case definitions
    /// </summary>
    /// <param name="args">Pairs of (string name, Type type) arguments defining the cases</param>
    /// <exception cref="ArgumentNullException">Thrown when args is null</exception>
    /// <exception cref="ArgumentException">Thrown when arguments are not in valid (name, type) pairs</exception>
    public NamedOneOfAttribute(params object[] args)
    {
        if (args == null) throw new ArgumentNullException(nameof(args));
        if (args.Length % 2 != 0)
            throw new ArgumentException("Arguments must be in (name, type) pairs");

        Cases = new (string, Type)[args.Length / 2];
        for (int i = 0; i < args.Length; i += 2)
        {
            if (args[i] is not string name)
                throw new ArgumentException($"Expected string for case name at index {i}");
            if (args[i + 1] is not Type type)
                throw new ArgumentException($"Expected Type for case type at index {i + 1}");
            Cases[i / 2] = (name, type);
        }
    }
}
