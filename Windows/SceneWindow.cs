using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MonoGame.Extended.Input;
using SceneEditor.EntitySystem;
using ImGuiNET;
using ImGuizmoNET;

using SNVector2 = System.Numerics.Vector2;

namespace SceneEditor {
	enum ViewManipulationMode {
		None,
		Rotate,
		Translate,
	}

	class SceneWindow : EditorWindow {
		// View manipulation
		private Point previousMousePos;
		private ViewManipulationMode manipulationMode = ViewManipulationMode.None;

		//Gizmos
		public OPERATION gizmoOperation = OPERATION.TRANSLATE;
		public MODE gizmoMode = MODE.LOCAL;


		// Scene camera
		public Matrix viewMatrix = Matrix.Identity;
		public Matrix projectionMatrix = Matrix.Identity;
		private Vector3 cameraPos = new Vector3(0, 0, 15);
		private Quaternion cameraRotation = Quaternion.Identity;
		private Vector3 cameraRotationEuler = new Vector3(0, 0, 0);

		private bool isPerspectiveProjection = true;
		private float viewFov = MathF.PI/4f;

		// Rendering
		private RenderTarget2D renderTarget;
		private IntPtr renderTargetHandle;

		public SceneWindow() {
			projectionMatrix = Matrix.CreatePerspectiveFieldOfView(viewFov, graphicsDevice.Viewport.AspectRatio, 0.01f, 100f);

			renderTarget = new RenderTarget2D(graphicsDevice, graphicsDevice.Viewport.Width, graphicsDevice.Viewport.Height, false, SurfaceFormat.Color, DepthFormat.Depth24);
			renderTargetHandle = game.imguiRenderer.BindTexture(renderTarget);
		}
		
		public override void Render(GameTime gameTime, uint dockId) {
			float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;

			ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, SNVector2.Zero);

			if (ImGui.Begin("Scene", ref isOpen, ImGuiWindowFlags.MenuBar)) {

				_renderToolbar();

				SNVector2 windowPos = ImGui.GetWindowPos();
				SNVector2 avail = ImGui.GetContentRegionAvail();
				SNVector2 origin = ImGui.GetCursorPos();

				ImGui.BeginChild("prevent_drag", avail, false, ImGuiWindowFlags.NoMove);

				_renderSceneView(gameTime);
				ImGui.Image(renderTargetHandle, avail);
				
				ImGuizmo.SetDrawlist();
				ImGuizmo.SetRect(windowPos.X + origin.X, windowPos.Y + origin.Y, avail.X, avail.Y);
				ImGuizmo.SetOrthographic(!isPerspectiveProjection);

				if (EntityManager.selected != null) {
					Utils.MatrixToArray(viewMatrix, out var viewArr);
					Utils.MatrixToArray(projectionMatrix, out var projArr);

					var transform = EntityManager.selected.transform;
					Utils.MatrixToArray(transform.TransformationMatrix, out var transformArr);

					if (ImGuizmo.Manipulate(ref viewArr[0], ref projArr[0], gizmoOperation, gizmoMode, ref transformArr[0])) {
						transform.TransformationMatrix = Utils.CopyArrayToMatrix(transformArr);
					}
				}

				// View manipulation
				if (ImGui.IsWindowHovered()) {
					if (ImGui.IsMouseClicked(ImGuiMouseButton.Middle)){
						manipulationMode = game.keyboardState.IsKeyDown(Keys.LeftShift) ? ViewManipulationMode.Translate
																						: ViewManipulationMode.Rotate;

						ImGui.SetWindowFocus();
					}

					// Zoom
					if (ImGui.IsWindowFocused()) {
						cameraPos += Vector3.Transform(new Vector3(0, 0, game.mouseState.DeltaScrollWheelValue * 0.005f), cameraRotation);

						if (ImGui.IsMouseClicked(ImGuiMouseButton.Right)) {
							EntityManager.selected = null;

							// Select entity
							foreach (var comp in EntityManager.GetComponentsInScene<IHoverable>()) {
								SNVector2 offset = origin + windowPos;
								Vector2 mousePos = new Vector2(game.mouseState.Position.X - offset.X, game.mouseState.Position.Y - offset.Y);
								Vector2 factor = new Vector2(avail.X / game.GraphicsDevice.Viewport.Width, avail.Y / game.GraphicsDevice.Viewport.Height);
								
								if (comp.IsHovered(viewMatrix, projectionMatrix, mousePos / factor)) {
									EntityManager.selected = (comp as Component).entity;
								}
							}
						}
					}
				}


				// Editor camera manipulation
				if (manipulationMode != ViewManipulationMode.None) {
					Point mouseDelta = game.mouseState.Position - previousMousePos;

					switch (manipulationMode) {
					case ViewManipulationMode.Translate:
						cameraPos += Vector3.Transform(new Vector3(-mouseDelta.X * 0.01f, mouseDelta.Y * 0.01f, 0), cameraRotation);
						break;
					
					// TODO: Make a better rotation system
					case ViewManipulationMode.Rotate:
						cameraRotationEuler -= new Vector3(mouseDelta.Y * 0.005f, mouseDelta.X * 0.005f, 0f);

						var newCamRot = Quaternion.CreateFromYawPitchRoll(cameraRotationEuler.Y, cameraRotationEuler.X, cameraRotationEuler.Z);
						cameraRotation = newCamRot;

						// cameraRotation *= Quaternion.CreateFromAxisAngle(Vector3.Left, -mouseDelta.Y * 0.005f) * Quaternion.CreateFromAxisAngle(Vector3.Up, mouseDelta.X * 0.005f);
						break;
					}
				}
				
				previousMousePos = game.mouseState.Position;

				if (ImGui.IsMouseReleased(ImGuiMouseButton.Middle)) {
					manipulationMode = ViewManipulationMode.None;
				}

				// Projection mode switch
				if (ImGui.IsWindowFocused() && game.keyboardState.WasKeyJustDown(Keys.P)) {
					isPerspectiveProjection = !isPerspectiveProjection;
				}


				Matrix newProj = isPerspectiveProjection ? Matrix.CreatePerspectiveFieldOfView(viewFov, avail.X/avail.Y, 0.01f, 10000f)
														 : Matrix.CreateOrthographic(avail.X, avail.Y, 0.01f, 10000f);

				projectionMatrix = Matrix.Lerp(projectionMatrix, newProj, 30f * deltaTime);

				ImGui.EndChild();
				ImGui.End();
			}
		
			ImGui.PopStyleVar();
		}

		public override void Update(GameTime gameTime) {
			float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;
			
			// Camera animatiom
			Matrix newMatrix = Matrix.CreateLookAt(cameraPos, cameraPos + Vector3.Transform(Vector3.Forward, cameraRotation), Vector3.Up);
			viewMatrix = Matrix.Lerp(viewMatrix, newMatrix, 30f * deltaTime);

			if (game.keyboardState.WasKeyJustDown(Keys.T)) gizmoOperation = OPERATION.TRANSLATE;
			if (game.keyboardState.WasKeyJustDown(Keys.R)) gizmoOperation = OPERATION.ROTATE;
			if (game.keyboardState.WasKeyJustDown(Keys.S)) gizmoOperation = OPERATION.SCALE;

			if (game.keyboardState.WasKeyJustDown(Keys.M)) gizmoMode ^= (MODE)1;

			if (EntityManager.selected != null) {
				var selected = EntityManager.selected;

				if (game.keyboardState.WasKeyJustDown(Keys.O)) {
					_moveToEntity(selected);
				}
			}
		}

		private void _renderSceneView(GameTime gameTime) {
			var _defaultRenderTargets = graphicsDevice.GetRenderTargets();
			
			graphicsDevice.SetRenderTarget(renderTarget);
			graphicsDevice.Clear(Color.CornflowerBlue);

			EntityManager.Render(gameTime, viewMatrix, projectionMatrix);

			graphicsDevice.SetRenderTargets(_defaultRenderTargets);
		}

		private void _renderToolbar() {
			ImGui.PopStyleVar();
				
			if (ImGui.BeginMenuBar()) {

				if (ImGui.BeginMenu("View")) {

					if (ImGui.BeginMenu("Projection")) {
						if (ImGui.MenuItem("Perspective", "P", isPerspectiveProjection)) isPerspectiveProjection = true;
						if (ImGui.MenuItem("Orthographic", "P", !isPerspectiveProjection)) isPerspectiveProjection = false;

						ImGui.EndMenu();
					}

					if (ImGui.BeginMenu("Manipulation mode")) {
						if (ImGui.MenuItem("Translation", "T", gizmoOperation == OPERATION.TRANSLATE)) gizmoOperation = OPERATION.TRANSLATE;
						if (ImGui.MenuItem("Rotation", "R", gizmoOperation == OPERATION.ROTATE)) gizmoOperation = OPERATION.ROTATE;
						if (ImGui.MenuItem("Scale", "S", gizmoOperation == OPERATION.SCALE)) gizmoOperation = OPERATION.SCALE;

						ImGui.EndMenu();
					}

					if (ImGui.BeginMenu("Pivot mode")) {
						if (ImGui.MenuItem("World", "M", gizmoMode == MODE.WORLD)) gizmoMode = MODE.WORLD;
						if (ImGui.MenuItem("Local", "M", gizmoMode == MODE.LOCAL)) gizmoMode = MODE.LOCAL;

						ImGui.EndMenu();
					}

					Entity selected = EntityManager.selected;
					if (ImGui.MenuItem("Move to selection", "O") && selected != null) _moveToEntity(selected);

					ImGui.Separator();

					ImGui.SliderAngle("Fov", ref viewFov, 15, 180);

					ImGui.EndMenu();
				}

				ImGui.EndMenuBar();
			}

			ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, SNVector2.Zero);
		}

		// Commands
		private void _moveToEntity(Entity entity) {
			var mesh = entity.GetComponent<MeshRenderer>()?.Mesh;
			float dist = 5f;

			if (mesh != null) {
				float meshRadius = mesh.BoundingSphere.Transform(entity.transform.TransformationMatrix).Radius;
				dist = meshRadius * 1.5f;
			}

			cameraPos = entity.transform.Position + Vector3.Transform(Vector3.Backward, cameraRotation) * dist;
		}

		public override void Close() {
			game.imguiRenderer.UnbindTexture(renderTargetHandle);
			renderTarget.Dispose();
		}
	}
}