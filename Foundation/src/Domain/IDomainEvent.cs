// Copyright (c) 2014-2025 Sarin Na Wangkanai, All Rights Reserved.

namespace Wangkanai.Foundation;

/// <summary>
/// Defines the contract for a domain event that represents something significant that happened in the domain.
/// Domain events are used to communicate between different parts of the domain and trigger side effects.
/// </summary>
public interface IDomainEvent
{
   /// <summary>
   /// Gets the unique identifier for this domain event.
   /// </summary>
   Guid Id { get; }

   /// <summary>
   /// Gets the timestamp when this domain event occurred.
   /// </summary>
   DateTime OccurredOn { get; }

   /// <summary>
   /// Gets the name of the event type. This is typically used for serialization and event routing.
   /// </summary>
   string EventType { get; }
}