// Copyright (c) 2014-2025 Sarin Na Wangkanai, All Rights Reserved.

namespace Wangkanai.Foundation;

/// <summary>
/// Marker interface that identifies an entity as an aggregate root in Domain-Driven Design.
/// Aggregate roots are the primary entry points for operations on aggregates and control
/// access to the aggregate's internal entities.
/// </summary>
/// <typeparam name="TKey">The type of the unique identifier for the aggregate root.</typeparam>
public interface IAggregateRoot<TKey> : IEntity<TKey>
   where TKey : IEquatable<TKey>, IComparable<TKey>
{
   /// <summary>
   /// Gets the domain events associated with this aggregate root.
   /// These events are typically published after successful persistence.
   /// </summary>
   IReadOnlyList<IDomainEvent> DomainEvents { get; }

   /// <summary>
   /// Adds a domain event to be published when the aggregate is persisted.
   /// </summary>
   /// <param name="domainEvent">The domain event to add.</param>
   void AddDomainEvent(IDomainEvent domainEvent);

   /// <summary>
   /// Removes a domain event from the collection.
   /// </summary>
   /// <param name="domainEvent">The domain event to remove.</param>
   void RemoveDomainEvent(IDomainEvent domainEvent);

   /// <summary>
   /// Clears all domain events from the collection.
   /// This is typically called after events have been published.
   /// </summary>
   void ClearDomainEvents();
}