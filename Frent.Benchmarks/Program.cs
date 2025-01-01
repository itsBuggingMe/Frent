using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;

namespace Frent.Benchmarks;

[MemoryDiagnoser]
[DisassemblyDiagnoser(3)]
public class Program
{
    static void Main(string[] args) => BenchmarkRunner.Run<Program>();

    private Entity[] _sharedEntityBuffer100k = null!;
    private World world = null!;

    [GlobalSetup]
    public void Setup()
    {

    }

    [Benchmark]
    public void RunEntities()
    {
        
    }
}