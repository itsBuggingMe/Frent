namespace Frent.Fuzzing;

[AttributeUsage(AttributeTargets.Field)]
internal sealed class Weight(float value) : Attribute
{
    public float Value => value;
}