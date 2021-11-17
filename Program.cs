using System;
using System.Numerics;

using Kuro.Renderer;
using Kuro.Renderer.Imgui;
using Silk.NET.Input;
using ImGuiNET;
using System.Drawing;

namespace SceneEditor {
    public static class Program {
        static ImGuiController imguiController;

        static void Main() {
            WindowManager.Load += Load;
            WindowManager.Update += Update;
            WindowManager.Render += Render;
            WindowManager.Closing += Quit;
            WindowManager.KeyDown += KeyDown;

            WindowManager.InitializeWindow();
        }

        static void Load() {
            imguiController = new ImGuiController();
        }

        static void Update(double delta) {
            imguiController.Update((float)delta);
        }

        static unsafe void Render(double delta) {
            GraphicsRenderer.Clear(Color.CornflowerBlue);

            ImGui.ShowDemoWindow();

            if (ImGui.Begin("batata")) {
                ImGui.Button("aaaa");

                ImGui.End();
            }


            imguiController.Render();
        }

        static void KeyDown(IKeyboard keyboard, Key key, int _) {
            if (key == Key.Escape) {
                WindowManager.Close();
            }
        }

        static void Quit() {
            imguiController.Dispose();
        }
    }
}
