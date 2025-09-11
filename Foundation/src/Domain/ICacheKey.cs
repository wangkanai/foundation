// Copyright (c) 2014-2025 Sarin Na Wangkanai, All Rights Reserved.

namespace Wangkanai.Foundation;

/// <summary>
/// Defines the contract for objects that can generate cache keys.
/// Objects implementing this interface can be used as keys in caching systems
/// by providing a unique string representation of their state.
/// </summary>
public interface ICacheKey
{
   /// <summary>
   /// Generates a cache key string that uniquely represents the state of the object.
   /// The cache key should be deterministic and reflect all the object's properties
   /// that affect its equality comparison.
   /// </summary>
   /// <returns>A string that can be used as a cache key.</returns>
   string GetCacheKey();
}