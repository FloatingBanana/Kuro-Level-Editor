using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using ImGuiNET;
using SceneEditor.EntitySystem;
using SceneEditor.Resources;

using SNVector2 = System.Numerics.Vector2;

namespace SceneEditor {
    class ResourceWindow : EditorWindow {
        private Type selectedType = typeof(ModelResource);
        public HashSet<Resource> selectedResource = new();

        public ResourceWindow() {
            
        }
        
        public override void Render(GameTime gameTime, uint dockId) {
            if (ImGui.Begin("Resources", ref isOpen)) {
                _renderTypeSelector(gameTime);
                ImGui.SameLine();
                _renderResourceSelector(gameTime);
            }
        }



        private void _renderTypeSelector(GameTime gameTime) {
            if (ImGui.BeginChild("Resource types", new SNVector2(250, GUI_FILL), true)) {
                //test
                Type[] types = new Type[] {
                    typeof(ModelResource),
                    typeof(MeshResource)
                };

                foreach (var type in types) {
                    if (ImGui.Selectable(type.Name.Stylize(), selectedType == type)) {
                        selectedType = type;
                    }
                }

                ImGui.EndChild();
            }
        }
    
        private const float spacing = 15;
        private SNVector2 thumbSize = new(50, 90);
        private void _renderResourceSelector(GameTime gameTime) {
            if (ImGui.BeginChild("Resources", new SNVector2(GUI_FILL, GUI_FILL), true)) {
                // Clear selection
                if (ImGui.IsMouseClicked(ImGuiMouseButton.Left) && ImGui.IsWindowHovered() && !Game.keyboardState.IsControlDown()) {
                    selectedResource.Clear();
                }
                
                int columns = (int)MathF.Max(ImGui.GetContentRegionAvail().X / (thumbSize.X + spacing), 1);

                if (ImGui.BeginTable("grid", columns)) {
                    foreach (var resource in ResourceManager.resources.Values) {
                        if (resource.GetType() == selectedType) {
                            ImGui.TableNextColumn();

                            if (ImGuiUtils.Thumbnail(resource.Name, resource.ImGuiThumbPointer, thumbSize, selectedResource.Contains(resource))) {
                                if (!selectedResource.Add(resource)) {
                                    selectedResource.Remove(resource);
                                }
                            }
                        }
                    }
                    ImGui.EndTable();
                }

                ImGui.EndChild();
            }
        }
    }
}