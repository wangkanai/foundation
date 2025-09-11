// Copyright (c) 2014-2025 Sarin Na Wangkanai, All Rights Reserved.

namespace Wangkanai.Foundation;

/// <summary>
/// Defines the contract for entities that support audit tracking.
/// Entities implementing this interface will automatically track creation and modification timestamps.
/// </summary>
public interface IAuditable
{
   /// <summary>
   /// Gets or sets the timestamp when the entity was created.
   /// </summary>
   DateTime CreatedAt { get; set; }

   /// <summary>
   /// Gets or sets the timestamp when the entity was last modified.
   /// </summary>
   DateTime? ModifiedAt { get; set; }

   /// <summary>
   /// Gets or sets the identifier of the user who created the entity.
   /// </summary>
   string? CreatedBy { get; set; }

   /// <summary>
   /// Gets or sets the identifier of the user who last modified the entity.
   /// </summary>
   string? ModifiedBy { get; set; }
}

/// <summary>
/// Defines the contract for entities that support audit tracking with strongly-typed user identifiers.
/// </summary>
/// <typeparam name="TUserKey">The type of the user identifier.</typeparam>
public interface IAuditable<TUserKey> 
   where TUserKey : IEquatable<TUserKey>, IComparable<TUserKey>
{
   /// <summary>
   /// Gets or sets the timestamp when the entity was created.
   /// </summary>
   DateTime CreatedAt { get; set; }

   /// <summary>
   /// Gets or sets the timestamp when the entity was last modified.
   /// </summary>
   DateTime? ModifiedAt { get; set; }

   /// <summary>
   /// Gets or sets the identifier of the user who created the entity.
   /// </summary>
   TUserKey? CreatedBy { get; set; }

   /// <summary>
   /// Gets or sets the identifier of the user who last modified the entity.
   /// </summary>
   TUserKey? ModifiedBy { get; set; }
}