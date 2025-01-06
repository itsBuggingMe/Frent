namespace Frent;

/// <summary>
/// Config information for a <see cref="World"/>
/// </summary>
/// <param name="threadCount">The number of threads to use when multithreading</param>
/// <param name="multiThreadedUpdate">Whether or not to multithread <see cref="World.Update"/></param>
public class Config(int threadCount, bool multiThreadedUpdate)
{
    /// <summary>
    /// The number of threads to use when multithreading
    /// </summary>
    public int ThreadCount { get; } = threadCount;
    /// <summary>
    /// Whether or not to multithread <see cref="World.Update"/>
    /// </summary>
    public bool MultiThreadedUpdate { get; } = multiThreadedUpdate;

    /// <summary>
    /// The default multithreaded config
    /// </summary>
    public static Config Multithreaded { get; } = new Config(Math.Min(1, Environment.ProcessorCount - 2), true);
    /// <summary>
    /// The default singlethreaded config
    /// </summary>
    public static Config Singlethreaded { get; } = new Config(1, false);
}
