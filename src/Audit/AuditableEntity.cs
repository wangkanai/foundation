// Copyright (c) 2014-2025 Sarin Na Wangkanai, All Rights Reserved.

namespace Wangkanai.Audit;

/// <summary>Represents an auditable entity with properties for tracking creation and modification timestamps.</summary>
/// <typeparam name="T">
/// The type of the identifier for the entity. Must implement <see cref="IComparable{T}"/> and <see cref="IEquatable{T}"/>.
/// </typeparam>
public class AuditableEntity<T> : Entity<T>, IAuditableEntity
   where T : IComparable<T>, IEquatable<T>
{
   public DateTime? Created { get; set; }

   public DateTime? Updated { get; set; }

   public DateTime? Deleted { get; set; }
}