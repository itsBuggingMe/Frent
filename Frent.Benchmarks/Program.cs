using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using System.Reflection;


namespace Frent.Benchmarks;

public class Program
{
    static void Main(string[] args) => RunBenchmark<MicroBenchmark>(m => m.Has());

    #region Bench Helpers
    private static void RunBenchmark<T>(Action<T> disasmCall )
    {
        if (Environment.GetEnvironmentVariable("DISASM") == "TRUE" ||
#if DEBUG
            true
#else
            false
#endif
            )
            JitTest<MicroBenchmark>(b => b.Has());
        else
            BenchmarkRunner.Run<MicroBenchmark>();
    }

    private static void JitTest<T>(Action<T> call)
    {
        T t = Activator.CreateInstance<T>();
        t.GetType()
            .GetMethods()
            .FirstOrDefault(m => m.GetCustomAttribute<GlobalSetupAttribute>() is not null)
            ?.Invoke(t, []);

        //jit warmup
        for(int i = 0; i < 5; i++)
        {
            for (int j = 0; j < 32; j++)
                call(t);
            Thread.Sleep(100);
        }
    }
#endregion
}