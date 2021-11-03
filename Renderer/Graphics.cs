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

    public static class GraphicsRenderer {
        public static GL gl {get; private set;}

        private static FaceCulling _cullFace;
        public static FaceCulling CullFace {
            get => _cullFace;
            set {
                if ((_cullFace = value) != FaceCulling.None) {
                    gl.Enable(EnableCap.CullFace);
                    gl.CullFace((GLEnum)value);
                }
                else
                    gl.Disable(EnableCap.CullFace);
            }
        }

        private static WindingOrder _windingOrder;
        public static WindingOrder CullDirection {
            get => _windingOrder;
            set {
                _windingOrder = value;
                gl.FrontFace((GLEnum)value);
            }
        }

        private static bool _depthTest;
        public static bool DepthTest {
            get => _depthTest;
            set {
                _depthTest = value;

                if (value)
                    gl.Enable(EnableCap.DepthTest);
                else
                    gl.Disable(EnableCap.DepthTest);
            }
        }

        private static bool _wireframe;
        public static bool Wireframe {
            get => _wireframe;
            set {
                _wireframe = value;
                if (value)
                    gl.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Line);
                else
                    gl.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Fill);
            }
        }

        // TODO: Stencil


        public static void InitializeGraphics(GL gl) {
            GraphicsRenderer.gl = gl;

            DepthTest = true;
            CullFace = FaceCulling.Back;
            CullDirection = WindingOrder.CounterClockwise;

            GraphicsRenderer.gl.Enable(EnableCap.Blend);
            GraphicsRenderer.gl.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
        }

        public static void Clear(Color color) {
            gl.ClearColor(color.R, color.G, color.B, color.A);
            gl.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit | ClearBufferMask.StencilBufferBit);
        }

        public static void Clear() {
            Clear(Color.Transparent);
        }

        
    }
}