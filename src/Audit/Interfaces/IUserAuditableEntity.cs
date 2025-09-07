// Copyright (c) 2014-2025 Sarin Na Wangkanai, All Rights Reserved.

namespace Wangkanai.Audit;

/// <summary>
/// Defines an interface for auditable entities that include user-related metadata.
/// This interface extends the <see cref="IAuditableEntity"/> interface and provides additional properties
/// to track the user responsible for creating, updating, and deleting the entity.
/// </summary>
public interface IUserAuditableEntity : IAuditableEntity
{
   /// <summary>
   /// Gets or sets the identifier of the user who created the entity.
   /// This property is used for tracking the user responsible for creating the entity
   /// in an auditable system.
   /// </summary>
   string? CreatedBy { get; set; }

   /// <summary>
   /// Gets or sets the identifier of the user who last updated the entity.
   /// This property is used for tracking the user responsible for the most recent modification
   /// in an auditable system.
   /// </summary>
   string? UpdatedBy { get; set; }

   /// <summary>
   /// Gets or sets the identifier of the user who deleted the entity.
   /// This property is used for tracking the user responsible for the deletion event
   /// as part of the audit process.
   /// </summary>
   string? DeletedBy { get; set; }
}