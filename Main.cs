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

        public Texture2D blankTexture;
        public IntPtr blankTextureHandle;

        // Managers
        private readonly GraphicsDeviceManager _graphics;
        private SpriteBatch _spriteBatch;
        public ImGuiRenderer imguiRenderer;

        // Device states
        public MouseStateExtended mouseState;
        public KeyboardStateExtended keyboardState;

        // Window management
        private readonly List<EditorWindow> windows = new();

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
            windows.Add(new ResourceWindow());


            ResourceManager.AddResource("room", new ModelResource("room"));

            // Test object
            var entity = EntityManager.AddEntity("Entity");
            var meshRenderer = new MeshRenderer();
            entity.AttachComponent(meshRenderer);

            meshRenderer.Mesh = ResourceManager.GetResource("drawer") as MeshResource;

            blankTexture = new Texture2D(GraphicsDevice, 1, 1);
            blankTexture.SetData(new[] {Color.Transparent});
            blankTextureHandle = imguiRenderer.BindTexture(blankTexture);
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

            blankTexture.Dispose();
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
        
        [DllImport("SDL2", CallingConvention = CallingConvention.Cdecl)]
        private static extern void SDL_MaximizeWindow(IntPtr window);
    }
}