// Copyright (c) 2014-2025 Sarin Na Wangkanai, All Rights Reserved.

namespace Wangkanai.Audit;

/// <summary>Represents an entity that supports soft delete functionality.</summary>
/// <remarks>
/// The <see cref="ISoftDeletable"/> interface defines a contract for entities that can be marked as deleted
/// without being physically removed from the database. This enables soft delete scenarios where
/// entities can be "deleted" while preserving data integrity and enabling recovery if needed.
/// </remarks>
public interface ISoftDeletable
{
   /// <summary>Gets or sets a value indicating whether the entity has been soft deleted.</summary>
   /// <remarks>
   /// The <see cref="IsDeleted"/> property acts as a flag to indicate the deletion state of the entity.
   /// When <c>true</c>, the entity is considered deleted and should typically be excluded from
   /// normal query operations. When <c>false</c>, the entity is active and available for use.
   /// This property is fundamental to implementing soft delete patterns in domain-driven design.
   /// </remarks>
   bool IsDeleted { get; set; }
}