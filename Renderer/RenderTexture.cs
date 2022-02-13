using System;
using System.Numerics;

using Silk.NET.OpenGL.Legacy;

namespace Kuro.Renderer {
    public class RenderTexture : IDisposable {
        private static GL _gl => GraphicsRenderer.gl;
        private const FramebufferTarget FB_TARGET = FramebufferTarget.Framebuffer;

        private uint _fbo;
        private uint _depthStencil;
        public uint Handle {get; private set;}

        public FramebufferStatus Status {
            get => (FramebufferStatus)_gl.CheckFramebufferStatus(FB_TARGET);
        }

        public unsafe RenderTexture(int width, int height) {
            _fbo = _gl.GenFramebuffer();
            _depthStencil = _gl.GenRenderbuffer();
            Handle = _gl.GenTexture();

            BindRendering();

            // Color buffer
            _gl.BindTexture(TextureTarget.Texture2D, Handle);
            _gl.TexImage2D(TextureTarget.Texture2D, 0, InternalFormat.Rgb, (uint)width, (uint)height, 0, PixelFormat.Rgb, PixelType.UnsignedInt, null);
            _gl.TexParameterI(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureFilter.Linear);
            _gl.TexParameterI(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureFilter.Linear);
            _gl.FramebufferTexture2D(FB_TARGET, FramebufferAttachment.ColorAttachment0, TextureTarget.Texture2D, Handle, 0);
            _gl.BindTexture(TextureTarget.Texture2D, 0);

            // Depth and stencil buffer
            _gl.BindRenderbuffer(RenderbufferTarget.Renderbuffer, _depthStencil);
            _gl.RenderbufferStorage(RenderbufferTarget.Renderbuffer, InternalFormat.Depth24Stencil8, (uint)width, (uint)height);
            _gl.BindRenderbuffer(RenderbufferTarget.Renderbuffer, 0);
            
            _gl.BindFramebuffer(FB_TARGET, 0);
            AssertCompleted();
            GraphicsRenderer.AssertGLError();
        }

        public RenderTexture(Vector2 size) : this((int)size.X, (int)size.Y) {}

        public void BindRendering() {
            _gl.BindFramebuffer(FB_TARGET, _fbo);
        }

        public void Bind(TextureUnit textureSlot) {
            _gl.ActiveTexture(textureSlot);
            _gl.BindTexture(TextureTarget.Texture2D, Handle);
        }

        public void Bind(int textureSlot) {
            Bind(TextureUnit.Texture0 + textureSlot);
        }

        private void AssertCompleted() {
            if (Status != FramebufferStatus.FramebufferComplete) {
                throw new InvalidOperationException("Framebuffer is not complete: " + Status.ToString());
            }
        }

        public void Dispose() {
            _gl.DeleteFramebuffer(_fbo);
            _gl.DeleteTexture(Handle);
            _gl.DeleteTexture(_depthStencil);
        }
    }
}