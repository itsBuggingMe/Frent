using Frent.Components;
using Frent.Core;
using Frent.Tests.Helpers;
using Frent.Tests.SparseComponents;
using static NUnit.Framework.Assert;

namespace Frent.Tests;

internal class LinkTests
{
    #region Link Kinds

    internal struct ChildOf : ILink;
    internal struct Owns : ILink;
    internal struct Targets : ILink;

    internal struct SingleOut : ISingleOutgoingLink;
    internal struct SingleIn : ISingleIncomingLink;
    internal struct SingleBoth : ISingleIncomingLink, ISingleOutgoingLink;

    #endregion

    #region Helpers

    private static List<int> ValuesOutgoing<TLink>(Entity entity)
    {
        List<int> values = [];
        foreach (var linked in entity.EnumerateOutgoing<TLink, Struct1>())
            values.Add(linked.Item1.Value.Value);
        values.Sort();
        return values;
    }

    private static List<int> ValuesIncoming<TLink>(Entity entity)
    {
        List<int> values = [];
        foreach (var linked in entity.EnumerateIncoming<TLink, Struct1>())
            values.Add(linked.Item1.Value.Value);
        values.Sort();
        return values;
    }

    private static List<Entity> EntitiesOutgoing<TLink>(Entity entity)
    {
        List<Entity> entities = [];
        foreach ((var linked, var component) in entity.EnumerateOutgoingWithEntities<TLink, Struct1>())
            entities.Add(linked);
        return entities;
    }

    private static List<Entity> EntitiesIncoming<TLink>(Entity entity)
    {
        List<Entity> entities = [];
        foreach ((var linked, var component) in entity.EnumerateIncomingWithEntities<TLink, Struct1>())
            entities.Add(linked);
        return entities;
    }

    #endregion

    #region LinkID

    [Test]
    public static void GetLinkID_Unique()
    {
        HashSet<LinkID> linkIDs =
        [
            Link.GetLinkID(typeof(int)),
            Link.GetLinkID(typeof(long)),
            Link.GetLinkID(typeof(double)),
            Link.GetLinkID(typeof(string)),
        ];

        That(linkIDs, Has.Count.EqualTo(4));
    }

    [Test]
    public static void GetLinkID_Same()
    {
#pragma warning disable NUnit2009 // The same value has been provided as both the actual and the expected argument
        That(Link.GetLinkID(typeof(int)), Is.EqualTo(Link.GetLinkID(typeof(int))));
        That(Link.GetLinkID(typeof(ChildOf)), Is.EqualTo(Link.GetLinkID(typeof(ChildOf))));
#pragma warning restore NUnit2009
    }

    [Test]
    public static void GetLinkIDGeneric_MatchesNonGeneric()
    {
        That(Link<ChildOf>.ID, Is.EqualTo(Link.GetLinkID(typeof(ChildOf))));
        That(Link<Owns>.ID, Is.EqualTo(Link.GetLinkID(typeof(Owns))));
    }

    [Test]
    public static void LinkID_Type_RoundTrips()
    {
        That(Link<ChildOf>.ID.Type, Is.EqualTo(typeof(ChildOf)));
        That(Link.GetLinkID(typeof(Owns)).Type, Is.EqualTo(typeof(Owns)));
    }

    #endregion

    #region Link

    [Test]
    public static void Link_Generic_RecordsBothDirections()
    {
        using World world = new();
        Entity parent = world.Create(new Struct1(10));
        Entity child = world.Create(new Struct1(20));

        child.Link<ChildOf>(parent);

        That(ValuesOutgoing<ChildOf>(child), Is.EqualTo(new[] { 10 }));
        That(ValuesIncoming<ChildOf>(parent), Is.EqualTo(new[] { 20 }));

        // the other ends see nothing
        That(ValuesIncoming<ChildOf>(child), Is.Empty);
        That(ValuesOutgoing<ChildOf>(parent), Is.Empty);
    }

    [Test]
    public static void Link_ByLinkID_MatchesGeneric()
    {
        using World world = new();
        Entity parent = world.Create(new Struct1(10));
        Entity child = world.Create(new Struct1(20));

        child.Link(Link<ChildOf>.ID, parent);

        That(ValuesOutgoing<ChildOf>(child), Is.EqualTo(new[] { 10 }));
        That(ValuesIncoming<ChildOf>(parent), Is.EqualTo(new[] { 20 }));
    }

    [Test]
    public static void Link_Duplicate_Throws()
    {
        using World world = new();
        Entity a = world.Create(new Struct1(1));
        Entity b = world.Create(new Struct1(2));

        a.Link<ChildOf>(b);

        Throws<InvalidOperationException>(() => a.Link<ChildOf>(b));
    }

    [Test]
    public static void Link_DeadEntity_Throws()
    {
        using World world = new();
        Entity alive = world.Create(new Struct1(1));
        Entity dead = world.Create(new Struct1(2));
        dead.Delete();

        Throws<InvalidOperationException>(() => alive.Link<ChildOf>(dead));
        Throws<InvalidOperationException>(() => dead.Link<ChildOf>(alive));
    }

    [Test]
    public static void Link_DifferentWorlds_Throws()
    {
        using World world = new();
        using World other = new();
        Entity here = world.Create(new Struct1(1));
        Entity there = other.Create(new Struct1(2));

        Throws<InvalidOperationException>(() => here.Link<ChildOf>(there));
    }

    [Test]
    public static void TryLink_Generic_TrueThenFalse()
    {
        using World world = new();
        Entity a = world.Create(new Struct1(1));
        Entity b = world.Create(new Struct1(2));

        That(a.TryLink<ChildOf>(b), Is.True);
        That(a.TryLink<ChildOf>(b), Is.False);

        That(ValuesOutgoing<ChildOf>(a), Is.EqualTo(new[] { 2 }));
    }

    [Test]
    public static void TryLink_ByLinkID_TrueThenFalse()
    {
        using World world = new();
        Entity a = world.Create(new Struct1(1));
        Entity b = world.Create(new Struct1(2));

        That(a.TryLink(Link<ChildOf>.ID, b), Is.True);
        That(a.TryLink(Link<ChildOf>.ID, b), Is.False);
    }

    [Test]
    public static void TryLink_DeadOrForeignEntity_ReturnsFalse()
    {
        using World world = new();
        using World other = new();
        Entity alive = world.Create(new Struct1(1));
        Entity dead = world.Create(new Struct1(2));
        Entity foreign = other.Create(new Struct1(3));
        dead.Delete();

        That(alive.TryLink<ChildOf>(dead), Is.False);
        That(dead.TryLink<ChildOf>(alive), Is.False);
        That(alive.TryLink<ChildOf>(foreign), Is.False);
    }

    #endregion

    #region Unlink

    [Test]
    public static void Unlink_Generic_RemovesBothDirections()
    {
        using World world = new();
        Entity a = world.Create(new Struct1(1));
        Entity b = world.Create(new Struct1(2));
        a.Link<ChildOf>(b);

        a.Unlink<ChildOf>(b);

        That(ValuesOutgoing<ChildOf>(a), Is.Empty);
        That(ValuesIncoming<ChildOf>(b), Is.Empty);
    }

    [Test]
    public static void Unlink_ByLinkID_RemovesBothDirections()
    {
        using World world = new();
        Entity a = world.Create(new Struct1(1));
        Entity b = world.Create(new Struct1(2));
        a.Link(Link<ChildOf>.ID, b);

        a.Unlink(Link<ChildOf>.ID, b);

        That(ValuesOutgoing<ChildOf>(a), Is.Empty);
        That(ValuesIncoming<ChildOf>(b), Is.Empty);
    }

    [Test]
    public static void Unlink_KeepsUnrelatedLinks()
    {
        using World world = new();
        Entity a = world.Create(new Struct1(1));
        Entity b = world.Create(new Struct1(2));
        Entity c = world.Create(new Struct1(3));
        a.Link<ChildOf>(b);
        a.Link<ChildOf>(c);

        a.Unlink<ChildOf>(b);

        That(ValuesOutgoing<ChildOf>(a), Is.EqualTo(new[] { 3 }));
        That(ValuesIncoming<ChildOf>(b), Is.Empty);
        That(ValuesIncoming<ChildOf>(c), Is.EqualTo(new[] { 1 }));
    }

    [Test]
    public static void Unlink_NeverLinked_Throws()
    {
        using World world = new();
        Entity a = world.Create(new Struct1(1));
        Entity b = world.Create(new Struct1(2));

        Throws<InvalidOperationException>(() => a.Unlink<ChildOf>(b));
    }

    [Test]
    public static void Unlink_WrongDirection_Throws()
    {
        using World world = new();
        Entity a = world.Create(new Struct1(1));
        Entity b = world.Create(new Struct1(2));
        a.Link<ChildOf>(b);

        Throws<InvalidOperationException>(() => b.Unlink<ChildOf>(a));
    }

    [Test]
    public static void Unlink_DeadEntity_Throws()
    {
        using World world = new();
        Entity a = world.Create(new Struct1(1));
        Entity b = world.Create(new Struct1(2));
        a.Link<ChildOf>(b);
        b.Delete();

        Throws<InvalidOperationException>(() => a.Unlink<ChildOf>(b));
    }

    [Test]
    public static void TryUnlink_Generic_TrueThenFalse()
    {
        using World world = new();
        Entity a = world.Create(new Struct1(1));
        Entity b = world.Create(new Struct1(2));
        a.Link<ChildOf>(b);

        That(a.TryUnlink<ChildOf>(b), Is.True);
        That(a.TryUnlink<ChildOf>(b), Is.False);
    }

    [Test]
    public static void TryUnlink_ByLinkID_TrueThenFalse()
    {
        using World world = new();
        Entity a = world.Create(new Struct1(1));
        Entity b = world.Create(new Struct1(2));
        a.Link(Link<ChildOf>.ID, b);

        That(a.TryUnlink(Link<ChildOf>.ID, b), Is.True);
        That(a.TryUnlink(Link<ChildOf>.ID, b), Is.False);
    }

    [Test]
    public static void TryUnlink_DeadOrForeignEntity_ReturnsFalse()
    {
        using World world = new();
        using World other = new();
        Entity alive = world.Create(new Struct1(1));
        Entity dead = world.Create(new Struct1(2));
        Entity foreign = other.Create(new Struct1(3));
        alive.Link<ChildOf>(dead);
        dead.Delete();

        That(alive.TryUnlink<ChildOf>(dead), Is.False);
        That(alive.TryUnlink<ChildOf>(foreign), Is.False);
    }

    [Test]
    public static void Relink_AfterUnlink_Works()
    {
        using World world = new();
        Entity a = world.Create(new Struct1(1));
        Entity b = world.Create(new Struct1(2));

        a.Link<ChildOf>(b);
        a.Unlink<ChildOf>(b);
        a.Link<ChildOf>(b);

        That(ValuesOutgoing<ChildOf>(a), Is.EqualTo(new[] { 2 }));
        That(ValuesIncoming<ChildOf>(b), Is.EqualTo(new[] { 1 }));
    }

    #endregion

    #region Enumeration

    [Test]
    public static void Enumerate_NoLinks_IsEmpty()
    {
        using World world = new();
        Entity lonely = world.Create(new Struct1(1));

        That(ValuesOutgoing<ChildOf>(lonely), Is.Empty);
        That(ValuesIncoming<ChildOf>(lonely), Is.Empty);
        That(EntitiesOutgoing<ChildOf>(lonely), Is.Empty);
        That(EntitiesIncoming<ChildOf>(lonely), Is.Empty);
    }

    [Test]
    public static void Enumerate_UnusedLinkKind_IsEmpty()
    {
        using World world = new();
        Entity a = world.Create(new Struct1(1));
        Entity b = world.Create(new Struct1(2));
        a.Link<ChildOf>(b);

        That(ValuesOutgoing<Owns>(a), Is.Empty);
        That(ValuesIncoming<Owns>(b), Is.Empty);
    }

    [Test]
    public static void Enumerate_ByLinkID_MatchesGeneric()
    {
        using World world = new();
        Entity a = world.Create(new Struct1(1));
        Entity b = world.Create(new Struct1(2));
        a.Link<ChildOf>(b);

        List<int> outgoing = [];
        foreach (var linked in a.EnumerateOutgoing<Struct1>(Link<ChildOf>.ID))
            outgoing.Add(linked.Item1.Value.Value);

        List<int> incoming = [];
        foreach (var linked in b.EnumerateIncoming<Struct1>(Link<ChildOf>.ID))
            incoming.Add(linked.Item1.Value.Value);

        That(outgoing, Is.EqualTo(new[] { 2 }));
        That(incoming, Is.EqualTo(new[] { 1 }));
    }

    [Test]
    public static void EnumerateWithEntities_ByLinkID_MatchesGeneric()
    {
        using World world = new();
        Entity a = world.Create(new Struct1(1));
        Entity b = world.Create(new Struct1(2));
        a.Link<ChildOf>(b);

        List<Entity> outgoing = [];
        foreach ((var entity, var component) in a.EnumerateOutgoingWithEntities<Struct1>(Link<ChildOf>.ID))
            outgoing.Add(entity);

        List<Entity> incoming = [];
        foreach ((var entity, var component) in b.EnumerateIncomingWithEntities<Struct1>(Link<ChildOf>.ID))
            incoming.Add(entity);

        That(outgoing, Is.EqualTo(new[] { b }));
        That(incoming, Is.EqualTo(new[] { a }));
    }

    [Test]
    public static void EnumerateWithEntities_YieldsEntityAndComponent()
    {
        using World world = new();
        Entity parent = world.Create(new Struct1(10));
        Entity childA = world.Create(new Struct1(20));
        Entity childB = world.Create(new Struct1(30));
        childA.Link<ChildOf>(parent);
        childB.Link<ChildOf>(parent);

        Dictionary<Entity, int> found = [];
        foreach ((var entity, var component) in parent.EnumerateIncomingWithEntities<ChildOf, Struct1>())
            found[entity] = component.Value.Value;

        That(found, Has.Count.EqualTo(2));
        That(found[childA], Is.EqualTo(20));
        That(found[childB], Is.EqualTo(30));
        That(EntitiesOutgoing<ChildOf>(childA), Is.EqualTo(new[] { parent }));
    }

    /// <summary>
    /// Two links fit inline; anything past that upgrades to array storage.
    /// </summary>
    [Test]
    public static void Enumerate_ManyLinks_UpgradesStorage()
    {
        using World world = new();
        Entity parent = world.Create(new Struct1(0));
        List<int> expected = [];
        for (int i = 1; i <= 10; i++)
        {
            Entity child = world.Create(new Struct1(i));
            child.Link<ChildOf>(parent);
            expected.Add(i);
        }

        That(ValuesIncoming<ChildOf>(parent), Is.EqualTo(expected));
    }

    [Test]
    public static void Enumerate_AfterRemovingFromLargeStorage_SkipsRemoved()
    {
        using World world = new();
        Entity parent = world.Create(new Struct1(0));
        List<Entity> children = [];
        for (int i = 1; i <= 6; i++)
        {
            Entity child = world.Create(new Struct1(i));
            child.Link<ChildOf>(parent);
            children.Add(child);
        }

        children[0].Unlink<ChildOf>(parent);
        children[3].Unlink<ChildOf>(parent);

        That(ValuesIncoming<ChildOf>(parent), Is.EqualTo(new[] { 2, 3, 5, 6 }));
    }

    [Test]
    public static void Enumerate_SkipsEntitiesMissingTheComponent()
    {
        using World world = new();
        Entity source = world.Create(new Struct3(0));
        Entity withStruct1 = world.Create(new Struct1(5));
        Entity withoutStruct1 = world.Create(new Struct3(1));

        source.Link<ChildOf>(withStruct1);
        source.Link<ChildOf>(withoutStruct1);

        That(ValuesOutgoing<ChildOf>(source), Is.EqualTo(new[] { 5 }));
        That(EntitiesOutgoing<ChildOf>(source), Is.EqualTo(new[] { withStruct1 }));
    }

    [Test]
    public static void Enumerate_DifferentLinkKinds_AreIndependent()
    {
        using World world = new();
        Entity source = world.Create(new Struct1(0));
        Entity child = world.Create(new Struct1(1));
        Entity owned = world.Create(new Struct1(2));

        source.Link<ChildOf>(child);
        source.Link<Owns>(owned);

        That(ValuesOutgoing<ChildOf>(source), Is.EqualTo(new[] { 1 }));
        That(ValuesOutgoing<Owns>(source), Is.EqualTo(new[] { 2 }));
    }

    [Test]
    public static void Enumerate_LinksToTheSameEntityWithBothKinds()
    {
        using World world = new();
        Entity source = world.Create(new Struct1(0));
        Entity target = world.Create(new Struct1(9));

        source.Link<ChildOf>(target);
        source.Link<Owns>(target);

        That(ValuesOutgoing<ChildOf>(source), Is.EqualTo(new[] { 9 }));
        That(ValuesOutgoing<Owns>(source), Is.EqualTo(new[] { 9 }));

        source.Unlink<ChildOf>(target);

        That(ValuesOutgoing<ChildOf>(source), Is.Empty);
        That(ValuesOutgoing<Owns>(source), Is.EqualTo(new[] { 9 }));
    }

    [Test]
    public static void Enumerate_ComponentReferencesAreWritable()
    {
        using World world = new();
        Entity parent = world.Create(new Struct1(0));
        Entity child = world.Create(new Struct1(20));
        child.Link<ChildOf>(parent);

        foreach (var linked in parent.EnumerateIncoming<ChildOf, Struct1>())
            linked.Item1.Value.Value = 99;

        That(child.Get<Struct1>().Value, Is.EqualTo(99));
    }

    [Test]
    public static void Enumerate_Variadic_YieldsEveryComponent()
    {
        using World world = new();
        Entity source = world.Create(new Struct1(0), new Struct2(0));
        Entity bothComponents = world.Create(new Struct1(7), new Struct2(3));
        Entity oneComponent = world.Create(new Struct1(8));

        source.Link<ChildOf>(bothComponents);
        source.Link<ChildOf>(oneComponent);

        List<(int First, int Second)> found = [];
        foreach ((var first, var second) in source.EnumerateOutgoing<ChildOf, Struct1, Struct2>())
            found.Add((first.Value.Value, second.Value.Value));

        // oneComponent is skipped - the enumeration requires every component
        That(found, Is.EqualTo(new[] { (7, 3) }));
    }

    [Test]
    public static void EnumerateWithEntities_Variadic_YieldsEntityAndComponents()
    {
        using World world = new();
        Entity source = world.Create(new Struct1(0), new Struct2(0));
        Entity target = world.Create(new Struct1(7), new Struct2(3));
        source.Link<ChildOf>(target);

        List<(Entity Entity, int First, int Second)> found = [];
        foreach ((var entity, var first, var second) in source.EnumerateOutgoingWithEntities<ChildOf, Struct1, Struct2>())
            found.Add((entity, first.Value.Value, second.Value.Value));

        That(found, Is.EqualTo(new[] { (target, 7, 3) }));
    }

    [Test]
    public static void Enumerate_SparseComponent()
    {
        using World world = new();
        Entity source = world.Create(new SimpleSparseComponent(0));
        Entity withSparse = world.Create(new SimpleSparseComponent(42));
        Entity withoutSparse = world.Create(new Struct3(1));

        source.Link<ChildOf>(withSparse);
        source.Link<ChildOf>(withoutSparse);

        List<int> found = [];
        foreach (var linked in source.EnumerateOutgoing<ChildOf, SimpleSparseComponent>())
            found.Add(linked.Item1.Value.Value);

        That(found, Is.EqualTo(new[] { 42 }));
    }

    [Test]
    public static void Enumerate_DisallowsStructualChanges()
    {
        using World world = new();
        Entity parent = world.Create(new Struct1(0));
        Entity child = world.Create(new Struct1(1));
        child.Link<ChildOf>(parent);

        foreach (var linked in parent.EnumerateIncoming<ChildOf, Struct1>())
        {
            That(world.AllowStructualChanges, Is.False);
        }

        That(world.AllowStructualChanges, Is.True);
    }

    #endregion

    #region HasLink

    [Test]
    public static void HasLink_IsDirectional()
    {
        using World world = new();
        Entity parent = world.Create(new Struct1(1));
        Entity child = world.Create(new Struct1(2));

        child.Link<ChildOf>(parent);

        That(child.HasOutgoingLink<ChildOf>(), Is.True);
        That(child.HasIncomingLink<ChildOf>(), Is.False);

        That(parent.HasIncomingLink<ChildOf>(), Is.True);
        That(parent.HasOutgoingLink<ChildOf>(), Is.False);
    }

    [Test]
    public static void HasLink_ByLinkID_MatchesGeneric()
    {
        using World world = new();
        Entity parent = world.Create(new Struct1(1));
        Entity child = world.Create(new Struct1(2));

        child.Link<ChildOf>(parent);

        That(child.HasOutgoingLink(Link<ChildOf>.ID), Is.True);
        That(parent.HasIncomingLink(Link<ChildOf>.ID), Is.True);
        That(child.HasIncomingLink(Link<ChildOf>.ID), Is.False);
        That(parent.HasOutgoingLink(Link<ChildOf>.ID), Is.False);
    }

    [Test]
    public static void HasLink_OtherLinkKinds_AreNotCounted()
    {
        using World world = new();
        Entity a = world.Create(new Struct1(1));
        Entity b = world.Create(new Struct1(2));

        a.Link<ChildOf>(b);

        That(a.HasOutgoingLink<Owns>(), Is.False);
        That(b.HasIncomingLink<Owns>(), Is.False);
    }

    [Test]
    public static void HasLink_NeverLinked_IsFalse()
    {
        using World world = new();
        Entity lonely = world.Create(new Struct1(1));

        That(lonely.HasOutgoingLink<ChildOf>(), Is.False);
        That(lonely.HasIncomingLink<ChildOf>(), Is.False);
    }

    [Test]
    public static void HasLink_AfterUnlink_IsFalse()
    {
        using World world = new();
        Entity parent = world.Create(new Struct1(1));
        Entity child = world.Create(new Struct1(2));

        child.Link<ChildOf>(parent);
        child.Unlink<ChildOf>(parent);

        That(child.HasOutgoingLink<ChildOf>(), Is.False);
        That(parent.HasIncomingLink<ChildOf>(), Is.False);
    }

    [Test]
    public static void HasLink_AfterUnlinkingOneOfMany_IsStillTrue()
    {
        using World world = new();
        Entity a = world.Create(new Struct1(1));
        Entity b = world.Create(new Struct1(2));
        Entity c = world.Create(new Struct1(3));

        a.Link<ChildOf>(b);
        a.Link<ChildOf>(c);
        a.Unlink<ChildOf>(b);

        That(a.HasOutgoingLink<ChildOf>(), Is.True);
    }

    [Test]
    public static void HasLink_DoesNotFilterOnComponents()
    {
        using World world = new();
        Entity source = world.Create(new Struct3(0));
        // the target has no Struct1, so the Struct1 enumeration skips it
        Entity target = world.Create(new Struct3(1));

        source.Link<ChildOf>(target);

        That(ValuesOutgoing<ChildOf>(source), Is.Empty);
        That(source.HasOutgoingLink<ChildOf>(), Is.True);
    }

    [Test]
    public static void HasLink_Dead_Throws()
    {
        using World world = new();
        Entity dead = world.Create(new Struct1(1));
        dead.Delete();

        Throws<InvalidOperationException>(() => dead.HasOutgoingLink<ChildOf>());
        Throws<InvalidOperationException>(() => dead.HasIncomingLink<ChildOf>());
        Throws<InvalidOperationException>(() => dead.HasOutgoingLink(Link<ChildOf>.ID));
        Throws<InvalidOperationException>(() => dead.HasIncomingLink(Link<ChildOf>.ID));
    }

    [Test]
    public static void TryHasLink_Dead_IsFalse()
    {
        using World world = new();
        Entity parent = world.Create(new Struct1(1));
        Entity child = world.Create(new Struct1(2));
        child.Link<ChildOf>(parent);
        child.Delete();

        That(child.TryHasOutgoingLink<ChildOf>(), Is.False);
        That(child.TryHasIncomingLink<ChildOf>(), Is.False);
        That(child.TryHasOutgoingLink(Link<ChildOf>.ID), Is.False);
        That(child.TryHasIncomingLink(Link<ChildOf>.ID), Is.False);
    }

    [Test]
    public static void TryHasLink_Null_IsFalse()
    {
        That(Entity.Null.TryHasOutgoingLink<ChildOf>(), Is.False);
        That(Entity.Null.TryHasIncomingLink<ChildOf>(), Is.False);
    }

    [Test]
    public static void TryHasLink_Alive_MatchesHasLink()
    {
        using World world = new();
        Entity parent = world.Create(new Struct1(1));
        Entity child = world.Create(new Struct1(2));

        child.Link<ChildOf>(parent);

        That(child.TryHasOutgoingLink<ChildOf>(), Is.EqualTo(child.HasOutgoingLink<ChildOf>()));
        That(child.TryHasIncomingLink<ChildOf>(), Is.EqualTo(child.HasIncomingLink<ChildOf>()));
        That(parent.TryHasIncomingLink<ChildOf>(), Is.EqualTo(parent.HasIncomingLink<ChildOf>()));
        That(parent.TryHasOutgoingLink<ChildOf>(), Is.EqualTo(parent.HasOutgoingLink<ChildOf>()));
    }

    #endregion

    #region Arity Zero Enumeration

    private static List<Entity> AllOutgoing<TLink>(Entity entity)
    {
        List<Entity> entities = [];
        foreach (Entity linked in entity.EnumerateOutgoingWithEntities<TLink>())
            entities.Add(linked);
        return entities;
    }

    private static List<Entity> AllIncoming<TLink>(Entity entity)
    {
        List<Entity> entities = [];
        foreach (Entity linked in entity.EnumerateIncomingWithEntities<TLink>())
            entities.Add(linked);
        return entities;
    }

    [Test]
    public static void EnumerateWithEntities_ArityZero_YieldsBothDirections()
    {
        using World world = new();
        Entity parent = world.Create(new Struct1(1));
        Entity childA = world.Create(new Struct1(2));
        Entity childB = world.Create(new Struct1(3));

        childA.Link<ChildOf>(parent);
        childB.Link<ChildOf>(parent);

        That(AllOutgoing<ChildOf>(childA), Is.EqualTo(new[] { parent }));
        That(AllIncoming<ChildOf>(parent), Is.EquivalentTo(new[] { childA, childB }));
        That(AllOutgoing<ChildOf>(parent), Is.Empty);
        That(AllIncoming<ChildOf>(childA), Is.Empty);
    }

    [Test]
    public static void EnumerateWithEntities_ArityZero_DoesNotSkipEntitiesMissingComponents()
    {
        using World world = new();
        Entity source = world.Create(new Struct3(0));
        Entity withStruct1 = world.Create(new Struct1(5));
        Entity withoutStruct1 = world.Create(new Struct3(1));

        source.Link<ChildOf>(withStruct1);
        source.Link<ChildOf>(withoutStruct1);

        // the Struct1 overload filters, the arity zero overload does not
        That(EntitiesOutgoing<ChildOf>(source), Is.EqualTo(new[] { withStruct1 }));
        That(AllOutgoing<ChildOf>(source), Is.EquivalentTo(new[] { withStruct1, withoutStruct1 }));
    }

    [Test]
    public static void EnumerateWithEntities_ArityZero_NeverLinked_IsEmpty()
    {
        using World world = new();
        Entity lonely = world.Create(new Struct1(1));

        That(AllOutgoing<ChildOf>(lonely), Is.Empty);
        That(AllIncoming<ChildOf>(lonely), Is.Empty);
    }

    [Test]
    public static void EnumerateWithEntities_ArityZero_ByLinkID_MatchesGeneric()
    {
        using World world = new();
        Entity parent = world.Create(new Struct1(1));
        Entity child = world.Create(new Struct1(2));

        child.Link<ChildOf>(parent);

        List<Entity> byID = [];
        foreach (Entity linked in child.EnumerateOutgoingWithEntities(Link<ChildOf>.ID))
            byID.Add(linked);

        That(byID, Is.EqualTo(AllOutgoing<ChildOf>(child)));
    }

    [Test]
    public static void EnumerateWithEntities_ArityZero_DisallowsStructuralChanges()
    {
        using World world = new();
        Entity parent = world.Create(new Struct1(1));
        Entity child = world.Create(new Struct1(2));

        child.Link<ChildOf>(parent);

        foreach (Entity linked in parent.EnumerateIncomingWithEntities<ChildOf>())
        {
            That(world.AllowStructualChanges, Is.False);
        }

        That(world.AllowStructualChanges, Is.True);
    }

    [Test]
    public static void EnumerateWithEntities_HigherArity_StillWorks()
    {
        using World world = new();
        Entity source = world.Create(new Struct3(0));
        Entity both = world.Create(new Struct1(7), new Struct2(8));
        Entity onlyOne = world.Create(new Struct1(9));

        source.Link<ChildOf>(both);
        source.Link<ChildOf>(onlyOne);

        List<Entity> entities = [];
        List<int> values = [];
        foreach ((Entity linked, var c1, var c2) in source.EnumerateOutgoingWithEntities<ChildOf, Struct1, Struct2>())
        {
            entities.Add(linked);
            values.Add(c1.Value.Value + c2.Value.Value);
        }

        That(entities, Is.EqualTo(new[] { both }));
        That(values, Is.EqualTo(new[] { 15 }));
    }

    #endregion

    #region World Events

    [Test]
    public static void WorldOutgoingLinked_Invoked()
    {
        using World world = new();
        Entity a = world.Create(new Struct1(1));
        Entity b = world.Create(new Struct1(2));

        List<(Entity, LinkID)> raised = [];
        world.OutgoingLinked += (entity, link) => raised.Add((entity, link));

        a.Link<ChildOf>(b);

        That(raised, Is.EqualTo(new[] { (a, Link<ChildOf>.ID) }));
    }

    [Test]
    public static void WorldIncomingLinked_Invoked()
    {
        using World world = new();
        Entity a = world.Create(new Struct1(1));
        Entity b = world.Create(new Struct1(2));

        List<(Entity, LinkID)> raised = [];
        world.IncomingLinked += (entity, link) => raised.Add((entity, link));

        a.Link<ChildOf>(b);

        That(raised, Is.EqualTo(new[] { (b, Link<ChildOf>.ID) }));
    }

    [Test]
    public static void WorldOutgoingUnlinked_Invoked()
    {
        using World world = new();
        Entity a = world.Create(new Struct1(1));
        Entity b = world.Create(new Struct1(2));
        a.Link<ChildOf>(b);

        List<(Entity, LinkID)> raised = [];
        world.OutgoingUnlinked += (entity, link) => raised.Add((entity, link));

        a.Unlink<ChildOf>(b);

        That(raised, Is.EqualTo(new[] { (a, Link<ChildOf>.ID) }));
    }

    [Test]
    public static void WorldIncomingUnlinked_Invoked()
    {
        using World world = new();
        Entity a = world.Create(new Struct1(1));
        Entity b = world.Create(new Struct1(2));
        a.Link<ChildOf>(b);

        List<(Entity, LinkID)> raised = [];
        world.IncomingUnlinked += (entity, link) => raised.Add((entity, link));

        a.Unlink<ChildOf>(b);

        That(raised, Is.EqualTo(new[] { (b, Link<ChildOf>.ID) }));
    }

    [Test]
    public static void WorldLinkEvents_AfterUnsubscribe_NotInvoked()
    {
        using World world = new();
        Entity a = world.Create(new Struct1(1));
        Entity b = world.Create(new Struct1(2));

        int count = 0;
        void OnLink(Entity entity, LinkID link) => count++;

        world.OutgoingLinked += OnLink;
        world.IncomingLinked += OnLink;
        world.OutgoingUnlinked += OnLink;
        world.IncomingUnlinked += OnLink;

        world.OutgoingLinked -= OnLink;
        world.IncomingLinked -= OnLink;
        world.OutgoingUnlinked -= OnLink;
        world.IncomingUnlinked -= OnLink;

        a.Link<ChildOf>(b);
        a.Unlink<ChildOf>(b);

        That(count, Is.Zero);
    }

    [Test]
    public static void WorldLinkEvents_NotInvokedWhenNothingChanges()
    {
        using World world = new();
        Entity a = world.Create(new Struct1(1));
        Entity b = world.Create(new Struct1(2));
        a.Link<ChildOf>(b);

        int count = 0;
        world.OutgoingLinked += (_, _) => count++;
        world.IncomingLinked += (_, _) => count++;
        world.OutgoingUnlinked += (_, _) => count++;
        world.IncomingUnlinked += (_, _) => count++;

        That(a.TryLink<ChildOf>(b), Is.False);
        That(b.TryUnlink<ChildOf>(a), Is.False);

        That(count, Is.Zero);
    }

    #endregion

    #region Entity Events

    [Test]
    public static void EntityOnOutgoingLinked_Invoked()
    {
        using World world = new();
        Entity a = world.Create(new Struct1(1));
        Entity b = world.Create(new Struct1(2));

        List<(Entity, LinkID)> raised = [];
        a.OnOutgoingLinked += (entity, link) => raised.Add((entity, link));
        b.OnOutgoingLinked += (_, _) => Fail("b is not the source of the link");

        a.Link<ChildOf>(b);

        That(raised, Is.EqualTo(new[] { (a, Link<ChildOf>.ID) }));
    }

    [Test]
    public static void EntityOnIncomingLinked_Invoked()
    {
        using World world = new();
        Entity a = world.Create(new Struct1(1));
        Entity b = world.Create(new Struct1(2));

        List<(Entity, LinkID)> raised = [];
        b.OnIncomingLinked += (entity, link) => raised.Add((entity, link));
        a.OnIncomingLinked += (_, _) => Fail("a is not the target of the link");

        a.Link<ChildOf>(b);

        That(raised, Is.EqualTo(new[] { (b, Link<ChildOf>.ID) }));
    }

    [Test]
    public static void EntityOnOutgoingUnlinked_Invoked()
    {
        using World world = new();
        Entity a = world.Create(new Struct1(1));
        Entity b = world.Create(new Struct1(2));
        a.Link<ChildOf>(b);

        List<(Entity, LinkID)> raised = [];
        a.OnOutgoingUnlinked += (entity, link) => raised.Add((entity, link));
        b.OnOutgoingUnlinked += (_, _) => Fail("b is not the source of the link");

        a.Unlink<ChildOf>(b);

        That(raised, Is.EqualTo(new[] { (a, Link<ChildOf>.ID) }));
    }

    [Test]
    public static void EntityOnIncomingUnlinked_Invoked()
    {
        using World world = new();
        Entity a = world.Create(new Struct1(1));
        Entity b = world.Create(new Struct1(2));
        a.Link<ChildOf>(b);

        List<(Entity, LinkID)> raised = [];
        b.OnIncomingUnlinked += (entity, link) => raised.Add((entity, link));
        a.OnIncomingUnlinked += (_, _) => Fail("a is not the target of the link");

        a.Unlink<ChildOf>(b);

        That(raised, Is.EqualTo(new[] { (b, Link<ChildOf>.ID) }));
    }

    [Test]
    public static void EntityLinkEvents_AfterUnsubscribe_NotInvoked()
    {
        using World world = new();
        Entity a = world.Create(new Struct1(1));
        Entity b = world.Create(new Struct1(2));

        int count = 0;
        void OnLink(Entity entity, LinkID link) => count++;

        a.OnOutgoingLinked += OnLink;
        b.OnIncomingLinked += OnLink;
        a.OnOutgoingUnlinked += OnLink;
        b.OnIncomingUnlinked += OnLink;

        a.OnOutgoingLinked -= OnLink;
        b.OnIncomingLinked -= OnLink;
        a.OnOutgoingUnlinked -= OnLink;
        b.OnIncomingUnlinked -= OnLink;

        a.Link<ChildOf>(b);
        a.Unlink<ChildOf>(b);

        That(count, Is.Zero);
    }

    [Test]
    public static void LinkEvents_CarryTheLinkKind()
    {
        using World world = new();
        Entity a = world.Create(new Struct1(1));
        Entity b = world.Create(new Struct1(2));

        List<LinkID> raised = [];
        a.OnOutgoingLinked += (_, link) => raised.Add(link);

        a.Link<ChildOf>(b);
        a.Link<Owns>(b);

        That(raised, Is.EqualTo(new[] { Link<ChildOf>.ID, Link<Owns>.ID }));
    }

    [Test]
    public static void LinkEvents_WorldAndEntity_BothInvoked()
    {
        using World world = new();
        Entity a = world.Create(new Struct1(1));
        Entity b = world.Create(new Struct1(2));

        int worldCount = 0;
        int entityCount = 0;
        world.OutgoingLinked += (_, _) => worldCount++;
        a.OnOutgoingLinked += (_, _) => entityCount++;

        a.Link<ChildOf>(b);

        That(worldCount, Is.EqualTo(1));
        That(entityCount, Is.EqualTo(1));
    }

    [Test]
    public static void EntityLinkEvents_SelfLink_InvokesBothSides()
    {
        using World world = new();
        Entity entity = world.Create(new Struct1(1));

        List<string> raised = [];
        entity.OnOutgoingLinked += (_, _) => raised.Add("outgoing");
        entity.OnIncomingLinked += (_, _) => raised.Add("incoming");

        entity.Link<ChildOf>(entity);

        That(raised, Is.EqualTo(new[] { "outgoing", "incoming" }));
    }

    #endregion

    #region Single Links

    [Test]
    public static void SingleOutgoing_LinkingNewTarget_ReplacesOldEdge()
    {
        using World world = new();
        Entity a = world.Create(new Struct1(1));
        Entity b = world.Create(new Struct1(2));
        Entity c = world.Create(new Struct1(3));

        a.Link<SingleOut>(b);
        a.Link<SingleOut>(c); // a may only have one outgoing SingleOut, so this replaces b with c

        // a now points only at c
        That(ValuesOutgoing<SingleOut>(a), Is.EqualTo(new[] { 3 }));
        That(EntitiesOutgoing<SingleOut>(a), Is.EqualTo(new[] { c }));

        // the old target's mirroring incoming edge must be gone (regression: it used to leak)
        That(ValuesIncoming<SingleOut>(b), Is.Empty);
        That(EntitiesIncoming<SingleOut>(b), Is.Empty);
        That(b.HasIncomingLink<SingleOut>(), Is.False);

        // the new target owns the incoming edge
        That(ValuesIncoming<SingleOut>(c), Is.EqualTo(new[] { 1 }));
        That(c.HasIncomingLink<SingleOut>(), Is.True);
    }

    [Test]
    public static void SingleOutgoing_ReplacedEdge_LeavesBothSidesConsistent()
    {
        using World world = new();
        Entity a = world.Create(new Struct1(1));
        Entity b = world.Create(new Struct1(2));
        Entity c = world.Create(new Struct1(3));

        a.Link<SingleOut>(b);
        a.Link<SingleOut>(c);

        // the surviving a -> c edge is fully removable from either side, and nothing dangling
        // on b interferes (regression: the stale b mirror pointed back into a's reused slot)
        a.Unlink<SingleOut>(c);

        That(ValuesOutgoing<SingleOut>(a), Is.Empty);
        That(a.HasOutgoingLink<SingleOut>(), Is.False);
        That(ValuesIncoming<SingleOut>(c), Is.Empty);
        That(ValuesIncoming<SingleOut>(b), Is.Empty);

        // b was never a real target after the replacement, so unlinking it must fail
        That(a.TryUnlink<SingleOut>(b), Is.False);
    }

    [Test]
    public static void SingleOutgoing_ReplaceFromLargeStorage_KeepsOtherMirrorsValid()
    {
        using World world = new();
        Entity a = world.Create(new Struct1(1));
        Entity x = world.Create(new Struct1(10));
        Entity y = world.Create(new Struct1(11));
        Entity b = world.Create(new Struct1(2));
        Entity c = world.Create(new Struct1(3));

        // b collects several incoming SingleOut edges -> its incoming entry upgrades to array storage
        a.Link<SingleOut>(b);
        x.Link<SingleOut>(b);
        y.Link<SingleOut>(b);

        // a swaps its single outgoing edge from b to c; this removes a's entry out of b's
        // large incoming storage, which swaps another element into a's slot and must fix up
        // that element's back-pointer using the outgoing array
        a.Link<SingleOut>(c);

        That(ValuesIncoming<SingleOut>(b), Is.EqualTo(new[] { 10, 11 }));
        That(ValuesIncoming<SingleOut>(c), Is.EqualTo(new[] { 1 }));

        // x and y still resolve to b through their (possibly relocated) mirrors
        That(ValuesOutgoing<SingleOut>(x), Is.EqualTo(new[] { 2 }));
        That(ValuesOutgoing<SingleOut>(y), Is.EqualTo(new[] { 2 }));

        // and those mirrors are still consistent enough to unlink cleanly
        x.Unlink<SingleOut>(b);
        y.Unlink<SingleOut>(b);
        That(ValuesIncoming<SingleOut>(b), Is.Empty);
    }

    [Test]
    public static void SingleIncoming_LinkingNewSource_ReplacesOldEdge()
    {
        using World world = new();
        Entity a = world.Create(new Struct1(1));
        Entity b = world.Create(new Struct1(2));
        Entity c = world.Create(new Struct1(3));

        a.Link<SingleIn>(c);
        b.Link<SingleIn>(c); // c may only have one incoming SingleIn, so this replaces a with b

        // c now receives only from b
        That(ValuesIncoming<SingleIn>(c), Is.EqualTo(new[] { 2 }));
        That(EntitiesIncoming<SingleIn>(c), Is.EqualTo(new[] { b }));

        // the old source's mirroring outgoing edge must be gone (regression: it used to leak)
        That(ValuesOutgoing<SingleIn>(a), Is.Empty);
        That(EntitiesOutgoing<SingleIn>(a), Is.Empty);
        That(a.HasOutgoingLink<SingleIn>(), Is.False);

        // the new source owns the outgoing edge
        That(ValuesOutgoing<SingleIn>(b), Is.EqualTo(new[] { 3 }));
        That(b.HasOutgoingLink<SingleIn>(), Is.True);
    }

    [Test]
    public static void SingleIncoming_ReplacedEdge_OldSourceCannotCorruptSurvivor()
    {
        using World world = new();
        Entity a = world.Create(new Struct1(1));
        Entity b = world.Create(new Struct1(2));
        Entity c = world.Create(new Struct1(3));

        a.Link<SingleIn>(c);
        b.Link<SingleIn>(c); // replaces a with b

        // the old source a has no live edge, so trying to unlink it must fail rather than
        // tear down the surviving b -> c edge (regression: a's stale mirror pointed at b's slot)
        That(a.TryUnlink<SingleIn>(c), Is.False);

        // b -> c is still intact
        That(ValuesOutgoing<SingleIn>(b), Is.EqualTo(new[] { 3 }));
        That(ValuesIncoming<SingleIn>(c), Is.EqualTo(new[] { 2 }));
    }

    [Test]
    public static void SingleIncoming_ReplaceFromLargeOutgoing_KeepsOtherMirrorsValid()
    {
        using World world = new();
        Entity a = world.Create(new Struct1(1));
        Entity t1 = world.Create(new Struct1(10));
        Entity c = world.Create(new Struct1(3));
        Entity t2 = world.Create(new Struct1(11));
        Entity b = world.Create(new Struct1(2));

        // a has several outgoing SingleIn edges -> its outgoing entry upgrades to array storage,
        // with c deliberately in the middle so removing it forces a swap
        a.Link<SingleIn>(t1);
        a.Link<SingleIn>(c);
        a.Link<SingleIn>(t2);

        // b takes over c's single incoming slot, removing a's mirror out of a's large outgoing
        b.Link<SingleIn>(c);

        That(ValuesOutgoing<SingleIn>(a), Is.EqualTo(new[] { 10, 11 }));
        That(ValuesIncoming<SingleIn>(c), Is.EqualTo(new[] { 2 }));
        That(ValuesOutgoing<SingleIn>(b), Is.EqualTo(new[] { 3 }));

        // t1 and t2 still see a as their incoming source through the fixed-up mirrors
        That(ValuesIncoming<SingleIn>(t1), Is.EqualTo(new[] { 1 }));
        That(ValuesIncoming<SingleIn>(t2), Is.EqualTo(new[] { 1 }));

        a.Unlink<SingleIn>(t1);
        a.Unlink<SingleIn>(t2);
        That(ValuesOutgoing<SingleIn>(a), Is.Empty);
    }

    [Test]
    public static void SingleBoth_ReplacesOnBothEnds()
    {
        using World world = new();
        Entity a = world.Create(new Struct1(1));
        Entity b = world.Create(new Struct1(2)); // a's original target
        Entity c = world.Create(new Struct1(3)); // the new target
        Entity e = world.Create(new Struct1(5)); // c's original source

        a.Link<SingleBoth>(b);   // a -> b
        e.Link<SingleBoth>(c);   // e -> c

        // a -> c must replace a's single outgoing edge (was b) AND c's single incoming edge (was e)
        a.Link<SingleBoth>(c);

        That(ValuesOutgoing<SingleBoth>(a), Is.EqualTo(new[] { 3 }));
        That(ValuesIncoming<SingleBoth>(c), Is.EqualTo(new[] { 1 }));

        // b's stale incoming and e's stale outgoing are both cleared
        That(ValuesIncoming<SingleBoth>(b), Is.Empty);
        That(ValuesOutgoing<SingleBoth>(e), Is.Empty);
        That(b.HasIncomingLink<SingleBoth>(), Is.False);
        That(e.HasOutgoingLink<SingleBoth>(), Is.False);
    }

    [Test]
    public static void SingleOutgoing_RelinkingSameTarget_Throws()
    {
        using World world = new();
        Entity a = world.Create(new Struct1(1));
        Entity b = world.Create(new Struct1(2));

        a.Link<SingleOut>(b);

        // replacing with the *same* target is still a duplicate, not a silent no-op replace
        Throws<InvalidOperationException>(() => a.Link<SingleOut>(b));
        That(ValuesOutgoing<SingleOut>(a), Is.EqualTo(new[] { 2 }));
        That(ValuesIncoming<SingleOut>(b), Is.EqualTo(new[] { 1 }));
    }

    [Test]
    public static void SingleOutgoing_Replacement_RaisesUnlinkAndLinkWorldEvents()
    {
        using World world = new();
        Entity a = world.Create(new Struct1(1));
        Entity b = world.Create(new Struct1(2));
        Entity c = world.Create(new Struct1(3));

        a.Link<SingleOut>(b);

        List<(Entity, LinkID)> outgoingUnlinked = [];
        List<(Entity, LinkID)> incomingUnlinked = [];
        List<(Entity, LinkID)> outgoingLinked = [];
        List<(Entity, LinkID)> incomingLinked = [];
        world.OutgoingUnlinked += (e, l) => outgoingUnlinked.Add((e, l));
        world.IncomingUnlinked += (e, l) => incomingUnlinked.Add((e, l));
        world.OutgoingLinked += (e, l) => outgoingLinked.Add((e, l));
        world.IncomingLinked += (e, l) => incomingLinked.Add((e, l));

        a.Link<SingleOut>(c); // replaces a -> b with a -> c

        // the displaced a -> b edge raises unlink on both of its ends
        That(outgoingUnlinked, Is.EqualTo(new[] { (a, Link<SingleOut>.ID) }));
        That(incomingUnlinked, Is.EqualTo(new[] { (b, Link<SingleOut>.ID) }));
        // the new a -> c edge raises link on both of its ends
        That(outgoingLinked, Is.EqualTo(new[] { (a, Link<SingleOut>.ID) }));
        That(incomingLinked, Is.EqualTo(new[] { (c, Link<SingleOut>.ID) }));
    }

    [Test]
    public static void SingleOutgoing_Replacement_RaisesEntityUnlinkEvents()
    {
        using World world = new();
        Entity a = world.Create(new Struct1(1));
        Entity b = world.Create(new Struct1(2));
        Entity c = world.Create(new Struct1(3));

        a.Link<SingleOut>(b);

        List<(Entity, LinkID)> aOutgoingUnlinked = [];
        List<(Entity, LinkID)> bIncomingUnlinked = [];
        a.OnOutgoingUnlinked += (e, l) => aOutgoingUnlinked.Add((e, l));
        b.OnIncomingUnlinked += (e, l) => bIncomingUnlinked.Add((e, l));
        // c is the new target; it must not see an unlink
        c.OnIncomingUnlinked += (_, _) => Fail("c is the new target, not a displaced one");

        a.Link<SingleOut>(c);

        That(aOutgoingUnlinked, Is.EqualTo(new[] { (a, Link<SingleOut>.ID) }));
        That(bIncomingUnlinked, Is.EqualTo(new[] { (b, Link<SingleOut>.ID) }));
    }

    [Test]
    public static void SingleIncoming_Replacement_RaisesUnlinkEventsForOldEdge()
    {
        using World world = new();
        Entity a = world.Create(new Struct1(1));
        Entity b = world.Create(new Struct1(2));
        Entity c = world.Create(new Struct1(3));

        a.Link<SingleIn>(c);

        List<(Entity, LinkID)> outgoingUnlinked = [];
        List<(Entity, LinkID)> incomingUnlinked = [];
        world.OutgoingUnlinked += (e, l) => outgoingUnlinked.Add((e, l));
        world.IncomingUnlinked += (e, l) => incomingUnlinked.Add((e, l));

        b.Link<SingleIn>(c); // replaces a -> c with b -> c

        // the displaced a -> c edge raises unlink on both of its ends
        That(outgoingUnlinked, Is.EqualTo(new[] { (a, Link<SingleIn>.ID) }));
        That(incomingUnlinked, Is.EqualTo(new[] { (c, Link<SingleIn>.ID) }));
    }

    [Test]
    public static void SingleBoth_Replacement_RaisesUnlinkEventsForBothDisplacedEdges()
    {
        using World world = new();
        Entity a = world.Create(new Struct1(1));
        Entity b = world.Create(new Struct1(2)); // a's displaced target
        Entity c = world.Create(new Struct1(3)); // the new target
        Entity e = world.Create(new Struct1(5)); // c's displaced source

        a.Link<SingleBoth>(b);
        e.Link<SingleBoth>(c);

        List<(Entity, LinkID)> outgoingUnlinked = [];
        List<(Entity, LinkID)> incomingUnlinked = [];
        world.OutgoingUnlinked += (en, l) => outgoingUnlinked.Add((en, l));
        world.IncomingUnlinked += (en, l) => incomingUnlinked.Add((en, l));

        a.Link<SingleBoth>(c); // displaces a -> b (outgoing side) and e -> c (incoming side)

        That(outgoingUnlinked, Is.EquivalentTo(new[] { (a, Link<SingleBoth>.ID), (e, Link<SingleBoth>.ID) }));
        That(incomingUnlinked, Is.EquivalentTo(new[] { (b, Link<SingleBoth>.ID), (c, Link<SingleBoth>.ID) }));
    }

    [Test]
    public static void SingleLink_FreshLink_RaisesNoUnlinkEvents()
    {
        using World world = new();
        Entity a = world.Create(new Struct1(1));
        Entity b = world.Create(new Struct1(2));

        int unlinkCount = 0;
        world.OutgoingUnlinked += (_, _) => unlinkCount++;
        world.IncomingUnlinked += (_, _) => unlinkCount++;

        a.Link<SingleOut>(b); // fresh link, nothing displaced

        That(unlinkCount, Is.Zero);
    }

    #endregion
}
