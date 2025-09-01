// Copyright (c) 2014-2025 Sarin Na Wangkanai, All Rights Reserved.

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using Wangkanai.Domain;

namespace Wangkanai.Benchmark;

/// <summary>
/// Performance benchmarks comparing different ValueObject implementations.
/// Expected results: Optimized versions should be 500-1000x faster than reflection.
/// </summary>
[MemoryDiagnoser]
[SimpleJob]
[RankColumn]
public class ValueObjectPerformanceBenchmark
{
    private TestValueObjectOriginal _originalA = null!;
    private TestValueObjectOriginal _originalB = null!;
    private TestValueObjectOptimized _optimizedA = null!;
    private TestValueObjectOptimized _optimizedB = null!;
    private TestValueObjectHybrid _hybridA = null!;
    private TestValueObjectHybrid _hybridB = null!;

    [GlobalSetup]
    public void Setup()
    {
        // Setup identical test data
        _originalA = new TestValueObjectOriginal("test", 42, DateTime.Now);
        _originalB = new TestValueObjectOriginal("test", 42, DateTime.Now);
        
        _optimizedA = new TestValueObjectOptimized("test", 42, DateTime.Now);
        _optimizedB = new TestValueObjectOptimized("test", 42, DateTime.Now);
        
        _hybridA = new TestValueObjectHybrid("test", 42, DateTime.Now);
        _hybridB = new TestValueObjectHybrid("test", 42, DateTime.Now);
    }

    [Benchmark(Baseline = true)]
    public bool OriginalEquals() => _originalA.Equals(_originalB);

    [Benchmark]
    public bool OptimizedEquals() => _optimizedA.Equals(_optimizedB);

    [Benchmark]
    public bool HybridEquals() => _hybridA.Equals(_hybridB);

    [Benchmark]
    public int OriginalGetHashCode() => _originalA.GetHashCode();

    [Benchmark]
    public int OptimizedGetHashCode() => _optimizedA.GetHashCode();

    [Benchmark]
    public int HybridGetHashCode() => _hybridA.GetHashCode();

    [Benchmark]
    public string OriginalGetCacheKey() => _originalA.GetCacheKey();

    [Benchmark]
    public string OptimizedGetCacheKey() => _optimizedA.GetCacheKey();

    [Benchmark]
    public string HybridGetCacheKey() => _hybridA.GetCacheKey();

    // Test different complexity scenarios
    [Benchmark]
    public bool ComplexObjectEquals()
    {
        var complex1 = new ComplexValueObject("test", new List<int> { 1, 2, 3 }, 
            new Dictionary<string, object> { ["key"] = "value" });
        var complex2 = new ComplexValueObject("test", new List<int> { 1, 2, 3 }, 
            new Dictionary<string, object> { ["key"] = "value" });
        return complex1.Equals(complex2);
    }
}

// Test implementations inheriting from different bases

public class TestValueObjectOriginal : ValueObject
{
    public string Name { get; }
    public int Number { get; }
    public DateTime Timestamp { get; }

    public TestValueObjectOriginal(string name, int number, DateTime timestamp)
    {
        Name = name;
        Number = number;
        Timestamp = timestamp;
    }
}

public class TestValueObjectOptimized : ValueObjectOptimized
{
    public string Name { get; }
    public int Number { get; }
    public DateTime Timestamp { get; }

    public TestValueObjectOptimized(string name, int number, DateTime timestamp)
    {
        Name = name;
        Number = number;
        Timestamp = timestamp;
    }
}

public class TestValueObjectHybrid : ValueObjectHybrid
{
    public string Name { get; }
    public int Number { get; }
    public DateTime Timestamp { get; }

    public TestValueObjectHybrid(string name, int number, DateTime timestamp)
    {
        Name = name;
        Number = number;
        Timestamp = timestamp;
    }
}

public class ComplexValueObject : ValueObjectHybrid
{
    public string Name { get; }
    public List<int> Numbers { get; }
    public Dictionary<string, object> Properties { get; }

    public ComplexValueObject(string name, List<int> numbers, Dictionary<string, object> properties)
    {
        Name = name;
        Numbers = numbers;
        Properties = properties;
    }
}

public class Program
{
    public static void Main(string[] args)
    {
        var summary = BenchmarkRunner.Run<ValueObjectPerformanceBenchmark>();
        Console.WriteLine($"Benchmark completed. Check results for performance improvements.");
    }
}