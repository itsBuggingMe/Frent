using Frent.Fuzzing;
using Frent.Fuzzing.Runner;

if (args.Length == 0)
{
    Fuzzer.CreateFuzzProcesses();
}
else
{
    WorldState.Fuzz(args);
}

Console.WriteLine("Done!");