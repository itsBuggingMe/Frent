﻿using Frent.Buffers;
using Frent.Collections;
using Frent.Core;
using Frent.Core.Events;

namespace Frent.Updating;

internal interface IComponentRunner
{
    internal void Run(World world, Archetype b);
    internal void MultithreadedRun(CountdownEvent countdown, World world, Archetype b);
    internal void Delete(DeleteComponentData deleteComponentData);
    internal void Trim(int chunkIndex);
    internal void ResizeBuffer(int size);
    internal void PullComponentFromAndClear(IComponentRunner otherRunner, int me, int other, int otherRemove);
    internal void PullComponentFrom(IDTable storage, int me, int other);
    internal void InvokeGenericActionWith(GenericEvent? action, Entity entity, int index);
    internal void InvokeGenericActionWith(IGenericAction action, int index);
    internal ComponentHandle Store(int index);
    internal void SetAt(object component, int index);
    internal object GetAt(int index);
    internal ComponentID ComponentID { get; }
}

internal interface IComponentRunner<T> : IComponentRunner
{
    internal Span<T> AsSpan();
}

internal record struct DeleteComponentData(int ToIndex, int FromIndex);