// Copyright (c) 2014-2025 Sarin Na Wangkanai, All Rights Reserved.

namespace Wangkanai.Foundation;

/// <summary>
/// Defines the contract for a database transaction with async operations.
/// Provides methods to commit or rollback the transaction.
/// </summary>
public interface ITransaction : IDisposable
{
   /// <summary>
   /// Gets the unique identifier for this transaction.
   /// </summary>
   Guid TransactionId { get; }

   /// <summary>
   /// Asynchronously commits the transaction, making all changes permanent.
   /// </summary>
   /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
   /// <returns>A task that represents the asynchronous operation.</returns>
   Task CommitAsync(CancellationToken cancellationToken = default);

   /// <summary>
   /// Asynchronously rolls back the transaction, discarding all changes.
   /// </summary>
   /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
   /// <returns>A task that represents the asynchronous operation.</returns>
   Task RollbackAsync(CancellationToken cancellationToken = default);
}