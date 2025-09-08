// Copyright (c) 2014-2025 Sarin Na Wangkanai, All Rights Reserved.

namespace Wangkanai.Audit;

/// <summary>
/// Defines an interface for entities that support audit tracking of the user who last updated them.
/// </summary>
/// <typeparam name="T">
/// The type used to represent the identifier of the user performing the update. The type must implement
/// <see cref="IComparable{T}"/> and <see cref="IEquatable{T}"/>.
/// </typeparam>
public interface IUserUpdatedAuditableEntity<T> where T : IComparable<T>, IEquatable<T>
{
   /// <summary>
   /// Gets or sets the identifier of the user who last updated the entity.
   /// This property is used for audit purposes to track modifications made to the entity.
   /// </summary>
   /// <typeparam name="T">
   /// The type representing the user identifier, which must implement <see cref="IComparable{T}"/> and <see cref="IEquatable{T}"/>.
   /// </typeparam>
   T? UpdatedBy { get; set; }
}