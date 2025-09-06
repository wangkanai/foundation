// Copyright (c) 2014-2025 Sarin Na Wangkanai, All Rights Reserved.

namespace Wangkanai.Audit.Models;

public class SoftDeleteEntity
   : KeyGuidEntity, ISoftDeleteAuditableEntity
{
   /// <summary>Gets or sets the date and time when the entity was created.</summary>
   public DateTime? Created { get; set; }

   /// <summary>Gets or sets the date and time when the entity was last updated.</summary>
   public DateTime? Updated { get; set; }

   /// <summary>Gets or sets a value indicating whether the entity has been soft deleted.</summary>
   public bool IsDeleted { get; set; }

   /// <summary>Gets or sets the date and time when the entity was soft deleted.</summary>
   public DateTime? Deleted { get; set; }
}