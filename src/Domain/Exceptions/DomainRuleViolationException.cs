// Copyright (c) 2014-2025 Sarin Na Wangkanai, All Rights Reserved.

namespace Wangkanai.Domain.Exceptions;

/// <summary>
/// Represents an exception thrown when a specific domain rule is violated within the application.
/// This exception is used to explicitly indicate a failure to adhere to predefined domain rules or constraints.
/// </summary>
public class DomainRuleViolationException
   : DomainException
{
   /// <summary>
   /// Gets the name of the violated domain rule.
   /// This property is used to identify the specific domain rule that was breached
   /// and caused the exception to be raised.
   /// </summary>
   public string RuleName { get; }

   /// <summary>
   /// Represents an exception thrown when a specific domain rule is violated within the application's business logic.
   /// This exception is intended for identifying and handling violations of defined domain rules or constraints.
   /// </summary>
   public DomainRuleViolationException(string ruleName, string message)
      : base($"Domain rule '{ruleName}' violated: {message}")
      => RuleName = ruleName;

   /// <summary>
   /// Represents an exception thrown when a specific domain rule is violated within the application's business logic.
   /// This exception is intended for identifying and handling violations of defined domain rules or constraints.
   /// </summary>
   public DomainRuleViolationException(string ruleName, string message, Exception innerException)
      : base($"Domain rule '{ruleName}' violated: {message}", innerException)
      => RuleName = ruleName;
}