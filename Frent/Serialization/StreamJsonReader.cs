using System.Buffers;
using System.Text.Json;

namespace Frent.Serialization;

internal ref struct StreamJsonReader
{
    private Stream _stream;
    private Utf8JsonReader _currentReader;
    private byte[] _currentBuffer;

    public StreamJsonReader(Stream stream, Stack<JsonDeserializationState> statePool)
    {
        _stream = stream;
        _currentReader = new Utf8JsonReader();
        _currentBuffer = ArrayPool<byte>.Shared.Rent(4096);
    }

    public readonly bool Read()
    {

    }

    public readonly JsonTokenType TokenType => _currentReader.TokenType;
    public readonly JsonDeserializationState Capture() => new(_currentBuffer, _currentReader.CurrentState, true);
}
