// Copyright (c) 2014-2025 Sarin Na Wangkanai, All Rights Reserved.

using System.Text.Json;

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

using Microsoft.AspNetCore.Identity;

namespace Wangkanai.Audit.Benchmark;

/// <summary>Performance benchmarks for optimized audit trail storage.</summary>
[MemoryDiagnoser]
[SimpleJob(RuntimeMoniker.Net90)]
[RankColumn]
public class AuditPerformanceBenchmark
{
   private readonly Dictionary<string, object> _largeChangeSet;
   private readonly string                     _largeJsonNew;
   private readonly string                     _largeJsonOld;
   private readonly Dictionary<string, object> _smallChangeSet;
   private readonly string                     _smallJsonNew;
   private readonly string                     _smallJsonOld;

   public AuditPerformanceBenchmark()
   {
      // Small change set (<=3 properties)
      _smallChangeSet = new()
                        {
                           { "Name", "John Doe" },
                           { "Age", 30 },
                           { "IsActive", true }
                        };

      // Large change set (>3 properties)
      _largeChangeSet = new();
      for (var i = 0; i < 10; i++)
         _largeChangeSet[$"Property{i}"] = $"Value{i}";

      // Pre-serialized JSON for testing
      _smallJsonOld = JsonSerializer.Serialize(_smallChangeSet);
      _smallJsonNew = JsonSerializer.Serialize(new Dictionary<string, object>
                                               {
                                                  { "Name", "Jane Doe" },
                                                  { "Age", 31 },
                                                  { "IsActive", false }
                                               });

      _largeJsonOld = JsonSerializer.Serialize(_largeChangeSet);
      var largeChangeSetNew = new Dictionary<string, object>();
      for (var i = 0; i < 10; i++)
         largeChangeSetNew[$"Property{i}"] = $"NewValue{i}";
      _largeJsonNew = JsonSerializer.Serialize(largeChangeSetNew);
   }

   /// <summary>Benchmark: Original dictionary-based approach for small change sets.</summary>
   [Benchmark(Baseline = true)]
   public void OriginalDictionary_SmallChangeSet()
   {
      var audit = new Audit<int, IdentityUser<int>, int>
                  {
                     OldValues = new(_smallChangeSet),
                     NewValues = new(_smallChangeSet)
                  };

      // Simulate access patterns
      _ = audit.OldValues["Name"];
      _ = audit.NewValues["Age"];
   }

   /// <summary>Benchmark: Optimized JSON-based approach for small change sets using SetValuesFromJson.</summary>
   [Benchmark]
   public void OptimizedJson_SmallChangeSet()
   {
      var audit = new Audit<int, IdentityUser<int>, int>();
      audit.SetValuesFromJson(_smallJsonOld, _smallJsonNew);

      // Simulate access patterns
      _ = audit.GetOldValue("Name");
      _ = audit.GetNewValue("Age");
   }

   /// <summary>Benchmark: Optimized Span-based approach for small change sets.</summary>
   [Benchmark]
   public void OptimizedSpan_SmallChangeSet()
   {
      var                  audit       = new Audit<int, IdentityUser<int>, int>();
      ReadOnlySpan<string> columnNames = ["Name", "Age", "IsActive"];
      ReadOnlySpan<object> oldValues   = ["John Doe", 30, true];
      ReadOnlySpan<object> newValues   = ["Jane Doe", 31, false];

      audit.SetValuesFromSpan(columnNames, oldValues, newValues);

      // Simulate access patterns
      _ = audit.GetOldValue("Name");
      _ = audit.GetNewValue("Age");
   }

   /// <summary>Benchmark: Original dictionary-based approach for large change sets.</summary>
   [Benchmark]
   public void OriginalDictionary_LargeChangeSet()
   {
      var audit = new Audit<int, IdentityUser<int>, int>
                  {
                     OldValues = new(_largeChangeSet),
                     NewValues = new(_largeChangeSet)
                  };

      // Simulate access patterns
      _ = audit.OldValues["Property0"];
      _ = audit.NewValues["Property5"];
   }

   /// <summary>Benchmark: Optimized JSON-based approach for large change sets.</summary>
   [Benchmark]
   public void OptimizedJson_LargeChangeSet()
   {
      var audit = new Audit<int, IdentityUser<int>, int>();
      audit.SetValuesFromJson(_largeJsonOld, _largeJsonNew);

      // Simulate access patterns
      _ = audit.GetOldValue("Property0");
      _ = audit.GetNewValue("Property5");
   }

   /// <summary>Benchmark: Optimized Span-based approach for large change sets.</summary>
   [Benchmark]
   public void OptimizedSpan_LargeChangeSet()
   {
      var audit       = new Audit<int, IdentityUser<int>, int>();
      var columnNames = new string[10];
      var oldValues   = new object[10];
      var newValues   = new object[10];

      for (var i = 0; i < 10; i++)
      {
         columnNames[i] = $"Property{i}";
         oldValues[i]   = $"Value{i}";
         newValues[i]   = $"NewValue{i}";
      }

      audit.SetValuesFromSpan<object>(columnNames.AsSpan(), oldValues.AsSpan(), newValues.AsSpan());

      // Simulate access patterns
      _ = audit.GetOldValue("Property0");
      _ = audit.GetNewValue("Property5");
   }

   /// <summary>Benchmark: JSON serialization overhead in original approach.</summary>
   [Benchmark]
   public void JsonSerialization_Original()
   {
      var values       = new Dictionary<string, object>(_smallChangeSet);
      var json         = JsonSerializer.Serialize(values);
      var deserialized = JsonSerializer.Deserialize<Dictionary<string, object>>(json);
      _ = deserialized?["Name"];
   }

   /// <summary>Benchmark: Optimized JSON handling.</summary>
   [Benchmark]
   public void JsonSerialization_Optimized()
   {
      var audit = new Audit<int, IdentityUser<int>, int>();
      audit.SetValuesFromJson(_smallJsonOld, _smallJsonNew);
      _ = audit.GetOldValue("Name");
   }

   /// <summary>Benchmark: Memory allocation for multiple audit records (simulates bulk operations).</summary>
   [Benchmark]
   [Arguments(100)]
   [Arguments(1000)]
   public void BulkAuditCreation_Original(int count)
   {
      var audits = new List<Audit<int, IdentityUser<int>, int>>(count);

      for (var i = 0; i < count; i++)
      {
         var audit = new Audit<int, IdentityUser<int>, int>
                     {
                        OldValues = new(_smallChangeSet),
                        NewValues = new(_smallChangeSet)
                     };
         audits.Add(audit);
      }
   }

   /// <summary>Benchmark: Memory allocation for multiple optimized audit records.</summary>
   [Benchmark]
   [Arguments(100)]
   [Arguments(1000)]
   public void BulkAuditCreation_Optimized(int count)
   {
      var audits = new List<Audit<int, IdentityUser<int>, int>>(count);

      for (var i = 0; i < count; i++)
      {
         var audit = new Audit<int, IdentityUser<int>, int>();
         audit.SetValuesFromJson(_smallJsonOld, _smallJsonNew);
         audits.Add(audit);
      }
   }

   /// <summary>Benchmark: Single property lookup performance.</summary>
   [Benchmark]
   public void PropertyLookup_Original()
   {
      var audit = new Audit<int, IdentityUser<int>, int>
                  {
                     OldValues = new(_largeChangeSet)
                  };

      // Multiple lookups to simulate real usage
      for (var i = 0; i < 10; i++)
         _ = audit.OldValues[$"Property{i % 10}"];
   }

   /// <summary>Benchmark: Single property lookup with optimized JSON parsing.</summary>
   [Benchmark]
   public void PropertyLookup_Optimized()
   {
      var audit = new Audit<int, IdentityUser<int>, int>();
      audit.SetValuesFromJson(_largeJsonOld, null);

      // Multiple lookups to simulate real usage
      for (var i = 0; i < 10; i++)
         _ = audit.GetOldValue($"Property{i % 10}");
   }
}