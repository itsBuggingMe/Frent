using Frent.Fuzzing;
using Frent.Fuzzing.Runner;

if (args.Length == 0 && false)
{
    Fuzzer.CreateFuzzProcesses();
}
else
{
    InconsistencyException? e = WorldState.Fuzz(["1", "1000"], true, out int seed);

    if(e is not null)
    {
        Console.Write($"Seed {e.Seed} failed at {e.FailedStep}");
    }
    else
    {
        Console.Write($"Seed {seed} passed");
    }
}