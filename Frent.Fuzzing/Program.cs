using Frent.Fuzzing;
using Frent.Fuzzing.Runner;

if (args.Length == 0)
{
    Fuzzer.CreateFuzzProcesses();
}
else
{
    Assert.Fuzz(args);
}

Console.WriteLine("Done!");