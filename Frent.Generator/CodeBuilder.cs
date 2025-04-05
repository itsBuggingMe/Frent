using System;
using System.Collections;
using System.Collections.Immutable;
using System.Text;
using System.Threading;

namespace Frent.Generator;

internal class CodeBuilder
{
    internal delegate void CodeBuilderDelegate<T>(in T model, CodeBuilder codeBuilder, CancellationToken ct);

    [ThreadStatic]
    private static CodeBuilder? _shared;
    public static CodeBuilder ThreadShared
    {
        get
        {
            _shared ??= new();
            _shared.Indents = 0;
            _shared.Clear();
            return _shared;
        }
    }

    public const int TabsPerIndent = 4;
    private StringBuilder _sb = new();

    public int Indents { get; private set; }

    public CodeBuilder Append<T>(T value)
    {
        _sb.Append(value);
        return this;
    }

    public CodeBuilder Append(ReadOnlySpan<char> value)
    {
        _sb.EnsureCapacity(value.Length + value.Length);
        foreach (char c in value)
        {
            _sb.Append(c);
        }
        return this;
    }

    public CodeBuilder Append(string value, int start, int count)
    {
        _sb.Append(value, start, count);
        return this;
    }

    public CodeBuilder AppendLine<T>(T value)
    {
        _sb.Append(value);
        _sb.AppendLine();
        _sb.Append(' ', TabsPerIndent * Indents);
        return this;
    }

    public CodeBuilder Foreach<T>(ReadOnlySpan<T> items, CancellationToken ct, CodeBuilderDelegate<T> onEach)
    {
        foreach(ref readonly var i in items)
        {
            onEach(in i, this, ct);
        }
        return this;
    }

    public CodeBuilder If(bool condition, Action<CodeBuilder> action)
    {
        if(condition)
        {
            action(this);
        }
        return this;
    }

    public CodeBuilder If<T>(bool condition, T uniform, Action<T, CodeBuilder> action)
    {
        if (condition)
        {
            action(uniform, this);
        }
        return this;
    }

    public CodeBuilder Execute<T>(in T uniform, CancellationToken ct, CodeBuilderDelegate<T> action)
    {
        action(in uniform, this, ct);
        return this;
    }

    public CodeBuilder AppendLine()
    {
        _sb.AppendLine();
        _sb.Append(' ', TabsPerIndent * Indents);
        return this;
    }

    public CodeBuilder Indent()
    {
        Indents++;
        return this;
    }

    public CodeBuilder Scope() => Indent().AppendLine("{");
    public CodeBuilder Unscope() => Outdent().AppendLine("}");


    public CodeBuilder Outdent()
    {
        Indents--;
        if (Indents < 0)
            throw new InvalidOperationException("Indentation level must be positive!");
        if (_sb[_sb.Length - 1] == ' '
            && _sb[_sb.Length - 2] == ' '
            && _sb[_sb.Length - 3] == ' '
            && _sb[_sb.Length - 4] == ' ')
        {
            _sb.Remove(_sb.Length - 4, 4);
        }
        return this;
    }

    public CodeBuilder AppendWithDot(string str)
    {
        if(string.IsNullOrEmpty(str))
            return this;
        _sb.Append(str);
        _sb.Append('.');
        return this;
    }

    public CodeBuilder Clear()
    {
        _sb.Clear();
        return this;
    }

    public override string ToString() => _sb.ToString();
}
