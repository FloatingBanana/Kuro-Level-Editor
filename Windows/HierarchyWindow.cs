using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using ImGuiNET;
using ImGuizmoNET;
using SceneEditor.EntitySystem;

using SNVector2 = System.Numerics.Vector2;

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

        public override void Update(GameTime gameTime) {
            
        }

        public override void Close() {
            
        }

    }
}