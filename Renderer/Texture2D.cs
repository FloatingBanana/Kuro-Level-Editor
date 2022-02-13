using System;
using System.Runtime.InteropServices;
using Silk.NET.OpenGL.Legacy;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace Kuro.Renderer {
    public enum TextureFilter {
        Linear = GLEnum.Linear,
        Nearest = GLEnum.Nearest
    }

    public enum TextureWrap {
        ClampZero = GLEnum.ClampToBorder,
        Clamp = GLEnum.ClampToEdge,
        Repeat = GLEnum.Repeat,
        MirroredRepeat = GLEnum.MirroredRepeat,
    }

    public class Texture2D : IDisposable {
        private static GL gl => GraphicsRenderer.gl;
        
        public static TextureFilter DefaultMinifyFilter {get; set;} = TextureFilter.Linear;
        public static TextureFilter DefaultMagnifyFilter {get; set;} = TextureFilter.Linear;
        public static TextureWrap DefaultWrapMode {get; set;} = TextureWrap.ClampZero;

        public uint Handle {get; private set;}

        private TextureFilter _magFilter = DefaultMinifyFilter;
        public TextureFilter MagnifyFilter {
            get => _magFilter;
            set {
                _magFilter = value;
                Bind();
                gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)value);
            }
        }

        private TextureFilter _minFilter = DefaultMagnifyFilter;
        public TextureFilter MinifyFilter {
            get => _minFilter;
            set {
                _minFilter = value;
                Bind();
                gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)value);
            }
        }

        private TextureWrap _wrapMode = DefaultWrapMode;
        public TextureWrap WrapMode {
            get => _wrapMode;
            set {
                _wrapMode = value;
                Bind();
                gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)value);
                gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)value);
            }
        }

        public unsafe Texture2D(string path) {
            var img = (Image<Rgba32>)Image.Load(path);

            img.Mutate(x => x.Flip(FlipMode.Vertical));

            fixed (void* data = &MemoryMarshal.GetReference(img.GetPixelRowSpan(0))) {
                Load(data, img.Width, img.Height);
            }

            img.Dispose();
        }

        public unsafe Texture2D(Span<Byte> data, int width, int height) {
            fixed (void* d = &data[0]) {
                Load(d, width, height);
            }
        }

        public unsafe Texture2D(byte* data, int width, int height) {
            Load(data, width, height);
        }

        private unsafe void Load(void* data, int width, int height) {
            Handle = gl.GenTexture();
            Bind();

            gl.TexImage2D(TextureTarget.Texture2D, 0, (int)InternalFormat.Rgba, (uint)width, (uint)height, 0, PixelFormat.Rgba, PixelType.UnsignedByte, data);

            gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)DefaultWrapMode);
            gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)DefaultWrapMode);
            gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)DefaultMinifyFilter);
            gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)DefaultMagnifyFilter);
        }

        public void Bind(TextureUnit textureSlot) {
            gl.ActiveTexture(textureSlot);
            gl.BindTexture(TextureTarget.Texture2D, Handle);
        }

        public void Bind(int textureSlot = 0) {
            Bind(TextureUnit.Texture0 + textureSlot);
        }

        public void Dispose() {
            gl.DeleteTexture(Handle);
        }
    }
}