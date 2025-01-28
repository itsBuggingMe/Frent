namespace Frent;
internal static class AttributeHelpers
{
    internal const string DebuggerDisplay = "{DebuggerDisplayString,nq}";

    public const string GetSpanFrom = "b.GetComponentSpan<TArg>()";
    public const string GetSpanPattern = "|b.GetComponentSpan<TArg$>(), |";

    public const string GenArgFrom = "TArg>";
    public const string GenArgPattern = "|TArg$, |>";

    public const string GetArgFrom = "ref TArg t1";
    public const string GetArgPattern = "|ref TArg$ t$, |";

    public const string PutArgFrom = "ref t1";
    public const string PutArgPattern = "|ref t$, |";

    public const string TArgFrom = "TArg>";
    public const string TArgPattern = "|TArg$, |>";

    public const string RefArgFrom = "ref TArg arg";
    public const string RefArgPattern = "|ref TArg$ arg$, |";

    public const string SpanArgFrom = "Span<TArg> arg";
    public const string SpanArgPattern = "|Span<TArg$> arg$, |";
}
