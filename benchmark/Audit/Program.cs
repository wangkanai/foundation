// Copyright (c) 2014-2025 Sarin Na Wangkanai, All Rights Reserved.

using BenchmarkDotNet.Running;
using Wangkanai.Audit.Benchmarks;

Console.WriteLine("=== Wangkanai.Audit Performance Benchmarks ===");
Console.WriteLine();

// Run the audit performance benchmarks
var summary = BenchmarkRunner.Run<AuditPerformanceBenchmark>();

Console.WriteLine("Benchmark completed. Results saved to BenchmarkDotNet.Artifacts folder.");
Console.WriteLine("Summary:");
Console.WriteLine($"- Total benchmarks: {summary.Reports.Length}");
Console.WriteLine($"- Runtime: {summary.HostEnvironmentInfo.DotNetSdkVersion}");
Console.WriteLine($"- Platform: {summary.HostEnvironmentInfo.Architecture}");