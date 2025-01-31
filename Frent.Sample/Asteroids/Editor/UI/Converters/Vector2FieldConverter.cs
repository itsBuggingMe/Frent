using SysVec2 = System.Numerics.Vector2;
using ImGuiNET;
using Microsoft.Xna.Framework;

namespace Frent.Sample.Asteroids.Editor.UI.Converters;

internal class Vector2FieldConverter : FieldModifierBase<Vector2>
{
    protected override Vector2 UpdateValue(ComponentField field)
    {
        SysVec2 vector2 = _current.ToNumerics();
        ImGui.InputFloat2(field.Name, ref vector2);
        return new Vector2(vector2.X, vector2.Y);
    }
}