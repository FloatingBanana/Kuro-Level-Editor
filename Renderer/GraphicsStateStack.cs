using System;
using System.Numerics;
using System.Drawing;
using System.Collections.Generic;

namespace Kuro.Renderer {
    class GraphicsState {
        public FaceCulling cullFace;
        public WindingOrder cullDirection;
        public bool depthTest;
        public bool wireframe;
        public Rectangle scissor;
    }

    public static partial class GraphicsRenderer {
        private const int MAX_STACK_SIZE = 100;
        private static Stack<GraphicsState> _graphicsStack = new();
        private static GraphicsState _currState => _graphicsStack.Peek();

        public static void PushState() {
            _graphicsStack.Push(_currState);

            if (_graphicsStack.Count > MAX_STACK_SIZE)
                throw new InvalidOperationException("State stack overflow. Maybe there are too many pushs withoup pops.");
        }

        public static void PopState() {
            _graphicsStack.Pop();

            if (_graphicsStack.Count == 0)
                throw new InvalidOperationException("State stack is empty. Mayybe there are pops without pushs");
            
            CullFace = _currState.cullFace;
            CullDirection = _currState.cullDirection;
            DepthTest = _currState.depthTest;
            Wireframe = _currState.wireframe;
            Scissor = _currState.scissor;
        }
    }
}