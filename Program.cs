using System;
using System.Numerics;

using Kuro.Renderer;
using Kuro.Renderer.Imgui;
using Silk.NET.Input;
using ImGuiNET;
using System.Drawing;

namespace Kuro.LevelEditor {
    public static class Program {
        static ImGuiController imguiController;

        static Model model;

        static TCamera cam;

        static void Main() {
            WindowManager.Load += Load;
            WindowManager.Update += Update;
            WindowManager.Render += Render;
            WindowManager.Closing += Quit;
            WindowManager.KeyDown += KeyDown;
            WindowManager.MouseMove += MouseMove;

            WindowManager.InitializeWindow();
        }

        static void Load() {
            imguiController = new ImGuiController();

            model = new Model("Content/room.fbx");

            Vector2 wsize = WindowManager.DisplaySize;
            cam = new TCamera(new Vector3(0f, 0f, 0f), new Vector3(0f, 0f, 0f), Vector3.UnitY, wsize.X / wsize.Y);
        }

        static void Update(double delta) {
            imguiController.Update((float)delta);

            var keyboard = WindowManager.Keyboard;
            float speed = 50f * (float)delta;

            if (keyboard.IsKeyPressed(Key.W))
                cam.Position += cam.Front * speed;

            if (keyboard.IsKeyPressed(Key.S))
                cam.Position -= cam.Front * speed;

            if (keyboard.IsKeyPressed(Key.A))
                cam.Position -= Vector3.Normalize(Vector3.Cross(cam.Front, cam.Up)) * speed;

            if (keyboard.IsKeyPressed(Key.D))
                cam.Position += Vector3.Normalize(Vector3.Cross(cam.Front, cam.Up)) * speed;
        }

        static unsafe void Render(double delta) {
            GraphicsRenderer.Scissor = new Rectangle(0, 0, (int)WindowManager.DisplaySize.X, (int)WindowManager.DisplaySize.Y);
            GraphicsRenderer.Clear(Color.CornflowerBlue);

            Matrix4x4 view = cam.ViewMatrix;
            Matrix4x4 proj = cam.ProjectionMatrix;

            GraphicsRenderer.PushState();

            GraphicsRenderer.CullDirection = WindingOrder.CounterClockwise;
            GraphicsRenderer.CullFace = FaceCulling.Back;
            GraphicsRenderer.DepthTest = true;

            foreach (var mesh in model.Meshes) {
                Matrix4x4 world = mesh.ModelTransform;

                foreach (var part in mesh.Parts) {
                    var shader = (BasicDiffuseShader)part.Shader;

                    shader.Use();
                    shader.World = world;
                    shader.ViewProjection = view * proj;

                    part.Draw();
                }
            }

            GraphicsRenderer.PopState();
            
            // ImGui.ShowDemoWindow();
            // imguiController.Render();
        }

        static void KeyDown(IKeyboard keyboard, Key key, int _) {
            if (key == Key.Escape) {
                WindowManager.Close();
            }
        }

        static Vector2 _lastMousePos;
        static unsafe void MouseMove(IMouse mouse, Vector2 position) {
            float sensitivity = 0.2f;

            if (_lastMousePos == default)
                _lastMousePos = position;
            else
            {
                float xOffset = (position.X - _lastMousePos.X) * sensitivity;
                float yOffset = (position.Y - _lastMousePos.Y) * sensitivity;

                _lastMousePos = position;

                cam.ModifyDirection(xOffset, yOffset);
            }
        }

        static void Quit() {
            imguiController.Dispose();
            model.Dispose();
        }
    }
}
