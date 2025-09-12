// Copyright (c) 2014-2025 Sarin Na Wangkanai, All Rights Reserved.

namespace Wangkanai.Foundation.Extensions;

/// <summary>Extension methods for generating cache keys from types.</summary>
internal static class CacheKeyExtensions
{
   /// <summary>Generates a cache key for the specified type.</summary>
   /// <param name="type">The type for which to generate a cache key.</param>
   /// <returns>A string representing the cache key for the type.</returns>
   internal static string GetCacheKey(this Type type)
      => type.FullName ?? type.Name;
}