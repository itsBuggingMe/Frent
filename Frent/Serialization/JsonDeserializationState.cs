using System.Text.Json;

namespace Frent.Serialization;

internal readonly struct JsonDeserializationState(byte[] buffer, JsonReaderState readerState, bool isFinalBlock)
{
    private readonly byte[] _buffer = buffer;
    private readonly JsonReaderState _readerState = readerState;
    private readonly bool _isFinalBlock = isFinalBlock;

    public readonly Utf8JsonReader CreateReader() => new(_buffer, _isFinalBlock, _readerState);
}