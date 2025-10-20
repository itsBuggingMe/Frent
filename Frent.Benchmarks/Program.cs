using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using Frent.Core;
using Frent.Serialization;
using Frent.Systems;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using static Frent.Benchmarks.Program;

namespace Frent.Benchmarks;

public class Program
{
    static void Main(string[] args) => RunBenchmark<AddRemove>(m => m.Sparse());

    #region Bench Helpers
    private static void RunBenchmark<T>(Action<T> disasmCall)
    {
        if (Environment.GetEnvironmentVariable("DISASM") == "TRUE" ||
#if DEBUG
            true
#else
            false
#endif
            )
        {
            JitTest(disasmCall);
        }
        else
        {
            BenchmarkRunner.Run<T>();
            JitTest(disasmCall);
        }
    }

    public class ChunkedStream : Stream
    {
        private readonly Stream _innerStream;
        private readonly int _chunkSize;

        public ChunkedStream(Stream innerStream, int chunkSize)
        {
            _innerStream = innerStream ?? throw new ArgumentNullException(nameof(innerStream));
            if (chunkSize <= 0)
                throw new ArgumentOutOfRangeException(nameof(chunkSize), "Chunk size must be positive.");

            _chunkSize = chunkSize;
        }

        public override bool CanRead => _innerStream.CanRead;
        public override bool CanSeek => _innerStream.CanSeek;
        public override bool CanWrite => _innerStream.CanWrite;
        public override long Length => _innerStream.Length;

        public override long Position
        {
            get => _innerStream.Position;
            set => _innerStream.Position = value;
        }

        public override void Flush() => _innerStream.Flush();

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (buffer == null)
                throw new ArgumentNullException(nameof(buffer));
            if (offset < 0 || count < 0 || (offset + count) > buffer.Length)
                throw new ArgumentOutOfRangeException();

            // Restrict reads to at most _chunkSize
            int toRead = Math.Min(count, _chunkSize);
            return _innerStream.Read(buffer, offset, toRead);
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            if (buffer == null)
                throw new ArgumentNullException(nameof(buffer));
            if (offset < 0 || count < 0 || (offset + count) > buffer.Length)
                throw new ArgumentOutOfRangeException();

            int remaining = count;
            int currentOffset = offset;

            while (remaining > 0)
            {
                int toWrite = Math.Min(remaining, _chunkSize);
                _innerStream.Write(buffer, currentOffset, toWrite);
                currentOffset += toWrite;
                remaining -= toWrite;
            }
        }

        public override long Seek(long offset, SeekOrigin origin) =>
            _innerStream.Seek(offset, origin);

        public override void SetLength(long value) =>
            _innerStream.SetLength(value);

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _innerStream.Dispose();
            }
            base.Dispose(disposing);
        }
    }
    private static void JitTest<T>(Action<T> call)
    {
        T t = Activator.CreateInstance<T>();
        t.GetType()
            .GetMethods()
            .FirstOrDefault(m => m.GetCustomAttribute<GlobalSetupAttribute>() is not null)
            ?.Invoke(t, []);

        //jit warmup
        for (int i = 0; i < 5; i++)
        {
            for (int j = 0; j < 32; j++)
                call(t);
            Thread.Sleep(100);  
        }
    }

    //agg opt because i suspect pgo devirtualizes the call
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    private static void ProfileTest<T>(Action<T> call)
    {
        T t = Activator.CreateInstance<T>();
        t.GetType()
            .GetMethods()
            .FirstOrDefault(m => m.GetCustomAttribute<GlobalSetupAttribute>() is not null)
            ?.Invoke(t, []);

        while(true)
        {
            call(t);
        }
    }
    #endregion

    internal struct Increment : IAction<Component1>
    {
        public void Run(ref Component1 arg) => arg.Value++;
    }

    internal record struct Component1(int Value);

    internal record struct Component2(int Value);

    internal record struct Component3(int Value);

    internal record struct Component4(int Value);

    internal record struct Component5(int Value);
}