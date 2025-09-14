// Copyright (c) 2014-2025 Sarin Na Wangkanai, All Rights Reserved.

namespace Wangkanai.Foundation.Exceptions;

/// <summary>
/// Represents an exception that is thrown when a requested entity cannot be found.
/// This exception is typically used to signify situations where the system
/// attempts to retrieve a specific entity that does not exist or is unavailable.
/// </summary>
public class EntityNotFoundException : DomainException
{
   /// <summary>
   /// Represents an exception that is thrown when a requested entity cannot be found.
   /// This exception is typically used to signify situations where the system
   /// attempts to retrieve a specific entity that does not exist or is unavailable.
   /// </summary>
   public EntityNotFoundException(string message) : base(message) { }

   /// <summary>
   /// Represents an exception that indicates a specific entity cannot be found.
   /// Typically used in scenarios where the application expects the existence of
   /// a particular entity, and it is not found during a retrieval or operation.
   /// </summary>
   public EntityNotFoundException(string message, Exception innerException) : base(message, innerException) { }
}