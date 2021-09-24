using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using ImGuiNET;
using SceneEditor.EntitySystem;

namespace SceneEditor {
    class HierarchyWindow : EditorWindow {
        public HierarchyWindow() {
            
        }
        
        public override void Render(GameTime gameTime, uint dockId) {
            if (ImGui.Begin("Hierarchy", ref isOpen)) {

                if (ImGui.IsWindowHovered() && ImGui.IsMouseClicked(ImGuiMouseButton.Left)) {
                    EntityManager.selected = null;
                }

                foreach (var entity in EntityManager.entities) {
                    ImGuiTreeNodeFlags flags = ImGuiTreeNodeFlags.OpenOnArrow | ImGuiTreeNodeFlags.SpanFullWidth;

                    if (entity == EntityManager.selected) flags |= ImGuiTreeNodeFlags.Selected;

                    if (ImGui.TreeNodeEx(entity.name, flags)) {

                        ImGui.TreePop();
                    }

                    if (ImGui.IsItemClicked()) {
                        EntityManager.selected = entity;
                    }
                }
            }
        }

    }
}