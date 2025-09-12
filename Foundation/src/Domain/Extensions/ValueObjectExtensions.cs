// Copyright (c) 2014-2025 Sarin Na Wangkanai, All Rights Reserved.

using System.Reflection;

namespace Wangkanai.Foundation.Extensions;

public static class ValueObjectExtensions
{
   /// <summary>
   /// Generates an alternative cache key string that uniquely represents the state of the value object.
   /// The cache key is constructed by concatenating the string representations of the object's equality components,
   /// separated by a pipe ('|') character. If a component is a string, it is enclosed in single quotes.
   /// If a component implements <see cref="ICacheKey"/>, its own cache key is used instead of its string representation.
   /// </summary>
   /// <param name="valueObject">The value object to generate a cache key for.</param>
   /// <returns>The cache key as a string.</returns>
   public static string GenerateCacheKey(this ValueObject valueObject)
   {
      var keyValues = valueObject.GetEqualityComponentsOptimized()
                                 .Select(x => x is string ? $"'{x}'" : x)
                                 .Select(x => x is ICacheKey cacheKey ? cacheKey.GetCacheKey() : x?.ToString());

      return string.Join("|", keyValues);
   }

   /// <summary>
   /// Creates a shallow copy of the value object.
   /// This method uses the MemberwiseClone technique to produce a new instance of the same type, with all fields copied directly.
   /// </summary>
   /// <typeparam name="T">The type of value object.</typeparam>
   /// <param name="valueObject">The value object to clone.</param>
   /// <returns>A new instance of the value object with the same property values as the current instance.</returns>
   public static T Clone<T>(this T valueObject) where T : ValueObject
      => (T)valueObject.Clone();

   /// <summary>
   /// Returns a formatted string representation of the value object, including its property names and their corresponding values.
   /// The string is formatted as a key-value pair collection enclosed in braces, where each property name is followed by its value.
   /// </summary>
   /// <param name="valueObject">The value object to format.</param>
   /// <returns>A string representing the value object, consisting of its property names and values.</returns>
   public static string ToFormattedString(this ValueObject valueObject)
   {
      var properties = valueObject.GetProperties();
      var components = valueObject.GetEqualityComponentsOptimized().ToArray();

      var pairs = properties.Zip(components, (prop, val) => $"{prop.Name}: {val}");
      return $"{{{string.Join(", ", pairs)}}}";
   }

   /// <summary>
   /// Compares two value objects and returns a detailed comparison result.
   /// </summary>
   /// <param name="valueObject">The first value object to compare.</param>
   /// <param name="other">The second value object to compare.</param>
   /// <returns>A tuple containing whether they are equal and which properties differ if not.</returns>
   public static (bool IsEqual, IEnumerable<string> DifferingProperties) DetailedCompare(
      this ValueObject valueObject, 
      ValueObject? other)
   {
      if (ReferenceEquals(valueObject, other))
         return (true, Enumerable.Empty<string>());

      if (other is null || valueObject.GetType() != other.GetType())
         return (false, new[] { "Type" });

      var properties = valueObject.GetProperties().ToArray();
      var differingProps = new List<string>();

      foreach (var prop in properties)
      {
         var value1 = prop.GetValue(valueObject);
         var value2 = prop.GetValue(other);

         if (!Equals(value1, value2))
            differingProps.Add(prop.Name);
      }

      return (differingProps.Count == 0, differingProps);
   }

   /// <summary>
   /// Converts the value object to a dictionary representation of its properties.
   /// </summary>
   /// <param name="valueObject">The value object to convert.</param>
   /// <returns>A dictionary containing property names as keys and their values.</returns>
   public static IDictionary<string, object?> ToDictionary(this ValueObject valueObject)
   {
      var properties = valueObject.GetProperties();
      return properties.ToDictionary(
         prop => prop.Name, 
         prop => prop.GetValue(valueObject)
      );
   }

}