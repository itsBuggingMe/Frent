namespace Frent.Core;

/// <summary>
/// Specifies the level of memory trimming Frent's internal buffers should do
/// </summary>
public enum MemoryTrimming
{
    /// <summary>
    /// Always trim buffers when there is memory pressure
    /// </summary>
    Always = 0,
    /// <summary>
    /// Trim buffers a balanced amount
    /// </summary>
    Normal = 1,
    /// <summary>
    /// Never trim buffers, even when there is memory pressure
    /// </summary>
    Never = 2,
}