// Copyright (c) 2014-2025 Sarin Na Wangkanai, All Rights Reserved.

namespace Wangkanai.Audit;

/// <summary>
/// Represents an interface that provides user-specific auditing functionality
/// to track the creation of an entity by a specific user.
/// </summary>
/// <typeparam name="T">
/// The type of the identifier for the user who created the entity. This type
/// must implement both <see cref="IComparable{T}"/> and <see cref="IEquatable{T}"/>.
/// </typeparam>
public interface IUserCreatedAuditableEntity<T> where T : IComparable<T>, IEquatable<T>
{
   /// <summary>
   /// Gets or sets the identifier of the user who created the entity.
   /// This property is used for audit purposes to track which user was
   /// responsible for the creation of a specific entity.
   /// </summary>
   /// <typeparam name="T">
   /// The type of the identifier for the user who created the entity, which
   /// must implement both <see cref="IComparable{T}"/> and <see cref="IEquatable{T}"/>.
   /// </typeparam>
   /// <remarks>
   /// The <c>CreatedBy</c> property is nullable, allowing scenarios
   /// where the creator information is not available or applicable.
   /// </remarks>
   T? CreatedBy { get; set; }
}