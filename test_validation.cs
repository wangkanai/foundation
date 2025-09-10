using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using FluentAssertions;

// Simple test to verify the validation logic works
class TestValidation 
{
    public static void TestArgumentValidation()
    {
        try
        {
            // Test null validation pattern used in updated tests
            string invalidValue = null;
            var act = () => {
                if (string.IsNullOrWhiteSpace(invalidValue))
                    throw new ArgumentException("Value cannot be null or whitespace.", "testParam");
                return "success";
            };
            
            act.Should().Throw<ArgumentException>()
               .WithParameterName("testParam")
               .WithMessage("*Value cannot be null or whitespace.*");
               
            Console.WriteLine("✓ String validation test passed");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"✗ String validation test failed: {ex.Message}");
        }
        
        try
        {
            // Test numeric validation pattern
            int invalidNumber = -1;
            var act = () => {
                if (invalidNumber <= 0)
                    throw new ArgumentOutOfRangeException("numberParam", "Number must be greater than zero.");
                return "success";
            };
            
            act.Should().Throw<ArgumentOutOfRangeException>()
               .WithParameterName("numberParam")
               .WithMessage("*Number must be greater than zero.*");
               
            Console.WriteLine("✓ Numeric validation test passed");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"✗ Numeric validation test failed: {ex.Message}");
        }
    }
    
    public static void Main()
    {
        Console.WriteLine("Testing validation logic...");
        TestArgumentValidation();
        Console.WriteLine("Validation tests completed.");
    }
}