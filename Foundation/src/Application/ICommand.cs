// Copyright (c) 2014-2025 Sarin Na Wangkanai, All Rights Reserved.

namespace Wangkanai.Foundation.Application;

/// <summary>
/// Marker interface that identifies a command that changes the state of the system but returns no result.
/// Commands represent write operations in the CQRS pattern.
/// </summary>
public interface ICommand
{
}

/// <summary>
/// Marker interface that identifies a command that changes the state of the system and returns a result.
/// Commands represent write operations in the CQRS pattern.
/// </summary>
/// <typeparam name="TResult">The type of result returned by the command.</typeparam>
public interface ICommand<TResult>
{
}