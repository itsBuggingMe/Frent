using Frent.Sample.Asteroids.Editor;
using Frent.Sample.Asteroids.Editor.UI;
using ImGuiNET;

namespace Frent.Sample.Asteroids.Editor.UI.Converters;

internal class SingleFieldConverter : FieldModifierBase<float>
{
    protected override float UpdateValue(ComponentField field)
    {
        float f = _current;
        ImGui.InputFloat(field.Name, ref f);
        return f;
    }
}