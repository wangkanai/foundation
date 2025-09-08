// Copyright (c) 2014-2025 Sarin Na Wangkanai, All Rights Reserved.

namespace Wangkanai.Audit;

/// <summary>Represents an entity that tracks the deletion date and time with timezone awareness.</summary>
/// <remarks>
/// The <see cref="IDeletedEntity"/> interface defines a contract for entities that require the ability to capture and
/// store the timezone-aware timestamp of their deletion. This is commonly used in soft delete scenarios where entities are marked as
/// deleted rather than physically removed from the database.
/// </remarks>
public interface IDeletedEntity
{
   /// <summary>Gets or sets the DateTimeOffset when the entity was deleted (soft delete), providing timezone-aware auditing.</summary>
   /// <remarks>
   /// The <see cref="Deleted"/> property is used to track the deletion timestamp of the entity with timezone precision.
   /// It is a nullable <see cref="DateTimeOffset"/> that remains null until the entity is marked as deleted.
   /// When set, it indicates the exact moment when the soft delete operation occurred, including timezone information.
   /// This property is essential for audit trails and soft delete functionality across different timezones.
   /// </remarks>
   DateTimeOffset? Deleted { get; set; }
}