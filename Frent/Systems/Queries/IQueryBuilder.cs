using Frent.Collections;

namespace Frent.Systems.Queries;

public interface IQueryBuilder
{
    void AddRules(List<Rule> rules);
    World? World { get; }
}