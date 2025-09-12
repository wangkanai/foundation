// Copyright (c) 2014-2025 Sarin Na Wangkanai, All Rights Reserved.

namespace Wangkanai.Foundation;

/// <summary>
/// Represents an action that processes a specific command and returns a result.
/// This interface defines the contract for handling operations within a CQRS pattern,
/// where a command is processed and a corresponding result is produced.
/// </summary>
/// <typeparam name="TCommand">
/// The type of the command being processed. The command must implement the <see cref="ICommand{TResult}"/> interface.
/// </typeparam>
/// <typeparam name="TResult">
/// The type of the result produced upon successfully processing the command.
/// </typeparam>
public interface IAction<in TCommand, TResult> where TCommand : ICommand<TResult>
{
   TResult Execute(TCommand command);
}

/// <summary>
/// Represents an asynchronous action that processes a specific command and returns a result.
/// This interface defines the contract for handling operations within a CQRS pattern,
/// where a command is processed asynchronously, and a corresponding result is produced.
/// </summary>
/// <typeparam name="TCommand">
/// The type of the command being processed. The command must implement the <see cref="ICommand{TResult}"/> interface.
/// </typeparam>
/// <typeparam name="TResult">
/// The type of the result produced upon successfully processing the command.
/// </typeparam>
public interface IAsyncAction<in TCommand, TResult> where TCommand : ICommand<TResult>
{
   Task<TResult> ExecuteAsync(TCommand command, CancellationToken cancellationToken = default);
}