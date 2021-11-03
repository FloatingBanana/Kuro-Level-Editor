using System.Linq;
using System.Reflection;
using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using ImGuiNET;
using SceneEditor.EntitySystem;
using SceneEditor.Resources;

using SNVector2 = System.Numerics.Vector2;

namespace SceneEditor {
	class ComponentsWindow : EditorWindow {
		public static List<Type> rootCategory = new List<Type>();
		public static Dictionary<string, List<Type>> categories = new Dictionary<string, List<Type>>();

		static ComponentsWindow() {
			UpdateAvailableComponents();
		}

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

					// Adding new components
					ImGui.Button("Add component", new SNVector2(GUI_FILL, 20));

					if (ImGui.BeginPopupContextItem("add_item_popup", ImGuiPopupFlags.MouseButtonLeft)) {
						Type added = null;

						foreach (var category in categories) {
							if (ImGui.BeginMenu(category.Key)) {

								foreach (var compType in category.Value) {
									if (ImGui.MenuItem(compType.Name.Stylize())) {
										added = compType;
									}
								}

								ImGui.EndMenu();
							}
						}

						foreach (var compType in rootCategory) {
							if (ImGui.MenuItem(compType.Name.Stylize())) {
								added = compType;
							}
						}

						if (added != null) {
							selected.AttachComponent(Activator.CreateInstance(added) as Component);
						}
					}
				}
			}
		}


		private void _renderValueEditor(ComponentFieldBase field) {
			Type type = field.type;
			bool disabled = field.isReadOnly;

			ImGui.PushID(field.name);
			ImGui.PushItemWidth(GUI_FILL);

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

			else if (typeof(Resource).IsAssignableFrom(type)) {
				string label = (field.Value == null ? "None" : (field.Value as Resource).Name) + $" ({type.Name})";
				
				ImGui.Button(label, new SNVector2(GUI_FILL, 20));

				if (_renderResourceSelectorPopup(type, field.Value as Resource, out var resource)) {
					field.Value = resource;
				}
			}

			else if (type == typeof(bool)) {
				bool check = (bool)field.Value;

				if (ImGui.Checkbox("", ref check)) {
					field.Value = check;
				}
			}

			else {
				ImGuiUtils.ButtonDisabled(type.Name.Stylize(), new SNVector2(GUI_FILL, 20));
			}

			ImGui.PopItemWidth();
			ImGui.PopID();
		}


		private const float spacing = 10;
		private SNVector2 thumbSize = new SNVector2(40, 70);
		private bool _renderResourceSelectorPopup(Type type, Resource current, out Resource selection) {
			bool selected = false;
			selection = null;

			if (ImGui.BeginPopupContextItem("res_selector", ImGuiPopupFlags.MouseButtonLeft)) {
				ImGuiUtils.TextCentered("Select resource");

				if (ImGui.BeginChild("res_list", new SNVector2(250, 200), true)) {
					int columns = (int)MathF.Max(ImGui.GetContentRegionAvail().X / (thumbSize.X + spacing), 1);

					if (ImGui.BeginTable("res_grid", columns)) {
						// Erase selection
						ImGui.TableNextColumn();
						if (ImGuiUtils.Thumbnail("None", MainGame.Instance.blankTextureHandle, thumbSize, current == null)) {
							selected = true;
							ImGui.CloseCurrentPopup();
                		}

						foreach (var resource in ResourceManager.resources.Values) {
							if (resource.GetType() == type) {
								ImGui.TableNextColumn();

								if (ImGuiUtils.Thumbnail(resource.Name, resource.ImGuiThumbPointer, thumbSize, current == resource)) {
									selection = resource;
									selected = true;
									ImGui.CloseCurrentPopup();
								}
							}
						}

						ImGui.EndTable();
					}

					ImGui.EndChild();
				}

				ImGui.EndPopup();
			}

			return selected;
		}


		// I plan to support custom components by linking external assemblies,
		// this function will go through all of them and search for component classes
		public static void UpdateAvailableComponents() {
			var compTypes = typeof(MainGame).Assembly.GetTypes().Where(type => typeof(Component).IsAssignableFrom(type) && !type.IsAbstract);

			rootCategory.Clear();
			categories.Clear();

			foreach (var type in compTypes) {
				var category = type.GetCustomAttribute<Category>();

				if (category != null) {
					if (!category.hidden) {
						// Ensure that this category exists
						categories.TryAdd(category.name, new List<Type>());

						categories[category.name].Add(type);
					}
				}
				else {
					rootCategory.Add(type);
				}
			}
		}
	}
}