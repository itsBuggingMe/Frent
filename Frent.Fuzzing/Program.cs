using Frent.Fuzzing;
using Frent.Fuzzing.Runner;

args = ["0", "1000"];
if (args.Length == 0)
{
    Fuzzer.CreateFuzzProcesses();
}
else
{
    Assert.Fuzz(args);
}