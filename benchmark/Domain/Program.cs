// Copyright (c) 2014-2025 Sarin Na Wangkanai, All Rights Reserved.

using BenchmarkDotNet.Running;
using Wangkanai.Benchmark;

Console.WriteLine("Running Wangkanai Domain Benchmarks...");
Console.WriteLine();

// Run ValueObject benchmarks
var valueObjectSummary = BenchmarkRunner.Run<ValueObjectPerformanceBenchmark>();
Console.WriteLine();

// Run Domain benchmarks
var domainSummary = BenchmarkRunner.Run<DomainBenchmark>();
Console.WriteLine();

Console.WriteLine("All benchmarks completed successfully!");