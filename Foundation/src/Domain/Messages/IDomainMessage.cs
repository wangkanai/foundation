// Copyright (c) 2014-2025 Sarin Na Wangkanai, All Rights Reserved.

namespace Wangkanai.Foundation;

/// <summary>
/// Represents a marker interface for domain messages, serving as a base contract for domain events or
/// other message types within the domain-driven design context.
/// </summary>
public interface IDomainMessage;

/// <summary>
/// Represents a marker interface for asynchronous domain messages, extending the functionality of
/// the base domain message interface to support async operations or patterns within domain-driven design.
/// </summary>
public interface IAsyncDomainMessage : IDomainMessage;