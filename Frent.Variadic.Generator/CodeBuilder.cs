using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;

namespace Frent.Variadic.Generator;
internal class CodeBuilder
{
    private StringBuilder _builder;
    public CodeBuilder(int arity)
    {
        _builder = new StringBuilder();
        Arity = arity;
    }

    public CodeBuilder(int arity, int capacity)
    {
        _builder = new StringBuilder(capacity);
        Arity = arity;
    }

    public int Arity { get; private set; }
    public StringBuilder Builder => _builder;

    public int Length { get => _builder.Length; set => _builder.Length = value; }
    public int Capacity { get => _builder.Capacity; set => _builder.Capacity = value; }
    public int MaxCapacity => _builder.MaxCapacity;
    public int IndentationLevel { get; set; }
    public const int TabsPerIndent = 4;

    public CodeBuilder Recycle(int newArity)
    {
        Builder.Clear();
        Arity = newArity;
        return this;
    }

    public CodeBuilder Loop<TState>(IEnumerable<TState> enumerable, Action<CodeBuilder, TState> onEach, CancellationToken ct = default)
    {
        foreach (var item in enumerable)
        {
            onEach(this, item);
            ct.ThrowIfCancellationRequested();
        }
        return this;
    }

    public CodeBuilder Break()
    {
        if (Debugger.IsAttached)
            Debugger.Break();
        else
            Debugger.Launch();
        return this;
    }
    public CodeBuilder BackTrack(int chars)
    {
        _builder.Remove(_builder.Length - chars, chars);
        return this;
    }

    public CodeBuilder Indent()
    {
        IndentationLevel++;
        _builder.Append(' ', TabsPerIndent);
        return this;
    }

    public CodeBuilder Outdent()
    {
        var newIndent = IndentationLevel - 1;
        if (newIndent < 0)
            throw new InvalidOperationException($"Indentation level must be positive");
        IndentationLevel = newIndent;
        if (_builder[_builder.Length - 1] == ' '
            && _builder[_builder.Length - 2] == ' '
            && _builder[_builder.Length - 3] == ' '
            && _builder[_builder.Length - 4] == ' ')
        {
            _builder.Remove(_builder.Length - 4, 4);
        }
        return this;
    }

    public CodeBuilder AppendLine()
    {
        _builder.AppendLine();
        _builder.Append(' ', IndentationLevel * TabsPerIndent);
        return this;
    }

    public CodeBuilder AppendLine(string value)
    {
        _builder.AppendLine(value);
        _builder.Append(' ', IndentationLevel * TabsPerIndent);
        return this;
    }

    public CodeBuilder AppendLine(char value)
    {
        _builder.Append(value).AppendLine();
        _builder.Append(' ', IndentationLevel * TabsPerIndent);
        return this;
    }

    public CodeBuilder Clear()
    {
        _builder.Clear();
        return this;
    }

    public CodeBuilder Insert(int index, string value)
    {
        _builder.Insert(index, value);
        return this;
    }

    public CodeBuilder Remove(int startIndex, int length)
    {
        _builder.Remove(startIndex, length);
        return this;
    }

    public CodeBuilder Replace(string oldValue, string newValue)
    {
        _builder.Replace(oldValue, newValue);
        return this;
    }

    public override string ToString() => _builder.ToString();

    #region Append
    public CodeBuilder Append(string value)
    {
        ReadOnlySpan<char> appendString = value.AsSpan();
        int index;
        if ((index = value.IndexOf('\n')) != -1)
        {
            ReadOnlySpan<char> start = value.AsSpan(0, index + 1);
            ReadOnlySpan<char> end = value.AsSpan(index + 1);

            _builder.Append(start);
            _builder.Append(' ', IndentationLevel * TabsPerIndent);

            appendString = end;
        }

        _builder.Append(appendString);
        return this;
    }

    public CodeBuilder Append(char value)
    {
        _builder.Append(value);
        if (value == '\n')
            _builder.Append(' ', IndentationLevel * TabsPerIndent);
        return this;
    }

    public CodeBuilder Append(bool value)
    {
        _builder.Append(value);
        return this;
    }

    public CodeBuilder Append(byte value)
    {
        _builder.Append(value);
        return this;
    }

    public CodeBuilder Append(decimal value)
    {
        _builder.Append(value);
        return this;
    }

    public CodeBuilder Append(double value)
    {
        _builder.Append(value);
        return this;
    }

    public CodeBuilder Append(float value)
    {
        _builder.Append(value);
        return this;
    }

    public CodeBuilder Append(int value)
    {
        _builder.Append(value);
        return this;
    }

    public CodeBuilder Append(long value)
    {
        _builder.Append(value);
        return this;
    }

    public CodeBuilder Append(object value)
    {
        _builder.Append(value);
        return this;
    }

    public CodeBuilder Append(ReadOnlyMemory<char> value)
    {
        Append(value.Span);
        return this;
    }

    public unsafe CodeBuilder Append(ReadOnlySpan<char> value)
    {
        fixed (char* ptr = value)
        {
            _builder.Append(ptr, value.Length);
        }
        return this;
    }

    public CodeBuilder Append(sbyte value)
    {
        _builder.Append(value);
        return this;
    }

    public CodeBuilder Append(short value)
    {
        _builder.Append(value);
        return this;
    }

    public CodeBuilder Append(StringBuilder value)
    {
        _builder.Append(value);
        return this;
    }

    public CodeBuilder Append(uint value)
    {
        _builder.Append(value);
        return this;
    }

    public CodeBuilder Append(ulong value)
    {
        _builder.Append(value);
        return this;
    }

    public CodeBuilder Append(ushort value)
    {
        _builder.Append(value);
        return this;
    }

    public CodeBuilder Append(char value, int repeatCount)
    {
        _builder.Append(value, repeatCount);
        return this;
    }
    #endregion
}
