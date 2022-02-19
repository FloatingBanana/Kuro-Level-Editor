using System;
using System.Runtime.InteropServices;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Kuro.LevelEditor.Graphics {

    [StructLayout(LayoutKind.Sequential, Pack = 0)]
    public struct Vertex {
        public Vector3 Position;
        public Vector3 Normal;
        public Vector2 TexCoords;
        // public Vector3 Tangent;
    }

    public class KuroMeshPart : IDisposable {
        private readonly Vertex[] vertices;
        private readonly uint[] indices;
        private readonly GraphicsDevice _graphicsDevice;
        
        public VertexBuffer vbo;
        public IndexBuffer ebo;

        public Effect Shader {get; private set;}

        private static VertexDeclaration _vertexDeclaration = new(new[] {
            new VertexElement((int)Marshal.OffsetOf<Vertex>("Position"), VertexElementFormat.Vector3, VertexElementUsage.Position, 0),
            new VertexElement((int)Marshal.OffsetOf<Vertex>("TexCoords"), VertexElementFormat.Vector2, VertexElementUsage.TextureCoordinate, 2),
        });

        public unsafe KuroMeshPart(GraphicsDevice gd, Vertex[] vertices, uint[] indices, Effect shader) {
            this.vertices = vertices;
            this.indices = indices;

            _graphicsDevice = gd;
            Shader = shader;

            vbo = new VertexBuffer(gd, _vertexDeclaration, vertices.Length, BufferUsage.None);
            ebo = new IndexBuffer(gd, IndexElementSize.ThirtyTwoBits, indices.Length, BufferUsage.None);

            vbo.SetData<Vertex>(0, vertices, 0, vertices.Length, sizeof(Vertex));
            ebo.SetData<uint>(0, indices, 0, indices.Length);
        }

        public unsafe void Draw() {
            _graphicsDevice.SetVertexBuffer(vbo);
            _graphicsDevice.Indices = ebo;

            foreach (var pass in Shader.CurrentTechnique.Passes) {
                pass.Apply();
                _graphicsDevice.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, indices.Length / 3);
            }

        }
        
        public void Dispose() {
            vbo.Dispose();
            ebo.Dispose();
        }
    }

    public class KuroModelMesh : KuroModelNode, IDisposable {
        public KuroMeshPart[] Parts {get; private set;}

        public KuroModelMesh(string name, Matrix transform, KuroModelNode parent, KuroMeshPart[] parts) : base(name, transform, parent) {
            Parts = parts;
        }
        
        public void Dispose() {
            foreach (var part in Parts)
                part.Dispose();
        }
    }
}
