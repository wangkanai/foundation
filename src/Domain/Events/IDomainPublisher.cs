// Copyright (c) 2014-2025 Sarin Na Wangkanai, All Rights Reserved.

namespace Wangkanai.Domain;

/// <summary>
/// Represents a mechanism to handle and publish domain events within the application.
/// This interface is designed to act as a mediator for propagating domain events
/// and allows both synchronous and asynchronous event handling.
/// </summary>
public interface IDomainPublisher
{
   /// <summary>
   /// Publishes a domain event of the specified type. It serves as a mechanism
   /// for synchronously distributing domain events within the application.
   /// </summary>
   /// <typeparam name="T">The type of the domain event being published. It must implement the <see cref="IDomainEvent"/> interface.</typeparam>
   /// <param name="domainEvent">The domain event instance that needs to be published.</param>
   void Publish<T>(T domainEvent) where T : IDomainEvent;

   /// <summary>
   /// Asynchronously publishes a domain event of the specified type. It serves as a mechanism
   /// for distributing domain events within the application in an asynchronous manner.
   /// </summary>
   /// <typeparam name="T">The type of the domain event being published. It must implement the <see cref="IDomainEvent"/> interface.</typeparam>
   /// <param name="domainEvent">The domain event instance that needs to be published.</param>
   /// <returns>A task representing the asynchronous operation of publishing the domain event.</returns>
   Task PublishAsync<T>(T domainEvent) where T : IDomainEvent;

   /// <summary>
   /// Retrieves a read-only list of all published domain events that have been processed within the application.
   /// This method allows inspection of the events for auditing or debugging purposes.
   /// </summary>
   /// <returns>A read-only list of domain events implementing the <see cref="IDomainEvent"/> interface.</returns>
   IReadOnlyList<IDomainEvent> GetPublishedEvents();
}