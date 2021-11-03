using System;
using System.Drawing;
using System.Numerics;
using System.Runtime.InteropServices;
using Silk.NET.OpenGL.Legacy;

namespace Kuro.Renderer {

    [StructLayout(LayoutKind.Sequential, Pack = 0)]
    public struct Vertex {
        public Vector3 Position;
        public Vector3 Normal;
        public Vector3 Tangent;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 0)]
    public struct ColorVertex {
        public Vector3 Position;
        public Vector3 Normal;
        public Vector3 Tangent;
        public Vector4 Color;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 0)]
    public struct TextureVertex {
        public Vector3 Position;
        public Vector3 Normal;
        public Vector3 Tangent;
        public Vector2 TexCoords;
    }

    public class MeshPart : IDisposable {
        private GL _gl => GraphicsRenderer.gl;
        
        private Vertex[] vertices;
        private uint[] indices;
        
        BufferObject<Vertex> vbo;
        BufferObject<uint> ebo;

        public MeshPart(Vertex[] vertices, uint[] indices) {
            this.vertices = vertices;
            this.indices = indices;
        }

        private void _setupMesh() {
            vbo = new BufferObject<Vertex>(vertices, BufferTargetARB.ArrayBuffer);
            ebo = new BufferObject<uint>(indices, BufferTargetARB.ElementArrayBuffer);
        }
        
        public void Dispose() {
            vbo.Dispose();
            ebo.Dispose();
        }
    }

    public class ModelMesh : ModelNode, IDisposable {
        private GL _gl => GraphicsRenderer.gl;

        public MeshPart[] Parts {get; private set;}

        public ModelMesh(string name, Matrix4x4 transform, ModelNode parent, MeshPart[] parts) : base(name, transform, parent) {
            Parts = parts;
        }
        
        public void Dispose() {
            foreach (var part in Parts)
                part.Dispose();
        }
    }
}