using Frent.Collections;

namespace Frent.Core.Events;

internal class EventRecord
{
    internal TagEvent Tag = new TagEvent();
    internal TagEvent Detach = new TagEvent();
    internal ComponentEvent Add = new ComponentEvent();
    internal ComponentEvent Remove = new ComponentEvent();
    internal FrugalStack<Action<Entity>> Delete = new FrugalStack<Action<Entity>>();
    internal LinkEvent IncomingLinked = new LinkEvent();
    internal LinkEvent OutgoingLinked = new LinkEvent();
    internal LinkEvent IncomingUnlinked = new LinkEvent();
    internal LinkEvent OutgoingUnlinked = new LinkEvent();
}