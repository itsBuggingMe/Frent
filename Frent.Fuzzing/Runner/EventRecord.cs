namespace Frent.Fuzzing.Runner;
internal record struct EventRecord(HashSet<Entity?> Subscribed, int InvocationCount);
