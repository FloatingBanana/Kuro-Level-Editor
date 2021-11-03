using System;
using Silk.NET.OpenGL.Legacy;

namespace Kuro.Renderer {
    public class BufferObject<TDataType> : IDisposable where TDataType : unmanaged {
        private GL _gl => GraphicsRenderer.gl;
        private uint _handle;
        private BufferTargetARB _bufferType;

        public unsafe BufferObject( Span<TDataType> data, BufferTargetARB bufferType) {
            _bufferType = bufferType;
            _handle = _gl.GenBuffer();

            Bind();
            fixed (void* d = data) {
                _gl.BufferData(bufferType, (nuint)(data.Length * sizeof(TDataType)), d, BufferUsageARB.StaticDraw);
            }

            _gl.BindBuffer(_bufferType, 0);
        }

        public void Bind() {
            _gl.BindBuffer(_bufferType, _handle);
        }

        public void Dispose() {
            _gl.DeleteBuffer(_handle);
        }
    }

}