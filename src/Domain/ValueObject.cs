// Copyright (c) 2014-2025 Sarin Na Wangkanai, All Rights Reserved.

using System.Collections;
using System.Collections.Concurrent;
using System.Reflection;

namespace Wangkanai.Domain;

/// <summary>
/// Represents an abstract base class for value objects in the domain-driven design context.
/// A value object is an immutable conceptual object that is compared based on its property values rather than a unique identity.
/// </summary>
/// <remarks>
/// Value objects provide a way to encapsulate and model domain concepts with specific attributes, ensuring immutability and
/// equality based on their state. The class implements <see cref="IValueObject"/> for domain definition, <see cref="ICacheKey"/>
/// to enable caching based on object states, and <see cref="ICloneable"/> to support shallow copying of the object.
/// </remarks>
public abstract class ValueObject : IValueObject, ICacheKey, ICloneable
{
   private static readonly ConcurrentDictionary<Type, IReadOnlyCollection<PropertyInfo>> _typeProperties = new();

   /// <summary>
   /// Generates a cache key string that uniquely represents the state of the value object.
   /// The cache key is constructed by concatenating the string representations of the object's equality components,
   /// separated by a pipe ('|') character. If a component is a string, it is enclosed in single quotes.
   /// If a component implements <see cref="ICacheKey"/>, its own cache key is used instead of its string representation.
   /// This method ensures that the cache key reflects the value object's properties, allowing for effective caching strategies.
   /// </summary>
   /// <returns>The cache key as a string.</returns>
   public virtual string GetCacheKey()
   {
      var keyValues = GetEqualityComponents()
                     .Select(x => x is string ? $"'{x}'" : x)
                     .Select(x => x is ICacheKey cacheKey ? cacheKey.GetCacheKey() : x?.ToString());

      return string.Join("|", keyValues);
   }

   /// <summary>
   /// Determines whether the specified object is equal to the current value object.
   /// Equality is based on the values of the properties defined in the value object.
   /// Two value objects are considered equal if they are of the same type and their equality components
   /// (as defined by the <see cref="GetEqualityComponents"/> method) are equal.
   /// This method overrides the default <see cref="object.Equals(object?)"/> implementation to provide
   /// value-based equality comparison.
   /// </summary>
   /// <param name="obj">The object to compare with the current value object.</param>
   /// <returns>true if the specified object is equal to the current value object; otherwise, false.</returns>
   public override bool Equals(object? obj)
   {
      if (ReferenceEquals(this, obj))
         return true;

      if (ReferenceEquals(null, obj))
         return false;

      if (GetType() != obj.GetType())
         return false;

      var other = obj as ValueObject;
      return other is not null && GetEqualityComponents().SequenceEqual(other.GetEqualityComponents());
   }

   /// <summary>
   /// Calculates and returns the hash code for the current value object.
   /// The hash code is computed based on the equality components of the object,
   /// ensuring that objects with identical property values produce the same hash code.
   /// This implementation uses a combination of prime numbers (17 and 23) to reduce collision probability
   /// when hashing the equality components. If an equality component is null, its contribution to the hash code is zero.
   /// </summary>
   /// <returns>The hash code as an integer value.</returns>
   public override int GetHashCode()
   {
      unchecked
      {
         return GetEqualityComponents()
           .Aggregate(17, (current, obj) => current * 23 + (obj?.GetHashCode() ?? 0));
      }
   }

   /// <summary>
   /// Compares two value object instances for equality.
   /// This operator provides a convenient way to check if two value objects are equal based on their state.
   /// </summary>
   /// <param name="left">The first value object to compare.</param>
   /// <param name="right">The second value object to compare.</param>
   /// <returns>true if the specified value objects are equal; otherwise, false.</returns>
   public static bool operator ==(ValueObject left, ValueObject right)
      => Equals(left, right);

   /// <summary>
   /// Compares two value objects for inequality.
   /// This operator provides a concise way to determine whether two value object instances are not equal.
   /// The comparison is based on the objects' equality components, ensuring that two value objects
   /// with different property values are correctly identified as unequal.
   /// </summary>
   /// <param name="left">The first value object to compare.</param>
   /// <param name="right">The second value object to compare.</param>
   /// <returns>true if the specified value objects are not equal; otherwise, false.</returns>
   public static bool operator !=(ValueObject left, ValueObject right)
      => !Equals(left, right);

   public override string ToString()
      => $"{{{string.Join(", ", GetProperties().Select(f => $"{f.Name}: {f.GetValue(this)}"))}}}";

   public virtual IEnumerable<PropertyInfo> GetProperties()
      => _typeProperties.GetOrAdd(GetType(), t => t.GetTypeInfo().GetProperties(BindingFlags.Instance | BindingFlags.Public))
                       .OrderBy(p => p.Name)
                       .ToList();

   protected virtual IEnumerable<object> GetEqualityComponents()
   {
      foreach (var property in GetProperties())
      {
         var value = property.GetValue(this);
         if (value is null)
            yield return null!;
         else
         {
            var valueType = value.GetType();
            if (valueType.IsAssignableFromGenericList())
            {
               yield return '[';
               foreach (var child in (IEnumerable)value)
                  yield return child;

               yield return ']';
            }
            else
               yield return value;
         }
      }
   }

   public object Clone()
      => MemberwiseClone();
}