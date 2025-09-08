// Copyright (c) 2014-2025 Sarin Na Wangkanai, All Rights Reserved.

namespace Wangkanai.Domain;

/// <summary>
/// Represents an interface that defines a RowVersion property for concurrency control.
/// Implementing this interface allows an entity to maintain a versioning system
/// through a RowVersion string, which can be used to track changes and manage concurrency.
/// </summary>
public interface IHasRowVersion
{
   /// <summary>
   /// Represents the version of a database record used for concurrency control.
   /// The RowVersion property is typically implemented in entities adhering to the
   /// IHasRowVersion interface, enabling detection of changes during data updates.
   /// It is commonly used for optimistic concurrency checks to ensure the record
   /// has not been modified or updated by another process since it was retrieved.
   /// </summary>
   public string RowVersion { get; set; }
}