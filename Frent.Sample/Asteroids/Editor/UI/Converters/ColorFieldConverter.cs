using Frent.Sample.Asteroids.Editor;
using Frent.Sample.Asteroids.Editor.UI;
using ImGuiNET;
using Microsoft.Xna.Framework;

namespace Frent.Sample.Asteroids.Editor.UI.Converters;

internal class ColorFieldConverter : FieldModifierBase<Color>
{
    protected override Color UpdateValue(ComponentField field)
    {
        System.Numerics.Vector4 f = _current.ToVector4().ToNumerics();
        ImGui.ColorPicker4(field.Name, ref f);
        return new Color(f);
    }
}