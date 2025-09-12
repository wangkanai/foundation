// Copyright (c) 2014-2025 Sarin Na Wangkanai, All Rights Reserved.

namespace Wangkanai.Foundation;

/// <summary>
/// Marker interface that identifies a query that reads data from the system without changing its state.
/// Queries represent read operations in the CQRS pattern.
/// </summary>
/// <typeparam name="TResult">The type of result returned by the query.</typeparam>
public interface IQuery<TResult>
{
}