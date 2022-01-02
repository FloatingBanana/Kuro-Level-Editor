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
        public Vector2 TexCoords;
        // public Vector3 Tangent;
    }

    public class MeshPart : IDisposable {
        private static GL gl => GraphicsRenderer.gl;
        
        private readonly Vertex[] vertices;
        private readonly uint[] indices;
        
        public BufferObject<Vertex> vbo;
        public BufferObject<uint> ebo;

        public Shader Shader {get; private set;}

        public MeshPart(Vertex[] vertices, uint[] indices, Shader shader) {
            this.vertices = vertices;
            this.indices = indices;

            Shader = shader;

            vbo = new BufferObject<Vertex>(vertices, BufferTargetARB.ArrayBuffer);
            ebo = new BufferObject<uint>(indices, BufferTargetARB.ElementArrayBuffer);
        }

        public unsafe void Draw() {
            ebo.Bind();
            vbo.Bind();

            const VertexAttribPointerType attrType = VertexAttribPointerType.Float;
            uint stride = (uint)sizeof(Vertex);

            uint positionLoc  = (uint)Shader.GetAttribLocation("vPosition");
            // uint normalLoc    = (uint)Shader.GetAttribLocation("vNormal");
            uint texCoordsLoc = (uint)Shader.GetAttribLocation("vTexCoords");

            gl.VertexAttribPointer(positionLoc,  3, attrType, false, stride, (void*)Marshal.OffsetOf<Vertex>("Position"));
            // gl.VertexAttribPointer(normalLoc,    3, attrType, false, stride, (void*)Marshal.OffsetOf<Vertex>("Normal"));
            gl.VertexAttribPointer(texCoordsLoc, 2, attrType, false, stride, (void*)Marshal.OffsetOf<Vertex>("TexCoords"));

            gl.EnableVertexAttribArray(positionLoc);
            // gl.EnableVertexAttribArray(normalLoc);
            gl.EnableVertexAttribArray(texCoordsLoc);

            gl.DrawElements(PrimitiveType.Triangles, (uint)indices.Length, DrawElementsType.UnsignedInt, (void*)0);

            gl.DisableVertexAttribArray(positionLoc);
            // gl.DisableVertexAttribArray(normalLoc);
            gl.DisableVertexAttribArray(texCoordsLoc);

            GraphicsRenderer.AssertGLError();
        }
        
        public void Dispose() {
            vbo.Dispose();
            ebo.Dispose();
        }
    }

    public class ModelMesh : ModelNode, IDisposable {
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