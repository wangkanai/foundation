// Copyright (c) 2014-2025 Sarin Na Wangkanai, All Rights Reserved.

namespace Wangkanai.Foundation;

/// <summary>
/// Defines the contract for a generic repository with async operations for entities that implement <see cref="IAggregateRoot"/>.
/// Provides standard CRUD operations with proper async/await patterns and CancellationToken support.
/// </summary>
/// <typeparam name="TEntity">The type of entity managed by this repository.</typeparam>
/// <typeparam name="TKey">The type of the entity's primary key.</typeparam>
public interface IRepository<TEntity, TKey>
   where TEntity : class, IAggregateRoot<TKey>
   where TKey : IEquatable<TKey>, IComparable<TKey>
{
   /// <summary>
   /// Asynchronously retrieves an entity by its unique identifier.
   /// </summary>
   /// <param name="id">The unique identifier of the entity.</param>
   /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
   /// <returns>A task that represents the asynchronous operation. The task result contains the entity if found; otherwise, null.</returns>
   Task<TEntity?> GetByIdAsync(TKey id, CancellationToken cancellationToken = default);

   /// <summary>
   /// Asynchronously retrieves all entities from the repository.
   /// </summary>
   /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
   /// <returns>A task that represents the asynchronous operation. The task result contains a collection of all entities.</returns>
   Task<IEnumerable<TEntity>> GetAllAsync(CancellationToken cancellationToken = default);

   /// <summary>
   /// Asynchronously adds a new entity to the repository.
   /// </summary>
   /// <param name="entity">The entity to add.</param>
   /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
   /// <returns>A task that represents the asynchronous operation. The task result contains the added entity.</returns>
   Task<TEntity> AddAsync(TEntity entity, CancellationToken cancellationToken = default);

   /// <summary>
   /// Asynchronously updates an existing entity in the repository.
   /// </summary>
   /// <param name="entity">The entity to update.</param>
   /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
   /// <returns>A task that represents the asynchronous operation. The task result contains the updated entity.</returns>
   Task<TEntity> UpdateAsync(TEntity entity, CancellationToken cancellationToken = default);

   /// <summary>
   /// Asynchronously removes an entity from the repository by its unique identifier.
   /// </summary>
   /// <param name="id">The unique identifier of the entity to remove.</param>
   /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
   /// <returns>A task that represents the asynchronous operation. The task result indicates whether the entity was successfully removed.</returns>
   Task<bool> DeleteAsync(TKey id, CancellationToken cancellationToken = default);

   /// <summary>
   /// Asynchronously removes an entity from the repository.
   /// </summary>
   /// <param name="entity">The entity to remove.</param>
   /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
   /// <returns>A task that represents the asynchronous operation. The task result indicates whether the entity was successfully removed.</returns>
   Task<bool> DeleteAsync(TEntity entity, CancellationToken cancellationToken = default);

   /// <summary>
   /// Asynchronously checks whether an entity with the specified identifier exists in the repository.
   /// </summary>
   /// <param name="id">The unique identifier to check.</param>
   /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
   /// <returns>A task that represents the asynchronous operation. The task result indicates whether an entity with the specified identifier exists.</returns>
   Task<bool> ExistsAsync(TKey id, CancellationToken cancellationToken = default);

   /// <summary>
   /// Asynchronously counts the total number of entities in the repository.
   /// </summary>
   /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
   /// <returns>A task that represents the asynchronous operation. The task result contains the total count of entities.</returns>
   Task<int> CountAsync(CancellationToken cancellationToken = default);
}