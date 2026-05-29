using static NUnit.Framework.Assert;

namespace Frent.Tests.Helpers;

internal sealed class CallHelper
{
    private int _calledCount;

    public void Call()
    {
        _calledCount++;
    }

    public void AssertCalled(int calledCount = 1)
    {
        That(_calledCount, Is.EqualTo(calledCount));
    }
}
