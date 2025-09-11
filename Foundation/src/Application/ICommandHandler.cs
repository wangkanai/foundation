// Copyright (c) 2014-2025 Sarin Na Wangkanai, All Rights Reserved.

namespace Wangkanai.Foundation.Application;

/// <summary>
/// Defines the contract for handling commands asynchronously that return no result.
/// Commands represent operations that change the state of the system.
/// </summary>
/// <typeparam name="TCommand">The type of command this handler can process.</typeparam>
public interface ICommandHandler<in TCommand>
   where TCommand : ICommand
{
   /// <summary>
   /// Asynchronously handles the specified command.
   /// </summary>
   /// <param name="command">The command to handle.</param>
   /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
   /// <returns>A task that represents the asynchronous operation.</returns>
   Task HandleAsync(TCommand command, CancellationToken cancellationToken = default);
}

/// <summary>
/// Defines the contract for handling commands asynchronously that return a result.
/// Commands represent operations that change the state of the system and return a result.
/// </summary>
/// <typeparam name="TCommand">The type of command this handler can process.</typeparam>
/// <typeparam name="TResult">The type of result returned by the command handler.</typeparam>
public interface ICommandHandler<in TCommand, TResult>
   where TCommand : ICommand<TResult>
{
   /// <summary>
   /// Asynchronously handles the specified command and returns a result.
   /// </summary>
   /// <param name="command">The command to handle.</param>
   /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
   /// <returns>A task that represents the asynchronous operation. The task result contains the command result.</returns>
   Task<TResult> HandleAsync(TCommand command, CancellationToken cancellationToken = default);
}