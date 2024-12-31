namespace Frent.Systems;
public readonly struct Rule(Type type, RuleTypes ruleTypes, CustomQueryDelegate? customDelegate = null)
{
    public readonly CustomQueryDelegate? CustomOperator = customDelegate;
    public readonly Type Type = type;
    public readonly RuleTypes RuleTypes = ruleTypes;
    public override int GetHashCode() => HashCode.Combine(Type, RuleTypes, CustomOperator);

    public static Rule With<T>() => new Rule(typeof(T), RuleTypes.Have);
}
