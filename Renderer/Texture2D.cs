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
        Clamp = GLEnum.Clamp,
        ClampEdge = GLEnum.ClampToEdge,
        Repeat = GLEnum.Repeat,
        MirroredRepeat = GLEnum.MirroredRepeat,
    }

    public class Texture2D : IDisposable {
        private static GL _gl => GraphicsRenderer.gl;
        private uint _handle;

        public static TextureFilter DefaultMinifyFilter {get; set;} = TextureFilter.Linear;
        public static TextureFilter DefaultMagnifyFilter {get; set;} = TextureFilter.Linear;
        public static TextureWrap DefaultWrapMode {get; set;} = TextureWrap.Clamp;

        private TextureFilter _magFilter = DefaultMinifyFilter;
        public TextureFilter MagnifyFilter {
            get => _magFilter;
            set {
                _magFilter = value;
                Bind();
                _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)value);
            }
        }

        private TextureFilter _minFilter = DefaultMagnifyFilter;
        public TextureFilter MinifyFilter {
            get => _minFilter;
            set {
                _minFilter = value;
                Bind();
                _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)value);
            }
        }

        private TextureWrap _wrapMode = DefaultWrapMode;
        public TextureWrap WrapMode {
            get => _wrapMode;
            set {
                _wrapMode = value;
                Bind();
                _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)value);
                _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)value);
            }
        }

        public unsafe Texture2D(string path) {
            var img = (Image<Rgba32>)Image.Load(path);

            img.Mutate(x => x.Flip(FlipMode.Vertical));

            fixed (void* data = &MemoryMarshal.GetReference(img.GetPixelRowSpan(0))) {
                Load(data, (uint)img.Width, (uint)img.Height);
            }

            img.Dispose();
        }

        public unsafe Texture2D(Span<Byte> data, uint width, uint height) {
            fixed (void* d = &data[0]) {
                Load(d, width, height);
            }
        }

        private unsafe void Load(void* data, uint width, uint height) {
            _handle = _gl.GenTexture();
            Bind();

            _gl.TexImage2D(TextureTarget.Texture2D, 0, (int)InternalFormat.Rgba, width, height, 0, PixelFormat.Rgba, PixelType.UnsignedByte, data);

            _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)DefaultWrapMode);
            _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)DefaultWrapMode);
            _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)DefaultMinifyFilter);
            _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)DefaultMagnifyFilter);

            // _gl.GenerateMipmap(TextureTarget.Texture2D);
        }

        public void Bind(TextureUnit textureSlot = TextureUnit.Texture0) {
            _gl.ActiveTexture(textureSlot);
            _gl.BindTexture(TextureTarget.Texture2D, _handle);
        }

        public void Dispose() {
            _gl.DeleteTexture(_handle);
        }
    }
}