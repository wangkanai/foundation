// Copyright (c) 2014-2025 Sarin Na Wangkanai, All Rights Reserved.

using System.Collections;

namespace Wangkanai.Foundation.Extensions;

/// <summary>
/// Extension methods for Type operations commonly used in domain objects.
/// </summary>
public static class TypeExtensions
{
   /// <summary>
   /// Determines whether the specified type can be assigned from a generic list type.
   /// This method checks if the type implements IEnumerable (excluding strings)
   /// and can be used in enumerable contexts.
   /// </summary>
   /// <param name="type">The type to check.</param>
   /// <returns>true if the type can be assigned from a generic list; otherwise, false.</returns>
   public static bool IsAssignableFromGenericList(this Type type)
   {
      type.ThrowIfNull();
      return type != typeof(string) && typeof(IEnumerable).IsAssignableFrom(type);
   }
}