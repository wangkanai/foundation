// Copyright (c) 2014-2025 Sarin Na Wangkanai, All Rights Reserved.

namespace Wangkanai.Foundation;

/// <summary>
/// Defines the contract for handling queries asynchronously.
/// Queries represent read operations that return data without changing the state of the system.
/// </summary>
/// <typeparam name="TQuery">The type of query this handler can process.</typeparam>
/// <typeparam name="TResult">The type of result returned by the query handler.</typeparam>
public interface IQueryHandler<in TQuery, TResult>
   where TQuery : IQuery<TResult>
{
   /// <summary>
   /// Asynchronously handles the specified query and returns a result.
   /// </summary>
   /// <param name="query">The query to handle.</param>
   /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
   /// <returns>A task that represents the asynchronous operation. The task result contains the query result.</returns>
   Task<TResult> HandleAsync(TQuery query, CancellationToken cancellationToken = default);
}