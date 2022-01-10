using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using ImGuiNET;
using ImGuizmoNET;

using SNVector2 = System.Numerics.Vector2;

namespace Kuro.LevelEditor {
    class EditorWindow {
        protected const float GUI_FILL = -float.Epsilon;
        
        public Rectangle WindowRect {get; private set;}
        public bool isOpen = true;

        protected static MainGame Game => MainGame.Instance;
        protected static GraphicsDevice GraphicsDevice => Game.GraphicsDevice;

        public virtual void Render(GameTime gameTime, uint dockId) {}
        public virtual void Update(GameTime gameTime) {}
        public virtual void Close() {}

    }
}