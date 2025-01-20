namespace Frent;

/// <summary>
/// Config information for a <see cref="World"/>
/// </summary>
/// <param name="threadCount">The number of threads to use when multithreading</param>
/// <param name="multiThreadedUpdate">Whether or not to multithread <see cref="World.Update"/></param>
public class Config
{
    /// <summary>
    /// Whether or not to multithread <see cref="World.Update"/>
    /// </summary>
    public bool MultiThreadedUpdate { get; init; }

    /// <summary>
    /// The default multithreaded config
    /// </summary>
    public static Config Multithreaded { get; } = new Config() { MultiThreadedUpdate = true };
    /// <summary>
    /// The default singlethreaded config
    /// </summary>
    public static Config Singlethreaded { get; } = new Config() { MultiThreadedUpdate = false };
}
