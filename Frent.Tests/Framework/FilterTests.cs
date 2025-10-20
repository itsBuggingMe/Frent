using Frent.Components;
using Frent.Core;
using Frent.Tests.Helpers;
using Frent.Updating;
using static NUnit.Framework.Assert;

namespace Frent.Tests.Framework;

internal partial class FilterTests
{
    [Test]
    public void Archetypical_IncludesAndExcludes_Work()
    {
        using World w = new();
        int count = 0;
        Action tick = () => count++;

        var e1 = w.Create<FilterComponent, Struct1, Struct2>(new(tick), default, default);
        e1.Tag<Tag1>();

        var e2 = w.Create<FilterComponent, Struct1>(new(tick), default);
        e2.Tag<Tag1>();

        var e3 = w.Create<FilterComponent, Struct1, Struct2, Struct3>(new(tick), default, default, default);
        e3.Tag<Tag1>();

        w.Update<FilterAttribute1>();

        That(count, Is.EqualTo(1));
    }

    [Test]
    public void Archetypical_ExcludesTag_PreventsUpdate()
    {
        using World w = new();
        int count = 0;
        Action tick = () => count++;

        var e = w.Create<FilterComponent, Struct1, Struct2>(new(tick), default, default);
        e.Tag<Tag1, Tag3>();

        w.Update<FilterAttribute1>();

        That(count, Is.Zero);
    }

    [Test]
    public void Sparse_IncludesAndExcludes_Work()
    {
        using World w = new();
        int count = 0;
        Action tick = () => count++;

        var e1 = w.Create<FilterComponentSparse, Struct1, Struct2>(new(tick), default, default);
        e1.Tag<Tag1>();

        var e2 = w.Create<FilterComponentSparse, Struct1>(new(tick), default);
        e2.Tag<Tag1>();

        var e3 = w.Create<FilterComponentSparse, Struct1, Struct2, Struct3>(new(tick), default, default, default);
        e3.Tag<Tag1>();

        w.Update<FilterAttribute1>();

        That(count, Is.EqualTo(1));
    }

    [Test]
    public void Sparse_ExcludesTag_PreventsUpdate()
    {
        using World w = new();
        int count = 0;
        Action tick = () => count++;

        var e = w.Create<FilterComponentSparse, Struct1, Struct2>(new(tick), default, default);
        e.Tag<Tag1, Tag3>();

        w.Update<FilterAttribute1>();

        That(count, Is.Zero);
    }

    [Test]
    public void Archetypical_UpdateComponent_RespectsFilters()
    {
        using World w = new();
        int count = 0;
        Action tick = () => count++;

        var e1 = w.Create<FilterComponent, Struct1, Struct2>(new(tick), default, default);
        e1.Tag<Tag1>();

        var e2 = w.Create<FilterComponent, Struct1, Struct2>(new(tick), default, default);
        e2.Tag<Tag1, Tag3>();

        w.UpdateComponent(Component<FilterComponent>.ID);

        That(count, Is.EqualTo(2));
    }

    [Test]
    public void Sparse_UpdateComponent_RespectsFilters()
    {
        using World w = new();
        int count = 0;
        Action tick = () => count++;

        var e1 = w.Create<FilterComponentSparse, Struct1, Struct2>(new(tick), default, default);
        e1.Tag<Tag1>();

        var e2 = w.Create<FilterComponentSparse, Struct1, Struct2>(new(tick), default, default);
        e2.Tag<Tag1, Tag3>();

        w.UpdateComponent(Component<FilterComponentSparse>.ID);

        That(count, Is.EqualTo(2));
    }
    struct Tag1;
    struct Tag2;
    struct Tag3;

    partial struct FilterComponent(Action onUpdate)
        : IComponent, IComponent<Struct1>, IInitable
    {
        private Entity _self;

        public void Init(Entity self) => _self = self;

        [IncludesComponents(typeof(Struct1), typeof(Struct2))]
        [ExcludesComponents(typeof(Struct3))]
        [IncludesTags(typeof(Tag1))]
        [ExcludesTags(typeof(Tag3))]
        [FilterAttribute1]
        public void Update()
        {
            onUpdate();
        }

        [ExcludesTags(typeof(Tag3))]
        [FilterAttribute2]
        public void Update(ref Struct1 a)
        {
            onUpdate();
        }
    }

    partial struct FilterComponentSparse(Action onUpdate)
        : IComponent, IComponent<Struct1>, IInitable, ISparseComponent
    {
        private Entity _self;

        public void Init(Entity self) => _self = self;

        [IncludesComponents(typeof(Struct1), typeof(Struct2))]
        [ExcludesComponents(typeof(Struct3))]
        [IncludesTags(typeof(Tag1))]
        [ExcludesTags(typeof(Tag3))]
        [FilterAttribute1]
        public void Update()
        {
            onUpdate();
        }

        [ExcludesTags(typeof(Tag3))]
        [FilterAttribute2]
        public void Update(ref Struct1 a)
        {
            onUpdate();
        }
    }
}
