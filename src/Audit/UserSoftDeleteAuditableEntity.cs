// Copyright (c) 2014-2025 Sarin Na Wangkanai, All Rights Reserved.

using System.ComponentModel.DataAnnotations;

namespace Wangkanai.Audit;

/// <summary>Represents an auditable entity with comprehensive soft delete capabilities including user tracking.</summary>
/// <typeparam name="T">The type of the identifier for the entity. Must implement <see cref="IComparable{T}"/> and <see cref="IEquatable{T}"/>.</typeparam>
public abstract class UserSoftDeleteAuditableEntity<T>
   : SoftDeleteAuditableEntity<T>, IUserSoftDeleteAuditable
   where T : IComparable<T>, IEquatable<T>
{
   /// <summary>Determines whether the CreatedBy property should be serialized.</summary>
   /// <returns>A boolean value indicating whether the CreatedBy property should be included in serialization.</returns>
   public virtual bool ShouldSerializeCreatedBy()
      => ShouldSerializeAuditableProperties;

   /// <summary>Determines whether the UpdatedBy property should be serialized.</summary>
   /// <returns>A boolean value indicating whether the UpdatedBy property should be included in serialization.</returns>
   public virtual bool ShouldSerializeUpdatedBy()
      => ShouldSerializeAuditableProperties;

   /// <summary>Determines whether the DeletedBy property should be serialized.</summary>
   /// <returns>A boolean value indicating whether the DeletedBy property should be included in serialization.</returns>
   public virtual bool ShouldSerializeDeletedBy()
      => ShouldSerializeSoftDeleteProperties;

   #region IUserAuditable Members

   [StringLength(128)]
   public string? CreatedBy { get; set; }

   [StringLength(128)]
   public string? UpdatedBy { get; set; }

   #endregion

   #region IUserSoftDeleteAuditable Members

   /// <summary>Gets or sets the identifier of the user who soft deleted the entity.</summary>
   /// <remarks>This property tracks the user responsible for soft deleting the entity. It remains null until the entity is marked as deleted and is essential for maintaining audit accountability.</remarks>
   [StringLength(128)]
   public string? DeletedBy { get; set; }

   #endregion
}