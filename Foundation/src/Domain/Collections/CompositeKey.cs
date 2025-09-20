// Copyright (c) 2014-2025 Sarin Na Wangkanai, All Rights Reserved.

namespace Wangkanai.Foundation.Collections;

/// <summary>
/// Represents a composite key for grouping operations with multiple values.
/// </summary>
public record CompositeKey : IEquatable<CompositeKey>
{
    private readonly object?[] _values;

    /// <summary>
    /// Gets the values that make up this composite key.
    /// </summary>
    public IReadOnlyList<object?> Values => _values;

    /// <summary>
    /// Initializes a new instance of the <see cref="CompositeKey"/> class.
    /// </summary>
    /// <param name="values">The values that make up the composite key.</param>
    public CompositeKey(params object?[] values)
    {
        ArgumentNullException.ThrowIfNull(values);
        _values = values;
    }

    /// <summary>
    /// Determines whether the specified composite key is equal to the current composite key.
    /// </summary>
    public virtual bool Equals(CompositeKey? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        if (_values.Length != other._values.Length) return false;

        for (int i = 0; i < _values.Length; i++)
        {
            if (!Equals(_values[i], other._values[i]))
                return false;
        }

        return true;
    }

    /// <summary>
    /// Returns the hash code for this composite key.
    /// </summary>
    public override int GetHashCode()
    {
        unchecked
        {
            int hash = 17;
            foreach (var value in _values)
            {
                hash = hash * 31 + (value?.GetHashCode() ?? 0);
            }
            return hash;
        }
    }

    /// <summary>
    /// Returns a string representation of the composite key.
    /// </summary>
    public override string ToString()
    {
        return $"[{string.Join(", ", _values.Select(v => v?.ToString() ?? "null"))}]";
    }
}