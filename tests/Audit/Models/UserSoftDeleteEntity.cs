// Copyright (c) 2014-2025 Sarin Na Wangkanai, All Rights Reserved.

using System.ComponentModel.DataAnnotations;

namespace Wangkanai.Audit.Models;

public class UserSoftDeleteEntity
   : KeyGuidEntity, IUserSoftDeleteAuditable
{
   /// <summary>Gets or sets the date and time when the entity was created.</summary>
   public DateTime? Created { get; set; }

   /// <summary>Gets or sets the date and time when the entity was last updated.</summary>
   public DateTime? Updated { get; set; }

   /// <summary>Gets or sets the identifier of the user who created the entity.</summary>
   [StringLength(128)]
   public string? CreatedBy { get; set; }

   /// <summary>Gets or sets the identifier of the user who last updated the entity.</summary>
   [StringLength(128)]
   public string? UpdatedBy { get; set; }

   /// <summary>Gets or sets a value indicating whether the entity has been soft deleted.</summary>
   public bool IsDeleted { get; set; }

   /// <summary>Gets or sets the date and time when the entity was soft deleted.</summary>
   public DateTime? Deleted { get; set; }

   /// <summary>Gets or sets the identifier of the user who soft deleted the entity.</summary>
   [StringLength(128)]
   public string? DeletedBy { get; set; }
}