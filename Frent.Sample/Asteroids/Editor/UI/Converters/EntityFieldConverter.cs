using ImGuiNET;
using Microsoft.Xna.Framework;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace Frent.Sample.Asteroids.Editor.UI.Converters;

internal class EntityFieldConverter : FieldModifierBase<Entity>
{
    protected override Entity UpdateValue(ComponentField field)
    {
        ref string str = ref CollectionsMarshal.GetValueRefOrNullRef(AsteroidsGame.Instance.AllEntitiesBackwards, _current);
        string finalName;

        if (Unsafe.IsNullRef(ref str))
        {
            finalName = string.Empty;
            _current = Entity.Null;
        }
        else
        {
            finalName = str;
        }

        ImGui.Text(field.Name);
        ImGui.InputText("Entity", ref finalName, 16);

        if(finalName == string.Empty)
        {
            _current = Entity.Null;
            return Entity.Null;
        }

        return AsteroidsGame.Instance.AllEntitiesForward.TryGetValue(finalName, out var entity) ? entity : _current;
    }
}