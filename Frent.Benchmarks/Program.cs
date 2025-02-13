using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using Frent.Systems;
using Frent.Core;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Frent.Benchmarks;

public class Program
{
    static void Main(string[] args) => RunBenchmark<MicroBenchmark>(m => m.AddRem());

    #region Bench Helpers
    private static void RunBenchmark<T>(Action<T> disasmCall)
    {
        if (Environment.GetEnvironmentVariable("DISASM") == "TRUE" ||
#if DEBUG
            true
#else
            false
#endif
            )
        {
            JitTest(disasmCall);
        }
        else
        {
            ProfileTest(disasmCall);
            JitTest(disasmCall);
        }
    }

    private static void JitTest<T>(Action<T> call)
    {
        T t = Activator.CreateInstance<T>();
        t.GetType()
            .GetMethods()
            .FirstOrDefault(m => m.GetCustomAttribute<GlobalSetupAttribute>() is not null)
            ?.Invoke(t, []);

        //jit warmup
        for (int i = 0; i < 5; i++)
        {
            for (int j = 0; j < 32; j++)
                call(t);
            Thread.Sleep(100);
        }
    }

    private static void ProfileTest<T>(Action<T> call)
    {
        T t = Activator.CreateInstance<T>();
        t.GetType()
            .GetMethods()
            .FirstOrDefault(m => m.GetCustomAttribute<GlobalSetupAttribute>() is not null)
            ?.Invoke(t, []);

        while(true)
        {
            call(t);
        }
    }
    #endregion

    internal struct Increment : IAction<Component1>
    {
        public void Run(ref Component1 arg) => arg.Value++;
    }

    internal record struct Component1(int Value);

    internal record struct Component2(int Value);

    internal record struct Component3(int Value);

    internal record struct Component4(int Value);

    internal record struct Component5(int Value);
}