namespace Frent.Core;

internal record class ComponentIDTypeFilter(ComponentID[] IncludeComponents, ComponentID[] ExcludeComponents, TagID[] IncludeTags, TagID[] ExcludeTags)
{
    public static readonly ComponentIDTypeFilter None = new([], [], [], []);

    public bool FilterArchetype(Archetype archetype)
    {
        if (ReferenceEquals(this, None))
            return true;

        ComponentID[] includes = IncludeComponents;
        for (int i = 0; i < includes.Length; i++)
        {
            if (archetype.GetComponentIndex(includes[i]) == 0)
                return false;
        }

        ComponentID[] excludes = ExcludeComponents;
        for (int i = 0; i < excludes.Length; i++)
        {
            if (archetype.GetComponentIndex(excludes[i]) != 0)
                return false;
        }

        TagID[] includesT = IncludeTags;
        for (int i = 0; i < includes.Length; i++)
        {
            if (!archetype.HasTag(includesT[i]))
                return false;
        }

        TagID[] excludesT = ExcludeTags;
        for (int i = 0; i < excludes.Length; i++)
        {
            if (archetype.HasTag(excludesT[i]))
                return false;
        }

        return true;
    }
}
