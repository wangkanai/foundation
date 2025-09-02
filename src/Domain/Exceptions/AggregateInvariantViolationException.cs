// Copyright (c) 2014-2025 Sarin Na Wangkanai, All Rights Reserved.

namespace Wangkanai.Domain.Exceptions;

/// <summary>
/// Represents an exception thrown when an invariant defined within an aggregate is violated.
/// This exception is used to explicitly indicate a failure to comply with the constraints or
/// rules safeguarding the consistency of an aggregate in the domain.
/// </summary>
public class AggregateInvariantViolationException
   : DomainException
{
   /// <summary>
   /// Gets the name of the aggregate associated with the exception.
   /// This property provides the contextual information about the specific aggregate whose invariant has been violated,
   /// helping to identify the aggregate involved in the rule enforcement failure.
   /// </summary>
   public string AggregateName { get; }

   /// <summary>
   /// Gets the name of the invariant that has been violated. This property provides specific information about
   /// the domain rule or condition that failed to be upheld, identifying the invariant involved in the violation.
   /// </summary>
   public string InvariantName { get; }

   /// <summary>
   /// Represents an exception thrown when an invariant defined within an aggregate is violated.
   /// This exception is used to explicitly indicate a failure to comply with the constraints or
   /// rules safeguarding the consistency of an aggregate in the domain.
   /// </summary>
   public AggregateInvariantViolationException(string aggregateName, string invariantName, string message)
      : base($"Aggregate '{aggregateName}' invariant '{invariantName}' violated: {message}")
   {
      AggregateName = aggregateName;
      InvariantName = invariantName;
   }
}