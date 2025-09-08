// Copyright (c) 2014-2025 Sarin Na Wangkanai, All Rights Reserved.

namespace Wangkanai.Foundation;

/// <summary>
/// Represents a specialized interface for domain events utilizing an integer as the identifier.
/// This interface extends the functionality of the generic event structure, tailored for use in domain-driven design architectures.
/// </summary>
public interface IEvent : IEvent<int> { }

/// <summary>
/// Represents a base interface for an event within the domain.
/// This interface is used as a generic contract for domain events and serves as the foundation for event-driven architectures.
/// </summary>
public interface IEvent<T>
   where T : IComparable<T>, IEquatable<T> { }