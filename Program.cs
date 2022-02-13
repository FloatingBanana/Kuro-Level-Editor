using System;
using System.Numerics;

using Kuro.Renderer;
using Kuro.Renderer.Imgui;
using Silk.NET.Input;
using ImGuiNET;
using System.Drawing;
// using Silk.NET.OpenGL.Legacy;

namespace Kuro.LevelEditor {
    public static class Program {
        static ImGuiController imguiController;

        static Model model;

        static TCamera cam;

        static RenderTexture rt;

        static float[] quad = {
            0, 0,     -1, -1,
            1, 0,      1, -1,
            0, 1,     -1,  1,
            1, 1,      1,  1,
        };

        static ushort[] indices = {
            0, 1, 2,
            1, 2, 3
        };

        static BufferObject<float> vbo;
        static BufferObject<ushort> ebo;

        static Shader shader2d;

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

            vbo = new BufferObject<float>(quad, Silk.NET.OpenGL.Legacy.BufferTargetARB.ArrayBuffer);
            ebo = new BufferObject<ushort>(indices, Silk.NET.OpenGL.Legacy.BufferTargetARB.ElementArrayBuffer);


            rt = new RenderTexture(WindowManager.DisplaySize);

            shader2d = Shader.CreateFromPath("Renderer/Shaders/2DRendering.vert", "Renderer/Shaders/2DRendering.frag");

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

            rt.BindRendering();

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

            GraphicsRenderer.SetMainRenderTarget();
            GraphicsRenderer.AssertGLError();

            GraphicsRenderer.PopState();

            shader2d.Use();
            rt.Bind(0);
            shader2d.SetUniform("texture", 0);
            GraphicsRenderer.AssertGLError();

            ebo.Bind();
            vbo.Bind();

            uint positionLoc  = (uint)shader2d.GetAttribLocation("vPos");
            uint texCoordsLoc = (uint)shader2d.GetAttribLocation("vTexCoords");
            GraphicsRenderer.AssertGLError();

            var gl = GraphicsRenderer.gl;

            gl.VertexAttribPointer(positionLoc,  2, Silk.NET.OpenGL.Legacy.VertexAttribPointerType.Float, false, sizeof(float)*4, (void*)0);
            gl.VertexAttribPointer(texCoordsLoc, 2, Silk.NET.OpenGL.Legacy.VertexAttribPointerType.Float, false, sizeof(float)*4, (void*)(sizeof(float)*2));
            GraphicsRenderer.AssertGLError();

            gl.EnableVertexAttribArray(positionLoc);
            gl.EnableVertexAttribArray(texCoordsLoc);
            GraphicsRenderer.AssertGLError();

            gl.DrawElements(Silk.NET.OpenGL.Legacy.PrimitiveType.Triangles, (uint)indices.Length, Silk.NET.OpenGL.Legacy.DrawElementsType.UnsignedInt, (void*)0);

            GraphicsRenderer.AssertGLError();
            gl.DisableVertexAttribArray(positionLoc);
            gl.DisableVertexAttribArray(texCoordsLoc);

            GraphicsRenderer.AssertGLError();
            
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

            rt.Dispose();
            vbo.Dispose();
            ebo.Dispose();
        }
    }
}
