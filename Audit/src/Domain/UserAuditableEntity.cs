// Copyright (c) 2014-2025 Sarin Na Wangkanai, All Rights Reserved.

namespace Wangkanai.Audit.Domain;

/// <summary>
/// Represents an abstract base class for auditable entities that captures user-related information during create and update operations.
/// This class extends the <see cref="AuditableEntity{T}"/> class and adheres to the <see cref="IUserAuditableEntity"/> interface.
/// </summary>
/// <typeparam name="T">
/// The type of the entity identifier, which must implement <see cref="IComparable{T}"/> and <see cref="IEquatable{T}"/>.
/// </typeparam>
public abstract class UserAuditableEntity<T> : AuditableEntity<T>, IUserAuditableEntity
   where T : IComparable<T>, IEquatable<T>
{
   public string? CreatedBy { get; set; }

   public string? UpdatedBy { get; set; }

   public string? DeletedBy { get; set; }
}