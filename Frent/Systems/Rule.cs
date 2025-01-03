namespace Frent.Systems;
public readonly struct Rule
{
    public Rule(Type type, RuleTypes ruleTypes, CustomQueryDelegate? customDelegate = null)
    {
        CustomOperator = customDelegate;
        Type = type;
        RuleTypes = ruleTypes;
    }

    public readonly CustomQueryDelegate? CustomOperator;
    public readonly Type Type;
    public readonly RuleTypes RuleTypes;
    public override int GetHashCode() => HashCode.Combine(Type, RuleTypes, CustomOperator);

    public static Rule With<T>() => new Rule(typeof(T), RuleTypes.Have);
}
