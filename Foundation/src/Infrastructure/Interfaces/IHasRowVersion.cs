// Copyright (c) 2014-2025 Sarin Na Wangkanai, All Rights Reserved.

namespace Wangkanai.Foundation;

/// <summary>
/// Represents an interface that defines a RowVersion property for concurrency control
/// with the default byte[] type for compatibility with SQL Server and most relational databases.
/// This interface provides backward compatibility while allowing for database-specific implementations.
/// </summary>
public interface IHasRowVersion : IHasRowVersion<byte[]>;

/// <summary>
/// Represents a generic interface that defines a RowVersion property for concurrency control.
/// Implementing this interface allows an entity to maintain a versioning system
/// through a strongly-typed RowVersion property, which can be used to track changes and manage concurrency.
/// </summary>
/// <typeparam name="T">The type of the RowVersion property (e.g., byte[], string, long, etc.)</typeparam>
public interface IHasRowVersion<T>
{
   /// <summary>
   /// Represents the version of a database record used for concurrency control.
   /// The RowVersion property is typically implemented in entities adhering to the
   /// IHasRowVersion interface, enabling detection of changes during data updates.
   /// It is commonly used for optimistic concurrency checks to ensure the record
   /// has not been modified or updated by another process since it was retrieved.
   /// </summary>
   T RowVersion { get; set; }
}