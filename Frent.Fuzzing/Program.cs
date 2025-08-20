using Frent.Fuzzing.Runner;

namespace Frent.Fuzzing;

internal class Program
{
    static void Main(string[] args)
    {
        if(args.Length == 0)
        {
            
        }
        else
        {
            Assert.Fuzz(args);
        }
    }

    private static void LaunchFuzzProcess(int seed, int count)
    {
        
    }
}
