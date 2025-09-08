// Copyright (c) 2014-2025 Sarin Na Wangkanai, All Rights Reserved.

namespace Wangkanai.Foundation;

/// <summary>
/// Represents a domain event publisher that facilitates the handling and distribution
/// of domain events within the application. It provides mechanisms to publish events
/// both synchronously and asynchronously, as well as to retrieve a list of all published events.
/// </summary>
public class DomainPublisher : IDomainPublisher
{
   private readonly List<IDomainEvent> _publishedEvents = new();

   /// <summary>
   /// Retrieves a read-only list of all domain events that have been published by the publisher.
   /// </summary>
   /// <returns>
   /// A read-only list of <see cref="IDomainEvent"/> objects that have been published.
   /// </returns>
   public IReadOnlyList<IDomainEvent> GetPublishedEvents()
      => _publishedEvents.AsReadOnly();

   /// <summary>
   /// Publishes a domain event of the specified type synchronously.
   /// </summary>
   /// <typeparam name="T">The type of the domain event to be published, which must implement <see cref="IDomainEvent"/>.</typeparam>
   /// <param name="domainEvent">The domain event instance to be published.</param>
   public void Publish<T>(T domainEvent) where T : IDomainEvent
      => PublishAsync(domainEvent);

   /// <summary>
   /// Asynchronously publishes the specified domain event to be handled or processed by appropriate consumers.
   /// </summary>
   /// <param name="domainEvent">
   /// The domain event to be published. It must implement the <see cref="IDomainEvent"/> interface.
   /// </param>
   /// <typeparam name="T">
   /// The type of the domain event being published, constrained to implement <see cref="IDomainEvent"/>.
   /// </typeparam>
   /// <returns>
   /// A task representing the asynchronous operation.
   /// </returns>
   public Task PublishAsync<T>(T domainEvent) where T : IDomainEvent
   {
      domainEvent.ThrowIfNull();
      _publishedEvents.Add(domainEvent);
      return Task.CompletedTask;
   }
}