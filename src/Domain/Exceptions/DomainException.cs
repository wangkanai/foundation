// Copyright (c) 2014-2025 Sarin Na Wangkanai, All Rights Reserved.

namespace Wangkanai.Domain.Exceptions;

/// <summary>Represents a base class for domain-specific exceptions. This class serves as a foundation for creating custom exceptions related to specific domain logic errors within the application.</summary>
public abstract class DomainException : Exception
{
   /// <summary>Represents a base class for domain-specific exceptions. This class serves as a foundation for creating custom exceptions related to specific domain logic errors within the application.</summary>
   protected DomainException(string message)
      : base(message) { }

   /// <summary>Represents a base class for domain-specific exceptions. This class serves as a foundation for creating custom exceptions related to specific domain logic errors within the application.</summary>
   protected DomainException(string message, Exception innerException)
      : base(message, innerException) { }
}