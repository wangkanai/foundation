// Copyright (c) 2014-2025 Sarin Na Wangkanai, All Rights Reserved.

namespace Wangkanai.Audit;

/// <summary>Represents an entity that tracks the deletion date and time.</summary>
/// <remarks>
/// The <see cref="IDeletedEntity"/> interface defines a contract for entities that require the ability to capture and store the timestamp of their deletion.
/// This is commonly used in soft delete scenarios where entities are marked as deleted rather than physically removed from the database.
/// </remarks>
public interface IDeletedEntity
{
   /// <summary>Gets or sets the date and time when the entity was deleted.</summary>
   /// <remarks>
   /// The <see cref="Deleted"/> property is used to track the deletion timestamp of the entity.
   /// It is a nullable <see cref="DateTime"/> that remains null until the entity is marked as deleted.
   /// When set, it indicates the exact moment when the soft delete operation occurred.
   /// This property is essential for audit trails and soft delete functionality.
   /// </remarks>
   DateTime? Deleted { get; set; }
}