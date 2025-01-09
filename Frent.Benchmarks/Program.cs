using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using Frent.Collections;
using Frent.Core;
using System.Diagnostics;

namespace Frent.Benchmarks;

public class Program
{
    static void Main(string[] args)
    {
        new HollisticBenchmark().Setup();
        BenchmarkRunner.Run<HollisticBenchmark>();
    }
}