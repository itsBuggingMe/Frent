namespace Frent.Fuzzing;

internal class InconsistencyException(int failedStep, int seed) : Exception($"Failed at step {failedStep} with seed {seed}")
{
    public int FailedStep = failedStep;
    public int Seed = seed;
}