using System;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Silk.NET.OpenGL.Legacy;
using Silk.NET.Assimp;

using Assimp = Silk.NET.Assimp.Assimp;
using AssScene = Silk.NET.Assimp.Scene;
using AssNode = Silk.NET.Assimp.Node;
using AssMesh = Silk.NET.Assimp.Mesh;
using AssFace = Silk.NET.Assimp.Face;
using AssMaterial = Silk.NET.Assimp.Material;
using AssCamera = Silk.NET.Assimp.Camera;
using AssLight = Silk.NET.Assimp.Light;
using AssTexture = Silk.NET.Assimp.Texture;

namespace Kuro.Renderer {
    public class Model : IDisposable {
        private static Assimp _assimp = Assimp.GetApi();
        private GL _gl => GraphicsRenderer.gl;

        private string directory;

        public List<ModelNode> Nodes {get; private set;}
        public ModelNode Root {get; private set;}

        public Mesh[] Meshes {
            get {
                return Nodes.Where((ModelNode node) => node is ModelMesh).Cast<Mesh>().ToArray();
            }
        }

        public Model(string path) {
            _loadModel(path);
        }

        private unsafe void _loadModel(string path) {
            var importFlags =
                PostProcessSteps.Triangulate |
                PostProcessSteps.FlipUVs |
                PostProcessSteps.GenerateSmoothNormals |
                PostProcessSteps.CalculateTangentSpace;
            
            AssScene* scene = _assimp.ImportFile(path, (uint)importFlags);

            if (scene == null || scene->MRootNode == null || (scene->MFlags & (uint)SceneFlags.Incomplete) != 0) {
                throw new ModelLoadingException($"Failed to load model at {path}: {_assimp.GetErrorStringS()}");
            }

            directory = Path.GetDirectoryName(path);
        }

        private unsafe void _processNode(AssNode* node, AssScene* scene, ModelNode parent) {
            ModelNode modelNode = null;

            // Mesh
            if (node->MNumMeshes > 0) {
                modelNode = _processMesh(node, scene, parent);
            }

            // Camera
            AssCamera* camera = null;
            for (int c = 0; c < scene->MNumCameras; c++) {
                if (scene->MCameras[c]->MName == node->MName) {
                    modelNode = _processCamera(node, scene->MCameras[c], parent)
                    break;
                }
            }

            if (modelNode != null) {
                parent.Children.Add(modelNode);
            }
            else {
                Console.WriteLine($"Discarted node '{node->MName}'");
            }

            new AssScene().MCameras[0]->
            new AssNode().

        }

        private unsafe ModelMesh _processMesh(AssNode* node, AssScene* scene, ModelNode parent) {
            var parts = new MeshPart[node->MNumMeshes];

            for (uint p = 0; p < node->MNumMeshes; p++) {
                AssMesh* part = scene->MMeshes[node->MMeshes[p]];

                Vertex[] vertices = new Vertex[part->MNumVertices];
                List<uint> indices = new List<uint>((int)part->MNumFaces * 3);
                
                for (uint v = 0; v < part->MNumVertices; v++) {
                    vertices[v] = new Vertex {
                        Position = part->MVertices[v],
                        Normal = part->MNormals[v],
                        Tangent = part->MTangents[v],
                    };
                }

                for (uint f = 0; f < part->MNumFaces; f++) {
                    AssFace face = part->MFaces[f];

                    for (uint i = 0; i < face.MNumIndices; i++)
                        indices.Add(face.MIndices[i]);
                }

                parts[p] = new MeshPart(vertices, indices.ToArray());
            }

            return new ModelMesh(node->MName, node->MTransformation, parent, parts);
        }

        private unsafe ModelCamera _processCamera(AssNode* node, AssCamera* camera, ModelNode parent) {
            return new ModelCamera(
                node->MName,
                node->MTransformation,
                camera->MLookAt,
                camera->MUp,
                camera->MHorizontalFOV,
                camera->MClipPlaneNear,
                camera->MClipPlaneFar,
                camera->MAspect,
                parent
            );
        }

        public void Dispose() {

        }
    }



    public class ModelNode {
        public string Name {get; protected set;}
        public ModelNode Parent {get; protected set;}
        public Matrix4x4 Transform {get; protected set;}
        public List<ModelNode> Children {get; protected set;}

        public Matrix4x4 ModelTransform {
            get => (Parent?.ModelTransform ?? Matrix4x4.Identity) * Transform;
        }

        public ModelNode(string name, Matrix4x4 transform, ModelNode parent) {
            Name = name;
            Transform = transform;
            Parent = parent;
        }
    }

    public class ModelCamera : ModelNode {
        public Vector3 Position {get; private set;}
        public Vector3 Target {get; private set;}
        public Vector3 Up {get; private set;}

        public float Far {get; private set;}
        public float Near {get; private set;}
        public float Fov {get; private set;}
        public float AspectRatio {get; private set;}

        // TODO: Refactor constructor
        public ModelCamera(string name, Matrix4x4 transform, Vector3 target, Vector3 up, float fov, float far, float near, float aspectRatio, ModelNode parent) : base(name, transform, parent) {

        }
    }


    [Serializable]
    public class ModelLoadingException : Exception {
        public ModelLoadingException() { }
        public ModelLoadingException(string message) : base(message) { }
        public ModelLoadingException(string message, Exception inner) : base(message, inner) { }
        protected ModelLoadingException(
            System.Runtime.Serialization.SerializationInfo info,
            System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
}