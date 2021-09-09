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
    class ComponentsWindow : EditorWindow {

        public ComponentsWindow() {
            
        }
        
        public override void Render(GameTime gameTime, uint dockId) {
            if (ImGui.Begin("Components", ref isOpen)) {

                if (EntityManager.selected != null) {
                    var selected = EntityManager.selected;

                    foreach (var comp in selected.components) {
                        string CompName = comp.GetType().Name.Stylize();

                        ImGui.PushID(comp.GetHashCode());

                        ImGui.Checkbox("##enable", ref comp.enabled);
                        ImGui.SameLine();

                        if (ImGui.TreeNodeEx(CompName, ImGuiTreeNodeFlags.SpanFullWidth)) {
                            if (ImGui.BeginTable("ComponentTable", 2)) {

                                foreach (var field in comp.Fields) {
                                    ImGui.TableNextRow();
                                    ImGui.TableNextColumn();
                                    ImGui.Text(field.name);

                                    ImGui.TableNextColumn();
                                    _renderValueEditor(field);
                                }

                                ImGui.EndTable();
                            }

                            ImGui.TreePop();
                        }
                        ImGui.PopID();
                        ImGui.Separator();
                    }

                    ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
                    ImGui.Button("Add component");

                    if (ImGui.BeginPopupContextItem("add_item_popup", ImGuiPopupFlags.MouseButtonLeft)) {
                        ImGui.MenuItem("Mesh Renderer");
                        ImGui.MenuItem("Image Renderer");
                        ImGui.MenuItem("Box Collider");
                    }
                }
            }
        }

        public override void Update(GameTime gameTime) {
            
        }

        public override void Close() {
            
        }


        private static void _renderValueEditor(ComponentFieldBase field) {
            Type type = field.type;
            bool disabled = field.isReadOnly;

            ImGui.PushID(field.name);
            ImGui.PushItemWidth(-float.Epsilon);

            if (type == typeof(string)) {
                string text = (string)field.Value;

                if (ImGui.InputText("", ref text, 50, disabled ? ImGuiInputTextFlags.ReadOnly : 0)) {
                    field.Value = text;
                }
            }

            else if (type == typeof(int)) {
                int number = (int)field.Value;

                if (ImGui.InputInt("", ref number, 1, 2, disabled ? ImGuiInputTextFlags.ReadOnly : 0)) {
                    field.Value = number;
                }
            }

            else if (type == typeof(float)) {
                float number = (float)field.Value;

                if (ImGui.InputFloat("", ref number, 1, 2, "%f", disabled ? ImGuiInputTextFlags.ReadOnly : 0)) {
                    field.Value = number;
                }
            }

            else if (type == typeof(Vector2)) {
                Vector2 vec2 = (Vector2)field.Value;
                var SNVec2 = new SNVector2(vec2.X, vec2.Y);

                if (ImGui.InputFloat2("", ref SNVec2, "%f", disabled ? ImGuiInputTextFlags.ReadOnly : 0)) {
                    field.Value = new Vector2(SNVec2.X, SNVec2.Y);
                }
            }

            else if (type == typeof(Vector3)) {
                Vector3 vec3 = (Vector3)field.Value;
                var SNVec3 = new System.Numerics.Vector3(vec3.X, vec3.Y, vec3.Z);

                if (ImGui.InputFloat3("", ref SNVec3, "%f", disabled ? ImGuiInputTextFlags.ReadOnly : 0)) {
                    field.Value = new Vector3(SNVec3.X, SNVec3.Y, SNVec3.Z);
                }
            }

            else if (type.IsEnum) {
                string curr = Enum.GetName(type, field.Value);

                if (ImGui.BeginCombo("", curr)) {
                    string[] names = Enum.GetNames(type);
                    
                    foreach (string name in names) {
                        if (ImGui.MenuItem(name)) {
                            field.Value = Enum.Parse(type, name);
                        }
                    }

                    ImGui.EndCombo();
                }
            }

            ImGui.PopItemWidth();

            if (type == typeof(bool)) {
                bool check = (bool)field.Value;

                if (ImGui.Checkbox("", ref check)) {
                    field.Value = check;
                }
            }

            // else if (type.IsArray) {
            //     var arr = (Array)field.Value;
                
            //     if (ImGui.TreeNode("")) {
            //         ImGui.Text("Count: " + arr.Length);
                    
            //         for (int i=0; i < arr.Length; i++) {
            //             object arrValue = arr.GetValue(i);

            //             RenderValueEditor("Element " + i, type, ref arrValue, disabled);

            //             arr.SetValue(arrValue, i);
            //         }
            //     }
            // }

            // else if (type == typeof(List<>)) {
            //     var list = (List<object>)field.Value;
                
            //     if (ImGui.TreeNode("")) {
            //         ImGui.Text("Count: " + list.Capacity);
            //         ImGui.SameLine();

            //         int capacity = list.Capacity;
            //         ImGui.InputInt("", ref capacity);
            //         list.Capacity = capacity;
                    
            //         for (int i=0; i < capacity; i++) {
            //             object val = list[i];
            //             RenderValueEditor("Element " + i, type, ref val, disabled);
            //             list[i] = val;
            //         }
            //     }
            // }

            else {
                // if (ImGui.TreeNode(name)) {
                //     RenderMembers(type, value);

                //     ImGui.TreePop();
                // }

                // if (value == null) {
                //     ImGui.Button($"Empty ({type.Name})");
                // }
                // else {
                //     ImGui.Button(value.ToString());
                // }

                // if (ImGui.BeginDragDropSource()) {
                //     if (ImGui.SetDragDropPayload("DND_VALUE_DRAG", new IntPtr(2), sizeof(int)))
                //         dragging = value;

                //     ImGui.Text(value.ToString());
                //     ImGui.EndDragDropSource();
                // }
                
                // if (ImGui.BeginDragDropTarget()) {
                //     var payload = ImGui.AcceptDragDropPayload("DND_VALUE_DRAG");

                //     if (payload.DataSize == sizeof(int)) {
                //         value = dragging;
                //         dragging = null;
                //     }
                // }
            }

            ImGui.PopID();
        }

    }
}