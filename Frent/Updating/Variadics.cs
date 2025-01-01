namespace Frent.Updating;

internal class Variadics
{
    public const string UpdateArgFrom = ", TArg>";
    public const string UpdateArgPattern = ", |TArg$, |>";

    public const string InterfaceFrom = "Component<TArg>";
    public const string InterfacePattern = "Component<|TArg$, |>";

    public const string GetCompSpanFrom = "        var a1 = b.GetComponentSpan<TArg>();";
    public const string GetCompSpanPattern = "|        var a$ = b.GetComponentSpan<TArg$>();\n|";

    public const string GetChunkFrom = "            ref Chunk<TArg> ca = ref a1[i];";
    public const string GetChunkPattern = "|            ref Chunk<TArg$> ca$ = ref a$[i];\n|";

    public const string CallArgFrom = "ref ca[j]";
    public const string CallArgPattern = "|ref ca$[j], |";

    public const string GetChunkLastFrom = "        var caLast = a1[..(b.ChunkCount + 1)][^1].AsSpan(0, b.LastChunkComponentCount);";
    public const string GetChunkLastPattern = "|        var caLast$ = a$[..(b.ChunkCount + 1)][^1].AsSpan(0, b.LastChunkComponentCount);\n|";

    public const string CallArgLastFrom = "ref caLast[j]";
    public const string CallArgLastPattern = "|ref caLast$[j], |";
}

