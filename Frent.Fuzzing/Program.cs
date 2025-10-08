using Frent.Fuzzing;
using Frent.Fuzzing.Runner;

if (args.Length == 0)
{
    Fuzzer.CreateFuzzProcesses();
}
else
{
    InconsistencyException? e = WorldState.Fuzz(args, true);

    if(e is not null)
    {
        Console.WriteLine($"Seed {e.Seed} failed at {e.FailedStep}");
    }
}