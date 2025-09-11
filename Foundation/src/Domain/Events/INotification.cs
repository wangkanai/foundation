// Copyright (c) 2014-2025 Sarin Na Wangkanai, All Rights Reserved.

namespace Wangkanai.Foundation;

/// <summary>
/// Represents a contract for notifications within the domain,
/// providing a standardized mechanism for handling notification-related operations.
/// </summary>
public interface INotification;

/// <summary>
/// Defines a contract for asynchronous notifications within the domain,
/// extending the capabilities of the base notification interface to support asynchronous operations.
/// </summary>
public interface IAsyncNotification : INotification;