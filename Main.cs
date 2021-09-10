using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MonoGame.Extended.Input;
using ImGuiNET;
using ImGuizmoNET;
using SceneEditor.EntitySystem;
using SceneEditor.Resources;

namespace SceneEditor {
    // TODO: Make this class partial and separate some actions into different files
    public class MainGame : Game {
        public static MainGame Instance {get; private set;}

        // Managers
        private GraphicsDeviceManager _graphics;
        private SpriteBatch _spriteBatch;
        public ImGuiRenderer imguiRenderer;

        // Device states
        public MouseStateExtended mouseState;
        public KeyboardStateExtended keyboardState;

        // Window management
        private List<EditorWindow> windows = new List<EditorWindow>();

        public MainGame() {
            _graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            IsMouseVisible = true;
            Instance = this;
        }

        protected override void Initialize() {
            imguiRenderer = new ImGuiRenderer(this);
            imguiRenderer.RebuildFontAtlas();
            ImGuizmo.SetImGuiContext(ImGui.GetCurrentContext());
            ImGui.GetIO().ConfigFlags |= ImGuiConfigFlags.DockingEnable;

            _graphics.PreferredBackBufferWidth = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Width;
            _graphics.PreferredBackBufferHeight = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Height;
            _graphics.ApplyChanges();

            Window.AllowUserResizing = true;
            SDL_MaximizeWindow(Window.Handle);

            base.Initialize();
        }

        protected override void LoadContent() {
            _spriteBatch = new SpriteBatch(GraphicsDevice);

            // Default windows
            windows.Add(new SceneWindow());
            windows.Add(new ComponentsWindow());
            windows.Add(new HierarchyWindow());


            // Test object
            ResourceManager.AddResource("drawer", new ModelResource("drawer"));
            var meshRes = ResourceManager.GetResource("Cube") as MeshResource;

            var entity = EntityManager.AddEntity("Entity");
            entity.AttachComponent(new Component[] {
                new Transform() {
                    TransformationMatrix = meshRes.Mesh.ParentBone.Transform
                },
                new MeshRenderer() {
                    MeshRes = meshRes
                }
            });
        }

        protected override void Update(GameTime gameTime) {
            mouseState = MouseExtended.GetState();
            keyboardState = KeyboardExtended.GetState();

            int i = 0;
            while (i < windows.Count) {
                windows[i].Update(gameTime);

                if (!windows[i].isOpen)
                    windows.RemoveAt(i);
                else
                    i++;
            }

            EntityManager.Update(gameTime);

            if (keyboardState.IsKeyDown(Keys.Escape)) Exit();
            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime) {
            GraphicsDevice.Clear(Color.Black);

            imguiRenderer.BeforeLayout(gameTime);
            RenderMainToolbar();

            uint dockId = ImGui.DockSpaceOverViewport();

            foreach (var window in windows) {
                ImGui.PushID(window.GetHashCode());
                window.Render(gameTime, dockId);
                ImGui.PopID();
            }

            imguiRenderer.AfterLayout();

            base.Draw(gameTime);
        }

        protected override void UnloadContent() {
            _graphics.Dispose();

            foreach (var window in windows) {
                window.Close();
            }
        }


        private void RenderMainToolbar() {
            if (ImGui.BeginMainMenuBar()) {

                if (ImGui.BeginMenu("File")) {

                    if (ImGui.MenuItem("New", "ctrl+N")) {
                        // Save file
                    }

                    if (ImGui.MenuItem("Open", "ctrl+O")) {
                        // Open file
                    }
                    
                    if (ImGui.BeginMenu("Open recent")) {
                        // Show reent files
                        for (int i = 0; i < 4; i++)
                            ImGui.MenuItem(@"C:\Users\Someone\Projects\Scene " + i);
                        
                        ImGui.EndMenu();
                    }

                    ImGui.Separator();

                    if (ImGui.MenuItem("Save", "ctrl+S")) {
                        // Save file
                    }

                    if (ImGui.MenuItem("Save as", "ctrl+shift+S")) {
                        // Choose the location to save the file
                    }

                    ImGui.Separator();

                    if (ImGui.MenuItem("Quit", "alt+F4")) {
                        // Quit
                    }

                    ImGui.EndMenu();
                }

                ImGui.EndMainMenuBar();
            }
        }

        [DllImport("SDL2.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern void SDL_MaximizeWindow(IntPtr window);
    }
}