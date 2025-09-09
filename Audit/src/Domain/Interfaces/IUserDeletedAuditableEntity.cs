// Copyright (c) 2014-2025 Sarin Na Wangkanai, All Rights Reserved.

namespace Wangkanai.Audit;

/// <summary>
/// Represents an interface for entities that record user-related deletion details
/// using a default string identifier for the user who performed the action.
/// </summary>
public interface IUserDeletedAuditableEntity : IUserDeletedAuditableEntity<string>;

/// <summary>
/// Defines an interface for entities that track deletion actions,
/// including the user responsible for performing the delete operation.
/// </summary>
/// <typeparam name="T">
/// The type of the identifier used to track the user performing the deletion.
/// Must implement <see cref="IComparable{T}"/> and <see cref="IEquatable{T}"/>.
/// </typeparam>
public interface IUserDeletedAuditableEntity<T> where T : IComparable<T>, IEquatable<T>
{
   /// <summary>
   /// Gets or sets the identifier of the user who performed the delete operation.
   /// </summary>
   /// <remarks>
   /// This property is typically used in auditing scenarios to track which user deleted the entity.
   /// The type of the identifier is determined by the implementing entity, which can implement generic interfaces.
   /// </remarks>
   T? DeletedBy { get; set; }
}