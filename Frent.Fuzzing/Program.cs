using Frent.Fuzzing;
using Frent.Fuzzing.Runner;

args = ["12", "1000"];

if (args.Length == 0)
{
    Fuzzer.CreateFuzzProcesses();
}
else
{
    InconsistencyException? e = WorldState.Fuzz(args);

    if(e is not null)
    {
        Console.WriteLine($"Seed {e.Seed} failed at {e.FailedStep}");
    }
}