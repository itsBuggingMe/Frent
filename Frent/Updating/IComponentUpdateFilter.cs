namespace Frent.Updating;

internal interface IComponentUpdateFilter
{
    public void UpdateSubset(ReadOnlySpan<ArchetypeDeferredUpdateRecord> archetypes);
}
