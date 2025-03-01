namespace Frent;
internal static class AttributeHelpers
{
    internal const string DebuggerDisplay = "{DebuggerDisplayString,nq}";

    public const string GetComponentRefFrom = "        ref TArg arg = ref b.GetComponentDataReference<TArg>();";
    public const string GetComponentRefPattern = "|        ref TArg$ arg$ = ref b.GetComponentDataReference<TArg$>();\n|";

    public const string IncRefFrom = "            arg = ref Unsafe.Add(ref arg, 1);";
    public const string IncRefPattern = "|            arg$ = ref Unsafe.Add(ref arg$, 1);\n|";

    public const string PutArgFrom = "ref arg);";
    public const string PutArgPattern = "|ref arg$, |);";

    public const string TArgFrom = "TArg>";
    public const string TArgPattern = "|TArg$, |>";

    public const string RefArgFrom = "ref TArg arg";
    public const string RefArgPattern = "|ref TArg$ arg$, |";

    public const string SpanArgFrom = "Span<TArg> arg";
    public const string SpanArgPattern = "|Span<TArg$> arg$, |";
}
