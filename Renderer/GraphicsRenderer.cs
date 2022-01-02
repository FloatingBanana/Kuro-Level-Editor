using System;
using System.Drawing;
using Silk.NET.OpenGL.Legacy;
using Silk.NET.Windowing;
using Silk.NET.Input;

namespace Kuro.Renderer {
    public enum FaceCulling {
        None,
        Front = GLEnum.Front,
        Back = GLEnum.Back,
        Both = GLEnum.FrontAndBack,
    }

    public enum WindingOrder {
        Clockwise = GLEnum.CW,
        CounterClockwise = GLEnum.Ccw,
    }


    // REVIEW: I think this should be an object
    //         instead of a static class
    public static partial class GraphicsRenderer {
        public static GL gl {get; private set;}

        public static FaceCulling CullFace {
            get => _currState.cullFace;
            set {
                if ((_currState.cullFace = value) != FaceCulling.None) {
                    gl.Enable(EnableCap.CullFace);
                    gl.CullFace((GLEnum)value);
                }
                else
                    gl.Disable(EnableCap.CullFace);
            }
        }

        public static WindingOrder CullDirection {
            get => _currState.cullDirection;
            set {
                _currState.cullDirection = value;
                gl.FrontFace((GLEnum)value);
            }
        }

        public static bool DepthTest {
            get => _currState.depthTest;
            set {
                _currState.depthTest = value;

                if (value)
                    gl.Enable(EnableCap.DepthTest);
                else
                    gl.Disable(EnableCap.DepthTest);
            }
        }

        public static bool Wireframe {
            get => _currState.wireframe;
            set {
                _currState.wireframe = value;

                if (value)
                    gl.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Line);
                else
                    gl.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Fill);
            }
        }

        // TODO: Stencil

        public static Rectangle Scissor {
            get => _currState.scissor;
            set {
                _currState.scissor = value;
                gl.Scissor(value.X, value.Y, (uint)value.Width, (uint)value.Height);
            }
        }

        public static void InitializeGraphics(GL glApi) {
            gl = glApi;

            // gl.Enable(EnableCap.Blend);
            gl.Enable(EnableCap.ScissorTest);
            gl.Disable(EnableCap.StencilTest);
            
            _graphicsStack.Push(new GraphicsState());

            DepthTest = true;
            CullFace = FaceCulling.None;
            CullDirection = WindingOrder.CounterClockwise;
            Scissor = new Rectangle(0, 0, (int)WindowManager.DisplaySize.X, (int)WindowManager.DisplaySize.Y);
            Wireframe = false;


            // gl.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
        }

        public static void Clear(Color color) {
            gl.ClearColor(color.R/255f, color.G/255f, color.B/255f, color.A/255f);
            gl.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit | ClearBufferMask.StencilBufferBit);
        }

        public static void Clear() {
            Clear(Color.Black);
        }

        public static void AssertGLError() {
            var err = gl.GetError();
            if (err != (GLEnum)ErrorCode.NoError)
                ; //Breakpoint goes here
        }
    }
}