// Copyright (c) 2014-2025 Sarin Na Wangkanai, All Rights Reserved.

namespace Wangkanai.Foundation;

/// <summary>
/// Defines the contract for handling domain events asynchronously.
/// Domain event handlers contain the business logic that should be executed when specific domain events occur.
/// </summary>
/// <typeparam name="TDomainEvent">The type of domain event this handler can process.</typeparam>
public interface IDomainEventHandler<in TDomainEvent>
   where TDomainEvent : IDomainEvent
{
   /// <summary>
   /// Asynchronously handles the specified domain event.
   /// </summary>
   /// <param name="domainEvent">The domain event to handle.</param>
   /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
   /// <returns>A task that represents the asynchronous operation.</returns>
   Task HandleAsync(TDomainEvent domainEvent, CancellationToken cancellationToken = default);
}