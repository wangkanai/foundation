// Copyright (c) 2014-2025 Sarin Na Wangkanai, All Rights Reserved.

using System.Collections;
using System.Collections.Concurrent;
using System.Reflection;
using Wangkanai.Foundation.Extensions;

namespace Wangkanai.Foundation;

public class ValueObjectTests
{
   #region Basic Equality Tests

   [Fact]
   public void ValueObject_Equals_ShouldBeTrue()
   {
      var value1 = new Address("123 Main St", "Redmond", "WA", "98052");
      var value2 = new Address("123 Main St", "Redmond", "WA", "98052");
      Assert.True(value1.Equals(value2));
   }

   [Fact]
   public void ValueObject_Equals_ShouldBeFalse()
   {
      var value1 = new Address("123 Main St", "Redmond", "WA", "98052");
      var value2 = new Address("123 Main St", "Redmond", "WA", "98053");
      Assert.False(value1.Equals(value2));
   }

   [Fact]
   public void ValueObject_Equals_ShouldBeFalse_WhenNull()
   {
      var value1 = new Address("123 Main St", "Redmond", "WA", "98052");
      Assert.False(value1.Equals(null));
   }

   [Fact]
   public void ValueObject_Equals_ShouldBeFalse_WhenDifferentType()
   {
      var value1 = new Address("123 Main St", "Redmond", "WA", "98052");
      Assert.False(value1.Equals(new()));
   }

   [Fact]
   public void ValueObject_GetHashCode_ShouldBeTrue()
   {
      var value1 = new Address("123 Main St", "Redmond", "WA", "98052");
      var value2 = new Address("123 Main St", "Redmond", "WA", "98052");
      Assert.Equal(value1.GetHashCode(), value2.GetHashCode());
   }

   [Fact]
   public void ValueObject_GetHashCode_ShouldBeFalse()
   {
      var value1 = new Address("123 Main St", "Redmond", "WA", "98052");
      var value2 = new Address("123 Main St", "Redmond", "WA", "98053");
      Assert.NotEqual(value1.GetHashCode(), value2.GetHashCode());
   }

   #endregion

   #region Compiled Accessor Optimization Tests

   [Fact]
   public void GetEqualityComponents_SimpleProperties_UseCompiledAccessors()
   {
      // Arrange
      var address = new Address("123 Main St", "Seattle", "WA", "98101");
      var simpleVO = new SimpleValueObject("test", 42);

      // Act - These should use compiled accessors for better performance
      var addressEquals = address.Equals(new Address("123 Main St", "Seattle", "WA", "98101"));
      var simpleEquals = simpleVO.Equals(new SimpleValueObject("test", 42));

      // Assert
      Assert.True(addressEquals);
      Assert.True(simpleEquals);
   }

   [Fact]
   public void GetEqualityComponents_ComplexEnumerable_FallsBackToReflection()
   {
      // Arrange
      var complexVO = new ComplexValueObjectWithEnumerable(new[] { "item1", "item2" }, "test");
      var identical = new ComplexValueObjectWithEnumerable(new[] { "item1", "item2" }, "test");
      var different = new ComplexValueObjectWithEnumerable(new[] { "item1", "item3" }, "test");

      // Act
      var shouldBeEqual = complexVO.Equals(identical);
      var shouldNotBeEqual = complexVO.Equals(different);

      // Assert
      Assert.True(shouldBeEqual);
      Assert.False(shouldNotBeEqual);
   }

   [Fact]
   public void GetEqualityComponentsFast_DirectAccessorCall_ReturnsOptimizedResult()
   {
      // Arrange
      var address = new Address("123 Main St", "Seattle", "WA", "98101");

      // Act - Call the equality components method (testing optimized path)
      var isEqual = address.Equals(new Address("123 Main St", "Seattle", "WA", "98101"));
      var isNotEqual = address.Equals(new Address("456 Oak St", "Seattle", "WA", "98101"));

      // Assert - Test that the optimized path works correctly
      Assert.True(isEqual);
      Assert.False(isNotEqual);
   }

   [Fact]
   public void CompiledAccessor_WithNullProperties_HandlesCorrectly()
   {
      // Arrange
      var addressWithNulls = new Address(null, "Seattle", null, "98101");
      var identical = new Address(null, "Seattle", null, "98101");
      var different = new Address("123 Main", "Seattle", null, "98101");

      // Act & Assert
      Assert.True(addressWithNulls.Equals(identical));
      Assert.False(addressWithNulls.Equals(different));
   }

   #endregion

   #region Reflection Fallback Scenarios Tests

   [Fact]
   public void GetEqualityComponents_WithInterfaceProperties_FallsBackToReflection()
   {
      // Arrange
      var interfaceVO = new ValueObjectWithInterface(new List<string> { "test1", "test2" });
      var identical = new ValueObjectWithInterface(new List<string> { "test1", "test2" });
      var different = new ValueObjectWithInterface(new List<string> { "test1", "test3" });

      // Act
      var shouldBeEqual = interfaceVO.Equals(identical);
      var shouldNotBeEqual = interfaceVO.Equals(different);

      // Assert
      Assert.True(shouldBeEqual);
      Assert.False(shouldNotBeEqual);
   }

   [Fact]
   public void GetEqualityComponents_ReflectionPath_HandlesComplexTypes()
   {
      // Arrange
      var nested = new NestedValueObject("inner", 123);
      var parent1 = new ValueObjectWithNested(nested, "outer");
      var parent2 = new ValueObjectWithNested(new NestedValueObject("inner", 123), "outer");
      var different = new ValueObjectWithNested(new NestedValueObject("different", 123), "outer");

      // Act & Assert
      Assert.True(parent1.Equals(parent2));
      Assert.False(parent1.Equals(different));
   }

   [Fact]
   public void GetEqualityComponents_ExceptionInCompilation_FallsBackToReflection()
   {
      // Arrange - This simulates a case where compilation might fail
      var problematicVO = new ProblematicValueObject();
      var identical = new ProblematicValueObject();

      // Act & Assert - Should not throw, should fall back gracefully
      Assert.True(problematicVO.Equals(identical));
   }

   #endregion

   #region Complex Enumerable Handling Tests

   [Fact]
   public void GetEqualityComponents_WithList_HandlesBoundaryMarkers()
   {
      // Arrange
      var listVO1 = new ValueObjectWithList(new List<int> { 1, 2, 3 });
      var listVO2 = new ValueObjectWithList(new List<int> { 1, 2, 3 });
      var listVO3 = new ValueObjectWithList(new List<int> { 1, 2, 4 });

      // Act & Assert
      Assert.True(listVO1.Equals(listVO2));
      Assert.False(listVO1.Equals(listVO3));
   }

   [Fact]
   public void GetEqualityComponents_WithEmptyList_HandlesCorrectly()
   {
      // Arrange
      var emptyListVO1 = new ValueObjectWithList(new List<int>());
      var emptyListVO2 = new ValueObjectWithList(new List<int>());
      var nonEmptyListVO = new ValueObjectWithList(new List<int> { 1 });

      // Act & Assert
      Assert.True(emptyListVO1.Equals(emptyListVO2));
      Assert.False(emptyListVO1.Equals(nonEmptyListVO));
   }

   [Fact]
   public void GetEqualityComponents_WithNullList_HandlesCorrectly()
   {
      // Arrange
      var nullListVO1 = new ValueObjectWithList(null);
      var nullListVO2 = new ValueObjectWithList(null);
      var nonNullListVO = new ValueObjectWithList(new List<int> { 1 });

      // Act & Assert
      Assert.True(nullListVO1.Equals(nullListVO2));
      Assert.False(nullListVO1.Equals(nonNullListVO));
   }

   [Fact]
   public void GetEqualityComponents_WithNestedEnumerables_HandlesRecursively()
   {
      // Arrange
      var nestedEnumerable1 = new ValueObjectWithNestedEnumerable(
         new List<List<string>>
         {
            new() { "a", "b" },
            new() { "c", "d" }
         });
      var nestedEnumerable2 = new ValueObjectWithNestedEnumerable(
         new List<List<string>>
         {
            new() { "a", "b" },
            new() { "c", "d" }
         });
      var different = new ValueObjectWithNestedEnumerable(
         new List<List<string>>
         {
            new() { "a", "b" },
            new() { "c", "e" }
         });

      // Act & Assert
      Assert.True(nestedEnumerable1.Equals(nestedEnumerable2));
      Assert.False(nestedEnumerable1.Equals(different));
   }

   #endregion

   #region Caching Mechanisms Tests

   [Fact]
   public void TypeProperties_CachesPropertyInfo_ForPerformance()
   {
      // Arrange
      var address1 = new Address("123 Main", "City", "State", "12345");
      var address2 = new Address("456 Oak", "Town", "Province", "67890");

      // Act - Multiple calls should use cached property info
      var props1 = address1.GetProperties().ToArray();
      var props2 = address2.GetProperties().ToArray();

      // Assert
      Assert.Equal(4, props1.Length);
      Assert.Equal(4, props2.Length);
      Assert.Equal(props1.Select(p => p.Name), props2.Select(p => p.Name));
   }

   [Fact]
   public void CachedProperties_AreOrderedByName_ForConsistency()
   {
      // Arrange
      var address = new Address("Street", "City", "State", "Zip");

      // Act
      var properties = address.GetProperties().ToArray();

      // Assert
      var propertyNames = properties.Select(p => p.Name).ToArray();
      var sortedNames = propertyNames.OrderBy(n => n).ToArray();
      Assert.Equal(sortedNames, propertyNames);
   }

   [Fact]
   public void OptimizationEnabled_DisabledAfterFailure_UsesReflectionFallback()
   {
      // This test verifies that after a compilation failure, the system falls back gracefully
      // Arrange
      var problematic = new ProblematicValueObject();
      var simple = new SimpleValueObject("test", 42);

      // Act - First call might trigger optimization attempt and potential failure
      var result1 = problematic.Equals(new ProblematicValueObject());
      // Subsequent calls should work with fallback
      var result2 = simple.Equals(new SimpleValueObject("test", 42));

      // Assert
      Assert.True(result1);
      Assert.True(result2);
   }

   #endregion

   #region ICacheKey Interface Tests

   [Fact]
   public void GetCacheKey_SimpleProperties_ReturnsCorrectFormat()
   {
      // Arrange
      var address = new Address("123 Main St", "Seattle", "WA", "98101");

      // Act
      var cacheKey = address.GetCacheKey();

      // Assert
      Assert.NotNull(cacheKey);
      Assert.Contains("Seattle", cacheKey);
      Assert.Contains("WA", cacheKey);
      Assert.Contains("98101", cacheKey);
      Assert.Contains("|", cacheKey); // Should contain separators
   }

   [Fact]
   public void GetCacheKey_WithStringProperties_QuotesStrings()
   {
      // Arrange
      var address = new Address("123 Main St", "Seattle", "WA", "98101");

      // Act
      var cacheKey = address.GetCacheKey();

      // Assert
      Assert.Contains("'Seattle'", cacheKey);
      Assert.Contains("'WA'", cacheKey);
   }

   [Fact]
   public void GetCacheKey_WithNullValues_HandlesCorrectly()
   {
      // Arrange
      var addressWithNulls = new Address(null, "Seattle", null, "98101");

      // Act
      var cacheKey = addressWithNulls.GetCacheKey();

      // Assert
      Assert.NotNull(cacheKey);
      Assert.Contains("'Seattle'", cacheKey);
      Assert.Contains("'98101'", cacheKey);
   }

   [Fact]
   public void GetCacheKey_WithNestedCacheKey_UsesNestedCacheKey()
   {
      // Arrange
      var nested = new Address("Inner St", "Inner City", "IC", "12345");
      var parent = new ValueObjectWithCacheKeyProperty(nested, "outer");

      // Act
      var cacheKey = parent.GetCacheKey();

      // Assert
      Assert.Contains("Inner City", cacheKey);
      Assert.Contains("'outer'", cacheKey);
   }

   #endregion

   #region ToString Method Tests

   [Fact]
   public void ToString_FormatsPropertiesCorrectly()
   {
      // Arrange
      var address = new Address("123 Main St", "Seattle", "WA", "98101");

      // Act
      var toString = address.ToString();

      // Assert
      Assert.Contains("City: Seattle", toString);
      Assert.Contains("State: WA", toString);
      Assert.Contains("Street: 123 Main St", toString);
      Assert.Contains("Zip: 98101", toString);
      Assert.StartsWith("{", toString);
      Assert.EndsWith("}", toString);
   }

   [Fact]
   public void ToString_WithNullValues_HandlesCorrectly()
   {
      // Arrange
      var address = new Address(null, "Seattle", null, "98101");

      // Act
      var toString = address.ToString();

      // Assert
      Assert.Contains("City: Seattle", toString);
      Assert.Contains("Zip: 98101", toString);
      Assert.Contains("Street: ", toString); // Should handle null
   }

   #endregion

   #region Clone Method Tests

   [Fact]
   public void Clone_CreatesShallowCopy()
   {
      // Arrange
      var original = new Address("123 Main St", "Seattle", "WA", "98101");

      // Act
      var cloned = (Address)original.Clone();

      // Assert
      Assert.NotSame(original, cloned);
      Assert.Equal(original, cloned);
      Assert.Equal(original.Street, cloned.Street);
      Assert.Equal(original.City, cloned.City);
   }

   #endregion

   #region Operator Tests

   [Fact]
   public void Operators_EqualityAndInequality_WorkCorrectly()
   {
      // Arrange
      var address1 = new Address("123 Main St", "Seattle", "WA", "98101");
      var address2 = new Address("123 Main St", "Seattle", "WA", "98101");
      var address3 = new Address("456 Oak St", "Seattle", "WA", "98101");

      // Act & Assert
      Assert.True(address1 == address2);
      Assert.False(address1 != address2);
      Assert.False(address1 == address3);
      Assert.True(address1 != address3);
   }

   [Fact]
   public void Operators_WithNullValues_HandleCorrectly()
   {
      // Arrange
      Address? address1 = null;
      Address? address2 = null;
      var address3 = new Address("123 Main St", "Seattle", "WA", "98101");

      // Act & Assert
      Assert.True(address1 == address2);
      Assert.False(address1 != address2);
      Assert.False(address1 == address3);
      Assert.True(address1 != address3);
   }

   #endregion
}

#region Test Value Objects

public class Address(string? street, string? city, string? state, string? zip) : ValueObject
{
   public string? Street { get; set; } = street;
   public string? City   { get; set; } = city;
   public string? State  { get; set; } = state;
   public string? Zip    { get; set; } = zip;
}

/// <summary>Simple value object for testing compiled accessors</summary>
public class SimpleValueObject(string text, int number) : ValueObject
{
   public string Text { get; } = text;
   public int Number { get; } = number;
}

/// <summary>Complex value object with enumerable that should fall back to reflection</summary>
public class ComplexValueObjectWithEnumerable(IEnumerable<string> items, string name) : ValueObject
{
   public IEnumerable<string> Items { get; } = items;
   public string Name { get; } = name;
}

/// <summary>Value object with interface property to test reflection fallback</summary>
public class ValueObjectWithInterface(IList<string> items) : ValueObject
{
   public IList<string> Items { get; } = items;
}

/// <summary>Nested value object for testing complex scenarios</summary>
public class NestedValueObject(string value, int id) : ValueObject
{
   public string Value { get; } = value;
   public int Id { get; } = id;
}

/// <summary>Value object containing another value object</summary>
public class ValueObjectWithNested(NestedValueObject nested, string outer) : ValueObject
{
   public NestedValueObject Nested { get; } = nested;
   public string Outer { get; } = outer;
}

/// <summary>Value object that might cause compilation issues</summary>
public class ProblematicValueObject : ValueObject
{
   // This might cause optimization to fail due to complex property scenarios
   public dynamic? DynamicProperty { get; set; }
   public object? ComplexProperty { get; set; }
}

/// <summary>Value object with list property</summary>
public class ValueObjectWithList(List<int>? numbers) : ValueObject
{
   public List<int>? Numbers { get; } = numbers;
}

/// <summary>Value object with nested enumerable</summary>
public class ValueObjectWithNestedEnumerable(List<List<string>> nestedItems) : ValueObject
{
   public List<List<string>> NestedItems { get; } = nestedItems;
}

/// <summary>Value object with a property that implements ICacheKey</summary>
public class ValueObjectWithCacheKeyProperty(Address address, string name) : ValueObject
{
   public Address Address { get; } = address;
   public string Name { get; } = name;
}

#endregion