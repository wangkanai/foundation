// Copyright (c) 2014-2025 Sarin Na Wangkanai, All Rights Reserved.

using Wangkanai.Foundation;

namespace Wangkanai.Audit;



/// <summary>Represents an auditable entity with properties for tracking creation and modification timestamps using DateTimeOffset for timezone-aware auditing.</summary>
/// <typeparam name="T">
/// The type of the identifier for the entity. Must implement <see cref="IComparable{T}"/> and <see cref="IEquatable{T}"/>.
/// </typeparam>
public class AuditableEntity<T> : Entity<T>, IAuditableEntity<T>
   where T : IComparable<T>, IEquatable<T>
{
   /// <summary>Gets or sets the DateTimeOffset when the entity was created, providing timezone-aware auditing.</summary>
   public DateTimeOffset? Created { get; set; }

   /// <summary>Gets or sets the DateTimeOffset when the entity was last updated, providing timezone-aware auditing.</summary>
   public DateTimeOffset? Updated { get; set; }

   /// <summary>Gets or sets the DateTimeOffset when the entity was deleted (soft delete), providing timezone-aware auditing.</summary>
   public DateTimeOffset? Deleted { get; set; }
}