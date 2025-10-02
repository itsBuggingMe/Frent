using System.Buffers;
using System.Diagnostics;
using System.Numerics;
using System.Text.Json;

namespace Frent.Serialization;

internal ref struct StreamJsonReader
{
    const int InitalBufferSize = 4096;

    public Utf8JsonReader CurrentReader;

    private Stream _stream;
    private byte[] _currentBuffer;
    private int _currentBufferSize;
    private bool _isLastBlock;

    private byte[] _capturedBuffer;
    private int _capturedLength;
    private JsonReaderState _capturedState;

    private bool _lastReadFetchedStreamData;

    public StreamJsonReader(Stream stream)
    {
        _stream = stream;
        _currentBuffer = ArrayPool<byte>.Shared.Rent(InitalBufferSize);
        _capturedBuffer = ArrayPool<byte>.Shared.Rent(InitalBufferSize);
    }

    public bool Read()
    {
        _lastReadFetchedStreamData = false;

        while (!CurrentReader.Read())
        {
            if (_isLastBlock)
                return false;

            _lastReadFetchedStreamData = true;

            int bytesLeft = _currentBufferSize - (int)CurrentReader.BytesConsumed;

            // Copy half a token to the front of the buffer
            if(CurrentReader.BytesConsumed != 0)
                _currentBuffer.AsSpan((int)CurrentReader.BytesConsumed, bytesLeft).CopyTo(_currentBuffer);

            // read some bytes

            if(bytesLeft == _currentBuffer.Length)
            {// we filled up the whole buffer, and still cant read a token
                byte[] oldBuffer = _currentBuffer;

                _currentBuffer = ArrayPool<byte>.Shared.Rent(_currentBuffer.Length << 1);

                oldBuffer.CopyTo(_currentBuffer);

                ArrayPool<byte>.Shared.Return(oldBuffer);
            }

            int bytesRead = _stream.Read(_currentBuffer, bytesLeft, _currentBuffer.Length - bytesLeft);

            _isLastBlock = bytesRead == 0;
            _currentBufferSize = bytesRead + bytesLeft;

            CurrentReader = new Utf8JsonReader(_currentBuffer.AsSpan(0, _currentBufferSize), _isLastBlock, CurrentReader.CurrentState);
        }

        return true;
    }

    /// <summary>
    /// Captures and stores the child elements of the current token
    /// </summary>
    public void Capture()
    {
        int depth = 0;
        int initalByteStartPosition = (int)CurrentReader.BytesConsumed;
        _capturedState = CurrentReader.CurrentState;
        _capturedLength = 0;

        do
        {
            if(!Read())
                throw new JsonException("Reached end of stream unexpectedly.");

            // if we read more from the stream, we need to adjust the start position
            if (_lastReadFetchedStreamData)
                initalByteStartPosition = 0;

            WriteSegment(ref this, _currentBuffer.AsSpan(initalByteStartPosition, (int)CurrentReader.BytesConsumed - initalByteStartPosition));

            initalByteStartPosition = (int)CurrentReader.BytesConsumed;

            if (CurrentReader.TokenType is JsonTokenType.StartObject or JsonTokenType.StartArray)
                depth++;
            else if (CurrentReader.TokenType is JsonTokenType.EndObject or JsonTokenType.EndArray)
                depth--;

        } while(depth > 0);


        static void WriteSegment(ref StreamJsonReader streamJsonReader, ReadOnlySpan<byte> bytes)
        {
            int destStart = streamJsonReader._capturedLength;
            int destEnd = destStart + bytes.Length;
            
            if (destEnd > streamJsonReader._capturedBuffer.Length)
            {
                var newBuffer = ArrayPool<byte>.Shared.Rent((int)BitOperations.RoundUpToPowerOf2((uint)(destEnd + 1)));
                streamJsonReader._capturedBuffer.AsSpan().CopyTo(newBuffer);
                streamJsonReader._capturedBuffer = newBuffer;
            }

            bytes.CopyTo(streamJsonReader._capturedBuffer.AsSpan(destStart, bytes.Length));
            streamJsonReader._capturedLength += bytes.Length;
        }
    }

    /// <summary>
    /// Gets a reader from the last capture call. Guanranteed to be have enough tokens for the children.
    /// </summary>
    public Utf8JsonReader Restore()
    {        
        var reader = new Utf8JsonReader(_capturedBuffer.AsSpan(0, _capturedLength), true, _capturedState);

        return reader;
    }

    public void Dispose()
    {
        ArrayPool<byte>.Shared.Return(_currentBuffer);
        ArrayPool<byte>.Shared.Return(_capturedBuffer);
    }
}
