// Copyright (c) 2014-2025 Sarin Na Wangkanai, All Rights Reserved.

using BenchmarkDotNet.Running;

using Wangkanai.Foundation;

Console.WriteLine("Choose benchmark mode:");
Console.WriteLine("1. Quick Performance Validation (recommended)");
Console.WriteLine("2. Full BenchmarkDotNet Suite");
Console.Write("Enter choice (1 or 2): ");

var choice = Console.ReadLine();

if (choice == "1")
   QuickPerformanceValidation.RunValidation();
else
{
   Console.WriteLine("Running Full Wangkanai Domain Benchmarks...");
   Console.WriteLine();

   // Run ValueObject benchmarks
   var valueObjectSummary = BenchmarkRunner.Run<ValueObjectPerformanceBenchmark>();
   Console.WriteLine();

   // Run Domain benchmarks
   var domainSummary = BenchmarkRunner.Run<DomainBenchmark>();
   Console.WriteLine();

   Console.WriteLine("All benchmarks completed successfully!");
}