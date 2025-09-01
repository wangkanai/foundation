// Copyright (c) 2014-2025 Sarin Na Wangkanai, All Rights Reserved.

using System.Collections;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using Wangkanai.Domain.Extensions;

namespace Wangkanai.Domain;

/// <summary>
/// Hybrid high-performance ValueObject with intelligent fast-path detection.
/// Automatically chooses optimal strategy based on property complexity.
/// </summary>
public abstract class ValueObjectHybrid : IValueObject, ICacheKey, ICloneable
{
    private static readonly ConcurrentDictionary<Type, IEqualityStrategy> _strategies = new();
    private static readonly ConcurrentDictionary<Type, PropertyInfo[]> _properties = new();

    public virtual string GetCacheKey()
    {
        var strategy = GetStrategy();
        var components = strategy.GetComponents(this);
        var keyValues = components.Select(FormatCacheValue);
        return string.Join("|", keyValues);
    }

    public object Clone() => MemberwiseClone();

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(this, obj)) return true;
        if (obj is null || GetType() != obj.GetType()) return false;

        var strategy = GetStrategy();
        return strategy.AreEqual(this, (ValueObjectHybrid)obj);
    }

    public override int GetHashCode()
    {
        var strategy = GetStrategy();
        return strategy.GetHashCode(this);
    }

    public static bool operator ==(ValueObjectHybrid? left, ValueObjectHybrid? right) => Equals(left, right);
    public static bool operator !=(ValueObjectHybrid? left, ValueObjectHybrid? right) => !Equals(left, right);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private IEqualityStrategy GetStrategy()
    {
        return _strategies.GetOrAdd(GetType(), CreateOptimalStrategy);
    }

    /// <summary>
    /// Creates the optimal equality strategy based on property analysis.
    /// Uses fast compiled delegates for simple types, reflection for complex types.
    /// </summary>
    private static IEqualityStrategy CreateOptimalStrategy(Type type)
    {
        var properties = GetProperties(type);
        var complexity = AnalyzeComplexity(properties);

        return complexity.Score switch
        {
            <= 0.3f => new CompiledStrategy(type, properties),  // Fast path: simple properties
            <= 0.7f => new CachedReflectionStrategy(type, properties),  // Medium: cached reflection
            _ => new ReflectionStrategy(type, properties)  // Complex: full reflection
        };
    }

    private static PropertyInfo[] GetProperties(Type type)
    {
        return _properties.GetOrAdd(type, t =>
            t.GetProperties(BindingFlags.Instance | BindingFlags.Public)
             .Where(p => p.CanRead && p.GetMethod!.IsPublic)
             .OrderBy(p => p.Name)
             .ToArray());
    }

    private static ComplexityAnalysis AnalyzeComplexity(PropertyInfo[] properties)
    {
        float score = 0f;
        var reasons = new List<string>();

        foreach (var prop in properties)
        {
            // Simple value types are fastest
            if (prop.PropertyType.IsValueType && !prop.PropertyType.IsGenericType)
                continue;

            // Strings are fast
            if (prop.PropertyType == typeof(string))
                continue;

            // Enumerables add complexity
            if (typeof(IEnumerable).IsAssignableFrom(prop.PropertyType) && prop.PropertyType != typeof(string))
            {
                score += 0.3f;
                reasons.Add($"Enumerable: {prop.Name}");
            }

            // Nullable types add slight complexity
            if (Nullable.GetUnderlyingType(prop.PropertyType) != null)
            {
                score += 0.1f;
                reasons.Add($"Nullable: {prop.Name}");
            }

            // Complex objects add significant complexity
            if (!prop.PropertyType.IsValueType && prop.PropertyType != typeof(string) && 
                !typeof(IEnumerable).IsAssignableFrom(prop.PropertyType))
            {
                score += 0.4f;
                reasons.Add($"Complex: {prop.Name}");
            }
        }

        return new ComplexityAnalysis(score, reasons);
    }

    private static string FormatCacheValue(object? value)
    {
        return value switch
        {
            null => "null",
            string s => $"'{s}'",
            ICacheKey ck => ck.GetCacheKey(),
            _ => value.ToString() ?? "null"
        };
    }

    // Legacy compatibility
    protected virtual IEnumerable<object> GetEqualityComponents()
    {
        var strategy = GetStrategy();
        return strategy.GetComponents(this);
    }

    public override string ToString()
    {
        var properties = GetProperties(GetType());
        var strategy = GetStrategy();
        var values = strategy.GetComponents(this).ToArray();
        
        var pairs = properties.Zip(values, (prop, val) => $"{prop.Name}: {val}");
        return $"{{{string.Join(", ", pairs)}}}";
    }

    private record ComplexityAnalysis(float Score, List<string> Reasons);

    #region Strategy Implementations

    private interface IEqualityStrategy
    {
        object?[] GetComponents(ValueObjectHybrid instance);
        bool AreEqual(ValueObjectHybrid left, ValueObjectHybrid right);
        int GetHashCode(ValueObjectHybrid instance);
    }

    /// <summary>Fastest strategy using pre-compiled expression trees.</summary>
    private class CompiledStrategy : IEqualityStrategy
    {
        private readonly Func<object, object?[]> _accessor;
        private readonly Func<object, int> _hashCodeFunc;

        public CompiledStrategy(Type type, PropertyInfo[] properties)
        {
            _accessor = BuildCompiledAccessor(type, properties);
            _hashCodeFunc = BuildHashCodeFunction(type, properties);
        }

        public object?[] GetComponents(ValueObjectHybrid instance) => _accessor(instance);

        public bool AreEqual(ValueObjectHybrid left, ValueObjectHybrid right)
        {
            var leftComponents = GetComponents(left);
            var rightComponents = GetComponents(right);
            return leftComponents.SequenceEqual(rightComponents);
        }

        public int GetHashCode(ValueObjectHybrid instance) => _hashCodeFunc(instance);

        private static Func<object, object?[]> BuildCompiledAccessor(Type type, PropertyInfo[] properties)
        {
            var instanceParam = Expression.Parameter(typeof(object), "instance");
            var typedInstance = Expression.Convert(instanceParam, type);

            var propertyExpressions = properties.Select(prop =>
            {
                var propertyAccess = Expression.Property(typedInstance, prop);
                return Expression.Convert(propertyAccess, typeof(object));
            });

            var arrayInit = Expression.NewArrayInit(typeof(object), propertyExpressions);
            var lambda = Expression.Lambda<Func<object, object?[]>>(arrayInit, instanceParam);
            return lambda.Compile();
        }

        private static Func<object, int> BuildHashCodeFunction(Type type, PropertyInfo[] properties)
        {
            var instanceParam = Expression.Parameter(typeof(object), "instance");
            var typedInstance = Expression.Convert(instanceParam, type);

            var hash = Expression.Variable(typeof(int), "hash");
            var assign17 = Expression.Assign(hash, Expression.Constant(17));

            var statements = new List<Expression> { assign17 };

            foreach (var prop in properties)
            {
                var propertyAccess = Expression.Property(typedInstance, prop);
                var boxed = Expression.Convert(propertyAccess, typeof(object));
                
                var getHashMethod = typeof(object).GetMethod("GetHashCode")!;
                var nullCheck = Expression.Condition(
                    Expression.Equal(boxed, Expression.Constant(null)),
                    Expression.Constant(0),
                    Expression.Call(boxed, getHashMethod));

                var multiply = Expression.Multiply(hash, Expression.Constant(23));
                var add = Expression.Add(multiply, nullCheck);
                var update = Expression.Assign(hash, add);
                statements.Add(update);
            }

            statements.Add(hash);
            var block = Expression.Block(new[] { hash }, statements);
            var lambda = Expression.Lambda<Func<object, int>>(block, instanceParam);
            return lambda.Compile();
        }
    }

    /// <summary>Medium performance strategy with cached reflection calls.</summary>
    private class CachedReflectionStrategy : IEqualityStrategy
    {
        private readonly PropertyInfo[] _properties;
        private readonly Func<object, object?>[] _accessors;

        public CachedReflectionStrategy(Type type, PropertyInfo[] properties)
        {
            _properties = properties;
            _accessors = properties.Select(CreateCachedAccessor).ToArray();
        }

        public object?[] GetComponents(ValueObjectHybrid instance)
        {
            return _accessors.Select(accessor => accessor(instance)).ToArray();
        }

        public bool AreEqual(ValueObjectHybrid left, ValueObjectHybrid right)
        {
            for (int i = 0; i < _accessors.Length; i++)
            {
                var leftValue = _accessors[i](left);
                var rightValue = _accessors[i](right);
                if (!Equals(leftValue, rightValue)) return false;
            }
            return true;
        }

        public int GetHashCode(ValueObjectHybrid instance)
        {
            unchecked
            {
                var hash = 17;
                foreach (var accessor in _accessors)
                {
                    hash = hash * 23 + (accessor(instance)?.GetHashCode() ?? 0);
                }
                return hash;
            }
        }

        private static Func<object, object?> CreateCachedAccessor(PropertyInfo property)
        {
            return instance => property.GetValue(instance);
        }
    }

    /// <summary>Fallback strategy for complex scenarios requiring full reflection.</summary>
    private class ReflectionStrategy : IEqualityStrategy
    {
        private readonly PropertyInfo[] _properties;

        public ReflectionStrategy(Type type, PropertyInfo[] properties)
        {
            _properties = properties;
        }

        public object?[] GetComponents(ValueObjectHybrid instance)
        {
            var result = new List<object?>();
            foreach (var property in _properties)
            {
                var value = property.GetValue(instance);
                if (value is IEnumerable enumerable && value is not string)
                {
                    result.Add('[');
                    foreach (var item in enumerable)
                        result.Add(item);
                    result.Add(']');
                }
                else
                {
                    result.Add(value);
                }
            }
            return result.ToArray();
        }

        public bool AreEqual(ValueObjectHybrid left, ValueObjectHybrid right)
        {
            var leftComponents = GetComponents(left);
            var rightComponents = GetComponents(right);
            return leftComponents.SequenceEqual(rightComponents);
        }

        public int GetHashCode(ValueObjectHybrid instance)
        {
            unchecked
            {
                return GetComponents(instance)
                    .Aggregate(17, (current, obj) => current * 23 + (obj?.GetHashCode() ?? 0));
            }
        }
    }

    #endregion
}