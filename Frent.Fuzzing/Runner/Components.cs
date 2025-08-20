using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Frent.Fuzzing.Runner;

internal record struct S1(Random random)
{
    public int Value = random.Next();
    public override string ToString() => Value.ToString();
}
internal record struct S2(Random random)
{
    public int Value = random.Next();
    public override string ToString() => Value.ToString();
}
internal record struct S3(Random random)
{
    public int Value = random.Next();
    public override string ToString() => Value.ToString();
}

internal class C1(Random random)
{
    public int Value = random.Next();
    public override string ToString() => Value.ToString();
}
internal class C2(Random random)
{
    public int Value = random.Next();
    public override string ToString() => Value.ToString();
}
internal class C3(Random random)
{
    public int Value = random.Next();
    public override string ToString() => Value.ToString();
}