using Frent.Updating;
using System.Collections.Specialized;

namespace Frent.Sample.Asteroids;

#pragma warning disable CS9113 // Parameter is unread.
internal class TickAttribute(int order = 0) : UpdateTypeAttribute, IComponentUpdateOrderAttribute;
#pragma warning restore CS9113 // Parameter is unread.
internal class DrawAttribute : UpdateTypeAttribute;
