// Copyright (c) 2014-2025 Sarin Na Wangkanai, All Rights Reserved.

namespace Wangkanai.Audit;

/// <summary>Represents an auditable entity with comprehensive soft delete capabilities and timestamp tracking.</summary>
/// <typeparam name="T">
/// The type of the identifier for the entity. Must implement <see cref="IComparable{T}"/> and <see cref="IEquatable{T}"/>.
/// </typeparam>
public abstract class SoftDeleteAuditableEntity<T>
   : AuditableEntity<T>, ISoftDeleteAuditableEntity
   where T : IComparable<T>, IEquatable<T>
{
   /// <summary>Gets or sets a value indicating whether the entity has been softly deleted.</summary>
   /// <remarks>
   /// This property acts as a flag to indicate the deletion state of the entity.
   /// When true, the entity is considered deleted and should typically be excluded from normal query operations.
   /// </remarks>
   public bool IsDeleted { get; set; }

   /// <summary>Gets or sets the date and time when the entity was soft deleted.</summary>
   /// <remarks>
   /// This property is nullable to accommodate scenarios where the deletion timestamp may not be available.
   /// It is typically set when the entity is marked as deleted and is essential for audit trails.
   /// </remarks>
   public DateTime? Deleted { get; set; }

   /// <summary>Determines whether the soft delete properties of the entity should be serialized.</summary>
   /// <remarks>
   /// This property acts as a central flag to control the serialization of soft delete related properties.
   /// It can be overridden in derived classes to customize the serialization behavior based on specific requirements.
   /// </remarks>
   public virtual bool ShouldSerializeSoftDeleteProperties
      => ShouldSerializeAuditableProperties;

   /// <summary>Determines whether the IsDeleted flag of the entity should be serialized.</summary>
   /// <returns>A boolean value indicating whether the IsDeleted flag should be included in serialization.</returns>
   public virtual bool ShouldSerializeIsDeleted()
      => ShouldSerializeSoftDeleteProperties;

   /// <summary>Determines whether the Deleted date of the entity should be serialized.</summary>
   /// <returns>A boolean value indicating whether the Deleted date should be included in serialization.</returns>
   public virtual bool ShouldSerializeDeleted()
      => ShouldSerializeSoftDeleteProperties;
}