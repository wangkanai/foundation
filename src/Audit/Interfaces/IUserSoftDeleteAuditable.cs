// Copyright (c) 2014-2025 Sarin Na Wangkanai, All Rights Reserved.

namespace Wangkanai.Audit;

/// <summary>Represents an auditable entity that includes user tracking for soft delete operations.</summary>
/// <remarks>
/// This interface extends the <see cref="ISoftDeleteAuditable"/> interface to include properties for
/// tracking the user who performed the soft delete operation. It provides complete audit trail functionality
/// for entities that support soft delete with user accountability.
/// </remarks>
public interface IUserSoftDeleteAuditable
   : ISoftDeleteAuditable, IUserAuditable
{
   /// <summary>Gets or sets the identifier of the user who soft deleted the entity.</summary>
   /// <remarks>
   /// The <see cref="DeletedBy"/> property is used to track the user responsible for soft deleting this entity.
   /// This is essential for audit purposes and ensuring accountability in systems where user actions are monitored.
   /// The property remains null until the entity is soft deleted.
   /// </remarks>
   string? DeletedBy { get; set; }
}