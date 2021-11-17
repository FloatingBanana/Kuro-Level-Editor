using System;
using System.Collections.Generic;
using System.Drawing;
using System.Numerics;
using ImGuiNET;
using Silk.NET.Input;
using Silk.NET.Input.Extensions;
using Silk.NET.OpenGL.Legacy;

namespace Kuro.Renderer.Imgui
{
    public class ImGuiController : IDisposable
    {
        private static GL gl => GraphicsRenderer.gl;

        private bool _frameBegun;
        private readonly List<char> _pressedChars = new List<char>();

        private int _attribLocationTex;
        private int _attribLocationProjMtx;
        private int _attribLocationVtxPos;
        private int _attribLocationVtxUV;
        private int _attribLocationVtxColor;
        private uint _vboHandle;
        private uint _elementsHandle;

        private Texture2D _fontTexture;
        private Shader _shader;

        private int _windowWidth;
        private int _windowHeight;

        /// <summary>
        /// Constructs a new ImGuiController.
        /// </summary>
        public ImGuiController()
        {
            Init();

            var io = ImGuiNET.ImGui.GetIO();
            io.Fonts.AddFontDefault();
            io.BackendFlags |= ImGuiBackendFlags.RendererHasVtxOffset;

            GraphicsRenderer.AssertGLError();

            CreateDeviceResources();
            SetKeyMappings();

            SetPerFrameImGuiData(1f / 60f);

            BeginFrame();
        }

        private void Init()
        {
            _windowWidth = (int)WindowManager.WindowSize.X;
            _windowHeight = (int)WindowManager.WindowSize.Y;

            GraphicsRenderer.AssertGLError();

            IntPtr context = ImGuiNET.ImGui.CreateContext();
            ImGuiNET.ImGui.SetCurrentContext(context);
            ImGuiNET.ImGui.StyleColorsDark();

            GraphicsRenderer.AssertGLError();
        }

        private void BeginFrame()
        {
            ImGuiNET.ImGui.NewFrame();
            _frameBegun = true;
            WindowManager.Resize += WindowResized;
            WindowManager.KeyChar += OnKeyChar;
        }

        private void OnKeyChar(IKeyboard keyboard, char character)
        {
            _pressedChars.Add(character);
        }

        private void WindowResized(Vector2 size)
        {
            _windowWidth = (int)size.X;
            _windowHeight = (int)size.Y;
        }

        /// <summary>
        /// Renders the ImGui draw list data.
        /// </summary>
        public void Render()
        {
            if (_frameBegun)
            {
                _frameBegun = false;
                ImGuiNET.ImGui.Render();
                RenderImDrawData(ImGuiNET.ImGui.GetDrawData());
            }
        }

        /// <summary>
        /// Updates ImGui input and IO configuration state.
        /// </summary>
        public void Update(float deltaSeconds)
        {
            if (_frameBegun)
            {
                ImGuiNET.ImGui.Render();
            }

            SetPerFrameImGuiData(deltaSeconds);
            UpdateImGuiInput();

            _frameBegun = true;
            ImGuiNET.ImGui.NewFrame();
        }

        /// <summary>
        /// Sets per-frame data based on the associated window.
        /// This is called by Update(float).
        /// </summary>
        private void SetPerFrameImGuiData(float deltaSeconds)
        {
            var io = ImGuiNET.ImGui.GetIO();
            io.DisplaySize = new Vector2(_windowWidth, _windowHeight);

            if (_windowWidth > 0 && _windowHeight > 0)
            {
                io.DisplayFramebufferScale = new Vector2(WindowManager.DisplaySize.X / _windowWidth,
                    WindowManager.DisplaySize.Y / _windowHeight);
            }

            io.DeltaTime = deltaSeconds; // DeltaTime is in seconds.
        }

        private void UpdateImGuiInput()
        {
            var io = ImGuiNET.ImGui.GetIO();

            var mouseState = WindowManager.Mouse.CaptureState();
            var keyboardState = WindowManager.Keyboard;

            io.MouseDown[0] = mouseState.IsButtonPressed(MouseButton.Left);
            io.MouseDown[1] = mouseState.IsButtonPressed(MouseButton.Right);
            io.MouseDown[2] = mouseState.IsButtonPressed(MouseButton.Middle);

            var point = new Point((int) mouseState.Position.X, (int) mouseState.Position.Y);
            io.MousePos = new Vector2(point.X, point.Y);

            // FIXME: Scroll not working
            var wheel = mouseState.GetScrollWheels()[0];
            io.MouseWheel = wheel.Y;
            io.MouseWheelH = wheel.X;

            foreach (Key key in Enum.GetValues(typeof(Key)))
            {
                if (key == Key.Unknown)
                {
                    continue;
                }
                io.KeysDown[(int) key] = keyboardState.IsKeyPressed(key);
            }

            foreach (var c in _pressedChars)
            {
                io.AddInputCharacter(c);
            }

            _pressedChars.Clear();

            io.KeyCtrl = keyboardState.IsKeyPressed(Key.ControlLeft) || keyboardState.IsKeyPressed(Key.ControlRight);
            io.KeyAlt = keyboardState.IsKeyPressed(Key.AltLeft) || keyboardState.IsKeyPressed(Key.AltRight);
            io.KeyShift = keyboardState.IsKeyPressed(Key.ShiftLeft) || keyboardState.IsKeyPressed(Key.ShiftRight);
            io.KeySuper = keyboardState.IsKeyPressed(Key.SuperLeft) || keyboardState.IsKeyPressed(Key.SuperRight);
        }

        internal void PressChar(char keyChar)
        {
            _pressedChars.Add(keyChar);
        }

        private static void SetKeyMappings()
        {
            var io = ImGuiNET.ImGui.GetIO();
            io.KeyMap[(int) ImGuiKey.Tab] = (int) Key.Tab;
            io.KeyMap[(int) ImGuiKey.LeftArrow] = (int) Key.Left;
            io.KeyMap[(int) ImGuiKey.RightArrow] = (int) Key.Right;
            io.KeyMap[(int) ImGuiKey.UpArrow] = (int) Key.Up;
            io.KeyMap[(int) ImGuiKey.DownArrow] = (int) Key.Down;
            io.KeyMap[(int) ImGuiKey.PageUp] = (int) Key.PageUp;
            io.KeyMap[(int) ImGuiKey.PageDown] = (int) Key.PageDown;
            io.KeyMap[(int) ImGuiKey.Home] = (int) Key.Home;
            io.KeyMap[(int) ImGuiKey.End] = (int) Key.End;
            io.KeyMap[(int) ImGuiKey.Delete] = (int) Key.Delete;
            io.KeyMap[(int) ImGuiKey.Backspace] = (int) Key.Backspace;
            io.KeyMap[(int) ImGuiKey.Enter] = (int) Key.Enter;
            io.KeyMap[(int) ImGuiKey.Escape] = (int) Key.Escape;
            io.KeyMap[(int) ImGuiKey.A] = (int) Key.A;
            io.KeyMap[(int) ImGuiKey.C] = (int) Key.C;
            io.KeyMap[(int) ImGuiKey.V] = (int) Key.V;
            io.KeyMap[(int) ImGuiKey.X] = (int) Key.X;
            io.KeyMap[(int) ImGuiKey.Y] = (int) Key.Y;
            io.KeyMap[(int) ImGuiKey.Z] = (int) Key.Z;
        }

        private unsafe void SetupRenderState(ImDrawDataPtr drawDataPtr, int framebufferWidth, int framebufferHeight)
        {
            // Setup render state: alpha-blending enabled, no face culling, no depth testing, scissor enabled, polygon fill
            gl.Enable(GLEnum.Blend);
            gl.BlendEquation(GLEnum.FuncAdd);
            gl.BlendFuncSeparate(GLEnum.SrcAlpha, GLEnum.OneMinusSrcAlpha, GLEnum.One, GLEnum.OneMinusSrcAlpha);
            gl.Disable(GLEnum.CullFace);
            gl.Disable(GLEnum.DepthTest);
            gl.Disable(GLEnum.StencilTest);
            gl.Enable(GLEnum.ScissorTest);
            gl.PolygonMode(GLEnum.FrontAndBack, GLEnum.Fill);

            float L = drawDataPtr.DisplayPos.X;
            float R = drawDataPtr.DisplayPos.X + drawDataPtr.DisplaySize.X;
            float T = drawDataPtr.DisplayPos.Y;
            float B = drawDataPtr.DisplayPos.Y + drawDataPtr.DisplaySize.Y;

            Span<float> orthoProjection = stackalloc float[] {
                2.0f / (R - L), 0.0f, 0.0f, 0.0f,
                0.0f, 2.0f / (T - B), 0.0f, 0.0f,
                0.0f, 0.0f, -1.0f, 0.0f,
                (R + L) / (L - R), (T + B) / (B - T), 0.0f, 1.0f,
            };

            _shader.Use();
            gl.Uniform1(_attribLocationTex, 0);
            gl.UniformMatrix4(_attribLocationProjMtx, 1, false, orthoProjection);
            GraphicsRenderer.AssertGLError();

            gl.BindSampler(0, 0);

            // Bind vertex/index buffers and setup attributes for ImDrawVert
            gl.BindBuffer(GLEnum.ArrayBuffer, _vboHandle);
            gl.BindBuffer(GLEnum.ElementArrayBuffer, _elementsHandle);
            gl.EnableVertexAttribArray((uint) _attribLocationVtxPos);
            gl.EnableVertexAttribArray((uint) _attribLocationVtxUV);
            gl.EnableVertexAttribArray((uint) _attribLocationVtxColor);
            gl.VertexAttribPointer((uint) _attribLocationVtxPos, 2, GLEnum.Float, false, (uint) sizeof(ImDrawVert), (void*) 0);
            gl.VertexAttribPointer((uint) _attribLocationVtxUV, 2, GLEnum.Float, false, (uint) sizeof(ImDrawVert), (void*) 8);
            gl.VertexAttribPointer((uint) _attribLocationVtxColor, 4, GLEnum.UnsignedByte, true, (uint) sizeof(ImDrawVert), (void*) 16);
        }

        private unsafe void RenderImDrawData(ImDrawDataPtr drawDataPtr)
        {
            int framebufferWidth = (int) (drawDataPtr.DisplaySize.X * drawDataPtr.FramebufferScale.X);
            int framebufferHeight = (int) (drawDataPtr.DisplaySize.Y * drawDataPtr.FramebufferScale.Y);
            if (framebufferWidth <= 0 || framebufferHeight <= 0)
                return;

            // Backup GL state
            gl.GetInteger(GLEnum.ActiveTexture, out int lastActiveTexture);
            gl.ActiveTexture(GLEnum.Texture0);

            gl.GetInteger(GLEnum.CurrentProgram, out int lastProgram);
            gl.GetInteger(GLEnum.TextureBinding2D, out int lastTexture);

            gl.GetInteger(GLEnum.SamplerBinding, out int lastSampler);

            gl.GetInteger(GLEnum.ArrayBufferBinding, out int lastArrayBuffer);

            Span<int> lastPolygonMode = stackalloc int[2];
            gl.GetInteger(GLEnum.PolygonMode, lastPolygonMode);

            Span<int> lastScissorBox = stackalloc int[4];
            gl.GetInteger(GLEnum.ScissorBox, lastScissorBox);

            gl.GetInteger(GLEnum.BlendSrcRgb, out int lastBlendSrcRgb);
            gl.GetInteger(GLEnum.BlendDstRgb, out int lastBlendDstRgb);

            gl.GetInteger(GLEnum.BlendSrcAlpha, out int lastBlendSrcAlpha);
            gl.GetInteger(GLEnum.BlendDstAlpha, out int lastBlendDstAlpha);

            gl.GetInteger(GLEnum.BlendEquationRgb, out int lastBlendEquationRgb);
            gl.GetInteger(GLEnum.BlendEquationAlpha, out int lastBlendEquationAlpha);

            bool lastEnableBlend = gl.IsEnabled(GLEnum.Blend);
            bool lastEnableCullFace = gl.IsEnabled(GLEnum.CullFace);
            bool lastEnableDepthTest = gl.IsEnabled(GLEnum.DepthTest);
            bool lastEnableStencilTest = gl.IsEnabled(GLEnum.StencilTest);
            bool lastEnableScissorTest = gl.IsEnabled(GLEnum.ScissorTest);

            SetupRenderState(drawDataPtr, framebufferWidth, framebufferHeight);

            // Will project scissor/clipping rectangles into framebuffer space
            Vector2 clipOff = drawDataPtr.DisplayPos;         // (0,0) unless using multi-viewports
            Vector2 clipScale = drawDataPtr.FramebufferScale; // (1,1) unless using retina display which are often (2,2)

            // Render command lists
            for (int n = 0; n < drawDataPtr.CmdListsCount; n++)
            {
                ImDrawListPtr cmdListPtr = drawDataPtr.CmdListsRange[n];

                // Upload vertex/index buffers

                gl.BufferData(GLEnum.ArrayBuffer, (nuint) (cmdListPtr.VtxBuffer.Size * sizeof(ImDrawVert)), (void*) cmdListPtr.VtxBuffer.Data, GLEnum.StreamDraw);
                GraphicsRenderer.AssertGLError();
                gl.BufferData(GLEnum.ElementArrayBuffer, (nuint) (cmdListPtr.IdxBuffer.Size * sizeof(ushort)), (void*) cmdListPtr.IdxBuffer.Data, GLEnum.StreamDraw);
                GraphicsRenderer.AssertGLError();

                for (int cmd_i = 0; cmd_i < cmdListPtr.CmdBuffer.Size; cmd_i++)
                {
                    ImDrawCmdPtr cmdPtr = cmdListPtr.CmdBuffer[cmd_i];

                    if (cmdPtr.UserCallback != IntPtr.Zero)
                    {
                        throw new NotImplementedException();
                    }
                    else
                    {
                        Vector4 clipRect;
                        clipRect.X = (cmdPtr.ClipRect.X - clipOff.X) * clipScale.X;
                        clipRect.Y = (cmdPtr.ClipRect.Y - clipOff.Y) * clipScale.Y;
                        clipRect.Z = (cmdPtr.ClipRect.Z - clipOff.X) * clipScale.X;
                        clipRect.W = (cmdPtr.ClipRect.W - clipOff.Y) * clipScale.Y;

                        if (clipRect.X < framebufferWidth && clipRect.Y < framebufferHeight && clipRect.Z >= 0.0f && clipRect.W >= 0.0f)
                        {
                            // Apply scissor/clipping rectangle
                            gl.Scissor((int) clipRect.X, (int) (framebufferHeight - clipRect.W), (uint) (clipRect.Z - clipRect.X), (uint) (clipRect.W - clipRect.Y));
                            GraphicsRenderer.AssertGLError();

                            // Bind texture, Draw
                            gl.BindTexture(GLEnum.Texture2D, (uint) cmdPtr.TextureId);
                            GraphicsRenderer.AssertGLError();

                            // gl.DrawElementsBaseVertex(GLEnum.Triangles, cmdPtr.ElemCount, GLEnum.UnsignedShort, (void*) (cmdPtr.IdxOffset * sizeof(ushort)), (int) cmdPtr.VtxOffset);
                            gl.DrawElements(PrimitiveType.Triangles, (uint)cmdPtr.ElemCount, DrawElementsType.UnsignedShort, (void*)(cmdPtr.IdxOffset * sizeof(ushort)));
                            GraphicsRenderer.AssertGLError();
                        }
                    }
                }
            }

            // Restore modified GL state
            gl.UseProgram((uint) lastProgram);
            gl.BindTexture(GLEnum.Texture2D, (uint) lastTexture);

            gl.BindSampler(0, (uint) lastSampler);

            gl.ActiveTexture((GLEnum) lastActiveTexture);

            gl.BindBuffer(GLEnum.ArrayBuffer, (uint) lastArrayBuffer);
            gl.BlendEquationSeparate((GLEnum) lastBlendEquationRgb, (GLEnum) lastBlendEquationAlpha);
            gl.BlendFuncSeparate((GLEnum) lastBlendSrcRgb, (GLEnum) lastBlendDstRgb, (GLEnum) lastBlendSrcAlpha, (GLEnum) lastBlendDstAlpha);

            if (lastEnableBlend)
            {
                gl.Enable(GLEnum.Blend);
            }
            else
            {
                gl.Disable(GLEnum.Blend);
            }

            if (lastEnableCullFace)
            {
                gl.Enable(GLEnum.CullFace);
            }
            else
            {
                gl.Disable(GLEnum.CullFace);
            }

            if (lastEnableDepthTest)
            {
                gl.Enable(GLEnum.DepthTest);
            }
            else
            {
                gl.Disable(GLEnum.DepthTest);
            }
            if (lastEnableStencilTest)
            {
                gl.Enable(GLEnum.StencilTest);
            }
            else
            {
                gl.Disable(GLEnum.StencilTest);
            }

            if (lastEnableScissorTest)
            {
                gl.Enable(GLEnum.ScissorTest);
            }
            else
            {
                gl.Disable(GLEnum.ScissorTest);
            }

            gl.PolygonMode(GLEnum.FrontAndBack, (GLEnum) lastPolygonMode[0]);

            gl.Scissor(lastScissorBox[0], lastScissorBox[1], (uint) lastScissorBox[2], (uint) lastScissorBox[3]);
        }

        private void CreateDeviceResources()
        {
            // Backup GL state

            GraphicsRenderer.AssertGLError();

            gl.GetInteger(GLEnum.TextureBinding2D, out int lastTexture);
            gl.GetInteger(GLEnum.ArrayBufferBinding, out int lastArrayBuffer);

            string vertexSource = @"
            #version 120

            attribute vec2 Position;
            attribute vec2 UV;
            attribute vec4 Color;

            uniform mat4 ProjMtx;

            varying vec2 Frag_UV;
            varying vec4 Frag_Color;

            void main() {
                Frag_UV = UV;
                Frag_Color = Color;
                gl_Position = ProjMtx * vec4(Position.xy,0,1);
            }";


            string fragmentSource = @"
            #version 120

            varying vec2 Frag_UV;
            varying vec4 Frag_Color;

            uniform sampler2D Texture;

            void main() {
                gl_FragColor = Frag_Color * texture2D(Texture, Frag_UV.st);
            }";

            _shader = new Shader(vertexSource, fragmentSource);

            _attribLocationTex = _shader.GetUniformLocation("Texture");
            _attribLocationProjMtx = _shader.GetUniformLocation("ProjMtx");
            _attribLocationVtxPos = _shader.GetAttribLocation("Position");
            _attribLocationVtxUV = _shader.GetAttribLocation("UV");
            _attribLocationVtxColor = _shader.GetAttribLocation("Color");

            _vboHandle = gl.GenBuffer();
            _elementsHandle = gl.GenBuffer();

            RecreateFontDeviceTexture();

            // Restore modified GL state
            gl.BindTexture(GLEnum.Texture2D, (uint) lastTexture);
            gl.BindBuffer(GLEnum.ArrayBuffer, (uint) lastArrayBuffer);

            GraphicsRenderer.AssertGLError();
        }

        /// <summary>
        /// Creates the texture used to render text.
        /// </summary>
        private unsafe void RecreateFontDeviceTexture()
        {
            // Build texture atlas
            var io = ImGuiNET.ImGui.GetIO();
            io.Fonts.GetTexDataAsRGBA32(out IntPtr pixels, out int width, out int height, out int bytesPerPixel);   // Load as RGBA 32-bit (75% of the memory is wasted, but default font is so small) because it is more likely to be compatible with user's existing shaders. If your ImTextureId represent a higher-level concept than just a GL texture id, consider calling GetTexDataAsAlpha8() instead to save on GPU memory.

            // Upload texture to graphics system
            gl.GetInteger(GLEnum.Texture2D, out int lastTexture);
         
            _fontTexture = new Texture2D((byte*)pixels, width, height);
            _fontTexture.Bind();
            _fontTexture.MagnifyFilter = TextureFilter.Linear;
            _fontTexture.MinifyFilter = TextureFilter.Linear;

            // Store our identifier
            io.Fonts.SetTexID((IntPtr) _fontTexture.Handle);

            // Restore state
            gl.BindTexture(GLEnum.Texture2D, (uint) lastTexture);
        }

        /// <summary>
        /// Frees all graphics resources used by the renderer.
        /// </summary>
        public void Dispose()
        {
            WindowManager.Resize -= WindowResized;
            WindowManager.Keyboard.KeyChar -= OnKeyChar;

            gl.DeleteBuffer(_vboHandle);
            gl.DeleteBuffer(_elementsHandle);

            _fontTexture.Dispose();
            _shader.Dispose();
        }
    }
}