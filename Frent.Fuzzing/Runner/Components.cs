using Frent.Components;

namespace Frent.Fuzzing.Runner;

internal record struct S1(int value)
{
    public int Value = value;
    public override string ToString() => Value.ToString();
}
internal record struct S2(int value)
{
    public int Value = value;
    public override string ToString() => Value.ToString();
}
internal record struct S3(int value) : ISparseComponent
{
    public int Value = value;
    public override string ToString() => Value.ToString();
}
internal record struct S4(int value)
{
    public int Value = value;
    public override string ToString() => Value.ToString();
}
internal class C1(int value)
{
    public int Value = value;
    public override string ToString() => Value.ToString();
}
internal class C2(int value)
{
    public int Value = value;
    public override string ToString() => Value.ToString();
}
internal class C3(int value) : ISparseComponent
{
    public int Value = value;
    public override string ToString() => Value.ToString();
}
internal class C4(int value) : ISparseComponent
{
    public int Value = value;
    public override string ToString() => Value.ToString();
}