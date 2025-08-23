namespace Frent.Fuzzing;

internal class InconsistencyException(string message, int failedStep, int seed) : Exception($"Assert {message} failed at step {failedStep} with seed {seed}.")
{
    public int FailedStep = failedStep;
    public int Seed = seed;
}