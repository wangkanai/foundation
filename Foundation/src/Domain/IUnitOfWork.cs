// Copyright (c) 2014-2025 Sarin Na Wangkanai, All Rights Reserved.

namespace Wangkanai.Foundation;

/// <summary>
/// Defines the contract for a Unit of Work pattern implementation that manages transactions 
/// and coordinates multiple repository operations. All operations follow async/await patterns
/// with proper CancellationToken support.
/// </summary>
public interface IUnitOfWork : IDisposable
{
   /// <summary>
   /// Asynchronously saves all changes made in this unit of work to the underlying data store.
   /// </summary>
   /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
   /// <returns>A task that represents the asynchronous operation. The task result contains the number of affected entities.</returns>
   Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

   /// <summary>
   /// Asynchronously begins a new database transaction.
   /// </summary>
   /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
   /// <returns>A task that represents the asynchronous operation. The task result contains the transaction object.</returns>
   Task<ITransaction> BeginTransactionAsync(CancellationToken cancellationToken = default);

   /// <summary>
   /// Asynchronously commits the current transaction, if any.
   /// </summary>
   /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
   /// <returns>A task that represents the asynchronous operation.</returns>
   Task CommitTransactionAsync(CancellationToken cancellationToken = default);

   /// <summary>
   /// Asynchronously rolls back the current transaction, if any.
   /// </summary>
   /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
   /// <returns>A task that represents the asynchronous operation.</returns>
   Task RollbackTransactionAsync(CancellationToken cancellationToken = default);

   /// <summary>
   /// Gets the repository for the specified entity type.
   /// </summary>
   /// <typeparam name="TEntity">The type of entity managed by the repository.</typeparam>
   /// <typeparam name="TKey">The type of the entity's primary key.</typeparam>
   /// <returns>The repository instance for the specified entity type.</returns>
   IRepository<TEntity, TKey> Repository<TEntity, TKey>() 
      where TEntity : class, IAggregateRoot<TKey>
      where TKey : IEquatable<TKey>, IComparable<TKey>;

   /// <summary>
   /// Asynchronously publishes all domain events from aggregate roots that have been added, modified, or deleted.
   /// </summary>
   /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
   /// <returns>A task that represents the asynchronous operation.</returns>
   Task PublishDomainEventsAsync(CancellationToken cancellationToken = default);
}