// Copyright (c) 2014-2025 Sarin Na Wangkanai, All Rights Reserved.

namespace Wangkanai.Foundation;

/// <summary>
/// Defines the contract for an entity with a unique identifier.
/// All entities in the domain must implement this interface to ensure they have a primary key.
/// </summary>
/// <typeparam name="TKey">The type of the unique identifier for the entity.</typeparam>
public interface IEntity<TKey>
   where TKey : IEquatable<TKey>, IComparable<TKey>
{
   /// <summary>
   /// Gets or sets the unique identifier for the entity.
   /// This property is used to uniquely identify an instance of the entity within the domain.
   /// </summary>
   TKey Id { get; set; }

   /// <summary>
   /// Determines whether the entity is transient, meaning it has not been assigned a valid identifier.
   /// An entity is considered transient if its identifier equals the default value for its type.
   /// </summary>
   /// <returns>true if the entity is transient; otherwise, false.</returns>
   bool IsTransient();
}