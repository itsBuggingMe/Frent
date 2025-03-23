using SysVec2 = System.Numerics.Vector2;
using ImGuiNET;
using Frent.Sample.Asteroids.Editor;
using Frent.Components;
using Frent.Core;
using Frent.Sample.Asteroids.Editor.UI;
using System.Collections.Frozen;
using FrentSandbox;

namespace Frent.Sample.Asteroids;

partial class AsteroidsGame
{
    private Entity _activeEntity;
    private Notification _notification;

    internal record struct Notification(string Text, float Decay, SysVec2 Location);

    private readonly FrozenDictionary<Type, IFieldModifer> FieldModifierTable = typeof(GameRoot)
        .Assembly
        .GetTypes()
        .Where(t => t.IsAssignableTo(typeof(IFieldModifer)) && !t.IsAbstract && !t.IsInterface)
        .Select(t => (IFieldModifer)Activator.CreateInstance(t)!)
        .ToFrozenDictionary(k => k.FieldType);

    private CommandBuffer _deferredActions;

    private void ImGuiLayout()
    {
        ImGui.SetNextWindowSize(new SysVec2(300, 600), ImGuiCond.FirstUseEver);
        if (ImGui.Begin("Component Menu"))
        {
            if (ImGui.Button("Create Entity"))
            {
                _activeEntity = _world.Create();
            }

            foreach (var component in ComponentMeta.Components)
            {
                if (ImGui.CollapsingHeader(component.Name))
                {
                    if (component.Description is not null)
                    {
                        ImGui.Text(component.Description);
                    }

                    if (component.Arguments.Length > 0)
                        ImGui.SeparatorText("Depends on");

                    foreach (var item in component.Arguments)
                        ImGui.Text(item.Type.Name);

                    if (component.ComponentFields.Length > 0)
                    {
                        ImGui.SeparatorText("Data");

                        foreach (var item in component.ComponentFields)
                            ImGui.Text(item.Name);
                    }

                    ImGui.Spacing();
                    ImGui.Spacing();

                    if (_activeEntity.IsAlive && ImGui.Button($"Add {component.Name}"))
                    {
                        _activeEntity.Add(component.ID, Activator.CreateInstance(component.ID.Type)!);
                    }

                    ImGui.Spacing();
                    ImGui.Spacing();
                }
            }

            ImGui.End();
        }

        ImGui.SetNextWindowSize(new SysVec2(300, 600), ImGuiCond.FirstUseEver);
        if(ImGui.Begin("Entity Manager"))
        {
            foreach(var (name, entity) in AllEntitiesForward)
            {
                string loc = name;
                ImGui.PushID(loc);
                if(ImGui.Button(name))
                {
                    ImGui.SetClipboardText(name);
                    _notification = new Notification("Copied to clipboard!", 10, InputHelper.MouseLocation.ToVector2().ToNumerics());
                }

                ImGui.SameLine();
                if (ImGui.SmallButton("Delete"))
                {
                    _deferredActions.DeleteEntity(entity);
                }

                ImGui.SameLine();
                if (_activeEntity == entity)
                {
                    ImGui.Text("Active");
                }
                else if(ImGui.SmallButton("Select"))
                {
                    _activeEntity = entity;
                }

                ImGui.PopID();
            }

            ImGui.End();
        }

        if (_activeEntity.IsAlive)
        {
            ImGui.SetNextWindowSize(new(400, 600), ImGuiCond.Once);
            ImGui.Begin("Entity");

            ImGui.SeparatorText("Current Components");

            foreach (ComponentID id in _activeEntity.ComponentTypes)
            {
                if (ImGui.CollapsingHeader(id.Type.Name))
                {
                    ComponentMeta metadata = ComponentMeta.ComponentMetaTable[id];
                    foreach (var fieldData in metadata.ComponentFields)
                    {
                        if(FieldModifierTable.TryGetValue(fieldData.Type, out var intf))
                        {
                            ImGui.PushID(fieldData.Name);
                            intf.Entity = _activeEntity;
                            intf.FieldToModify = fieldData;
                            intf.UpdateUI();
                            ImGui.PopID();
                        }
                    }
                }
            }

            ImGui.SeparatorText("Tags");

            foreach (var item in _activeEntity.TagTypes)
                ImGui.Text(item.Type.Name);

            ImGui.End();
        }

        if (_notification.Decay > 0)
        {
            ImGui.SetNextWindowPos(_notification.Location, ImGuiCond.Always, new SysVec2(1, 0));

            if (ImGui.Begin("Notification", ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoSavedSettings | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoTitleBar))
            {
                ImGui.Text(_notification.Text);
                ImGui.End();
            }

            _notification.Decay--;
        }

        _deferredActions.Playback();
    }
}
