using Frent.Collections;
using Frent.Updating;
using Frent.Core.Archetypes;

namespace Frent.Core;

internal class IDTypeFilter(
    ComponentID[] includeComponents,
    ComponentID[] excludeComponents,
    TagID[] includeTags,
    TagID[] excludeTags)
{
    public static readonly IDTypeFilter None = new([], [], [], []);

    public readonly ComponentID[] IncludeC = includeComponents;
    public readonly Bitset IncludedSparseComponents;
    public readonly ComponentID[] ExcludeC = excludeComponents;
    public readonly Bitset ExcludedSparseComponents;
    public readonly TagID[] IncludeT = includeTags;
    public readonly TagID[] ExcludeT = excludeTags;

    public bool FilterArchetype(Archetype archetype)
    {
        if (ReferenceEquals(this, None))
            return true;

        foreach (var comp in IncludeC)
        {
            if (archetype.GetComponentIndex(comp) == 0)
                return false;
        }

        foreach (var comp in ExcludeC)
        {
            if (archetype.GetComponentIndex(comp) != 0)
                return false;
        }

        foreach (var tag in IncludeT)
        {
            if (!archetype.HasTag(tag))
                return false;
        }

        foreach (var tag in ExcludeT)
        {
            if (archetype.HasTag(tag))
                return false;
        }

        return true;
    }

    internal static IDTypeFilter[] CreateComponentIDFilters(UpdateMethodData[] methods)
    {
        IDTypeFilter[]? componentIDTypeFilter = null;

        for (int i = methods.Length - 1; i >= 0; i--)
        {
            ref UpdateMethodData data = ref methods[i];

            if (TypeFilterRecord.None == data.TypeFilterRecord)
                continue;

            componentIDTypeFilter ??= new IDTypeFilter[i + 1];

            componentIDTypeFilter[i] = CreateFromRecordCore(in data.TypeFilterRecord);
        }

        return componentIDTypeFilter ?? [];
    }

    private static IDTypeFilter CreateFromRecordCore(in TypeFilterRecord typeFilterRecord)
    {
        return new IDTypeFilter(
            ConvertComponents(typeFilterRecord.IncludeComponents),
            ConvertComponents(typeFilterRecord.ExcludeComponents),
            ConvertTags(typeFilterRecord.IncludeTags),
            ConvertTags(typeFilterRecord.ExcludeTags));

        static ComponentID[] ConvertComponents(Type[] types)
        {
            ComponentID[] componentIds = new ComponentID[types.Length];
            for (int i = 0; i < componentIds.Length; i++)
                componentIds[i] = Component.GetComponentID(types[i]);
            return componentIds;
        }

        static TagID[] ConvertTags(Type[] types)
        {
            TagID[] componentIds = new TagID[types.Length];
            for (int i = 0; i < componentIds.Length; i++)
                componentIds[i] = Tag.GetTagID(types[i]);
            return componentIds;
        }
    }
}
