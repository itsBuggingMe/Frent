using ImGuiNET;

namespace Frent.Sample.Asteroids.Editor.UI.Converters;

internal class IntFieldConverter : FieldModifierBase<int>
{
    protected override int UpdateValue(ComponentField field)
    {
        int f = _current;
        ImGui.InputInt(field.Name, ref f);
        return f;
    }
}