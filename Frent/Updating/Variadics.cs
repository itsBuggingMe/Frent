namespace Frent.Updating;

internal static class Variadics
{
    public const string GetSpanFrom = "b.GetComponentSpan<TArg>()";
    public const string GetSpanPattern = "|b.GetComponentSpan<TArg$>(), |";

    public const string GenArgFrom = "TArg>";
    public const string GenArgPattern = "|TArg$, |>";

    public const string GetArgFrom = "ref TArg t1";
    public const string GetArgPattern = "|ref TArg$ t$, |";

    public const string PutArgFrom = "ref t1";
    public const string PutArgPattern = "|ref t$, |";
}

