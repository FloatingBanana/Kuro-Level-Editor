using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using ImGuiNET;
using ImGuizmoNET;

using SNVector2 = System.Numerics.Vector2;

namespace SceneEditor {
    class EditorWindow {
        public Rectangle windowRect {get; private set;}
        public bool isOpen = true;

        protected MainGame game => MainGame.Instance;
        protected GraphicsDevice graphicsDevice => game.GraphicsDevice;

        public virtual void Render(GameTime gameTime, uint dockId) {}
        public virtual void Update(GameTime gameTime) {}
        public virtual void Close() {}

    }
}