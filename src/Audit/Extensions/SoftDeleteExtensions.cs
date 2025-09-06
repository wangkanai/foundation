// Copyright (c) 2014-2025 Sarin Na Wangkanai, All Rights Reserved.

namespace Wangkanai.Audit;

/// <summary>Provides extension methods for soft delete operations on auditable entities.</summary>
public static class SoftDeleteExtensions
{
   /// <summary>Marks an entity as soft deleted by setting the IsDeleted flag to true and recording the deletion timestamp.</summary>
   /// <typeparam name="T">The type of the entity that implements <see cref="ISoftDeleteAuditable"/>.</typeparam>
   /// <param name="entity">The entity to mark as deleted.</param>
   /// <param name="deletedAt">The optional timestamp when the entity was deleted. If not provided, uses the current UTC time.</param>
   /// <returns>The same entity instance for method chaining.</returns>
   public static T MarkAsDeleted<T>(this T entity, DateTime? deletedAt = null)
      where T : ISoftDeleteAuditable
   {
      entity.IsDeleted = true;
      entity.Deleted   = deletedAt ?? DateTime.UtcNow;
      return entity;
   }

   /// <summary>
   /// Marks an entity as soft deleted with user tracking by setting the IsDeleted flag to true, recording the deletion timestamp,
   /// and tracking the user who performed the deletion.
   /// </summary>
   /// <typeparam name="T">The type of the entity that implements <see cref="IUserSoftDeleteAuditable"/>.</typeparam>
   /// <param name="entity">The entity to mark as deleted.</param>
   /// <param name="deletedBy">The identifier of the user who performed the deletion.</param>
   /// <param name="deletedAt">The optional timestamp when the entity was deleted. If not provided, uses the current UTC time.</param>
   /// <returns>The same entity instance for method chaining.</returns>
   public static T MarkAsDeleted<T>(this T entity, string deletedBy, DateTime? deletedAt = null)
      where T : IUserSoftDeleteAuditable
   {
      entity.IsDeleted = true;
      entity.Deleted   = deletedAt ?? DateTime.UtcNow;
      entity.DeletedBy = deletedBy;
      return entity;
   }

   /// <summary>
   /// Restores a soft deleted entity by setting the IsDeleted flag to false and clearing the deletion-related properties.
   /// </summary>
   /// <typeparam name="T">The type of the entity that implements <see cref="ISoftDeleteAuditable"/>.</typeparam>
   /// <param name="entity">The entity to restore.</param>
   /// <returns>The same entity instance for method chaining.</returns>
   public static T Restore<T>(this T entity)
      where T : ISoftDeleteAuditable
   {
      entity.IsDeleted = false;
      entity.Deleted   = null;

      // Clear user tracking if the entity supports it
      if (entity is IUserSoftDeleteAuditable userEntity)
         userEntity.DeletedBy = null;

      return entity;
   }

   /// <summary>Determines whether an entity has been soft deleted.</summary>
   /// <typeparam name="T">The type of the entity that implements <see cref="ISoftDeletable"/>.</typeparam>
   /// <param name="entity">The entity to check.</param>
   /// <returns>True if the entity is soft deleted; otherwise, false.</returns>
   public static bool IsSoftDeleted<T>(this T entity)
      where T : ISoftDeletable
      => entity.IsDeleted;

   /// <summary>Determines whether an entity is active (not soft deleted).</summary>
   /// <typeparam name="T">The type of the entity that implements <see cref="ISoftDeletable"/>.</typeparam>
   /// <param name="entity">The entity to check.</param>
   /// <returns>True if the entity is active; otherwise, false.</returns>
   public static bool IsActive<T>(this T entity)
      where T : ISoftDeletable
      => !entity.IsDeleted;
}