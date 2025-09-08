// Copyright (c) 2014-2025 Sarin Na Wangkanai, All Rights Reserved.

using Microsoft.Extensions.Hosting;

namespace Wangkanai.Foundation;

public interface IEventListener<in TEvent, in TAction> : IHostedService
   where TEvent : IEvent
   where TAction : class
{
   Task HandleAsync(TEvent domainEvent, TAction action, CancellationToken token = default);
}